import React, { useCallback, useRef } from 'react';
import { IEvent, Position, User } from './types';

export default function Event({ 
	user, 
	event,
	origin 
}: { 
	user: User, 
	event: IEvent,
	origin: Position
}) {
  const eventRef = useRef<HTMLDivElement|null>(null);
  const saveThisEvent = useCallback(() => {

  }, []);
  const saveAllTheseEvents = useCallback(() => {

  }, []);
  const deleteThisEvent = useCallback(() => {

  }, []);
  const deleteAllTheseEvents = useCallback(() => {

  }, []);
	return <div className="modal" style={{ left: origin.left + 'px', top: origin.top + 'px' }}>
    <div className='id modal-content' id="event-edit">
      <div id="modal-event" ref={eventRef}>
        <input disabled className="hidden" name="id" value={event.id} />
        <input disabled className="hidden" name="recurrenceId" value={event.recurrenceId} />
        <div className="upper-div-collection">
          <div id="summary-div">
            <label>Summary</label>
            <input type="text" className="this focusable"
              value={event.summary} 
              name="summary" />
          </div>
          <div id="frequency-container">
            <label>Frequency</label>
            <div className="td select-container focusable">
              <select value={event.frequency}>
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
            <input className="this focusable" type="number" id="event-amount" name="amount" 
              value={event.amount} />
          </div>
          
          <div id="date-container">
            <div>
              <label>Date</label>
              <input name="date" className="td focusable" type="date" 
                value={event.date} />
            </div>
            <div>
              <label>End Date</label>
              <input name="recurrenceEndDate" className="td focusable" type="date" 
                value={event.recurrenceEndDate} />
            </div>
          </div>
        </div>
      </div>
      <div className="button-footer">
        <button id="save-this-event" className="focusable" onClick={saveThisEvent}>save</button>
        <button id="save-this-and-future-events" className="focusable" onClick={saveAllTheseEvents}>save all</button>
        <button id="clude-this-event" className="focusable">{ event.exclude ? 'exclude' : 'include' }</button>
        <button id="clude-all-these-events" className="focusable">{ event.exclude ? 'exclude' : 'include' } all</button>
        <button id="delete-this-event" className="focusable" onClick={deleteThisEvent}>delete</button>
        <button id="delete-all-these-events" className="focusable" onClick={deleteAllTheseEvents}>delete all</button>
      </div>
    </div>
	</div> 
}