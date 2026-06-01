'use client';

import { useEffect, useState, useMemo } from 'react';
import { api, type SeriesResponse } from '@/lib/api';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Plus,
  Hash,
  FileSpreadsheet,
  Receipt,
  FileMinus,
  FilePlus,
  CheckCircle2,
  AlertCircle,
  Loader2,
  Sparkles,
  type LucideIcon,
} from 'lucide-react';
import { toast } from 'sonner';
import { cn } from '@/lib/utils';

type DocType = '01' | '03' | '07' | '08';

const DOC_TYPES: Record<DocType, { label: string; sub: string; icon: LucideIcon; suggested: string }> = {
  '01': { label: 'Factura', sub: 'Empresas (RUC)', icon: FileSpreadsheet, suggested: 'F001' },
  '03': { label: 'Boleta', sub: 'Consumidor final', icon: Receipt, suggested: 'B001' },
  '07': { label: 'Nota de crédito', sub: 'Devoluciones', icon: FileMinus, suggested: 'FC01' },
  '08': { label: 'Nota de débito', sub: 'Cargos extra', icon: FilePlus, suggested: 'FD01' },
};

const DOC_TYPE_KEYS = Object.keys(DOC_TYPES) as DocType[];

const SERIE_RE = /^[BFR][A-Z0-9]{3}$/;

function CreateDialog({
  open,
  onOpenChange,
  onCreated,
  presetType,
  presetSerie,
}: {
  open: boolean;
  onOpenChange: (v: boolean) => void;
  onCreated: () => void;
  presetType?: DocType;
  presetSerie?: string;
}) {
  const [form, setForm] = useState({
    documentType: '01' as DocType,
    serie: '',
    emissionPoint: 'PRINCIPAL',
  });
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (open) {
      setForm({
        documentType: presetType ?? '01',
        serie: presetSerie ?? DOC_TYPES[presetType ?? '01'].suggested,
        emissionPoint: 'PRINCIPAL',
      });
    }
  }, [open, presetType, presetSerie]);

  const typeInfo = DOC_TYPES[form.documentType];
  const serieValid = SERIE_RE.test(form.serie);

  const handleSelect = (v: DocType) => {
    setForm((f) => {
      // If current serie matches ANY suggested template, swap to the new type's suggested.
      // If user typed something custom (not a known suggestion), keep their input.
      const isStockSuggestion = DOC_TYPE_KEYS.some((k) => DOC_TYPES[k].suggested === f.serie);
      return {
        ...f,
        documentType: v,
        serie: isStockSuggestion || !f.serie ? DOC_TYPES[v].suggested : f.serie,
      };
    });
  };

  const handleCreate = async () => {
    if (!serieValid) {
      toast.error('La serie debe tener 4 caracteres (ej: F001)');
      return;
    }
    setSubmitting(true);
    try {
      await api.post('/v1/series', form);
      toast.success(`Serie ${form.serie} creada`);
      onOpenChange(false);
      onCreated();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al crear');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-xl">
        <DialogHeader>
          <DialogTitle>Nueva serie</DialogTitle>
          <DialogDescription>
            Cada tipo de comprobante necesita su propia serie para llevar el correlativo SUNAT.
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-4 mt-2">
          {/* Tipo */}
          <div>
            <Label className="t-label mb-2 block">Tipo de comprobante</Label>
            <div className="grid grid-cols-2 gap-2">
              {DOC_TYPE_KEYS.map((k) => {
                const info = DOC_TYPES[k];
                const Icon = info.icon;
                const active = form.documentType === k;
                return (
                  <button
                    key={k}
                    type="button"
                    onClick={() => handleSelect(k)}
                    className={cn(
                      'relative flex items-center gap-2.5 rounded-[var(--radius-md)] border px-3 py-2.5 transition-colors text-left min-w-0'
                    )}
                    style={{
                      background: active
                        ? 'color-mix(in oklch, var(--accent) 18%, transparent)'
                        : 'var(--card)',
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
                        {info.label}
                      </span>
                      <span
                        className="block t-caption leading-tight mt-0.5 whitespace-normal"
                        style={{ color: 'var(--muted-foreground)' }}
                      >
                        {info.sub} ·{' '}
                        <span
                          className="mono font-bold tnum"
                          style={{ color: active ? 'var(--accent-foreground)' : undefined }}
                        >
                          {info.suggested}
                        </span>
                      </span>
                    </span>
                  </button>
                );
              })}
            </div>
          </div>

          {/* Serie */}
          <div>
            <Label className="t-label mb-1.5 block">Código de serie</Label>
            <Input
              placeholder={typeInfo.suggested}
              maxLength={4}
              value={form.serie}
              onChange={(e) =>
                setForm((f) => ({ ...f, serie: e.target.value.toUpperCase() }))
              }
              className="mono"
            />
            <p
              className="t-caption mt-1.5"
              style={{
                color: form.serie && !serieValid ? 'var(--danger)' : 'var(--muted-foreground)',
              }}
            >
              4 caracteres. Facturas empiezan con <strong>F</strong>, boletas con{' '}
              <strong>B</strong>, notas con <strong>F</strong> (ej.{' '}
              <span className="mono">{typeInfo.suggested}</span>).
            </p>
          </div>

          {/* Emission point */}
          <div>
            <Label className="t-label mb-1.5 block">Punto de emisión</Label>
            <Input
              placeholder="PRINCIPAL"
              value={form.emissionPoint}
              onChange={(e) =>
                setForm((f) => ({ ...f, emissionPoint: e.target.value.toUpperCase() }))
              }
            />
            <p className="t-caption mt-1.5" style={{ color: 'var(--muted-foreground)' }}>
              Útil si emites desde varias sucursales. Si solo tienes una, deja "PRINCIPAL".
            </p>
          </div>

          {/* Preview */}
          {serieValid && (
            <div
              className="rounded-[var(--radius-md)] border p-3 flex items-center gap-3"
              style={{
                background: 'color-mix(in oklch, var(--success) 8%, transparent)',
                borderColor: 'color-mix(in oklch, var(--success) 35%, transparent)',
              }}
            >
              <CheckCircle2 className="h-5 w-5 shrink-0" style={{ color: 'var(--success)' }} />
              <div className="min-w-0 flex-1">
                <p className="t-body-sm m-0 font-semibold">El primer comprobante será</p>
                <p
                  className="t-body mono tnum m-0 mt-0.5 font-semibold"
                  style={{ color: 'var(--success)' }}
                >
                  {form.serie}-00000001
                </p>
              </div>
            </div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancelar
            </Button>
            <Button onClick={handleCreate} disabled={!serieValid || submitting}>
              {submitting ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Creando…
                </>
              ) : (
                <>
                  <Plus className="h-4 w-4 mr-2" /> Crear serie
                </>
              )}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}

function StatusBadge({ active }: { active: boolean }) {
  const color = active ? 'var(--success)' : 'var(--slate-500)';
  return (
    <span
      className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 t-caption font-semibold whitespace-nowrap"
      style={{
        color,
        background: `color-mix(in oklch, ${color} 14%, transparent)`,
      }}
    >
      <span className="h-1.5 w-1.5 rounded-full" style={{ background: color }} />
      {active ? 'Activa' : 'Inactiva'}
    </span>
  );
}

export default function SeriesPage() {
  const [series, setSeries] = useState<SeriesResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [preset, setPreset] = useState<{ type?: DocType; serie?: string }>({});

  const fetchSeries = async () => {
    setIsLoading(true);
    try {
      setSeries(await api.get<SeriesResponse[]>('/v1/series'));
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchSeries();
  }, []);

  const openCreate = (type?: DocType, serie?: string) => {
    setPreset({ type, serie });
    setDialogOpen(true);
  };

  const isEmpty = !isLoading && series.length === 0;

  const grouped = useMemo(() => {
    const map = new Map<DocType, SeriesResponse[]>();
    DOC_TYPE_KEYS.forEach((k) => map.set(k, []));
    series.forEach((s) => {
      const key = s.documentType as DocType;
      if (map.has(key)) map.get(key)!.push(s);
    });
    return map;
  }, [series]);

  return (
    <div>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0">Series</h1>
          <p className="t-body mt-1.5 mb-0" style={{ color: 'var(--muted-foreground)' }}>
            Cada tipo de comprobante usa su propia serie y correlativo. SUNAT los exige.
          </p>
        </div>
        {!isEmpty && (
          <Button onClick={() => openCreate()}>
            <Plus className="h-4 w-4 mr-2" /> Nueva serie
          </Button>
        )}
      </div>

      {isLoading ? (
        <div
          className="rounded-[var(--radius-lg)] border bg-card p-6"
          style={{ boxShadow: 'var(--shadow-xs)' }}
        >
          <div className="flex items-center gap-3 text-[var(--muted-foreground)]">
            <Loader2 className="h-4 w-4 animate-spin" />
            <span className="t-body-sm">Cargando series…</span>
          </div>
        </div>
      ) : isEmpty ? (
        <div
          className="rounded-[var(--radius-lg)] border bg-card overflow-hidden"
          style={{ boxShadow: 'var(--shadow-xs)' }}
        >
          {/* Hero empty state */}
          <div className="p-10 text-center border-b" style={{ borderColor: 'var(--border)' }}>
            <div
              className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl"
              style={{ background: 'color-mix(in oklch, var(--accent) 14%, transparent)' }}
            >
              <Hash className="h-8 w-8" style={{ color: 'var(--accent-foreground)' }} />
            </div>
            <h2 className="t-h1 m-0">Aún no tienes series creadas</h2>
            <p
              className="t-body mt-2 mb-0 max-w-[460px] mx-auto"
              style={{ color: 'var(--muted-foreground)' }}
            >
              Las series son los códigos que SUNAT usa para identificar tus tipos de comprobante
              (ej. <span className="mono font-semibold">F001</span> para facturas,{' '}
              <span className="mono font-semibold">B001</span> para boletas). Necesitas al menos
              una por cada tipo que vayas a emitir.
            </p>
          </div>

          {/* Quick start cards */}
          <div className="p-6">
            <div className="flex items-center gap-2 mb-4">
              <Sparkles className="h-4 w-4" style={{ color: 'var(--brand-toucan-orange)' }} />
              <span className="t-overline" style={{ color: 'var(--muted-foreground)' }}>
                Inicio rápido
              </span>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              {DOC_TYPE_KEYS.map((k) => {
                const info = DOC_TYPES[k];
                const Icon = info.icon;
                return (
                  <button
                    key={k}
                    type="button"
                    onClick={() => openCreate(k, info.suggested)}
                    className="text-left rounded-[var(--radius-md)] border p-4 hover:border-[var(--accent)] hover:bg-[var(--muted)] transition-colors flex items-center gap-3"
                    style={{ borderColor: 'var(--border)' }}
                  >
                    <span
                      className="flex h-10 w-10 items-center justify-center rounded-md shrink-0"
                      style={{ background: 'var(--muted)' }}
                    >
                      <Icon className="h-5 w-5" />
                    </span>
                    <div className="min-w-0 flex-1">
                      <div className="t-body-sm font-semibold">
                        Crear{' '}
                        <span className="mono" style={{ color: 'var(--brand-ink)' }}>
                          {info.suggested}
                        </span>{' '}
                        para {info.label.toLowerCase()}
                      </div>
                      <div className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
                        {info.sub}
                      </div>
                    </div>
                    <Plus className="h-4 w-4 shrink-0" style={{ color: 'var(--muted-foreground)' }} />
                  </button>
                );
              })}
            </div>
          </div>
        </div>
      ) : (
        // Grouped by type
        <div className="flex flex-col gap-[var(--gap-cards)]">
          {DOC_TYPE_KEYS.map((k) => {
            const info = DOC_TYPES[k];
            const Icon = info.icon;
            const list = grouped.get(k) ?? [];
            if (list.length === 0) {
              return (
                <div
                  key={k}
                  className="rounded-[var(--radius-lg)] border-dashed border-2 p-5 flex items-center gap-4"
                  style={{ borderColor: 'var(--border)' }}
                >
                  <span
                    className="flex h-10 w-10 items-center justify-center rounded-md shrink-0"
                    style={{ background: 'var(--muted)', color: 'var(--muted-foreground)' }}
                  >
                    <Icon className="h-5 w-5" />
                  </span>
                  <div className="flex-1 min-w-0">
                    <p className="t-body-sm m-0 font-semibold">
                      Sin series para {info.label.toLowerCase()}
                    </p>
                    <p
                      className="t-caption m-0"
                      style={{ color: 'var(--muted-foreground)' }}
                    >
                      {info.sub}
                    </p>
                  </div>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => openCreate(k, info.suggested)}
                  >
                    <Plus className="h-4 w-4 mr-1.5" /> Crear {info.suggested}
                  </Button>
                </div>
              );
            }
            return (
              <section
                key={k}
                className="rounded-[var(--radius-lg)] border bg-card overflow-hidden"
                style={{ boxShadow: 'var(--shadow-xs)' }}
              >
                <header className="px-6 py-4 flex items-center gap-3 border-b" style={{ borderColor: 'var(--border)' }}>
                  <span
                    className="flex h-9 w-9 items-center justify-center rounded-md"
                    style={{ background: 'var(--muted)' }}
                  >
                    <Icon className="h-4 w-4" />
                  </span>
                  <div className="flex-1 min-w-0">
                    <h2 className="t-h2 m-0">{info.label}</h2>
                    <p className="t-caption m-0" style={{ color: 'var(--muted-foreground)' }}>
                      {list.length} {list.length === 1 ? 'serie' : 'series'}
                    </p>
                  </div>
                  <Button variant="outline" size="sm" onClick={() => openCreate(k)}>
                    <Plus className="h-4 w-4 mr-1.5" /> Añadir
                  </Button>
                </header>
                <table className="w-full">
                  <thead>
                    <tr
                      className="t-overline"
                      style={{ color: 'var(--muted-foreground)', background: 'var(--muted)' }}
                    >
                      <th className="text-left py-2.5 pl-6 pr-2">Serie</th>
                      <th className="text-left py-2.5 px-2">Punto de emisión</th>
                      <th className="text-right py-2.5 px-2">Correlativo actual</th>
                      <th className="text-left py-2.5 px-2">Próximo comprobante</th>
                      <th className="text-right py-2.5 pr-6 pl-2">Estado</th>
                    </tr>
                  </thead>
                  <tbody>
                    {list.map((s) => (
                      <tr
                        key={s.id}
                        className="hover:bg-[var(--muted)] transition-colors"
                        style={{ borderTop: '1px solid var(--border)' }}
                      >
                        <td className="py-3 pl-6 pr-2">
                          <span className="mono t-body font-semibold">{s.serie}</span>
                        </td>
                        <td className="py-3 px-2 t-body-sm" style={{ color: 'var(--muted-foreground)' }}>
                          {s.emissionPoint}
                        </td>
                        <td className="py-3 px-2 text-right mono tnum t-body-sm">
                          {String(s.currentCorrelative).padStart(8, '0')}
                        </td>
                        <td className="py-3 px-2 mono tnum t-body-sm font-semibold">
                          {s.serie}-{String(s.currentCorrelative + 1).padStart(8, '0')}
                        </td>
                        <td className="py-3 pl-2 pr-6 text-right">
                          <StatusBadge active={s.isActive} />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </section>
            );
          })}

          {/* Helper card */}
          <div
            className="rounded-[var(--radius-lg)] border p-4 flex items-start gap-3"
            style={{
              background: 'color-mix(in oklch, var(--info) 6%, transparent)',
              borderColor: 'color-mix(in oklch, var(--info) 25%, transparent)',
            }}
          >
            <AlertCircle className="h-5 w-5 shrink-0 mt-0.5" style={{ color: 'var(--info)' }} />
            <div>
              <p className="t-body-sm m-0 font-semibold">¿Y si me equivoco con un correlativo?</p>
              <p className="t-body-sm m-0 mt-0.5" style={{ color: 'var(--muted-foreground)' }}>
                Los correlativos se asignan automáticamente y nunca se repiten. Si necesitas
                anular un comprobante, hazlo desde la sección de Bajas; no toques esta página.
              </p>
            </div>
          </div>
        </div>
      )}

      <CreateDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        onCreated={fetchSeries}
        presetType={preset.type}
        presetSerie={preset.serie}
      />
    </div>
  );
}
