'use client';

import { useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import {
  ArrowUp,
  ArrowDown,
  Plus,
  Download,
  Sparkles,
  Send,
  ChevronRight,
  Inbox,
} from 'lucide-react';
import {
  BarChart as RBarChart,
  Bar,
  Cell as BarCell,
  ResponsiveContainer,
  Tooltip,
  PieChart,
  Pie,
  Cell,
} from 'recharts';
import {
  api,
  type DashboardResponse,
  type DocumentResponse,
  type PaginatedResponse,
} from '@/lib/api';
import { useAuth } from '@/lib/auth-context';
import { Button } from '@/components/ui/button';
import { Sparkline } from '@/components/dashboard/Sparkline';

const fmtMoney = (n: number) =>
  new Intl.NumberFormat('es-PE', {
    style: 'currency',
    currency: 'PEN',
    maximumFractionDigits: 0,
  }).format(n);

const fmtNum = (n: number) => new Intl.NumberFormat('es-PE').format(n);

const fmtMoneyPlain = (n: number) =>
  new Intl.NumberFormat('es-PE', { maximumFractionDigits: 0 }).format(n);

const MONTHS = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];

const STATUS: Record<string, { label: string; color: string }> = {
  accepted: { label: 'Aceptados', color: 'var(--success)' },
  pending: { label: 'Pendientes', color: 'var(--warning)' },
  rejected: { label: 'Rechazados', color: 'var(--danger)' },
  voided: { label: 'Anulados', color: 'var(--slate-500)' },
  draft: { label: 'Borradores', color: 'var(--slate-400)' },
  sent: { label: 'Enviados', color: 'var(--info)' },
  signed: { label: 'Firmados', color: 'var(--brand-toucan-orange)' },
};

const statusInfo = (s: string) =>
  STATUS[s] ?? { label: s, color: 'var(--slate-400)' };

const formatDate = () => {
  const d = new Date();
  const wd = d.toLocaleDateString('es-PE', { weekday: 'long' });
  const month = d.toLocaleDateString('es-PE', { month: 'long' });
  return `${wd[0]!.toUpperCase()}${wd.slice(1)}, ${d.getDate()} de ${month}`;
};

function Badge({
  color,
  dot,
  children,
}: {
  color: string;
  dot?: boolean;
  children: React.ReactNode;
}) {
  return (
    <span
      className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 t-caption font-semibold whitespace-nowrap"
      style={{
        color,
        background: `color-mix(in oklch, ${color} 14%, transparent)`,
      }}
    >
      {dot && <span className="h-1.5 w-1.5 rounded-full" style={{ background: color }} />}
      {children}
    </span>
  );
}

function KpiCard({
  eyebrow,
  prefix,
  value,
  delta,
  good,
  spark,
  color,
}: {
  eyebrow: string;
  prefix?: string;
  value: string;
  delta?: { pct: number; positive: boolean };
  good?: boolean;
  spark?: number[];
  color: string;
}) {
  const deltaColor = good ? 'var(--success)' : 'var(--danger)';
  const Arrow = delta?.positive ? ArrowUp : ArrowDown;
  return (
    <div
      className="rounded-[var(--radius-lg)] border bg-card overflow-hidden p-5 flex flex-col gap-2.5"
      style={{ boxShadow: 'var(--shadow-xs)' }}
    >
      <div className="flex items-start justify-between gap-2">
        <span className="t-overline" style={{ color: 'var(--muted-foreground)' }}>
          {eyebrow}
        </span>
        {delta && (
          <span
            className="inline-flex items-center gap-0.5 rounded-full px-1.5 py-0.5 text-[11px] font-bold"
            style={{
              color: deltaColor,
              background: `color-mix(in oklch, ${deltaColor} 12%, transparent)`,
            }}
          >
            <Arrow className="h-3 w-3" strokeWidth={2.5} />
            <span className="tnum">{Math.abs(delta.pct).toFixed(0)}%</span>
          </span>
        )}
      </div>
      <div className="flex items-baseline gap-1">
        {prefix && (
          <span className="t-body font-semibold" style={{ color: 'var(--muted-foreground)' }}>
            {prefix}
          </span>
        )}
        <span className="t-num-lg">{value}</span>
      </div>
      <div className="mt-auto" style={{ height: 40 }}>
        {spark && spark.length > 1 ? (
          <Sparkline data={spark} color={color} height={40} />
        ) : (
          <div className="h-full flex items-center">
            <div
              className="w-full h-px"
              style={{
                background:
                  'linear-gradient(90deg, transparent, color-mix(in oklch, ' +
                  color +
                  ' 40%, transparent), transparent)',
              }}
            />
          </div>
        )}
      </div>
    </div>
  );
}

function CardHead({
  title,
  sub,
  right,
}: {
  title: string;
  sub?: string;
  right?: React.ReactNode;
}) {
  return (
    <div className="flex items-center justify-between gap-3 mb-4">
      <div>
        <h2 className="t-h2 m-0">{title}</h2>
        {sub && (
          <p className="t-body-sm m-0 mt-0.5" style={{ color: 'var(--muted-foreground)' }}>
            {sub}
          </p>
        )}
      </div>
      {right}
    </div>
  );
}

function Donut({ data }: { data: { label: string; value: number; color: string }[] }) {
  const total = data.reduce((s, d) => s + d.value, 0);
  if (total === 0) return null;
  return (
    <div className="flex items-center gap-5 flex-wrap">
      <div className="relative" style={{ width: 168, height: 168, flexShrink: 0 }}>
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={data}
              dataKey="value"
              nameKey="label"
              cx="50%"
              cy="50%"
              innerRadius={56}
              outerRadius={80}
              stroke="none"
              startAngle={90}
              endAngle={-270}
              isAnimationActive={false}
            >
              {data.map((d) => (
                <Cell key={d.label} fill={d.color} />
              ))}
            </Pie>
          </PieChart>
        </ResponsiveContainer>
        <div className="absolute inset-0 flex flex-col items-center justify-center pointer-events-none">
          <span className="t-num-lg">{fmtNum(total)}</span>
          <span className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
            total
          </span>
        </div>
      </div>
      <div className="flex flex-col gap-2.5 flex-1 min-w-[140px]">
        {data.map((d) => (
          <div key={d.label} className="flex items-center gap-2">
            <span
              className="h-2.5 w-2.5 rounded-sm shrink-0"
              style={{ background: d.color }}
            />
            <span
              className="t-body-sm flex-1"
              style={{ color: 'var(--muted-foreground)' }}
            >
              {d.label}
            </span>
            <span className="t-body-sm tnum font-semibold">{fmtNum(d.value)}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

function EmptyState({ icon: Icon, title, body, cta }: {
  icon: React.ElementType;
  title: string;
  body: string;
  cta?: React.ReactNode;
}) {
  return (
    <div className="flex flex-col items-center text-center py-8 px-4">
      <Icon className="h-10 w-10 mb-3" style={{ color: 'var(--slate-400)' }} />
      <h3 className="t-h3 m-0">{title}</h3>
      <p
        className="t-body-sm mt-1.5 mb-4 max-w-[280px]"
        style={{ color: 'var(--muted-foreground)' }}
      >
        {body}
      </p>
      {cta}
    </div>
  );
}

export default function DashboardPage() {
  const { user } = useAuth();
  const [data, setData] = useState<DashboardResponse | null>(null);
  const [recent, setRecent] = useState<DocumentResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [hovered, setHovered] = useState<number | null>(null);

  useEffect(() => {
    let cancelled = false;
    Promise.allSettled([
      api.get<DashboardResponse>('/v1/dashboard'),
      api.get<PaginatedResponse<DocumentResponse>>('/v1/documents?pageSize=5&page=1'),
    ])
      .then(([d, r]) => {
        if (cancelled) return;
        if (d.status === 'fulfilled') setData(d.value);
        if (r.status === 'fulfilled') setRecent(r.value.items ?? []);
      })
      .finally(() => !cancelled && setIsLoading(false));
    return () => {
      cancelled = true;
    };
  }, []);

  const firstName = (user?.fullName || user?.email || 'amigo').split(/[\s@]/)[0];

  const monthly = useMemo(() => {
    if (!data?.monthlySales?.length) return [] as { label: string; total: number; count: number }[];
    return data.monthlySales.map((m) => ({
      label: MONTHS[m.month - 1]!,
      total: m.total,
      count: m.count,
    }));
  }, [data]);

  const ventasDelta = useMemo(() => {
    if (!data?.monthlySales || data.monthlySales.length < 2) return undefined;
    const arr = data.monthlySales;
    const last = arr[arr.length - 1]!.total;
    const prev = arr[arr.length - 2]!.total;
    if (prev === 0) return undefined;
    const pct = ((last - prev) / prev) * 100;
    return { pct, positive: pct >= 0 };
  }, [data]);

  const docsDelta = useMemo(() => {
    if (!data?.monthlySales || data.monthlySales.length < 2) return undefined;
    const arr = data.monthlySales;
    const last = arr[arr.length - 1]!.count;
    const prev = arr[arr.length - 2]!.count;
    if (prev === 0) return undefined;
    const pct = ((last - prev) / prev) * 100;
    return { pct, positive: pct >= 0 };
  }, [data]);

  const ventasSpark = data?.monthlySales?.map((m) => m.total) ?? [];
  const docsSpark = data?.monthlySales?.map((m) => m.count) ?? [];

  const donutData =
    data?.byStatus
      .map((s) => ({ label: statusInfo(s.status).label, value: s.count, color: statusInfo(s.status).color }))
      .filter((d) => d.value > 0) ?? [];

  const ventasMes = data?.thisMonth.totalAmount ?? 0;
  const docsMes = data?.thisMonth.totalDocuments ?? 0;
  const pendientesMes = data?.thisMonth.pending ?? 0;
  const rechazosMes = data?.thisMonth.rejected ?? 0;

  return (
    <div>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0">
            Hola, {firstName} <span aria-hidden>👋</span>
          </h1>
          <p className="t-body mt-1.5 mb-0" style={{ color: 'var(--muted-foreground)' }}>
            {formatDate()} · Aquí está el resumen de tu facturación.
          </p>
        </div>
        <div className="flex gap-2 shrink-0">
          <Button variant="outline">
            <Download className="h-4 w-4 mr-2" /> Exportar
          </Button>
          <Button
            asChild
            style={{ background: 'var(--accent)', color: 'var(--accent-foreground)', fontWeight: 600 }}
          >
            <Link href="/documents/new">
              <Plus className="h-4 w-4 mr-2" /> Emitir comprobante
            </Link>
          </Button>
        </div>
      </div>

      {/* KPI grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-[var(--gap-cards)] mb-[var(--gap-cards)]">
        <KpiCard
          eyebrow="VENTAS DEL MES"
          prefix="S/"
          value={fmtMoneyPlain(Math.round(ventasMes))}
          delta={ventasDelta}
          good={ventasDelta?.positive ?? true}
          spark={ventasSpark}
          color="var(--brand-toucan-yellow)"
        />
        <KpiCard
          eyebrow="COMPROBANTES EMITIDOS"
          value={fmtNum(docsMes)}
          delta={docsDelta}
          good={docsDelta?.positive ?? true}
          spark={docsSpark}
          color="var(--info)"
        />
        <KpiCard
          eyebrow="PENDIENTES SUNAT"
          value={fmtNum(pendientesMes)}
          good={false}
          color="var(--warning)"
        />
        <KpiCard
          eyebrow="RECHAZOS"
          value={fmtNum(rechazosMes)}
          good={false}
          color="var(--danger)"
        />
      </div>

      {/* Row 2: monthly chart + donut */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-[var(--gap-cards)] mb-[var(--gap-cards)]">
        <div
          className="lg:col-span-2 rounded-[var(--radius-lg)] border bg-card p-6"
          style={{ boxShadow: 'var(--shadow-xs)' }}
        >
          <CardHead title="Ventas mensuales" sub="Monto facturado por mes (S/)" />
          <div style={{ height: 220 }}>
            {monthly.length === 0 ? (
              <EmptyState
                icon={Inbox}
                title="Aún sin ventas"
                body="Cuando emitas tus primeros comprobantes verás tu evolución mensual aquí."
              />
            ) : (
              <ResponsiveContainer width="100%" height="100%">
                <RBarChart
                  data={monthly}
                  onMouseMove={(s) =>
                    setHovered(typeof s.activeTooltipIndex === 'number' ? s.activeTooltipIndex : null)
                  }
                  onMouseLeave={() => setHovered(null)}
                  margin={{ top: 24, right: 0, left: 0, bottom: 0 }}
                >
                  <Tooltip
                    cursor={false}
                    content={({ active, payload }) => {
                      if (!active || !payload?.[0]) return null;
                      const v = payload[0].value as number;
                      const ct = (payload[0].payload as { count: number }).count;
                      return (
                        <div
                          className="rounded-md px-2.5 py-1.5"
                          style={{
                            background: 'var(--slate-900)',
                            color: '#fff',
                            boxShadow: 'var(--shadow-lg)',
                          }}
                        >
                          <div className="t-caption tnum font-bold">S/ {fmtMoneyPlain(v)}</div>
                          <div className="t-caption tnum" style={{ color: 'var(--slate-400)' }}>
                            {fmtNum(ct)} comprobantes
                          </div>
                        </div>
                      );
                    }}
                  />
                  <Bar dataKey="total" radius={[4, 4, 2, 2]} isAnimationActive={false}>
                    {monthly.map((m, i) => (
                      <BarCell
                        key={`${m.label}-${i}`}
                        fill={
                          hovered === i
                            ? 'var(--brand-toucan-orange)'
                            : 'var(--brand-toucan-yellow)'
                        }
                      />
                    ))}
                  </Bar>
                </RBarChart>
              </ResponsiveContainer>
            )}
          </div>
        </div>

        <div
          className="rounded-[var(--radius-lg)] border bg-card p-6"
          style={{ boxShadow: 'var(--shadow-xs)' }}
        >
          <CardHead title="Estado SUNAT" sub="Distribución global" />
          {donutData.length === 0 ? (
            <EmptyState
              icon={Inbox}
              title="Aún sin comprobantes"
              body="Aquí verás el reparto de tus envíos a SUNAT por estado."
            />
          ) : (
            <Donut data={donutData} />
          )}
        </div>
      </div>

      {/* Row 3: últimos comprobantes + por tipo */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-[var(--gap-cards)] mb-[var(--gap-cards)]">
        <div
          className="rounded-[var(--radius-lg)] border bg-card overflow-hidden flex flex-col"
          style={{ boxShadow: 'var(--shadow-xs)' }}
        >
          <div className="px-6 pt-5 pb-3">
            <CardHead title="Últimos comprobantes" />
          </div>
          {recent.length === 0 ? (
            <EmptyState
              icon={Inbox}
              title="No hay comprobantes aún"
              body="Emite tu primer comprobante para verlo aquí."
              cta={
                <Button asChild>
                  <Link href="/documents/new">
                    <Plus className="h-4 w-4 mr-2" /> Emitir comprobante
                  </Link>
                </Button>
              }
            />
          ) : (
            <div className="overflow-x-auto flex-1">
              <table className="w-full text-[13px]">
                <tbody>
                  {recent.map((d) => {
                    const st = statusInfo(d.status);
                    return (
                      <tr key={d.id} className="hover:bg-[var(--muted)] transition-colors">
                        <td
                          className="py-2.5 px-6"
                          style={{ borderTop: '1px solid var(--border)' }}
                        >
                          <span className="mono t-body-sm font-semibold">{d.fullNumber}</span>
                        </td>
                        <td
                          className="py-2.5 px-2"
                          style={{ borderTop: '1px solid var(--border)' }}
                        >
                          <Badge color="var(--info)">{d.documentTypeName}</Badge>
                        </td>
                        <td
                          className="py-2.5 px-2 truncate max-w-[150px]"
                          style={{
                            borderTop: '1px solid var(--border)',
                            color: 'var(--muted-foreground)',
                          }}
                        >
                          {d.customerName}
                        </td>
                        <td
                          className="py-2.5 px-2 text-right"
                          style={{ borderTop: '1px solid var(--border)' }}
                        >
                          <span className="mono t-body-sm font-semibold">
                            {d.currency === 'PEN' ? 'S/ ' : d.currency + ' '}
                            {fmtMoneyPlain(d.total)}
                          </span>
                        </td>
                        <td
                          className="py-2.5 pl-2 pr-6 text-right"
                          style={{ borderTop: '1px solid var(--border)' }}
                        >
                          <Badge color={st.color} dot>
                            {st.label}
                          </Badge>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
          {recent.length > 0 && (
            <div className="px-6 py-3" style={{ borderTop: '1px solid var(--border)' }}>
              <Link
                href="/documents"
                className="inline-flex items-center gap-1 t-body-sm font-medium"
                style={{ color: 'var(--info)' }}
              >
                Ver todos <ChevronRight className="h-3.5 w-3.5" />
              </Link>
            </div>
          )}
        </div>

        <div
          className="rounded-[var(--radius-lg)] border bg-card p-6"
          style={{ boxShadow: 'var(--shadow-xs)' }}
        >
          <CardHead title="Por tipo de comprobante" sub="Resumen del año" />
          {(data?.byType.length ?? 0) === 0 ? (
            <EmptyState
              icon={Inbox}
              title="Sin desglose disponible"
              body="Aquí verás el reparto por boletas, facturas y notas cuando emitas."
            />
          ) : (
            <div className="flex flex-col gap-1">
              {(data?.byType ?? []).map((t, i) => {
                const max = Math.max(...(data?.byType.map((x) => x.total) ?? [1]));
                const pct = max > 0 ? (t.total / max) * 100 : 0;
                const palette = [
                  'var(--brand-toucan-yellow)',
                  'var(--info)',
                  'var(--brand-toucan-orange)',
                  'var(--success)',
                  'var(--slate-500)',
                ];
                const color = palette[i % palette.length]!;
                return (
                  <div
                    key={t.documentType}
                    className="py-2.5"
                    style={{ borderTop: i ? '1px solid var(--border)' : undefined }}
                  >
                    <div className="flex items-center justify-between gap-3 mb-1.5">
                      <span className="t-body-sm font-semibold truncate">{t.name}</span>
                      <span className="mono t-body-sm font-semibold">
                        S/ {fmtMoneyPlain(t.total)}
                      </span>
                    </div>
                    <div className="flex items-center gap-3">
                      <div
                        className="flex-1 rounded-full overflow-hidden"
                        style={{ height: 6, background: 'var(--muted)' }}
                      >
                        <div
                          className="h-full rounded-full"
                          style={{
                            width: `${pct}%`,
                            background: color,
                            transition: 'width 400ms var(--ease-out)',
                          }}
                        />
                      </div>
                      <span
                        className="t-caption tnum shrink-0"
                        style={{ color: 'var(--muted-foreground)' }}
                      >
                        {fmtNum(t.count)} {t.count === 1 ? 'comp.' : 'comps.'}
                      </span>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>

      {/* Row 4: AI assistant */}
      <div
        className="relative overflow-hidden rounded-[var(--radius-2xl)] p-7"
        style={{
          background: 'linear-gradient(135deg, var(--slate-900), var(--slate-800))',
          border: '1px solid color-mix(in oklch, var(--accent) 20%, transparent)',
        }}
      >
        <div
          aria-hidden
          className="pointer-events-none absolute -top-20 right-10 h-[240px] w-[240px] rounded-full"
          style={{
            background:
              'radial-gradient(circle, color-mix(in oklch, var(--accent) 18%, transparent), transparent 70%)',
          }}
        />
        <div className="relative z-10 flex items-center justify-between gap-6 flex-wrap">
          <div className="max-w-[460px]">
            <div className="flex items-center gap-2.5 mb-2">
              <Sparkles className="h-6 w-6" style={{ color: 'var(--accent)' }} />
              <h2 className="t-h1 m-0 text-white">Pregúntale a TukiFact</h2>
            </div>
            <p className="t-body mt-0 mb-4" style={{ color: 'var(--slate-300)' }}>
              Genera reportes, busca comprobantes o resuelve dudas SUNAT en lenguaje natural.
            </p>
            <Button
              asChild
              style={{ background: 'var(--accent)', color: 'var(--accent-foreground)', fontWeight: 600 }}
            >
              <Link href="/ai">
                <Sparkles className="h-4 w-4 mr-2" /> Abrir asistente
              </Link>
            </Button>
          </div>
          <div className="flex flex-col gap-2 min-w-[220px]">
            {['Resumen del mes', 'Comprobantes rechazados hoy', 'Cómo emitir nota de crédito'].map((q) => (
              <Link
                key={q}
                href={`/ai?q=${encodeURIComponent(q)}`}
                className="inline-flex items-center gap-2 rounded-[var(--radius-md)] px-3.5 py-2.5 t-body-sm transition-colors"
                style={{
                  background: 'rgba(255,255,255,0.06)',
                  border: '1px solid rgba(255,255,255,0.12)',
                  color: 'var(--slate-200)',
                }}
              >
                <Send className="h-3.5 w-3.5" style={{ color: 'var(--accent)' }} />
                {q}
              </Link>
            ))}
          </div>
        </div>
      </div>

      {isLoading && (
        <p className="t-caption mt-4" style={{ color: 'var(--muted-foreground)' }}>
          Cargando datos…
        </p>
      )}
    </div>
  );
}
