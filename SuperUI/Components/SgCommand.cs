// SuperUI/Components/SgCommand.cs
// DTO для команд Command Palette — вынесен из .razor файла
// Причина: типы в .razor файлах недоступны из других компонентов
namespace SuperUI.Components;

/// <summary>
/// Команда для SgCommandPalette.
/// </summary>
public sealed class SgCommand
{
    public string Label { get; init; } = "";
    public string? Icon { get; init; }
    public string? Shortcut { get; init; }
    public string? Group { get; init; }
    public string[]? Keywords { get; init; }
    public Func<Task> ExecuteAsync { get; init; } = () => Task.CompletedTask;
}