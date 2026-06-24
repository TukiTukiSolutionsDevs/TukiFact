import type { ReactElement } from 'react';

export const TUKIFACT_BRAND = {
  name: 'TukiFact',
  legalName: 'Tukituki Solution S.A.C.',
  url: 'https://tukifact.com.pe',
  logo: 'https://tukifact.com.pe/logo.png',
  ogImage: 'https://tukifact.com.pe/opengraph-image',
  description:
    'Plataforma de facturación electrónica para empresas peruanas. Emite facturas, boletas, notas y guías de remisión SUNAT con firma digital, IA y API REST.',
  email: 'hola@tukifact.com.pe',
  phone: '+51966388258',
  whatsapp: 'https://wa.me/51966388258',
  address: {
    streetAddress: 'Pasaje Carabaya 105, Urb. Alto Libertad',
    addressLocality: 'Cerro Colorado',
    addressRegion: 'Arequipa',
    addressCountry: 'PE',
  },
  sameAs: [
    'https://www.linkedin.com/company/119484308',
    'https://www.instagram.com/tuki_tuki_solutions/',
  ] as string[],
  foundingDate: '2026',
} as const;

function JsonLd({ id, data }: { id?: string; data: Record<string, unknown> }) {
  return (
    <script
      type="application/ld+json"
      id={id}
      dangerouslySetInnerHTML={{ __html: JSON.stringify(data) }}
    />
  );
}

export function OrganizationJsonLd(): ReactElement {
  const b = TUKIFACT_BRAND;
  return (
    <JsonLd
      id="ld-organization"
      data={{
        '@context': 'https://schema.org',
        '@type': 'Organization',
        '@id': `${b.url}/#organization`,
        name: b.name,
        legalName: b.legalName,
        url: b.url,
        logo: { '@type': 'ImageObject', url: b.logo, width: 1258, height: 398 },
        image: b.ogImage,
        description: b.description,
        email: b.email,
        telephone: `+${b.phone.replace(/[^\d]/g, '')}`,
        address: {
          '@type': 'PostalAddress',
          streetAddress: b.address.streetAddress,
          addressLocality: b.address.addressLocality,
          addressRegion: b.address.addressRegion,
          addressCountry: b.address.addressCountry,
        },
        contactPoint: [
          {
            '@type': 'ContactPoint',
            contactType: 'customer support',
            email: b.email,
            telephone: `+${b.phone.replace(/[^\d]/g, '')}`,
            areaServed: 'PE',
            availableLanguage: ['es', 'es-PE'],
          },
        ],
        ...(b.sameAs.length > 0 ? { sameAs: b.sameAs } : {}),
        foundingDate: b.foundingDate,
        foundingLocation: { '@type': 'Place', name: 'Arequipa, Perú' },
      }}
    />
  );
}

export function WebSiteJsonLd(): ReactElement {
  const b = TUKIFACT_BRAND;
  return (
    <JsonLd
      id="ld-website"
      data={{
        '@context': 'https://schema.org',
        '@type': 'WebSite',
        '@id': `${b.url}/#website`,
        url: b.url,
        name: b.name,
        description: b.description,
        publisher: { '@id': `${b.url}/#organization` },
        inLanguage: 'es-PE',
      }}
    />
  );
}

export function SoftwareApplicationJsonLd(): ReactElement {
  const b = TUKIFACT_BRAND;
  return (
    <JsonLd
      id="ld-software"
      data={{
        '@context': 'https://schema.org',
        '@type': 'SoftwareApplication',
        '@id': `${b.url}/#software`,
        name: b.name,
        applicationCategory: 'BusinessApplication',
        applicationSubCategory: 'Facturación electrónica',
        operatingSystem: 'Web',
        description: b.description,
        url: b.url,
        image: b.ogImage,
        publisher: { '@id': `${b.url}/#organization` },
        offers: [
          { '@type': 'Offer', name: 'Gratis', price: '0', priceCurrency: 'PEN', availability: 'https://schema.org/InStock', url: `${b.url}/planes` },
          { '@type': 'Offer', name: 'Emprendedor', price: '35', priceCurrency: 'PEN', availability: 'https://schema.org/InStock', url: `${b.url}/planes` },
          { '@type': 'Offer', name: 'Negocio', price: '79', priceCurrency: 'PEN', availability: 'https://schema.org/InStock', url: `${b.url}/planes` },
          { '@type': 'Offer', name: 'Profesional', price: '179', priceCurrency: 'PEN', availability: 'https://schema.org/InStock', url: `${b.url}/planes` },
          { '@type': 'Offer', name: 'Empresa', price: '349', priceCurrency: 'PEN', availability: 'https://schema.org/InStock', url: `${b.url}/planes` },
        ],
        featureList: [
          'Facturas y boletas electrónicas SUNAT',
          'Guías de remisión 2.0 (GRE)',
          'Notas de crédito y débito',
          'Percepciones y retenciones',
          'Facturación recurrente programada',
          'Asistente con IA conversacional',
          'API REST y webhooks firmados HMAC',
          'Multi-usuario con roles y audit log',
        ],
        inLanguage: 'es-PE',
      }}
    />
  );
}

export function FAQPageJsonLd({ items }: { items: Array<{ q: string; a: string }> }): ReactElement {
  return (
    <JsonLd
      id="ld-faq"
      data={{
        '@context': 'https://schema.org',
        '@type': 'FAQPage',
        mainEntity: items.map((it) => ({
          '@type': 'Question',
          name: it.q,
          acceptedAnswer: { '@type': 'Answer', text: it.a },
        })),
      }}
    />
  );
}

export function BreadcrumbJsonLd({ items }: { items: Array<{ name: string; url: string }> }): ReactElement {
  return (
    <JsonLd
      id="ld-breadcrumb"
      data={{
        '@context': 'https://schema.org',
        '@type': 'BreadcrumbList',
        itemListElement: items.map((it, i) => ({
          '@type': 'ListItem',
          position: i + 1,
          name: it.name,
          item: it.url,
        })),
      }}
    />
  );
}

export function ContactPageJsonLd(): ReactElement {
  const b = TUKIFACT_BRAND;
  return (
    <JsonLd
      id="ld-contact"
      data={{
        '@context': 'https://schema.org',
        '@type': 'ContactPage',
        url: `${b.url}/contacto`,
        about: { '@id': `${b.url}/#organization` },
        mainEntity: {
          '@id': `${b.url}/#organization`,
        },
      }}
    />
  );
}
