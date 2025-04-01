import { RequestOptions, ApiResponse, Day, IEvent, User } from './types';
import { jwtDecode } from 'jwt-decode';

export const xhr = async (options: RequestOptions): Promise<any> => {
    const { url, method = 'GET', headers = {}, body } = options;

    const response = await fetch(url, {
        method,
        headers: {
            'Content-Type': 'application/json',
            ...headers,
        },
        credentials: 'include',
        body: body ? JSON.stringify(body) : undefined,
    });

    if (response.status == 401) {
        return {
            error: "Unauthorized"
        } as ApiResponse
    }

    return await response.json() as ApiResponse;
}

export const capitalizeKeys = (obj: any): any => {
    if (Array.isArray(obj)) {
        return obj.map(capitalizeKeys);
    } else if (obj !== null && typeof obj === 'object') {
        return Object.keys(obj).reduce((acc, key) => {
            const newKey = key.charAt(0).toUpperCase() + key.slice(1);
            acc[newKey] = capitalizeKeys(obj[key]);
            return acc;
        }, {} as { [key: string]: any });
    }
    return obj;
};

export const serializeRow = <T>(tr: HTMLElement, cls: new () => T, additionalProperties: any): T => {
    const instance = new cls();
    const keys = Object.keys(instance as {}) as (keyof T)[];
    const additionalPropertyKeys = Object.keys(additionalProperties as {}) as (keyof T)[];
    const result = {} as T;
    let error = false;
    
    for (const key of keys) {
        const input = tr.querySelector<HTMLInputElement>(`[name="${new String(key)}"]`);
        if (input) {
            result[key] = input.value as any;
        }
    }

    for (let prop of additionalPropertyKeys) {
        result[prop] = additionalProperties[prop];
    }

    return result;
};

export const findLastEventOfMonth = (monthIndex: number, month: Day[][], user: User): IEvent => {
    const today = new Date();
    for (let i = month.length - 1; i >= 0; i--) {
        for (let j = month[i].length - 1; j >= 0; j--) {
            let day: Day = month[i][j] as Day;
            if (day.month == monthIndex) {
                if (today.getFullYear() == day.year && day.month - 1 == today.getMonth() && day.date == today.getDate()) {
                    return { total: user.checkingBalance } as IEvent;
                } if (day.events.length) {
                    return day.events[day.events.length - 1] as IEvent;
                }
            }
        }
    }
    return {} as IEvent;
}
