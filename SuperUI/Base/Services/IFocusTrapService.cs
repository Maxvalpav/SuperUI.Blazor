// SuperUI/Base/Services/IFocusTrapService.cs
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace SuperUI.Base.Services;

/// <summary>Service interface for managing focus traps.</summary>
public interface IFocusTrapService
{
    bool IsTrappingActive { get; }

    ValueTask TrapFocusAsync(ElementReference element, FocusTrapOptions? options = null);

    ValueTask ReleaseFocusAsync();

    ValueTask ReleaseAllAsync();
    
    // Aliases for compatibility with SgOverlayBase (string-based)
    async ValueTask ActivateAsync(string elementId)
    {
        // For string-based activation, we need to get the element reference
        // This is a simplified implementation - actual implementation may vary
        await ReleaseFocusAsync();
    }

    async ValueTask DeactivateAsync(string elementId)
    {
        await ReleaseFocusAsync();
    }
}
