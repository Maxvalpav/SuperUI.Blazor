using System;
using System.Collections.Generic;
using System.Linq;
using SuperUI.Components.SgGantCanvas.Models;

namespace SuperUI.Components.SgGantCanvas.Services;

public class GanttCriticalPathEngine
{
    public List<string> CalculateCriticalPath(List<GanttTask> tasks, List<GanttDependency> dependencies)
    {
        if (tasks == null || !tasks.Any()) return new List<string>();

        // 1. Initialize ES, EF, LS, LF
        var es = tasks.ToDictionary(t => t.Id, t => 0.0);
        var ef = tasks.ToDictionary(t => t.Id, t => (t.End - t.Start).TotalDays);
        
        // 2. Forward Pass
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var dep in dependencies)
            {
                if (ef.ContainsKey(dep.FromTaskId) && es.ContainsKey(dep.ToTaskId))
                {
                    var newES = ef[dep.FromTaskId] + dep.Lag.TotalDays;
                    if (newES > es[dep.ToTaskId])
                    {
                        es[dep.ToTaskId] = newES;
                        var task = tasks.First(t => t.Id == dep.ToTaskId);
                        ef[dep.ToTaskId] = newES + (task.End - task.Start).TotalDays;
                        changed = true;
                    }
                }
            }
        }

        // 3. Backward Pass (simplified)
        var maxEF = ef.Values.Any() ? ef.Values.Max() : 0;
        var lf = tasks.ToDictionary(t => t.Id, t => maxEF);
        var ls = tasks.ToDictionary(t => t.Id, t => maxEF - (t.End - t.Start).TotalDays);

        changed = true;
        while (changed)
        {
            changed = false;
            foreach (var dep in dependencies)
            {
                if (ls.ContainsKey(dep.ToTaskId) && lf.ContainsKey(dep.FromTaskId))
                {
                    var newLF = ls[dep.ToTaskId] - dep.Lag.TotalDays;
                    if (newLF < lf[dep.FromTaskId])
                    {
                        lf[dep.FromTaskId] = newLF;
                        var task = tasks.First(t => t.Id == dep.FromTaskId);
                        ls[dep.FromTaskId] = newLF - (task.End - task.Start).TotalDays;
                        changed = true;
                    }
                }
            }
        }

        // 4. Critical Path: tasks with slack near zero
        return tasks.Where(t => Math.Abs(lf[t.Id] - ef[t.Id]) < 0.01).Select(t => t.Id).ToList();
    }
}
