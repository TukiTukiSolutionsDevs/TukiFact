'use client';

import { useState, useCallback, useMemo } from 'react';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Section } from '@/components/ui/section';
import { Toolbar, ChipGroup } from '@/components/ui/toolbar';
import { PillGroup, type PillOption } from '@/components/ui/pill-group';
import {
  ArrowRightLeft,
  Search,
  TrendingUp,
  TrendingDown,
  RefreshCw,
  DollarSign,
  Euro,
  AlertTriangle,
  Info,
} from 'lucide-react';

type Currency = 'USD' | 'EUR';
type Direction = 'sell' | 'buy';
type QuickDate = 'today' | 'yesterday' | 'minus7' | '';

interface ExchangeRate {
  date: string;
  currency: string;
  buyRate: number;
  sellRate: number;
  source: string;
  fetchedAt: string;
}

const CURRENCY_OPTIONS: readonly PillOption<Currency>[] = [
  { value: 'USD', label: 'USD', sub: 'Dólar EE.UU.', icon: DollarSign },
  { value: 'EUR', label: 'EUR', sub: 'Euro', icon: Euro },
];

const DIRECTION_OPTIONS: readonly PillOption<Direction>[] = [
  { value: 'sell', label: 'Venta', sub: 'SUNAT oficial', icon: TrendingUp },
  { value: 'buy', label: 'Compra', sub: 'Compra de USD', icon: TrendingDown },
];

const QUICK_DATE_OPTIONS = [
  { value: 'today' as QuickDate, label: 'Hoy' },
  { value: 'yesterday' as QuickDate, label: 'Ayer' },
  { value: 'minus7' as QuickDate, label: '-7d' },
] as const;

function todayIso() {
  return new Date().toISOString().split('T')[0];
}

function shiftIso(days: number) {
  const d = new Date();
  d.setDate(d.getDate() + days);
  return d.toISOString().split('T')[0];
}

function formatRate(rate: number) {
  return rate.toFixed(4);
}

function formatLongDate(date: string) {
  return new Date(date + 'T12:00:00').toLocaleDateString('es-PE', {
    weekday: 'long',
    day: '2-digit',
    month: 'long',
    year: 'numeric',
  });
}

function previousFridayIso(date: string) {
  const d = new Date(date + 'T12:00:00');
  do {
    d.setDate(d.getDate() - 1);
  } while (d.getDay() !== 5);
  return d.toISOString().split('T')[0];
}

export default function ExchangeRatesPage() {
  const [date, setDate] = useState(todayIso);
  const [currency, setCurrency] = useState<Currency>('USD');
  const [rate, setRate] = useState<ExchangeRate | null>(null);
  const [history, setHistory] = useState<ExchangeRate[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  const [amount, setAmount] = useState('1');
  const [direction, setDirection] = useState<Direction>('sell');

  const isFuture = useMemo(() => date > todayIso(), [date]);

  const activeQuickDate: QuickDate = useMemo(() => {
    if (date === todayIso()) return 'today';
    if (date === shiftIso(-1)) return 'yesterday';
    if (date === shiftIso(-7)) return 'minus7';
    return '';
  }, [date]);

  const applyQuickDate = (q: QuickDate) => {
    if (q === 'today') setDate(todayIso());
    else if (q === 'yesterday') setDate(shiftIso(-1));
    else if (q === 'minus7') setDate(shiftIso(-7));
  };

  const fetchRate = useCallback(
    async (targetDate: string = date) => {
      if (targetDate > todayIso()) {
        setError('La fecha no puede ser futura');
        return;
      }
      setIsLoading(true);
      setError('');
      try {
        const params = new URLSearchParams({ date: targetDate, currency });
        const res = await api.get<ExchangeRate>(`/v1/utils/exchange-rate?${params}`);
        setRate(res);
        setHistory((prev) => {
          const exists = prev.some((r) => r.date === res.date && r.currency === res.currency);
          if (exists) return prev;
          return [res, ...prev].slice(0, 30);
        });
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Error al consultar tipo de cambio');
        setRate(null);
      } finally {
        setIsLoading(false);
      }
    },
    [date, currency]
  );

  const fetchWeek = useCallback(async () => {
    setIsLoading(true);
    setError('');
    try {
      const results: ExchangeRate[] = [];
      const baseDate = new Date(date + 'T12:00:00');
      for (let i = 0; i < 7; i++) {
        const d = new Date(baseDate);
        d.setDate(d.getDate() - i);
        const dayStr = d.toISOString().split('T')[0];
        if (dayStr > todayIso()) continue;
        try {
          const params = new URLSearchParams({ date: dayStr, currency });
          const res = await api.get<ExchangeRate>(`/v1/utils/exchange-rate?${params}`);
          results.push(res);
        } catch {
          /* fin de semana o feriado, salteo */
        }
      }
      if (results.length > 0) {
        setRate(results[0]);
        setHistory(results);
      } else {
        setError('No se encontraron tipos de cambio para la semana');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al consultar');
    } finally {
      setIsLoading(false);
    }
  }, [date, currency]);

  const amountValue = parseFloat(amount || '0');
  const activeRate = rate ? (direction === 'sell' ? rate.sellRate : rate.buyRate) : 0;
  const converted = amountValue * activeRate;
  const inverseConverted = activeRate ? amountValue / activeRate : 0;

  return (
    <div className="space-y-[var(--gap-cards,1.5rem)]">
      <header>
        <h1 className="t-display-lg m-0">Tipo de cambio</h1>
        <p className="t-body-sm m-0 mt-1" style={{ color: 'var(--muted-foreground)' }}>
          Cotización oficial SBS para facturación electrónica.
        </p>
      </header>

      <Toolbar>
        <div className="flex flex-col gap-2 min-w-[200px]">
          <Label className="t-overline" style={{ color: 'var(--muted-foreground)' }}>
            Moneda
          </Label>
          <PillGroup<Currency>
            value={currency}
            onChange={setCurrency}
            options={CURRENCY_OPTIONS}
            cols={2}
          />
        </div>

        <div className="flex flex-col gap-2 min-w-[180px]">
          <Label
            htmlFor="date"
            className="t-overline"
            style={{ color: 'var(--muted-foreground)' }}
          >
            Fecha
          </Label>
          <Input
            id="date"
            type="date"
            value={date}
            max={todayIso()}
            onChange={(e) => setDate(e.target.value)}
            className="tnum"
          />
          <ChipGroup<QuickDate>
            value={activeQuickDate}
            onChange={applyQuickDate}
            options={QUICK_DATE_OPTIONS}
          />
        </div>

        <div className="flex items-end gap-2 ml-auto">
          <Button onClick={() => fetchRate()} disabled={isLoading || isFuture}>
            <Search className="mr-2 h-4 w-4" />
            {isLoading ? 'Consultando…' : 'Consultar'}
          </Button>
          <Button variant="outline" onClick={fetchWeek} disabled={isLoading}>
            <RefreshCw className="mr-2 h-4 w-4" />
            Últimos 7 días
          </Button>
        </div>
      </Toolbar>

      {error && (
        <Section className="border-[color:var(--warning)]/40">
          <div className="flex items-start gap-3">
            <span
              className="flex h-9 w-9 items-center justify-center rounded-full shrink-0"
              style={{
                background: 'color-mix(in oklch, var(--warning) 14%, transparent)',
                color: 'var(--warning)',
              }}
            >
              <AlertTriangle className="h-4 w-4" />
            </span>
            <div className="flex-1">
              <p className="t-body font-medium m-0">{error}</p>
              <p
                className="t-body-sm m-0 mt-1"
                style={{ color: 'var(--muted-foreground)' }}
              >
                No hay cotización SBS para sábados, domingos ni feriados.
              </p>
              <button
                type="button"
                onClick={() => {
                  const friday = previousFridayIso(date);
                  setDate(friday);
                  fetchRate(friday);
                }}
                className="t-caption font-semibold mt-2 rounded-full px-3 py-1.5"
                style={{
                  background: 'color-mix(in oklch, var(--accent) 18%, transparent)',
                  color: 'var(--brand-ink)',
                  border: '1px solid var(--accent)',
                }}
              >
                Buscar viernes anterior
              </button>
            </div>
          </div>
        </Section>
      )}

      {rate && (
        <div className="grid gap-[var(--gap-cards,1.5rem)] md:grid-cols-2">
          <Section
            title={`${rate.currency} / PEN`}
            desc={formatLongDate(rate.date)}
            right={
              <span
                className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 t-caption font-semibold"
                style={{
                  color: 'var(--info)',
                  background: 'color-mix(in oklch, var(--info) 14%, transparent)',
                }}
              >
                <ArrowRightLeft className="h-3 w-3" />
                {rate.source}
              </span>
            }
          >
            <div className="grid grid-cols-2 gap-4">
              <div
                className="rounded-[var(--radius-md)] border p-4"
                style={{
                  background: 'color-mix(in oklch, var(--success) 8%, transparent)',
                  borderColor: 'color-mix(in oklch, var(--success) 30%, transparent)',
                }}
              >
                <p
                  className="t-overline flex items-center gap-1.5 m-0"
                  style={{ color: 'var(--success)' }}
                >
                  <TrendingDown className="h-3 w-3" /> Compra
                </p>
                <p className="t-num-lg mono tnum mt-1 m-0" style={{ color: 'var(--success)' }}>
                  S/ {formatRate(rate.buyRate)}
                </p>
              </div>
              <div
                className="rounded-[var(--radius-md)] border p-4"
                style={{
                  background: 'color-mix(in oklch, var(--info) 8%, transparent)',
                  borderColor: 'color-mix(in oklch, var(--info) 30%, transparent)',
                }}
              >
                <p
                  className="t-overline flex items-center gap-1.5 m-0"
                  style={{ color: 'var(--info)' }}
                >
                  <TrendingUp className="h-3 w-3" /> Venta
                </p>
                <p className="t-num-lg mono tnum mt-1 m-0" style={{ color: 'var(--info)' }}>
                  S/ {formatRate(rate.sellRate)}
                </p>
              </div>
            </div>
            <p
              className="t-caption mt-3 flex items-center gap-1"
              style={{ color: 'var(--muted-foreground)' }}
            >
              <Info className="h-3 w-3" />
              Cacheado el {new Date(rate.fetchedAt).toLocaleString('es-PE')}
            </p>
          </Section>

          <Section title="Convertidor" desc="Calcula PEN ↔ moneda extranjera con el TC del día">
            <div className="space-y-4">
              <div>
                <Label
                  className="t-overline mb-2 block"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Tipo de cambio
                </Label>
                <PillGroup<Direction>
                  value={direction}
                  onChange={setDirection}
                  options={DIRECTION_OPTIONS}
                  cols={2}
                />
              </div>

              <div>
                <Label
                  htmlFor="amount"
                  className="t-overline mb-2 block"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Monto en {rate.currency}
                </Label>
                <Input
                  id="amount"
                  type="number"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  placeholder="0.00"
                  min="0"
                  step="0.01"
                  className="t-num-md mono tnum"
                />
              </div>

              <div
                className="rounded-[var(--radius-md)] p-4 space-y-3"
                style={{ background: 'var(--muted)' }}
              >
                <div className="flex items-baseline justify-between gap-3">
                  <span
                    className="t-caption tnum mono"
                    style={{ color: 'var(--muted-foreground)' }}
                  >
                    {amountValue.toFixed(2)} {rate.currency} →
                  </span>
                  <span className="t-num-lg mono tnum">S/ {converted.toFixed(2)}</span>
                </div>
                <div
                  className="flex items-baseline justify-between gap-3 pt-3 border-t"
                  style={{ borderColor: 'var(--border)' }}
                >
                  <span
                    className="t-caption tnum mono"
                    style={{ color: 'var(--muted-foreground)' }}
                  >
                    S/ {amountValue.toFixed(2)} →
                  </span>
                  <span className="t-num-md mono tnum">
                    {rate.currency} {inverseConverted.toFixed(4)}
                  </span>
                </div>
                <p className="t-caption m-0" style={{ color: 'var(--muted-foreground)' }}>
                  TC {direction === 'sell' ? 'venta' : 'compra'}: S/ {formatRate(activeRate)}
                </p>
              </div>
            </div>
          </Section>
        </div>
      )}

      {history.length > 0 && (
        <Section
          title="Historial"
          desc={`Últimas ${history.length} consultas en esta sesión`}
        >
          <div className="overflow-x-auto -mx-6">
            <table className="w-full">
              <thead>
                <tr
                  className="border-b"
                  style={{ borderColor: 'var(--border)' }}
                >
                  <th className="t-overline text-left px-6 py-2.5" style={{ color: 'var(--muted-foreground)' }}>
                    Fecha
                  </th>
                  <th className="t-overline text-left px-3 py-2.5" style={{ color: 'var(--muted-foreground)' }}>
                    Moneda
                  </th>
                  <th className="t-overline text-right px-3 py-2.5" style={{ color: 'var(--muted-foreground)' }}>
                    Compra
                  </th>
                  <th className="t-overline text-right px-3 py-2.5" style={{ color: 'var(--muted-foreground)' }}>
                    Venta
                  </th>
                  <th className="t-overline text-left px-6 py-2.5" style={{ color: 'var(--muted-foreground)' }}>
                    Fuente
                  </th>
                </tr>
              </thead>
              <tbody>
                {history.map((r, i) => (
                  <tr
                    key={`${r.date}-${r.currency}-${i}`}
                    className="border-b last:border-0"
                    style={{ borderColor: 'var(--border)' }}
                  >
                    <td className="px-6 py-2.5 t-body-sm tnum">
                      {new Date(r.date + 'T12:00:00').toLocaleDateString('es-PE')}
                    </td>
                    <td className="px-3 py-2.5">
                      <span
                        className="inline-flex items-center rounded-full px-2 py-0.5 t-caption font-semibold"
                        style={{
                          background: 'var(--muted)',
                          color: 'var(--muted-foreground)',
                        }}
                      >
                        {r.currency}
                      </span>
                    </td>
                    <td
                      className="px-3 py-2.5 text-right mono tnum"
                      style={{ color: 'var(--success)' }}
                    >
                      {formatRate(r.buyRate)}
                    </td>
                    <td
                      className="px-3 py-2.5 text-right mono tnum"
                      style={{ color: 'var(--info)' }}
                    >
                      {formatRate(r.sellRate)}
                    </td>
                    <td
                      className="px-6 py-2.5 t-caption"
                      style={{ color: 'var(--muted-foreground)' }}
                    >
                      {r.source}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Section>
      )}
    </div>
  );
}
