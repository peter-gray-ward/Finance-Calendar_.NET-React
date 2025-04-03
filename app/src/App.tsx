import React, { useRef, useState, useEffect, useCallback, useMemo } from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import './App.scss';
import { 
  User, DOW, MONTHNAMES, Day, ApiResponse, 
  IEvent, Expense, Position, Debt
} from './types';
import Modal from './Modal';
import { xhr, capitalizeKeys } from './util';
import EventModal from './EventModal';
import DayBlock from './DayBlock';
import Navigation from './Navigation';
import Outlook from './Outlook';
import Table from './Table';

function App({ _user }: { _user: User }) {
  const [user, setUser] = useState(_user);
  const [viewModal, setViewModal] = useState({
    event: false
  });
  const [windowWidth, setWindowWidth] = useState<number>(window.innerWidth);
  const [eventOrigin, setEventOrigin] = useState<Position>({ top: 0, left: 0 } as Position);
  const [event, setEvent] = useState<IEvent | null>(null);
  const [calendar, setCalendar] = useState<Day[][]>([]);
  const eventOriginRef = useRef<HTMLElement | null>(null);

  const handleResize = () => {
    setWindowWidth(window.innerWidth);
    if (event) {
      const calendarEvent = document.getElementById(`event-${event.id}`);
      if (calendarEvent) {
        const rect = calendarEvent.getBoundingClientRect();
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
        setEventOrigin({
          left: rect.left + 400 > window.innerWidth ? window.innerWidth - 410 : rect.left,
          top: rect.top + 200 > window.innerHeight ? window.innerHeight - 210 : rect.top
        });
      }
    }
  }, [event]);

  const logout = useCallback(() => {
    xhr({
      method: 'GET',
      url: '/logout'
    }).then((res: ApiResponse) => {
      if (res.success) {
        window.location.reload();
      }
    });
  }, []);

  const getCalendar = useCallback(() => {
    xhr({
      method: 'GET',
      url: '/get-calendar'
    }).then((res: ApiResponse) => {
      if (res.success) {
        setCalendar(res.data);
      }
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
      if (res.success) {
        setUser({
          ...user,
          account: {
            ...user.account,
            month: res.user!.account.month,
            year: res.user!.account.year
          }
        });
        setCalendar(res.data);
      }
    });
  }, []);

  return <Router>
    <Navigation setEvent={setEvent} />

    <header id="left">
      <button onClick={logout}>logout</button>
      <Table 
        cls={Expense}
        title="Expense" 
        user={user} 
        columns={['Expense', 'Frequency', 'Amount', 'Start Date', 'End Date']}
        order={['name', 'frequency', 'amount', 'startDate', 'recurrenceEndDate']}
        onAdd={(res: ApiResponse) => {
          if (res.success) {
            setUser({
              ...user,
              account: {
                ...user.account,
                expenses: [
                  ...user.account.expenses,
                  res.data as Expense
                ]
              }
            });
          }
        }}
        onDelete={(res: ApiResponse) => {
          if (res.success) {
            setUser({
              ...user,
              account: {
                ...user.account,
                expenses: user.account.expenses.filter((e: Expense) => e.id !== res.data)
              }
            });
          }
        }}
        localUpdate={(expense: Expense) => {
          setUser({
            ...user,
            account: {
              ...user.account,
              expenses: user.account.expenses.map((e: Expense) => {
                if (e.id == expense.id) {
                  return expense;
                }
                return e;
              })
            }
          });
        }}
        data={user.account.expenses} />
      
      <Table
        cls={Debt}
        title="Debt"
        user={user} 
        columns={['Creditor', 'Balance', 'Interest', 'Type', 'Date', 'Link']}
        order={['name', 'balance', 'interest', 'interestType', 'date', 'link']}
        onAdd={(res: ApiResponse) => {
          if (res.success) {
            setUser({
              ...user,
              account: {
                ...user.account,
                debts: [
                  ...user.account.debts,
                  res.data as Debt
                ]
              }
            });
          }
        }}
        onDelete={(res: ApiResponse) => {
          if (res.success) {
            setUser({
              ...user,
              account: {
                ...user.account,
                debts: user.account.debts.filter((d: Debt) => d.id !== res.data)
              }
            });
          }
        }}
        localUpdate={(debt: Debt) => {
          setUser({
            ...user,
            account: {
              ...user.account,
              debts: user.account.debts.map((d: Debt) => {
                if (d.id == debt.id) {
                  return debt;
                }
                return d;
              })
            }
          });
        }}
        data={user.account.debts} />
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
                  key={`${a}.${b}`}
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

    <footer id="right">
      <Outlook calendar={calendar} user={user} innerWidth={windowWidth} />
    </footer>

    <Routes>
      <Route path="/" element={<></>} />
      <Route path="/event/:id" element={<EventModal 
        origin={eventOrigin} 
        setEvent={setEvent}
        setCalendar={setCalendar} />} />
    </Routes>
  </Router>
}

export default App;
