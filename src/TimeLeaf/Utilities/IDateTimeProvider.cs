using System;

namespace TimeLeaf.Utilities;

/// <summary>
/// 現在日時を提供するためのインターフェース。
/// テスト時に現在日時をモックするために使用します。
/// </summary>
public interface IDateTimeProvider
{
    DateTime Now { get; }
}
