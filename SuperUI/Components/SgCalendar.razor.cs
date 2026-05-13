using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Globalization;
using System.Text;

namespace SuperUI.Components
{
    public partial class SgCalendar : ComponentBase
    {
        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Currently focused / selected date.</summary>
        [Parameter] public DateTime Value { get; set; } = DateTime.Today;

        /// <summary>Raised when <see cref="Value"/> changes (selection or navigation).</summary>
        [Parameter] public EventCallback<DateTime> ValueChanged { get; set; }

        /// <summary>Active display mode.</summary>
        [Parameter] public SgCalendarView View { get; set; } = SgCalendarView.Month;

        /// <summary>Raised when the user switches the view.</summary>
        [Parameter] public EventCallback<SgCalendarView> ViewChanged { get; set; }

        /// <summary>Calendar events. Bind with <c>@bind-Events</c> to get internal create/edit/delete updates back.</summary>
        [Parameter] public IEnumerable<SgCalendarEvent>? Events { get; set; }

        /// <summary>Raised whenever the events collection is mutated by the built-in editor.</summary>
        [Parameter] public EventCallback<IEnumerable<SgCalendarEvent>> EventsChanged { get; set; }

        /// <summary>Custom day cell renderer (replaces the default chip stack).</summary>
        [Parameter] public RenderFragment<DateTime>? DateCellContent { get; set; }

        /// <summary>Custom event chip renderer for month view.</summary>
        [Parameter] public RenderFragment<SgCalendarEvent>? EventTemplate { get; set; }

        /// <summary>Optional toolbar slot rendered to the right of the search box.</summary>
        [Parameter] public RenderFragment? ToolbarContent { get; set; }

        /// <summary>Replacement empty-state fragment.</summary>
        [Parameter] public RenderFragment? EmptyContent { get; set; }

        /// <summary>Pixel/CSS height of the component (e.g. <c>"640px"</c>, <c>"100%"</c>).</summary>
        [Parameter] public string Height { get; set; } = "auto";

        /// <summary>Optional title shown in the polished header.</summary>
        [Parameter] public string? Title { get; set; }

        /// <summary>Description rendered under the title.</summary>
        [Parameter] public string? Description { get; set; }

        /// <summary>Compact density (smaller paddings, fonts, cells).</summary>
        [Parameter] public bool Compact { get; set; }

        /// <summary>Render a skeleton instead of content while data is being fetched.</summary>
        [Parameter] public bool Loading { get; set; }

        /// <summary>Show a search box that filters events by title/description/location.</summary>
        [Parameter] public bool ShowSearch { get; set; }

        /// <summary>Search placeholder text. Defaults to a localized string.</summary>
        [Parameter] public string? SearchPlaceholder { get; set; }

        /// <summary>Show ISO week numbers in the leftmost column of month view.</summary>
        [Parameter] public bool ShowWeekNumbers { get; set; }

        /// <summary>Maximum events shown per day cell in month view (extras collapse into a "+N more" chip).</summary>
        [Parameter] public int MaxEventsPerDay { get; set; } = 3;

        /// <summary>Show the agenda view tab in the view switcher.</summary>
        [Parameter] public bool ShowAgendaView { get; set; } = true;

        /// <summary>Show the year view tab in the view switcher.</summary>
        [Parameter] public bool ShowYearView { get; set; } = true;

        /// <summary>Number of days the agenda view spans, starting from <see cref="Value"/>.</summary>
        [Parameter] public int AgendaRange { get; set; } = 30;

        /// <summary>First hour displayed in the day/week timeline.</summary>
        [Parameter] public int DayStartHour { get; set; } = 0;

        /// <summary>Hour the day/week timeline ends (exclusive).</summary>
        [Parameter] public int DayEndHour { get; set; } = 24;

        /// <summary>Pixels per hour in the day/week timeline.</summary>
        [Parameter] public int HourHeight { get; set; } = 48;

        /// <summary>Highlights working hours in the day/week timeline.</summary>
        [Parameter] public bool ShowWorkHours { get; set; } = true;

        /// <summary>Inclusive first hour of the highlighted working block.</summary>
        [Parameter] public int WorkStartHour { get; set; } = 9;

        /// <summary>Exclusive last hour of the highlighted working block.</summary>
        [Parameter] public int WorkEndHour { get; set; } = 18;

        /// <summary>Allow editing existing events through the built-in modal/UI.</summary>
        [Parameter] public bool EnableEditing { get; set; }

        /// <summary>Allow creating new events through the built-in modal/UI.</summary>
        [Parameter] public bool EnableCreation { get; set; }

        /// <summary>Allow drag-and-drop of events between dates (requires <see cref="EnableEditing"/>).</summary>
        [Parameter] public bool EnableDragAndDrop { get; set; } = true;

        /// <summary>Optional time zone reserved for future absolute-time conversion (currently passed through).</summary>
        [Parameter] public TimeZoneInfo? TimeZone { get; set; }

        /// <summary>Additional CSS class names.</summary>
        [Parameter] public string? CssClass { get; set; }

        // Callbacks
        [Parameter] public EventCallback<DateTime> OnAddEvent { get; set; }
        [Parameter] public EventCallback<SgCalendarEvent> OnEventClick { get; set; }
        [Parameter] public EventCallback<SgCalendarEvent> OnDeleteEvent { get; set; }
        [Parameter] public EventCallback<SgCalendarEvent> OnEventSaved { get; set; }

        [Inject] private ISuperUILocalizer Localizer { get; set; } = default!;

        // ── Internal state ─────────────────────────────────────────────────────

        private DateTime _currentMonth = DateTime.Today;
        private bool _isModalVisible;
        private bool _isNewEvent;
        private SgCalendarEvent _editingEvent = new();
        private DateTime? _editingEventDate;
        private SgCalendarEvent? _draggedEvent;
        private DateTime? _expandedDate;
        private string? _search;

        private static readonly string[] _recurrenceOptions = { "", "FREQ=DAILY", "FREQ=WEEKLY", "FREQ=MONTHLY", "FREQ=YEARLY" };

        private static readonly Dictionary<string, string> _recurrenceLabels = new()
        {
            [""] = "Never",
            ["FREQ=DAILY"] = "Daily",
            ["FREQ=WEEKLY"] = "Weekly",
            ["FREQ=MONTHLY"] = "Monthly",
            ["FREQ=YEARLY"] = "Yearly"
        };

        private static readonly SgCalendarEventStatus[] _statusOptions =
            (SgCalendarEventStatus[])Enum.GetValues(typeof(SgCalendarEventStatus));

        private static readonly SgCalendarEventPriority[] _priorityOptions =
            (SgCalendarEventPriority[])Enum.GetValues(typeof(SgCalendarEventPriority));

        protected override void OnInitialized()
        {
            _currentMonth = new DateTime(Value.Year, Value.Month, 1);
        }

        protected override void OnParametersSet()
        {
            if (_currentMonth == default)
                _currentMonth = new DateTime(Value.Year, Value.Month, 1);
            if (DayStartHour < 0) DayStartHour = 0;
            if (DayEndHour > 24) DayEndHour = 24;
            if (DayEndHour <= DayStartHour) DayEndHour = Math.Min(24, DayStartHour + 1);
            if (HourHeight < 24) HourHeight = 24;
        }

        // ── Navigation ─────────────────────────────────────────────────────────

        private async Task SetViewAsync(SgCalendarView view)
        {
            if (View == view) return;
            View = view;
            if (ViewChanged.HasDelegate)
                await ViewChanged.InvokeAsync(view);
        }

        private async Task PreviousRange()
        {
            switch (View)
            {
                case SgCalendarView.Month:
                    _currentMonth = _currentMonth.AddMonths(-1);
                    Value = _currentMonth;
                    break;
                case SgCalendarView.Week:
                    Value = Value.AddDays(-7);
                    break;
                case SgCalendarView.Day:
                    Value = Value.AddDays(-1);
                    break;
                case SgCalendarView.Agenda:
                    Value = Value.AddDays(-Math.Max(1, AgendaRange));
                    break;
                case SgCalendarView.Year:
                    Value = Value.AddYears(-1);
                    _currentMonth = new DateTime(Value.Year, _currentMonth.Month, 1);
                    break;
            }
            await ValueChanged.InvokeAsync(Value);
        }

        private async Task NextRange()
        {
            switch (View)
            {
                case SgCalendarView.Month:
                    _currentMonth = _currentMonth.AddMonths(1);
                    Value = _currentMonth;
                    break;
                case SgCalendarView.Week:
                    Value = Value.AddDays(7);
                    break;
                case SgCalendarView.Day:
                    Value = Value.AddDays(1);
                    break;
                case SgCalendarView.Agenda:
                    Value = Value.AddDays(Math.Max(1, AgendaRange));
                    break;
                case SgCalendarView.Year:
                    Value = Value.AddYears(1);
                    _currentMonth = new DateTime(Value.Year, _currentMonth.Month, 1);
                    break;
            }
            await ValueChanged.InvokeAsync(Value);
        }

        private async Task PreviousYear()
        {
            _currentMonth = _currentMonth.AddYears(-1);
            Value = ClampToMonth(_currentMonth, Value.Day);
            await ValueChanged.InvokeAsync(Value);
        }

        private async Task NextYear()
        {
            _currentMonth = _currentMonth.AddYears(1);
            Value = ClampToMonth(_currentMonth, Value.Day);
            await ValueChanged.InvokeAsync(Value);
        }

        private static DateTime ClampToMonth(DateTime month, int day) =>
            new(month.Year, month.Month, Math.Min(day, DateTime.DaysInMonth(month.Year, month.Month)));

        private async Task GoToTodayAsync() => await SelectDate(DateTime.Today);

        private async Task SelectDate(DateTime date)
        {
            Value = date;
            _currentMonth = new DateTime(date.Year, date.Month, 1);
            await ValueChanged.InvokeAsync(date);
        }

        private async Task JumpToMonth(DateTime month)
        {
            _currentMonth = new DateTime(month.Year, month.Month, 1);
            Value = ClampToMonth(_currentMonth, Value.Day);
            await SetViewAsync(SgCalendarView.Month);
            await ValueChanged.InvokeAsync(Value);
        }

        private async Task HandleHeaderKey(KeyboardEventArgs e)
        {
            switch (e.Key)
            {
                case "ArrowLeft": await PreviousRange(); break;
                case "ArrowRight": await NextRange(); break;
                case "Home": await GoToTodayAsync(); break;
            }
        }

        // ── Day enumeration ────────────────────────────────────────────────────

        private IEnumerable<DateTime> GetDays()
        {
            var firstDayOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            var startDay = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek + (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek);
            if (startDay > firstDayOfMonth) startDay = startDay.AddDays(-7);

            for (int i = 0; i < 42; i++)
                yield return startDay.AddDays(i);
        }

        private IEnumerable<DateTime> GetWeekDays()
        {
            var firstDayOfWeek = GetWeekStart(Value);
            for (var i = 0; i < 7; i++)
                yield return firstDayOfWeek.AddDays(i);
        }

        private static DateTime GetWeekStart(DateTime date)
        {
            var firstDayOfWeek = (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            var currentDay = (int)date.DayOfWeek;
            var offset = (7 + currentDay - firstDayOfWeek) % 7;
            return date.Date.AddDays(-offset);
        }

        private static int GetIsoWeek(DateTime date) => ISOWeek.GetWeekOfYear(date);

        private IEnumerable<DateTime> GetMiniMonthDays(DateTime month)
        {
            var first = new DateTime(month.Year, month.Month, 1);
            var startDay = first.AddDays(-(int)first.DayOfWeek + (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek);
            if (startDay > first) startDay = startDay.AddDays(-7);
            for (int i = 0; i < 42; i++) yield return startDay.AddDays(i);
        }

        // ── Events / filtering ─────────────────────────────────────────────────

        private IEnumerable<SgCalendarEvent> FilteredEvents()
        {
            if (Events == null) return Enumerable.Empty<SgCalendarEvent>();
            if (string.IsNullOrWhiteSpace(_search)) return Events;

            var q = _search.Trim();
            return Events.Where(e =>
                (!string.IsNullOrEmpty(e.Title) && e.Title.Contains(q, StringComparison.CurrentCultureIgnoreCase)) ||
                (!string.IsNullOrEmpty(e.Description) && e.Description!.Contains(q, StringComparison.CurrentCultureIgnoreCase)) ||
                (!string.IsNullOrEmpty(e.Location) && e.Location!.Contains(q, StringComparison.CurrentCultureIgnoreCase)) ||
                (!string.IsNullOrEmpty(e.Category) && e.Category!.Contains(q, StringComparison.CurrentCultureIgnoreCase)));
        }

        private IEnumerable<SgCalendarEvent> GetEventsForDate(DateTime date) =>
            FilteredEvents().Where(e => IsEventOnDate(e, date)).OrderBy(SortKey);

        private static (int, TimeSpan) SortKey(SgCalendarEvent e) =>
            (e.IsAllDay ? 0 : 1, e.StartTime ?? e.Date.TimeOfDay);

        private bool IsEventOnDate(SgCalendarEvent ev, DateTime date)
        {
            var startDate = ev.Date.Date;
            var endDate = (ev.EndDate ?? ev.Date).Date;
            if (endDate < startDate) endDate = startDate;

            if (date.Date >= startDate && date.Date <= endDate) return true;
            if (string.IsNullOrEmpty(ev.RecurrenceRule) || startDate > date.Date) return false;

            var parts = ev.RecurrenceRule.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('='))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].ToUpperInvariant(), p => p[1].ToUpperInvariant());

            if (!parts.TryGetValue("FREQ", out var freq)) return false;

            if (parts.TryGetValue("UNTIL", out var untilStr) &&
                DateTime.TryParseExact(untilStr, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var until) &&
                date.Date > until.Date) return false;

            return freq switch
            {
                "DAILY" => true,
                "WEEKLY" => ev.Date.DayOfWeek == date.DayOfWeek,
                "MONTHLY" => ev.Date.Day == date.Day,
                "YEARLY" => ev.Date.Month == date.Month && ev.Date.Day == date.Day,
                _ => false
            };
        }

        private IEnumerable<SgCalendarEvent> GetSelectedDayEvents() => GetEventsForDate(Value);

        private IEnumerable<DateTime> GetAgendaDays()
        {
            var range = Math.Max(1, AgendaRange);
            var start = Value.Date;
            for (var i = 0; i < range; i++)
            {
                var date = start.AddDays(i);
                if (GetEventsForDate(date).Any())
                    yield return date;
            }
        }

        private bool HasAnyEvents() => FilteredEvents().Any();

        // ── Search handlers ────────────────────────────────────────────────────

        private void OnSearchChanged(ChangeEventArgs e)
        {
            _search = e.Value?.ToString();
        }

        private void ClearSearch() => _search = null;

        // ── Drag and drop ──────────────────────────────────────────────────────

        private async Task HandleDropOnDate(DateTime date)
        {
            if (_draggedEvent == null || !EnableEditing || !EnableDragAndDrop) return;

            var ev = _draggedEvent;
            _draggedEvent = null;

            if (ev.IsReadOnly) return;
            if (ev.Date.Date == date.Date) return;

            var updatedEvents = GetMutableEvents();
            var target = updatedEvents.FirstOrDefault(e => e.Id == ev.Id);
            if (target != null)
            {
                var span = (target.EndDate ?? target.Date).Date - target.Date.Date;
                target.Date = date.Date.Add(target.Date.TimeOfDay);
                if (target.EndDate.HasValue) target.EndDate = target.Date.Add(span);
                await ApplyEventsChangeAsync(updatedEvents);
            }
        }

        // ── Modal lifecycle ────────────────────────────────────────────────────

        private async Task HandleAddClick(DateTime date)
        {
            if (OnAddEvent.HasDelegate)
            {
                await OnAddEvent.InvokeAsync(date);
            }
            else if (EnableCreation)
            {
                _isNewEvent = true;
                _editingEvent = new SgCalendarEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    Date = date,
                    Title = string.Empty,
                    Color = "#1890ff",
                    IsAllDay = true,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 0, 0),
                    Status = SgCalendarEventStatus.Confirmed,
                    Priority = SgCalendarEventPriority.Normal
                };
                _editingEventDate = date;
                _isModalVisible = true;
            }
        }

        private async Task HandleEventClick(SgCalendarEvent ev)
        {
            if (OnEventClick.HasDelegate)
                await OnEventClick.InvokeAsync(ev);
            else if (EnableEditing && !ev.IsReadOnly)
            {
                _isNewEvent = false;
                _editingEvent = CloneEvent(ev);
                _editingEventDate = ev.Date;
                _isModalVisible = true;
            }
        }

        private async Task HandleDeleteEvent(SgCalendarEvent ev)
        {
            if (ev.IsReadOnly) return;
            if (OnDeleteEvent.HasDelegate)
            {
                await OnDeleteEvent.InvokeAsync(ev);
            }
            else if (EnableEditing)
            {
                var updatedEvents = GetMutableEvents();
                if (updatedEvents.RemoveAll(e => e.Id == ev.Id) > 0)
                    await ApplyEventsChangeAsync(updatedEvents);
            }
        }

        private async Task SaveInternalEvent()
        {
            if (string.IsNullOrWhiteSpace(_editingEvent.Title)) return;

            _editingEvent.Date = _editingEventDate ?? DateTime.Today;
            if (_editingEvent.IsAllDay)
            {
                _editingEvent.StartTime = null;
                _editingEvent.EndTime = null;
            }

            if (OnEventSaved.HasDelegate)
            {
                await OnEventSaved.InvokeAsync(_editingEvent);
            }
            else if (EnableEditing || EnableCreation)
            {
                var updatedEvents = GetMutableEvents();
                var existingIndex = updatedEvents.FindIndex(e => e.Id == _editingEvent.Id);
                var eventCopy = CloneEvent(_editingEvent);

                if (existingIndex >= 0)
                {
                    updatedEvents[existingIndex] = eventCopy;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(eventCopy.Id))
                        eventCopy.Id = Guid.NewGuid().ToString();
                    updatedEvents.Add(eventCopy);
                }

                await ApplyEventsChangeAsync(updatedEvents);
            }

            _isModalVisible = false;
        }

        private async Task DeleteFromModal()
        {
            if (_isNewEvent) { _isModalVisible = false; return; }
            await HandleDeleteEvent(_editingEvent);
            _isModalVisible = false;
        }

        private List<SgCalendarEvent> GetMutableEvents() =>
            Events?.Select(CloneEvent).ToList() ?? new List<SgCalendarEvent>();

        private async Task ApplyEventsChangeAsync(List<SgCalendarEvent> events)
        {
            Events = events;
            if (EventsChanged.HasDelegate)
                await EventsChanged.InvokeAsync(events);
            await InvokeAsync(StateHasChanged);
        }

        private static SgCalendarEvent CloneEvent(SgCalendarEvent ev) => new()
        {
            Id = ev.Id,
            Title = ev.Title,
            Description = ev.Description,
            Date = ev.Date,
            EndDate = ev.EndDate,
            Color = ev.Color,
            Icon = ev.Icon,
            IsAllDay = ev.IsAllDay,
            StartTime = ev.StartTime,
            EndTime = ev.EndTime,
            RecurrenceRule = ev.RecurrenceRule,
            Location = ev.Location,
            Category = ev.Category,
            Url = ev.Url,
            Status = ev.Status,
            Priority = ev.Priority,
            IsReadOnly = ev.IsReadOnly
        };

        // ── Class / style helpers ──────────────────────────────────────────────

        private string RootClass()
        {
            var sb = new StringBuilder("sg-calendar");
            if (Compact) sb.Append(" sg-calendar-compact");
            if (Loading) sb.Append(" sg-calendar-loading");
            sb.Append(" sg-calendar-view-").Append(View.ToString().ToLowerInvariant());
            if (!string.IsNullOrEmpty(CssClass)) sb.Append(' ').Append(CssClass);
            return sb.ToString();
        }

        private string GetDayClass(DateTime date)
        {
            var classes = new List<string> { "sg-calendar-date" };
            if (date.Month != _currentMonth.Month && View == SgCalendarView.Month) classes.Add("sg-calendar-date-other-month");
            if (date.Date == DateTime.Today) classes.Add("sg-calendar-date-today");
            if (date.Date == Value.Date) classes.Add("sg-calendar-date-selected");
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) classes.Add("sg-calendar-date-weekend");
            if (View != SgCalendarView.Month) classes.Add("sg-calendar-date-compact");
            return string.Join(" ", classes);
        }

        private string GetEventClass(SgCalendarEvent ev)
        {
            var sb = new StringBuilder("sg-calendar-event");
            if (ev.IsAllDay) sb.Append(" sg-calendar-event-allday-chip");
            sb.Append(" sg-calendar-event-status-").Append(ev.Status.ToString().ToLowerInvariant());
            if (ev.Priority != SgCalendarEventPriority.Normal)
                sb.Append(" sg-calendar-event-priority-").Append(ev.Priority.ToString().ToLowerInvariant());
            if (ev.IsReadOnly) sb.Append(" sg-calendar-event-readonly");
            return sb.ToString();
        }

        private string GetHeaderTitle() => View switch
        {
            SgCalendarView.Week => $"{GetWeekStart(Value):dd MMM} – {GetWeekStart(Value).AddDays(6):dd MMM yyyy}",
            SgCalendarView.Day => Value.ToString("dddd, dd MMMM yyyy"),
            SgCalendarView.Agenda => $"{Value:dd MMM} – {Value.AddDays(Math.Max(1, AgendaRange) - 1):dd MMM yyyy}",
            SgCalendarView.Year => Value.Year.ToString(CultureInfo.CurrentCulture),
            _ => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_currentMonth.ToString("MMMM yyyy"))
        };

        private string GetWeekdayHeader(DateTime date)
        {
            var dayName = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames[(int)date.DayOfWeek];
            return $"{dayName}, {date:dd.MM}";
        }

        private string GetViewButtonClass(SgCalendarView view) =>
            view == View ? "sg-calendar-btn sg-calendar-btn-active" : "sg-calendar-btn";

        private IEnumerable<string> GetWeekdayNames()
        {
            var firstDay = (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            var names = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;
            for (int i = 0; i < 7; i++)
                yield return names[(firstDay + i) % 7];
        }

        private IEnumerable<string> GetMiniWeekdayNames()
        {
            var firstDay = (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            var names = CultureInfo.CurrentCulture.DateTimeFormat.ShortestDayNames;
            for (int i = 0; i < 7; i++)
                yield return names[(firstDay + i) % 7];
        }

        private static string FormatEventTime(SgCalendarEvent ev)
        {
            if (ev.IsAllDay) return string.Empty;
            var start = ev.StartTime ?? ev.Date.TimeOfDay;
            if (ev.EndTime.HasValue && ev.EndTime.Value > start)
                return $"{FormatTime(start)} – {FormatTime(ev.EndTime.Value)}";
            return FormatTime(start);
        }

        private static string FormatTime(TimeSpan t) =>
            new DateTime(2000, 1, 1).Add(t).ToString("t", CultureInfo.CurrentCulture);

        private string FormatTimeShort(SgCalendarEvent ev)
        {
            if (ev.IsAllDay) return string.Empty;
            var t = ev.StartTime ?? ev.Date.TimeOfDay;
            return new DateTime(2000, 1, 1).Add(t).ToString("HH:mm");
        }

        private string GetEventStyle(SgCalendarEvent ev)
        {
            if (string.IsNullOrEmpty(ev.Color)) return string.Empty;
            return $"--sg-event-color: {ev.Color};";
        }

        private string GetEventTitleAttr(SgCalendarEvent ev)
        {
            var time = FormatEventTime(ev);
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(time)) sb.Append(time).Append(" • ");
            sb.Append(ev.Title);
            if (!string.IsNullOrEmpty(ev.Location)) sb.Append('\n').Append(ev.Location);
            if (!string.IsNullOrEmpty(ev.Description)) sb.Append('\n').Append(ev.Description);
            return sb.ToString();
        }

        private void ToggleExpanded(DateTime date)
        {
            _expandedDate = _expandedDate == date.Date ? null : date.Date;
        }

        // ── Hour grid helpers (Day / Week views) ───────────────────────────────

        private IEnumerable<int> GetHourSlots()
        {
            for (int h = DayStartHour; h < DayEndHour; h++)
                yield return h;
        }

        private string FormatHour(int hour)
        {
            var dt = new DateTime(2000, 1, 1, hour, 0, 0);
            return dt.ToString("HH:mm");
        }

        private string GridHeightStyle()
        {
            var totalHours = Math.Max(1, DayEndHour - DayStartHour);
            return $"height: {totalHours * HourHeight}px;";
        }

        private string TimedEventStyle(SgCalendarEvent ev)
        {
            var color = string.IsNullOrEmpty(ev.Color) ? string.Empty : $"--sg-event-color: {ev.Color};";
            var start = ev.StartTime ?? ev.Date.TimeOfDay;
            var end = ev.EndTime ?? start.Add(TimeSpan.FromMinutes(45));
            if (end <= start) end = start.Add(TimeSpan.FromMinutes(30));

            var startMin = Math.Max(0, (start.TotalMinutes - DayStartHour * 60));
            var endMin = Math.Min((DayEndHour - DayStartHour) * 60, (end.TotalMinutes - DayStartHour * 60));
            var top = startMin / 60.0 * HourHeight;
            var height = Math.Max(HourHeight * 0.45, (endMin - startMin) / 60.0 * HourHeight);
            return $"{color}top: {top.ToString("0.##", CultureInfo.InvariantCulture)}px; height: {height.ToString("0.##", CultureInfo.InvariantCulture)}px;";
        }

        private string WorkRangeStyle()
        {
            if (!ShowWorkHours) return "display: none;";
            var ws = Math.Max(DayStartHour, WorkStartHour);
            var we = Math.Min(DayEndHour, WorkEndHour);
            if (we <= ws) return "display: none;";
            var top = (ws - DayStartHour) * HourHeight;
            var height = (we - ws) * HourHeight;
            return $"top: {top}px; height: {height}px;";
        }

        private string CurrentTimeIndicatorStyle(DateTime date)
        {
            if (date.Date != DateTime.Today) return "display: none;";
            var now = DateTime.Now.TimeOfDay;
            if (now < TimeSpan.FromHours(DayStartHour) || now >= TimeSpan.FromHours(DayEndHour))
                return "display: none;";
            var top = (now.TotalMinutes - DayStartHour * 60) / 60.0 * HourHeight;
            return $"top: {top.ToString("0.##", CultureInfo.InvariantCulture)}px;";
        }

        // ── Drag preview helpers ───────────────────────────────────────────────

        private void OnDragStart(SgCalendarEvent ev)
        {
            if (!EnableEditing || !EnableDragAndDrop || ev.IsReadOnly) return;
            _draggedEvent = ev;
        }

        private string SearchText => SearchPlaceholder ?? Localizer["Calendar_SearchPlaceholder"];

        private string StatusLabel(SgCalendarEventStatus s) => s switch
        {
            SgCalendarEventStatus.Confirmed => Localizer["Calendar_Status_Confirmed"],
            SgCalendarEventStatus.Tentative => Localizer["Calendar_Status_Tentative"],
            SgCalendarEventStatus.Cancelled => Localizer["Calendar_Status_Cancelled"],
            _ => s.ToString()
        };

        private string PriorityLabel(SgCalendarEventPriority p) => p switch
        {
            SgCalendarEventPriority.Normal => Localizer["Calendar_Priority_Normal"],
            SgCalendarEventPriority.High => Localizer["Calendar_Priority_High"],
            SgCalendarEventPriority.Urgent => Localizer["Calendar_Priority_Urgent"],
            _ => p.ToString()
        };
    }
}
