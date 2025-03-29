import React, { useState, useMemo, useCallback } from 'react';
import { Day, User, MONTHNAMES } from './types';

export default function Outlook(
  { 
    calendar,
    user,
    innerWidth
  }: {
    calendar: Day[][],
    user: User,
    innerWidth: number
  }) {
	const allMonths = useMemo(() => {
    const result: Day[][][] = [];
    calendar.forEach((week: Day[]) => {
      let monthsAccounted = new Set();
      for (let day of week) {
        if ((day.year >= user.account.year && day.month >= user.account.month) || (day.year > user.account.year)) {
          if (!monthsAccounted.has(day.month)) {  
            if (!result[day.month]) {
              result[day.month] = [];
            }
            result[day.month].push(week);
            monthsAccounted.add(day.month);
          }
        }
      }
    });
    return result.sort((monthA: Day[][], monthB: Day[][]) => {
      return monthA[2][1].year - monthB[2][1].year;
    });
  }, [calendar, innerWidth, user]);

  const groupedMonths: Day[][][][] = useMemo(() => {
    const result: Day[][][][] = [];
    let group: Day[][][] = [];
    const groupSize = innerWidth < 950 ? 2 : 3;
    let groupIndex = 0;
    allMonths.forEach((month, monthIndex) => {
      if (!month) return;

      group[monthIndex] = month;
      groupIndex++;

      if (groupIndex % groupSize == 0) {
        result.push(group);
        group = [];
      }
    });
    return result;
  }, [allMonths, innerWidth, user]);



	return <section id="outlook">
		{
			groupedMonths.map((monthGroup: Day[][][]) => {
				return <div className="mini-month-row">
					{
						monthGroup.map((month: Day[][], monthIndex: number) => <div className="mini-month">
							<h2>{MONTHNAMES[month[2][1].month - 1]} {month[2][1].year}</h2>
							<div className="row">
								{['S','M','T','W','Th','F','Sa'].map(dow => <span className="dow">{dow}</span>)}
							</div>
							{
								month.map((week: Day[]) => <div className="row">
									{
										week.map((day: Day) => {
                      return <span className={`day-date${day.isToday ? ' today' : ''}`}>{day.date}</span>
                    })
									}
								</div>)
							}
						</div>)
					}
				</div>
			})
		}
	</section>
}