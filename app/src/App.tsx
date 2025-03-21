import React, { useRef, useState, useEffect, useCallback, useMemo } from 'react';
import './App.scss';
import { User, DOW, MONTHNAMES, Day, ApiResponse } from './types';
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
  const eventOriginRef = useRef<HTMLElement | null>(null);

  const handleResize = () => {
    setPagePositions({
      eventOrigin: {
        top: eventOriginRef.current ? eventOriginRef.current.getBoundingClientRect().top : 0,
        left: eventOriginRef.current ? eventOriginRef.current.getBoundingClientRect().left : 0
      }
    });
  };

  const getCalendar = useCallback(() => {
    xhr({
      method: 'GET',
      url: '/get-calendar'
    }).then((res: ApiResponse) => {
      console.log("get-calendar", res);
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


  return <main id="main" onClick={clickMain}>
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
  </main>
}

export default App;
