'use client'

import { useState, useEffect } from 'react';
import { 
  Table, 
  TableBody, 
  TableCell, 
  TableHead, 
  TableHeader, 
  TableRow 
} from '@/components/ui/table';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { useAuth } from '@/contexts/AuthContext';
import axios from 'axios';
import { toast } from 'react-hot-toast';
import { AccountType, CurrencyType } from '@/types/dashboard';
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from '@/components/ui/accordion';


interface Transaction {
  transactionReference: string;
  amount: number;
  transactionType: string;
  narration: string;
  createdAt: string;
  processedDate: string;
  originalTransactionReference: string;
  isReversed: boolean;
  reversalReference: string | null;
  accountNumber: string | null;
  balance: number | null;
}

interface AccountTransactions {
  accountNumber: string;
  accountName: string | null;
  transactionCount: number;
  dateRange: string | null;
  transactions: Transaction[];
}

interface Account {
  accountNumber: string;
  accountType: number;
  currencyType: number;
  balance: number;
}

export default function TransactionsPage() {
  const [accountsTransactions, setAccountsTransactions] = useState<Map<string, AccountTransactions>>(new Map());
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const { user } = useAuth();

  useEffect(() => {
    const fetchAccounts = async () => {
      try {
        const response = await axios.get(
          `https://localhost:7031/api/Customer/${user?.email}/accounts`,
          {
            headers: {
              Authorization: `Bearer ${localStorage.getItem('token')}`
            }
          }
        );

        if (response.data.success) {
          setAccounts(response.data.data);
          await fetchTransactionsForAccounts(response.data.data);
        }
      } catch (error) {
        toast.error('Failed to fetch accounts');
        setIsLoading(false);
      }
    };

    if (user?.email) {
      fetchAccounts();
    }
  }, [user?.email]);

  const fetchTransactionsForAccounts = async (accounts: Account[]) => {
    const transactionsMap = new Map<string, AccountTransactions>();

    try {
      await Promise.all(accounts.map(async (account) => {
        const response = await axios.get(
          `https://localhost:7031/api/AccountTransaction/account/${account.accountNumber}/all`,
          {
            headers: {
              Authorization: `Bearer ${localStorage.getItem('token')}`
            }
          }
        );

        if (response.data.success) {
          transactionsMap.set(account.accountNumber, response.data.data);
        }
      }));

      setAccountsTransactions(transactionsMap);
    } catch (error) {
      toast.error('Failed to fetch transactions');
    } finally {
      setIsLoading(false);
    }
  };

  const getTransactionBadge = (type: string) => {
    if (type.toLowerCase() === 'credit') {
      return <Badge variant="secondary" className="bg-green-100 text-green-800">CREDIT</Badge>;
    }
    return <Badge variant="secondary" className="bg-red-100 text-red-800">DEBIT</Badge>;
  };

  const getReversalBadge = (isReversed: boolean) => {
    if (isReversed) {
      return <Badge variant="destructive">Reversed</Badge>;
    }
    return null;
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-full">
        <div className="text-center">Loading transactions...</div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <h1 className="text-3xl font-bold">Transaction History</h1>
      
      <Accordion type="single" collapsible className="space-y-4">
        {Array.from(accounts).map((account) => (
          <AccordionItem 
            key={account.accountNumber}
            value={account.accountNumber}
            className="bg-card border rounded-lg"
          >
            <AccordionTrigger className="px-4">
              <div className="flex items-center justify-between w-full">
                <div className="flex items-center gap-4">
                  <Badge variant="outline">
                    {AccountType[account.accountType]}
                  </Badge>
                  <span className="font-semibold">{account.accountNumber}</span>
                  <Badge>
                    {CurrencyType[account.currencyType]}
                  </Badge>
                </div>
                <div className="text-right">
                  <span className="font-medium">
                    Balance: {CurrencyType[account.currencyType]} {account.balance.toLocaleString()}
                  </span>
                </div>
              </div>
            </AccordionTrigger>
            <AccordionContent>
              <Card>
                <CardContent className="pt-4">
                  {accountsTransactions.get(account.accountNumber)?.transactions.length === 0 ? (
                    <div className="text-center py-4 text-muted-foreground">
                      No transactions found for this account
                    </div>
                  ) : (
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead>Reference</TableHead>
                          <TableHead>Type</TableHead>
                          <TableHead>Amount</TableHead>
                          <TableHead>Description</TableHead>
                          <TableHead>Status</TableHead>
                          <TableHead>Date</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {accountsTransactions.get(account.accountNumber)?.transactions.map((transaction) => (
                          <TableRow key={transaction.transactionReference}>
                            <TableCell className="font-mono text-sm">
                              {transaction.transactionReference}
                            </TableCell>
                            <TableCell>
                              {getTransactionBadge(transaction.transactionType)}
                            </TableCell>
                            <TableCell className="font-medium">
                              {CurrencyType[account.currencyType]} {transaction.amount.toLocaleString()}
                            </TableCell>
                            <TableCell>{transaction.narration}</TableCell>
                            <TableCell>
                              {getReversalBadge(transaction.isReversed)}
                            </TableCell>
                            <TableCell className="text-muted-foreground">
                              {new Date(transaction.createdAt).toLocaleString()}
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  )}
                </CardContent>
              </Card>
            </AccordionContent>
          </AccordionItem>
        ))}
      </Accordion>
    </div>
  );
}