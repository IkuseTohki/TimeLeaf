<#
.SYNOPSIS
    プロジェクト全域からリソース（x:Key）を抽出し、インベントリ（目録）を作成します。
.DESCRIPTION
    TimeLeaf および LeafKit.UI のリソースファイルを走査し、キー名、定義ファイルパス、
    および（可能な場合は）定義内容をオブジェクトとして返します。
#>
param(
    [string[]]$ResourceDirectories = @(
        "src/TimeLeaf/Resources",
        "LeafKit/UI/src/LeafKit.UI/Resources"
    )
)

$inventory = @{}

foreach ($dir in $ResourceDirectories) {
    if (-not (Test-Path $dir)) { continue }
    
    $files = Get-ChildItem -Path $dir -Filter "*.xaml" -Recurse
    foreach ($file in $files) {
        [xml]$xml = Get-Content $file.FullName
        
        # 名前空間の解決
        $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
        $ns.AddNamespace("x", "http://schemas.microsoft.com/winfx/2006/xaml")
        $ns.AddNamespace("def", "http://schemas.microsoft.com/winfx/2006/xaml/presentation")

        # x:Key を持つすべての要素を抽出
        $nodes = $xml.SelectNodes("//*[@x:Key]", $ns)
        foreach ($node in $nodes) {
            $key = $node.Attributes["x:Key", "http://schemas.microsoft.com/winfx/2006/xaml"].Value
            if (-not $key) { $key = $node.GetAttribute("x:Key") } # フォールバック

            $item = [PSCustomObject]@{
                Key = $key
                FilePath = $file.FullName
                RelativePath = $file.FullName.Replace((Get-Location).Path + "\", "")
                Type = $node.LocalName
                RawXml = $node.OuterXml
            }
            
            # 同一キーがある場合は警告（WPFの仕様上、マージ順で上書きされるため最後を優先）
            $inventory[$key] = $item
        }
    }
}

return $inventory
