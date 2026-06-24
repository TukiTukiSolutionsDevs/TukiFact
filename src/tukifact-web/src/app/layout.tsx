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
  metadataBase: new URL(process.env.NEXT_PUBLIC_PUBLIC_URL ?? 'https://tukifact.com.pe'),
  title: {
    default: 'TukiFact — Facturación electrónica SUNAT para empresas peruanas',
    template: '%s · TukiFact',
  },
  description:
    'Emite facturas, boletas, notas y guías de remisión electrónicas con TukiFact. SUNAT al día, SOL incluida, asistente IA y API REST. Empieza gratis.',
  applicationName: 'TukiFact',
  authors: [{ name: 'Tukituki Solution S.A.C.', url: 'https://tukifact.com.pe' }],
  creator: 'Tukituki Solution S.A.C.',
  publisher: 'Tukituki Solution S.A.C.',
  keywords: [
    'facturación electrónica',
    'facturación electrónica Perú',
    'SUNAT',
    'factura electrónica SUNAT',
    'boleta electrónica',
    'guía de remisión electrónica',
    'GRE 2.0',
    'facturador electrónico',
    'OSE Perú',
    'PSE Perú',
    'TukiFact',
    'emitir factura electrónica',
    'sistema de facturación Perú',
    'API facturación electrónica',
  ],
  category: 'business',
  referrer: 'origin-when-cross-origin',
  formatDetection: { email: false, address: false, telephone: false },
  robots: {
    index: true,
    follow: true,
    nocache: false,
    googleBot: {
      index: true,
      follow: true,
      'max-image-preview': 'large',
      'max-snippet': -1,
      'max-video-preview': -1,
    },
  },
  openGraph: {
    type: 'website',
    locale: 'es_PE',
    siteName: 'TukiFact',
    url: 'https://tukifact.com.pe',
  },
  twitter: {
    card: 'summary_large_image',
    creator: '@tukifact',
    site: '@tukifact',
  },
  alternates: {
    canonical: '/',
    languages: { 'es-PE': '/' },
  },
  verification: {
    // Add Google / Bing site-verification tokens when issued by Search Console.
    // google: 'TOKEN_FROM_SEARCH_CONSOLE',
  },
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
