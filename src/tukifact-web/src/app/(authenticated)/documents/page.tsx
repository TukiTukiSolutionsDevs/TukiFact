'use client';

import { useEffect, useState, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import { api, type DocumentResponse, type PaginatedResponse } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Plus, FileText, Download, Eye, ChevronLeft, ChevronRight } from 'lucide-react';

const formatCurrency = (amount: number, currency = 'PEN') =>
  new Intl.NumberFormat('es-PE', { style: 'currency', currency }).format(amount);

const formatDate = (date: string) =>
  new Date(date + 'T00:00:00').toLocaleDateString('es-PE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });

const statusBadge = (status: string) => {
  const map: Record<
    string,
    { label: string; variant: 'default' | 'secondary' | 'destructive' | 'outline' }
  > = {
    accepted: { label: 'Aceptado', variant: 'default' },
    rejected: { label: 'Rechazado', variant: 'destructive' },
    voided: { label: 'Anulado', variant: 'secondary' },
    draft: { label: 'Borrador', variant: 'outline' },
    sent: { label: 'Enviado', variant: 'secondary' },
    signed: { label: 'Firmado', variant: 'outline' },
  };
  const s = map[status] || { label: status, variant: 'outline' as const };
  return <Badge variant={s.variant}>{s.label}</Badge>;
};

export default function DocumentsPage() {
  const router = useRouter();
  const [data, setData] = useState<PaginatedResponse<DocumentResponse> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [filters, setFilters] = useState({ documentType: '', status: '' });

  const fetchDocuments = useCallback(async () => {
    setIsLoading(true);
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: '15' });
      if (filters.documentType) params.set('documentType', filters.documentType);
      if (filters.status) params.set('status', filters.status);
      const res = await api.get<PaginatedResponse<DocumentResponse>>(
        `/v1/documents?${params}`
      );
      setData(res);
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  }, [page, filters]);

  useEffect(() => {
    fetchDocuments();
  }, [fetchDocuments]);

  const downloadPdf = async (id: string, fullNumber: string) => {
    const token = api.getToken();
    const res = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL || ''}/v1/documents/${id}/pdf`,
      { headers: { Authorization: `Bearer ${token}` } }
    );
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${fullNumber}.pdf`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Comprobantes</h1>
          <p className="text-muted-foreground">
            {data ? `${data.pagination.totalCount} documentos emitidos` : 'Cargando...'}
          </p>
        </div>
        <Button onClick={() => router.push('/documents/new')}>
          <Plus className="mr-2 h-4 w-4" /> Emitir Comprobante
        </Button>
      </div>

      {/* Filters */}
      <Card>
        <CardContent className="pt-4">
          <div className="flex gap-3">
            <Select
              value={filters.documentType}
              onValueChange={(v) => {
                if (v == null) return;
                setFilters((f) => ({ ...f, documentType: v === 'all' ? '' : v }));
                setPage(1);
              }}
            >
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="Tipo" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos los tipos</SelectItem>
                <SelectItem value="01">Factura</SelectItem>
                <SelectItem value="03">Boleta</SelectItem>
                <SelectItem value="07">Nota de Crédito</SelectItem>
                <SelectItem value="08">Nota de Débito</SelectItem>
              </SelectContent>
            </Select>
            <Select
              value={filters.status}
              onValueChange={(v) => {
                if (v == null) return;
                setFilters((f) => ({ ...f, status: v === 'all' ? '' : v }));
                setPage(1);
              }}
            >
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="Estado" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos los estados</SelectItem>
                <SelectItem value="accepted">Aceptado</SelectItem>
                <SelectItem value="rejected">Rechazado</SelectItem>
                <SelectItem value="voided">Anulado</SelectItem>
                <SelectItem value="draft">Borrador</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Table */}
      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Número</TableHead>
                <TableHead>Tipo</TableHead>
                <TableHead>Fecha</TableHead>
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
              ) : data?.data.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} className="text-center py-8 text-muted-foreground">
                    <FileText className="mx-auto h-8 w-8 mb-2 opacity-50" />
                    No hay comprobantes
                  </TableCell>
                </TableRow>
              ) : (
                data?.data.map((doc) => (
                  <TableRow
                    key={doc.id}
                    className="cursor-pointer hover:bg-muted/50"
                    onClick={() => router.push(`/documents/${doc.id}`)}
                  >
                    <TableCell className="font-mono font-medium">{doc.fullNumber}</TableCell>
                    <TableCell>
                      <Badge variant="outline" className="text-xs">
                        {doc.documentTypeName}
                      </Badge>
                    </TableCell>
                    <TableCell>{formatDate(doc.issueDate)}</TableCell>
                    <TableCell className="max-w-[200px] truncate">{doc.customerName}</TableCell>
                    <TableCell className="text-right font-medium">
                      {formatCurrency(doc.total, doc.currency)}
                    </TableCell>
                    <TableCell>{statusBadge(doc.status)}</TableCell>
                    <TableCell className="text-right">
                      <div
                        className="flex gap-1 justify-end"
                        onClick={(e) => e.stopPropagation()}
                      >
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => router.push(`/documents/${doc.id}`)}
                        >
                          <Eye className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => downloadPdf(doc.id, doc.fullNumber)}
                        >
                          <Download className="h-4 w-4" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Pagination */}
      {data && data.pagination.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            Página {data.pagination.page} de {data.pagination.totalPages}
          </p>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
            >
              <ChevronLeft className="h-4 w-4 mr-1" /> Anterior
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={page >= data.pagination.totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              Siguiente <ChevronRight className="h-4 w-4 ml-1" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
