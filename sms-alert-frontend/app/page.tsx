"use client"
import React from 'react';
import { Building2, ChevronRight } from 'lucide-react';
import Link from 'next/link';

const SplashPage = () => {
 const user = localStorage.getItem('user');
 console.log(user);

  return (
    <div className="relative mt-28 bg-white">
      {/* Background Pattern */}
      <div className="fixed inset-0">
        <div className="absolute inset-0 bg-gradient-to-br from-teal-50 to-indigo-50" />
        <div className="absolute inset-y-0 right-0 w-1/2 bg-gradient-to-l from-white/50 to-transparent" />
        <div className="absolute h-96 w-96 -top-12 -right-12 bg-teal-100/20 rounded-full blur-3xl" />
        <div className="absolute h-96 w-96 bottom-0 left-0 bg-indigo-100/20 rounded-full blur-3xl" />
      </div>

      {/* Content */}
      <div className="relative z-10 flex  flex-col items-center justify-center px-4 sm:px-6 lg:px-8">
        <div className="w-full max-w-md space-y-8">
          {/* Logo and Title */}
          <div className="flex flex-col items-center">
            <div className="flex items-center justify-center h-20 w-20 rounded-full bg-gradient-to-br from-teal-600 to-teal-500 shadow-lg">
              <Building2 className="h-10 w-10 text-white" />
            </div>
            <h1 className="mt-6 text-4xl font-bold tracking-tight text-gray-900 text-center">
              SmartBank
            </h1>
            <p className="mt-2 text-sm text-gray-600 text-center">
              Experience Modern Banking
            </p>
          </div>

          {/* Action Buttons */}
          <div className="mt-10 flex flex-col gap-4">
            <Link
              href={user ? '/dashboard' : '/register'}
              className="flex items-center justify-center px-6 py-3 text-base font-medium text-white bg-teal-600 hover:bg-teal-700 rounded-lg shadow-md transition-all duration-200 hover:shadow-lg group"
            >
              Get Started
              <ChevronRight className="ml-2 h-5 w-5 group-hover:translate-x-1 transition-transform" />
            </Link>
            <Link
              href="/login"
              className="flex items-center justify-center px-6 py-3 text-base font-medium text-gray-900 bg-white hover:bg-gray-50 rounded-lg border border-gray-200 shadow-sm transition-all duration-200"
            >
              Sign In
            </Link>
          </div>
        </div>

        {/* Decorative Elements */}
        <div className="absolute top-0 left-0 w-32 h-32 bg-teal-100/30 rounded-br-full blur-2xl" />
        <div className="absolute bottom-0 right-0 w-32 h-32 bg-indigo-100/30 rounded-tl-full blur-2xl" />
      </div>
    </div>
  );
};

export default SplashPage;