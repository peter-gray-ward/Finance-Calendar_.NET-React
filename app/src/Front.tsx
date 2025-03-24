import './App.scss';
import React, { useCallback, useEffect, useState } from 'react';
import App from './App';
import Login from './Login';
import { User } from './types';
import { xhr } from './util';

export default function Front() {
    let [loading, setLoading] = useState(true);
    let [user, setUser] = useState<User|null>(null);
    let [authenticated, setAuthenticated] = useState(false);

    useEffect(() => {
        xhr({
            method: 'GET',
            url: '/get-user'
        }).then(res => {
            console.log("----", res);
            setLoading(false);
            if (!res.error) {
                setUser(res.user);
                setAuthenticated(true);
            }
        });
    }, []);

    

    return (
        loading ? <div id="Front">
            <h1>FINANCE<br/>CALENDAR</h1>
        </div> : (
            user ? <App _user={user!} /> : <Login setUser={setUser} />
        )
    );
}