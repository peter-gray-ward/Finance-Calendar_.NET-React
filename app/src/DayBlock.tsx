import React, { useCallback, MouseEvent, useRef } from 'react';
import { User, Day, IEvent, Position, ApiResponse } from './types';
import { xhr } from './util';
import { useNavigate } from 'react-router-dom';

export default function DayBlock(
  { user, day, a, b, setEventOrigin, setEvent, setCalendar, setUser }: 
  { user: User, day: Day, a: number, b: number, 
    setEventOrigin: React.Dispatch<React.SetStateAction<Position>>,
    setEvent: React.Dispatch<React.SetStateAction<IEvent|null>>,
    setCalendar: React.Dispatch<React.SetStateAction<Day[][]>>,
    setUser: React.Dispatch<React.SetStateAction<User>>
  }) {
  const checkingBalanceRef = useRef<HTMLInputElement | null>(null);
  const navigate = useNavigate();
  const selectEvent = useCallback((e: MouseEvent, event: IEvent) => {
    e.stopPropagation();
    const x = e.clientX + 400 > window.innerWidth ? window.innerWidth - 410 : e.clientX;
    const y = e.clientY + 200 > window.innerHeight ? window.innerHeight - 210 : e.clientY;
    const eventOrigin: Position = { left: x, top: y } as Position;
    setEventOrigin(eventOrigin);
    setEvent(event);
    console.log(`/event/${event.id}`)
    navigate(`/event/${event.id}`, {
      state: {
        event,
        origin: eventOrigin,
        user,
      },
    });
  }, []);
  const blurEvent = useCallback((event: MouseEvent) => {
    setEvent(null);
    navigate('/');
  }, []);
  const updateCheckingBalanceDebounceRef = useRef<NodeJS.Timeout | null>(null);
  const updateCheckingBalance = useCallback(() => {
    if (updateCheckingBalanceDebounceRef.current !== null) {
      clearTimeout(updateCheckingBalanceDebounceRef.current);
    }
    const checkingBalance = checkingBalanceRef.current!.value;
    setUser({
      ...user,
      checkingBalance
    });
    updateCheckingBalanceDebounceRef.current = setTimeout(() => {
      xhr({
        method: 'POST',
        url: '/update-checking-balance',
        body: checkingBalance
      }).then((res: ApiResponse) => {
        if (!res.error) {
          setCalendar(res.data);
        }
      });
    }, 888);
  }, []);

	return <div className="day-block" key={`${a}.${b}`} onClick={blurEvent}>
    <div className="day-header">
      {
        day.isTodayOrLater ?
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
        : null
      }
      <div className={`day-date${day.isToday ? ' today' : ''}`}>
        { day.date }
      </div> 
    </div>
    <div className="events">
      {
        day.events.map((event: IEvent, c: number) => (
          <div className="event" id={`event-${ event.id }`} key={`${a}.${b}.${c}`}
              onClick={(e) => selectEvent(e, event)}>
            <span className={`${event.amount >= 0 ? 'positive' : 'negative'}`}>•</span> 
            <span className="summary">{ event.summary.replace('&nbsp;', '').replace('   ', '') }</span> 
            <span className={`${event.amount >= 0 ? 'positive' : 'negative'}`}>
              { event.amount }
            </span>
          </div>
        ))
      }
    </div>
  </div>
}