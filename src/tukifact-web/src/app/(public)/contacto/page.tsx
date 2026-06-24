import type { Metadata } from 'next';
import { Mail, MessageCircle, MapPin } from 'lucide-react';
import { ContactForm } from './ContactForm';
import { BreadcrumbJsonLd, ContactPageJsonLd, TUKIFACT_BRAND } from '@/components/seo/jsonld';

export const metadata: Metadata = {
  title: 'Contacto — Conversemos',
  description:
    'Conversemos. Resolvemos tus dudas sobre TukiFact, planes empresariales o integración con tu ERP. Email, WhatsApp y formulario directo. Respuesta en <24h hábiles.',
  alternates: { canonical: '/contacto' },
};

export default function ContactoPage() {
  return (
    <>
      <BreadcrumbJsonLd
        items={[
          { name: 'Inicio', url: `${TUKIFACT_BRAND.url}/` },
          { name: 'Contacto', url: `${TUKIFACT_BRAND.url}/contacto` },
        ]}
      />
      <ContactPageJsonLd />
      <section className="border-b border-border bg-gradient-to-br from-background to-slate-50 py-20">
        <div className="mx-auto max-w-3xl px-6 text-center">
          <h1 className="t-display-xl text-foreground">Hablemos</h1>
          <p className="mt-4 text-lg text-slate-600">
            Cuéntanos qué necesitas. Te respondemos en menos de un día hábil.
          </p>
        </div>
      </section>

      <section className="py-20">
        <div className="mx-auto grid max-w-6xl gap-12 px-6 lg:grid-cols-[2fr_1fr]">
          <ContactForm />

          <aside className="space-y-6">
            <div className="rounded-2xl border border-border bg-card p-6">
              <Mail className="h-5 w-5 text-[var(--brand-toucan-orange)]" />
              <h3 className="mt-3 font-semibold text-foreground">Email</h3>
              <p className="mt-1 text-sm text-slate-600">
                <a href="mailto:administration@tukisolutions.com" className="hover:text-foreground">
                  administration@tukisolutions.com
                </a>
              </p>
              <p className="mt-1 text-sm text-slate-600">
                <a href="mailto:hola@tukifact.com.pe" className="hover:text-foreground">
                  hola@tukifact.com.pe
                </a>
              </p>
              <p className="mt-1 text-sm text-slate-600">
                <a href="mailto:ventas@tukifact.com.pe" className="hover:text-foreground">
                  ventas@tukifact.com.pe
                </a>
              </p>
            </div>

            <div className="rounded-2xl border border-border bg-card p-6">
              <MessageCircle className="h-5 w-5 text-[var(--brand-toucan-orange)]" />
              <h3 className="mt-3 font-semibold text-foreground">WhatsApp</h3>
              <p className="mt-1 text-sm text-slate-600">
                <a
                  href="https://wa.me/51966388258"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="hover:text-foreground"
                >
                  +51 966 388 258
                </a>
              </p>
              <p className="mt-1 text-xs text-slate-500">L–V 9:00 a 18:00 PET</p>
            </div>

            <div className="rounded-2xl border border-border bg-card p-6">
              <MapPin className="h-5 w-5 text-[var(--brand-toucan-orange)]" />
              <h3 className="mt-3 font-semibold text-foreground">Domicilio fiscal</h3>
              <p className="mt-1 text-sm text-slate-600">
                Pasaje Carabaya 105, Urb. Alto Libertad
                <br />
                Cerro Colorado, Arequipa, Perú
              </p>
              <p className="mt-1 text-xs text-slate-500">Atención solo con cita previa</p>
            </div>
          </aside>
        </div>
      </section>
    </>
  );
}
