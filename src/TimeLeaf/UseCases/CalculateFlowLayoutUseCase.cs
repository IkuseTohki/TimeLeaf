using System;
using System.Collections.Generic;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// タスクの依存関係と階層構造に基づいたフロー図（DAG）のレイアウトを算出するユースケース。
/// </summary>
public class CalculateFlowLayoutUseCase
{
    /// <summary>
    /// 各タスクの描画位置情報を含むレイアウトマップを返します。
    /// </summary>
    /// <param name="project">対象プロジェクト。</param>
    /// <returns>タスクIDをキーとした座標マップ。</returns>
    public virtual Dictionary<Guid, (double X, double Y)> Execute(Project project)
    {
        // 簡易実装：タスクの依存関係に基づく階層的レイアウト
        // 実際にはDAGのレベル分けを行い、座標を決定するロジックを実装する
        var layout = new Dictionary<Guid, (double X, double Y)>();

        var tasks = project.Tasks;
        int index = 0;
        foreach (var task in tasks)
        {
            // 仮のグリッド配置
            layout[task.Id] = (index * 100.0, 50.0);
            index++;
        }

        return layout;
    }
}
