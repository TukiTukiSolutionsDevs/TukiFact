import type { Metadata } from 'next';
import { PublicHeader } from '@/components/public/Header';
import { PublicFooter } from '@/components/public/Footer';

export const metadata: Metadata = {
  title: {
    default: 'TukiFact — Facturación electrónica para empresas peruanas',
    template: '%s · TukiFact',
  },
  description:
    'Emite facturas, boletas, notas y guías de remisión electrónicas con TukiFact. SUNAT al día, SOL incluida, asistente IA y API REST.',
  metadataBase: new URL('https://tukifact.pe'),
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
      <PublicHeader />
      <main className="flex-1">{children}</main>
      <PublicFooter />
    </div>
  );
}
