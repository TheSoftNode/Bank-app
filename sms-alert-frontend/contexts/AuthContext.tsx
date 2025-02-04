'use client';

import { createContext, useContext, useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import axios from 'axios';
import { AuthResponse, Customer } from '@/types/auth';

interface AuthContextType {
  user: Customer | null;
  token: string | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (userData: RegisterData) => Promise<void>;
  logout: () => void;
  isAdmin: boolean;
}

interface RegisterData {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  preferredLanguage?: string;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<Customer | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    // Check for stored auth data on mount
    const storedToken = localStorage.getItem('token');
    const storedUser = localStorage.getItem('user');

    if (storedToken && storedUser) {
      setToken(storedToken);
      setUser(JSON.parse(storedUser));
      axios.defaults.headers.common['Authorization'] = `Bearer ${storedToken}`;
    }

    setIsLoading(false);
  }, []);

  const login = async (email: string, password: string) => {
    try {
      const response = await axios.post<AuthResponse>(
        'https://localhost:7031/api/Customer/login',
        { email, password }
      );

      if (response.data.success) {
        const { token, customer } = response.data.data;
        setToken(token);
        setUser(customer);
        localStorage.setItem('token', token);
        localStorage.setItem('user', JSON.stringify(customer));
        axios.defaults.headers.common['Authorization'] = `Bearer ${token}`;
        router.push('/dashboard');
      }
    } catch (error) {
      throw error;
    }
  };

  const register = async (userData: RegisterData) => {
    console.log(userData);
    try {
      const response = await axios.post<AuthResponse>(
        'https://localhost:7031/api/Customer/register',
        userData
      );

      if (response.data.success) {
        const { token, customer } = response.data.data;
        setToken(token);
        setUser(customer);
        localStorage.setItem('token', token);
        localStorage.setItem('user', JSON.stringify(customer));
        axios.defaults.headers.common['Authorization'] = `Bearer ${token}`;
        router.push('/dashboard');
      }
    } catch (error) {
      throw error;
    }
  };

  const logout = () => {
    setToken(null);
    setUser(null);
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    delete axios.defaults.headers.common['Authorization'];
    router.push('/');
  };

  const isAdmin = user?.role === 'Admin';

  return (
    <AuthContext.Provider
      value={{ user, token, isLoading, login, register, logout, isAdmin }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}