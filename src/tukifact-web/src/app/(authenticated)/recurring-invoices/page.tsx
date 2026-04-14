'use client';

import { useEffect, useState, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import { api, type RecurringInvoiceResponse } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';
import { Plus, Repeat, Pause, Play, XCircle, ChevronLeft, ChevronRight } from 'lucide-react';
import { toast } from 'sonner';

const formatDate = (date: string) =>
  new Date(date + 'T00:00:00').toLocaleDateString('es-PE', {
    day: '2-digit', month: '2-digit', year: 'numeric',
  });

const freqLabel: Record<string, string> = {
  daily: 'Diaria',
  weekly: 'Semanal',
  biweekly: 'Quincenal',
  monthly: 'Mensual',
  yearly: 'Anual',
};

const statusBadge = (status: string) => {
  const map: Record<string, { label: string; variant: 'default' | 'secondary' | 'destructive' | 'outline' }> = {
    active: { label: 'Activa', variant: 'default' },
    paused: { label: 'Pausada', variant: 'secondary' },
    cancelled: { label: 'Cancelada', variant: 'destructive' },
    completed: { label: 'Completada', variant: 'outline' },
  };
  const s = map[status] || { label: status, variant: 'outline' as const };
  return <Badge variant={s.variant}>{s.label}</Badge>;
};

const docTypeLabel: Record<string, string> = { '01': 'Factura', '03': 'Boleta' };

interface ListResponse {
  items: RecurringInvoiceResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export default function RecurringInvoicesPage() {
  const router = useRouter();
  const [data, setData] = useState<ListResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState('');

  const fetchData = useCallback(async () => {
    setIsLoading(true);
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: '15' });
      if (statusFilter) params.set('status', statusFilter);
      const res = await api.get<ListResponse>(`/v1/recurring-invoices?${params}`);
      setData(res);
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  }, [page, statusFilter]);

  useEffect(() => { fetchData(); }, [fetchData]);

  const updateStatus = async (id: string, status: string) => {
    try {
      await api.put(`/v1/recurring-invoices/${id}`, { status });
      toast.success(`Estado actualizado`);
      fetchData();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    }
  };

  const totalPages = data ? Math.ceil(data.totalCount / 15) : 0;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Facturación Recurrente</h1>
          <p className="text-muted-foreground">
            {data ? `${data.totalCount} programaciones` : 'Cargando...'}
          </p>
        </div>
        <Button onClick={() => router.push('/recurring-invoices/new')}>
          <Plus className="mr-2 h-4 w-4" /> Nueva Recurrente
        </Button>
      </div>

      <Card>
        <CardContent className="pt-4">
          <Select
            value={statusFilter}
            onValueChange={(v) => { if (!v) return; setStatusFilter(v === 'all' ? '' : v); setPage(1); }}
          >
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Estado" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos</SelectItem>
              <SelectItem value="active">Activa</SelectItem>
              <SelectItem value="paused">Pausada</SelectItem>
              <SelectItem value="cancelled">Cancelada</SelectItem>
              <SelectItem value="completed">Completada</SelectItem>
            </SelectContent>
          </Select>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Tipo</TableHead>
                <TableHead>Serie</TableHead>
                <TableHead>Cliente</TableHead>
                <TableHead>Frecuencia</TableHead>
                <TableHead>Próxima emisión</TableHead>
                <TableHead className="text-center">Emitidas</TableHead>
                <TableHead>Estado</TableHead>
                <TableHead className="text-right">Acciones</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                Array.from({ length: 5 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 8 }).map((_, j) => (
                      <TableCell key={j}>
                        <div className="h-4 bg-muted animate-pulse rounded w-20" />
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              ) : !data?.items.length ? (
                <TableRow>
                  <TableCell colSpan={8} className="text-center py-8 text-muted-foreground">
                    <Repeat className="mx-auto h-8 w-8 mb-2 opacity-50" />
                    No hay facturaciones recurrentes
                  </TableCell>
                </TableRow>
              ) : (
                data.items.map((r) => (
                  <TableRow key={r.id}>
                    <TableCell>
                      <Badge variant="outline">{docTypeLabel[r.documentType] || r.documentType}</Badge>
                    </TableCell>
                    <TableCell className="font-mono">{r.serie}</TableCell>
                    <TableCell className="max-w-[180px] truncate">{r.customerName}</TableCell>
                    <TableCell>{freqLabel[r.frequency] || r.frequency}</TableCell>
                    <TableCell>
                      {r.nextEmissionDate ? formatDate(r.nextEmissionDate) : '—'}
                    </TableCell>
                    <TableCell className="text-center font-medium">{r.emittedCount}</TableCell>
                    <TableCell>{statusBadge(r.status)}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex gap-1 justify-end">
                        {r.status === 'active' && (
                          <Button variant="ghost" size="sm" onClick={() => updateStatus(r.id, 'paused')}>
                            <Pause className="h-4 w-4" />
                          </Button>
                        )}
                        {r.status === 'paused' && (
                          <Button variant="ghost" size="sm" onClick={() => updateStatus(r.id, 'active')}>
                            <Play className="h-4 w-4" />
                          </Button>
                        )}
                        {r.status !== 'cancelled' && r.status !== 'completed' && (
                          <Button variant="ghost" size="sm" onClick={() => updateStatus(r.id, 'cancelled')}>
                            <XCircle className="h-4 w-4 text-destructive" />
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {totalPages > 1 && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">Página {page} de {totalPages}</p>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>
              <ChevronLeft className="h-4 w-4 mr-1" /> Anterior
            </Button>
            <Button variant="outline" size="sm" disabled={page >= totalPages} onClick={() => setPage(p => p + 1)}>
              Siguiente <ChevronRight className="h-4 w-4 ml-1" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
