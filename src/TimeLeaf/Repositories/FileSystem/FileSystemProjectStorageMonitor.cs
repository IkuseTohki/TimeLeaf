using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace TimeLeaf.Repositories.FileSystem;

/// <summary>
/// FileSystemWatcher を使用してプロジェクトストレージの変更を監視するクラス。
/// </summary>
public class FileSystemProjectStorageMonitor : IProjectStorageMonitor
{
    private readonly string _baseDirectory;
    private readonly ILogger<FileSystemProjectStorageMonitor> _logger;
    private readonly FileSystemWatcher _watcher;
    private readonly ConcurrentDictionary<string, DateTime> _justWrittenFiles = new();

    public event Action<Guid>? ProjectChanged;

    public FileSystemProjectStorageMonitor(string baseDirectory, ILogger<FileSystemProjectStorageMonitor> logger)
    {
        _baseDirectory = baseDirectory;
        _logger = logger;

        if (!Directory.Exists(_baseDirectory))
        {
            Directory.CreateDirectory(_baseDirectory);
        }

        _watcher = new FileSystemWatcher(_baseDirectory)
        {
            IncludeSubdirectories = true,
            Filter = "*.json",
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
        };
        _watcher.Created += OnFileCreated;
        _watcher.Changed += OnFileCreated;
        _watcher.EnableRaisingEvents = true;
        _logger.LogDebug("FileSystemWatcher initialized for {BaseDirectory}", _baseDirectory);
    }

    public void MarkFileAsJustWritten(string fileName)
    {
        _justWrittenFiles[fileName] = DateTime.Now;
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        var fileName = Path.GetFileName(e.FullPath);
        if (_justWrittenFiles.TryGetValue(fileName, out var writeTime))
        {
            // 書き込みから1秒以内のイベントは無視する
            if (DateTime.Now - writeTime < TimeSpan.FromSeconds(1))
            {
                _logger.LogDebug("Ignoring file change event for our own write (within 1s): {FileName}", fileName);
                return;
            }
            else
            {
                // 1秒以上経過している場合はリストから削除して、外部変更として扱う
                _justWrittenFiles.TryRemove(fileName, out _);
            }
        }

        _logger.LogInformation(
            "File created/changed event detected: {FullPath} (ChangeType: {ChangeType})",
            e.FullPath,
            e.ChangeType
        );

        var relativePath = Path.GetRelativePath(_baseDirectory, e.FullPath);
        var pathParts = relativePath.Split(Path.DirectorySeparatorChar);

        if (pathParts.Length >= 2)
        {
            var projectDirName = pathParts[0];
            var idPart = projectDirName.Split('_')[0];

            if (Guid.TryParse(idPart, out var projectId))
            {
                _logger.LogDebug(
                    "Project ID {ProjectId} extracted from path. Invoking ProjectChanged event.",
                    projectId
                );
                ProjectChanged?.Invoke(projectId);
            }
        }

        // 定期的に古い無視リストを掃除する
        if (_justWrittenFiles.Count > 100)
        {
            CleanupIgnoreList();
        }
    }

    private void CleanupIgnoreList()
    {
        var now = DateTime.Now;
        var toRemove = _justWrittenFiles
            .Where(kv => now - kv.Value > TimeSpan.FromSeconds(10))
            .Select(kv => kv.Key)
            .ToList();
        foreach (var key in toRemove)
        {
            _justWrittenFiles.TryRemove(key, out _);
        }
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }
}
