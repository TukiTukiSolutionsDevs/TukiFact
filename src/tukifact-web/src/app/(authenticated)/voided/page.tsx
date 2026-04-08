'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Ban } from 'lucide-react';

interface VoidedDoc {
  id: string;
  ticketNumber: string;
  status: string;
  sunatTicket: string | null;
  sunatResponseCode: string | null;
  sunatResponseDescription: string | null;
  createdAt: string;
}

export default function VoidedPage() {
  const [items, setItems] = useState<VoidedDoc[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    api.get<VoidedDoc[]>('/v1/voided-documents')
      .then(setItems)
      .catch(console.error)
      .finally(() => setIsLoading(false));
  }, []);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Comunicaciones de Baja</h1>
        <p className="text-muted-foreground">Documentos anulados ante SUNAT</p>
      </div>

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Ticket</TableHead>
                <TableHead>Estado</TableHead>
                <TableHead>SUNAT Ticket</TableHead>
                <TableHead>Respuesta</TableHead>
                <TableHead>Fecha</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                Array.from({ length: 3 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 5 }).map((_, j) => (
                      <TableCell key={j}><div className="h-4 bg-muted animate-pulse rounded w-20" /></TableCell>
                    ))}
                  </TableRow>
                ))
              ) : items.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} className="text-center py-8 text-muted-foreground">
                    <Ban className="mx-auto h-8 w-8 mb-2 opacity-50" />
                    No hay comunicaciones de baja
                  </TableCell>
                </TableRow>
              ) : items.map(v => (
                <TableRow key={v.id}>
                  <TableCell className="font-mono">{v.ticketNumber}</TableCell>
                  <TableCell><Badge variant={v.status === 'accepted' ? 'default' : 'secondary'}>{v.status}</Badge></TableCell>
                  <TableCell className="font-mono text-xs">{v.sunatTicket || '-'}</TableCell>
                  <TableCell className="text-sm">{v.sunatResponseDescription || '-'}</TableCell>
                  <TableCell>{new Date(v.createdAt).toLocaleDateString('es-PE')}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
