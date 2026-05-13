// SuperUI/Base/Optimization/SgTrimmingHints.cs

using System.Diagnostics.CodeAnalysis;

namespace SuperUI.Base.Optimization;

/// <summary>
/// Атрибуты и аннотации для защиты от агрессивного trimming в WASM.
/// Использует [DynamicDependency] для сохранения типов, используемых через reflection.
/// </summary>
public static class SgTrimmingHints
{
    /// <summary>
    /// Все типы компонентов, которые должны быть сохранены при trimming.
    /// Генерируется автоматически через Source Generator.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SgComponentBase))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SgInteractiveBase))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SgJsComponentBase))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SgDataBase<>))]
    public static void PreserveComponentTypes() { }

    /// <summary>
    /// Регистрация типов для Reflection-free сериализации в WASM.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SgDataRequest))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SgDataResult<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SgSortDescriptor))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SgFilterDescriptor))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SgFilterGroup))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SgGroupDescriptor))]
    public static void PreserveDataTypes() { }

    /// <summary>
    /// Сохранение новых сервисов, добавленных в ServiceCollectionExtensions.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Configuration.SgComponentBuilder<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Services.IComponentRegistry))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Services.ComponentRegistry))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Services.IComponentFactory))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Services.ComponentFactory))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SuperUI.Base.IFormNameGenerator))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SuperUI.Base.DefaultFormNameGenerator))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Services.ICryptoOptimizer))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Services.WasmCryptoOptimizer))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Services.AdaptiveRenderBudgetService))]
    public static void PreserveNewServices() { }
}