// SuperUI/Base/Interfaces/SgMarkerInterfaces.cs
// Маркерные интерфейсы-контракты для компонентов SuperUI.
// Blazor не наследует [Parameter] через интерфейсы — это намеренно:
// интерфейсы служат для тестов, сервисов и unit-of-work композиций.

using Microsoft.AspNetCore.Components;

namespace SuperUI.Base.Interfaces;

/// <summary>Компонент с поддержкой размеров (SgSize).</summary>
public interface ISgHasSize
{
    /// <summary>Размер компонента.</summary>
    Enums.SgSize Size { get; set; }
}

/// <summary>Компонент с поддержкой отключения (Disabled).</summary>
public interface ISgHasDisabled
{
    /// <summary>Если <c>true</c> — компонент недоступен для взаимодействия.</summary>
    bool Disabled { get; set; }
}

/// <summary>Компонент с поддержкой чтения-только (ReadOnly).</summary>
public interface ISgHasReadOnly
{
    /// <summary>Если <c>true</c> — компонент недоступен для редактирования, но фокусируется.</summary>
    bool ReadOnly { get; set; }
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

/// <summary>Компонент с density (плотность отображения).</summary>
public interface ISgHasDensity
{
    /// <summary>Плотность: компактная, дефолтная, комфортная.</summary>
    Enums.SgDensity Density { get; set; }
}

/// <summary>
/// Компонент, способный рапортовать о loading-состоянии.
/// </summary>
public interface ISgLoadingState
{
    /// <summary>True, если компонент сейчас в loading-состоянии.</summary>
    bool IsLoading { get; }
    /// <summary>Событие изменения loading-состояния.</summary>
    event Action<bool>? LoadingStateChanged;
}

/// <summary>
/// Компонент, который может показывать ошибку пользователю.
/// </summary>
public interface ISgErrorState
{
    /// <summary>Сообщение об ошибке (null = нет ошибки).</summary>
    string? Error { get; }
    /// <summary>True, если есть активная ошибка.</summary>
    bool HasError => Error is not null;
    /// <summary>Сбросить ошибку.</summary>
    void ClearError();
}

/// <summary>
/// Форма/компонент, поддерживающий валидацию и EditContext.
/// </summary>
public interface ISgFormComponent
{
    /// <summary>Идентификатор поля в форме.</summary>
    string? FieldId { get; set; }
    /// <summary>Disabled-флаг формы.</summary>
    bool Disabled { get; set; }
}

/// <summary>
/// Компонент, поддерживающий ARIA Listbox/Combobox-семантику.
/// Используется SgDropdown/SgSelect/SgListbox для a11y-сертификации.
/// </summary>
public interface ISgListboxLike
{
    /// <summary>Текущий выбранный индекс (или -1).</summary>
    int ActiveIndex { get; }
    /// <summary>Общее число опций.</summary>
    int ItemCount { get; }
    /// <summary>Ориентация для клавиатурной навигации.</summary>
    Enums.SgOrientation Orientation { get; }
}
