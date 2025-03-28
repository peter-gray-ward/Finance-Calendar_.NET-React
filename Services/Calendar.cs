using System;
using System.Collections.Generic;
using System.Linq;
using FinanceCalendar;
using Microsoft.EntityFrameworkCore;

namespace FinanceCalendar.Services
{
    public class Calendar
    {
        private readonly FinanceCalendarContext _context;

        public Calendar(FinanceCalendarContext context)
        {
            _context = context;
        }

        public async Task<List<List<Day>>> GenerateCalendar(User user)
        {
            DateTime currentMonth = new DateTime(user.Account.Year, user.Account.Month, 1);
            DateTime previousMonth = currentMonth.AddMonths(-1);
            DateTime nextMonth = currentMonth.AddMonths(2);

            List<Event> events = await _context.Events
                .Where(e => e.UserId == user.Id && e.Date >= previousMonth && e.Date < nextMonth)
                .ToListAsync();

            List<List<Day>> weeks = new List<List<Day>>();
            string[] DOW = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
            

            DateTime startDate = DateTime.SpecifyKind(new DateTime(user.Account.Year, user.Account.Month, 1), DateTimeKind.Utc);

            startDate = startDate.AddDays(-(int)startDate.DayOfWeek);

            DateTime yearFromStart = startDate.AddDays(365);

            var timeZoneId = user.TimeZone ?? "UTC";

            TimeZoneInfo userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            DateTime userNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTimeZone);
            DateTime today = userNow.Date;

            List<Day> currentWeek = new List<Day>();

            while (startDate.Date < yearFromStart.Date)
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

        public async Task CalculateEventTotals(User user)
        {
            List<Event> priorEvents = await _context.Events
                .Where(e => e.UserId == user.Id && e.Date < DateTime.UtcNow.Date)
                .ToListAsync();

            foreach (var e in priorEvents)
            {
                e.Total = 0.0;
            }

            await _context.SaveChangesAsync();

            List<Event> events = await _context.Events
                .Where(e => e.UserId == user.Id && e.Date >= DateTime.UtcNow.Date)
                .OrderBy(e => e.Date)
                .ToListAsync();

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

            await _context.SaveChangesAsync();
        }

        public async Task<ServiceResponse<List<List<Day>>>> RefreshCalendar(User user)
        {
            user.Account.Expenses = await _context.Expenses.Where(e => e.UserId == user.Id).ToListAsync();
            try
            {
                List<List<Day>> cal = await GenerateEventsFromExpenses(user);
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

        public async Task<ServiceResponse<List<List<Day>>>> UpdateCheckingBalance(User user, double checkingBalance)
        {
            user.CheckingBalance = checkingBalance;
            await _context.SaveChangesAsync();

            await CalculateEventTotals(user);

            try
            {
                List<List<Day>> cal = await GenerateEventsFromExpenses(user);
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

        public async Task<List<List<Day>>> GenerateEventsFromExpenses(User user)
        {
            List<Event> eventsToInsert = new();
            List<Event> eventsToRemove = await _context.Events.Where(e => e.UserId == user.Id).ToListAsync();

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

            await using var transaction = _context.Database.BeginTransaction();

            _context.RemoveRange(eventsToRemove);
            await _context.Events.AddRangeAsync(eventsToInsert);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await CalculateEventTotals(user);

            return await GenerateCalendar(user);
        }

        public async Task<Expense> AddExpense(User user)
        {
            Expense expense = new() { UserId = user.Id, Id = Guid.NewGuid() };
            await _context.Expenses.AddAsync(expense);
            await _context.SaveChangesAsync();
            return expense;
        }

        public async Task<ServiceResponse<object>> DeleteExpense(User user, Guid expenseId)
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
            _context.SaveChangesAsync();

            return new ServiceResponse<object>.Builder()
                .success(true)
                .build();
        }

        public async Task<ServiceResponse<object>> UpdateExpense(User user, Expense expense)
        {
            Console.WriteLine("UpdateExpense");
            try
            {
                var existingExpense = _context.Expenses.FirstOrDefault(e => e.Id == expense.Id);
                if (existingExpense == null)
                {
                    await _context.Expenses.AddAsync(expense);
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

                await _context.SaveChangesAsync();

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

        private async Task RecurEvent(Event ev)
        {
            var properties = typeof(Event).GetProperties();
            DateTime startDate = ev.Date;
            while (startDate <= ev.RecurrenceEndDate)
            {
                Event evt = _context.Events.FirstOrDefault(e => 
                    e.RecurrenceId == ev.RecurrenceId 
                    && e.Date.Date == startDate.Date
                );
                if (evt == null)
                {
                    evt = new Event();
                    foreach (var prop in properties)
                    {
                        if (prop.Name == "Id") continue;

                        var newValue = prop.GetValue(ev);

                        if (prop.Name == "Date")
                        {
                            newValue = startDate;
                        }

                        prop.SetValue(evt, newValue);

                        _context.Events.AddAsync(evt);
                    }
                }
                else
                {
                    foreach (var prop in properties)
                    {
                        if (prop.Name == "Id") continue;
                        if (prop.Name == "Date") continue;

                        var newValue = prop.GetValue(ev);

                        prop.SetValue(evt, newValue);
                    }
                }
                switch (ev.Frequency)
                {
                    case "monthly":
                        startDate = startDate.AddMonths(1);
                        break;
                    case "biweekly":
                        startDate = startDate.AddDays(14);
                        break;
                    case "weekly":
                        startDate = startDate.AddDays(7);
                        break;
                    default:
                        startDate = startDate.AddDays(1);
                        break;
                }
            }
        }

        public async Task<ServiceResponse<Event>> SaveEvent(User user, Event ev, bool all)
        {
            try
            {
                var existingEvent =  _context.Events.FirstOrDefault(e => e.Id == ev.Id);

                
                if (existingEvent == null)
                {
                    if (all)
                    {
                        await RecurEvent(ev);
                    }
                    else
                    {
                        await _context.Events.AddAsync(ev);
                    }
                }
                else
                {
                    var properties = typeof(Event).GetProperties();
                    if (all)
                    {
                        await RecurEvent(ev);
                    }
                    else
                    {
                        bool didChangeDate = false;

                        if (ev.Date != existingEvent.Date)
                        {
                            didChangeDate = true;
                        }

                        foreach (var prop in properties)
                        {
                            if (prop.Name == "Id") continue;

                            var newValue = prop.GetValue(ev);

                            prop.SetValue(existingEvent, newValue);
                        }

                        if (didChangeDate)
                        {
                            existingEvent.RecurrenceId = Guid.NewGuid();
                        }
                    }
                }

                await _context.SaveChangesAsync();

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

        public async Task<ServiceResponse<bool>> DeleteEvent(User user, Guid eventId, Guid recurrenceId)
        {
            try
            {
                if (recurrenceId != Guid.Empty)
                {
                    var events = await _context.Events
                        .Where(e => e.RecurrenceId == recurrenceId)
                        .ToListAsync();

                    if (events.Any())
                    {
                        _context.Events.RemoveRange(events);
                        await _context.SaveChangesAsync();
                        return new ServiceResponse<bool>.Builder()
                            .success(true)
                            .build();
                    }
                }
                else
                {
                    var ev = await _context.Events.FindAsync(eventId);
                    if (ev != null)
                    {
                        _context.Events.Remove(ev);
                        await _context.SaveChangesAsync();
                        return new ServiceResponse<bool>.Builder()
                            .success(true)
                            .build();
                    }
                }

                return new ServiceResponse<bool>.Builder()
                    .success(false)
                    .message("Event(s) not found.")
                    .build();
            }
            catch (Exception ex)
            {
                return new ServiceResponse<bool>.Builder()
                    .success(false)
                    .message($"Error deleting event(s): {ex.Message}")
                    .build();
            }
        }

    }
}