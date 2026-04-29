using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LeafKit.UI.ViewModels;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.UseCases;

namespace TimeLeaf.ViewModels.Workspace;

/// <summary>
/// タスクフロー図を表示するための ViewModel。
/// </summary>
public partial class ProjectFlowViewModel : ObservableObject
{
    private readonly ProjectViewModel _projectViewModel;
    private readonly CalculateFlowLayoutUseCase _layoutUseCase;
    private readonly IUserService _userService;

    [ObservableProperty]
    private int _currentDepth = 0; // 0: 全表示, 1: トップレベルのみ, ...

    public ObservableCollection<ProjectTaskViewModel> TaskNodes => _projectViewModel.Tasks;
    public ObservableCollection<TaskEdgeViewModel> TaskEdges { get; } = new();

    public ProjectFlowViewModel(
        ProjectViewModel projectViewModel,
        CalculateFlowLayoutUseCase layoutUseCase,
        IUserService userService
    )
    {
        _projectViewModel = projectViewModel ?? throw new ArgumentNullException(nameof(projectViewModel));
        _layoutUseCase = layoutUseCase ?? throw new ArgumentNullException(nameof(layoutUseCase));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));

        RefreshLayout();
    }

    /// <summary>
    /// タスクの配置を再計算して更新します。
    /// </summary>
    public void RefreshLayout()
    {
        var layout = _layoutUseCase.Execute(_projectViewModel.Model);

        var vmMap = TaskNodes.ToDictionary(vm => vm.Id);

        foreach (var vm in TaskNodes)
        {
            if (layout.TryGetValue(vm.Id, out var pos))
            {
                vm.X = pos.X;
                vm.Y = pos.Y;
            }
        }

        // エッジの生成
        TaskEdges.Clear();
        foreach (var vm in TaskNodes)
        {
            foreach (var constraint in vm.Constraints)
            {
                if (vmMap.TryGetValue(constraint.PredecessorId, out var toVm))
                {
                    // 先行タスクから後続タスクへの線
                    TaskEdges.Add(new TaskEdgeViewModel(toVm, vm));
                }
            }
        }
    }
}
