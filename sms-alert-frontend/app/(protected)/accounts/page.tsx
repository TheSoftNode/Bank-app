"use client"

import React, { useState, useEffect } from 'react'
import
{
  UserPlus,
  Save,
  FileText,
  CreditCard,
  Globe,
  List,
  MoreHorizontal,
  Filter,
  Banknote
} from 'lucide-react'

import
{
  Card,
  CardContent,
  CardHeader,
  CardTitle
} from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import
{
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue
} from "@/components/ui/select"
import { Switch } from "@/components/ui/switch"
import
{
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
  DialogTrigger
} from "@/components/ui/dialog"
import
{
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from "@/components/ui/dropdown-menu"
import
{
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from "@/components/ui/table"
import { Badge } from "@/components/ui/badge"
import toast from "react-hot-toast"
import axios from "axios"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { useAuth } from '@/contexts/AuthContext'

enum AccountType
{
  Savings = 0,
  Current = 1,
  Domiciliary = 2
}

enum CurrencyType
{
  NGN = 0,
  USD = 1,
  EUR = 2,
  GBP = 3
}

interface CreateCustomerAccountDto
{
  email: string;
  accountNumber: string;
  accountType: AccountType;
  currencyType: CurrencyType;
  branchSolId: string;
  initialBalance: number;
  isDomiciliaryAccount: boolean;
  linkedNigerianAccountNumber?: string;
}

interface GetAccountsResponseDto
{
  email: string;
  accountNumber: string;
  accountType: AccountType;
  currencyType: CurrencyType;
  branchSolId: string;
  balance: number;
  isDomiciliaryAccount: boolean;
  linkedNigerianAccountNumber?: string;
}



const CustomerAccountManagement: React.FC = () =>
{
  // State for account creation form
  const [accountForm, setAccountForm] = useState<CreateCustomerAccountDto>({
    email: '',
    accountNumber: '',
    accountType: AccountType.Savings,
    currencyType: CurrencyType.NGN,
    branchSolId: '',
    initialBalance: 0,
    isDomiciliaryAccount: false,
    linkedNigerianAccountNumber: ''
  })


  // Filtering and search states
  const [searchTerm, setSearchTerm] = useState('')
  const [filterAccountType, setFilterAccountType] = useState<AccountType | 'All'>('All')
  const [accounts, setAccounts] = useState<GetAccountsResponseDto[]>([])
  const { user: userData } = useAuth();

  // Handler for form input changes (same as previous implementation)
  const handleInputChange = (field: keyof CreateCustomerAccountDto, value: string | number | boolean) =>
  {
    setAccountForm(prev => ({
      ...prev,
      [field]: value
    }))
  }

  useEffect(() =>
  {
    fetchAccounts();
  }, []);

  const fetchAccounts = async () =>
  {
    try
    {
      const response = await axios.get(`https://localhost:7031/api/Customer/${userData?.email}/accounts`, {
        headers: {
          Authorization: `Bearer ${localStorage.getItem('token')}`
        }
      });

      console.log('Fetched accounts:', response.data);

      if (response.data.success)
      {
        setAccounts(response.data.data);
      }
    } catch (error: any)
    {
      toast.error(error.response?.data?.message || 'Failed to fetch accounts');
    }
  };

  // Generate account number (simplified mock)
  const generateAccountNumber = () =>
  {
    const prefix = accountForm.accountType === AccountType.Domiciliary ? '302' : '101'
    const randomSuffix = Math.floor(10000000 + Math.random() * 90000000)
    return `${prefix}${randomSuffix}`
  }


  const handleAccountCreation = async (e: React.FormEvent) =>
  {
    e.preventDefault();

    const errors: string[] = [];

    if (!accountForm.email.trim())
    {
      errors.push("Customer Email is required");
    }

    if (!accountForm.branchSolId.trim())
    {
      errors.push("Branch Sol ID is required");
    }

    if (accountForm.isDomiciliaryAccount && !accountForm.linkedNigerianAccountNumber)
    {
      errors.push("Linked Nigerian Account Number is required for Domiciliary Accounts");
    }

    if (errors.length > 0)
    {
      toast.error(errors.join(", "));
      return;
    }

    try
    {
      const requestData = {
        email: accountForm.email,
        accountNumber: accountForm.accountNumber || generateAccountNumber(),
        accountType: Number(accountForm.accountType),  // send as number
        currencyType: Number(accountForm.currencyType),  // send as number
        branchSolId: accountForm.branchSolId,
        initialBalance: Number(accountForm.initialBalance),
        isDomiciliaryAccount: accountForm.isDomiciliaryAccount,
        linkedNigerianAccountNumber: accountForm.linkedNigerianAccountNumber || ""
      };

      console.log('Sending account data:', requestData);


      const response = await axios.post(
        'https://localhost:7031/api/CustomerAccount',
        requestData,
        // {
        //   dto: requestData
        // },
        {
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('token')}`,
            'Content-Type': 'application/json'
          }
        }
      );

      if (response.data.success)
      {
        toast.success('Account created successfully');
        setAccounts(prev => [...prev, response.data.data]);
        // Reset form
        setAccountForm({
          email: '',
          accountNumber: '',
          accountType: AccountType.Savings,
          currencyType: CurrencyType.NGN,
          branchSolId: '',
          initialBalance: 0,
          isDomiciliaryAccount: false,
          linkedNigerianAccountNumber: ''
        });
      }
    } catch (error: any)
    {
      // Fixed error handling
      console.error('Account creation error:', error.response?.data);
      const errorMessage = error.response?.data?.message ||
        (error.response?.data?.errors ? Object.values(error.response.data.errors).flat()[0] : 'Failed to create account');
      toast.error(errorMessage);
    }
  };

  const filteredAccounts = accounts.filter(account =>
    // First check account type filter
    (filterAccountType === 'All' || account.accountType === filterAccountType) &&
    // Then check search term against email or account number
    (
      // account?.email?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      account?.accountNumber?.includes(searchTerm)
    )
  );


  return (
    <div className=" p-6">
      <div className="container mx-auto">
        <Tabs defaultValue={`${userData?.role === 'Admin' ? "create" : "list"}`} className="space-y-4">
          <TabsList className={`grid w-full  ${userData?.role === 'Admin' ? "grid-cols-2" : "grid-cols-1"}`}>
            {userData?.role === 'Admin' && (
              <TabsTrigger value="create" className="flex items-center gap-2">
                <UserPlus className="h-4 w-4" /> Create Account
              </TabsTrigger>
            )}

            <TabsTrigger value="list" className="flex items-center gap-2">
              <List className="h-4 w-4" /> Account List
            </TabsTrigger>
          </TabsList>

          {/* Create Account Tab */}
          {userData?.role === 'Admin' && (
            <TabsContent value="create">
              <Card className="w-full max-w-4xl mx-auto">
                <CardHeader className="flex flex-row items-center justify-between">
                  <CardHeader className="flex flex-row items-center justify-between">
                    <div>
                      <CardTitle className="flex items-center gap-2 mr-3">
                        <UserPlus className="h-6 w-6" />
                        Create Customer Account
                      </CardTitle>
                    </div>
                    <Dialog>
                      <DialogTrigger asChild>
                        <Button variant="outline">View Account Rules</Button>
                      </DialogTrigger>
                      <DialogContent>
                        <DialogHeader>
                          <DialogTitle>Account Creation Guidelines</DialogTitle>
                          <DialogDescription>
                            <ul className="list-disc pl-5 space-y-2">
                              <li>Customer Number must be unique</li>
                              <li>Branch Sol ID is mandatory</li>
                              <li>Domiciliary accounts require a linked Nigerian account</li>
                              <li>Initial balance can be zero</li>
                            </ul>
                          </DialogDescription>
                        </DialogHeader>
                      </DialogContent>
                    </Dialog>
                  </CardHeader>
                </CardHeader>
                <CardContent>
                  {/* Form content from previous implementation */}
                  <form onSubmit={handleAccountCreation}>
                    <div className="grid md:grid-cols-2 gap-6">
                      {/* Customer Email */}
                      <div className="space-y-2">
                        <Label htmlFor="email">
                          <FileText className="inline-block mr-2 h-4 w-4" />
                          Customer Email
                        </Label>
                        <Input
                          id="email"
                          placeholder="Enter Customer Email"
                          value={accountForm.email}
                          onChange={(e) => handleInputChange('email', e.target.value)}
                          required
                        />
                      </div>

                      {/* Account Number */}
                      <div className="space-y-2">
                        <Label htmlFor="accountNumber">
                          <CreditCard className="inline-block mr-2 h-4 w-4" />
                          Account Number
                        </Label>
                        <Input
                          id="accountNumber"
                          placeholder="Auto-generate or Enter Manually"
                          value={accountForm.accountNumber}
                          onChange={(e) => handleInputChange('accountNumber', e.target.value)}
                        />
                      </div>

                      {/* Account Type */}
                      <div className="space-y-2">
                        <Label>
                          <Banknote className="inline-block mr-2 h-4 w-4" />
                          Account Type
                        </Label>
                        <Select
                          value={AccountType[accountForm.accountType].toString()}
                          onValueChange={(value) => handleInputChange('accountType', AccountType[value as keyof typeof AccountType])}
                        >
                          <SelectTrigger>
                            <SelectValue placeholder="Select Account Type" />
                          </SelectTrigger>
                          <SelectContent>
                            {Object.keys(AccountType)
                              .filter(key => isNaN(Number(key)))
                              .map((type) => (
                                <SelectItem key={type} value={type}>
                                  {type}
                                </SelectItem>
                              ))}
                          </SelectContent>
                        </Select>
                      </div>

                      {/* Currency Type */}
                      <div className="space-y-2">
                        <Label>
                          <Globe className="inline-block mr-2 h-4 w-4" />
                          Currency Type
                        </Label>
                        <Select
                          value={CurrencyType[accountForm.currencyType].toString()}
                          onValueChange={(value) => handleInputChange('currencyType', CurrencyType[value as keyof typeof CurrencyType])}
                        >
                          <SelectTrigger>
                            <SelectValue placeholder="Select Currency" />
                          </SelectTrigger>
                          <SelectContent>
                            {Object.keys(CurrencyType)
                              .filter(key => isNaN(Number(key)))
                              .map((currency) => (
                                <SelectItem key={currency} value={currency}>
                                  {currency}
                                </SelectItem>
                              ))}
                          </SelectContent>
                        </Select>
                      </div>

                      {/* Branch Sol ID */}
                      <div className="space-y-2">
                        <Label htmlFor="branchSolId">Branch Sol ID</Label>
                        <Input
                          id="branchSolId"
                          placeholder="Enter Branch Sol ID"
                          value={accountForm.branchSolId}
                          onChange={(e) => handleInputChange('branchSolId', e.target.value)}
                          required
                        />
                      </div>

                      {/* Initial Balance */}
                      <div className="space-y-2">
                        <Label htmlFor="initialBalance">Initial Balance</Label>
                        <Input
                          id="initialBalance"
                          type="number"
                          placeholder="Enter Initial Balance"
                          value={accountForm.initialBalance}
                          onChange={(e) => handleInputChange('initialBalance', parseFloat(e.target.value))}
                        />
                      </div>

                      {/* Domiciliary Account Toggle */}
                      <div className="space-y-2 flex items-center justify-between">
                        <Label htmlFor="isDomiciliaryAccount">Domiciliary Account</Label>
                        <Switch
                          id="isDomiciliaryAccount"
                          checked={accountForm.isDomiciliaryAccount}
                          onCheckedChange={(checked) => handleInputChange('isDomiciliaryAccount', checked)}
                        />
                      </div>

                      {/* Linked Nigerian Account (Conditional) */}
                      {accountForm.isDomiciliaryAccount && (
                        <div className="space-y-2 md:col-span-2">
                          <Label htmlFor="linkedNigerianAccountNumber">
                            Linked Nigerian Account Number
                          </Label>
                          <Input
                            id="linkedNigerianAccountNumber"
                            placeholder="Enter Linked Nigerian Account Number"
                            value={accountForm.linkedNigerianAccountNumber || ''}
                            onChange={(e) => handleInputChange('linkedNigerianAccountNumber', e.target.value)}
                            required
                          />
                        </div>
                      )}
                    </div>

                    {/* Submit Button */}
                    <div className="flex justify-end mt-4">
                      <Button type="submit" className="flex items-center gap-2">
                        <Save className="h-4 w-4" />
                        Create Account
                      </Button>
                    </div>
                  </form>
                </CardContent>
              </Card>
            </TabsContent>
          )}

          {/* Account List Tab */}
          <TabsContent value="list">
            <Card>
              <CardHeader>
                <div className="flex justify-between items-center">
                  <CardTitle className="flex items-center gap-2">
                    <List className="h-6 w-6" /> Account List
                  </CardTitle>
                  <div className="flex items-center gap-2">
                    {/* Search Input */}
                    <Input
                      placeholder="Search by email or account number"
                      className="w-64"
                      value={searchTerm}
                      onChange={(e) => setSearchTerm(e.target.value)}
                    />

                    {/* Account Type Filter */}
                    <Select
                      value={filterAccountType === 'All' ? 'All' : AccountType[filterAccountType].toString()}
                      onValueChange={(value) =>
                      {
                        if (value === 'All')
                        {
                          setFilterAccountType('All');
                        } else
                        {
                          setFilterAccountType(AccountType[value as keyof typeof AccountType]);
                        }
                      }}
                    >
                      <SelectTrigger className="w-[180px]">
                        <SelectValue placeholder="Filter Account Type" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="All">All Account Types</SelectItem>
                        {Object.keys(AccountType)
                          .filter(key => isNaN(Number(key)))
                          .map((type) => (
                            <SelectItem key={type} value={type}>
                              {type}
                            </SelectItem>
                          ))}
                      </SelectContent>
                    </Select>
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Account Number</TableHead>
                      <TableHead>Account Type</TableHead>
                      <TableHead>Currency</TableHead>
                      <TableHead>Balance</TableHead>
                      <TableHead>Branch Sol ID</TableHead>
                      <TableHead>Domiciliary</TableHead>
                      <TableHead>LinkedAccount</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {filteredAccounts.map((account) => (
                      <TableRow key={account.accountNumber}>
                        <TableCell>{account.accountNumber}</TableCell>
                        <TableCell>
                          <Badge variant="secondary">
                            {AccountType[account.accountType]}
                          </Badge>
                        </TableCell>
                        <TableCell>{CurrencyType[account.currencyType]}</TableCell>
                        <TableCell>
                          {(account.balance ?? 0).toLocaleString()}{' '}
                          {account.currencyType !== undefined ? CurrencyType[account.currencyType] : 'NGN'}
                        </TableCell>
                        <TableCell>{account.branchSolId}</TableCell>
                        <TableCell>
                          <Badge variant="outline">
                            {account.isDomiciliaryAccount ? 'Yes' : 'No'}
                          </Badge>
                        </TableCell>
                        <TableCell>
                          {account.linkedNigerianAccountNumber || '-'}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
                {filteredAccounts.length === 0 && (
                  <div className="text-center py-4 text-muted-foreground">
                    No accounts found
                  </div>
                )}
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  )
}

export default CustomerAccountManagement