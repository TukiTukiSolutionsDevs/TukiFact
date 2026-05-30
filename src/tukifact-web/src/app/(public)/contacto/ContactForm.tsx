'use client';

import { useState } from 'react';
import { toast } from 'sonner';
import { Loader2, Send, CheckCircle2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';

const REASONS = [
  { value: 'ventas', label: 'Cotización empresarial' },
  { value: 'integracion', label: 'Integración / API' },
  { value: 'soporte', label: 'Soporte técnico' },
  { value: 'general', label: 'Consulta general' },
];

export function ContactForm() {
  const [submitting, setSubmitting] = useState(false);
  const [done, setDone] = useState(false);

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const fd = new FormData(e.currentTarget);
    const payload = {
      name: String(fd.get('name') ?? '').trim(),
      email: String(fd.get('email') ?? '').trim(),
      company: String(fd.get('company') ?? '').trim(),
      phone: String(fd.get('phone') ?? '').trim(),
      reason: String(fd.get('reason') ?? 'general'),
      message: String(fd.get('message') ?? '').trim(),
    };

    if (!payload.name || !payload.email || !payload.message) {
      toast.error('Completa los campos requeridos.');
      return;
    }

    setSubmitting(true);
    try {
      const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5186';
      const res = await fetch(`${apiUrl}/v1/leads`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });
      if (!res.ok) {
        const detail = await res.text();
        throw new Error(detail || 'Error al enviar.');
      }
      setDone(true);
      toast.success('Mensaje enviado. Te respondemos pronto.');
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Error al enviar.';
      toast.error(msg);
    } finally {
      setSubmitting(false);
    }
  }

  if (done) {
    return (
      <div className="rounded-2xl border border-border bg-card p-10 text-center">
        <CheckCircle2 className="mx-auto h-10 w-10 text-[var(--success,oklch(0.66_0.14_152))]" />
        <h2 className="mt-4 text-xl font-semibold text-foreground">¡Gracias por escribirnos!</h2>
        <p className="mt-2 text-slate-600">
          Recibimos tu mensaje. Te respondemos al correo que indicaste en menos de un día hábil.
        </p>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="rounded-2xl border border-border bg-card p-8">
      <div className="grid gap-5 sm:grid-cols-2">
        <div>
          <Label htmlFor="name">Nombre completo *</Label>
          <Input id="name" name="name" required placeholder="Juan Pérez" className="mt-1.5" />
        </div>
        <div>
          <Label htmlFor="email">Email *</Label>
          <Input id="email" name="email" type="email" required placeholder="juan@empresa.com" className="mt-1.5" />
        </div>
        <div>
          <Label htmlFor="company">Empresa</Label>
          <Input id="company" name="company" placeholder="Mi Empresa SAC" className="mt-1.5" />
        </div>
        <div>
          <Label htmlFor="phone">Teléfono</Label>
          <Input id="phone" name="phone" type="tel" placeholder="+51 999 999 999" className="mt-1.5" />
        </div>
      </div>

      <div className="mt-5">
        <Label htmlFor="reason">Motivo</Label>
        <select
          id="reason"
          name="reason"
          defaultValue="general"
          className="mt-1.5 flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-xs ring-offset-background focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
        >
          {REASONS.map((r) => (
            <option key={r.value} value={r.value}>
              {r.label}
            </option>
          ))}
        </select>
      </div>

      <div className="mt-5">
        <Label htmlFor="message">Mensaje *</Label>
        <Textarea
          id="message"
          name="message"
          required
          rows={5}
          placeholder="¿En qué te ayudamos?"
          className="mt-1.5"
        />
      </div>

      <Button type="submit" size="lg" className="mt-6 w-full gap-2 bg-foreground text-background hover:bg-foreground/90" disabled={submitting}>
        {submitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
        {submitting ? 'Enviando…' : 'Enviar mensaje'}
      </Button>

      <p className="mt-4 text-xs text-slate-500">
        Al enviar este formulario aceptas nuestra{' '}
        <a href="/privacy" className="underline hover:text-foreground">
          política de privacidad
        </a>
        .
      </p>
    </form>
  );
}
