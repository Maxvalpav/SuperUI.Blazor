// ─────────────────────────────────────────────────────────────────
// FILE: Services/ComponentBoundary.cs
// ИННОВАЦИЯ: Error Boundary на уровне компонента.
// Перехватывает исключения дочерних компонентов и показывает fallback.
// ─────────────────────────────────────────────────────────────────
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace SuperUI.Services;

/// <summary>
/// Error Boundary для отдельного компонента SuperUI.
/// Показывает ErrorContent при исключении, изолируя ошибку.
/// </summary>
public sealed class SgErrorBoundary : ErrorBoundaryBase
{
    [Parameter] public RenderFragment? ErrorContent   { get; set; }
    [Parameter] public RenderFragment? ChildContent   { get; set; }
    [Parameter] public bool            AutoRecover    { get; set; }
    [Parameter] public int             AutoRecoverMs  { get; set; } = 3000;

    [Parameter] public EventCallback<Exception> OnError { get; set; }

    protected override async Task OnErrorAsync(Exception exception)
    {
        if (OnError.HasDelegate)
            await OnError.InvokeAsync(exception);

        if (AutoRecover)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(AutoRecoverMs);
                Recover();
            });
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (CurrentException is null)
        {
            builder.AddContent(0, ChildContent);
        }
        else if (ErrorContent is not null)
        {
            builder.AddContent(1, ErrorContent);
        }
        else
        {
            builder.OpenElement(2, "div");
            builder.AddAttribute(3, "class", "sg-error-boundary");
            builder.AddAttribute(4, "role", "alert");
            builder.AddContent(5, $"Error: {CurrentException.Message}");
            builder.CloseElement();
        }
    }
}
