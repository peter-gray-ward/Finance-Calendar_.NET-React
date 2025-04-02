import React, { useState, useMemo, useCallback } from 'react';
import { Day, User, MONTHNAMES } from './types';
import { findLastEventOfMonth, findLastDayOfMonth } from './util';

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

	const groupedMonths: Day[][][][] = useMemo(() => {
    const result: Day[][][][] = [];

    let allMonths: Day[][][] = [];
    let group: Day[][][] = [];
    const groupSize = innerWidth < 950 ? 2 : 3;
    let groupIndex = 0;

    calendar.forEach((week: Day[]) => {
      let monthsAccounted = new Set();
      for (let day of week) {
        if ((day.year >= user.account.year && day.month >= user.account.month) || (day.year > user.account.year)) {
          if (!monthsAccounted.has(day.month)) {  
            if (!allMonths[day.month]) {
              allMonths[day.month] = [];
            }
            allMonths[day.month].push(week);
            monthsAccounted.add(day.month);
          }
        }
      }
    });

    allMonths.sort((monthA: Day[][], monthB: Day[][]) => {
      return monthA[2][1].year - monthB[2][1].year;
    }).forEach((month, monthIndex) => {
      if (!month) return;

      group[monthIndex] = month;
      groupIndex++;

      if (groupIndex % groupSize == 0) {
        result.push(group);
        group = [];
      }
    });
    return result;
  }, [calendar, innerWidth, user]);

	return <section id="outlook">
		{
			groupedMonths.map((monthGroup: Day[][][], i: number) => {
				return <div key={i} className="mini-month-row">
					{
						monthGroup.map((month: Day[][], j: number) => {
              const monthIndex = month[2][1].month;
              const lastDayOfMonth = findLastDayOfMonth(monthIndex, month, user);
              const lastEventOfMonth = findLastEventOfMonth(monthIndex, month, user);
              let monthFinalTotal = lastEventOfMonth.total.toFixed(0);
              const inTheNegative = +monthFinalTotal < 0;
              var debtBalance = 0;
              for (let debt of lastDayOfMonth.debts) {
                debtBalance += debt.balance;
              }
              return <div key={`${i}.${monthIndex}`} className="mini-month">
  							<h2>{MONTHNAMES[monthIndex - 1]} {month[2][1].year}</h2>
  							<div className="row">
  								{['S','M','T','W','Th','F','Sa'].map(dow => <span key={dow} className="dow">{dow}</span>)}
  							</div>
                <div className="weeks">
    							{
    								month.map((week: Day[], k: number) => <div key={`${i}.${j}.${k}`} className="row">
    									{
    										week.map((day: Day) => {
                          return <span key={`${i}.${j}.${k}.${day.dow}`} className={
                            `day-date${day.isToday ? ' today' : ''}`
                            + ` ${day.month == monthIndex ? '' : 'opaque'}`
                          }>{day.date}</span>
                        })
    									}
    								</div>)
    							}
                </div>
                <div className="info-footer">
                  <i>Db: </i>
                  <span className={debtBalance < 0 ? 'negative' : 'positive'}>
                    {debtBalance < 0 ? '-$' + Math.abs(+debtBalance).toFixed(0) : '$' + debtBalance.toFixed(0)}
                  </span>
                  <i>Ch: </i>
                  <span className={inTheNegative ? 'negative' : 'positive'}>
                    {inTheNegative ? '-$' + Math.abs(+monthFinalTotal) : '$' + monthFinalTotal}
                  </span>
                </div>
  						</div>
            })
					}
  			</div>
			})
		}
	</section>
}