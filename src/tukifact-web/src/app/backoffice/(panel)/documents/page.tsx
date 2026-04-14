'use client';

import { useState, useCallback } from 'react';
import { api, type PaginatedResponse } from '@/lib/api';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { Search, FileSearch, ChevronLeft, ChevronRight } from 'lucide-react';

interface DocRow {
  id: string;
  tenantId: string;
  documentType: string;
  serie: string;
  correlative: number;
  fullNumber: string;
  customerName: string;
  customerDocNumber: string;
  total: number;
  status: string;
  createdAt: string;
}

export default function BackofficeDocumentsPage() {
  const [docs, setDocs] = useState<DocRow[]>([]);
  const [pagination, setPagination] = useState({ page: 1, pageSize: 20, totalCount: 0, totalPages: 0 });
  const [filters, setFilters] = useState({ ruc: '', serie: '', correlative: '', customerDocNumber: '' });
  const [loading, setLoading] = useState(false);
  const [searched, setSearched] = useState(false);

  const fetchDocs = useCallback(async (page: number) => {
    setLoading(true);
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: '20' });
      if (filters.ruc) params.set('ruc', filters.ruc);
      if (filters.serie) params.set('serie', filters.serie);
      if (filters.correlative) params.set('correlative', filters.correlative);
      if (filters.customerDocNumber) params.set('customerDocNumber', filters.customerDocNumber);

      const res = await api.get<PaginatedResponse<DocRow>>(`/v1/backoffice/documents?${params}`);
      setDocs(res.data);
      setPagination(res.pagination);
      setSearched(true);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  }, [filters]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    fetchDocs(1);
  };

  const docTypeLabel = (type: string) => {
    const map: Record<string, string> = { '01': 'Factura', '03': 'Boleta', '07': 'NC', '08': 'ND' };
    return map[type] ?? type;
  };

  const statusColor = (status: string) => {
    const map: Record<string, string> = {
      accepted: 'bg-emerald-950 text-emerald-300',
      pending: 'bg-amber-950 text-amber-300',
      rejected: 'bg-red-950 text-red-300',
      voided: 'bg-slate-800 text-slate-400',
    };
    return map[status] ?? 'bg-slate-800 text-slate-400';
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-white">Búsqueda de Documentos</h1>
        <p className="text-sm text-slate-400 mt-1">
          Buscar comprobantes de cualquier tenant para soporte
        </p>
      </div>

      {/* Search Form */}
      <Card className="bg-slate-900 border-slate-800">
        <CardContent className="p-5">
          <form onSubmit={handleSearch} className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
            <div className="space-y-1">
              <Label className="text-slate-400 text-xs">RUC Emisor</Label>
              <Input
                placeholder="20123456789"
                value={filters.ruc}
                onChange={(e) => setFilters((f) => ({ ...f, ruc: e.target.value }))}
                className="bg-slate-800 border-slate-700 text-white placeholder:text-slate-500"
              />
            </div>
            <div className="space-y-1">
              <Label className="text-slate-400 text-xs">Serie</Label>
              <Input
                placeholder="F001"
                value={filters.serie}
                onChange={(e) => setFilters((f) => ({ ...f, serie: e.target.value }))}
                className="bg-slate-800 border-slate-700 text-white placeholder:text-slate-500"
              />
            </div>
            <div className="space-y-1">
              <Label className="text-slate-400 text-xs">Correlativo</Label>
              <Input
                type="number"
                placeholder="1"
                value={filters.correlative}
                onChange={(e) => setFilters((f) => ({ ...f, correlative: e.target.value }))}
                className="bg-slate-800 border-slate-700 text-white placeholder:text-slate-500"
              />
            </div>
            <div className="space-y-1">
              <Label className="text-slate-400 text-xs">DNI/RUC Cliente</Label>
              <Input
                placeholder="10123456"
                value={filters.customerDocNumber}
                onChange={(e) => setFilters((f) => ({ ...f, customerDocNumber: e.target.value }))}
                className="bg-slate-800 border-slate-700 text-white placeholder:text-slate-500"
              />
            </div>
            <div className="flex items-end">
              <Button type="submit" className="w-full bg-indigo-600 hover:bg-indigo-700 text-white" disabled={loading}>
                <Search className="h-4 w-4 mr-2" />
                {loading ? 'Buscando...' : 'Buscar'}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      {/* Results */}
      {!searched ? (
        <div className="text-center py-16">
          <FileSearch className="h-12 w-12 mx-auto text-slate-700 mb-3" />
          <p className="text-slate-500">Ingresá criterios de búsqueda para encontrar documentos</p>
        </div>
      ) : (
        <Card className="bg-slate-900 border-slate-800 overflow-hidden">
          <CardContent className="p-0">
            {docs.length === 0 ? (
              <div className="p-8 text-center text-slate-500">
                <FileSearch className="h-10 w-10 mx-auto mb-2 opacity-50" />
                <p>No se encontraron documentos</p>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-slate-800 bg-slate-800/50">
                      <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Número</th>
                      <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Tipo</th>
                      <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Cliente</th>
                      <th className="px-4 py-3 text-right text-xs font-medium text-slate-400 uppercase">Total</th>
                      <th className="px-4 py-3 text-center text-xs font-medium text-slate-400 uppercase">Estado</th>
                      <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Fecha</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-800">
                    {docs.map((d) => (
                      <tr key={d.id} className="hover:bg-slate-800/50 transition-colors">
                        <td className="px-4 py-3 font-mono text-sm text-indigo-300">{d.fullNumber}</td>
                        <td className="px-4 py-3">
                          <span className="inline-flex items-center rounded-md bg-slate-800 px-2 py-0.5 text-xs font-medium text-slate-300">
                            {docTypeLabel(d.documentType)}
                          </span>
                        </td>
                        <td className="px-4 py-3">
                          <p className="text-slate-200 text-sm">{d.customerName}</p>
                          <p className="text-xs text-slate-500">{d.customerDocNumber}</p>
                        </td>
                        <td className="px-4 py-3 text-right font-mono text-slate-200">
                          S/ {d.total.toLocaleString('es-PE', { minimumFractionDigits: 2 })}
                        </td>
                        <td className="px-4 py-3 text-center">
                          <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${statusColor(d.status)}`}>
                            {d.status}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-slate-500 text-xs">
                          {new Date(d.createdAt).toLocaleString('es-PE')}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {/* Pagination */}
      {searched && pagination.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-slate-500">
            {pagination.totalCount} resultado{pagination.totalCount !== 1 ? 's' : ''} — Página {pagination.page}/{pagination.totalPages}
          </p>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" disabled={pagination.page <= 1} onClick={() => fetchDocs(pagination.page - 1)} className="border-slate-700 text-slate-400 hover:bg-slate-800">
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Button variant="outline" size="sm" disabled={pagination.page >= pagination.totalPages} onClick={() => fetchDocs(pagination.page + 1)} className="border-slate-700 text-slate-400 hover:bg-slate-800">
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
