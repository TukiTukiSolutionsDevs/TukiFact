'use client';

import { useEffect, useState, useCallback } from 'react';
import Link from 'next/link';
import { api, type PaginatedResponse } from '@/lib/api';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Search, Building2, ChevronLeft, ChevronRight, Filter } from 'lucide-react';

interface TenantRow {
  id: string;
  ruc: string;
  razonSocial: string;
  nombreComercial: string | null;
  isActive: boolean;
  environment: string;
  createdAt: string;
  plan: string;
  usersCount: number;
  documentsCount: number;
}

export default function TenantsPage() {
  const [tenants, setTenants] = useState<TenantRow[]>([]);
  const [pagination, setPagination] = useState({ page: 1, pageSize: 20, totalCount: 0, totalPages: 0 });
  const [search, setSearch] = useState('');
  const [filterActive, setFilterActive] = useState<boolean | undefined>(undefined);
  const [loading, setLoading] = useState(true);

  const fetchTenants = useCallback(async (page: number) => {
    setLoading(true);
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: '20' });
      if (search) params.set('search', search);
      if (filterActive !== undefined) params.set('isActive', String(filterActive));

      const res = await api.get<PaginatedResponse<TenantRow>>(`/v1/backoffice/tenants?${params}`);
      setTenants(res.data);
      setPagination(res.pagination);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  }, [search, filterActive]);

  useEffect(() => {
    fetchTenants(1);
  }, [fetchTenants]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    fetchTenants(1);
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-white">Tenants</h1>
        <p className="text-sm text-slate-400 mt-1">
          {pagination.totalCount} empresa{pagination.totalCount !== 1 ? 's' : ''} registrada{pagination.totalCount !== 1 ? 's' : ''}
        </p>
      </div>

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-3">
        <form onSubmit={handleSearch} className="flex-1 flex gap-2">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-500" />
            <Input
              placeholder="Buscar por RUC o razón social..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-9 bg-slate-900 border-slate-700 text-white placeholder:text-slate-500"
            />
          </div>
          <Button type="submit" variant="outline" className="border-slate-700 text-slate-300 hover:bg-slate-800">
            <Search className="h-4 w-4" />
          </Button>
        </form>

        <div className="flex gap-2">
          <Button
            variant={filterActive === undefined ? 'default' : 'outline'}
            size="sm"
            onClick={() => setFilterActive(undefined)}
            className={filterActive === undefined ? 'bg-indigo-600 text-white' : 'border-slate-700 text-slate-400 hover:bg-slate-800'}
          >
            <Filter className="h-3 w-3 mr-1" /> Todos
          </Button>
          <Button
            variant={filterActive === true ? 'default' : 'outline'}
            size="sm"
            onClick={() => setFilterActive(true)}
            className={filterActive === true ? 'bg-emerald-600 text-white' : 'border-slate-700 text-slate-400 hover:bg-slate-800'}
          >
            Activos
          </Button>
          <Button
            variant={filterActive === false ? 'default' : 'outline'}
            size="sm"
            onClick={() => setFilterActive(false)}
            className={filterActive === false ? 'bg-red-600 text-white' : 'border-slate-700 text-slate-400 hover:bg-slate-800'}
          >
            Suspendidos
          </Button>
        </div>
      </div>

      {/* Table */}
      <Card className="bg-slate-900 border-slate-800 overflow-hidden">
        <CardContent className="p-0">
          {loading ? (
            <div className="p-8 text-center">
              <div className="h-6 w-6 animate-spin rounded-full border-2 border-indigo-500 border-t-transparent mx-auto" />
            </div>
          ) : tenants.length === 0 ? (
            <div className="p-8 text-center text-slate-500">
              <Building2 className="h-10 w-10 mx-auto mb-2 opacity-50" />
              <p>No se encontraron tenants</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-slate-800 bg-slate-800/50">
                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Empresa</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">RUC</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Plan</th>
                    <th className="px-4 py-3 text-center text-xs font-medium text-slate-400 uppercase">Usuarios</th>
                    <th className="px-4 py-3 text-center text-xs font-medium text-slate-400 uppercase">Docs</th>
                    <th className="px-4 py-3 text-center text-xs font-medium text-slate-400 uppercase">Estado</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Registro</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-800">
                  {tenants.map((t) => (
                    <tr key={t.id} className="hover:bg-slate-800/50 transition-colors">
                      <td className="px-4 py-3">
                        <Link href={`/backoffice/tenants/${t.id}`} className="text-indigo-300 hover:text-indigo-200 font-medium">
                          {t.razonSocial}
                        </Link>
                        {t.nombreComercial && (
                          <p className="text-xs text-slate-500">{t.nombreComercial}</p>
                        )}
                      </td>
                      <td className="px-4 py-3 text-slate-300 font-mono text-xs">{t.ruc}</td>
                      <td className="px-4 py-3">
                        <span className="inline-flex items-center rounded-md bg-slate-800 px-2 py-1 text-xs font-medium text-slate-300">
                          {t.plan}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-center text-slate-300">{t.usersCount}</td>
                      <td className="px-4 py-3 text-center text-slate-300">{t.documentsCount}</td>
                      <td className="px-4 py-3 text-center">
                        <span
                          className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
                            t.isActive
                              ? 'bg-emerald-950 text-emerald-300'
                              : 'bg-red-950 text-red-300'
                          }`}
                        >
                          {t.isActive ? 'Activo' : 'Suspendido'}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-slate-500 text-xs">
                        {new Date(t.createdAt).toLocaleDateString('es-PE')}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Pagination */}
      {pagination.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-slate-500">
            Página {pagination.page} de {pagination.totalPages}
          </p>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={pagination.page <= 1}
              onClick={() => fetchTenants(pagination.page - 1)}
              className="border-slate-700 text-slate-400 hover:bg-slate-800"
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={pagination.page >= pagination.totalPages}
              onClick={() => fetchTenants(pagination.page + 1)}
              className="border-slate-700 text-slate-400 hover:bg-slate-800"
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
