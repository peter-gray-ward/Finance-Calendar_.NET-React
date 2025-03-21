using System;
using System.Collections.Generic;
using System.Linq;

namespace FinanceCalendar
{
    public class Calendar
    {
        public List<List<Day>> GetWeeks(User user, int month, int year, List<Event> events)
        {
            Console.WriteLine($"\t\t{year}: {month}");

            List<List<Day>> weeks = new List<List<Day>>();
            string[] DOW = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

            // Set the date to the first day of the current month
            DateTime cal = new DateTime(year, month, 1);

            // Adjust date to the nearest previous Sunday
            cal = cal.AddDays(-(int)cal.DayOfWeek);

            // Get today's date for comparison
            DateTime today = DateTime.Today;

            List<Day> currentWeek = new List<Day>();

            while (weeks.Count < 6 || cal.Month == month)
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
                    Console.WriteLine("found today");
                    day.Total = (double)user.CheckingBalance;
                }
                else if (0 == day.Events.Count)
                {
                    day.Total = 0.0;
                }

                currentWeek.Add(day);

                // If the week has 7 days, add it to the list of weeks
                if (currentWeek.Count == 7)
                {
                    weeks.Add(currentWeek);
                    currentWeek = new List<Day>();
                }

                // Move to the next day
                cal = cal.AddDays(1);
            }

            return weeks;
        }
    }
}