using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using TimeLeaf.UseCases;

namespace TimeLeaf.ViewModels;

/// <summary>
/// プロジェクトの変更を監視し、自動保存を調整するコーディネーター。
/// </summary>
public class ProjectSaveCoordinator : IProjectSaveCoordinator
{
    private readonly ISaveProjectUseCase _saveUseCase;
    private readonly ILogger<ProjectSaveCoordinator> _logger;
    private ObservableCollection<ProjectViewModel>? _monitoredProjects;

    public bool IsEnabled { get; set; } = true;

    public ProjectSaveCoordinator(ISaveProjectUseCase saveUseCase, ILogger<ProjectSaveCoordinator> logger)
    {
        _saveUseCase = saveUseCase;
        _logger = logger;
    }

    public void StartMonitoring(ObservableCollection<ProjectViewModel> projects)
    {
        if (_monitoredProjects != null)
        {
            _monitoredProjects.CollectionChanged -= OnProjectsCollectionChanged;
            foreach (var vm in _monitoredProjects)
            {
                vm.PropertyChanged -= OnProjectPropertyChanged;
            }
        }

        _monitoredProjects = projects;
        _monitoredProjects.CollectionChanged += OnProjectsCollectionChanged;

        foreach (var vm in _monitoredProjects)
        {
            vm.PropertyChanged += OnProjectPropertyChanged;
        }

        _logger.LogInformation("Started monitoring projects for auto-save.");
    }

    private void OnProjectsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (ProjectViewModel vm in e.NewItems)
            {
                vm.PropertyChanged += OnProjectPropertyChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (ProjectViewModel vm in e.OldItems)
            {
                vm.PropertyChanged -= OnProjectPropertyChanged;
            }
        }
    }

    private async void OnProjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!IsEnabled) return;
        if (sender is not ProjectViewModel vm) return;
        if (vm.IsSyncing) return;

        // 保存対象となる主要なデータプロパティの変更を監視
        if (e.PropertyName == nameof(ProjectViewModel.Name) ||
            e.PropertyName == nameof(ProjectViewModel.Description) ||
            e.PropertyName == nameof(ProjectViewModel.Status) ||
            e.PropertyName == nameof(ProjectViewModel.HealthStatus) ||
            e.PropertyName == nameof(ProjectViewModel.Tasks) ||
            e.PropertyName == nameof(ProjectViewModel.Milestones))
        {
            await AutoSaveProjectAsync(vm);
        }
    }

    private async System.Threading.Tasks.Task AutoSaveProjectAsync(ProjectViewModel vm)
    {
        _logger.LogInformation("Auto-save triggered for project {ProjectId}", vm.Id);
        try
        {
            await _saveUseCase.ExecuteAsync(vm.Model);
            _logger.LogInformation("Auto-save completed for project {ProjectId}", vm.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-save project {ProjectId}", vm.Id);
        }
    }

    public void Dispose()
    {
        if (_monitoredProjects != null)
        {
            _monitoredProjects.CollectionChanged -= OnProjectsCollectionChanged;
            foreach (var vm in _monitoredProjects)
            {
                vm.PropertyChanged -= OnProjectPropertyChanged;
            }
        }
    }
}
