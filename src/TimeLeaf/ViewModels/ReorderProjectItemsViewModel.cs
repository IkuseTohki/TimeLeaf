using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeafKit.UI.Services;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;
using TimeLeaf.ViewModels.Workspace;

namespace TimeLeaf.ViewModels;

public partial class ReorderProjectItemsViewModel : ObservableObject, IDialogViewModel
{
    private readonly IWorkItemContainer _rootContainer;
    private readonly Dictionary<IWorkItemContainer, List<Guid>> _originalOrders = new();

    [ObservableProperty]
    private ObservableCollection<ProjectTaskDisplayItem> _items = default!;

    public event Action<bool>? RequestClose;

    public ReorderProjectItemsViewModel(IWorkItemContainer rootContainer)
    {
        _rootContainer = rootContainer ?? throw new ArgumentNullException(nameof(rootContainer));

        // 変更前の状態をスナップショットとして保存（キャンセル時に復元するため）
        CaptureOrders(_rootContainer);

        RefreshItems();
    }

    private void CaptureOrders(IWorkItemContainer container)
    {
        _originalOrders[container] = container.Children.Select(c => c.Id).ToList();
        foreach (var child in container.Children)
        {
            if (child is IWorkItemContainer childContainer)
            {
                CaptureOrders(childContainer);
            }
        }
    }

    private void RefreshItems()
    {
        Items = new ObservableCollection<ProjectTaskDisplayItem>(Flatten(_rootContainer.Children, 0));
    }

    private IEnumerable<ProjectTaskDisplayItem> Flatten(IEnumerable<ProjectWorkItem> items, int depth)
    {
        foreach (var item in items)
        {
            yield return new ProjectTaskDisplayItem(item, depth, item is IWorkItemContainer);
            if (item is IWorkItemContainer container)
            {
                foreach (var child in Flatten(container.Children, depth + 1))
                {
                    yield return child;
                }
            }
        }
    }

    [RelayCommand]
    private void Confirm() => RequestClose?.Invoke(true);

    [RelayCommand]
    private void Cancel()
    {
        // 変更を破棄して元の順序に復元
        foreach (var entry in _originalOrders)
        {
            var container = entry.Key;
            var order = entry.Value;
            for (int i = 0; i < order.Count; i++)
            {
                container.MoveChild(order[i], i);
            }
        }
        RequestClose?.Invoke(false);
    }

    [RelayCommand(CanExecute = nameof(CanMoveItem))]
    private void MoveItem(object parameter)
    {
        if (
            parameter is not object[] values
            || values.Length < 2
            || values[0] is not ProjectTaskDisplayItem displayItem
        )
            return;

        bool moveUp = bool.Parse(values[1].ToString() ?? "false");

        var parent = FindParent(_rootContainer, displayItem.Item.Id);
        if (parent == null)
            return;

        var children = parent.Children.ToList();
        int currentIndex = children.FindIndex(c => c.Id == displayItem.Item.Id);
        int newIndex = moveUp ? currentIndex - 1 : currentIndex + 1;

        if (newIndex >= 0 && newIndex < children.Count)
        {
            parent.MoveChild(displayItem.Item.Id, newIndex);
            RefreshItems();

            // 全ての移動ボタンの有効・無効状態を更新
            MoveItemCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanMoveItem(object? parameter)
    {
        if (
            parameter is not object[] values
            || values.Length < 2
            || values[0] is not ProjectTaskDisplayItem displayItem
            || values[1] == null
        )
            return false;

        bool moveUp = bool.Parse(values[1].ToString() ?? "false");

        var parent = FindParent(_rootContainer, displayItem.Item.Id);
        if (parent == null)
            return false;

        var children = parent.Children.ToList();
        int currentIndex = children.FindIndex(c => c.Id == displayItem.Item.Id);
        int newIndex = moveUp ? currentIndex - 1 : currentIndex + 1;

        return newIndex >= 0 && newIndex < children.Count;
    }

    private IWorkItemContainer? FindParent(IWorkItemContainer current, Guid itemId)
    {
        if (current.Children.Any(c => c.Id == itemId))
            return current;

        foreach (var child in current.Children)
        {
            if (child is IWorkItemContainer childContainer)
            {
                var found = FindParent(childContainer, itemId);
                if (found != null)
                    return found;
            }
        }
        return null;
    }
}
