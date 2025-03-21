import React, { useRef, useState, useEffect, useCallback, useMemo } from 'react';
import './App.scss';
import { User, DOW, MONTHNAMES, Day, ApiResponse, Event } from './types';
import Modal from './Modal';
import { xhr, capitalizeKeys } from './util';
import * as signalR from '@microsoft/signalr';
import { error } from 'console';

function App({ user }: { user: User }) {
  const [viewModal, setViewModal] = useState({
    event: false
  });
  const [pagePositions, setPagePositions] = useState({
    eventOrigin: { top: 0, left: 0 }
  });
  const [calendar, setCalendar] = useState<Day[][]>([]);
  const [expanding, setExpanding] = useState<boolean>(false);
  const eventOriginRef = useRef<HTMLElement | null>(null);
  const checkingBalanceRef = useRef<HTMLInputElement | null>(null);

  const handleResize = () => {
    setPagePositions({
      eventOrigin: {
        top: eventOriginRef.current ? eventOriginRef.current.getBoundingClientRect().top : 0,
        left: eventOriginRef.current ? eventOriginRef.current.getBoundingClientRect().left : 0
      }
    });
  };

  const expandToBudget = useCallback(() => {
    if (expanding) return;
    setExpanding(true);

    const body = document.body;
    const header = document.querySelector('header');

    if (!body.classList.contains('view-left')) {
      body.classList.add('view-left');
    } else {
      body.classList.remove('view-left');
    }
  }, []);

  const logout = useCallback(() => {

  }, []);

  const addExpense = useCallback(() => {

  }, []);

  const refreshCalendar = useCallback(() => {

  }, []);

  const updateCheckingBalance = useCallback(() => {

  }, []);

  

  const getCalendar = useCallback(() => {
    xhr({
      method: 'GET',
      url: '/get-calendar'
    }).then((res: ApiResponse) => {
      console.log("get-calendar", res);
      setCalendar(res.data);
    }); 
  }, []);

  useEffect(() => {
    window.addEventListener('resize', handleResize);
    // clean up the event on unmount
    return () => {
      window.removeEventListener('resize', handleResize);
    };
  }, []);

  useEffect(() => {
    getCalendar();
  }, []);

  const clickMain = useCallback((event: any) => {

  }, []);

  const changeMonth = useCallback((which: number) => {

  }, []);

  if (!user || !user.account) {
    debugger
  }


  return <>
    <button id="expand-to-budget" onClick={expandToBudget}>☰</button>
    <header id="left">
      <button onClick={logout}>logout</button>
      <h2>
        <div>Regular Expenses</div>
      </h2>
      <div className="table" id="expenses">
        <div className="tr">
          <div className="th">Expense</div>
          <div className="th">Frequency</div>
          <div className="th">Amount</div>
          <div className="th">Start Date</div>
          <div className="th">End Date</div>
        </div>
        {

        }
      </div>
      <div className="tr button-row-right">
        <button className="add-expense" onClick={addExpense}>+</button>
        <button id="refresh-calendar" onClick={refreshCalendar}>↺</button>
      </div>
      <h2>
        <div>Debts</div>
      </h2>
      <div className="table" id="debts">
        <div className="tr">
          <div className="th">Creditor</div>
          <div className="th">Account Number</div>
          <div className="th">Balance</div>
          <div className="th">Interest</div>
          <div className="th">Payoff Date</div>
          <div className="th">Event</div>
        </div>
      
      </div>
      <div className="tr button-row-right">
        <button id="add-debt">+</button>
      </div>
    </header>
    <main id="main" onClick={clickMain}>
      <div id="calendar-month-header">
        <div>
          <h1 id="month-name">{MONTHNAMES[user.account.month]}</h1>
          &nbsp;
          <h1 id="year-name">{user.account.year}</h1>
        </div>
        <div>
          <button id="prev-month" onClick={() => changeMonth(-1)}>
            <span>
              ∟
            </span>
          </button>
          <button id="go-to-today" onClick={() => changeMonth(0)}>
            Today
          </button>
          <button id="next-month" onClick={() => changeMonth(1)}>
            <span>
              ∟
            </span>
          </button>
        </div>
      </div>
      <div id="calendar-week-header">
        <div className="weekend">Sun</div>
        <div>Mon</div>
        <div>Tue</div>
        <div>Wed</div>
        <div>Thu</div>
        <div>Fri</div>
        <div className="weekend">Sat</div>
      </div>
      <div id="calendar">
        {
          calendar.map((week: Day[], a: number) => (
            <div className="week" key={`${a}`}>
              {
                week.map((day: Day, b: number) => (
                  <div className="day-block" key={`${a}.${b}`}>
                    <div className="day-header">
                      <div className="total">
                         {
                          day.isToday ? <input type="number"
                            value={ user.checkingBalance }
                            onChange={updateCheckingBalance}
                            ref={checkingBalanceRef}
                            id="checking-balance"/> : (
                              day.events.length ? day.total : null
                            )
                          }
                        </div>
                      <div className={`day-date${day.isToday ? ' today' : ''}`}>
                        { day.date }
                      </div>
                    </div>
                    <div className="events">
                      {
                        day.events.map((event: Event, c: number) => (
                          <div className="event" id={`event-${ event.id }`} key={`${a}.${b}.${c}`}>
                            <span>•</span> 
                            <span className="summary">{ event.summary.replace('&nbsp;', '').replace('   ', '') }</span> 
                            <span>
                              { event.amount }
                            </span>
                          </div>
                        ))
                      }
                    </div>
                  </div>
                ))
              }
            </div>
          ))
        }
      </div>
    </main>
  </>
}

export default App;
