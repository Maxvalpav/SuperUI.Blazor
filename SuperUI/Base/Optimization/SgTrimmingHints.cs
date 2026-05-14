// ================================================================
// Файл: SuperUI/Base/Optimization/SgTrimmingHints.cs
// ИСПРАВЛЕНО:
// ✅ CS0234: Services.IComponentRegistry → Services.ISgComponentTypeRegistry
// ✅ Services.ICryptoOptimizer — удалён (тип не существует)
// ✅ Добавлены корректные ссылки на существующие типы
// ✅ .NET 8/9/10: [DynamicDependency] совместим со всеми версиями
// ================================================================

using System.Diagnostics.CodeAnalysis;
using SuperUI.Base;
using SuperUI.Base.Configuration;
using SuperUI.Base.Services;

namespace SuperUI.Base.Optimization;

/// <summary>
/// Подсказки для триммера .NET (PublishTrimmed=true) и NativeAOT.
/// Гарантирует, что критические типы не будут удалены линкером.
/// Применимо для WASM (trimming включён по умолчанию).
/// </summary>
public static class SgTrimmingHints
{
    /// <summary>Сохранить базовые типы компонентов SuperUI.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SgComponentBase))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SgInteractiveBase))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SgJsComponentBase))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SgDataBase<>))]
    public static void PreserveComponentTypes() { }

    /// <summary>Сохранить типы данных (используются при JSON serialization в WASM).</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SgDataRequest))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SgDataResult<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SgSortDescriptor))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SgFilterDescriptor))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SgFilterGroup))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SgGroupDescriptor))]
    public static void PreserveDataTypes() { }

    /// <summary>
    /// Сохранить новые сервисы и их интерфейсы.
    /// ✅ FIX CS0234: Services.ISgComponentTypeRegistry (не IComponentRegistry)
    /// ✅ ICryptoOptimizer удалён (не существует)
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SgComponentBuilder))]
    // ✅ FIX: ISgComponentTypeRegistry (не IComponentRegistry)
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ISgComponentTypeRegistry))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SgComponentRegistry))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IComponentFactory))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SgComponentFactory))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IFormNameGenerator))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DefaultFormNameGenerator))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AdaptiveRenderBudgetService))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SgWasmCryptoOptimizer))]
    public static void PreserveNewServices() { }

    /// <summary>Сохранить reactive-систему (сигналы, эффекты, computed).</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SgSignal<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SgComputed<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SgEffect))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SgSignalPersistence))]
    public static void PreserveReactiveSystem() { }
}
