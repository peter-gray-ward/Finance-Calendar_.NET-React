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
    year: number;
    month: number;
    isTodayOrLater: boolean;
    isToday: boolean;
    total: number;
}

export interface IEvent {
    id: string;
    recurrenceId: string;
    summary: string;
    date: string;
    recurrenceEndDate: string;
    amount: number;
    total: number;
    balance: number;
    exclude: boolean;
    frequency: string;
    userId: string;
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

export interface ApiResponse {
    error?: string;
    message?: string;
    data?: any;
}

export const ChatModalItems: ModalItem[] = [
    {
        label: 'Profile',
        route: '/profile'
    }
];

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