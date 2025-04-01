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

    if (body.classList.contains('view-left-right')) {
      body.classList.remove('view-left-right');
      body.classList.add('view-right-main');
    } else if (body.classList.contains('view-right-main')) {
      body.classList.add('view-left-right');
      body.classList.remove('view-right-main');
    } else if (body.classList.contains('view-left-main')) {
      body.classList.remove('view-left-main')
    } else {
      body.classList.remove('view-right-main');
      body.classList.add('view-left-main');
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

    if (body.classList.contains('view-left-right')) {
      body.classList.remove('view-left-right');
      body.classList.add('view-left-main');
    } else if (body.classList.contains('view-left-main')) {
      body.classList.add('view-left-right');
      body.classList.remove('view-left-main');
    } else if (body.classList.contains('view-right-main')) {
      body.classList.remove('view-right-main');
    } else {
      body.classList.remove('view-left-main');
      body.classList.add('view-right-main');
    }
  }, []);
	return <>
    <button id="expand-to-budget" onClick={expandToBudget}>☰</button>
    <button id="expand-to-outlook" onClick={expandToOutlook}>☰</button>
  </>
}