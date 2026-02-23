using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.UseCases;

namespace TimeLeaf.ViewModels;

/// <summary>
/// プロジェクトオーバービュー画面のロジックを担当するViewModel。
/// </summary>
public partial class OverviewViewModel : ObservableObject
{
    private readonly IAddProjectUseCase _addProjectUseCase;
    private readonly ILogger<OverviewViewModel> _logger;

    public IEnumerable<ProjectStatus> ProjectStatusValues => (ProjectStatus[])Enum.GetValues(typeof(ProjectStatus));
    public IEnumerable<ProjectHealth> ProjectHealthValues => (ProjectHealth[])Enum.GetValues(typeof(ProjectHealth));

    [ObservableProperty]
    private string _newProjectName = string.Empty;

    [ObservableProperty]
    private string _newProjectDescription = string.Empty;

    [ObservableProperty]
    private ProjectStatus _newProjectStatus = ProjectStatus.Initial;

    [ObservableProperty]
    private ProjectHealth _newProjectHealth = ProjectHealth.Healthy;

    /// <summary>
    /// 表示対象となるプロジェクトのリスト。
    /// </summary>
    public ObservableCollection<ProjectViewModel> Projects { get; }

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="projects">共有プロジェクトリスト。</param>
    /// <param name="addProjectUseCase">プロジェクト追加ユースケース。</param>
    /// <param name="logger">ロガー。</param>
    public OverviewViewModel(ObservableCollection<ProjectViewModel> projects, IAddProjectUseCase addProjectUseCase, ILogger<OverviewViewModel> logger)
    {
        Projects = projects;
        _addProjectUseCase = addProjectUseCase;
        _logger = logger;
        _logger.LogInformation("OverviewViewModel initialized.");
    }

    /// <summary>
    /// 新規プロジェクトを追加するコマンド。
    /// </summary>
    [RelayCommand]
    private async System.Threading.Tasks.Task AddProject()
    {
        _logger.LogInformation("AddProject started for {NewProjectName}", NewProjectName);
        if (string.IsNullOrWhiteSpace(NewProjectName))
        {
            _logger.LogWarning("AddProject aborted: NewProjectName is empty.");
            return;
        }

        try
        {
            _logger.LogDebug("Calling AddProjectUseCase with Name: {Name}, Description: {Description}, Status: {Status}, Health: {Health}",
                NewProjectName, NewProjectDescription, NewProjectStatus, NewProjectHealth);

            var projectEntity = await _addProjectUseCase.ExecuteAsync( // Projectエンティティとして受け取る
                NewProjectName,
                NewProjectDescription,
                NewProjectStatus,
                NewProjectHealth);

            var projectViewModel = new ProjectViewModel(projectEntity); // ViewModelでラップ
            Projects.Add(projectViewModel); // ViewModelをコレクションに追加

            NewProjectName = string.Empty;
            NewProjectDescription = string.Empty;
            NewProjectStatus = ProjectStatus.Initial;
            NewProjectHealth = ProjectHealth.Healthy;

            _logger.LogInformation("AddProject completed successfully. Created project {ProjectId}", projectViewModel.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add project {ProjectName}.", NewProjectName);
        }
    }
}
