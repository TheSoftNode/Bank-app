import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

// Paths that don't require authentication
const publicPaths = ['/', '/login', '/register'];

export function middleware(request: NextRequest) {
  const currentPath = request.nextUrl.pathname;

  // Allow public paths
  if (publicPaths.includes(currentPath)) {
    return NextResponse.next();
  }

  // For protected routes, we'll handle the auth check on the client side
  return NextResponse.next();
}

export const config = {
  matcher: [
    '/((?!api|_next/static|_next/image|favicon.ico).*)',
  ],
};