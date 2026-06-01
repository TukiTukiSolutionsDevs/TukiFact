'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Plus,
  Trash2,
  Search,
  Loader2,
  Building2,
  User as UserIcon,
  Globe,
  CircleSlash,
  Calendar,
  Truck,
  Bus,
  MapPin,
  Hash,
  Send,
  PackageCheck,
} from 'lucide-react';
import { toast } from 'sonner';
import { cn } from '@/lib/utils';

interface ItemForm {
  productCode: string;
  description: string;
  quantity: string;
  unitCode: string;
}

const emptyItem = (): ItemForm => ({
  productCode: '',
  description: '',
  quantity: '1',
  unitCode: 'NIU',
});

const CUSTOMER_DOC_TYPES = [
  { value: '6', label: 'RUC', sub: '11 dígitos', icon: Building2, length: 11, placeholder: '20XXXXXXXXX' },
  { value: '1', label: 'DNI', sub: '8 dígitos', icon: UserIcon, length: 8, placeholder: '4XXXXXXX' },
  { value: '4', label: 'CE', sub: 'Carné extranjería', icon: Globe, length: 12, placeholder: 'CE12345...' },
  { value: '7', label: 'Pasaporte', sub: 'Internacional', icon: Globe, length: 12, placeholder: 'P12345...' },
  { value: '0', label: 'Sin doc.', sub: 'Cliente eventual', icon: CircleSlash, length: 0, placeholder: '' },
] as const;

const TRANSFER_REASONS: { code: string; label: string }[] = [
  { code: '01', label: 'Venta' },
  { code: '02', label: 'Compra' },
  { code: '04', label: 'Traslado entre establecimientos de la misma empresa' },
  { code: '08', label: 'Importación' },
  { code: '09', label: 'Exportación' },
  { code: '13', label: 'Otros' },
  { code: '14', label: 'Venta sujeta a confirmación del comprador' },
  { code: '18', label: 'Traslado emisor itinerante' },
];

const TRANSPORT_MODES = [
  {
    value: '02',
    label: 'Transporte privado',
    sub: 'Tu propio vehículo / personal',
    icon: Truck,
  },
  {
    value: '01',
    label: 'Transporte público',
    sub: 'Contratas a un transportista',
    icon: Bus,
  },
] as const;

interface LookupStatus {
  configured: boolean;
  provider: string;
  providerName: string;
}

function defaultDate() {
  return new Date().toISOString().slice(0, 10);
}

function PillGroup<T extends string>({
  value,
  onChange,
  options,
}: {
  value: T;
  onChange: (v: T) => void;
  options: readonly { value: T; label: string; sub?: string; icon: React.ElementType }[];
}) {
  return (
    <div className="grid grid-cols-2 gap-2">
      {options.map((o) => {
        const Icon = o.icon;
        const active = value === o.value;
        return (
          <button
            key={o.value}
            type="button"
            onClick={() => onChange(o.value)}
            className={cn(
              'relative flex items-center gap-2.5 rounded-[var(--radius-md)] border px-3 py-2.5 transition-colors text-left min-w-0'
            )}
            style={{
              background: active ? 'color-mix(in oklch, var(--accent) 18%, transparent)' : 'var(--card)',
              borderColor: active ? 'var(--accent)' : 'var(--border)',
            }}
          >
            <span
              className="flex h-8 w-8 items-center justify-center rounded-md shrink-0"
              style={{
                background: active ? 'var(--accent)' : 'var(--muted)',
                color: active ? 'var(--accent-foreground)' : 'var(--muted-foreground)',
              }}
            >
              <Icon className="h-4 w-4" />
            </span>
            <span className="min-w-0 flex-1 whitespace-normal">
              <span className="block t-body-sm font-semibold leading-tight whitespace-normal">
                {o.label}
              </span>
              {o.sub && (
                <span
                  className="block t-caption leading-tight mt-0.5 whitespace-normal"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  {o.sub}
                </span>
              )}
            </span>
          </button>
        );
      })}
    </div>
  );
}

function Section({
  title,
  desc,
  right,
  children,
}: {
  title: string;
  desc?: string;
  right?: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <section
      className="rounded-[var(--radius-lg)] border bg-card p-6"
      style={{ boxShadow: 'var(--shadow-xs)' }}
    >
      <div className="flex items-start justify-between gap-3 mb-4">
        <div>
          <h2 className="t-h2 m-0">{title}</h2>
          {desc && (
            <p className="t-body-sm m-0 mt-0.5" style={{ color: 'var(--muted-foreground)' }}>
              {desc}
            </p>
          )}
        </div>
        {right}
      </div>
      {children}
    </section>
  );
}

export default function NewDespatchAdvicePage() {
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSearching, setIsSearching] = useState(false);
  const [lookupStatus, setLookupStatus] = useState<LookupStatus | null>(null);
  const [form, setForm] = useState({
    documentType: '09',
    serie: 'T001',
    recipientDocType: '6',
    recipientDocNumber: '',
    recipientName: '',
    recipientAddress: '',
    transferReasonCode: '01',
    transferReasonDescription: 'Venta',
    transferStartDate: defaultDate(),
    transportMode: '02' as '01' | '02',
    originAddress: '',
    originUbigeo: '',
    destinationAddress: '',
    destinationUbigeo: '',
    grossWeight: '',
    totalPackages: '1',
    vehiclePlate: '',
    driverDocType: '1',
    driverDocNumber: '',
    driverName: '',
    driverLicense: '',
    carrierDocType: '6',
    carrierDocNumber: '',
    carrierName: '',
    note: '',
  });
  const [items, setItems] = useState<ItemForm[]>([emptyItem()]);

  useEffect(() => {
    api.get<LookupStatus>('/v1/services/lookup/status').then(setLookupStatus).catch(() => {});
  }, []);

  const set = (key: string, value: string | null) => {
    if (value !== null) setForm((f) => ({ ...f, [key]: value }));
  };

  const setItem = (idx: number, key: string, value: string | null) => {
    if (value === null) return;
    setItems((prev) => prev.map((it, i) => (i === idx ? { ...it, [key]: value } : it)));
  };

  const addItem = () => setItems((prev) => [...prev, emptyItem()]);
  const removeItem = (idx: number) => {
    if (items.length <= 1) return;
    setItems((prev) => prev.filter((_, i) => i !== idx));
  };

  const selectedDocType =
    CUSTOMER_DOC_TYPES.find((d) => d.value === form.recipientDocType) ?? CUSTOMER_DOC_TYPES[0];

  const lookup = async () => {
    if (!(form.recipientDocType === '6' || form.recipientDocType === '1')) return;
    const endpoint = form.recipientDocType === '6' ? 'ruc' : 'dni';
    const expectedLen = form.recipientDocType === '6' ? 11 : 8;
    if (form.recipientDocNumber.length !== expectedLen) {
      toast.error(`El número debe tener ${expectedLen} dígitos`);
      return;
    }
    setIsSearching(true);
    try {
      const data = await api.get<{
        name?: string;
        fullName?: string;
        firstName?: string;
        lastName?: string;
        motherLastName?: string;
        address?: string;
      }>(`/v1/services/lookup/${endpoint}/${form.recipientDocNumber}`);
      const name =
        data.name ||
        data.fullName ||
        [data.firstName, data.lastName, data.motherLastName].filter(Boolean).join(' ') ||
        '';
      if (!name) {
        toast.error('No se encontraron datos para ese número');
        return;
      }
      setForm((f) => ({
        ...f,
        recipientName: name,
        recipientAddress: data.address ?? f.recipientAddress,
        destinationAddress: f.destinationAddress || data.address || f.destinationAddress,
      }));
      toast.success(`Datos encontrados: ${name}`);
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Error al consultar datos';
      if (msg.includes('No hay proveedor')) {
        toast.error('Configura un proveedor de datos en Configuración → Servicios Externos');
      } else {
        toast.error(msg);
      }
    } finally {
      setIsSearching(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.recipientDocNumber || !form.recipientName) {
      toast.error('Completa los datos del destinatario');
      return;
    }
    if (!form.originAddress || !form.originUbigeo) {
      toast.error('Completa la dirección y ubigeo de origen');
      return;
    }
    if (!form.destinationAddress || !form.destinationUbigeo) {
      toast.error('Completa la dirección y ubigeo de destino');
      return;
    }
    if (items.some((i) => !i.description)) {
      toast.error('Todos los items necesitan descripción');
      return;
    }

    setIsSubmitting(true);
    try {
      const body = {
        documentType: form.documentType,
        serie: form.serie,
        recipientDocType: form.recipientDocType,
        recipientDocNumber: form.recipientDocNumber,
        recipientName: form.recipientName,
        transferReasonCode: form.transferReasonCode,
        transferReasonDescription: form.transferReasonDescription,
        transferStartDate: form.transferStartDate,
        transportMode: form.transportMode,
        originAddress: form.originAddress,
        originUbigeo: form.originUbigeo,
        destinationAddress: form.destinationAddress,
        destinationUbigeo: form.destinationUbigeo,
        grossWeight: parseFloat(form.grossWeight) || 0,
        totalPackages: parseInt(form.totalPackages) || 1,
        vehiclePlate: form.vehiclePlate || null,
        driverDocType: form.transportMode === '02' ? form.driverDocType : null,
        driverDocNumber: form.transportMode === '02' ? form.driverDocNumber : null,
        driverName: form.transportMode === '02' ? form.driverName : null,
        driverLicense:
          form.transportMode === '02' ? form.driverLicense || null : null,
        carrierDocType: form.transportMode === '01' ? form.carrierDocType : null,
        carrierDocNumber:
          form.transportMode === '01' ? form.carrierDocNumber : null,
        carrierName: form.transportMode === '01' ? form.carrierName : null,
        note: form.note || null,
        items: items.map((i, idx) => ({
          lineNumber: idx + 1,
          productCode: i.productCode || null,
          description: i.description,
          quantity: parseFloat(i.quantity),
          unitCode: i.unitCode,
        })),
      };
      const res = await api.post<{ id: string }>('/v1/despatch-advices', body);
      toast.success('Guía de remisión creada como borrador');
      router.push(`/despatch-advices/${res.id}`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al crear');
    } finally {
      setIsSubmitting(false);
    }
  };

  const selectedReason = TRANSFER_REASONS.find((r) => r.code === form.transferReasonCode);
  const selectedMode = TRANSPORT_MODES.find((m) => m.value === form.transportMode);

  const totalItems = items.length;
  const totalQty = items.reduce((s, i) => s + (parseFloat(i.quantity) || 0), 0);

  return (
    <form onSubmit={handleSubmit}>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0">Nueva guía de remisión</h1>
          <p className="t-body mt-1.5 mb-0" style={{ color: 'var(--muted-foreground)' }}>
            Documenta el traslado de mercadería desde el origen al destino.
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-[var(--gap-cards)]">
        {/* Left: form */}
        <div className="lg:col-span-2 flex flex-col gap-[var(--gap-cards)]">
          {/* Destinatario */}
          <Section
            title="Destinatario"
            desc="Quién recibe la mercadería al final del traslado."
            right={
              lookupStatus?.configured ? (
                <span
                  className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 t-caption"
                  style={{ background: 'var(--muted)', color: 'var(--muted-foreground)' }}
                >
                  <Search className="h-3 w-3" />
                  {lookupStatus.providerName}
                </span>
              ) : undefined
            }
          >
            <PillGroup
              value={form.recipientDocType}
              onChange={(v) => set('recipientDocType', v)}
              options={CUSTOMER_DOC_TYPES}
            />

            <div className="mt-5 flex flex-col gap-4">
              {form.recipientDocType !== '0' && (
                <div>
                  <Label className="t-label mb-1.5 block">Número de documento</Label>
                  <div className="flex gap-2">
                    <Input
                      placeholder={selectedDocType.placeholder}
                      value={form.recipientDocNumber}
                      maxLength={selectedDocType.length || undefined}
                      onChange={(e) =>
                        set('recipientDocNumber', e.target.value.replace(/\s/g, ''))
                      }
                      className="mono"
                    />
                    {(form.recipientDocType === '6' || form.recipientDocType === '1') && (
                      <Button
                        type="button"
                        variant="outline"
                        disabled={isSearching || !lookupStatus?.configured}
                        title={
                          lookupStatus?.configured
                            ? `Buscar con ${lookupStatus.providerName}`
                            : 'Configura un proveedor en Ajustes → Servicios Externos'
                        }
                        onClick={lookup}
                      >
                        {isSearching ? (
                          <>
                            <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Buscando…
                          </>
                        ) : (
                          <>
                            <Search className="h-4 w-4 mr-2" /> Buscar
                          </>
                        )}
                      </Button>
                    )}
                  </div>
                </div>
              )}

              <div>
                <Label className="t-label mb-1.5 block">
                  {form.recipientDocType === '6' ? 'Razón Social' : 'Nombre del destinatario'}
                </Label>
                <Input
                  placeholder={
                    form.recipientDocType === '6' ? 'MI EMPRESA SAC' : 'Juan Pérez García'
                  }
                  value={form.recipientName}
                  onChange={(e) => set('recipientName', e.target.value)}
                  style={form.recipientDocType === '6' ? { textTransform: 'uppercase' } : undefined}
                  required
                />
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label className="t-label mb-1.5 block">Serie</Label>
                  <Input
                    value={form.serie}
                    onChange={(e) =>
                      set('serie', e.target.value.toUpperCase().slice(0, 4))
                    }
                    maxLength={4}
                    className="mono"
                  />
                  <p className="t-caption mt-1.5" style={{ color: 'var(--muted-foreground)' }}>
                    Para guías la serie empieza con <strong>T</strong> (ej. T001).
                  </p>
                </div>
              </div>
            </div>
          </Section>

          {/* Traslado */}
          <Section
            title="Detalles del traslado"
            desc="Motivo, fecha y modalidad del transporte."
          >
            <div className="flex flex-col gap-5">
              {/* Modalidad como pills */}
              <div>
                <Label className="t-label mb-2 block">Modalidad de transporte</Label>
                <PillGroup
                  value={form.transportMode}
                  onChange={(v) => set('transportMode', v)}
                  options={TRANSPORT_MODES}
                />
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label className="t-label mb-1.5 block">Motivo del traslado</Label>
                  <Select
                    value={form.transferReasonCode}
                    onValueChange={(v) => {
                      if (!v) return;
                      set('transferReasonCode', v);
                      const r = TRANSFER_REASONS.find((x) => x.code === v);
                      if (r) set('transferReasonDescription', r.label);
                    }}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {TRANSFER_REASONS.map((r) => (
                        <SelectItem key={r.code} value={r.code}>
                          <span className="mono mr-2">{r.code}</span>
                          {r.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                <div>
                  <Label className="t-label mb-1.5 block">Fecha de inicio</Label>
                  <div className="relative">
                    <Calendar
                      className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 pointer-events-none"
                      style={{ color: 'var(--muted-foreground)' }}
                    />
                    <Input
                      type="date"
                      value={form.transferStartDate}
                      onChange={(e) => set('transferStartDate', e.target.value)}
                      className="pl-9 mono"
                      required
                    />
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label className="t-label mb-1.5 block">Peso bruto total</Label>
                  <div className="relative">
                    <Input
                      type="number"
                      step="0.01"
                      min="0"
                      value={form.grossWeight}
                      onChange={(e) => set('grossWeight', e.target.value)}
                      className="mono tnum text-right pr-12"
                      placeholder="0.00"
                      required
                    />
                    <span
                      className="absolute right-3 top-1/2 -translate-y-1/2 t-body-sm mono pointer-events-none"
                      style={{ color: 'var(--muted-foreground)' }}
                    >
                      KG
                    </span>
                  </div>
                </div>
                <div>
                  <Label className="t-label mb-1.5 block">Número de bultos</Label>
                  <Input
                    type="number"
                    min="1"
                    value={form.totalPackages}
                    onChange={(e) => set('totalPackages', e.target.value)}
                    className="mono tnum text-right"
                  />
                </div>
              </div>
            </div>
          </Section>

          {/* Origen / Destino */}
          <Section
            title="Trayecto"
            desc="Dirección y ubigeo del punto de partida y de llegada."
          >
            <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
              <div className="flex flex-col gap-3">
                <div className="flex items-center gap-2">
                  <span
                    className="flex h-7 w-7 items-center justify-center rounded-md"
                    style={{
                      background: 'color-mix(in oklch, var(--info) 14%, transparent)',
                      color: 'var(--info)',
                    }}
                  >
                    <MapPin className="h-4 w-4" />
                  </span>
                  <span className="t-h3 m-0">Origen</span>
                </div>
                <div>
                  <Label className="t-label mb-1.5 block">Dirección</Label>
                  <Input
                    value={form.originAddress}
                    onChange={(e) => set('originAddress', e.target.value)}
                    placeholder="Av. Argentina 1234, Cercado de Lima"
                    required
                  />
                </div>
                <div>
                  <Label className="t-label mb-1.5 block">Ubigeo INEI</Label>
                  <Input
                    value={form.originUbigeo}
                    onChange={(e) =>
                      set('originUbigeo', e.target.value.replace(/\D/g, '').slice(0, 6))
                    }
                    maxLength={6}
                    placeholder="150101"
                    className="mono tnum"
                    required
                  />
                  <p className="t-caption mt-1.5" style={{ color: 'var(--muted-foreground)' }}>
                    6 dígitos (Departamento-Provincia-Distrito).
                  </p>
                </div>
              </div>

              <div className="flex flex-col gap-3">
                <div className="flex items-center gap-2">
                  <span
                    className="flex h-7 w-7 items-center justify-center rounded-md"
                    style={{
                      background: 'color-mix(in oklch, var(--success) 14%, transparent)',
                      color: 'var(--success)',
                    }}
                  >
                    <PackageCheck className="h-4 w-4" />
                  </span>
                  <span className="t-h3 m-0">Destino</span>
                </div>
                <div>
                  <Label className="t-label mb-1.5 block">Dirección</Label>
                  <Input
                    value={form.destinationAddress}
                    onChange={(e) => set('destinationAddress', e.target.value)}
                    placeholder="Calle Las Begonias 555, San Isidro"
                    required
                  />
                </div>
                <div>
                  <Label className="t-label mb-1.5 block">Ubigeo INEI</Label>
                  <Input
                    value={form.destinationUbigeo}
                    onChange={(e) =>
                      set('destinationUbigeo', e.target.value.replace(/\D/g, '').slice(0, 6))
                    }
                    maxLength={6}
                    placeholder="150131"
                    className="mono tnum"
                    required
                  />
                  <p className="t-caption mt-1.5" style={{ color: 'var(--muted-foreground)' }}>
                    6 dígitos (Departamento-Provincia-Distrito).
                  </p>
                </div>
              </div>
            </div>
          </Section>

          {/* Conductor o Transportista */}
          {form.transportMode === '02' ? (
            <Section
              title="Conductor y vehículo"
              desc="Datos del chofer y del vehículo que realiza el traslado."
            >
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label className="t-label mb-1.5 block">DNI del conductor</Label>
                  <Input
                    value={form.driverDocNumber}
                    onChange={(e) =>
                      set('driverDocNumber', e.target.value.replace(/\D/g, '').slice(0, 8))
                    }
                    placeholder="4XXXXXXX"
                    className="mono"
                    maxLength={8}
                  />
                </div>
                <div>
                  <Label className="t-label mb-1.5 block">Nombre completo</Label>
                  <Input
                    value={form.driverName}
                    onChange={(e) => set('driverName', e.target.value)}
                    placeholder="Juan Pérez García"
                  />
                </div>
                <div>
                  <Label className="t-label mb-1.5 block">Número de licencia</Label>
                  <Input
                    value={form.driverLicense}
                    onChange={(e) => set('driverLicense', e.target.value.toUpperCase())}
                    placeholder="QXXXXXXXX"
                    className="mono"
                  />
                </div>
                <div>
                  <Label className="t-label mb-1.5 block">Placa del vehículo</Label>
                  <Input
                    value={form.vehiclePlate}
                    onChange={(e) => set('vehiclePlate', e.target.value.toUpperCase())}
                    placeholder="ABC-123"
                    className="mono"
                    maxLength={8}
                  />
                </div>
              </div>
            </Section>
          ) : (
            <Section
              title="Transportista"
              desc="Empresa contratada para realizar el traslado."
            >
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div>
                  <Label className="t-label mb-1.5 block">RUC del transportista</Label>
                  <Input
                    value={form.carrierDocNumber}
                    onChange={(e) =>
                      set('carrierDocNumber', e.target.value.replace(/\D/g, '').slice(0, 11))
                    }
                    placeholder="20XXXXXXXXX"
                    className="mono"
                    maxLength={11}
                  />
                </div>
                <div className="md:col-span-2">
                  <Label className="t-label mb-1.5 block">Razón Social</Label>
                  <Input
                    value={form.carrierName}
                    onChange={(e) => set('carrierName', e.target.value)}
                    placeholder="TRANSPORTES EJEMPLO SAC"
                    style={{ textTransform: 'uppercase' }}
                  />
                </div>
              </div>
            </Section>
          )}

          {/* Items */}
          <Section
            title="Mercadería a trasladar"
            desc={`${items.length} ${items.length === 1 ? 'línea' : 'líneas'}`}
            right={
              <Button type="button" variant="outline" size="sm" onClick={addItem}>
                <Plus className="h-4 w-4 mr-1.5" /> Agregar línea
              </Button>
            }
          >
            <div className="-mx-6 overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr
                    className="t-overline"
                    style={{ color: 'var(--muted-foreground)', background: 'var(--muted)' }}
                  >
                    <th className="text-left py-2.5 pl-6 pr-2 w-10">#</th>
                    <th className="text-left py-2.5 px-2 w-32">Código</th>
                    <th className="text-left py-2.5 px-2">Descripción</th>
                    <th className="text-right py-2.5 px-2 w-24">Cantidad</th>
                    <th className="text-left py-2.5 px-2 w-24">Unidad</th>
                    <th className="py-2.5 pr-6 pl-2 w-10" aria-label="acciones" />
                  </tr>
                </thead>
                <tbody>
                  {items.map((item, idx) => (
                    <tr key={idx} style={{ borderTop: '1px solid var(--border)' }}>
                      <td
                        className="py-3 pl-6 pr-2 t-body-sm tnum"
                        style={{ color: 'var(--muted-foreground)' }}
                      >
                        {idx + 1}
                      </td>
                      <td className="py-3 px-2">
                        <Input
                          value={item.productCode}
                          onChange={(e) => setItem(idx, 'productCode', e.target.value)}
                          placeholder="Opcional"
                          className="mono"
                        />
                      </td>
                      <td className="py-3 px-2">
                        <Input
                          value={item.description}
                          onChange={(e) => setItem(idx, 'description', e.target.value)}
                          placeholder="Ej. Cajas de café molido 250g"
                          required
                        />
                      </td>
                      <td className="py-3 px-2">
                        <Input
                          type="number"
                          step="any"
                          min="0.01"
                          inputMode="decimal"
                          value={item.quantity || ''}
                          onChange={(e) => setItem(idx, 'quantity', e.target.value)}
                          className="mono tnum text-right"
                        />
                      </td>
                      <td className="py-3 px-2">
                        <Input
                          value={item.unitCode}
                          onChange={(e) =>
                            setItem(idx, 'unitCode', e.target.value.toUpperCase())
                          }
                          className="mono text-center"
                          maxLength={4}
                        />
                      </td>
                      <td className="py-3 pl-2 pr-6 text-right">
                        {items.length > 1 && (
                          <button
                            type="button"
                            onClick={() => removeItem(idx)}
                            className="h-8 w-8 inline-flex items-center justify-center rounded-md hover:bg-[var(--muted)] transition-colors"
                            aria-label={`Quitar línea ${idx + 1}`}
                            style={{ color: 'var(--danger)' }}
                          >
                            <Trash2 className="h-4 w-4" />
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <button
              type="button"
              onClick={addItem}
              className="mt-4 w-full rounded-[var(--radius-md)] border-2 border-dashed py-3 t-body-sm font-medium transition-colors hover:bg-[var(--muted)] flex items-center justify-center gap-2"
              style={{ borderColor: 'var(--border)', color: 'var(--muted-foreground)' }}
            >
              <Plus className="h-4 w-4" /> Agregar otra línea
            </button>
          </Section>

          {/* Observaciones */}
          <Section title="Observaciones" desc="Opcional. Aparecerá en el PDF de la guía.">
            <Textarea
              placeholder="Ej. Mercadería frágil — transportar con cuidado, entregar en horario de oficina…"
              value={form.note}
              onChange={(e) => set('note', e.target.value)}
              rows={3}
            />
          </Section>
        </div>

        {/* Right: sticky summary */}
        <aside className="lg:col-span-1">
          <div
            className="rounded-[var(--radius-lg)] border bg-card p-6 lg:sticky lg:top-20"
            style={{ boxShadow: 'var(--shadow-xs)' }}
          >
            <h2 className="t-h2 m-0 mb-4">Resumen</h2>

            <div className="flex flex-col gap-3 t-body-sm">
              <div>
                <div
                  className="t-caption"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Modalidad
                </div>
                <div className="mt-0.5 font-semibold flex items-center gap-1.5">
                  {selectedMode && (
                    <selectedMode.icon className="h-3.5 w-3.5" style={{ color: 'var(--brand-toucan-orange)' }} />
                  )}
                  {selectedMode?.label ?? '—'}
                </div>
              </div>

              <div>
                <div
                  className="t-caption"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Motivo
                </div>
                <div className="mt-0.5 font-semibold">
                  <span className="mono mr-1.5">{selectedReason?.code}</span>
                  {selectedReason?.label}
                </div>
              </div>

              <div>
                <div
                  className="t-caption"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Fecha de traslado
                </div>
                <div className="mt-0.5 font-semibold mono tnum">
                  {form.transferStartDate || '—'}
                </div>
              </div>

              <div className="rounded-[var(--radius-md)] border p-3 flex items-center gap-3" style={{ borderColor: 'var(--border)' }}>
                <span
                  className="flex h-8 w-8 items-center justify-center rounded-md shrink-0"
                  style={{
                    background: 'color-mix(in oklch, var(--info) 14%, transparent)',
                    color: 'var(--info)',
                  }}
                >
                  <MapPin className="h-4 w-4" />
                </span>
                <div className="min-w-0 flex-1">
                  <div className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
                    Origen
                  </div>
                  <div className="mono tnum t-body-sm font-semibold">
                    {form.originUbigeo || '—'}
                  </div>
                </div>
                <span className="text-[var(--muted-foreground)]">→</span>
                <span
                  className="flex h-8 w-8 items-center justify-center rounded-md shrink-0"
                  style={{
                    background: 'color-mix(in oklch, var(--success) 14%, transparent)',
                    color: 'var(--success)',
                  }}
                >
                  <PackageCheck className="h-4 w-4" />
                </span>
                <div className="min-w-0 flex-1 text-right">
                  <div className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
                    Destino
                  </div>
                  <div className="mono tnum t-body-sm font-semibold">
                    {form.destinationUbigeo || '—'}
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div
                  className="rounded-[var(--radius-md)] border p-3"
                  style={{ borderColor: 'var(--border)' }}
                >
                  <div className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
                    Bultos
                  </div>
                  <div className="t-num-md mono mt-0.5">{form.totalPackages || '0'}</div>
                </div>
                <div
                  className="rounded-[var(--radius-md)] border p-3"
                  style={{ borderColor: 'var(--border)' }}
                >
                  <div className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
                    Peso (KG)
                  </div>
                  <div className="t-num-md mono mt-0.5">
                    {form.grossWeight ? parseFloat(form.grossWeight).toFixed(2) : '—'}
                  </div>
                </div>
              </div>

              <div
                className="rounded-[var(--radius-md)] border p-3 flex items-center justify-between"
                style={{ borderColor: 'var(--border)' }}
              >
                <span className="inline-flex items-center gap-1.5">
                  <Hash className="h-3.5 w-3.5" style={{ color: 'var(--muted-foreground)' }} />
                  Líneas de mercadería
                </span>
                <span className="mono tnum font-semibold">
                  {totalItems} · {totalQty} unid.
                </span>
              </div>
            </div>

            <div className="my-4 h-px" style={{ background: 'var(--border)' }} />

            <div className="flex flex-col gap-2">
              <Button type="submit" size="lg" className="w-full h-12" disabled={isSubmitting}>
                {isSubmitting ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Creando…
                  </>
                ) : (
                  <>
                    <Send className="h-4 w-4 mr-2" /> Crear guía
                  </>
                )}
              </Button>
              <Button
                type="button"
                variant="outline"
                size="lg"
                className="w-full h-12"
                onClick={() => router.push('/despatch-advices')}
              >
                Cancelar
              </Button>
            </div>
            <p
              className="t-caption mt-2.5 text-center"
              style={{ color: 'var(--muted-foreground)' }}
            >
              Se guarda como borrador. Podrás enviarla a SUNAT desde el detalle.
            </p>
          </div>
        </aside>
      </div>
    </form>
  );
}
