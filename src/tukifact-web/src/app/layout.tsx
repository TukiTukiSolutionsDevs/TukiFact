import type { Metadata } from 'next';
import { Inter, Fredoka, JetBrains_Mono } from 'next/font/google';
import './globals.css';
import { AuthProvider } from '@/lib/auth-context';
import { Toaster } from '@/components/ui/sonner';
import { ThemeProvider } from 'next-themes';

const inter = Inter({ subsets: ['latin'], variable: '--font-inter', display: 'swap' });
const fredoka = Fredoka({ subsets: ['latin'], variable: '--font-fredoka', weight: ['500', '600', '700'], display: 'swap' });
const mono = JetBrains_Mono({ subsets: ['latin'], variable: '--font-geist-mono', display: 'swap' });

export const metadata: Metadata = {
  title: 'TukiFact — Facturación Inteligente',
  description: 'Plataforma SaaS de facturación electrónica para Perú',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html
      lang="es-PE"
      className={`${inter.variable} ${fredoka.variable} ${mono.variable}`}
      suppressHydrationWarning
    >
      <body className="min-h-screen bg-background antialiased">
        <ThemeProvider attribute="class" defaultTheme="light" enableSystem disableTransitionOnChange>
          <AuthProvider>
            {children}
            <Toaster richColors position="top-right" />
          </AuthProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
