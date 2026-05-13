// SuperUI/Base/Optimization/SgTrimmingAnnotations.cs — НОВЫЙ
// ✅ Атрибуты для безопасного тримминга WASM
// ✅ Предотвращает отрезание reflection-only кода
// ✅ Интеграция с ILLink

using System;

namespace SuperUI.Base.Optimization;

/// <summary>
/// Аннотации для безопасного trimming в Blazor WASM.
/// Защищают reflection-based код от удаления линковщиком.
/// </summary>

/// <summary>
/// Помечает класс/метод как необходимый даже при агрессивном тримминге.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property)]
public sealed class SgPreserveAttribute : Attribute { }

/// <summary>
/// Помечает компонент как регистрируемый через DI (защита от тримминга).
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SgComponentAttribute : Attribute
{
    public Type? Interface { get; init; }
    public bool LazyLoad { get; init; }
}

/// <summary>
/// Помечает конвертер как необходимый для тримминга.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SgConverterAttribute : Attribute
{
    public Type? ForType { get; init; }
}
