using System;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.Models.Entities;

/// <summary>
/// 検出されたプロジェクト・リスクを表すクラス。
/// </summary>
/// <param name="Type">リスクの種類。</param>
/// <param name="Message">ユーザー向けのメッセージ。</param>
/// <param name="TaskId">関連するタスクのID（あれば）。</param>
/// <param name="IsError">致命的なエラー（異常）かどうか。falseの場合は警告。</param>
public record ProjectRisk(RiskType Type, string Message, Guid? TaskId = null, bool IsError = false);
