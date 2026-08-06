using System.Text;

namespace SuperUI.Demo.Components;

/// <summary>
/// Single-pass Razor markup highlighter. Scans the raw source once and emits
/// escaped spans directly — the emitted markup is never re-scanned, which is
/// what a chain of Replace-based regexes would do (and would corrupt).
/// </summary>
public static class DataGridCodeHighlighter
{
    public static string AutoHighlight(string plain)
    {
        if (string.IsNullOrEmpty(plain)) return string.Empty;

        var sb = new StringBuilder(plain.Length * 3);
        var i = 0;

        while (i < plain.Length)
        {
            if (plain[i] == '<' && StartsWith(plain, i, "<!--"))
            {
                var end = plain.IndexOf("-->", i + 4, StringComparison.Ordinal);
                end = end < 0 ? plain.Length : end + 3;
                Emit(sb, "comment", plain[i..end]);
                i = end;
            }
            else if (plain[i] == '<' && i + 1 < plain.Length &&
                     (char.IsLetter(plain[i + 1]) || plain[i + 1] == '/'))
            {
                i = ReadTag(plain, i, sb);
            }
            else
            {
                var next = plain.IndexOf('<', i + 1);
                if (next < 0) next = plain.Length;
                ReadText(plain[i..next], sb);
                i = next;
            }
        }

        return sb.ToString();
    }

    /// <summary>Reads a full element tag starting at '&lt;'. Returns the index just past it.</summary>
    private static int ReadTag(string src, int i, StringBuilder sb)
    {
        var open = src[i + 1] == '/' ? "</" : "<";
        Emit(sb, "operator", open);
        i += open.Length;

        var nameEnd = i;
        while (nameEnd < src.Length && (char.IsLetterOrDigit(src[nameEnd]) ||
               src[nameEnd] is '.' or '_' or '-' or ':')) nameEnd++;
        if (nameEnd > i)
        {
            Emit(sb, "type", src[i..nameEnd]);
            i = nameEnd;
        }

        while (i < src.Length)
        {
            if (char.IsWhiteSpace(src[i]))
            {
                var wsEnd = i;
                while (wsEnd < src.Length && char.IsWhiteSpace(src[wsEnd])) wsEnd++;
                sb.Append(Escape(src[i..wsEnd]));
                i = wsEnd;
                continue;
            }

            if (StartsWith(src, i, "/>")) { Emit(sb, "operator", "/>"); return i + 2; }
            if (src[i] == '>') { Emit(sb, "operator", ">"); return i + 1; }

            // Attribute name (may be a directive: @bind-Value, @onclick).
            var attrEnd = i;
            if (attrEnd < src.Length && src[attrEnd] == '@') attrEnd++;
            while (attrEnd < src.Length && (char.IsLetterOrDigit(src[attrEnd]) ||
                   src[attrEnd] is '.' or '_' or '-' or ':')) attrEnd++;

            if (attrEnd == i) { sb.Append(Escape(src[i].ToString())); i++; continue; }

            Emit(sb, src[i] == '@' ? "keyword" : "attr", src[i..attrEnd]);
            i = attrEnd;

            if (i >= src.Length || src[i] != '=') continue;

            Emit(sb, "operator", "=");
            i++;
            i = ReadAttributeValue(src, i, sb);
        }

        return i;
    }

    /// <summary>Reads an attribute value (quoted or bare) and classifies it.</summary>
    private static int ReadAttributeValue(string src, int i, StringBuilder sb)
    {
        if (i >= src.Length) return i;

        if (src[i] is '"' or '\'')
        {
            var quote = src[i];
            var end = FindValueEnd(src, i + 1, quote);

            Emit(sb, "string", quote.ToString());
            EmitValue(sb, src[(i + 1)..end]);
            Emit(sb, "string", quote.ToString());
            return end + 1;
        }

        var bareEnd = i;
        while (bareEnd < src.Length && !char.IsWhiteSpace(src[bareEnd]) &&
               src[bareEnd] != '>' && !StartsWith(src, bareEnd, "/>")) bareEnd++;
        EmitValue(sb, src[i..bareEnd]);
        return bareEnd;
    }

    /// <summary>
    /// Finds the closing quote of an attribute value. A Razor expression such as
    /// <c>@(e =&gt; string.Join(", ", e.Skills))</c> contains quotes of its own, so
    /// inside a parenthesised expression the closing quote is only accepted once
    /// the parens balance out.
    /// </summary>
    private static int FindValueEnd(string src, int start, char quote)
    {
        var depth = 0;
        for (var i = start; i < src.Length; i++)
        {
            var c = src[i];
            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == quote && depth <= 0) return i;
        }
        return src.Length - 1;
    }

    /// <summary>Colours the inside of an attribute value by its shape.</summary>
    private static void EmitValue(StringBuilder sb, string value)
    {
        if (value.Length == 0) return;

        if (value[0] == '@') { EmitExpression(sb, value); return; }

        if (value is "true" or "false" or "True" or "False" or "null")
        {
            Emit(sb, "keyword", value);
            return;
        }

        if (IsNumeric(value)) { Emit(sb, "number", value); return; }

        Emit(sb, "string", value);
    }

    /// <summary>Renders a Razor expression: '@' as a keyword, the body as code.</summary>
    private static void EmitExpression(StringBuilder sb, string expr)
    {
        Emit(sb, "keyword", "@");
        var body = expr[1..];
        if (body.Length == 0) return;

        var i = 0;
        while (i < body.Length)
        {
            var c = body[i];

            if (c is '(' or ')' or '[' or ']' or ',' or '.')
            {
                Emit(sb, "operator", c.ToString());
                i++;
            }
            else if (c == '=' && i + 1 < body.Length && body[i + 1] == '>')
            {
                Emit(sb, "keyword", "=>");
                i += 2;
            }
            else if (c == '"')
            {
                var end = body.IndexOf('"', i + 1);
                if (end < 0) end = body.Length - 1;
                Emit(sb, "string", body[i..(end + 1)]);
                i = end + 1;
            }
            else if (char.IsLetterOrDigit(c) || c == '_')
            {
                var end = i;
                while (end < body.Length && (char.IsLetterOrDigit(body[end]) || body[end] == '_')) end++;
                var word = body[i..end];
                var cls = word is "true" or "false" or "null" or "new" or "string" or "var"
                    ? "keyword"
                    : IsNumeric(word) ? "number" : "expr";
                Emit(sb, cls, word);
                i = end;
            }
            else
            {
                sb.Append(Escape(c.ToString()));
                i++;
            }
        }
    }

    /// <summary>Text between tags — only Razor expressions need colouring here.</summary>
    private static void ReadText(string text, StringBuilder sb)
    {
        var i = 0;
        while (i < text.Length)
        {
            var at = text.IndexOf('@', i);
            if (at < 0) { sb.Append(Escape(text[i..])); return; }

            sb.Append(Escape(text[i..at]));

            var end = at + 1;
            var depth = 0;
            while (end < text.Length)
            {
                var c = text[end];
                if (c == '(') depth++;
                else if (c == ')') { depth--; end++; if (depth <= 0) break; continue; }
                else if (depth == 0 && !char.IsLetterOrDigit(c) && c is not ('.' or '_')) break;
                end++;
            }

            EmitExpression(sb, text[at..end]);
            i = end;
        }
    }

    private static bool IsNumeric(string s)
    {
        if (s.Length == 0) return false;
        var dot = false;
        foreach (var c in s)
        {
            if (c == '.' && !dot) { dot = true; continue; }
            if (!char.IsDigit(c)) return false;
        }
        return true;
    }

    private static bool StartsWith(string src, int i, string token) =>
        src.AsSpan(i).StartsWith(token, StringComparison.Ordinal);

    private static void Emit(StringBuilder sb, string kind, string text) =>
        sb.Append("<span class=\"sgc-code-").Append(kind).Append("\">")
          .Append(Escape(text)).Append("</span>");

    private static string Escape(string s) => s
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;");
}
