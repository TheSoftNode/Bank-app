'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import
  {
    Home,
    Users,
    CreditCard,
    Activity,
    Settings,
    ShieldAlert,
    RefreshCcw,
    Building2
  } from 'lucide-react';
import { Button } from '@/components/ui/button';
import
  {
    Tooltip,
    TooltipContent,
    TooltipProvider,
    TooltipTrigger
  } from '@/components/ui/tooltip';
import { useAuth } from '@/contexts/AuthContext';
import { cn } from '@/lib/utils';

export default function Sidebar()
{
  const pathname = usePathname();
  const { user } = useAuth();
  const isAdmin = user?.role === 'Admin';

  // Regular user items
  const userItems = [
    {
      href: '/dashboard',
      icon: Home,
      label: 'Dashboard'
    },
    {
      href: '/accounts',
      icon: CreditCard,
      label: 'Accounts'
    },
    {
      href: '/transaction',
      icon: Activity,
      label: 'Transactions'
    },
  ];

  // Admin-only items
  const adminItems = [
    {
      href: '/admin',
      icon: ShieldAlert,
      label: 'Admin Panel'
    },
    {
      href: '/admin/processing',
      icon: RefreshCcw,
      label: 'Processing'
    },
    {
      href: '/admin/customers',
      icon: Users,
      label: 'Manage Customers'
    },
  ];

  const sidebarItems = [...userItems, ...(isAdmin ? adminItems : [])];

  return (
    <div className="hidden md:flex flex-col h-screen fixed left-0 top-0 z-40 w-14 hover:w-60 bg-background border-r shadow-sm transition-all duration-300 ease-in-out group">
      {/* Logo Section */}
      <Link href={"/"} className="h-14 flex items-center justify-center border-b">
        <Building2 className="h-6 w-6 text-teal-600" />
        <span className="hidden group-hover:inline ml-3 font-semibold text-lg transition-all duration-300">
          SmartBank
        </span>
      </Link>

      {/* Navigation Section */}
      <div className="flex-1 flex flex-col gap-2 p-2">
        {/* <TooltipProvider delayDuration={0}> */}
        <nav className="space-y-1">
          {sidebarItems.map((item, index) =>
          {
            const isActive = pathname === item.href;
            // Add separator before admin items
            const showSeparator = isAdmin && index === userItems.length;

            return (
              <div key={item.href}>
                {showSeparator && (
                  <div className="my-2 border-t border-border opacity-50" />
                )}
                {/* <Tooltip> */}
                {/* <TooltipTrigger asChild> */}
                <Link href={item.href} className="block">
                  <Button
                    variant={isActive ? 'secondary' : 'ghost'}
                    className={cn(
                      "w-full h-10 px-2 justify-start",
                      isActive && "bg-teal-50 text-teal-700 hover:bg-teal-100 hover:text-teal-700",
                      !isActive && "hover:bg-gray-50"
                    )}
                  >
                    <item.icon className={cn(
                      "h-4 w-4 shrink-0",
                      isActive && "text-teal-600"
                    )} />
                    <span className="hidden group-hover:inline ml-3 text-sm">
                      {item.label}
                    </span>
                  </Button>
                </Link>
                {/* </TooltipTrigger> */}
                {/* <TooltipContent 
                      side="right" 
                      className="group-hover:hidden"
                    >
                      {item.label}
                    </TooltipContent> */}
                {/* </Tooltip> */}
              </div>
            );
          })}
        </nav>
        {/* </TooltipProvider> */}
      </div>

      {/* User Section */}
      <div className="p-2 border-t">
        <div className="flex items-center px-2 py-2 rounded-lg bg-gray-50">
          <div className="w-8 h-8 rounded-full bg-teal-100 flex items-center justify-center">
            <span className="text-sm font-medium text-teal-700">
              {user?.firstName?.[0]}
            </span>
          </div>
          <div className="hidden group-hover:block ml-3">
            <p className="text-sm font-medium truncate">{user?.firstName} {user?.lastName}</p>
            <p className="text-xs text-muted-foreground">{user?.email}</p>
          </div>
        </div>
      </div>
    </div>
  );
}

