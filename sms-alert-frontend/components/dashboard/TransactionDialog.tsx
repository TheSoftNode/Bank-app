import { useState } from 'react';
import { useTransactionForm } from '@/hooks/useTransactionForm';
import { TransactionType, TransactionAction, CreateTransactionDto } from '@/types/dashboard';
import { toast } from 'react-hot-toast';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '../ui/dialog';
import { Button } from '../ui/button';
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from '../ui/form';
import { Input } from '../ui/input';
import { Form } from '@/components/ui/form';

interface TransactionDialogProps extends TransactionAction
{
  onSubmit: (data: CreateTransactionDto) => Promise<void>;
}

export const TransactionDialog = ({
  type,
  title,
  description,
  icon,
  onSubmit
}: TransactionDialogProps) =>
{
  const [isOpen, setIsOpen] = useState(false);
  const form = useTransactionForm(type);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorResponse, setErrorResponse] = useState<string | null>(null);

  const handleSubmit = async (data: CreateTransactionDto) =>
  {
    console.log(data);
    setIsSubmitting(true);
    try
    {
      await onSubmit(data);
      setIsOpen(false);
      form.reset();
      toast.success('Transaction processed successfully');
    } catch (error: any)
    {
      const errorMessage = error.response?.data?.message ||
        (error.response?.data?.errors &&
          Object.values(error.response.data.errors).flat()[0]) ||
        'Failed to process transaction';

      setErrorResponse(error.response?.data?.errors[0]);

      console.log(error.response?.data);

      toast.error(errorMessage);
    } finally
    {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogTrigger asChild>
        <div className="flex items-center justify-between p-3 border rounded-lg hover:bg-accent transition cursor-pointer">
          <div className="flex items-center space-x-4">
            {icon}
            <div>
              <div className="font-semibold">{title}</div>
              <div className="text-sm text-muted-foreground">
                {description}
              </div>
            </div>
          </div>
          <Button variant="outline">{TransactionType[type]}</Button>
        </div>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>
            Enter the details for your {TransactionType[type]} transaction
          </DialogDescription>
        </DialogHeader>
        <Form {...form}>
          <form onSubmit={form.handleSubmit(handleSubmit)}>
            <div className="space-y-4">
              <FormField
                control={form.control}
                name="accountNumber"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Account Number</FormLabel>
                    <FormControl>
                      <Input {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="amount"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Amount</FormLabel>
                    <FormControl>
                      <Input
                        type="number"
                        {...field}
                        onChange={e => field.onChange(Number(e.target.value))}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <span className="text-red-500 pt-2">
                {errorResponse ?? ""}
              </span>
              <FormField
                control={form.control}
                name="transactionReference"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Transaction Reference</FormLabel>
                    <FormControl>
                      <Input {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              {type === TransactionType.Reversal && (
                <FormField
                  control={form.control}
                  name="originalTransactionReference"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Original Transaction Reference</FormLabel>
                      <FormControl>
                        <Input {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              )}
            </div>
            <DialogFooter className="mt-4">
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? 'Processing...' : 'Submit Transaction'}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
};
