using System.Xml.Serialization;

namespace SuperUI.Services.Data;

// ── Daily Rates ──────────────────────────────────────────────────────────────

[XmlRoot("ValCurs")]
public class SgCbrDailyRates
{
    [XmlAttribute("Date")]
    public string? Date { get; set; }

    [XmlAttribute("name")]
    public string? Name { get; set; }

    [XmlElement("Valute")]
    public List<SgCbrValute> Valutes { get; set; } = new();
}

public class SgCbrValute
{
    [XmlAttribute("ID")]
    public string? Id { get; set; }

    [XmlElement("NumCode")]
    public string? NumCode { get; set; }

    [XmlElement("CharCode")]
    public string? CharCode { get; set; }

    [XmlElement("Nominal")]
    public int Nominal { get; set; }

    [XmlElement("Name")]
    public string? Name { get; set; }

    [XmlElement("Value")]
    public string? ValueString { get; set; }

    [XmlElement("VunitRate")]
    public string? VunitRateString { get; set; }

    [XmlIgnore]
    public decimal Value => decimal.TryParse(ValueString?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;

    [XmlIgnore]
    public decimal VunitRate => decimal.TryParse(VunitRateString?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;
}

// ── Dynamic Rates ────────────────────────────────────────────────────────────

[XmlRoot("ValCurs")]
public class SgCbrDynamicRates
{
    [XmlAttribute("ID")]
    public string? Id { get; set; }

    [XmlAttribute("DateRange1")]
    public string? DateRange1 { get; set; }

    [XmlAttribute("DateRange2")]
    public string? DateRange2 { get; set; }

    [XmlAttribute("name")]
    public string? Name { get; set; }

    [XmlElement("Record")]
    public List<SgCbrRateRecord> Records { get; set; } = new();
}

public class SgCbrRateRecord
{
    [XmlAttribute("Date")]
    public string? DateString { get; set; }

    [XmlAttribute("Id")]
    public string? Id { get; set; }

    [XmlElement("Nominal")]
    public int Nominal { get; set; }

    [XmlElement("Value")]
    public string? ValueString { get; set; }

    [XmlElement("VunitRate")]
    public string? VunitRateString { get; set; }

    [XmlIgnore]
    public DateTime Date => DateTime.TryParse(DateString, out var d) ? d : DateTime.MinValue;

    [XmlIgnore]
    public decimal Value => decimal.TryParse(ValueString?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;
}

// ── Precious Metals ──────────────────────────────────────────────────────────

[XmlRoot("Metall")]
public class SgCbrMetals
{
    [XmlAttribute("FromDate")]
    public string? FromDate { get; set; }

    [XmlAttribute("ToDate")]
    public string? ToDate { get; set; }

    [XmlElement("Record")]
    public List<SgCbrMetalRecord> Records { get; set; } = new();
}

public class SgCbrMetalRecord
{
    [XmlAttribute("Date")]
    public string? DateString { get; set; }

    [XmlElement("Buy")]
    public string? BuyString { get; set; }

    [XmlElement("Sell")]
    public string? SellString { get; set; }

    [XmlAttribute("Code")]
    public int Code { get; set; }

    [XmlIgnore]
    public DateTime Date => DateTime.TryParse(DateString, out var d) ? d : DateTime.MinValue;

    [XmlIgnore]
    public decimal Buy => decimal.TryParse(BuyString?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;

    [XmlIgnore]
    public decimal Sell => decimal.TryParse(SellString?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;

    [XmlIgnore]
    public string MetalName => Code switch
    {
        1 => "Золото",
        2 => "Серебро",
        3 => "Платина",
        4 => "Палладий",
        _ => "Неизвестно"
    };
}
