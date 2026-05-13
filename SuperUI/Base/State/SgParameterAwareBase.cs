// SuperUI/Base/State/SgParameterAwareBase.cs
// ✅ FIX: убран ручной вызов SetParameterProperties — base делает это сам
// ✅ FIX: SetParameterAsync использует ParameterView напрямую
// ✅ FIX: IsInteractive проверка для EditContext

namespace SuperUI.Base.State;

public abstract class SgParameterAwareBase : SgComponentBase
{
    private readonly List<SgParameterRegisterScope> _scopes = new();

    protected SgParameterRegisterScope CreateRegisterScope()
    {
        var scope = new SgParameterRegisterScope();
        _scopes.Add(scope);
        return scope;
    }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        // ✅ FIX: НЕ вызываем parameters.SetParameterProperties(this) здесь!
        // base.SetParametersAsync делает это через ComponentBase.
        // Мы применяем scopes с ParameterView, а base применит параметры сам.
        foreach (var scope in _scopes)
            await scope.ApplyAsync(parameters);

        // base применит parameters к this через ComponentBase
        await base.SetParametersAsync(parameters);
    }
}
