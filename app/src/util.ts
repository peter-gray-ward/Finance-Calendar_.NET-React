import { RequestOptions, ApiResponse } from './types';
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
        };
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