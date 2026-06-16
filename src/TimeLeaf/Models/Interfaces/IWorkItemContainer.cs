using System;
using System.Collections.Generic;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Models.Interfaces;

/// <summary>
/// 子要素を持ち、順序を保持できるコンテナの抽象定義。
/// </summary>
public interface IWorkItemContainer
{
    IEnumerable<ProjectWorkItem> Children { get; }
    void MoveChild(Guid childId, int newIndex);
}
