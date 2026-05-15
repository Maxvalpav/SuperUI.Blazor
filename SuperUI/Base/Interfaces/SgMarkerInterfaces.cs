// SuperUI/Base/Interfaces/SgMarkerInterfaces.cs
// Маркерные интерфейсы-контракты для компонентов SuperUI.
// Blazor не наследует [Parameter] через интерфейсы — это намеренно:
// интерфейсы служат для тестов и сервисов (например, FocusFirstAsync).

using Microsoft.AspNetCore.Components;

namespace SuperUI.Base.Interfaces;

/// <summary>Компонент с поддержкой размеров.</summary>
public interface ISgHasSize
{
    /// <summary>Размер компонента.</summary>
    Enums.SgSize Size { get; set; }
}

/// <summary>Компонент с поддержкой отключения.</summary>
public interface ISgHasDisabled
{
    /// <summary>Если <c>true</c> — компонент недоступен для взаимодействия.</summary>
    bool Disabled { get; set; }
}

/// <summary>Компонент с управляемой видимостью (two-way binding).</summary>
public interface ISgHasVisible
{
    /// <summary>Видимость компонента.</summary>
    bool Visible { get; set; }

    /// <summary>Callback при изменении <see cref="Visible"/>.</summary>
    EventCallback<bool> VisibleChanged { get; set; }
}

/// <summary>Компонент, поддерживающий программный фокус.</summary>
public interface ISgFocusable
{
    /// <summary>Устанавливает фокус на компонент.</summary>
    ValueTask FocusAsync();
}

/// <summary>Компонент с вариантами отображения.</summary>
/// <typeparam name="TVariant">Enum вариантов.</typeparam>
public interface ISgHasVariant<TVariant>
    where TVariant : struct, Enum
{
    /// <summary>Текущий вариант отображения.</summary>
    TVariant Variant { get; set; }
}