using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// プロジェクトのクリティカルパスを算出するユースケース。
/// </summary>
public class CalculateCriticalPathUseCase
{
    /// <summary>
    /// 指定されたプロジェクトのクリティカルパスに含まれるタスクIDのリストを返します。
    /// </summary>
    public virtual Task<IEnumerable<Guid>> ExecuteAsync(Project project)
    {
        var tasks = project.Tasks.Where(t => t.Children.Count == 0).ToList();
        if (!tasks.Any())
            return Task.FromResult(Enumerable.Empty<Guid>());

        var taskDict = tasks.ToDictionary(t => t.Id);
        var successors = tasks.ToDictionary(t => t.Id, _ => new List<Guid>());
        foreach (var t in tasks)
        {
            foreach (var c in t.Constraints)
            {
                if (successors.ContainsKey(c.PredecessorId))
                {
                    successors[c.PredecessorId].Add(t.Id);
                }
            }
        }

        var sorted = TopologicalSort(tasks, taskDict);
        if (sorted == null)
            return Task.FromResult(Enumerable.Empty<Guid>());

        var es = new Dictionary<Guid, DateTime>();
        var ef = new Dictionary<Guid, DateTime>();
        var baseDate = project.CreatedAt.Date;

        // 2. Forward Pass
        foreach (var t in sorted)
        {
            var duration = GetDurationDays(t);
            var predEfs = t
                .Constraints.Where(c => ef.ContainsKey(c.PredecessorId))
                .Select(c => ef[c.PredecessorId].AddDays(c.LagDays))
                .ToList();

            var scheduledStart = t.ScheduledStartDate?.Date ?? baseDate;
            var maxPredEf = predEfs.Any() ? predEfs.Max() : baseDate;

            es[t.Id] = maxPredEf > scheduledStart ? maxPredEf : scheduledStart;
            ef[t.Id] = es[t.Id].AddDays(duration);
        }

        if (!ef.Any())
            return Task.FromResult(Enumerable.Empty<Guid>());

        // 3. Backward Pass
        var ls = new Dictionary<Guid, DateTime>();
        var lf = new Dictionary<Guid, DateTime>();
        var projectFinish = ef.Values.Max();

        // 後続タスクを効率的に引くための逆引き辞書を構築（制約付き）
        var constraintsByPredecessor = tasks
            .SelectMany(t => t.Constraints.Select(c => new { SuccessorId = t.Id, Constraint = c }))
            .GroupBy(x => x.Constraint.PredecessorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var t in sorted.AsEnumerable().Reverse())
        {
            var duration = GetDurationDays(t);

            List<DateTime> successorLss = new();
            if (constraintsByPredecessor.TryGetValue(t.Id, out var succs))
            {
                foreach (var s in succs)
                {
                    if (ls.ContainsKey(s.SuccessorId))
                    {
                        successorLss.Add(ls[s.SuccessorId].AddDays(-s.Constraint.LagDays));
                    }
                }
            }

            lf[t.Id] = successorLss.Any() ? successorLss.Min() : projectFinish;
            ls[t.Id] = lf[t.Id].AddDays(-duration);
        }

        // 4. Slack 算出とクリティカルパス抽出
        var criticalPath = tasks.Where(t => (lf[t.Id] - ef[t.Id]).TotalDays < 0.1).Select(t => t.Id).ToList();

        return Task.FromResult<IEnumerable<Guid>>(criticalPath);
    }

    private List<ProjectTask>? TopologicalSort(List<ProjectTask> tasks, Dictionary<Guid, ProjectTask> taskDict)
    {
        var result = new List<ProjectTask>();
        var visited = new HashSet<Guid>();
        var stack = new HashSet<Guid>();

        foreach (var t in tasks)
        {
            if (!Visit(t.Id, taskDict, visited, stack, result))
                return null; // Cycle detected
        }

        return result;
    }

    private bool Visit(
        Guid id,
        Dictionary<Guid, ProjectTask> dict,
        HashSet<Guid> visited,
        HashSet<Guid> stack,
        List<ProjectTask> result
    )
    {
        if (stack.Contains(id))
            return false;
        if (visited.Contains(id))
            return true;

        visited.Add(id);
        stack.Add(id);

        if (dict.TryGetValue(id, out var t))
        {
            foreach (var c in t.Constraints)
            {
                if (!Visit(c.PredecessorId, dict, visited, stack, result))
                    return false;
            }
        }

        stack.Remove(id);
        result.Add(dict[id]);
        return true;
    }

    private double GetDurationDays(ProjectTask t)
    {
        var start = t.ScheduledStartDate?.Date ?? DateTime.Today.Date;
        var end = t.Deadline?.Date ?? start;
        return (end - start).TotalDays;
    }
}
