using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Localization;
using System.Globalization;

namespace SuperUI.Components
{
    public partial class SgCalendar : ComponentBase
    {
        [Parameter] public DateTime Value { get; set; } = DateTime.Today;
        [Parameter] public EventCallback<DateTime> ValueChanged { get; set; }

        [Parameter] public RenderFragment<DateTime>? DateCellContent { get; set; }
        [Parameter] public RenderFragment<SgCalendarEvent>? EventTemplate { get; set; }

        [Parameter] public string Height { get; set; } = "auto";

        [Parameter] public SgCalendarView View { get; set; } = SgCalendarView.Month;
        [Parameter] public EventCallback<SgCalendarView> ViewChanged { get; set; }

        [Parameter] public IEnumerable<SgCalendarEvent>? Events { get; set; }
        [Parameter] public EventCallback<IEnumerable<SgCalendarEvent>> EventsChanged { get; set; }

        [Parameter] public EventCallback<DateTime> OnAddEvent { get; set; }
        [Parameter] public EventCallback<SgCalendarEvent> OnEventClick { get; set; }
        [Parameter] public EventCallback<SgCalendarEvent> OnDeleteEvent { get; set; }
        [Parameter] public EventCallback<SgCalendarEvent> OnEventSaved { get; set; }

        [Parameter] public bool EnableEditing { get; set; }
        [Parameter] public bool EnableCreation { get; set; }

        /// <summary>Show ISO week numbers in the leftmost column of month view.</summary>
        [Parameter] public bool ShowWeekNumbers { get; set; }

        /// <summary>Maximum events shown per day cell in month view (the rest collapse into a "+N more" chip).</summary>
        [Parameter] public int MaxEventsPerDay { get; set; } = 3;

        /// <summary>Show the agenda view tab in the view switcher.</summary>
        [Parameter] public bool ShowAgendaView { get; set; } = true;

        /// <summary>Number of days the agenda view spans, starting from <see cref="Value"/>.</summary>
        [Parameter] public int AgendaRange { get; set; } = 30;

        [Parameter] public TimeZoneInfo? TimeZone { get; set; }

        [Inject] private ISuperUILocalizer Localizer { get; set; } = default!;

        private DateTime _currentMonth = DateTime.Today;
        private bool _isModalVisible;
        private bool _isNewEvent;
        private SgCalendarEvent _editingEvent = new();
        private DateTime? _editingEventDate;
        private SgCalendarEvent? _draggedEvent;
        private DateTime? _expandedDate;

        private static readonly string[] _recurrenceOptions = { "", "FREQ=DAILY", "FREQ=WEEKLY", "FREQ=MONTHLY", "FREQ=YEARLY" };
        private static readonly Dictionary<string, string> _recurrenceLabels = new()
        {
            [""] = "Never",
            ["FREQ=DAILY"] = "Daily",
            ["FREQ=WEEKLY"] = "Weekly",
            ["FREQ=MONTHLY"] = "Monthly",
            ["FREQ=YEARLY"] = "Yearly"
        };

        protected override void OnInitialized()
        {
            _currentMonth = new DateTime(Value.Year, Value.Month, 1);
        }

        protected override void OnParametersSet()
        {
            var expectedMonth = new DateTime(Value.Year, Value.Month, 1);
            if (_currentMonth == default)
                _currentMonth = expectedMonth;
        }

        private async Task HandleDropOnDate(DateTime date)
        {
            if (_draggedEvent == null || !EnableEditing) return;

            var ev = _draggedEvent;
            _draggedEvent = null;

            if (ev.Date.Date == date.Date) return;

            var updatedEvents = GetMutableEvents();
            var target = updatedEvents.FirstOrDefault(e => e.Id == ev.Id);
            if (target != null)
            {
                target.Date = date.Date.Add(ev.Date.TimeOfDay);
                await ApplyEventsChangeAsync(updatedEvents);
            }
        }

        private async Task SetViewAsync(SgCalendarView view)
        {
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
            }
            await ValueChanged.InvokeAsync(Value);
        }

        private async Task PreviousYear()
        {
            _currentMonth = _currentMonth.AddYears(-1);
            Value = new DateTime(_currentMonth.Year, _currentMonth.Month, Math.Min(Value.Day, DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month)));
            await ValueChanged.InvokeAsync(Value);
        }

        private async Task NextYear()
        {
            _currentMonth = _currentMonth.AddYears(1);
            Value = new DateTime(_currentMonth.Year, _currentMonth.Month, Math.Min(Value.Day, DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month)));
            await ValueChanged.InvokeAsync(Value);
        }

        private async Task GoToTodayAsync() => await SelectDate(DateTime.Today);

        private async Task SelectDate(DateTime date)
        {
            Value = date;
            _currentMonth = new DateTime(date.Year, date.Month, 1);
            await ValueChanged.InvokeAsync(date);
        }

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

        private static int GetIsoWeek(DateTime date) =>
            ISOWeek.GetWeekOfYear(date);

        private IEnumerable<SgCalendarEvent> GetEventsForDate(DateTime date)
        {
            if (Events == null) return Enumerable.Empty<SgCalendarEvent>();
            return Events.Where(e => IsEventOnDate(e, date)).OrderBy(SortKey);
        }

        private static (int, TimeSpan) SortKey(SgCalendarEvent e) =>
            (e.IsAllDay ? 0 : 1, e.StartTime ?? e.Date.TimeOfDay);

        private bool IsEventOnDate(SgCalendarEvent ev, DateTime date)
        {
            if (ev.Date.Date == date.Date) return true;
            if (string.IsNullOrEmpty(ev.RecurrenceRule) || ev.Date.Date > date.Date) return false;

            var parts = ev.RecurrenceRule.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('='))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].ToUpperInvariant(), p => p[1].ToUpperInvariant());

            if (!parts.TryGetValue("FREQ", out var freq)) return false;

            if (parts.TryGetValue("UNTIL", out var untilStr) && DateTime.TryParseExact(untilStr, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var until) && date.Date > until.Date)
                return false;

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
                    EndTime = new TimeSpan(10, 0, 0)
                };
                _editingEventDate = date;
                _isModalVisible = true;
            }
        }

        private async Task HandleEventClick(SgCalendarEvent ev)
        {
            if (OnEventClick.HasDelegate)
                await OnEventClick.InvokeAsync(ev);
            else if (EnableEditing)
            {
                _isNewEvent = false;
                _editingEvent = CloneEvent(ev);
                _editingEventDate = ev.Date;
                _isModalVisible = true;
            }
        }

        private async Task HandleDeleteEvent(SgCalendarEvent ev)
        {
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
            Color = ev.Color,
            Icon = ev.Icon,
            IsAllDay = ev.IsAllDay,
            StartTime = ev.StartTime,
            EndTime = ev.EndTime,
            RecurrenceRule = ev.RecurrenceRule
        };

        private string GetDayClass(DateTime date)
        {
            var classes = new List<string> { "sg-calendar-date" };
            if (date.Month != _currentMonth.Month) classes.Add("sg-calendar-date-other-month");
            if (date.Date == DateTime.Today) classes.Add("sg-calendar-date-today");
            if (date.Date == Value.Date) classes.Add("sg-calendar-date-selected");
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) classes.Add("sg-calendar-date-weekend");
            if (View != SgCalendarView.Month) classes.Add("sg-calendar-date-compact");
            return string.Join(" ", classes);
        }

        private string GetHeaderTitle() => View switch
        {
            SgCalendarView.Week => $"{GetWeekStart(Value):dd MMM} – {GetWeekStart(Value).AddDays(6):dd MMM yyyy}",
            SgCalendarView.Day => Value.ToString("dddd, dd MMMM yyyy"),
            SgCalendarView.Agenda => $"{Value:dd MMM} – {Value.AddDays(Math.Max(1, AgendaRange) - 1):dd MMM yyyy}",
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

        private string GetEventStyle(SgCalendarEvent ev)
        {
            if (string.IsNullOrEmpty(ev.Color)) return string.Empty;
            return $"--sg-event-color: {ev.Color};";
        }

        private string GetEventTitleAttr(SgCalendarEvent ev)
        {
            var time = FormatEventTime(ev);
            if (string.IsNullOrEmpty(time)) return ev.Title;
            return string.IsNullOrEmpty(ev.Description) ? $"{time} • {ev.Title}" : $"{time} • {ev.Title}\n{ev.Description}";
        }

        private void ToggleExpanded(DateTime date)
        {
            _expandedDate = _expandedDate == date.Date ? null : date.Date;
        }
    }
}
