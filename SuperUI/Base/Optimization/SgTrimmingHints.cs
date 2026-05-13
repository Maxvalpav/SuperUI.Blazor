// ================================================================
// Файл: SuperUI/Base/Optimization/SgTrimmingHints.cs
// ИСПРАВЛЕНО: правильные имена классов
// ================================================================

using System.Diagnostics.CodeAnalysis;

namespace SuperUI.Base.Optimization;

public static class SgTrimmingHints
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SgComponentBase))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SgInteractiveBase))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SgJsComponentBase))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SgDataBase<object>))]
    public static void PreserveComponentTypes() { }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SgDataRequest))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SgDataResult<object>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SgSortDescriptor))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SgFilterDescriptor))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SgFilterGroup))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SgGroupDescriptor))]
    public static void PreserveDataTypes() { }

    // ИСПРАВЛЕНО: правильные имена классов
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Configuration.SgComponentBuilder))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Services.IComponentRegistry))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Services.SgComponentRegistry))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Services.IComponentFactory))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Services.SgComponentFactory))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IFormNameGenerator))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DefaultFormNameGenerator))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Services.ICryptoOptimizer))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Services.WasmCryptoOptimizer))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Services.AdaptiveRenderBudgetService))]
    public static void PreserveNewServices() { }
}
