using System.Text.RegularExpressions;

namespace SuperUI.Demo.Components;

public static class DataGridCodeHighlighter
{
    private static readonly Regex _stringPattern = new("""(&quot;(?:[^&quot;]*)&quot;)""");
    private static readonly Regex _commentPattern = new("""(&lt;!--.*?--&gt;)""");
    private static readonly Regex _tagOpenPattern = new("""(&lt;)(/?)""");
    private static readonly Regex _selfClosePattern = new("""(/)(&gt;)""");
    private static readonly Regex _tagClosePattern = new("""(&gt;)""");
    private static readonly Regex _tagNamePattern = new("""(?<=<span class="sgc-code-operator">&lt;</span>)(\w+(?:\.\w+)?)""");
    private static readonly Regex _directivePattern = new("""(@(?:bind-)?\w+)""");
    private static readonly Regex _attrPattern = new("""(\b[\w-]+)(=)""");
    private static readonly Regex _boolPattern = new("""\b(true|false|True|False|null)\b""");
    private static readonly Regex _numberPattern = new("""\b(\d+(?:\.\d+)?)\b""");

    public static string AutoHighlight(string plain)
    {
        var code = plain
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");

        code = _stringPattern.Replace(code, "<span class=\"sgc-code-string\">$1</span>");
        code = _commentPattern.Replace(code, "<span class=\"sgc-code-comment\">$1</span>");
        code = _tagOpenPattern.Replace(code, "<span class=\"sgc-code-operator\">$1</span><span class=\"sgc-code-operator\">$2</span>");
        code = _selfClosePattern.Replace(code, "<span class=\"sgc-code-operator\">$1</span><span class=\"sgc-code-operator\">$2</span>");
        code = _tagClosePattern.Replace(code, "<span class=\"sgc-code-operator\">$1</span>");
        code = _tagNamePattern.Replace(code, "<span class=\"sgc-code-keyword\">$1</span>");
        code = _directivePattern.Replace(code, "<span class=\"sgc-code-keyword\">$1</span>");
        code = _attrPattern.Replace(code, "<span class=\"sgc-code-attr\">$1</span><span class=\"sgc-code-operator\">$2</span>");
        code = _boolPattern.Replace(code, "<span class=\"sgc-code-keyword\">$1</span>");
        code = _numberPattern.Replace(code, "<span class=\"sgc-code-number\">$1</span>");

        return code;
    }
}
