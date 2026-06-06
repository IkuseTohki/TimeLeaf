<#
.SYNOPSIS
    プロジェクト内の XAML ファイルを走査し、あらゆる形式のリソース参照エラーを検出します。
.DESCRIPTION
    - Markup Extension 形式 {StaticResource ...}
    - タグ形式 <StaticResourceExtension ResourceKey="..." />
    - Style BasedOn="..."
    上記すべてを対象に、App.xaml のマージ順序およびファイル内定義順序に基づいた解決可能性を検証します。
#>
param(
    [string[]]$SearchDirectories = @(
        "src/TimeLeaf",
        "LeafKit/UI/src/LeafKit.UI"
    )
)

$scriptDir = Split-Path $PSCommandPath
$inventory = & "$scriptDir/Get-ResourceInventory.ps1"

# App.xaml のマージ順序を正確に取得
[xml]$appXml = Get-Content "src/TimeLeaf/App.xaml"
$ns = New-Object System.Xml.XmlNamespaceManager($appXml.NameTable)
$ns.AddNamespace("def", "http://schemas.microsoft.com/winfx/2006/xaml/presentation")
$appMerged = $appXml.SelectNodes("//def:ResourceDictionary.MergedDictionaries/def:ResourceDictionary", $ns) | ForEach-Object { 
    $_.Source
}

function Resolve-XamlPath($currentFile, $sourceUri) {
    if ($sourceUri -match "LeafKit.UI;component/(.+)") { return "LeafKit/UI/src/LeafKit.UI/$($Matches[1])" }
    if ($sourceUri.StartsWith("/")) { return "src/TimeLeaf/$($sourceUri.TrimStart('/'))" }
    $dir = Split-Path $currentFile
    $combined = Join-Path $dir $sourceUri
    if (Test-Path $combined) { return $combined }
    return "src/TimeLeaf/$sourceUri"
}

$cache = @{}
function Get-AvailableKeys($filePath, $visited = @()) {
    if (-not (Test-Path $filePath)) { return @() }
    $absPath = (Resolve-Path $filePath).Path
    if ($visited -contains $absPath) { return @() }
    if ($cache.ContainsKey($absPath)) { return $cache[$absPath] }
    
    $visited += $absPath
    $keys = @()
    [xml]$xml = Get-Content $absPath
    $fileNs = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $fileNs.AddNamespace("x", "http://schemas.microsoft.com/winfx/2006/xaml")
    $fileNs.AddNamespace("def", "http://schemas.microsoft.com/winfx/2006/xaml/presentation")
    
    # 定義されているキーを収集
    $xml.SelectNodes("//*[@x:Key]", $fileNs) | ForEach-Object { 
        $k = $_.Attributes["x:Key", "http://schemas.microsoft.com/winfx/2006/xaml"].Value
        if (-not $k) { $k = $_.GetAttribute("x:Key") }
        if ($k) { $keys += $k }
    }

    # マージ先を再帰探索
    $merged = $xml.SelectNodes("//def:ResourceDictionary.MergedDictionaries/def:ResourceDictionary", $fileNs)
    foreach ($m in $merged) {
        $keys += Get-AvailableKeys (Resolve-XamlPath $absPath $m.Source) $visited
    }
    
    $result = $keys | Select-Object -Unique
    $cache[$absPath] = $result
    return $result
}

$files = Get-ChildItem -Path "src/TimeLeaf" -Filter "*.xaml" -Recurse
$errorCount = 0

Write-Host "--- Scanning ALL XAML Resource References (Advanced Integrity Check) ---" -ForegroundColor Cyan

# App.xaml の全キー（ベースコンテキスト）を事前計算
$fullAppContextKeys = @()
foreach ($m in $appMerged) {
    $fullAppContextKeys += Get-AvailableKeys (Resolve-XamlPath "src/TimeLeaf/App.xaml" $m)
}

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $relativeExpr = $file.FullName.Replace((Get-Location).Path + "\", "").Replace("\", "/")
    
    # このファイルで利用可能なキー（外部から供給されるもの）を構築
    $externalKeys = @()
    $normalizedRel = $relativeExpr.Replace("src/TimeLeaf/", "")
    $isAppMergedFile = $false
    foreach ($m in $appMerged) {
        $mResolved = Resolve-XamlPath "src/TimeLeaf/App.xaml" $m
        if ($mResolved -eq $file.FullName) { $isAppMergedFile = $true; break }
        $externalKeys += Get-AvailableKeys $mResolved
    }
    if (-not $isAppMergedFile) {
        # View の場合は App.xaml 全体を利用可能
        $externalKeys = $fullAppContextKeys
        # ファイル自身のマージも追加
        $externalKeys += Get-AvailableKeys $file.FullName
    }
    
    # ファイル内の定義順序を解析
    [xml]$xml = [xml]$content
    $fileNs = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $fileNs.AddNamespace("x", "http://schemas.microsoft.com/winfx/2006/xaml")
    $fileNs.AddNamespace("def", "http://schemas.microsoft.com/winfx/2006/xaml/presentation")
    
    $definedInThisFile = @()
    $fileHeaderPrinted = $false

    # ファイルを行ごとに読み込み、定義と参照の順序を確認する
    $lines = Get-Content $file.FullName
    $currentlyDefinedKeys = $externalKeys.Clone()

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        
        # 1. 定義の検出
        if ($line -match 'x:Key="([^"]+)"') {
            $newKey = $Matches[1]
            $currentlyDefinedKeys += $newKey
        }

        # 2. 参照の検出
        $matches = [regex]::Matches($line, '\{(StaticResource|DynamicResource|StaticResourceExtension|DynamicResourceExtension)\s+([^}, ]+)\}')
        # BasedOn や ResourceKey 属性も同様にチェック
        $attrMatches = [regex]::Matches($line, '(?:BasedOn|ResourceKey|Source|Value)="{StaticResource\s+([^}]+)\}"')
        $tagMatches = [regex]::Matches($line, '<(?:StaticResourceExtension|DynamicResourceExtension)[^>]+ResourceKey="([^"]+)"')

        $refs = @()
        foreach ($m in $matches) { $refs += [PSCustomObject]@{ Key = $m.Groups[2].Value.Trim(); Type = $m.Groups[1].Value } }
        foreach ($m in $attrMatches) { $refs += [PSCustomObject]@{ Key = $m.Groups[1].Value.Trim(); Type = "StaticResource" } }
        foreach ($m in $tagMatches) { $refs += [PSCustomObject]@{ Key = $m.Groups[1].Value.Trim(); Type = "StaticResource" } }

        foreach ($ref in $refs) {
            $key = $ref.Key
            if ($key -match "x:Static|x:Type|Binding|RelativeSource|TemplateBinding|ComponentResourceKey") { continue }

            $existsInProject = $inventory.ContainsKey($key) -or ($currentlyDefinedKeys -contains $key)
            $resolvable = ($currentlyDefinedKeys -contains $key)

            if (-not $existsInProject) {
                if (-not $fileHeaderPrinted) { Write-Host "`nIn file: $relativeExpr" -ForegroundColor Yellow; $fileHeaderPrinted = $true }
                Write-Host "  [MISSING] $key (Line $($i+1))" -ForegroundColor Red
                $errorCount++
            } elseif ($ref.Type -match "Static" -and -not $resolvable) {
                if (-not $fileHeaderPrinted) { Write-Host "`nIn file: $relativeExpr" -ForegroundColor Yellow; $fileHeaderPrinted = $true }
                Write-Host "  [ORDER ERR] $key (Line $($i+1)): Defined later in file or in later dictionary" -ForegroundColor Red
                $errorCount++
            }
        }
    }
}

Write-Host "`n--- Scan Complete ---" -ForegroundColor Cyan
if ($errorCount -eq 0) {
    Write-Host "No resource issues found. Integrity OK!" -ForegroundColor Green
} else {
    Write-Host "Found $errorCount issue(s). Please fix them." -ForegroundColor Red
    exit 1
}
