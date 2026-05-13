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
}
