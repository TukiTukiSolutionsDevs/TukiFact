'use client';

import { useState, useCallback } from 'react';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { ArrowRightLeft, Search, TrendingUp, TrendingDown, RefreshCw, Calendar } from 'lucide-react';

interface ExchangeRate {
  date: string;
  currency: string;
  buyRate: number;
  sellRate: number;
  source: string;
  fetchedAt: string;
}

const formatRate = (rate: number) => rate.toFixed(4);
const formatDate = (date: string) =>
  new Date(date + 'T00:00:00').toLocaleDateString('es-PE', {
    weekday: 'long', day: '2-digit', month: 'long', year: 'numeric',
  });

export default function ExchangeRatesPage() {
  const [date, setDate] = useState(() => new Date().toISOString().split('T')[0]);
  const [currency, setCurrency] = useState('USD');
  const [rate, setRate] = useState<ExchangeRate | null>(null);
  const [history, setHistory] = useState<ExchangeRate[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  // Amount converter
  const [amount, setAmount] = useState('1');
  const [direction, setDirection] = useState<'buy' | 'sell'>('sell');

  const fetchRate = useCallback(async () => {
    setIsLoading(true);
    setError('');
    try {
      const params = new URLSearchParams({ date, currency });
      const res = await api.get<ExchangeRate>(`/v1/utils/exchange-rate?${params}`);
      setRate(res);
      // Add to history if not already there
      setHistory(prev => {
        const exists = prev.some(r => r.date === res.date && r.currency === res.currency);
        if (exists) return prev;
        return [res, ...prev].slice(0, 30);
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al consultar tipo de cambio');
      setRate(null);
    } finally {
      setIsLoading(false);
    }
  }, [date, currency]);

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
        try {
          const params = new URLSearchParams({ date: dayStr, currency });
          const res = await api.get<ExchangeRate>(`/v1/utils/exchange-rate?${params}`);
          results.push(res);
        } catch {
          // Weekend or holiday — skip
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

  const converted = rate
    ? direction === 'sell'
      ? (parseFloat(amount || '0') * rate.sellRate)
      : (parseFloat(amount || '0') * rate.buyRate)
    : 0;

  const inverseConverted = rate
    ? direction === 'sell'
      ? (parseFloat(amount || '0') / rate.sellRate)
      : (parseFloat(amount || '0') / rate.buyRate)
    : 0;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Tipo de Cambio</h1>
        <p className="text-muted-foreground">
          Consulta el tipo de cambio oficial SBS/SUNAT
        </p>
      </div>

      {/* Search */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex flex-wrap items-end gap-4">
            <div className="space-y-2">
              <Label htmlFor="date">Fecha</Label>
              <div className="relative">
                <Calendar className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
                <Input
                  id="date"
                  type="date"
                  value={date}
                  onChange={(e) => setDate(e.target.value)}
                  className="pl-9 w-[180px]"
                />
              </div>
            </div>
            <div className="space-y-2">
              <Label>Moneda</Label>
              <Select value={currency} onValueChange={(v) => v && setCurrency(v)}>
                <SelectTrigger className="w-[140px]">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="USD">USD - Dólar</SelectItem>
                  <SelectItem value="EUR">EUR - Euro</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <Button onClick={fetchRate} disabled={isLoading}>
              <Search className="mr-2 h-4 w-4" />
              {isLoading ? 'Consultando...' : 'Consultar'}
            </Button>
            <Button variant="outline" onClick={fetchWeek} disabled={isLoading}>
              <RefreshCw className="mr-2 h-4 w-4" />
              Últimos 7 días
            </Button>
          </div>
          {error && (
            <p className="text-sm text-destructive mt-3">{error}</p>
          )}
        </CardContent>
      </Card>

      {/* Current Rate + Converter */}
      {rate && (
        <div className="grid gap-6 md:grid-cols-2">
          {/* Rate Card */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <ArrowRightLeft className="h-5 w-5" />
                {rate.currency}/PEN
              </CardTitle>
              <CardDescription>{formatDate(rate.date)}</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1">
                  <p className="text-sm text-muted-foreground flex items-center gap-1">
                    <TrendingDown className="h-3.5 w-3.5 text-green-500" /> Compra
                  </p>
                  <p className="text-3xl font-bold text-green-600 dark:text-green-400">
                    S/ {formatRate(rate.buyRate)}
                  </p>
                </div>
                <div className="space-y-1">
                  <p className="text-sm text-muted-foreground flex items-center gap-1">
                    <TrendingUp className="h-3.5 w-3.5 text-blue-500" /> Venta
                  </p>
                  <p className="text-3xl font-bold text-blue-600 dark:text-blue-400">
                    S/ {formatRate(rate.sellRate)}
                  </p>
                </div>
              </div>
              <div className="mt-4 flex items-center gap-2">
                <Badge variant="outline">{rate.source}</Badge>
                <span className="text-xs text-muted-foreground">
                  Obtenido: {new Date(rate.fetchedAt).toLocaleString('es-PE')}
                </span>
              </div>
            </CardContent>
          </Card>

          {/* Converter */}
          <Card>
            <CardHeader>
              <CardTitle>Convertidor</CardTitle>
              <CardDescription>Calcula el equivalente en soles o dólares</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label>Tipo</Label>
                <Select value={direction} onValueChange={(v) => v && setDirection(v as 'buy' | 'sell')}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="sell">Venta (compro dólares → pago soles)</SelectItem>
                    <SelectItem value="buy">Compra (vendo dólares → recibo soles)</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>{rate.currency}</Label>
                <Input
                  type="number"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  placeholder="Monto"
                  min="0"
                  step="0.01"
                />
              </div>
              <div className="rounded-lg bg-muted p-4 space-y-2">
                <div className="flex justify-between">
                  <span className="text-sm text-muted-foreground">
                    {amount} {rate.currency} →
                  </span>
                  <span className="font-bold text-lg">
                    S/ {converted.toFixed(2)}
                  </span>
                </div>
                <div className="flex justify-between border-t pt-2">
                  <span className="text-sm text-muted-foreground">
                    S/ {amount} →
                  </span>
                  <span className="font-medium">
                    {rate.currency} {inverseConverted.toFixed(2)}
                  </span>
                </div>
                <p className="text-xs text-muted-foreground">
                  TC {direction === 'sell' ? 'venta' : 'compra'}: {formatRate(direction === 'sell' ? rate.sellRate : rate.buyRate)}
                </p>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* History */}
      {history.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Historial</CardTitle>
            <CardDescription>Últimas consultas de tipo de cambio</CardDescription>
          </CardHeader>
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Fecha</TableHead>
                  <TableHead>Moneda</TableHead>
                  <TableHead className="text-right">Compra</TableHead>
                  <TableHead className="text-right">Venta</TableHead>
                  <TableHead>Fuente</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {history.map((r, i) => (
                  <TableRow key={`${r.date}-${r.currency}-${i}`}>
                    <TableCell className="font-medium">
                      {new Date(r.date + 'T00:00:00').toLocaleDateString('es-PE')}
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline">{r.currency}</Badge>
                    </TableCell>
                    <TableCell className="text-right font-mono text-green-600 dark:text-green-400">
                      {formatRate(r.buyRate)}
                    </TableCell>
                    <TableCell className="text-right font-mono text-blue-600 dark:text-blue-400">
                      {formatRate(r.sellRate)}
                    </TableCell>
                    <TableCell className="text-muted-foreground text-sm">{r.source}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
