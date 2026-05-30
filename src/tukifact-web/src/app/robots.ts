import type { MetadataRoute } from 'next';

const BASE = process.env.NEXT_PUBLIC_PUBLIC_URL ?? 'https://tukifact.com.pe';

export default function robots(): MetadataRoute.Robots {
  return {
    rules: [
      {
        userAgent: '*',
        allow: '/',
        disallow: ['/dashboard', '/documents', '/customers', '/products', '/settings', '/api-keys', '/webhooks', '/audit-log', '/backoffice', '/welcome'],
      },
    ],
    sitemap: `${BASE}/sitemap.xml`,
    host: BASE,
  };
}
