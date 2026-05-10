using System;
using System.IO;

namespace TimeLeaf.Repositories.FileSystem;

/// <summary>
/// プロジェクトのファイル構造を解決するためのユーティリティクラス。
/// </summary>
public class ProjectDirectoryResolver
{
    private readonly string _baseDirectory;

    public ProjectDirectoryResolver(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
    }

    public string GetProjectDirectory(Guid projectId, string projectName) =>
        Path.Combine(_baseDirectory, $"{projectId}_{projectName}");

    public string GetChangesDirectory(Guid projectId, string projectName) =>
        Path.Combine(GetProjectDirectory(projectId, projectName), "changes");

    public string GetEntityDirectory(string changesDir, Guid entityId) => Path.Combine(changesDir, entityId.ToString());

    public string GetMetadataFilePath(Guid projectId, string projectName) =>
        Path.Combine(GetProjectDirectory(projectId, projectName), ".project");
}
