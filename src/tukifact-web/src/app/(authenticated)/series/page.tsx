'use client';

import { useEffect, useState } from 'react';
import { api, type SeriesResponse } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Plus } from 'lucide-react';
import { toast } from 'sonner';

const DOC_TYPES: Record<string, string> = {
  '01': 'Factura', '03': 'Boleta', '07': 'Nota de Crédito', '08': 'Nota de Débito',
};

export default function SeriesPage() {
  const [series, setSeries] = useState<SeriesResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isOpen, setIsOpen] = useState(false);
  const [newSeries, setNewSeries] = useState({ documentType: '01', serie: '', emissionPoint: 'PRINCIPAL' });

  const fetchSeries = async () => {
    setIsLoading(true);
    try { setSeries(await api.get<SeriesResponse[]>('/v1/series')); }
    catch (err) { console.error(err); }
    finally { setIsLoading(false); }
  };

  useEffect(() => { fetchSeries(); }, []);

  const handleCreate = async () => {
    if (!newSeries.serie || newSeries.serie.length !== 4) { toast.error('La serie debe tener 4 caracteres (ej: F001)'); return; }
    try {
      await api.post('/v1/series', newSeries);
      toast.success(`Serie ${newSeries.serie} creada`);
      setIsOpen(false);
      setNewSeries({ documentType: '01', serie: '', emissionPoint: 'PRINCIPAL' });
      fetchSeries();
    } catch (err) { toast.error(err instanceof Error ? err.message : 'Error'); }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Series</h1>
          <p className="text-muted-foreground">Gestiona las series de tus comprobantes</p>
        </div>
        <Dialog open={isOpen} onOpenChange={setIsOpen}>
          <DialogTrigger render={<Button />}>
            <Plus className="mr-2 h-4 w-4" /> Nueva Serie
          </DialogTrigger>
          <DialogContent>
            <DialogHeader><DialogTitle>Crear Serie</DialogTitle></DialogHeader>
            <div className="space-y-4">
              <div>
                <Label>Tipo de Documento</Label>
                <Select value={newSeries.documentType} onValueChange={v => v != null && setNewSeries(s => ({ ...s, documentType: v }))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {Object.entries(DOC_TYPES).map(([k, v]) => <SelectItem key={k} value={k}>{v}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label>Serie (4 caracteres)</Label>
                <Input placeholder="F001" maxLength={4} value={newSeries.serie}
                  onChange={e => setNewSeries(s => ({ ...s, serie: e.target.value.toUpperCase() }))} />
              </div>
              <Button onClick={handleCreate} className="w-full">Crear Serie</Button>
            </div>
          </DialogContent>
        </Dialog>
      </div>

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Serie</TableHead>
                <TableHead>Tipo</TableHead>
                <TableHead>Punto de Emisión</TableHead>
                <TableHead className="text-right">Correlativo Actual</TableHead>
                <TableHead>Estado</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                Array.from({ length: 3 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 5 }).map((_, j) => (
                      <TableCell key={j}><div className="h-4 bg-muted animate-pulse rounded w-16" /></TableCell>
                    ))}
                  </TableRow>
                ))
              ) : series.map(s => (
                <TableRow key={s.id}>
                  <TableCell className="font-mono font-medium">{s.serie}</TableCell>
                  <TableCell>{DOC_TYPES[s.documentType] || s.documentType}</TableCell>
                  <TableCell>{s.emissionPoint}</TableCell>
                  <TableCell className="text-right font-mono">{s.currentCorrelative}</TableCell>
                  <TableCell>
                    <Badge variant={s.isActive ? 'default' : 'secondary'}>
                      {s.isActive ? 'Activa' : 'Inactiva'}
                    </Badge>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
