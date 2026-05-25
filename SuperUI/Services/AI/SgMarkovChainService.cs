using System;
using System.Collections.Generic;
using System.Linq;

namespace SuperUI.Services.AI;

/// <summary>
/// Легковесная реализация цепей Маркова для предсказания ввода на основе контекста.
/// </summary>
public class SgMarkovChainService
{
    // Хранилище: [Контекст] -> { [Следующее значение] -> Частота }
    private readonly Dictionary<string, Dictionary<string, int>> _chain = new();

    /// <summary>
    /// Обучает модель на паре значений (контекст -> текущий ввод).
    /// </summary>
    public void Learn(string context, string value)
    {
        if (string.IsNullOrWhiteSpace(context) || string.IsNullOrWhiteSpace(value)) return;

        if (!_chain.ContainsKey(context))
        {
            _chain[context] = new Dictionary<string, int>();
        }

        if (!_chain[context].ContainsKey(value))
        {
            _chain[context][value] = 0;
        }

        _chain[context][value]++;
    }

    /// <summary>
    /// Возвращает список предсказаний для заданного контекста, отсортированный по вероятности.
    /// </summary>
    public List<string> Predict(string context, int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(context) || !_chain.ContainsKey(context))
        {
            return new List<string>();
        }

        return _chain[context]
            .OrderByDescending(x => x.Value)
            .Take(maxResults)
            .Select(x => x.Key)
            .ToList();
    }

    /// <summary>
    /// Сброс модели обучения.
    /// </summary>
    public void Reset()
    {
        _chain.Clear();
    }
}
