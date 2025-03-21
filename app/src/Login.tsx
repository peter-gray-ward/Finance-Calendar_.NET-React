import React, { useCallback, useState, useRef } from 'react';
import { xhr } from './util';
import { ApiResponse, User, UserLoginRequest, UserLoginResponse } from './types';
import './App.scss';

function Login({ setUser }: { setUser: React.Dispatch<React.SetStateAction<User|null>> }) {
  const [tab, setTab] = useState('login');
  const [error, setError] = useState<string|null>(null);
  const usernameRef = useRef<HTMLInputElement>(null);
  const passwordRef = useRef<HTMLInputElement>(null);
  const changeTab = useCallback((tab: string) => {
    setTab(tab);
  }, []);
  const request = useCallback(() => {
    if (usernameRef.current && passwordRef.current) {
      const username = usernameRef.current.value;
      const password = passwordRef.current.value;
      if (username && password) {
        xhr({
          method: 'POST',
          url: `/${tab}-user`,
          body: { 
            userName: username, 
            password: password 
          } as UserLoginRequest
        }).then((res: ApiResponse) => {
          if (!res.error) {
            console.log(tab + " successful", res);
            setUser(res.data as User);
          } else {
            console.error(tab + " failed", res);
            setError(res.error);
          }
        })
      }
    }
  }, [usernameRef, passwordRef, tab]);
  return (
    <div id="Login">
      <h1>FINANCE<br/>CALENDAR</h1>
      <div className="Tab-Container">
        <div className="Tabs">
          <button onClick={() => changeTab('login')} className={tab === 'login' ? 'active' : ''}>Login</button>
          <button onClick={() => changeTab('register')} className={tab === 'register' ? 'active' : ''}>Register</button>
        </div>
        <div className="Tab-Content">
          {
            tab == 'login' ? (<>
              <input type="text" placeholder="Username" ref={usernameRef} />
              <input type="password" placeholder="Password" ref={passwordRef} />
              <button onClick={request}>Login</button>
              </>) : (<>
              <input type="text" placeholder="Username" ref={usernameRef} />
              <input type="password" placeholder="Password" ref={passwordRef} />
              <button onClick={request}>Register</button>
            </>)
          }
        </div>
        <p className="error">{ error || null }</p>
      </div>
    </div>
  );
}

export default Login;
