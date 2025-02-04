"use client"

import { useEffect, useState } from 'react';
import { AccountSummary } from '@/components/dashboard/AccountSummary';
import { TransactionDialog } from '@/components/dashboard/TransactionDialog';
import { CreditCard, Wallet, RefreshCw } from 'lucide-react';
import { AccountData, CreateTransactionDto, TransactionAction, TransactionType } from '@/types/dashboard';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import axios from 'axios';
import { toast } from 'react-hot-toast';
import { useAuth } from '@/contexts/AuthContext';

const actionItems: TransactionAction[] = [
  {
    type: TransactionType.Credit,
    title: 'Credit Transaction',
    description: 'Add funds to account',
    icon: <CreditCard className="text-green-500" />
  },
  {
    type: TransactionType.Debit,
    title: 'Debit Transaction',
    description: 'Withdraw or spend',
    icon: <Wallet className="text-red-500" />
  }
];

const reverseActionItems: TransactionAction[] = [
  {
    type: TransactionType.Reversal,
    title: 'Reversal Transaction',
    description: 'Reverse a transaction',
    icon: <CreditCard className="text-green-500" />
  },
];



export default function BankingDashboard()
{
  const [accounts, setAccounts] = useState<AccountData[]>([]);
  const { user: userData } = useAuth();

  useEffect(() =>
  {
    if (userData?.email)
    {
      fetchAccounts();
    }
  }, [userData?.email]);

  const fetchAccounts = async () =>
  {
    try
    {
      const response = await axios.get(
        `https://localhost:7031/api/Customer/${userData?.email}/accounts`,
        {
          headers: {
            Authorization: `Bearer ${localStorage.getItem('token')}`
          }
        }
      );

      if (response.data.success)
      {
        setAccounts(response.data.data);
      }
    } catch (error)
    {
      console.error('Failed to fetch accounts:', error);
      toast.error('Failed to fetch accounts');
    }
  };

  async function handleTransactionSubmit(data: CreateTransactionDto)
  {
    try
    {
      const response = await axios.post(
        'https://localhost:7031/api/AccountTransaction/create',
        data,
        {
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${localStorage.getItem('token')}`
          }
        }
      );


      if (response.data.success)
      {
        // setAccountBalance(response.data.data.newBalance);
        return Promise.resolve();
      } else
      {
        throw new Error(response.data.message);
      }
    } catch (error: any)
    {
      const errorMessage = error.response?.data?.message ||
        error.response?.data?.errors?.[0] ||
        error.message;
      throw error; // Let the TransactionDialog handle the error
    }
  }

  const AdminActions = () =>
  {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Admin Actions</CardTitle>
        </CardHeader>
        <CardContent className="space-y-2">
          {reverseActionItems.map((action, index) => (
            <TransactionDialog
              key={index}
              {...action}
              onSubmit={handleTransactionSubmit}
            />
          ))}
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold flex items-center gap-3">
          <RefreshCw className="h-6 w-6" />
          Dashboard Overview
        </h1>
      </div>

      <div className="grid md:grid-cols-2 gap-6">
        <AccountSummary
          accounts={accounts}
        />

        <Card>
          <CardHeader>
            <CardTitle>Quick Actions</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            {actionItems.map((action, index) => (
              <TransactionDialog
                key={index}
                {...action}
                onSubmit={handleTransactionSubmit}
              />
            ))}
            {userData?.role === 'Admin' && <AdminActions />}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}