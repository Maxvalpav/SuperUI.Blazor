// SuperUI/Base/State/SgParameterAwareBase.cs
// ИСПРАВЛЕНО:
// ✅ CS0101: единственное объявление класса
// ✅ ЛОГИКА: base.SetParametersAsync(parameters) — передаём оригинальный ParameterView
// ✅ ЛОГИКА: НЕ вызываем parameters.SetParameterProperties(this) — base делает это сам

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace SuperUI.Base.State;

/// <summary>
/// Базовый класс с поддержкой SgParameterState&lt;T&gt;.
/// Позволяет типобезопасно управлять параметрами с change handlers.
/// </summary>
/// <example>
/// <code>
/// public class MyComponent : SgParameterAwareBase
/// {
///     [Parameter] public bool Expanded { get; set; }
///     [Parameter] public EventCallback&lt;bool&gt; ExpandedChanged { get; set; }
///     
///     private readonly SgParameterState&lt;bool&gt; _expandedState;
///     
///     public MyComponent()
///     {
///         using var scope = CreateRegisterScope();
///         _expandedState = scope.RegisterParameter&lt;bool&gt;(nameof(Expanded))
///             .WithParameter(() => Expanded)
///             .WithEventCallback(() => ExpandedChanged)
///             .WithChangeHandler(OnExpandedChangedAsync)
///             .Build();
///     }
///     
///     private async Task OnExpandedChangedAsync(bool value) { ... }
/// }
/// </code>
/// </example>
public abstract class SgParameterAwareBase : SgComponentBase
{
    private readonly List<SgParameterRegisterScope> _scopes = new();

    /// <summary>
    /// Создаёт scope для регистрации параметров.
    /// Вызывать в конструкторе компонента.
    /// </summary>
    protected SgParameterRegisterScope CreateRegisterScope()
    {
        var scope = new SgParameterRegisterScope();
        _scopes.Add(scope);
        return scope;
    }

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        // ✅ FIX: Применяем scopes с ParameterView ДО base.SetParametersAsync
        // base.SetParametersAsync сам вызовет SetParameterProperties(this)
        // и затем OnParametersSet / OnParametersSetAsync
        foreach (var scope in _scopes)
            await scope.ApplyAsync(parameters);

        // Передаём оригинальный ParameterView, чтобы base присвоил все параметры
        await base.SetParametersAsync(parameters);
    }
}
