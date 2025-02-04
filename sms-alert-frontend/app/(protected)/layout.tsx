'use client';

import AuthGuard from "@/components/gaurds/AuthGuard";
import Navbar from "@/components/Navbar";
import Sidebar from "@/components/Sidebar";
import Provider from "@/components/theme-provider";

export default function ProtectedLayout({
    children,
}: {
    children: React.ReactNode;
})
{
    return (
        <AuthGuard>
            <Provider>
                <div className="min-h-screen bg-gray-50">
                    <Navbar />
                    <div className="flex">
                        <Sidebar />
                        <main className="flex-1 p-4 ml-16 md:p-6 lg:p-8">
                            {children}
                        </main>
                    </div>
                </div>
            </Provider>
        </AuthGuard>
    );
}