import { v4 as uuidv4 } from 'uuid';

export type Class<T> = new () => T;

export interface RequestOptions {
    url: string;
    method?: string;
    headers?: Record<string, string>;
    body?: any;
}

export interface UserLoginRequest {
    userName: string;
    password: string;
}

export interface User {
    id: string;
    userName: string;
    password: string;
    profileThumbnailBase64?: string;
    [key: string]: any;
}

export interface ApiResponse {
    success?: boolean;
    message?: string;
    data?: any;
    user?: User;
}

export interface UserLoginResponse {
    user: User,
    token: string;
}

export interface ModalItem {
    label: string;
    route: string;
}

export interface Day {
    date: number;
    name: string;
    events: IEvent[];
    debts: DebtRecord[];
    year: number;
    month: number;
    isTodayOrLater: boolean;
    isToday: boolean;
    total: number;
    dow: number;
}

export interface IEvent {
    id: string;
    recurrenceId: string;
    summary: string;
    date: string;
    recurrenceEndDate: string;
    amount: number|string;
    total: number;
    balance: number;
    exclude: boolean;
    frequency: string;
    userId: string;
    debtId?: string|null;
}

export class Event {
    id: string = uuidv4();
    recurrenceId: string = uuidv4();
    summary: string = '';
    date: string;
    recurrenceEndDate: string;
    amount: number = 0.0;
    total: number = 0.0;
    balance: number = 0.0;
    exclude: boolean = false;
    frequency: string = 'monthly';
    userId: string;
    debtId?: string|null;

    constructor(day: Day, userId: string) {
        const date = `${day.year}-${day.month < 10 ? '0' : ''}${day.month}-${day.date < 10 ? '0' : ''}${day.date}`;
        this.date = date;
        this.recurrenceEndDate = date;
        this.userId = userId;
    }
}

export interface Position {
    left: number;
    top: number;
}

export interface IExpense {
    id: string;
    name: string;
    amount: number;
    startDate: string;
    recurrenceEndDate: string;
    frequency: string;
    userId: string;
}

export class Expense {
    id: string = '';
    name: string = '';
    amount: string = '';
    startDate: string = new Date().toISOString().split('T')[0];
    recurrenceEndDate: string = new Date().toISOString().split('T')[0];
    frequency: string = 'monthly';
    userId: string = '';
}

export class Debt {
    id: string =  '';
    name: string = '';
    balance: number =  0.0;
    date: string = new Date().toISOString().split('T')[0];
    interest: number = 0.0;
    interestType: string = 'compound';
    userId: string = '';
    link: string = '';
}

export interface DebtRecord {
    name: string;
    balance: number;
}

export interface ApiResponse {
    error?: string;
    message?: string;
    data?: any;
}

export interface TableObject {
    id: string;
    [key: string]: any;
}

export const DOW: string[] = [
    "Monday","Tuesday",
    "Wednesday","Thursday","Friday",
    "Saturday", "Sunday"
];

export const MONTHNAMES: string[] = [
    "January","February","March","April",
    "May","June","July","August",
    "September","October","November","December"
];