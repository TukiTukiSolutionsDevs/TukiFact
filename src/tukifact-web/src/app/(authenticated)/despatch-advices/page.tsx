'use client';

import { useEffect, useState, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import { api, type DespatchAdviceResponse, type PaginatedResponse } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';
import { Plus, Truck, Eye, ChevronLeft, ChevronRight } from 'lucide-react';

const formatDate = (date: string) =>
  new Date(date + 'T00:00:00').toLocaleDateString('es-PE', {
    day: '2-digit', month: '2-digit', year: 'numeric',
  });

const statusBadge = (status: string) => {
  const map: Record<string, { label: string; variant: 'default' | 'secondary' | 'destructive' | 'outline' }> = {
    accepted: { label: 'Aceptado', variant: 'default' },
    rejected: { label: 'Rechazado', variant: 'destructive' },
    draft: { label: 'Borrador', variant: 'outline' },
    signed: { label: 'Firmado', variant: 'outline' },
    sent: { label: 'Enviado', variant: 'secondary' },
    pending_ticket: { label: 'Pendiente ticket', variant: 'secondary' },
  };
  const s = map[status] || { label: status, variant: 'outline' as const };
  return <Badge variant={s.variant}>{s.label}</Badge>;
};

const transportMode: Record<string, string> = {
  '01': 'Transporte público',
  '02': 'Transporte privado',
};

export default function DespatchAdvicesPage() {
  const router = useRouter();
  const [data, setData] = useState<PaginatedResponse<DespatchAdviceResponse> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState('');

  const fetchData = useCallback(async () => {
    setIsLoading(true);
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: '15' });
      if (statusFilter) params.set('status', statusFilter);
      const res = await api.get<PaginatedResponse<DespatchAdviceResponse>>(
        `/v1/despatch-advices?${params}`
      );
      setData(res);
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  }, [page, statusFilter]);

  useEffect(() => { fetchData(); }, [fetchData]);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Guías de Remisión</h1>
          <p className="text-muted-foreground">
            {data ? `${data.pagination.totalCount} guías emitidas` : 'Cargando...'}
          </p>
        </div>
        <Button onClick={() => router.push('/despatch-advices/new')}>
          <Plus className="mr-2 h-4 w-4" /> Nueva Guía
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
              <SelectItem value="accepted">Aceptado</SelectItem>
              <SelectItem value="rejected">Rechazado</SelectItem>
              <SelectItem value="draft">Borrador</SelectItem>
            </SelectContent>
          </Select>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Número</TableHead>
                <TableHead>Fecha</TableHead>
                <TableHead>Destinatario</TableHead>
                <TableHead>Transporte</TableHead>
                <TableHead>Origen → Destino</TableHead>
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
              ) : !data?.data.length ? (
                <TableRow>
                  <TableCell colSpan={7} className="text-center py-8 text-muted-foreground">
                    <Truck className="mx-auto h-8 w-8 mb-2 opacity-50" />
                    No hay guías de remisión
                  </TableCell>
                </TableRow>
              ) : (
                data.data.map((g) => (
                  <TableRow
                    key={g.id}
                    className="cursor-pointer hover:bg-muted/50"
                    onClick={() => router.push(`/despatch-advices/${g.id}`)}
                  >
                    <TableCell className="font-mono font-medium">{g.fullNumber}</TableCell>
                    <TableCell>{formatDate(g.issueDate)}</TableCell>
                    <TableCell className="max-w-[150px] truncate">{g.recipientName}</TableCell>
                    <TableCell>
                      <Badge variant="outline" className="text-xs">
                        {transportMode[g.transportMode] || g.transportMode}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-xs max-w-[200px] truncate">
                      {g.originUbigeo} → {g.destinationUbigeo}
                    </TableCell>
                    <TableCell>{statusBadge(g.status)}</TableCell>
                    <TableCell className="text-right">
                      <Button
                        variant="ghost" size="sm"
                        onClick={(e) => { e.stopPropagation(); router.push(`/despatch-advices/${g.id}`); }}
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

      {data && data.pagination.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            Página {data.pagination.page} de {data.pagination.totalPages}
          </p>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>
              <ChevronLeft className="h-4 w-4 mr-1" /> Anterior
            </Button>
            <Button variant="outline" size="sm" disabled={page >= data.pagination.totalPages} onClick={() => setPage(p => p + 1)}>
              Siguiente <ChevronRight className="h-4 w-4 ml-1" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
