import type { MetadataRoute } from 'next';

export default function manifest(): MetadataRoute.Manifest {
  return {
    name: 'TukiFact — Facturación electrónica SUNAT',
    short_name: 'TukiFact',
    description:
      'Emite facturas, boletas, notas y guías de remisión electrónicas SUNAT con TukiFact.',
    start_url: '/',
    display: 'standalone',
    background_color: '#ffffff',
    theme_color: '#0a0a0a',
    lang: 'es-PE',
    orientation: 'portrait-primary',
    categories: ['business', 'productivity', 'finance'],
    icons: [
      { src: '/icon.png', sizes: '512x512', type: 'image/png', purpose: 'any' },
      { src: '/icon.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
    ],
  };
}
