import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import * as z from 'zod';
import { CreateTransactionDto, TransactionType } from '@/types/dashboard';

const transactionSchema = z.object({
  accountNumber: z.string().min(5, "Account number must be at least 5 characters"),
  amount: z.number().positive("Amount must be greater than 0"),
  transactionType: z.nativeEnum(TransactionType),
  transactionReference: z.string(),
  originalTransactionReference: z.string().optional()
});

export const useTransactionForm = (initialType: TransactionType) => {
  const form = useForm<CreateTransactionDto>({
    resolver: zodResolver(transactionSchema),
    defaultValues: {
      accountNumber: '',
      amount: 0,
      transactionType: initialType,
      transactionReference: `TXN_${Date.now()}`,
      originalTransactionReference: ''
    }
  });

  return form;
};

