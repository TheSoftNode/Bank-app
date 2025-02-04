import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { AccountData, AccountType, CurrencyType } from "@/types/dashboard";
import { Badge } from "@/components/ui/badge";
import { BarChart3, Wallet, ArrowUpCircle } from 'lucide-react';

interface AccountSummaryGridProps {
  accounts: AccountData[];
}

export const AccountSummary = ({ accounts }: AccountSummaryGridProps) => {
  const totalBalance = accounts.reduce((sum, account) => sum + account.balance, 0);

  return (
    <div className="space-y-6">
      {/* Total Balance Card */}
      <Card className="bg-gradient-to-br from-teal-600 to-teal-700">
        <CardHeader>
          <CardTitle className="text-white flex items-center gap-2">
            <Wallet className="h-5 w-5" />
            Total Portfolio Value
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="text-3xl font-bold text-white">
            ₦{totalBalance.toLocaleString()}
          </div>
          <p className="text-teal-100 text-sm mt-1">
            Across {accounts.length} account{accounts.length !== 1 ? 's' : ''}
          </p>
        </CardContent>
      </Card>

      {/* Individual Accounts Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {accounts.map((account) => (
          <Card key={account.accountNumber} className="backdrop-blur-sm bg-white/50 hover:bg-white/60 transition-all">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <div className="space-y-1">
                <CardTitle className="text-sm font-medium">
                  {AccountType[account.accountType]} Account
                </CardTitle>
                <p className="text-xs text-muted-foreground">
                  {account.accountNumber}
                </p>
              </div>
              <Badge variant={account.isDomiciliaryAccount ? "secondary" : "outline"}>
                {CurrencyType[account.currencyType]}
              </Badge>
            </CardHeader>
            <CardContent>
              <div className="flex justify-between items-center">
                <div>
                  <div className="text-2xl font-bold">
                    {CurrencyType[account.currencyType] === 'NGN' ? '₦' : '$'}
                    {account.balance.toLocaleString()}
                  </div>
                  <p className="text-xs text-muted-foreground mt-1">
                    Branch: {account.branchSolId}
                  </p>
                </div>
                <BarChart3 className="h-4 w-4 text-muted-foreground" />
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
};