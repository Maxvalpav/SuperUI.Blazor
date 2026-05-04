namespace SuperUI.Components;

/// <summary>
/// Built-in mask presets for <see cref="SgMaskedInput"/>.
/// </summary>
public enum SgMaskType
{
    /// <summary>User-defined mask via <see cref="SgMaskedInput.Mask"/>.</summary>
    Custom,

    /// <summary>Phone number: +7 (999) 999-99-99.</summary>
    Phone,

    /// <summary>Russian INN: 10 or 12 digits (auto-detected by length).</summary>
    Inn,

    /// <summary>Russian KPP: 9 chars (digits + letters in positions 5–6).</summary>
    Kpp,

    /// <summary>Bank account: 20 digits grouped by 4.</summary>
    Account,

    /// <summary>Russian BIC: 9 digits.</summary>
    Bic,

    /// <summary>Russian SNILS: 999-999-999 99.</summary>
    Snils,

    /// <summary>Russian OGRN: 13 digits (or OGRNIP 15 digits).</summary>
    Ogrn,

    /// <summary>IBAN: up to 34 alphanumeric chars, grouped by 4.</summary>
    Iban,

    /// <summary>Credit card: 9999 9999 9999 9999.</summary>
    CreditCard,

    /// <summary>Card expiry: MM/YY.</summary>
    CardExpiry,

    /// <summary>Card CVV/CVC: 3–4 digits.</summary>
    Cvv,

    /// <summary>Postal code: 6 digits (RU).</summary>
    Postal,

    /// <summary>Date in current culture short format (e.g. dd.MM.yyyy).</summary>
    Date,

    /// <summary>Time HH:mm.</summary>
    Time,

    /// <summary>Currency with symbol and thousands separator.</summary>
    Currency,

    /// <summary>Percentage with % suffix.</summary>
    Percent
}
