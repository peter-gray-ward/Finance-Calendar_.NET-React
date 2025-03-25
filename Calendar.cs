using System;
using System.Collections.Generic;
using System.Linq;

namespace FinanceCalendar
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

            DateTime today = DateTime.UtcNow.Date;

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
                        day.Total = ev.Amount;
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
                if (e.Date < DateTime.UtcNow)
                {
                    e.Total = 0.0;
                }
            }

            events = events.Where(e => e.Date >= DateTime.UtcNow).ToList();

            double runningTotal = user.CheckingBalance;
            if (events.Count > 0)
            {
                runningTotal += events[0].Amount;
                events[0].Total = runningTotal;
                for (int i = 1; i < events.Count; i++)
                {
                    events[i].Total = runningTotal + events[i].Amount;
                    if (i < 10) Console.WriteLine(events[i].Total);
                    runningTotal = events[i].Total;
                    if (events[i].Summary == "Amazon Prime") {
                        Console.WriteLine($"Amazon Prime {events[i].Date}");
                    }
                }
            }

            _context.SaveChanges();
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

                    if (ev.Summary == "Amazon Prime") {
                        Console.WriteLine($"Amazon Prime {ev.Date}");
                    }

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
            Console.WriteLine($"Inserting {eventsToInsert.Count} events");


            using var transaction = _context.Database.BeginTransaction();

            _context.RemoveRange(eventsToRemove);
            _context.Events.AddRange(eventsToInsert);
            _context.SaveChanges();

            transaction.Commit();

            CalculateEventTotals(user);

            return GenerateCalendar(user);
        }
    }
}