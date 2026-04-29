namespace SuperUI.Demo.Models
{
    public class ComponentParameter
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Default { get; set; } = "-";
        public string Description { get; set; } = "";
    }

    public class ComponentEvent
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SgPropertyAttribute : Attribute
    {
        public string? Category { get; set; }
        public string? Description { get; set; }
    }

    public class SgProperty
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string? Category { get; set; }
        public string? Description { get; set; }
    }

    public class SgQuery
    {
        public string? Operator { get; set; } = "AND";
        public List<SgQueryRule> Rules { get; set; } = new();
    }

    public class SgQueryRule
    {
        public string? Field { get; set; }
        public string? Operator { get; set; }
        public object? Value { get; set; }
    }

    public class SgQueryField
    {
        public string Name { get; set; } = "";
        public string Label { get; set; } = "";
        public string Type { get; set; } = "";
        public List<object>? Options { get; set; }

        public SgQueryField() { }
        public SgQueryField(string name, string label, string type)
        {
            Name = name;
            Label = label;
            Type = type;
        }
    }
}
