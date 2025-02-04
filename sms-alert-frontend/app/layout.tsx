import type { Metadata } from 'next'
import { Inter } from 'next/font/google'
import './globals.css'
import { AuthProvider } from '@/contexts/AuthContext'
import { Toaster } from 'react-hot-toast'

const inter = Inter({ subsets: ['latin'] })

export const metadata: Metadata = {
  title: 'Banking Dashboard',
  description: 'Modern banking management application',
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
})
{
  return (
    <html lang="en" suppressHydrationWarning>
      <body className={`${inter.className} flex h-screen`}>
        <AuthProvider>

          <div className="flex-1 overflow-y-auto w-full bg-background">
            {children}
          </div>
          <Toaster
            position="top-center"
            toastOptions={{
              duration: 4000,
              style: {
                background: '#363636',
                color: '#fff',
              },
              success: {
                style: {
                  background: 'green',
                },
              },
              error: {
                style: {
                  background: 'white',
                  color: 'red',
                },
                duration: 5000
              },
            }}
          />

        </AuthProvider>
      </body>
    </html>
  )
}