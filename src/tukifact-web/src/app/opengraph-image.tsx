import { ImageResponse } from 'next/og';

export const alt = 'TukiFact — Facturación electrónica SUNAT para empresas peruanas';
export const size = { width: 1200, height: 630 };
export const contentType = 'image/png';

export default async function OpenGraphImage() {
  return new ImageResponse(
    (
      <div
        style={{
          height: '100%',
          width: '100%',
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'center',
          alignItems: 'flex-start',
          background:
            'linear-gradient(135deg, #0F172A 0%, #1E293B 55%, #0F172A 100%)',
          padding: '80px 96px',
          color: '#FFFFFF',
          fontFamily: 'sans-serif',
          position: 'relative',
        }}
      >
        <div
          style={{
            position: 'absolute',
            top: -180,
            right: -180,
            width: 540,
            height: 540,
            borderRadius: 999,
            background: 'radial-gradient(circle, rgba(250,204,21,0.35), transparent 70%)',
            display: 'flex',
          }}
        />
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 12,
            background: 'rgba(255,255,255,0.06)',
            border: '1px solid rgba(255,255,255,0.14)',
            padding: '10px 22px',
            borderRadius: 999,
            fontSize: 24,
            color: '#FACC15',
            marginBottom: 40,
          }}
        >
          <div
            style={{
              width: 12,
              height: 12,
              borderRadius: 999,
              background: '#F59E0B',
              display: 'flex',
            }}
          />
          Hecho en Perú · OSE en trámite ante SUNAT
        </div>
        <div
          style={{
            fontSize: 132,
            fontWeight: 800,
            lineHeight: 1.0,
            letterSpacing: -3,
            display: 'flex',
          }}
        >
          TukiFact
        </div>
        <div
          style={{
            fontSize: 52,
            fontWeight: 600,
            marginTop: 20,
            color: '#E2E8F0',
            lineHeight: 1.15,
            display: 'flex',
            flexDirection: 'column',
          }}
        >
          <span>Facturación electrónica</span>
          <span>SUNAT para Perú</span>
        </div>
        <div style={{ flex: 1, display: 'flex' }} />
        <div
          style={{
            display: 'flex',
            gap: 28,
            fontSize: 28,
            color: '#94A3B8',
            marginTop: 32,
          }}
        >
          <span>Facturas</span>
          <span>·</span>
          <span>Boletas</span>
          <span>·</span>
          <span>Guías 2.0</span>
          <span>·</span>
          <span>API · IA</span>
        </div>
        <div
          style={{
            position: 'absolute',
            right: 96,
            bottom: 72,
            fontSize: 28,
            color: '#FACC15',
            fontWeight: 600,
            display: 'flex',
          }}
        >
          tukifact.com.pe
        </div>
      </div>
    ),
    { ...size },
  );
}
