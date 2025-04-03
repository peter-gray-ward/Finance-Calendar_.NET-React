import React, { useState, useCallback, useRef, useEffect, useMemo } from 'react';
import { useLocation, useParams, useNavigate } from 'react-router-dom';
import { Event, IEvent, Position, User, ApiResponse, Day, Debt } from './types';
import { xhr } from './util';

export default function EventModal({ 
  origin, 
  setEvent,
  setCalendar
}: { 
  origin: Position, 
  setEvent: React.Dispatch<React.SetStateAction<IEvent|null>>
  setCalendar: React.Dispatch<React.SetStateAction<Day[][]>>
}) {
  const { id } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const { event, origin: initialOrigin, user } = location.state || {};
  const [saved, setSaved] = useState<boolean>(false);
  const [localEvent, setLocalEvent] = useState<IEvent>(event);
  const validatedEvent = useMemo(() => {
    return {
      ...localEvent,
      amount: (localEvent.amount == '' || Number.isNaN(localEvent.amount) ? 0.0 : +localEvent.amount) 
    };
  }, [localEvent]);
  useEffect(() => {
    if (event) {
      setLocalEvent(event);
    }
  }, [event]);
  useEffect(() => {
    const waitForElement = () => {
      const calendarEvent = document.getElementById(`event-${event.id}`);
      if (calendarEvent) {
        setEvent(event);
        setLocalEvent(event);
      } else {
        window.requestAnimationFrame(waitForElement);
      }
    }
    waitForElement();
  }, []);
  const eventRef = useRef<HTMLDivElement|null>(null);
  const saveThisEvent = useCallback(() => {
    xhr({
      method: 'PUT',
      url: '/save-event',
      body: validatedEvent
    }).then((res: ApiResponse) => {
      if (res.success) {
        setCalendar(res.data as Day[][]);
        setSaved(true)
        setTimeout(() => {
          setSaved(false);
        }, 740);
      }
    });
  }, [localEvent]);
  const saveAllTheseEvents = useCallback(() => {
    xhr({
      method: 'PUT',
      url: '/save-event?all=true',
      body: validatedEvent
    }).then((res: ApiResponse) => {
      if (res.success) {
        setCalendar(res.data as Day[][]);
        setSaved(true)
        setTimeout(() => {
          setSaved(false);
        }, 740);
      }
    });
  }, [localEvent]);
  const deleteThisEvent = useCallback(() => {
    xhr({
      method: 'DELETE',
      url: '/delete-event?id=' + localEvent.id
    }).then((res: ApiResponse) => {
      if (res.success) {
        setCalendar(res.data as Day[][]);
        navigate("/");
      }
    });
  }, []);
  const deleteAllTheseEvents = useCallback(() => {
    xhr({
      method: 'DELETE',
      url: '/delete-event?recurrenceId=' + localEvent.recurrenceId
    }).then((res: ApiResponse) => {
      if (res.success) {
        setCalendar(res.data as Day[][]);
        navigate("/");
      }
    });
  }, []);

  if (!origin.left && !origin.top) return null;

	return <div className="modal" 
    style={{
      left: origin.left + 'px', 
      top: origin.top + 'px',
      border: saved ? '2px solid lawngreen' : 'none'
    }}>
    <div className='id modal-content' id="event-edit">
      <div id="modal-event" ref={eventRef}>
        <input disabled className="hidden" name="id" 
          value={localEvent.id} />
        <input disabled className="hidden" 
          name="recurrenceId" 
          value={localEvent.recurrenceId}/>
        <div className="upper-div-collection">
          <div id="summary-div">
            <label>Summary</label>
            <textarea className="this focusable"
              value={localEvent.summary} 
              name="summary"
              onChange={(e) => setLocalEvent({ ...localEvent, summary: e.target.value })}  />
          </div>
          <div id="debt-id">
            <label>Debt Id</label>
            <select name="debtId" value={localEvent.debtId ?? ""} onChange={(e) => setLocalEvent({ ...localEvent, debtId: e.target.value.length ? e.target.value : null })}>
              <option value="">None</option>
              {
                user.account.debts.map((debt: Debt) => <option key={debt.id} value={debt.id}>{debt.name}</option>)
              }
            </select>
          </div>
          <div id="frequency-container">
            <label>Frequency</label>
            <div className="td select-container focusable">
              <select value={localEvent.frequency}
                onChange={(e) => setLocalEvent({ ...localEvent, frequency: e.target.value })}>
                <option value="monthly">Monthly</option>
                <option value="weekly">Weekly</option>
                <option value="biweekly">BiWeekly</option>
                <option value="daily">Daily</option>
              </select>
            </div>
          </div>
        </div>
        <div id="time-container">
          <div id="amount-div">
            <label>Amount</label>
            <input className={`this focusable ${localEvent.amount < 0 ? 'negative' : 'positive'}`} 
              type="text" id="event-amount" name="amount"
              data-type="number"
              value={localEvent.amount}
              onChange={(e) => setLocalEvent({ ...localEvent, amount: e.target.value })} />
          </div>
          
          <div id="date-container">
            <div>
              <label>Date</label>
              <input name="date" className="td focusable" type="date" 
                value={localEvent.date.split("T")[0]}
                onChange={(e) => setLocalEvent({ ...localEvent, date: e.target.value })} />
            </div>
            <div>
              <label>End Date</label>
              <input name="recurrenceEndDate" className="td focusable" type="date" 
                value={localEvent.recurrenceEndDate.split("T")[0]}
                onChange={(e) => setLocalEvent({ ...localEvent, recurrenceEndDate: e.target.value })} />
            </div>
          </div>
        </div>
      </div>
      <div className="button-footer">
        <button id="save-this-event" className="focusable" onClick={saveThisEvent}>save</button>
        <button id="save-this-and-future-events" className="focusable" onClick={saveAllTheseEvents}>save all</button>
        <button id="clude-this-event" className="focusable">{ localEvent.exclude ? 'exclude' : 'include' }</button>
        <button id="clude-all-these-events" className="focusable">{ localEvent.exclude ? 'exclude' : 'include' } all</button>
        <button id="delete-this-event" className="focusable" onClick={deleteThisEvent}>delete</button>
        <button id="delete-all-these-events" className="focusable" onClick={deleteAllTheseEvents}>delete all</button>
      </div>
    </div>
	</div> 
}