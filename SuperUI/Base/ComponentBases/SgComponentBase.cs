// SuperUI/Base/ComponentBases/SgComponentBase.cs
// Корневой базовый класс для НЕ-input компонентов SuperUI.
// Убирает дублирование CssClass/Style/AdditionalAttributes из ~140 компонентов.

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SuperUI.Base.Builders;
using SuperUI.Base.Utilities;
using SuperUI.Services;
using SuperUI.Themes;

namespace SuperUI.Base.ComponentBases;

/// <summary>
/// Корневой базовый класс для компонентов SuperUI (не input-компоненты).
/// </summary>
/// <remarks>
/// <para>Предоставляет унифицированные параметры <see cref="CssClass"/>,
/// <see cref="Style"/>, <see cref="Id"/>, <see cref="AdditionalAttributes"/>
/// и фабричные методы <see cref="Css"/>, <see cref="Styles"/>.</para>
/// <para>Все существующие компоненты продолжают работать без изменений.
/// Миграция — только аддитивная: удалить дублированные объявления параметров
/// и перейти на <c>Css()</c>/<c>Styles()</c>.</para>
/// </remarks>
public abstract class SgComponentBase : ComponentBase, IDisposable
{
    private string? _autoId;
    private ILogger? _logger;

    // ── Параметры ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Дополнительные CSS-классы, добавляемые к корневому элементу.
    /// </summary>
    [Parameter]
    public string? CssClass { get; set; }

    /// <summary>
    /// Дополнительные inline-стили, добавляемые к корневому элементу.
    /// </summary>
    [Parameter]
    public string? Style { get; set; }

    /// <summary>
    /// HTML-атрибут <c>id</c> корневого элемента.
    /// Если не задан — используется <see cref="ResolvedId"/> (автогенерация).
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>
    /// Захватывает все неизвестные HTML-атрибуты (<c>aria-*</c>, <c>data-*</c>, <c>role</c>, и т.д.)
    /// и передаёт их корневому элементу.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    // ── Каскадируемые параметры ───────────────────────────────────────────────

    /// <summary>
    /// Контекст хост-окружения (тема, prefers-reduced-motion).
    /// Устанавливается через <c>CascadingValue</c> в App.razor.
    /// </summary>
    [CascadingParameter]
    protected HostEnvironmentContext? Host { get; set; }

    // ── Инжекция ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Фабрика логгеров. Используйте через свойство <see cref="Logger"/>.
    /// Помечена <c>[Inject(...)]</c>, но <see cref="Logger"/> устойчиво обрабатывает
    /// случай отсутствия DI (например, в юнит-тестах) — возвращает <see cref="NullLogger.Instance"/>.
    /// </summary>
    [Inject]
    protected ILoggerFactory? LoggerFactory { get; set; }

    /// <summary>
    /// Сервис управления темами.
    /// </summary>
    [Inject]
    protected SgThemeService ThemeService { get; set; } = default!;

    /// <summary>
    /// Возвращает текущий режим темы ("light", "dark", "auto").
    /// </summary>
    protected string CurrentMode => ThemeService.CurrentMode;

    /// <summary>
    /// true, если сейчас активен тёмный режим (с учётом системных настроек для "auto").
    /// </summary>
    protected bool IsDark => ThemeService.IsDark;

    /// <summary>
    /// Текущая активная тема.
    /// </summary>
    protected IThemeDefinition CurrentTheme => ThemeService.CurrentTheme;

    // ── Жизненный цикл ────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ThemeService.ThemeChanged += HandleThemeChanged;
    }

    private void HandleThemeChanged(IThemeDefinition theme, string mode)
    {
        InvokeAsync(StateHasChanged);
    }

    public virtual void Dispose()
    {
        ThemeService.ThemeChanged -= HandleThemeChanged;
    }

    // ── Защищённые члены ──────────────────────────────────────────────────────

    /// <summary>
    /// Ссылка на корневой HTML-элемент компонента.
    /// </summary>
    protected ElementReference RootRef;

    /// <summary>
    /// Разрешённый HTML-идентификатор: <see cref="Id"/> если задан,
    /// иначе автоматически сгенерированный стабильный ID.
    /// </summary>
    /// <remarks>
    /// Использует <see cref="SgIdGenerator.StableIdFor"/> — возвращает одинаковый
    /// ID при последующих рендерах одного экземпляра компонента.
    /// </remarks>
    protected string ResolvedId
        => Id ?? (_autoId ??= SgIdGenerator.StableIdFor(this, IdPrefix));

    /// <summary>
    /// Префикс для автогенерации ID. По умолчанию <c>"sg"</c>.
    /// Переопределите в подклассе: <c>protected override string IdPrefix => "sg-modal";</c>
    /// </summary>
    protected virtual string IdPrefix => "sg";

    /// <summary>
    /// Типизированный логгер для текущего компонента.
    /// Создаётся лениво при первом обращении. В тестовом контексте без DI
    /// возвращает <see cref="NullLogger.Instance"/>.
    /// </summary>
    protected ILogger Logger =>
        _logger ??= LoggerFactory?.CreateLogger(GetType()) ?? NullLogger.Instance;

    // ── CSS / Style factory methods ───────────────────────────────────────────

    /// <summary>
    /// Создаёт <see cref="CssBuilder"/> с корневым классом и автоматически
    /// добавляет <see cref="CssClass"/> из параметра и <c>class</c> из
    /// <see cref="AdditionalAttributes"/>.
    /// </summary>
    /// <param name="rootClass">Базовый CSS-класс компонента.</param>
    /// <returns>Настроенный <see cref="CssBuilder"/>.</returns>
    /// <remarks>
    /// <para>Чтобы избежать дублирования при <c>@attributes="AdditionalAttributes"</c>
    /// в шаблоне, корневой элемент должен либо не использовать splatting <c>class</c>,
    /// либо передавать в splatting отфильтрованный словарь. См. <see cref="AttributesWithoutClassAndStyle"/>.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // В razor-файле:
    /// class="@Css("sg-button").AddClass(VariantClass).AddClass("sg-block", Block).Build()"
    /// </code>
    /// </example>
    protected CssBuilder Css(string? rootClass = null)
        => CssBuilder.Default(rootClass)
                     .AddClass(CssClass)
                     .AddClassFromAttributes(AdditionalAttributes);

    /// <summary>
    /// Создаёт <see cref="StyleBuilder"/> с параметром <see cref="Style"/>
    /// и <c>style</c> из <see cref="AdditionalAttributes"/>.
    /// </summary>
    /// <returns>Настроенный <see cref="StyleBuilder"/>.</returns>
    protected StyleBuilder Styles()
        => StyleBuilder.Default(Style)
                       .AddStyleFromAttributes(AdditionalAttributes);

    /// <summary>
    /// Возвращает <see cref="AdditionalAttributes"/> без ключей <c>class</c> и <c>style</c>.
    /// Используйте в <c>@attributes</c>, чтобы не дублировать значения,
    /// уже учтённые в <see cref="Css"/>/<see cref="Styles"/>.
    /// </summary>
    /// <remarks>
    /// <code>
    /// &lt;div class="@Css("sg-foo").Build()"
    ///      style="@Styles().Build()"
    ///      @attributes="AttributesWithoutClassAndStyle"&gt;
    /// </code>
    /// </remarks>
    protected IReadOnlyDictionary<string, object>? AttributesWithoutClassAndStyle
    {
        get
        {
            if (AdditionalAttributes is null) return null;
            var hasClass = AdditionalAttributes.ContainsKey("class");
            var hasStyle = AdditionalAttributes.ContainsKey("style");
            if (!hasClass && !hasStyle) return AdditionalAttributes;

            var dict = new Dictionary<string, object>(AdditionalAttributes.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var kv in AdditionalAttributes)
            {
                if (kv.Key.Equals("class", StringComparison.OrdinalIgnoreCase)) continue;
                if (kv.Key.Equals("style", StringComparison.OrdinalIgnoreCase)) continue;
                dict[kv.Key] = kv.Value;
            }
            return dict;
        }
    }

    /// <summary>
    /// Объединяет несколько CSS-классов через пробел, игнорируя пустые.
    /// </summary>
    protected static string CombineCss(params string?[] tokens)
        => string.Join(" ", tokens.Where(t => !string.IsNullOrWhiteSpace(t))!);
}

/// <summary>
/// Каскадируемый контекст хост-окружения.
/// Устанавливается один раз в App.razor через <c>CascadingValue</c>.
/// </summary>
public sealed class HostEnvironmentContext
{
    /// <summary>
    /// Текущая тема (<c>"light"</c>, <c>"dark"</c>, <c>"auto"</c>).
    /// </summary>
    public string? ThemeTag { get; init; }

    /// <summary>
    /// <c>true</c>, если пользователь включил «Уменьшить движение» в ОС.
    /// Используйте для отключения анимаций.
    /// </summary>
    public bool PrefersReducedMotion { get; init; }
}