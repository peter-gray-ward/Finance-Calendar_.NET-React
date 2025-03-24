import React from 'react';
import { User } from './types';

export default function Debts({ user }: { user: User }) {
	return <article id="Debts">
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
	</article>
}