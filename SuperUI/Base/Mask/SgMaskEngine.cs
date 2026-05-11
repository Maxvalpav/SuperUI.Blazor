// SuperUI/Base/Mask/SgMaskEngine.cs
using System.Text;

namespace SuperUI.Base.Mask;

/// <summary>
/// ИННОВАЦИЯ: Stateful input mask на чистом C#.
/// Форматы: "####-##-##", "+7 (###) ###-##-##", "##:##", "IP: ###.###.###.###"
///
/// Аналоги: InputMask.js (JS), jQuery Mask Plugin (JS).
/// В Blazor на C#: НЕТ.
/// </summary>
public sealed class SgMaskEngine
{
    private readonly string _mask;
    private readonly char   _placeholder;
    private readonly HashSet<char> _literals;

    private static readonly HashSet<char> MaskChars = ['#', 'A', 'a', '*', '9'];

    public SgMaskEngine(string mask, char placeholder = '_')
    {
        _mask        = mask;
        _placeholder = placeholder;
        _literals    = new HashSet<char>(
            mask.Where(c => !MaskChars.Contains(c)));
    }

    /// <summary>Применить маску к вводимому тексту.</summary>
    public MaskResult Apply(string? rawInput, int cursorPosition)
    {
        if (string.IsNullOrEmpty(rawInput))
            return new MaskResult(GetEmptyMask(), 0, false);

        var result  = new char[_mask.Length];
        var rawIdx  = 0;
        var maskIdx = 0;
        var newCursor = 0;

        while (maskIdx < _mask.Length)
        {
            var maskChar = _mask[maskIdx];

            if (_literals.Contains(maskChar))
            {
                result[maskIdx] = maskChar;
                if (rawIdx < rawInput.Length && rawInput[rawIdx] == maskChar) rawIdx++;
                if (rawIdx > 0 && maskIdx < cursorPosition) newCursor++;
            }
            else if (rawIdx < rawInput.Length)
            {
                var inputChar = rawInput[rawIdx];
                if (IsValidForMask(maskChar, inputChar))
                {
                    result[maskIdx] = inputChar;
                    rawIdx++;
                    if (maskIdx < cursorPosition) newCursor = maskIdx + 1;
                }
                else
                {
                    result[maskIdx] = _placeholder;
                    rawIdx++; // пропустить невалидный символ
                }
            }
            else
            {
                result[maskIdx] = _placeholder;
            }
            maskIdx++;
        }

        var masked   = new string(result);
        var complete = result.All(c => c != _placeholder);
        return new MaskResult(masked, newCursor, complete);
    }

    public string GetEmptyMask()
    {
        var result = new char[_mask.Length];
        for (var i = 0; i < _mask.Length; i++)
            result[i] = _literals.Contains(_mask[i]) ? _mask[i] : _placeholder;
        return new string(result);
    }

    public string StripMask(string? masked)
    {
        if (string.IsNullOrEmpty(masked)) return "";
        var sb = new StringBuilder();
        for (var i = 0; i < Math.Min(masked.Length, _mask.Length); i++)
        {
            if (!_literals.Contains(_mask[i]) && masked[i] != _placeholder)
                sb.Append(masked[i]);
        }
        return sb.ToString();
    }

    private static bool IsValidForMask(char maskChar, char input) => maskChar switch
    {
        '#' => char.IsDigit(input),
        'A' => char.IsLetter(input),
        'a' => char.IsLetter(input),
        '*' => true,
        '9' => char.IsDigit(input),
        _   => false
    };
}

public readonly record struct MaskResult(string Masked, int CursorPosition, bool IsComplete);
