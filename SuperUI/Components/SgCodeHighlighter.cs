using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace SuperUI.Components;

/// <summary>
/// Lightweight, dependency-free syntax highlighter used by <see cref="SgCode"/>.
/// Produces HTML-encoded output with token spans (sg-tk-*).
/// </summary>
internal static class Highlighter
{
    public static string Highlight(string code, SgCodeLanguage language)
    {
        if (string.IsNullOrEmpty(code)) return string.Empty;

        return language switch
        {
            SgCodeLanguage.CSharp     => HighlightCSharp(code),
            SgCodeLanguage.Razor      => HighlightRazor(code),
            SgCodeLanguage.Html       => HighlightMarkup(code),
            SgCodeLanguage.Xml        => HighlightMarkup(code),
            SgCodeLanguage.Css        => HighlightCss(code),
            SgCodeLanguage.Json       => HighlightJson(code),
            SgCodeLanguage.JavaScript => HighlightJs(code, ts: false),
            SgCodeLanguage.TypeScript => HighlightJs(code, ts: true),
            SgCodeLanguage.Bash       => HighlightBash(code),
            SgCodeLanguage.Sql        => HighlightSql(code),
            _                         => WebUtility.HtmlEncode(code)
        };
    }

    // ── Token-based highlighter ─────────────────────────────────────────────
    // Produces a single concatenated HTML string. Operates on raw text and
    // calls HtmlEncode on every captured token before wrapping it in a span.

    private sealed record Rule(string Class, Regex Pattern);

    private static string Apply(string code, IReadOnlyList<Rule> rules)
    {
        // Walk the string and at each position, try every rule. Whichever
        // matches at the current index wins (longest-match preferred).
        var sb = new StringBuilder(code.Length + 64);
        int i = 0;
        while (i < code.Length)
        {
            Match? best = null;
            string? bestClass = null;

            foreach (var rule in rules)
            {
                var m = rule.Pattern.Match(code, i);
                if (m.Success && m.Index == i)
                {
                    if (best is null || m.Length > best.Length)
                    {
                        best = m;
                        bestClass = rule.Class;
                    }
                }
            }

            if (best is not null && bestClass is not null && best.Length > 0)
            {
                sb.Append("<span class=\"sg-tk-").Append(bestClass).Append("\">");
                sb.Append(WebUtility.HtmlEncode(best.Value));
                sb.Append("</span>");
                i += best.Length;
            }
            else
            {
                // No rule matched: emit the next character escaped.
                sb.Append(WebUtility.HtmlEncode(code[i].ToString()));
                i++;
            }
        }
        return sb.ToString();
    }

    private const RegexOptions Opts = RegexOptions.Compiled | RegexOptions.CultureInvariant;

    // ── C# ──────────────────────────────────────────────────────────────────
    private static readonly Rule[] CSharpRules =
    {
        new("comment",  new Regex(@"//[^\n]*|/\*[\s\S]*?\*/", Opts)),
        new("string",   new Regex(@"@?\$?""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])*'", Opts)),
        new("keyword",  new Regex(@"\b(abstract|as|async|await|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|do|double|else|enum|event|explicit|extern|false|finally|fixed|float|for|foreach|goto|if|implicit|in|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|private|protected|public|readonly|record|ref|return|sbyte|sealed|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|var|virtual|void|volatile|where|while|yield|init|nameof|with|file|required|scoped)\b", Opts)),
        new("number",   new Regex(@"\b(?:0[xX][0-9a-fA-F_]+|0[bB][01_]+|\d[\d_]*(?:\.\d+)?(?:[eE][+-]?\d+)?)[fFdDmMlLuU]?\b", Opts)),
        new("function", new Regex(@"\b([A-Za-z_]\w*)(?=\s*\()", Opts)),
        new("class",    new Regex(@"\b[A-Z][A-Za-z0-9_]*\b", Opts)),
        new("operator", new Regex(@"[+\-*/%=<>!&|\^~?:]+", Opts)),
        new("punct",    new Regex(@"[(){}\[\];,.]", Opts)),
    };

    private static string HighlightCSharp(string code) => Apply(code, CSharpRules);

    // ── Razor ───────────────────────────────────────────────────────────────
    // Razor is a mix of HTML + C#. We do a simple split: code blocks @{...},
    // implicit @expressions, and the rest as markup.
    private static string HighlightRazor(string code)
    {
        // Match @code/@functions/@{} blocks first, then @ expressions, then
        // whatever is left is markup.
        var sb = new StringBuilder(code.Length + 64);
        int i = 0;
        while (i < code.Length)
        {
            // @code { ... } / @functions { ... } / @{ ... }
            if (code[i] == '@' && i + 1 < code.Length)
            {
                if (TryReadBalancedBlock(code, i, out var blockEnd, out var atKind))
                {
                    var block = code[i..blockEnd];
                    sb.Append("<span class=\"sg-tk-directive\">").Append(WebUtility.HtmlEncode(atKind)).Append("</span>");
                    var inner = block[atKind.Length..];
                    sb.Append(HighlightCSharp(inner));
                    i = blockEnd;
                    continue;
                }

                // @expression up to whitespace / non-identifier
                int j = i + 1;
                while (j < code.Length && (char.IsLetterOrDigit(code[j]) || code[j] == '_' || code[j] == '.'))
                    j++;
                if (j > i + 1)
                {
                    sb.Append("<span class=\"sg-tk-directive\">@</span>");
                    sb.Append("<span class=\"sg-tk-keyword\">").Append(WebUtility.HtmlEncode(code[(i + 1)..j])).Append("</span>");
                    i = j;
                    continue;
                }
            }

            // Markup tag
            if (code[i] == '<')
            {
                int end = code.IndexOf('>', i);
                if (end > i)
                {
                    var tag = code[i..(end + 1)];
                    sb.Append(HighlightTag(tag));
                    i = end + 1;
                    continue;
                }
            }

            // Plain text until next special char
            int next = code.IndexOfAny(new[] { '<', '@' }, i);
            if (next < 0) next = code.Length;
            sb.Append(WebUtility.HtmlEncode(code[i..next]));
            i = next;
        }
        return sb.ToString();
    }

    private static bool TryReadBalancedBlock(string code, int at, out int endIndex, out string atKind)
    {
        endIndex = at;
        atKind = "@";

        // Identify @code / @functions / @{
        int p = at + 1;
        if (p < code.Length && code[p] == '{')
        {
            atKind = "@";
            int braceStart = p;
            return ReadToMatchingBrace(code, braceStart, out endIndex, ref atKind);
        }

        // word
        int wEnd = p;
        while (wEnd < code.Length && (char.IsLetter(code[wEnd]) || code[wEnd] == '_')) wEnd++;
        if (wEnd > p)
        {
            var word = code[p..wEnd];
            if (word is "code" or "functions" or "if" or "for" or "foreach" or "while" or "switch" or "using")
            {
                int q = wEnd;
                while (q < code.Length && code[q] is ' ' or '\t') q++;
                if (q < code.Length && code[q] == '{')
                {
                    atKind = "@" + word;
                    return ReadToMatchingBrace(code, q, out endIndex, ref atKind);
                }
            }
        }
        return false;
    }

    private static bool ReadToMatchingBrace(string code, int braceStart, out int endIndex, ref string atKind)
    {
        int depth = 0;
        int i = braceStart;
        while (i < code.Length)
        {
            if (code[i] == '{') depth++;
            else if (code[i] == '}')
            {
                depth--;
                if (depth == 0) { endIndex = i + 1; return true; }
            }
            i++;
        }
        endIndex = code.Length;
        return true;
    }

    // ── HTML / XML ──────────────────────────────────────────────────────────
    private static string HighlightMarkup(string code)
    {
        var sb = new StringBuilder(code.Length + 64);
        int i = 0;
        while (i < code.Length)
        {
            if (code[i] == '<')
            {
                // Comment
                if (code.AsSpan(i).StartsWith("<!--".AsSpan()))
                {
                    int end = code.IndexOf("-->", i + 4, StringComparison.Ordinal);
                    if (end < 0) end = code.Length; else end += 3;
                    sb.Append("<span class=\"sg-tk-comment\">")
                      .Append(WebUtility.HtmlEncode(code[i..end]))
                      .Append("</span>");
                    i = end;
                    continue;
                }
                int gt = code.IndexOf('>', i);
                if (gt > i)
                {
                    sb.Append(HighlightTag(code[i..(gt + 1)]));
                    i = gt + 1;
                    continue;
                }
            }
            int next = code.IndexOf('<', i);
            if (next < 0) next = code.Length;
            sb.Append(WebUtility.HtmlEncode(code[i..next]));
            i = next;
        }
        return sb.ToString();
    }

    private static readonly Regex TagAttr =
        new("(\\b[\\w:.-]+)(\\s*=\\s*)(\"(?:\\\\.|[^\"\\\\])*\"|'(?:\\\\.|[^'\\\\])*')", Opts);

    private static string HighlightTag(string tag)
    {
        // tag == "<...>"
        if (tag.Length < 2 || tag[0] != '<') return WebUtility.HtmlEncode(tag);

        var sb = new StringBuilder(tag.Length + 32);
        sb.Append("<span class=\"sg-tk-punct\">&lt;</span>");
        var inner = tag[1..^1]; // strip < >
        bool closing = inner.StartsWith("/");
        if (closing) { sb.Append("<span class=\"sg-tk-punct\">/</span>"); inner = inner[1..]; }

        // tag name
        int n = 0;
        while (n < inner.Length && (char.IsLetterOrDigit(inner[n]) || inner[n] is '-' or '_' or ':' or '.'))
            n++;
        if (n > 0)
        {
            sb.Append("<span class=\"sg-tk-tag\">").Append(WebUtility.HtmlEncode(inner[..n])).Append("</span>");
        }
        var rest = inner[n..];

        // self-closing slash at end
        bool selfClose = rest.EndsWith("/");
        if (selfClose) rest = rest[..^1];

        // attributes
        int last = 0;
        foreach (Match m in TagAttr.Matches(rest))
        {
            sb.Append(WebUtility.HtmlEncode(rest[last..m.Index]));
            sb.Append("<span class=\"sg-tk-attr\">").Append(WebUtility.HtmlEncode(m.Groups[1].Value)).Append("</span>");
            sb.Append(WebUtility.HtmlEncode(m.Groups[2].Value));
            sb.Append("<span class=\"sg-tk-string\">").Append(WebUtility.HtmlEncode(m.Groups[3].Value)).Append("</span>");
            last = m.Index + m.Length;
        }
        sb.Append(WebUtility.HtmlEncode(rest[last..]));

        if (selfClose) sb.Append("<span class=\"sg-tk-punct\">/</span>");
        sb.Append("<span class=\"sg-tk-punct\">&gt;</span>");
        return sb.ToString();
    }

    // ── CSS ─────────────────────────────────────────────────────────────────
    private static readonly Rule[] CssRules =
    {
        new("comment",  new Regex(@"/\*[\s\S]*?\*/", Opts)),
        new("string",   new Regex(@"""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])*'", Opts)),
        new("number",   new Regex(@"-?\b\d+(?:\.\d+)?(?:px|em|rem|%|vh|vw|s|ms|deg)?\b", Opts)),
        new("attr",     new Regex(@"--[a-zA-Z0-9_-]+|[a-zA-Z-]+(?=\s*:)", Opts)),
        new("class",    new Regex(@"[.#:][a-zA-Z_][\w-]*", Opts)),
        new("punct",    new Regex(@"[{};,()]", Opts)),
        new("operator", new Regex(@"[:>+~*]", Opts)),
    };
    private static string HighlightCss(string code) => Apply(code, CssRules);

    // ── JSON ────────────────────────────────────────────────────────────────
    private static readonly Rule[] JsonRules =
    {
        new("attr",     new Regex(@"""(?:\\.|[^""\\])*""(?=\s*:)", Opts)),
        new("string",   new Regex(@"""(?:\\.|[^""\\])*""", Opts)),
        new("number",   new Regex(@"-?\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b", Opts)),
        new("keyword",  new Regex(@"\b(true|false|null)\b", Opts)),
        new("punct",    new Regex(@"[{}\[\],:]", Opts)),
    };
    private static string HighlightJson(string code) => Apply(code, JsonRules);

    // ── JS / TS ─────────────────────────────────────────────────────────────
    private static Rule[] JsRules(bool ts) => new[]
    {
        new Rule("comment",  new Regex(@"//[^\n]*|/\*[\s\S]*?\*/", Opts)),
        new Rule("string",   new Regex(@"`(?:\\.|[^`\\])*`|""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])*'", Opts)),
        new Rule("keyword",  new Regex(ts
            ? @"\b(abstract|any|as|async|await|boolean|break|case|catch|class|const|constructor|continue|debugger|declare|default|delete|do|else|enum|export|extends|false|finally|for|from|function|get|if|implements|import|in|instanceof|interface|is|keyof|let|namespace|never|new|null|number|of|package|private|protected|public|readonly|return|set|static|string|super|switch|this|throw|true|try|type|typeof|undefined|union|unknown|var|void|while|with|yield)\b"
            : @"\b(async|await|break|case|catch|class|const|continue|debugger|default|delete|do|else|export|extends|false|finally|for|from|function|get|if|import|in|instanceof|let|new|null|of|return|set|static|super|switch|this|throw|true|try|typeof|undefined|var|void|while|with|yield)\b", Opts)),
        new Rule("number",   new Regex(@"\b(?:0[xX][0-9a-fA-F_]+|\d[\d_]*(?:\.\d+)?(?:[eE][+-]?\d+)?)\b", Opts)),
        new Rule("function", new Regex(@"\b([A-Za-z_$][\w$]*)(?=\s*\()", Opts)),
        new Rule("operator", new Regex(@"=>|[+\-*/%=<>!&|\^~?:]+", Opts)),
        new Rule("punct",    new Regex(@"[(){}\[\];,.]", Opts)),
    };
    private static string HighlightJs(string code, bool ts) => Apply(code, JsRules(ts));

    // ── Bash ────────────────────────────────────────────────────────────────
    private static readonly Rule[] BashRules =
    {
        new("comment",  new Regex(@"#[^\n]*", Opts)),
        new("string",   new Regex(@"""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])*'", Opts)),
        new("keyword",  new Regex(@"\b(if|then|else|elif|fi|for|in|do|done|while|until|case|esac|function|return|export|local|readonly|set|unset)\b", Opts)),
        new("function", new Regex(@"^\s*([a-zA-Z_][\w-]*)(?=\s*\()", RegexOptions.Multiline | Opts)),
        new("number",   new Regex(@"\b\d+\b", Opts)),
        new("operator", new Regex(@"[|&;<>$]+", Opts)),
    };
    private static string HighlightBash(string code) => Apply(code, BashRules);

    // ── SQL ─────────────────────────────────────────────────────────────────
    private static readonly Rule[] SqlRules =
    {
        new("comment",  new Regex(@"--[^\n]*|/\*[\s\S]*?\*/", Opts)),
        new("string",   new Regex(@"'(?:''|[^'])*'", Opts)),
        new("keyword",  new Regex(@"\b(SELECT|FROM|WHERE|AND|OR|NOT|NULL|IS|IN|LIKE|BETWEEN|JOIN|LEFT|RIGHT|INNER|OUTER|FULL|ON|GROUP|BY|ORDER|HAVING|LIMIT|OFFSET|INSERT|INTO|VALUES|UPDATE|SET|DELETE|CREATE|TABLE|VIEW|INDEX|DROP|ALTER|ADD|COLUMN|PRIMARY|KEY|FOREIGN|REFERENCES|CONSTRAINT|UNIQUE|DEFAULT|CASE|WHEN|THEN|ELSE|END|AS|DISTINCT|UNION|ALL|EXISTS|WITH|TRUE|FALSE)\b", RegexOptions.IgnoreCase | Opts)),
        new("number",   new Regex(@"\b\d+(?:\.\d+)?\b", Opts)),
        new("operator", new Regex(@"[=<>!+\-*/]+", Opts)),
        new("punct",    new Regex(@"[(),;]", Opts)),
    };
    private static string HighlightSql(string code) => Apply(code, SqlRules);
}
