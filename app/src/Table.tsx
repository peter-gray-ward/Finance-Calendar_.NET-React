import React, { useState, useMemo, useCallback, useRef, ChangeEvent } from 'react';
import { Class, User, TableObject, ApiResponse } from './types';
import { xhr, serializeRow } from './util';

export default function Table<T extends TableObject>({
  cls,
  user,
  title,
  columns,
  order,
  data,
  onAdd,
  onDelete,
  localUpdate
}: {
  cls: Class<T>,
  title: string,
  columns: string[],
  order: string[],
  data: T[],
  user: User,
  onAdd: any,
  onDelete: any,
  localUpdate: any
}) {
  const [successfulUpdates, setSuccessfulUpdates] = useState<string[]>([]);

  const selects: { [key: string]: string[] } = useMemo(() => ({
    'frequency': ['daily','weekly','biweekly','monthly','trimonthly'],
    'interestType': ['compound', 'simple']
  }), []);

  const addT = useCallback(() => {
    xhr({
      method: 'POST',
      url: `/add-${title.toLowerCase()}`
    }).then(onAdd);
  }, [user]);

  const deleteT = useCallback((id: string) => {
    xhr({
      method: 'DELETE',
      url: `/delete-${title.toLowerCase()}/${id}`
    }).then(onDelete);
  }, [user]);

  const updateTDebounceRef = useRef<NodeJS.Timeout | null>(null);

  const updateT = useCallback((event: ChangeEvent<HTMLInputElement|HTMLSelectElement>) => {
    if (updateTDebounceRef.current) clearTimeout(updateTDebounceRef.current);

    let el = event.target as HTMLElement;
    while (el && !el.classList.contains('tr')) el = el.parentElement as HTMLElement;

    const t: T = serializeRow<T>(el, cls, { userId: user.id });

    localUpdate(t as T);

    if (t.amount && t.amount.trim() == "") return;
    if (t.balance && t.balance.trim() == "") return;
    if (t.interest && t.interest.trim() == "") return;

    updateTDebounceRef.current = setTimeout(() => {
      xhr({
        method: 'PUT',
        url: `/update-${title.toLowerCase()}`,
        body: t
      }).then((res: ApiResponse) => {
        if (res.success) {
          setSuccessfulUpdates(Array.from(new Set([
            ...successfulUpdates,
            (res.data as T).id
          ])));
          setTimeout(() => {
            setSuccessfulUpdates(successfulUpdates.filter(id => id !== t.id));
          }, 1000);
        }
      });
    }, 750)
  }, [user]);

  return <article>
    <h2>
        <div>{title}</div>
      </h2>
      <div className="table" id={`table-${title.toLowerCase()}`}>
        <div className="tr header">
          {
            columns.map((th: string) => <div key={th} className="th">{th}</div>)
          }
        </div>
        {
          data.map((datum: T, i: number) => {
            const values = order.map((key: string, index: number) => {
              let result;
              let val: any = datum[key];

              switch (typeof val) {
                case "string":
                  if (selects[key]) {
                    if (!val) val = selects[0];
                    result = <div key={i + '.' + index} className="td select-container">
                        <select required name={key} value={val} onChange={updateT}>
                        {
                          selects[key].map((option: string) => <option key={i + '.' + index + '.' + option} value={option}>{option}</option>)
                        }
                      </select>
                    </div>;
                  } else if (/date/i.test(key)) {
                    result = <input key={i + '.' + index} name={key} className="td" type="date" value={val.split('T')[0]} onChange={updateT} />
                  } else if (/link|href/i.test(key)) {
                    result = <div key={i + '.' + index} className="td">
                      <a href={val} target="_blank">🔗</a>
                      <input name={key} type="text" value={val} onChange={updateT} />
                    </div>
                  } else {
                    result = <input key={i + '.' + index} name={key} className="td" type="text" value={val} onChange={updateT} />
                  }
                  break;
                case "number":
                    result = <input key={i + '.' + index} name={key} className="td" type="number" value={val || val == 0.0 ? val : 0.0} onChange={updateT} />;
                    break;
              }
              return result;
            });

            return <div key={datum.id} 
              id={ datum.id } 
              className={`tr data ${successfulUpdates.includes(datum.id) ? ' updated' : ''}`}>
              
              {
                values.map(value => value)
              }

              <input disabled name="id" className="hidden" value={datum.id} />
              
              <button className="delete-t" onClick={() => deleteT(datum.id)}>-</button>
            </div>
          })
          
        }
      </div>
      <div className="tr button-row-right">
        <button className="add-expense" onClick={addT}>+</button>
        {/*<button id="refresh-calendar" className={refreshingCalendar ? 'refreshing' : ''} onClick={refreshCalendar}>↺</button>*/}
      </div>
  </article>
}