using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.UseCases;

using Microsoft.Extensions.DependencyInjection;
using LeafKit.UI.Services;

namespace TimeLeaf.ViewModels;

/// <summary>
/// プロジェクトオーバービュー画面のロジックを担当するViewModel。
/// </summary>
public partial class OverviewViewModel : ObservableObject
{
    private readonly IAddProjectUseCase _addProjectUseCase;
    private readonly IDialogService _dialogService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OverviewViewModel> _logger;

    /// <summary>
    /// 表示対象となるプロジェクトのリスト。
    /// </summary>
    public ObservableCollection<ProjectViewModel> Projects { get; }

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="projects">共有プロジェクトリスト。</param>
    /// <param name="addProjectUseCase">プロジェクト追加ユースケース。</param>
    /// <param name="dialogService">ダイアログサービス。</param>
    /// <param name="serviceProvider">サービスプロバイダー。</param>
    /// <param name="logger">ロガー。</param>
    public OverviewViewModel(
        ObservableCollection<ProjectViewModel> projects,
        IAddProjectUseCase addProjectUseCase,
        IDialogService dialogService,
        IServiceProvider serviceProvider,
        ILogger<OverviewViewModel> logger)
    {
        Projects = projects;
        _addProjectUseCase = addProjectUseCase;
        _dialogService = dialogService;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _logger.LogInformation("OverviewViewModel initialized.");
    }

    /// <summary>
    /// プロジェクト作成ダイアログを表示し、新規プロジェクトを追加します。
    /// </summary>
    [RelayCommand]
    private async System.Threading.Tasks.Task AddProject()
    {
        _logger.LogInformation("ShowAddProjectDialog started.");

        try
        {
            var addProjectVm = _serviceProvider.GetRequiredService<AddProjectViewModel>();
            var result = await _dialogService.ShowDialogAsync(addProjectVm);

            if (result)
            {
                _logger.LogDebug("Adding project: {Name}", addProjectVm.Name);

                var projectEntity = await _addProjectUseCase.ExecuteAsync(
                    addProjectVm.Name,
                    addProjectVm.Description,
                    addProjectVm.Status,
                    addProjectVm.Health);

                var projectViewModel = new ProjectViewModel(projectEntity);
                Projects.Add(projectViewModel);

                _logger.LogInformation("AddProject completed successfully. Created project {ProjectId}", projectViewModel.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add project.");
        }
    }
}
