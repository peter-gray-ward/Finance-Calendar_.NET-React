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
      body.classList.remove('view-right');
      body.classList.add('view-left');
    } else {
      body.classList.remove('view-left');
    }

    
  }, []);
  const expandToOutlook = useCallback(() => {
    if (expanding) return;
    setEvent(null);
    navigate("/");
    setExpanding(true);
    setTimeout(() => {
      setExpanding(false);
    }, 500);

    const body = document.body;
    const header = document.querySelector('header');

    if (!body.classList.contains('view-right')) {
      body.classList.remove('view-left');
      body.classList.add('view-right');
    } else {
      body.classList.remove('view-right');
    }
  }, []);
	return <>
    <button id="expand-to-budget" onClick={expandToBudget}>☰</button>
    <button id="expand-to-outlook" onClick={expandToOutlook}>☰</button>
  </>
}