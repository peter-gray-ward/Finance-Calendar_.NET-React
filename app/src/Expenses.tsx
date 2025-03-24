import React, { useCallback, useState, ChangeEvent, useRef } from 'react';
import { User, IExpense, Expense, ApiResponse } from './types';
import { xhr, serializeRow } from './util';

export default function Expenses({ user, setUser }: { user: User, setUser: React.Dispatch<React.SetStateAction<User>> }) {
  const [successfulUpdates, setSuccessfulUpdates] = useState<string[]>([]);

  const addExpense = useCallback(() => {
    xhr({
      method: 'POST',
      url: '/add-expense'
    }).then((res: ApiResponse) => {
      if (!res.error) {
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
    });
  }, [user]);

  const refreshCalendar = useCallback(() => {
    xhr({
      method: 'POST',
      url: '/refresh-calendar'
    }).then((res: ApiResponse) => {
      if (!res.error) {

      }
    });
  }, [user]);

  const updateExpenseDebounceRef = useRef<NodeJS.Timeout | null>(null);

  const updateExpense = useCallback((event: ChangeEvent<HTMLInputElement|HTMLSelectElement>) => {
    if (updateExpenseDebounceRef.current) clearTimeout(updateExpenseDebounceRef.current);

    let el = event.target as HTMLElement;
    while (el && !el.classList.contains('tr')) el = el.parentElement as HTMLElement;
    const expense = serializeRow<Expense>(el, Expense, { userId: user.id } as Expense);
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

    updateExpenseDebounceRef.current = setTimeout(() => {
      xhr({
        method: 'PUT',
        url: '/update-expense',
        body: expense
      }).then((res: ApiResponse) => {
        if (!res.error) {
          setSuccessfulUpdates(Array.from(new Set([
            ...successfulUpdates,
            (res.data as Expense).id
          ])));
          setTimeout(() => {
            setSuccessfulUpdates(successfulUpdates.filter(id => id !== expense.id));
          }, 1000);
        }
      });
    }, 750)
  }, [user]);

  const deleteExpense = useCallback((expenseId: string) => {
    xhr({
      method: 'DELETE',
      url: '/delete-expense/' + expenseId
    }).then((res: ApiResponse) => {
      if (!res.error) {
        setUser({
          ...user,
          account: {
            ...user.account,
            expenses: user.account.expenses.filter((e: Expense) => e.id !== expenseId)
          }
        });
      }
    });
  }, [user]);

	return <article id="Expenses">
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
          user.account.expenses.map((expense: Expense) => (
            <div className={`tr data id expenses${successfulUpdates.includes(expense.id) ? ' updated' : ''}`} id={ expense.id } key={expense.id}>
              <input disabled name="id" className="hidden" value={expense.id} />  

              <input name="name" className="td" type="text" value={expense.name} onChange={updateExpense} />
              
              <div className="td select-container td">
                <select name="frequency" 
                     className="select"
                     value={expense.frequency} onChange={updateExpense}>
                  <option value="monthly">monthly</option>
                  <option value="weekly">weekly</option>
                  <option value="biweekly">biweekly</option>
                  <option value="daily">daily</option>
                </select>

              </div>
              
              <input name="amount" className="td" type="number" value={expense.amount} onChange={updateExpense} />
              
              <input name="startDate" className="td" type="date" value={expense.startDate.split('T')[0]} onChange={updateExpense} />
              
              <input name="recurrenceEndDate" className="td" type="date" value={expense.recurrenceEndDate.split('T')[0]} onChange={updateExpense} />

              <button className="delete-expense" onClick={() => deleteExpense(expense.id)}>-</button>
            </div>
          ))
        }
      </div>
      <div className="tr button-row-right">
        <button className="add-expense" onClick={addExpense}>+</button>
        <button id="refresh-calendar" onClick={refreshCalendar}>↺</button>
      </div>
  </article>
}