export enum TransactionType
{
  Credit = 0,
  Debit = 1,
  Reversal = 2
}

export enum AccountType {
  Savings = 0,
  Current = 1,
  Domiciliary = 2
}

export enum CurrencyType {
  NGN = 0,
  USD = 1,
  EUR = 2,
  GBP = 3
}

export interface CreateTransactionDto
{
  accountNumber: string;
  amount: number;
  transactionType: TransactionType;
  transactionReference: string;
  originalTransactionReference?: string;
}

export interface ProcessingType
{
  type: 'daily' | 'monthly' | 'reversal';
  title: string;
  description: string;
  icon: JSX.Element;
  frequency: string;
}

export interface TransactionAction
{
  type: TransactionType;
  title: string;
  description: string;
  icon: JSX.Element;
}

export interface ApiResponse<T>
{
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

export interface AccountData
{
  email: string;
  accountNumber: string;
  accountType: number;
  currencyType: number;
  branchSolId: string;
  balance: number;
  isDomiciliaryAccount: boolean;
  linkedNigerianAccountNumber?: string;
}






