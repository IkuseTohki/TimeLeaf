#if !NET5_0_OR_GREATER

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// .NET 5.0 未満（.NET Core 3.1 等）で C# 9.0 の record や init プロパティを使用するためのポリフィル。
    /// </summary>
    internal static class IsExternalInit { }
}

#endif
