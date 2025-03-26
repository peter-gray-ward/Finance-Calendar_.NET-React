using System;
using System.Collections.Generic;
using System.Linq;
using FinanceCalendar;

namespace FinanceCalendar.Services
{
    public class Calendar
    {
        private readonly FinanceCalendarContext _context;

        public Calendar(FinanceCalendarContext context)
        {
            _context = context;
        }

        public List<List<Day>> GenerateCalendar(User user)
        {
            DateTime currentMonth = new DateTime(user.Account.Year, user.Account.Month, 1);
            DateTime previousMonth = currentMonth.AddMonths(-1);
            DateTime nextMonth = currentMonth.AddMonths(2);

            List<Event> events = _context.Events
                .Where(e => e.UserId == user.Id && e.Date >= previousMonth && e.Date < nextMonth)
                .ToList();

            List<List<Day>> weeks = new List<List<Day>>();
            string[] DOW = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
            

            DateTime startDate = DateTime.SpecifyKind(new DateTime(user.Account.Year, user.Account.Month, 1), DateTimeKind.Utc);

            startDate = startDate.AddDays(-(int)startDate.DayOfWeek);

            var timeZoneId = user.TimeZone ?? "UTC";

            TimeZoneInfo userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            DateTime userNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTimeZone);
            DateTime today = userNow.Date;

            List<Day> currentWeek = new List<Day>();

            while (weeks.Count < 6 || startDate.Month == user.Account.Month)
            {
                int dayOfMonth = startDate.Day;
                int dayOfWeekIndex = (int)startDate.DayOfWeek; // Sunday = 0, Monday = 1, etc.

                bool isToday = startDate.Date == today.Date;
                bool isAfterToday = isToday || startDate.Date > today.Date;

                Day day = new Day
                {
                    Date = dayOfMonth,
                    Name = DOW[dayOfWeekIndex],
                    Year = startDate.Year,
                    Month = startDate.Month,
                    IsToday = isToday,
                    IsTodayOrLater = isAfterToday
                };

                List<Event> dayEvents = new List<Event>();
                foreach (var ev in events)
                {
                    if (startDate.Date == ev.Date.Date)
                    {
                        dayEvents.Add(ev);
                        day.Total = ev.Total;
                    }
                }
                day.Events = dayEvents;

                if (isToday)
                {
                    day.Total = (double)user.CheckingBalance;
                }

                currentWeek.Add(day);

                if (currentWeek.Count == 7)
                {
                    weeks.Add(currentWeek);
                    currentWeek = new List<Day>();
                }

                startDate = startDate.AddDays(1);
            }

            return weeks;
        }

        public void CalculateEventTotals(User user)
        {
            List<Event> events = _context.Events
                .Where(e => e.UserId == user.Id)
                .OrderBy(e => e.Date)
                .ToList();

            foreach (var e in events)
            {
                if (e.Date < DateTime.UtcNow.Date)
                {
                    e.Total = 0.0;
                }
            }

            events = events.Where(e => e.Date >= DateTime.UtcNow.Date).ToList();

            double runningTotal = user.CheckingBalance;
            if (events.Count > 0)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    events[i].Total = runningTotal + events[i].Amount;
                    _context.Entry(events[i]).Property(ev => ev.Total).IsModified = true;
                    runningTotal = events[i].Total;

                    if (i < 10) Console.WriteLine($"{events[i].Date}: {events[i].Summary} = {events[i].Total}");
                }
            }

            _context.SaveChanges();
        }

        public ServiceResponse<List<List<Day>>> RefreshCalendar(User user)
        {
            user.Account.Expenses = _context.Expenses.Where(e => e.UserId == user.Id).ToList();
            try
            {
                List<List<Day>> cal = GenerateEventsFromExpenses(user);
                return new ServiceResponse<List<List<Day>>>.Builder()
                    .success(true)
                    .data(cal)
                    .build();
            }
            catch (Exception e)
            {
                return new ServiceResponse<List<List<Day>>>.Builder()
                    .success(false)
                    .message(e.Message)
                    .build();
            }
        }

        public User ChangeMonth(User user, int direction)
        {
            user.Account.Month += direction;

            if (direction == 0)
            {
                user.Account.Month = DateTime.Now.Month;
                user.Account.Year = DateTime.Now.Year;
            }
            else if (user.Account.Month > 12)
            {
                user.Account.Month = 1;
                user.Account.Year += 1;
            }
            else if (user.Account.Month < 1)
            {
                user.Account.Month = 12;
                user.Account.Year -= 1;
            }

            return user;
        }

        public ServiceResponse<List<List<Day>>> UpdateCheckingBalance(User user, double checkingBalance)
        {
            user.CheckingBalance = checkingBalance;
            _context.SaveChanges();

            CalculateEventTotals(user);

            try
            {
                List<List<Day>> cal = GenerateEventsFromExpenses(user);
                return new ServiceResponse<List<List<Day>>>.Builder()
                    .success(true)
                    .data(cal)
                    .user(user)
                    .build();
            }
            catch (Exception e)
            {
                return new ServiceResponse<List<List<Day>>>.Builder()
                    .success(false)
                    .message(e.Message)
                    .build();
            }
        }

        public List<List<Day>> GenerateEventsFromExpenses(User user)
        {
            List<Event> eventsToInsert = new();
            List<Event> eventsToRemove = _context.Events.Where(e => e.UserId == user.Id).ToList();

            foreach (Expense expense in user.Account.Expenses)
            {
                DateTime eventStart = DateTime.SpecifyKind(expense.StartDate, DateTimeKind.Utc);
                Guid recurrenceId = Guid.NewGuid();
                while (eventStart < expense.RecurrenceEndDate)
                {
                    Event ev = new Event();
                    ev.Id = Guid.NewGuid();
                    ev.Summary = expense.Name;
                    ev.Amount = expense.Amount;
                    ev.RecurrenceId = recurrenceId;
                    ev.Date = eventStart;
                    ev.RecurrenceEndDate = expense.RecurrenceEndDate;
                    ev.UserId = user.Id;
                    ev.Frequency = expense.Frequency;
                    ev.Total = 0.0;

                    eventsToInsert.Add(ev);

                    switch (expense.Frequency)
                    {
                        case "monthly":
                            eventStart = eventStart.AddMonths(1);
                            break;
                        case "biweekly":
                            eventStart = eventStart.AddDays(14);
                            break;
                        case "weekly":
                            eventStart = eventStart.AddDays(7);
                            break;
                        default:
                            eventStart = eventStart.AddDays(1);
                            break;
                    }
                }
            }

            using var transaction = _context.Database.BeginTransaction();

            _context.RemoveRange(eventsToRemove);
            _context.Events.AddRange(eventsToInsert);
            _context.SaveChanges();

            transaction.Commit();

            CalculateEventTotals(user);

            return GenerateCalendar(user);
        }

        public Expense AddExpense(User user)
        {
            Expense expense = new() { UserId = user.Id, Id = Guid.NewGuid() };
            _context.Expenses.Add(expense);
            _context.SaveChanges();
            return expense;
        }

        public ServiceResponse<object> DeleteExpense(User user, Guid expenseId)
        {
            var expense = _context.Expenses.FirstOrDefault(e => e.Id == expenseId);
            if (expense == null)
            {
                return new ServiceResponse<object>.Builder()
                    .success(false)
                    .message("Expense not found.")
                    .build();
            }

            _context.Expenses.Remove(expense);
            _context.SaveChanges();

            return new ServiceResponse<object>.Builder()
                .success(true)
                .build();
        }

        public ServiceResponse<object> UpdateExpense(User user, Expense expense)
        {
            Console.WriteLine("UpdateExpense");
            try
            {
                var existingExpense = _context.Expenses.FirstOrDefault(e => e.Id == expense.Id);
                if (existingExpense == null)
                {
                    _context.Expenses.Add(expense);
                }
                else
                {
                    var properties = typeof(Expense).GetProperties();
                    foreach (var prop in properties)
                    {
                        if (prop.Name == "Id") continue;

                        var newValue = prop.GetValue(expense);

                        if (prop.Name == "Amount" && newValue == null) newValue = 0.0;

                        prop.SetValue(existingExpense, newValue);
                    }
                }

                _context.SaveChanges();

                return new ServiceResponse<object>.Builder().success(true).build();
            }
            catch (Exception e)
            {
                return new ServiceResponse<object>.Builder()
                    .success(false)
                    .message(e.Message)
                    .build();
            }
        }

        public ServiceResponse<Event> SaveEvent(User user, Event ev, bool all)
        {
            try
            {
                var existingEvent =  _context.Events.FirstOrDefault(e => e.Id == ev.Id);
                
                if (existingEvent == null)
                {
                    _context.Events.Add(ev);
                }
                else
                {
                    var existingEvents = _context.Events
                        .Where(e => all ? e.RecurrenceId == ev.RecurrenceId : e.Id == ev.Id)
                        .ToList();

                    TimeSpan dateDiff = ev.Date - existingEvent.Date;

                    foreach (Event evt in existingEvents)
                    {
                        var properties = typeof(Event).GetProperties();
                        foreach (var prop in properties)
                        {
                            if (prop.Name == "Id") continue;

                            var newValue = prop.GetValue(ev);

                            if (all && prop.Name == "Date" && newValue is DateTime newDate)
                            {
                                newValue = evt.Date + dateDiff;
                            }

                            prop.SetValue(evt, newValue);
                        }
                    }
                }

                _context.SaveChanges();

                return new ServiceResponse<Event>.Builder()
                    .success(true)
                    .data(existingEvent ?? ev)
                    .user(user)
                    .build();
            }
            catch (Exception e)
            {
              return new ServiceResponse<Event>.Builder()
                    .success(false)
                    .message(e.Message)
                    .data(ev)
                    .build();
            }
        }
    }
}