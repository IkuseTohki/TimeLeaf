using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;

namespace TimeLeaf.Services;

/// <summary>
/// IProjectService の実装クラス。
/// プロジェクトデータのキャッシュ管理、保存の委譲、および外部変更のリアクティブな同期を行います。
/// </summary>
public class ProjectService : IProjectService, IDisposable
{
    private readonly IProjectRepository _repository;
    private readonly IIdentityService _identityService;
    private readonly IProjectDiffService _diffService;
    private readonly ILogger<ProjectService> _logger;
    private readonly ConcurrentDictionary<Guid, Project> _cache = new();
    private readonly object _syncLock = new();

    public event Action<Project>? ProjectAdded;
    public event Action<Project>? ProjectUpdated;
    public event Action<Guid>? ProjectRemoved;
    public event Action<Notification>? NotificationRequested;

    public IEnumerable<Project> AllProjects => _cache.Values.OrderBy(p => p.CreatedAt);

    public ProjectService(
        IProjectRepository repository,
        IIdentityService identityService,
        IProjectDiffService diffService,
        ILogger<ProjectService> logger
    )
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        _diffService = diffService ?? throw new ArgumentNullException(nameof(diffService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // リポジトリからの外部変更通知を購読
        _repository.ProjectChanged += OnProjectExternalChanged;
    }

    public async Task LoadAllAsync()
    {
        _logger.LogInformation("Loading all projects into cache...");
        var projects = await _repository.LoadAllAsync();

        lock (_syncLock)
        {
            _cache.Clear();
            foreach (var project in projects)
            {
                _cache[project.Id] = project;
            }
        }
        _logger.LogInformation("{Count} projects loaded into cache.", _cache.Count);
    }

    public async Task<Project?> GetProjectAsync(Guid projectId)
    {
        if (_cache.TryGetValue(projectId, out var project))
        {
            return project;
        }

        project = await _repository.LoadAsync(projectId);
        if (project != null)
        {
            _cache[projectId] = project;
        }
        return project;
    }

    public async Task SaveProjectAsync(Project project)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        _logger.LogDebug("Saving project {ProjectId} ({ProjectName})", project.Id, project.Name);

        var userId = _identityService.CurrentUserId.ToString();
        await _repository.SaveAsync(project, userId);

        // キャッシュを更新または追加
        bool isNew = !_cache.ContainsKey(project.Id);
        _cache[project.Id] = project;

        if (isNew)
        {
            ProjectAdded?.Invoke(project);
        }
        else
        {
            ProjectUpdated?.Invoke(project);
        }
    }

    public async Task SaveAllAsync(IEnumerable<Project> projects)
    {
        if (projects == null)
            throw new ArgumentNullException(nameof(projects));

        var userId = _identityService.CurrentUserId.ToString();
        await _repository.SaveAllAsync(projects, userId);

        foreach (var project in projects)
        {
            _cache[project.Id] = project;
            ProjectUpdated?.Invoke(project);
        }
    }

    public async Task DeleteTaskAsync(Project project, Guid taskId)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        project.RemoveTask(taskId);
        await SaveProjectAsync(project);
    }

    public async Task DeleteContainerAsync(Project project, Guid containerId)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        project.RemoveContainerRecursively(containerId);
        await SaveProjectAsync(project);
    }

    private async void OnProjectExternalChanged(Guid projectId)
    {
        _logger.LogInformation("External change detected for project {ProjectId}. Re-syncing...", projectId);

        try
        {
            var updatedProject = await _repository.LoadAsync(projectId);
            if (updatedProject != null)
            {
                if (_cache.TryGetValue(projectId, out var oldProject))
                {
                    var diff = _diffService.CalculateDiff(oldProject, updatedProject, _identityService.CurrentUserId);

                    foreach (var taskId in diff.AssignedTaskIds)
                    {
                        NotificationRequested?.Invoke(
                            new Notification(
                                "タスクアサイン",
                                $"タスク「{updatedProject.Tasks.FirstOrDefault(t => t.Id == taskId)?.Name}」の担当になりました。",
                                taskId.ToString()
                            )
                        );
                    }

                    foreach (var taskId in diff.NewCommentTaskIds)
                    {
                        NotificationRequested?.Invoke(
                            new Notification(
                                "新着コメント",
                                $"タスク「{updatedProject.Tasks.FirstOrDefault(t => t.Id == taskId)?.Name}」に新しいコメントが追加されました。",
                                taskId.ToString()
                            )
                        );
                    }
                }

                _cache[projectId] = updatedProject;
                ProjectUpdated?.Invoke(updatedProject);

                NotificationRequested?.Invoke(
                    new Notification(
                        "同期成功",
                        $"プロジェクト「{updatedProject.Name}」の変更を同期しました。",
                        updatedProject.Id.ToString()
                    )
                );
            }
            else
            {
                // ロードに失敗（または削除）した場合はキャッシュから削除
                if (_cache.TryRemove(projectId, out var removedProject))
                {
                    ProjectRemoved?.Invoke(projectId);

                    NotificationRequested?.Invoke(
                        new Notification(
                            "同期成功",
                            $"プロジェクト「{removedProject.Name}」が削除されました。",
                            projectId.ToString()
                        )
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while re-syncing project {ProjectId} after external change.", projectId);
            NotificationRequested?.Invoke(
                new Notification(
                    "同期エラー",
                    $"プロジェクトの同期に失敗しました。\n詳細: {ex.Message}",
                    projectId.ToString()
                )
            );
        }
    }

    public void Dispose()
    {
        _repository.ProjectChanged -= OnProjectExternalChanged;
    }
}
