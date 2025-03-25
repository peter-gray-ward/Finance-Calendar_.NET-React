import React, { useRef, useState, useEffect, useCallback, useMemo } from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import './App.scss';
import { User, DOW, MONTHNAMES, Day, ApiResponse, IEvent, Expense, Position } from './types';
import Modal from './Modal';
import { xhr, capitalizeKeys } from './util';
import { error } from 'console';
import Expenses from './Expenses';
import Debts from './Debts';
import Event from './Event';
import DayBlock from './DayBlock';

function App({ _user }: { _user: User }) {
  const [user, setUser] = useState(_user);
  const [viewModal, setViewModal] = useState({
    event: false
  });
  const [eventOrigin, setEventOrigin] = useState<Position>({ top: 0, left: 0 } as Position);
  const [event, setEvent] = useState<IEvent | null>(null);
  const [calendar, setCalendar] = useState<Day[][]>([]);
  const [expanding, setExpanding] = useState<boolean>(false);
  const eventOriginRef = useRef<HTMLElement | null>(null);

  const handleResize = () => {
    console.log("...", event)
    if (event) {
      const calendarEvent = document.getElementById(`event-${event.id}`);
      if (calendarEvent) {
        const rect = calendarEvent.getBoundingClientRect();
        console.log(rect)
        setEventOrigin({
          left: rect.left + 400 > window.innerWidth ? window.innerWidth - 410 : rect.left,
          top: rect.top + 200 > window.innerHeight ? window.innerHeight - 210 : rect.top
        });
      }
    }
  };


  useEffect(() => {
    if (event) {
      const calendarEvent = document.getElementById(`event-${event.id}`);
      if (calendarEvent) {
        const rect = calendarEvent.getBoundingClientRect();
        console.log(rect)
        setEventOrigin({
          left: rect.left + 400 > window.innerWidth ? window.innerWidth - 410 : rect.left,
          top: rect.top + 200 > window.innerHeight ? window.innerHeight - 210 : rect.top
        });
      }
    }
  }, [event]);

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
    xhr({
      method: 'GET',
      url: '/logout'
    }).then((res: ApiResponse) => {
      window.location.reload();
    });
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
    return () => {
      window.removeEventListener('resize', handleResize);
    };
  }, [event]);

  useEffect(() => {
    getCalendar();
  }, []);

  const changeMonth = useCallback((direction: number) => {
    xhr({
      method: 'GET',
      url: '/change-month/' + direction
    }).then((res: ApiResponse) => {
      setUser({
        ...user,
        account: {
          ...user.account,
          month: res.user!.account.month,
          year: res.user!.account.year
        }
      });
      setCalendar(res.data);
    });
  }, []);

  return <Router>
    <button id="expand-to-budget" onClick={expandToBudget}>☰</button>
    <header id="left">
      <button onClick={logout}>logout</button>
      <Expenses user={user} setUser={setUser} setCalendar={setCalendar} />
      <Debts user={user} />
    </header>
    <main id="main">
      <div id="calendar-month-header">
        <div>
          <h1 id="month-name">{MONTHNAMES[user.account.month - 1]}</h1>
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
            <div className="week" key={a}>
              {
                week.map((day: Day, b: number) => <DayBlock 
                    user={user} 
                    day={day} 
                    a={a} b={b} 
                    setEvent={setEvent}
                    setEventOrigin={setEventOrigin}
                    setCalendar={setCalendar}
                    setUser={setUser} />)
              }
            </div>
          ))
        }
      </div>
    </main>
    <Routes>
      <Route path="/event/:id" element={<Event origin={eventOrigin} setEvent={setEvent} />} />
    </Routes>
  </Router>
}

export default App;
