using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeafKit.UI.Services;
using TimeLeaf.Models.Entities;
using TimeLeaf.UseCases;

namespace TimeLeaf.ViewModels;

/// <summary>
/// プロジェクト内のアイテム（タスク・コンテナ）の表示順序を調整するための ViewModel。
/// </summary>
public partial class ReorderWorkItemsViewModel : ObservableObject, IDialogViewModel
{
    private readonly Project _project;
    private readonly IUpdateSortOrderUseCase _updateSortOrderUseCase;

    [ObservableProperty]
    private ObservableCollection<ProjectWorkItem> _items;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
    private ProjectWorkItem? _selectedItem;

    public event Action<bool>? RequestClose;

    public ReorderWorkItemsViewModel(
        Project project,
        IEnumerable<ProjectWorkItem> initialItems,
        IUpdateSortOrderUseCase updateSortOrderUseCase
    )
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _items = new ObservableCollection<ProjectWorkItem>(initialItems ?? Enumerable.Empty<ProjectWorkItem>());
        _updateSortOrderUseCase =
            updateSortOrderUseCase ?? throw new ArgumentNullException(nameof(updateSortOrderUseCase));
    }

    /// <summary>
    /// 選択されたアイテムを一つ上に移動します。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp()
    {
        if (SelectedItem == null)
            return;
        int index = Items.IndexOf(SelectedItem);
        if (index > 0)
        {
            Items.Move(index, index - 1);
        }
    }

    private bool CanMoveUp() => SelectedItem != null && Items.IndexOf(SelectedItem) > 0;

    /// <summary>
    /// 選択されたアイテムを一つ下に移動します。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown()
    {
        if (SelectedItem == null)
            return;
        int index = Items.IndexOf(SelectedItem);
        if (index >= 0 && index < Items.Count - 1)
        {
            Items.Move(index, index + 1);
        }
    }

    private bool CanMoveDown() => SelectedItem != null && Items.IndexOf(SelectedItem) < Items.Count - 1;

    /// <summary>
    /// 変更を確定し、永続化します。
    /// </summary>
    [RelayCommand]
    private async Task Confirm()
    {
        // 現在のリスト順に基づいて ID リストを作成
        var orderedIds = Items.Select(i => i.Id).ToList();

        await _updateSortOrderUseCase.ExecuteAsync(_project, orderedIds);
        RequestClose?.Invoke(true);
    }

    /// <summary>
    /// キャンセルします。
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }
}
