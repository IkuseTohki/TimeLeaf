using System.Collections.Generic;
using System.Linq;

namespace TimeLeaf.Models.Entities;

/// <summary>
/// プロジェクトの検証結果を保持するクラス。
/// </summary>
public class ProjectValidationResult
{
    private readonly List<string> _errors = new();

    /// <summary>
    /// 検証の結果、有効であるかどうか。
    /// </summary>
    public bool IsValid => !_errors.Any();

    /// <summary>
    /// 発生したエラーメッセージのリスト。
    /// </summary>
    public IReadOnlyList<string> Errors => _errors;

    /// <summary>
    /// エラーを追加します。
    /// </summary>
    public void AddError(string error)
    {
        _errors.Add(error);
    }
}
