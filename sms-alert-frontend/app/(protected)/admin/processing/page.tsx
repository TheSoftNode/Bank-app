'use client';

import { useState } from 'react';
import
{
  Clock,
  Calendar,
  Repeat2,
  ShieldAlert,
  Building2,
  AlertCircle
} from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Input } from '@/components/ui/input';
import { toast } from 'react-hot-toast';
import axios from 'axios';
import { format } from 'date-fns';

interface ProcessingResult
{
  processedSMSAlerts: number;
  processedQBERequests: number;
  processingDate: string;
}

interface QBEProcessingResult
{
  processingDate: string;
  totalAccounts: number;
  processedCount: number;
  queuedForRetryCount: number;
  totalChargesAmount: number;
  dateRange: {
    startDate: string;
    endDate: string;
  };
}

interface TelcoSettlementResult
{
  settlementDate: string;
  providersProcessed: number;
  totalSettlementAmount: number;
}

interface ReconciliationResult
{
  processedDate: string;
  transactionsProcessed: number;
  consolidatedAmount: number;
  consolidatedAccounts: number;
}

interface MonthEndProcessingResult
{
  processingDate: string;
  accountsProcessed: number;
  transactionsConsolidated: number;
  totalConsolidatedAmount: number;
}

interface ApiResponse<T>
{
  success: boolean;
  message: string;
  length?: number;
  data: T;
  errors?: string[];
}

export default function AdminProcessingPage()
{
  const [isProcessing, setIsProcessing] = useState<Record<string, boolean>>({});
  const [lastProcessed, setLastProcessed] = useState<Record<string, string>>({});
  const [selectedDates, setSelectedDates] = useState({
    startDate: '',
    endDate: ''
  });

  const handleDailyProcessing = async () =>
  {
    setIsProcessing(prev => ({ ...prev, daily: true }));
    try
    {
      const response = await axios.post<ApiResponse<ProcessingResult>>(
        'https://localhost:7031/api/BatchProcessing/process-daily-charges',
        {},
        {
          headers: {
            Authorization: `Bearer ${localStorage.getItem('token')}`
          }
        }
      );

      if (response.data.success)
      {
        toast.success(response.data.message);
        setLastProcessed(prev => ({
          ...prev,
          daily: response.data.data.processingDate
        }));
      }
    } catch (error: any)
    {
      toast.error(error.response?.data?.message || 'Processing failed');
    } finally
    {
      setIsProcessing(prev => ({ ...prev, daily: false }));
    }
  };

  const handleMonthlyQBE = async () =>
  {
    if (!selectedDates.startDate || !selectedDates.endDate)
    {
      toast.error('Please select date range');
      return;
    }

    setIsProcessing(prev => ({ ...prev, monthly: true }));
    try
    {
      const response = await axios.post<ApiResponse<QBEProcessingResult>>(
        'https://localhost:7031/api/BatchProcessing/process-monthly-qbe',
        selectedDates,
        {
          headers: {
            Authorization: `Bearer ${localStorage.getItem('token')}`
          }
        }
      );

      if (response.data.success)
      {
        toast.success(response.data.message);
        setLastProcessed(prev => ({
          ...prev,
          monthly: response.data.data.processingDate
        }));
      }
    } catch (error: any)
    {
      toast.error(error.response?.data?.message || 'Processing failed');
    } finally
    {
      setIsProcessing(prev => ({ ...prev, monthly: false }));
    }
  };


  const handleTelcoSettlements = async () =>
  {
    setIsProcessing(prev => ({ ...prev, telco: true }));
    try
    {
      const response = await axios.post<ApiResponse<TelcoSettlementResult>>(
        'https://localhost:7031/api/BatchProcessing/process-telco-settlements',
        {},
        {
          headers: {
            Authorization: `Bearer ${localStorage.getItem('token')}`
          }
        }
      );

      if (response.data.success)
      {
        toast.success(response.data.message);
        setLastProcessed(prev => ({
          ...prev,
          telco: response.data.data.settlementDate
        }));
      }
    } catch (error: any)
    {
      toast.error(error.response?.data?.message || 'Failed to process telco settlements');
    } finally
    {
      setIsProcessing(prev => ({ ...prev, telco: false }));
    }
  };

  const handleReconcileFailedTransactions = async () =>
  {
    if (!selectedDates.startDate)
    {
      toast.error('Please select a date for reconciliation');
      return;
    }

    setIsProcessing(prev => ({ ...prev, reconcile: true }));
    try
    {
      const response = await axios.post<ApiResponse<ReconciliationResult>>(
        'https://localhost:7031/api/BatchProcessing/reconcile-failed-transactions',
        {
          date: selectedDates.startDate
        },
        {
          headers: {
            Authorization: `Bearer ${localStorage.getItem('token')}`
          }
        }
      );

      if (response.data.success)
      {
        toast.success(response.data.message);
        setLastProcessed(prev => ({
          ...prev,
          reconcile: response.data.data.processedDate
        }));
      }
    } catch (error: any)
    {
      toast.error(error.response?.data?.message || 'Failed to reconcile transactions');
    } finally
    {
      setIsProcessing(prev => ({ ...prev, reconcile: false }));
    }
  };

  const handleMonthEndProcessing = async () =>
  {
    if (!selectedDates.endDate)
    {
      toast.error('Please select month end date');
      return;
    }

    setIsProcessing(prev => ({ ...prev, monthEnd: true }));
    try
    {
      const response = await axios.post<ApiResponse<MonthEndProcessingResult>>(
        'https://localhost:7031/api/BatchProcessing/process-month-end',
        {
          monthEndDate: selectedDates.endDate
        },
        {
          headers: {
            Authorization: `Bearer ${localStorage.getItem('token')}`
          }
        }
      );

      if (response.data.success)
      {
        toast.success(response.data.message);
        setLastProcessed(prev => ({
          ...prev,
          monthEnd: response.data.data.processingDate
        }));
      }
    } catch (error: any)
    {
      toast.error(error.response?.data?.message || 'Failed to process month end');
    } finally
    {
      setIsProcessing(prev => ({ ...prev, monthEnd: false }));
    }
  };

  return (
    <div className="space-y-6 p-6">
      <div className="space-y-6 p-6">
        <div className="flex items-center justify-between">
          <h1 className="text-3xl font-bold flex items-center gap-3">
            <ShieldAlert className="h-6 w-6 text-teal-600" />
            System Processing
          </h1>
        </div>

        <Tabs defaultValue="daily" className="space-y-6">
          <TabsList className="grid grid-cols-4 w-full">
            <TabsTrigger value="daily">Daily Processing</TabsTrigger>
            <TabsTrigger value="monthly">Monthly Processing</TabsTrigger>
            <TabsTrigger value="settlements">Settlements</TabsTrigger>
            <TabsTrigger value="reconcile">Reconciliation</TabsTrigger>
            {/* <TabsTrigger value="month-end">Month End</TabsTrigger> */}
          </TabsList>

          {/* Daily Processing Tab */}
          <TabsContent value="daily">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Clock className="h-5 w-5 text-blue-500" />
                  Daily Batch Processing
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <Alert>
                  <AlertCircle className="h-4 w-4" />
                  <AlertTitle>Daily Charge Processing</AlertTitle>
                  <AlertDescription>
                    Process SMS alerts and QBE charges for the day
                  </AlertDescription>
                </Alert>

                {lastProcessed.daily && (
                  <div className="text-sm text-muted-foreground">
                    Last processed: {format(new Date(lastProcessed.daily), 'PPpp')}
                  </div>
                )}

                <Button
                  onClick={handleDailyProcessing}
                  disabled={isProcessing.daily}
                  className="w-full"
                >
                  {isProcessing.daily ? 'Processing...' : 'Process Daily Charges'}
                </Button>
              </CardContent>
            </Card>
          </TabsContent>

          {/* Monthly Processing Tab */}
          <TabsContent value="monthly">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Calendar className="h-5 w-5 text-purple-500" />
                  Monthly Processing
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <label className="text-sm font-medium">Start Date</label>
                    <Input
                      type="date"
                      value={selectedDates.startDate}
                      onChange={(e) => setSelectedDates(prev => ({
                        ...prev,
                        startDate: e.target.value
                      }))}
                    />
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-medium">End Date</label>
                    <Input
                      type="date"
                      value={selectedDates.endDate}
                      onChange={(e) => setSelectedDates(prev => ({
                        ...prev,
                        endDate: e.target.value
                      }))}
                    />
                  </div>
                </div>

                <Button
                  onClick={handleMonthlyQBE}
                  disabled={isProcessing.monthly}
                  className="w-full"
                >
                  {isProcessing.monthly ? 'Processing...' : 'Process Monthly QBE'}
                </Button>
              </CardContent>
            </Card>
          </TabsContent>

          {/* Settlements Tab */}
          <TabsContent value="settlements">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Building2 className="h-5 w-5 text-green-500" />
                  Telco Settlements
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <Alert>
                  <AlertCircle className="h-4 w-4" />
                  <AlertTitle>Telco Settlements</AlertTitle>
                  <AlertDescription>
                    Process settlements for telco providers
                  </AlertDescription>
                </Alert>

                <Button
                  onClick={handleTelcoSettlements}
                  disabled={isProcessing.telco}
                  className="w-full"
                >
                  {isProcessing.telco ? 'Processing...' : 'Process Telco Settlements'}
                </Button>
              </CardContent>
            </Card>
          </TabsContent>

          <TabsContent value="reconcile">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Repeat2 className="h-5 w-5 text-yellow-500" />
                  Failed Transactions Reconciliation
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <Alert>
                  <AlertCircle className="h-4 w-4" />
                  <AlertTitle>Reconciliation Process</AlertTitle>
                  <AlertDescription>
                    Reconcile failed transactions for a specific date
                  </AlertDescription>
                </Alert>

                <div className="space-y-2">
                  <label className="text-sm font-medium">Reconciliation Date</label>
                  <Input
                    type="date"
                    value={selectedDates.startDate}
                    onChange={(e) => setSelectedDates(prev => ({
                      ...prev,
                      startDate: e.target.value
                    }))}
                  />
                </div>

                {lastProcessed.reconcile && (
                  <div className="text-sm text-muted-foreground">
                    Last processed: {format(new Date(lastProcessed.reconcile), 'PPpp')}
                  </div>
                )}

                <Button
                  onClick={handleReconcileFailedTransactions}
                  disabled={isProcessing.reconcile}
                  className="w-full"
                >
                  {isProcessing.reconcile ? 'Processing...' : 'Reconcile Failed Transactions'}
                </Button>
              </CardContent>
            </Card>
          </TabsContent>

          {/* <TabsContent value="month-end">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Calendar className="h-5 w-5 text-indigo-500" />
                  Month End Processing
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <Alert>
                  <AlertCircle className="h-4 w-4" />
                  <AlertTitle>Month End Process</AlertTitle>
                  <AlertDescription>
                    Process end of month consolidation and charges
                  </AlertDescription>
                </Alert>

                <div className="space-y-2">
                  <label className="text-sm font-medium">Month End Date</label>
                  <Input
                    type="date"
                    value={selectedDates.endDate}
                    onChange={(e) => setSelectedDates(prev => ({
                      ...prev,
                      endDate: e.target.value
                    }))}
                  />
                </div>

                {lastProcessed.monthEnd && (
                  <div className="text-sm text-muted-foreground">
                    Last processed: {format(new Date(lastProcessed.monthEnd), 'PPpp')}
                  </div>
                )}

                <Button
                  onClick={handleMonthEndProcessing}
                  disabled={isProcessing.monthEnd}
                  className="w-full"
                >
                  {isProcessing.monthEnd ? 'Processing...' : 'Process Month End'}
                </Button>
              </CardContent>
            </Card>
          </TabsContent> */}
        </Tabs>
      </div>
    </div>
  );
}