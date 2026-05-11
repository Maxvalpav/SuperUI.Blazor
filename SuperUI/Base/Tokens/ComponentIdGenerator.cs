namespace SuperUI.Base.Tokens;

/// <summary>
/// Lock-free генератор уникальных ID компонентов.
/// Формат: "sg-{prefix}-{counter}"
/// </summary>
public static class ComponentIdGenerator
{
    private static int _counter;

    public static string Next(string prefix)
    {
        var id = Interlocked.Increment(ref _counter);
        return string.Create(
            4 + prefix.Length + GetDigitCount(id),
            (prefix, id),
            static (span, state) =>
            {
                var (p, n) = state;
                span[0] = 's';
                span[1] = 'g';
                span[2] = '-';
                p.AsSpan().CopyTo(span[3..]);
                span[3 + p.Length] = '-';
                n.TryFormat(span[(4 + p.Length)..], out _);
            });
    }

    private static int GetDigitCount(int n) =>
        n < 10 ? 1 : n < 100 ? 2 : n < 1000 ? 3 : n < 10000 ? 4 :
        n < 100000 ? 5 : n < 1000000 ? 6 : n < 10000000 ? 7 : 10;
}
