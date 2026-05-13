// SuperUI/Base/Optimization/SgParameterDependencyGraph.cs
// 🆕 Граф зависимостей параметров компонента.
// Автоматически строит DAG параметров.
// При изменении одного параметра перерендериваются только зависимые поддеревья.
// Ни у кого нет.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SuperUI.Base.Optimization;

/// <summary>
/// Represents a dependency between two parameters.
/// </summary>
public sealed class ParameterDependency
{
    public string SourceParameter { get; init; } = null!;
    public string TargetParameter { get; init; } = null!;
    public DependencyKind Kind { get; init; }
}

/// <summary>
/// Kind of dependency.
/// </summary>
public enum DependencyKind
{
    /// <summary>Default value of TargetParameter depends on SourceParameter.</summary>
    DefaultValue,

    /// <summary>Computed from SourceParameter.</summary>
    Computed,

    /// <summary>Validation of SourceParameter affects TargetParameter.</summary>
    Validation,

    /// <summary>TargetParameter is derived from SourceParameter.</summary>
    Derived
}

/// <summary>
/// Directed Acyclic Graph (DAG) of parameter dependencies for a component type.
/// Used to optimize rendering: when parameter X changes, only parameters that
/// depend on X get marked dirty, avoiding re-render for independent parameters.
/// </summary>
public sealed class SgParameterDependencyGraph
{
    private readonly Type _componentType;
    private readonly Dictionary<string, HashSet<string>> _adjacencyList = new();
    private readonly Dictionary<string, HashSet<string>> _reverseAdjacency = new();
    private readonly HashSet<string> _allParameters = new();

    public Type ComponentType => _componentType;
    public int ParameterCount => _allParameters.Count;
    public int DependencyCount => _adjacencyList.Sum(kv => kv.Value.Count);

    public SgParameterDependencyGraph(Type componentType)
    {
        _componentType = componentType;
        BuildGraph();
    }

    /// <summary>
    /// Get all parameters that depend on the given parameter (downstream).
    /// </summary>
    public IReadOnlySet<string> GetDependents(string parameterName)
    {
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(parameterName);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!_adjacencyList.TryGetValue(current, out var dependents))
                continue;

            foreach (var dep in dependents)
            {
                if (visited.Add(dep))
                    queue.Enqueue(dep);
            }
        }

        return visited;
    }

    /// <summary>
    /// Get all parameters that the given parameter depends on (upstream).
    /// </summary>
    public IReadOnlySet<string> GetDependenciesOf(string parameterName)
    {
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(parameterName);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!_reverseAdjacency.TryGetValue(current, out var dependencies))
                continue;

            foreach (var dep in dependencies)
            {
                if (visited.Add(dep))
                    queue.Enqueue(dep);
            }
        }

        return visited;
    }

    /// <summary>
    /// Check if parameter A depends on parameter B (directly or transitively).
    /// </summary>
    public bool DependsOn(string parameterA, string parameterB)
    {
        return GetDependenciesOf(parameterA).Contains(parameterB);
    }

    /// <summary>
    /// Get topological order of parameters (for initialization order).
    /// </summary>
    public List<string> GetTopologicalOrder()
    {
        var result = new List<string>();
        var visited = new HashSet<string>();
        var inProgress = new HashSet<string>();

        foreach (var param in _allParameters)
        {
            if (!visited.Contains(param))
                TopologicalSortVisit(param, visited, inProgress, result);
        }

        return result;
    }

    private void TopologicalSortVisit(string node, HashSet<string> visited,
        HashSet<string> inProgress, List<string> result)
    {
        if (inProgress.Contains(node))
            throw new InvalidOperationException($"Circular dependency detected on '{node}' in {_componentType.Name}");

        if (visited.Contains(node)) return;

        inProgress.Add(node);

        if (_adjacencyList.TryGetValue(node, out var deps))
        {
            foreach (var dep in deps)
                TopologicalSortVisit(dep, visited, inProgress, result);
        }

        inProgress.Remove(node);
        visited.Add(node);
        result.Add(node);
    }

    private void BuildGraph()
    {
        var properties = _componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<ParameterAttribute>() != null)
            .ToList();

        foreach (var prop in properties)
        {
            _allParameters.Add(prop.Name);
            _adjacencyList[prop.Name] = new HashSet<string>();
        }

        // Analyze [Parameter] default value dependencies via DependsOnAttribute
        foreach (var prop in properties)
        {
            var dependsOnAttrs = prop.GetCustomAttributes<SgDependsOnAttribute>();
            foreach (var attr in dependsOnAttrs)
            {
                if (_allParameters.Contains(attr.ParameterName))
                {
                    AddDependency(attr.ParameterName, prop.Name);
                }
            }
        }
    }

    private void AddDependency(string source, string target)
    {
        if (!_adjacencyList.ContainsKey(source))
            _adjacencyList[source] = new HashSet<string>();

        if (!_reverseAdjacency.ContainsKey(target))
            _reverseAdjacency[target] = new HashSet<string>();

        _adjacencyList[source].Add(target);
        _reverseAdjacency[target].Add(source);
    }

    public string GetGraphvizDot()
    {
        var lines = new List<string> { "digraph ParameterDependencies {" };
        lines.Add("  rankdir=LR;");

        foreach (var (source, targets) in _adjacencyList)
        {
            foreach (var target in targets)
            {
                lines.Add($"  \"{source}\" -> \"{target}\";");
            }
        }

        lines.Add("}");
        return string.Join("\n", lines);
    }
}

/// <summary>
/// Marks a parameter as dependent on another parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class SgDependsOnAttribute : Attribute
{
    public string ParameterName { get; }

    public SgDependsOnAttribute(string parameterName)
    {
        ParameterName = parameterName;
    }
}

/// <summary>
/// Factory for creating and caching ParameterDependencyGraphs for component types.
/// Thread-safe singleton.
/// </summary>
public static class SgParameterDependencyGraphFactory
{
    private static readonly ConcurrentDictionary<Type, SgParameterDependencyGraph> _cache = new();

    public static SgParameterDependencyGraph GetOrCreate(Type componentType)
    {
        return _cache.GetOrAdd(componentType, t => new SgParameterDependencyGraph(t));
    }

    public static void Invalidate(Type componentType)
    {
        _cache.TryRemove(componentType, out _);
    }

    public static void InvalidateAll()
    {
        _cache.Clear();
    }
}

/// <summary>
/// Dirty parameter tracker for optimized rendering.
/// </summary>
public sealed class SgDirtyParameterTracker
{
    private readonly SgParameterDependencyGraph _graph;
    private readonly HashSet<string> _dirtyParams = new();
    private readonly object _lock = new();

    public SgDirtyParameterTracker(Type componentType)
    {
        _graph = SgParameterDependencyGraphFactory.GetOrCreate(componentType);
    }

    public void MarkDirty(string parameterName)
    {
        lock (_lock)
        {
            _dirtyParams.Add(parameterName);

            // Propagate to dependents
            foreach (var dep in _graph.GetDependents(parameterName))
            {
                _dirtyParams.Add(dep);
            }
        }
    }

    public bool IsDirty(string parameterName)
    {
        lock (_lock)
        {
            return _dirtyParams.Contains(parameterName);
        }
    }

    public IReadOnlySet<string> GetDirtyParameters()
    {
        lock (_lock)
        {
            return new HashSet<string>(_dirtyParams);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _dirtyParams.Clear();
        }
    }
}
