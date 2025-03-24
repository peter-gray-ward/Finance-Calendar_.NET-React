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

            DateTime cal = new DateTime(user.Account.Year, user.Account.Month, 1);

            cal = cal.AddDays(-(int)cal.DayOfWeek);

            DateTime today = DateTime.Today;

            List<Day> currentWeek = new List<Day>();

            while (weeks.Count < 6 || cal.Month == user.Account.Month)
            {
                int dayOfMonth = cal.Day;
                int dayOfWeekIndex = (int)cal.DayOfWeek; // Sunday = 0, Monday = 1, etc.

                bool isToday = cal.Date == today.Date;
                bool isAfterToday = isToday || cal.Date > today.Date;

                Day day = new Day
                {
                    Date = dayOfMonth,
                    Name = DOW[dayOfWeekIndex],
                    Year = cal.Year,
                    Month = cal.Month,
                    IsToday = isToday,
                    IsTodayOrLater = isAfterToday
                };

                // Check for events on this day
                List<Event> dayEvents = new List<Event>();
                foreach (var evt in events)
                {
                    if (cal.Date == evt.Date.Date)
                    {
                        dayEvents.Add(evt);
                        day.Total = evt.Total;
                    }
                }
                day.Events = dayEvents;

                if (isToday)
                {
                    day.Total = (double)user.CheckingBalance;
                }
                else if (0 == day.Events.Count)
                {
                    day.Total = 0.0;
                }

                currentWeek.Add(day);

                if (currentWeek.Count == 7)
                {
                    weeks.Add(currentWeek);
                    currentWeek = new List<Day>();
                }

                cal = cal.AddDays(1);
            }

            return weeks;
        }
    }
}