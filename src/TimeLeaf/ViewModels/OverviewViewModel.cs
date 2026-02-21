using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.ViewModels;

/// <summary>
/// プロジェクトオーバービュー画面のロジックを担当するViewModel。
/// </summary>
public partial class OverviewViewModel : ObservableObject
{
    public IEnumerable<ProjectStatus> ProjectStatusValues => Enum.GetValues<ProjectStatus>();
    public IEnumerable<ProjectHealth> ProjectHealthValues => Enum.GetValues<ProjectHealth>();

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
    public ObservableCollection<Project> Projects { get; }

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="projects">共有プロジェクトリスト。</param>
    public OverviewViewModel(ObservableCollection<Project> projects)
    {
        Projects = projects;
    }

    /// <summary>
    /// 新規プロジェクトを追加するコマンド。
    /// </summary>
    [RelayCommand]
    private void AddProject()
    {
        if (string.IsNullOrWhiteSpace(NewProjectName)) return;

        var project = new Project
        {
            Name = NewProjectName,
            Description = NewProjectDescription,
            Status = NewProjectStatus,
            HealthStatus = NewProjectHealth
        };
        Projects.Add(project);

        NewProjectName = string.Empty;
        NewProjectDescription = string.Empty;
        NewProjectStatus = ProjectStatus.Initial;
        NewProjectHealth = ProjectHealth.Healthy;
    }
}
