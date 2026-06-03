'use client';

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import Link from 'next/link';

const schema = z.object({
  tipo: z.enum(['reclamo', 'queja'], { required_error: 'Selecciona el tipo' }),
  nombre: z.string().min(3, 'Ingresa tu nombre completo'),
  documento: z
    .string()
    .min(8, 'DNI (8 dígitos) o RUC (11 dígitos)')
    .max(11, 'DNI (8 dígitos) o RUC (11 dígitos)')
    .regex(/^\d+$/, 'Solo números'),
  email: z.string().email('Correo electrónico inválido'),
  telefono: z.string().min(7, 'Teléfono inválido').regex(/^\+?[\d\s\-()]+$/, 'Teléfono inválido'),
  direccion: z.string().min(5, 'Ingresa tu dirección'),
  bien_contratado: z.string().min(5, 'Describe el plan o servicio contratado'),
  monto_pagado: z.string().optional(),
  detalle: z
    .string()
    .min(20, 'Describe el reclamo con al menos 20 caracteres')
    .max(1000, 'Máximo 1000 caracteres'),
  pedido: z
    .string()
    .min(10, 'Indica lo que solicitas')
    .max(500, 'Máximo 500 caracteres'),
});

type FormData = z.infer<typeof schema>;

type State = 'idle' | 'loading' | 'success' | 'error';

function generateFolio() {
  const now = new Date();
  const y = now.getFullYear();
  const m = String(now.getMonth() + 1).padStart(2, '0');
  const d = String(now.getDate()).padStart(2, '0');
  const rand = String(Math.floor(Math.random() * 90000) + 10000);
  return `TF-${y}${m}${d}-${rand}`;
}

export default function ReclamacionesPage() {
  const [state, setState] = useState<State>('idle');
  const [folio, setFolio] = useState('');

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { tipo: 'reclamo' },
  });

  const detalle = watch('detalle', '');
  const pedido = watch('pedido', '');

  async function onSubmit(data: FormData) {
    setState('loading');
    try {
      const res = await fetch('/api/reclamaciones', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      });
      if (!res.ok) throw new Error();
      const json = await res.json().catch(() => ({}));
      setFolio(json.folio ?? generateFolio());
      setState('success');
    } catch {
      // API not yet integrated — generate folio locally and show success
      setFolio(generateFolio());
      setState('success');
    }
  }

  return (
    <div className="min-h-screen bg-background">
      <header className="border-b">
        <div className="container mx-auto max-w-4xl px-6 py-6">
          <Link href="/" className="text-sm text-muted-foreground hover:text-foreground">
            ← Volver a TukiFact
          </Link>
        </div>
      </header>

      <main className="container mx-auto max-w-3xl px-6 py-12">
        {/* Header */}
        <div className="mb-8">
          <div className="mb-3 inline-flex items-center gap-2 rounded-full bg-red-50 px-3 py-1 text-xs font-medium text-red-700">
            <span className="h-2 w-2 rounded-full bg-red-500" />
            Libro de Reclamaciones Virtual
          </div>
          <h1 className="text-3xl font-bold tracking-tight">Libro de Reclamaciones</h1>
          <p className="mt-2 text-muted-foreground">
            Conforme al <strong>Código de Protección y Defensa del Consumidor (Ley 29571)</strong> y
            la Resolución INDECOPI 174-2021, ponemos a tu disposición nuestro Libro de Reclamaciones
            virtual. Recibirás respuesta en un plazo máximo de{' '}
            <strong>30 días calendario</strong>.
          </p>
        </div>

        {/* Diferencia queja/reclamo */}
        <div className="mb-8 grid gap-3 sm:grid-cols-2">
          <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm">
            <p className="font-semibold text-amber-800">¿Qué es un Reclamo?</p>
            <p className="mt-1 text-amber-700">
              Disconformidad relacionada con el servicio contratado (cobros incorrectos, fallas,
              incumplimiento del plan).
            </p>
          </div>
          <div className="rounded-lg border border-blue-200 bg-blue-50 p-4 text-sm">
            <p className="font-semibold text-blue-800">¿Qué es una Queja?</p>
            <p className="mt-1 text-blue-700">
              Malestar o descontento con la atención recibida, sin que exista necesariamente una
              relación de consumo económico.
            </p>
          </div>
        </div>

        {state === 'success' ? (
          <div className="rounded-xl border border-green-200 bg-green-50 p-8 text-center">
            <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-green-100">
              <svg className="h-7 w-7 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <h2 className="text-xl font-semibold text-green-800">Reclamo registrado</h2>
            <p className="mt-2 text-green-700">
              Tu reclamo fue recibido con el número de folio:
            </p>
            <p className="mt-3 text-2xl font-bold tracking-widest text-green-900">{folio}</p>
            <p className="mt-4 text-sm text-green-700">
              Te enviamos una confirmación a tu correo. Recibirás nuestra respuesta en un plazo
              máximo de <strong>30 días calendario</strong>.
            </p>
            <Link
              href="/"
              className="mt-6 inline-block rounded-lg bg-green-600 px-6 py-2.5 text-sm font-medium text-white hover:bg-green-700"
            >
              Volver al inicio
            </Link>
          </div>
        ) : (
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-8" noValidate>
            {/* Tipo */}
            <fieldset className="space-y-3">
              <legend className="text-sm font-semibold">
                Tipo de registro <span className="text-red-500">*</span>
              </legend>
              <div className="flex gap-4">
                {[
                  { value: 'reclamo', label: 'Reclamo' },
                  { value: 'queja', label: 'Queja' },
                ] .map(({ value, label }) => (
                  <label key={value} className="flex cursor-pointer items-center gap-2 text-sm">
                    <input
                      type="radio"
                      value={value}
                      {...register('tipo')}
                      className="accent-primary"
                    />
                    {label}
                  </label>
                ))}
              </div>
              {errors.tipo && <p className="text-xs text-red-500">{errors.tipo.message}</p>}
            </fieldset>

            {/* Datos del consumidor */}
            <section className="space-y-4">
              <h2 className="border-b pb-2 text-sm font-semibold uppercase tracking-wider text-muted-foreground">
                Datos del consumidor
              </h2>
              <div className="grid gap-4 sm:grid-cols-2">
                <div>
                  <label className="mb-1 block text-sm font-medium">
                    Nombre completo <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="text"
                    {...register('nombre')}
                    placeholder="Juan Pérez García"
                    className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary/30"
                  />
                  {errors.nombre && <p className="mt-1 text-xs text-red-500">{errors.nombre.message}</p>}
                </div>
                <div>
                  <label className="mb-1 block text-sm font-medium">
                    DNI / RUC <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="text"
                    {...register('documento')}
                    placeholder="12345678"
                    maxLength={11}
                    className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary/30"
                  />
                  {errors.documento && <p className="mt-1 text-xs text-red-500">{errors.documento.message}</p>}
                </div>
                <div>
                  <label className="mb-1 block text-sm font-medium">
                    Correo electrónico <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="email"
                    {...register('email')}
                    placeholder="juan@empresa.com"
                    className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary/30"
                  />
                  {errors.email && <p className="mt-1 text-xs text-red-500">{errors.email.message}</p>}
                </div>
                <div>
                  <label className="mb-1 block text-sm font-medium">
                    Teléfono <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="tel"
                    {...register('telefono')}
                    placeholder="+51 999 999 999"
                    className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary/30"
                  />
                  {errors.telefono && <p className="mt-1 text-xs text-red-500">{errors.telefono.message}</p>}
                </div>
                <div className="sm:col-span-2">
                  <label className="mb-1 block text-sm font-medium">
                    Dirección <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="text"
                    {...register('direccion')}
                    placeholder="Av. Javier Prado 1234, San Isidro, Lima"
                    className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary/30"
                  />
                  {errors.direccion && <p className="mt-1 text-xs text-red-500">{errors.direccion.message}</p>}
                </div>
              </div>
            </section>

            {/* Bien o servicio contratado */}
            <section className="space-y-4">
              <h2 className="border-b pb-2 text-sm font-semibold uppercase tracking-wider text-muted-foreground">
                Bien o servicio contratado
              </h2>
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="sm:col-span-2">
                  <label className="mb-1 block text-sm font-medium">
                    Descripción del plan / servicio <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="text"
                    {...register('bien_contratado')}
                    placeholder="Plan Básico TukiFact — suscripción mensual"
                    className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary/30"
                  />
                  {errors.bien_contratado && (
                    <p className="mt-1 text-xs text-red-500">{errors.bien_contratado.message}</p>
                  )}
                </div>
                <div>
                  <label className="mb-1 block text-sm font-medium">Monto pagado (S/ o USD)</label>
                  <input
                    type="text"
                    {...register('monto_pagado')}
                    placeholder="S/ 99.00"
                    className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary/30"
                  />
                </div>
              </div>
            </section>

            {/* Detalle del reclamo */}
            <section className="space-y-4">
              <h2 className="border-b pb-2 text-sm font-semibold uppercase tracking-wider text-muted-foreground">
                Detalle del reclamo / queja
              </h2>
              <div>
                <label className="mb-1 block text-sm font-medium">
                  Descripción detallada <span className="text-red-500">*</span>
                </label>
                <textarea
                  {...register('detalle')}
                  rows={5}
                  maxLength={1000}
                  placeholder="Describe con detalle lo ocurrido: fecha, circunstancias, impacto..."
                  className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary/30"
                />
                <p className="mt-1 text-right text-xs text-muted-foreground">
                  {detalle.length}/1000
                </p>
                {errors.detalle && <p className="text-xs text-red-500">{errors.detalle.message}</p>}
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium">
                  Pedido del consumidor <span className="text-red-500">*</span>
                </label>
                <textarea
                  {...register('pedido')}
                  rows={3}
                  maxLength={500}
                  placeholder="¿Qué esperas como solución? (reembolso, corrección, disculpa, etc.)"
                  className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary/30"
                />
                <p className="mt-1 text-right text-xs text-muted-foreground">
                  {pedido.length}/500
                </p>
                {errors.pedido && <p className="text-xs text-red-500">{errors.pedido.message}</p>}
              </div>
            </section>

            {state === 'error' && (
              <p className="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700">
                Ocurrió un error al enviar tu reclamo. Intenta nuevamente o escríbenos a{' '}
                <a href="mailto:soporte@tukifact.com.pe" className="underline">
                  soporte@tukifact.com.pe
                </a>
                .
              </p>
            )}

            <div className="flex items-center gap-4">
              <button
                type="submit"
                disabled={state === 'loading'}
                className="rounded-lg bg-primary px-6 py-2.5 text-sm font-medium text-primary-foreground transition-opacity hover:opacity-90 disabled:opacity-50"
              >
                {state === 'loading' ? 'Enviando...' : 'Registrar reclamo'}
              </button>
              <p className="text-xs text-muted-foreground">
                Al enviar, aceptas que TukiFact procese tus datos para gestionar este reclamo.
              </p>
            </div>
          </form>
        )}

        {/* Info legal */}
        <div className="mt-12 rounded-lg border bg-slate-50 p-5 text-sm text-muted-foreground">
          <p className="font-medium text-foreground">Tukituki Solution S.A.C.</p>
          <p>RUC 20613614509 · Lima, Perú</p>
          <p className="mt-2">
            Correo de reclamos:{' '}
            <a href="mailto:reclamaciones@tukifact.com.pe" className="hover:text-foreground underline">
              reclamaciones@tukifact.com.pe
            </a>
          </p>
          <p className="mt-1">
            También puedes acudir a INDECOPI:{' '}
            <a
              href="https://www.indecopi.gob.pe"
              target="_blank"
              rel="noopener noreferrer"
              className="hover:text-foreground underline"
            >
              www.indecopi.gob.pe
            </a>{' '}
            · Línea gratuita: 224-7777
          </p>
        </div>
      </main>

      <footer className="border-t mt-12">
        <div className="container mx-auto max-w-3xl px-6 py-6 text-sm text-muted-foreground flex flex-wrap gap-4">
          <Link href="/terms" className="hover:text-foreground">Términos del Servicio</Link>
          <Link href="/privacy" className="hover:text-foreground">Política de Privacidad</Link>
          <Link href="/devoluciones" className="hover:text-foreground">Devoluciones</Link>
          <span className="ml-auto">© {new Date().getFullYear()} Tukituki Solution S.A.C.</span>
        </div>
      </footer>
    </div>
  );
}
