import type { MetadataRoute } from 'next';

const BASE = process.env.NEXT_PUBLIC_PUBLIC_URL ?? 'https://tukifact.pe';

export default function sitemap(): MetadataRoute.Sitemap {
  const now = new Date();
  return [
    { url: `${BASE}/`, lastModified: now, changeFrequency: 'weekly', priority: 1.0 },
    { url: `${BASE}/funcionalidades`, lastModified: now, changeFrequency: 'monthly', priority: 0.9 },
    { url: `${BASE}/planes`, lastModified: now, changeFrequency: 'weekly', priority: 0.9 },
    { url: `${BASE}/seguridad`, lastModified: now, changeFrequency: 'monthly', priority: 0.7 },
    { url: `${BASE}/contacto`, lastModified: now, changeFrequency: 'yearly', priority: 0.6 },
    { url: `${BASE}/login`, lastModified: now, changeFrequency: 'yearly', priority: 0.3 },
    { url: `${BASE}/register`, lastModified: now, changeFrequency: 'yearly', priority: 0.5 },
    { url: `${BASE}/privacy`, lastModified: now, changeFrequency: 'yearly', priority: 0.3 },
    { url: `${BASE}/terms`, lastModified: now, changeFrequency: 'yearly', priority: 0.3 },
  ];
}
