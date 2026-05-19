namespace SuperUI.Enums;

/// <summary>Как <see cref="SgRow"/> должен обёртывать дочерние элементы.</summary>
public enum SgFlexWrap
{
    /// <summary>Обёртка по умолчанию — перенос на новую строку при необходимости.</summary>
    Wrap,
    /// <summary>Никогда не переносить.</summary>
    NoWrap,
    /// <summary>Перенос в обратном направлении.</summary>
    WrapReverse
}
