export interface Customer
{
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    phoneNumber: string;
    preferredLanguage: string;
    isSMSAlertEnabled: boolean;
    isBlacklisted: boolean;
    blacklistReason: string | null;
    lastTransactionDate: string;
    role: 'Admin' | 'Customer';
}

export interface AuthResponse
{
    success: boolean;
    message: string;
    length: number | null;
    data: {
        token: string;
        customer: Customer;
    };
    errors: string[];
}