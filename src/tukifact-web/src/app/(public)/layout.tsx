import type { Metadata } from 'next';
import Script from 'next/script';
import { PublicHeader } from '@/components/public/Header';
import { PublicFooter } from '@/components/public/Footer';

const PLAUSIBLE_DOMAIN = process.env.NEXT_PUBLIC_PLAUSIBLE_DOMAIN;
const PLAUSIBLE_SRC = process.env.NEXT_PUBLIC_PLAUSIBLE_SRC ?? 'https://plausible.io/js/script.js';

export const metadata: Metadata = {
  title: {
    default: 'TukiFact — Facturación electrónica para empresas peruanas',
    template: '%s · TukiFact',
  },
  description:
    'Emite facturas, boletas, notas y guías de remisión electrónicas con TukiFact. SUNAT al día, SOL incluida, asistente IA y API REST.',
  metadataBase: new URL(process.env.NEXT_PUBLIC_PUBLIC_URL ?? 'https://tukifact.com.pe'),
  openGraph: {
    type: 'website',
    locale: 'es_PE',
    siteName: 'TukiFact',
    images: ['/icon.png'],
  },
  twitter: { card: 'summary_large_image', images: ['/icon.png'] },
};

export default function PublicLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col bg-background">
      {PLAUSIBLE_DOMAIN && (
        <Script
          defer
          data-domain={PLAUSIBLE_DOMAIN}
          src={PLAUSIBLE_SRC}
          strategy="afterInteractive"
        />
      )}
      <PublicHeader />
      <main className="flex-1">{children}</main>
      <PublicFooter />
    </div>
  );
}
