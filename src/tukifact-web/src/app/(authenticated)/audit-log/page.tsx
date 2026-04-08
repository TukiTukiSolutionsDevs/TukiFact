'use client';

import { useEffect, useState, useCallback } from 'react';
import { api } from '@/lib/api';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
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
import { ChevronLeft, ChevronRight, ScrollText } from 'lucide-react';

interface AuditEntry {
  id: string;
  action: string;
  entityType: string;
  entityId: string | null;
  details: string | null;
  userId: string | null;
  ipAddress: string | null;
  createdAt: string;
}

interface AuditResponse {
  data: AuditEntry[];
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

const ACTION_COLORS: Record<string, string> = {
  'document.created': 'bg-blue-100 text-blue-800',
  'creditnote.created': 'bg-purple-100 text-purple-800',
  'document.voided': 'bg-red-100 text-red-800',
  'user.login': 'bg-green-100 text-green-800',
  'user.created': 'bg-emerald-100 text-emerald-800',
  'webhook.created': 'bg-amber-100 text-amber-800',
  'apikey.generated': 'bg-orange-100 text-orange-800',
  'series.created': 'bg-cyan-100 text-cyan-800',
};

export default function AuditLogPage() {
  const [data, setData] = useState<AuditResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [entityFilter, setEntityFilter] = useState('');

  const fetchAuditLog = useCallback(async () => {
    setIsLoading(true);
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: '20' });
      if (entityFilter) params.set('entityType', entityFilter);
      setData(await api.get<AuditResponse>(`/v1/audit-log?${params}`));
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  }, [page, entityFilter]);

  useEffect(() => {
    fetchAuditLog();
  }, [fetchAuditLog]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Audit Log</h1>
        <p className="text-muted-foreground">Registro de todas las acciones en la plataforma</p>
      </div>

      <Card>
        <CardContent className="pt-4">
          <Select
            value={entityFilter || 'all'}
            onValueChange={(v) => {
              if (v == null) return;
              setEntityFilter(v !== 'all' ? v : '');
              setPage(1);
            }}
          >
            <SelectTrigger className="w-[200px]">
              <SelectValue placeholder="Filtrar por tipo" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos</SelectItem>
              <SelectItem value="Document">Documentos</SelectItem>
              <SelectItem value="User">Usuarios</SelectItem>
              <SelectItem value="Auth">Autenticación</SelectItem>
              <SelectItem value="ApiKey">API Keys</SelectItem>
              <SelectItem value="Webhook">Webhooks</SelectItem>
              <SelectItem value="Series">Series</SelectItem>
            </SelectContent>
          </Select>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Acción</TableHead>
                <TableHead>Tipo</TableHead>
                <TableHead>IP</TableHead>
                <TableHead>Fecha</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                Array.from({ length: 5 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 4 }).map((_, j) => (
                      <TableCell key={j}>
                        <div className="h-4 bg-muted animate-pulse rounded w-20" />
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              ) : !data || data.data.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={4} className="text-center py-8 text-muted-foreground">
                    <ScrollText className="mx-auto h-8 w-8 mb-2 opacity-50" />
                    No hay registros
                  </TableCell>
                </TableRow>
              ) : (
                data.data.map((entry) => (
                  <TableRow key={entry.id}>
                    <TableCell>
                      <Badge
                        className={
                          ACTION_COLORS[entry.action] || 'bg-gray-100 text-gray-800'
                        }
                      >
                        {entry.action}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline">{entry.entityType}</Badge>
                    </TableCell>
                    <TableCell className="font-mono text-xs">
                      {entry.ipAddress || '-'}
                    </TableCell>
                    <TableCell className="text-sm">
                      {new Date(entry.createdAt).toLocaleString('es-PE')}
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
            Página {data.pagination.page} de {data.pagination.totalPages} (
            {data.pagination.totalCount} registros)
          </p>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={page >= data.pagination.totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
