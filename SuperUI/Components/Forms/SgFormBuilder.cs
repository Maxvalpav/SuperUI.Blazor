// SuperUI/Components/Forms/SgFormBuilder.cs
using System.Linq.Expressions;

namespace SuperUI.Components;

/// <summary>
/// Fluent Form Builder — форма без единой строки Razor.
/// </summary>
public sealed class SgFormBuilder<TModel> where TModel : class, new()
{
    private readonly List<IFormField> _fields = [];

    public SgFormBuilder<TModel> Text<TProp>(
        Expression<Func<TModel, TProp>> prop,
        Action<TextFieldConfig>? configure = null)
    {
        var config = new TextFieldConfig();
        configure?.Invoke(config);
        _fields.Add(new TextField<TModel, TProp>(prop, config));
        return this;
    }

    public SgFormBuilder<TModel> Number<TProp>(
        Expression<Func<TModel, TProp>> prop,
        Action<NumberFieldConfig>? configure = null)
        where TProp : struct, IComparable<TProp>
    {
        var config = new NumberFieldConfig();
        configure?.Invoke(config);
        _fields.Add(new NumberField<TModel, TProp>(prop, config));
        return this;
    }

    public SgFormBuilder<TModel> Select<TProp>(
        Expression<Func<TModel, TProp>> prop,
        IEnumerable<SgSelectOption<TProp>> options,
        Action<SelectFieldConfig>? configure = null)
    {
        var config = new SelectFieldConfig();
        configure?.Invoke(config);
        _fields.Add(new SelectField<TModel, TProp>(prop, options, config));
        return this;
    }

    public SgFormBuilder<TModel> Switch<TProp>(
        Expression<Func<TModel, bool>> prop,
        Action<SwitchFieldConfig>? configure = null)
    {
        var config = new SwitchFieldConfig();
        configure?.Invoke(config);
        _fields.Add(new SwitchField<TModel>(prop, config));
        return this;
    }

    public SgFormBuilder<TModel> Row(Action<SgFormBuilder<TModel>> row)
    {
        var rowBuilder = new SgFormBuilder<TModel>();
        row(rowBuilder);
        _fields.Add(new RowGroup(rowBuilder._fields));
        return this;
    }

    public IReadOnlyList<IFormField> Build() => _fields;
}

// Конфиги полей
public sealed class TextFieldConfig
{
    public string? Label       { get; set; }
    public string? Placeholder { get; set; }
    public bool    Required    { get; set; }
    public bool    ShowClear   { get; set; }
    public int?    MaxLength   { get; set; }
    public SgInputType Type    { get; set; } = SgInputType.Text;
}

public sealed class NumberFieldConfig
{
    public string? Label       { get; set; }
    public string? Placeholder { get; set; }
    public bool    Required    { get; set; }
    public double? Min         { get; set; }
    public double? Max         { get; set; }
    public int?    Decimals    { get; set; }
}

public sealed class SelectFieldConfig
{
    public string? Label       { get; set; }
    public bool    Required    { get; set; }
    public bool    Searchable  { get; set; }
}

public sealed class SwitchFieldConfig
{
    public string? Label { get; set; }
}

// Интерфейсы полей
public interface IFormField { }

public sealed class TextField<TModel, TProp> : IFormField
{
    public Expression<Func<TModel, TProp>> Property { get; }
    public TextFieldConfig Config { get; }
    public TextField(Expression<Func<TModel, TProp>> prop, TextFieldConfig config)
    {
        Property = prop; Config = config;
    }
}

public sealed class NumberField<TModel, TProp> : IFormField
    where TProp : struct, IComparable<TProp>
{
    public Expression<Func<TModel, TProp>> Property { get; }
    public NumberFieldConfig Config { get; }
    public NumberField(Expression<Func<TModel, TProp>> prop, NumberFieldConfig config)
    {
        Property = prop; Config = config;
    }
}

public sealed class SelectField<TModel, TProp> : IFormField
{
    public Expression<Func<TModel, TProp>> Property { get; }
    public IEnumerable<SgSelectOption<TProp>> Options { get; }
    public SelectFieldConfig Config { get; }
    public SelectField(Expression<Func<TModel, TProp>> prop, IEnumerable<SgSelectOption<TProp>> options, SelectFieldConfig config)
    {
        Property = prop; Options = options; Config = config;
    }
}

public sealed class SwitchField<TModel> : IFormField
{
    public Expression<Func<TModel, bool>> Property { get; }
    public SwitchFieldConfig Config { get; }
    public SwitchField(Expression<Func<TModel, bool>> prop, SwitchFieldConfig config)
    {
        Property = prop; Config = config;
    }
}

public sealed class RowGroup : IFormField
{
    public IReadOnlyList<IFormField> Fields { get; }
    public RowGroup(IReadOnlyList<IFormField> fields) => Fields = fields;
}

// вспомогательные типы
public sealed class SgSelectOption<TValue>
{
    public TValue Value { get; }
    public string? Label { get; }
    public SgSelectOption(TValue value, string? label = null) => (Value, Label) = (value, label);
    public static implicit operator SgSelectOption<TValue>((TValue, string?) t) => new(t.Item1, t.Item2);
}

public enum SgInputType { Text, Email, Password, Number, Tel, Url, Date, Time, DateTime }
