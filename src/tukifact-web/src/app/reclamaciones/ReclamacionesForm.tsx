'use client';

import { useState } from 'react';

type Estado = 'idle' | 'enviando' | 'exito' | 'error';

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'https://api.tukifact.com.pe';

export function ReclamacionesForm() {
  const [estado, setEstado] = useState<Estado>('idle');
  const [tracking, setTracking] = useState('');
  const [errorMsg, setErrorMsg] = useState('');

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setEstado('enviando');
    setErrorMsg('');

    const form = e.currentTarget;
    const get = (name: string) =>
      (form.elements.namedItem(name) as HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement)?.value ?? '';

    const body = {
      tipo: get('tipo'),
      nombre: get('nombre'),
      documento: get('documento'),
      email: get('email'),
      telefono: get('telefono'),
      bien: get('bien'),
      descripcion: get('descripcion'),
      pedido: get('pedido'),
    };

    try {
      const res = await fetch(`${API_URL}/v1/public/reclamaciones`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        throw new Error((err as { error?: string }).error ?? 'Error al enviar');
      }
      const data = (await res.json()) as { trackingNumber: string };
      setTracking(data.trackingNumber ?? '');
      setEstado('exito');
    } catch (err) {
      setErrorMsg(
        err instanceof Error
          ? err.message
          : 'Ocurrió un error. Por favor inténtalo de nuevo o escríbenos a soporte@tukifact.com.pe.',
      );
      setEstado('error');
    }
  }

  if (estado === 'exito') {
    return (
      <div className="rounded-lg border border-green-200 bg-green-50 p-8 text-center">
        <div className="text-4xl mb-4">✅</div>
        <h2 className="text-xl font-semibold text-green-800 mb-2">Reclamo registrado</h2>
        <p className="text-green-700 mb-4">
          Tu reclamo fue registrado exitosamente. Te responderemos dentro de <strong>30 días hábiles</strong> al
          correo indicado.
        </p>
        {tracking && (
          <p className="text-sm text-green-600">
            Número de seguimiento: <strong className="font-mono">{tracking}</strong>
          </p>
        )}
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="grid gap-4 sm:grid-cols-2">
        <div>
          <label htmlFor="tipo" className="block text-sm font-medium mb-1.5">
            Tipo de reclamo <span className="text-red-500">*</span>
          </label>
          <select
            id="tipo"
            name="tipo"
            required
            className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
          >
            <option value="">Selecciona…</option>
            <option value="reclamo">Reclamo (disconformidad con el servicio contratado)</option>
            <option value="queja">Queja (malestar o descontento sin afectación directa)</option>
          </select>
        </div>

        <div>
          <label htmlFor="bien" className="block text-sm font-medium mb-1.5">
            Producto / Servicio <span className="text-red-500">*</span>
          </label>
          <select
            id="bien"
            name="bien"
            required
            className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
          >
            <option value="">Selecciona…</option>
            <option value="Suscripción TukiFact">Suscripción TukiFact</option>
            <option value="Emisión de comprobantes">Emisión de comprobantes electrónicos</option>
            <option value="Soporte técnico">Soporte técnico</option>
            <option value="Facturación / Cobros">Facturación / Cobros</option>
            <option value="Otro">Otro</option>
          </select>
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div>
          <label htmlFor="nombre" className="block text-sm font-medium mb-1.5">
            Nombre completo <span className="text-red-500">*</span>
          </label>
          <input
            id="nombre"
            name="nombre"
            type="text"
            required
            placeholder="Juan Pérez García"
            className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
          />
        </div>

        <div>
          <label htmlFor="documento" className="block text-sm font-medium mb-1.5">
            DNI / CE / RUC <span className="text-red-500">*</span>
          </label>
          <input
            id="documento"
            name="documento"
            type="text"
            required
            placeholder="12345678"
            className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
          />
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div>
          <label htmlFor="email" className="block text-sm font-medium mb-1.5">
            Correo electrónico <span className="text-red-500">*</span>
          </label>
          <input
            id="email"
            name="email"
            type="email"
            required
            placeholder="juan@empresa.com"
            className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
          />
        </div>

        <div>
          <label htmlFor="telefono" className="block text-sm font-medium mb-1.5">
            Teléfono
          </label>
          <input
            id="telefono"
            name="telefono"
            type="tel"
            placeholder="+51 999 999 999"
            className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
          />
        </div>
      </div>

      <div>
        <label htmlFor="descripcion" className="block text-sm font-medium mb-1.5">
          Descripción detallada del reclamo <span className="text-red-500">*</span>
        </label>
        <textarea
          id="descripcion"
          name="descripcion"
          required
          rows={5}
          minLength={20}
          placeholder="Describe con detalle lo ocurrido, incluyendo fechas, importes y cualquier información relevante…"
          className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring resize-y"
        />
      </div>

      <div>
        <label htmlFor="pedido" className="block text-sm font-medium mb-1.5">
          Pedido o solución que solicitas
        </label>
        <textarea
          id="pedido"
          name="pedido"
          rows={3}
          placeholder="Ej.: reembolso del cobro duplicado, corrección del documento, respuesta de soporte, etc."
          className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring resize-y"
        />
      </div>

      {estado === 'error' && (
        <p className="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{errorMsg}</p>
      )}

      <button
        type="submit"
        disabled={estado === 'enviando'}
        className="w-full rounded-md bg-primary px-6 py-3 text-sm font-semibold text-primary-foreground transition-opacity hover:opacity-90 disabled:opacity-50"
      >
        {estado === 'enviando' ? 'Enviando…' : 'Enviar reclamo'}
      </button>

      <p className="text-xs text-muted-foreground text-center">
        Al enviar este formulario aceptas nuestra{' '}
        <a href="/privacy" className="underline hover:text-foreground">Política de Privacidad</a>.
        Los datos serán usados únicamente para gestionar tu reclamo.
      </p>
    </form>
  );
}
