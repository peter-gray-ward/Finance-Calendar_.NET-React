import React, { useState, useCallback, useRef, useEffect } from 'react';
import { useLocation, useParams } from 'react-router-dom';
import { IEvent, Position, User, ApiResponse, Day } from './types';
import { xhr } from './util';

export default function Event({ 
  origin, 
  setEvent,
  setCalendar
}: { 
  origin: Position, 
  setEvent: React.Dispatch<React.SetStateAction<IEvent|null>>
  setCalendar: React.Dispatch<React.SetStateAction<Day[][]>>
}) {
  const { id } = useParams();
  const location = useLocation();
  const { event, origin: initialOrigin, user } = location.state || {};
  const [saved, setSaved] = useState<boolean>(false);
  const [localEvent, setLocalEvent] = useState<IEvent>(event);
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
      body: localEvent
    }).then((res: ApiResponse) => {
      if (res.success) {
        console.log("@", res);
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
      body: localEvent
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

  }, []);
  const deleteAllTheseEvents = useCallback(() => {

  }, []);

  if (!origin.left && !origin.top) return null;

  console.log(event.summary, localEvent.summary);

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
              type="number" id="event-amount" name="amount" 
              value={localEvent.amount}
              onChange={(e) => setLocalEvent({ ...localEvent, amount: +e.target.value })} />
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