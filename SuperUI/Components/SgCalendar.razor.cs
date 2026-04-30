using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Localization;
using System.Globalization;

namespace SuperUI.Components
{
    public partial class SgCalendar : ComponentBase
    {
        /// <summary>
        /// Gets or sets the currently selected date.
        /// Default is today's date.
        /// </summary>
        [Parameter] public DateTime Value { get; set; } = DateTime.Today;
        
        /// <summary>
        /// Gets or sets the callback invoked when the selected date changes.
        /// </summary>
        [Parameter] public EventCallback<DateTime> ValueChanged { get; set; }
        
        /// <summary>
        /// Gets or sets the custom template for rendering date cell content.
        /// </summary>
        [Parameter] public RenderFragment<DateTime>? DateCellContent { get; set; }
        
        /// <summary>
        /// Gets or sets the calendar height.
        /// Default is "auto".
        /// </summary>
        [Parameter] public string Height { get; set; } = "auto";
        
        /// <summary>
        /// Gets or sets the calendar view mode.
        /// Default is <see cref="SgCalendarView.Month"/>.
        /// </summary>
        [Parameter] public SgCalendarView View { get; set; } = SgCalendarView.Month;
        
        /// <summary>
        /// Gets or sets the callback invoked when the view mode changes.
        /// </summary>
        [Parameter] public EventCallback<SgCalendarView> ViewChanged { get; set; }

        /// <summary>
        /// Gets or sets the collection of calendar events to display.
        /// </summary>
        [Parameter] public IEnumerable<SgCalendarEvent>? Events { get; set; }
        
        /// <summary>
        /// Gets or sets the callback invoked when the events collection changes.
        /// </summary>
        [Parameter] public EventCallback<IEnumerable<SgCalendarEvent>> EventsChanged { get; set; }
        
        /// <summary>
        /// Gets or sets the callback invoked when adding a new event.
        /// </summary>
        [Parameter] public EventCallback<DateTime> OnAddEvent { get; set; }
        
        /// <summary>
        /// Gets or sets the callback invoked when an event is clicked.
        /// </summary>
        [Parameter] public EventCallback<SgCalendarEvent> OnEventClick { get; set; }
        
        /// <summary>
        /// Gets or sets the callback invoked when deleting an event.
        /// </summary>
        [Parameter] public EventCallback<SgCalendarEvent> OnDeleteEvent { get; set; }

        /// <summary>
        /// Gets or sets whether event editing is enabled.
        /// </summary>
        [Parameter] public bool EnableEditing { get; set; }
        
        /// <summary>
        /// Gets or sets whether event creation is enabled.
        /// </summary>
        [Parameter] public bool EnableCreation { get; set; }
        
        /// <summary>
        /// Gets or sets the callback invoked when an event is saved.
        /// </summary>
        [Parameter] public EventCallback<SgCalendarEvent> OnEventSaved { get; set; }

        /// <summary>
        /// Gets or sets the timezone to use for event display.
        /// </summary>
        [Parameter] public TimeZoneInfo? TimeZone { get; set; }

        [Inject] private ISuperUILocalizer Localizer { get; set; } = default!;

        private DateTime _currentMonth = DateTime.Today;
        private bool _isModalVisible;
        private bool _isNewEvent;
        private SgCalendarEvent _editingEvent = new();
        private DateTime? _editingEventDate;
        private SgCalendarEvent? _draggedEvent;

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
            // Keep _currentMonth in sync when Value is changed externally
            // but don't override it if the user is navigating months
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
            {
                await ViewChanged.InvokeAsync(view);
            }
        }

        private async Task PreviousRange()
        {
            if (View == SgCalendarView.Month)
            {
                _currentMonth = _currentMonth.AddMonths(-1);
                Value = _currentMonth;
            }
            else if (View == SgCalendarView.Week)
            {
                Value = Value.AddDays(-7);
            }
            else
            {
                Value = Value.AddDays(-1);
            }

            await ValueChanged.InvokeAsync(Value);
        }

        private async Task NextRange()
        {
            if (View == SgCalendarView.Month)
            {
                _currentMonth = _currentMonth.AddMonths(1);
                Value = _currentMonth;
            }
            else if (View == SgCalendarView.Week)
            {
                Value = Value.AddDays(7);
            }
            else
            {
                Value = Value.AddDays(1);
            }

            await ValueChanged.InvokeAsync(Value);
        }

        private async Task GoToTodayAsync()
        {
            await SelectDate(DateTime.Today);
        }

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
            
            // If startDay is after firstDayOfMonth, we need to go back 7 days
            if (startDay > firstDayOfMonth) startDay = startDay.AddDays(-7);

            for (int i = 0; i < 42; i++) // 6 weeks
            {
                yield return startDay.AddDays(i);
            }
        }

        private IEnumerable<DateTime> GetWeekDays()
        {
            var firstDayOfWeek = GetWeekStart(Value);
            for (var i = 0; i < 7; i++)
            {
                yield return firstDayOfWeek.AddDays(i);
            }
        }

        private static DateTime GetWeekStart(DateTime date)
        {
            var firstDayOfWeek = (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            var currentDay = (int)date.DayOfWeek;
            var offset = (7 + currentDay - firstDayOfWeek) % 7;
            return date.Date.AddDays(-offset);
        }

        private IEnumerable<SgCalendarEvent> GetEventsForDate(DateTime date)
        {
            if (Events == null) return Enumerable.Empty<SgCalendarEvent>();
            return Events.Where(e => IsEventOnDate(e, date));
        }

        private bool IsEventOnDate(SgCalendarEvent ev, DateTime date)
        {
            if (ev.Date.Date == date.Date) return true;
            if (string.IsNullOrEmpty(ev.RecurrenceRule) || ev.Date.Date > date.Date) return false;

            // Simple RRULE parsing: FREQ=DAILY, WEEKLY, MONTHLY, YEARLY
            var parts = ev.RecurrenceRule.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('='))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].ToUpperInvariant(), p => p[1].ToUpperInvariant());

            if (!parts.TryGetValue("FREQ", out var freq)) return false;

            // Check UNTIL
            if (parts.TryGetValue("UNTIL", out var untilStr) && DateTime.TryParseExact(untilStr, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var until) && date.Date > until.Date)
                return false;

            switch (freq)
            {
                case "DAILY":
                    return true;
                case "WEEKLY":
                    return ev.Date.DayOfWeek == date.DayOfWeek;
                case "MONTHLY":
                    return ev.Date.Day == date.Day;
                case "YEARLY":
                    return ev.Date.Month == date.Month && ev.Date.Day == date.Day;
                default:
                    return false;
            }
        }

        private IEnumerable<SgCalendarEvent> GetSelectedDayEvents() => GetEventsForDate(Value).OrderBy(e => e.Title);

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
                    Title = "New event",
                    Color = "#1890ff",
                    IsAllDay = true
                };
                _editingEventDate = date;
                _isModalVisible = true;
            }
        }

        private async Task HandleEventClick(SgCalendarEvent ev)
        {
            if (OnEventClick.HasDelegate)
            {
                await OnEventClick.InvokeAsync(ev);
            }
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
                var removed = updatedEvents.RemoveAll(e => e.Id == ev.Id) > 0;
                if (removed)
                {
                    await ApplyEventsChangeAsync(updatedEvents);
                }
            }
        }

        private async Task SaveInternalEvent()
        {
            if (string.IsNullOrWhiteSpace(_editingEvent.Title)) return;
            
            _editingEvent.Date = _editingEventDate ?? DateTime.Today;
            
            if (OnEventSaved.HasDelegate)
            {
                await OnEventSaved.InvokeAsync(_editingEvent);
            }
            else if (EnableEditing)
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
                    {
                        eventCopy.Id = Guid.NewGuid().ToString();
                    }

                    updatedEvents.Add(eventCopy);
                }

                await ApplyEventsChangeAsync(updatedEvents);
            }
            
            _isModalVisible = false;
        }

        private List<SgCalendarEvent> GetMutableEvents()
        {
            return Events?
                .Select(CloneEvent)
                .ToList() ?? new List<SgCalendarEvent>();
        }

        private async Task ApplyEventsChangeAsync(List<SgCalendarEvent> events)
        {
            Events = events;

            if (EventsChanged.HasDelegate)
            {
                await EventsChanged.InvokeAsync(events);
            }

            await InvokeAsync(StateHasChanged);
        }

        private static SgCalendarEvent CloneEvent(SgCalendarEvent ev)
        {
            return new SgCalendarEvent
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
        }

        private string GetDayClass(DateTime date)
        {
            var classes = new List<string> { "sg-calendar-date" };
            if (date.Month != _currentMonth.Month) classes.Add("sg-calendar-date-other-month");
            if (date.Date == DateTime.Today) classes.Add("sg-calendar-date-today");
            if (date.Date == Value.Date) classes.Add("sg-calendar-date-selected");
            if (View != SgCalendarView.Month) classes.Add("sg-calendar-date-compact");
            return string.Join(" ", classes);
        }

        private string GetHeaderTitle()
        {
            return View switch
            {
                SgCalendarView.Week => $"{GetWeekStart(Value):dd MMM} - {GetWeekStart(Value).AddDays(6):dd MMM yyyy}",
                SgCalendarView.Day => Value.ToString("dd MMMM yyyy"),
                _ => _currentMonth.ToString("MMMM yyyy")
            };
        }

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
            {
                yield return names[(firstDay + i) % 7];
            }
        }
    }
}
