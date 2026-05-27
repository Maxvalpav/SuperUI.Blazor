namespace SuperUI.Components;

/// <summary>
/// Converts a 5-field cron expression into a human-readable Russian description.
/// Used by <see cref="SgCron"/> and <see cref="SgCronPicker"/>.
/// </summary>
public static class SgCronHumanizer
{
    private static readonly string[] MonthFullNames =
    {
        "январе", "феврале", "марте", "апреле", "мае", "июне",
        "июле", "августе", "сентябре", "октябре", "ноябре", "декабре"
    };

    private static readonly string[] WeekDayFullNames =
    {
        "воскресенье", "понедельник", "вторник", "среду",
        "четверг", "пятницу", "субботу"
    };

    private static readonly string[] MonthFullNamesEn =
    {
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    };

    private static readonly string[] WeekDayFullNamesEn =
    {
        "Sunday", "Monday", "Tuesday", "Wednesday",
        "Thursday", "Friday", "Saturday"
    };

    /// <summary>
    /// Returns a human-readable description of the given cron expression,
    /// or the raw expression if it cannot be parsed.
    /// </summary>
    public static string Describe(string? expr, bool preferEnglish = false)
    {
        if (string.IsNullOrWhiteSpace(expr)) return string.Empty;

        var parts = expr.Trim().Split(' ');
        if (parts.Length != 5) return expr;

        var sb = new System.Text.StringBuilder();

        if (preferEnglish)
        {
            DescribeEnglish(parts, sb);
        }
        else
        {
            DescribeRussian(parts, sb);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns a human-readable English description of the given cron expression.
    /// </summary>
    public static string DescribeEn(string? expr) => Describe(expr, true);

    private static void DescribeRussian(string[] parts, System.Text.StringBuilder sb)
    {
        // ── time ──────────────────────────────────────────────────────────────
        if (parts[0] == "*" && parts[1] == "*")
            sb.Append("Каждую минуту");
        else if (parts[0].StartsWith("*/") && int.TryParse(parts[0][2..], out var mEvery))
            sb.Append($"Каждые {Plural(mEvery, "минуту", "минуты", "минут")}");
        else if (parts[1].StartsWith("*/") && int.TryParse(parts[1][2..], out var hEvery))
        {
            sb.Append($"Каждые {Plural(hEvery, "час", "часа", "часов")}");
            if (parts[0] != "*" && !parts[0].Contains(',') && !parts[0].Contains('-'))
                sb.Append($" в :{parts[0].PadLeft(2, '0')}");
        }
        else if (!parts[1].Contains('*') && !parts[0].Contains('*'))
        {
            if (parts[1].Contains(',') || parts[0].Contains(','))
                sb.Append($"В {parts[1]}:{parts[0]}");
            else
                sb.Append($"В {parts[1].PadLeft(2, '0')}:{parts[0].PadLeft(2, '0')}");
        }
        else if (parts[0] != "*" && parts[1] == "*")
            sb.Append($"В :{parts[0].PadLeft(2, '0')} каждого часа");
        else
            sb.Append("По расписанию");

        // ── day-of-month ───────────────────────────────────────────────────────
        if (parts[2] == "L")
            sb.Append(", в последний день месяца");
        else if (parts[2] != "*" && parts[2] != "?")
        {
            if (parts[2].StartsWith("*/") && int.TryParse(parts[2][2..], out var dEvery))
                sb.Append($", каждые {Plural(dEvery, "день", "дня", "дней")}");
            else
                sb.Append($", в дни месяца {parts[2]}");
        }

        // ── month ──────────────────────────────────────────────────────────────
        if (parts[3] != "*")
        {
            if (parts[3].StartsWith("*/") && int.TryParse(parts[3][2..], out var moEvery))
                sb.Append($", каждые {Plural(moEvery, "месяц", "месяца", "месяцев")}");
            else
            {
                var months = ParseList(parts[3], 1, 12);
                if (months.Count > 0)
                    sb.Append(", в " + string.Join(", ", months.Select(m => MonthFullNames[m - 1])));
            }
        }

        // ── weekday ────────────────────────────────────────────────────────────
        if (parts[4] != "*" && parts[4] != "?")
        {
            var days = ParseList(parts[4], 0, 6);
            if (days.Count > 0)
                sb.Append(", по " + string.Join(", ", days.Select(d => WeekDayFullNames[d % 7])));
        }
    }

    private static void DescribeEnglish(string[] parts, System.Text.StringBuilder sb)
    {
        // ── time ──────────────────────────────────────────────────────────────
        if (parts[0] == "*" && parts[1] == "*")
            sb.Append("Every minute");
        else if (parts[0].StartsWith("*/") && int.TryParse(parts[0][2..], out var mEvery))
            sb.Append($"Every {mEvery} minute{(mEvery > 1 ? "s" : "")}");
        else if (parts[1].StartsWith("*/") && int.TryParse(parts[1][2..], out var hEvery))
        {
            sb.Append($"Every {hEvery} hour{(hEvery > 1 ? "s" : "")}");
            if (parts[0] != "*" && !parts[0].Contains(',') && !parts[0].Contains('-'))
                sb.Append($" at :{parts[0].PadLeft(2, '0')}");
        }
        else if (!parts[1].Contains('*') && !parts[0].Contains('*'))
        {
            if (parts[1].Contains(',') || parts[0].Contains(','))
                sb.Append($"At {parts[1]}:{parts[0]}");
            else
                sb.Append($"At {parts[1].PadLeft(2, '0')}:{parts[0].PadLeft(2, '0')}");
        }
        else if (parts[0] != "*" && parts[1] == "*")
            sb.Append($"At :{parts[0].PadLeft(2, '0')} past every hour");
        else
            sb.Append("Scheduled");

        // ── day-of-month ───────────────────────────────────────────────────────
        if (parts[2] == "L")
            sb.Append(", on the last day of the month");
        else if (parts[2] != "*" && parts[2] != "?")
        {
            if (parts[2].StartsWith("*/") && int.TryParse(parts[2][2..], out var dEvery))
                sb.Append($", every {dEvery} day{(dEvery > 1 ? "s" : "")}");
            else
                sb.Append($", on day(s) {parts[2]}");
        }

        // ── month ──────────────────────────────────────────────────────────────
        if (parts[3] != "*")
        {
            if (parts[3].StartsWith("*/") && int.TryParse(parts[3][2..], out var moEvery))
                sb.Append($", every {moEvery} month{(moEvery > 1 ? "s" : "")}");
            else
            {
                var months = ParseList(parts[3], 1, 12);
                if (months.Count > 0)
                    sb.Append(", in " + string.Join(", ", months.Select(m => MonthFullNamesEn[m - 1])));
            }
        }

        // ── weekday ────────────────────────────────────────────────────────────
        if (parts[4] != "*" && parts[4] != "?")
        {
            var days = ParseList(parts[4], 0, 6);
            if (days.Count > 0)
                sb.Append(", on " + string.Join(", ", days.Select(d => WeekDayFullNamesEn[d % 7])));
        }
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static string Plural(int n, string one, string few, string many)
    {
        var abs = Math.Abs(n) % 100;
        var n1 = abs % 10;
        if (abs > 10 && abs < 20) return $"{n} {many}";
        if (n1 == 1) return $"{n} {one}";
        if (n1 >= 2 && n1 <= 4) return $"{n} {few}";
        return $"{n} {many}";
    }

    private static List<int> ParseList(string token, int min, int max)
    {
        var result = new List<int>();
        foreach (var p in token.Split(','))
        {
            if (p.Contains('-'))
            {
                var rr = p.Split('-');
                if (int.TryParse(rr[0], out var a) && int.TryParse(rr[1], out var b))
                    for (int i = a; i <= b; i++) result.Add(i);
            }
            else if (int.TryParse(p, out var v))
                result.Add(v);
        }
        return result.Distinct().OrderBy(x => x).ToList();
    }
}
