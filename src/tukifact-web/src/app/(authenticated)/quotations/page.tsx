'use client';

import { useEffect, useState, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import { api, type QuotationResponse } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';
import { Plus, FileSpreadsheet, Eye, ChevronLeft, ChevronRight } from 'lucide-react';

const formatCurrency = (amount: number, currency = 'PEN') =>
  new Intl.NumberFormat('es-PE', { style: 'currency', currency }).format(amount);

const formatDate = (date: string) =>
  new Date(date + 'T00:00:00').toLocaleDateString('es-PE', {
    day: '2-digit', month: '2-digit', year: 'numeric',
  });

const statusBadge = (status: string) => {
  const map: Record<string, { label: string; variant: 'default' | 'secondary' | 'destructive' | 'outline' }> = {
    draft: { label: 'Borrador', variant: 'outline' },
    sent: { label: 'Enviada', variant: 'secondary' },
    approved: { label: 'Aprobada', variant: 'default' },
    invoiced: { label: 'Facturada', variant: 'default' },
    cancelled: { label: 'Cancelada', variant: 'destructive' },
    expired: { label: 'Vencida', variant: 'secondary' },
  };
  const s = map[status] || { label: status, variant: 'outline' as const };
  return <Badge variant={s.variant}>{s.label}</Badge>;
};

interface ListResponse {
  items: QuotationResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export default function QuotationsPage() {
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
      const res = await api.get<ListResponse>(`/v1/quotations?${params}`);
      setData(res);
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  }, [page, statusFilter]);

  useEffect(() => { fetchData(); }, [fetchData]);

  const totalPages = data ? Math.ceil(data.totalCount / 15) : 0;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Cotizaciones</h1>
          <p className="text-muted-foreground">
            {data ? `${data.totalCount} cotizaciones` : 'Cargando...'}
          </p>
        </div>
        <Button onClick={() => router.push('/quotations/new')}>
          <Plus className="mr-2 h-4 w-4" /> Nueva Cotización
        </Button>
      </div>

      <Card>
        <CardContent className="pt-4">
          <div className="flex gap-3">
            <Select
              value={statusFilter}
              onValueChange={(v) => { if (!v) return; setStatusFilter(v === 'all' ? '' : v); setPage(1); }}
            >
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="Estado" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos</SelectItem>
                <SelectItem value="draft">Borrador</SelectItem>
                <SelectItem value="sent">Enviada</SelectItem>
                <SelectItem value="approved">Aprobada</SelectItem>
                <SelectItem value="invoiced">Facturada</SelectItem>
                <SelectItem value="cancelled">Cancelada</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Número</TableHead>
                <TableHead>Fecha</TableHead>
                <TableHead>Válida hasta</TableHead>
                <TableHead>Cliente</TableHead>
                <TableHead className="text-right">Total</TableHead>
                <TableHead>Estado</TableHead>
                <TableHead className="text-right">Acciones</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                Array.from({ length: 5 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 7 }).map((_, j) => (
                      <TableCell key={j}>
                        <div className="h-4 bg-muted animate-pulse rounded w-20" />
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              ) : !data?.items.length ? (
                <TableRow>
                  <TableCell colSpan={7} className="text-center py-8 text-muted-foreground">
                    <FileSpreadsheet className="mx-auto h-8 w-8 mb-2 opacity-50" />
                    No hay cotizaciones
                  </TableCell>
                </TableRow>
              ) : (
                data.items.map((q) => (
                  <TableRow
                    key={q.id}
                    className="cursor-pointer hover:bg-muted/50"
                    onClick={() => router.push(`/quotations/${q.id}`)}
                  >
                    <TableCell className="font-mono font-medium">{q.quotationNumber}</TableCell>
                    <TableCell>{formatDate(q.issueDate)}</TableCell>
                    <TableCell>{formatDate(q.validUntil)}</TableCell>
                    <TableCell className="max-w-[200px] truncate">{q.customerName}</TableCell>
                    <TableCell className="text-right font-medium">
                      {formatCurrency(q.total, q.currency)}
                    </TableCell>
                    <TableCell>{statusBadge(q.status)}</TableCell>
                    <TableCell className="text-right">
                      <Button
                        variant="ghost" size="sm"
                        onClick={(e) => { e.stopPropagation(); router.push(`/quotations/${q.id}`); }}
                      >
                        <Eye className="h-4 w-4" />
                      </Button>
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
