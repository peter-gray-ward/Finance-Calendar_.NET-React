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
    error?: string;
    message?: string;
    data?: any;
}

export interface UserLoginResponse {
    user: User,
    token: string;
}

export interface ModalItem {
    label: string;
    route: string;
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