import Script from 'next/script';
import { PublicHeader } from '@/components/public/Header';
import { PublicFooter } from '@/components/public/Footer';
import { OrganizationJsonLd, WebSiteJsonLd } from '@/components/seo/jsonld';

const PLAUSIBLE_DOMAIN = process.env.NEXT_PUBLIC_PLAUSIBLE_DOMAIN;
const PLAUSIBLE_SRC = process.env.NEXT_PUBLIC_PLAUSIBLE_SRC ?? 'https://plausible.io/js/script.js';

export default function PublicLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col bg-background">
      <OrganizationJsonLd />
      <WebSiteJsonLd />
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
