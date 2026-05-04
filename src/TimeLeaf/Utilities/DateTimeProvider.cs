using System;

namespace TimeLeaf.Utilities;

/// <summary>
/// 現在日時を提供するための標準的な実装クラス。
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    public DateTime Now => DateTime.Now;
}
