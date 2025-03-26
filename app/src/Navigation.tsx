import React, { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { IEvent } from './types';

export default function Navigation({ setEvent }: { setEvent: React.Dispatch<React.SetStateAction<IEvent|null>> }) {
  const [expanding, setExpanding] = useState<boolean>(false);
  const navigate = useNavigate();
	const expandToBudget = useCallback(() => {
    if (expanding) return;
    setEvent(null);
    navigate("/");
    setExpanding(true);
    setTimeout(() => {
      setExpanding(false);
    }, 500);

    const body = document.body;
    const header = document.querySelector('header');

    if (!body.classList.contains('view-left')) {
      body.classList.add('view-left');
    } else {
      body.classList.remove('view-left');
    }
  }, []);
	return <button id="expand-to-budget" onClick={expandToBudget}>☰</button>;
}