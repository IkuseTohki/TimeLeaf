namespace TimeLeaf.Models.Enums;

/// <summary>
/// タスク間の制約の種類。
/// </summary>
public enum TaskConstraintType
{
    /// <summary>
    /// Finish-to-Start: 先行タスクの完了後に、このタスクを開始できる。
    /// </summary>
    FS,

    /// <summary>
    /// Start-to-Start: 先行タスクの開始後に、このタスクを開始できる。
    /// </summary>
    SS,

    /// <summary>
    /// Finish-to-Finish: 先行タスクの完了時に、このタスクを完了させていなければならない。
    /// </summary>
    FF,

    /// <summary>
    /// Start-to-Finish: 先行タスクの開始時に、このタスクを完了させていなければならない。
    /// </summary>
    SF,
}
