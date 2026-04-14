'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { api } from '@/lib/api';
import { useBackofficeAuth } from '@/lib/backoffice-auth-context';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  ArrowLeft,
  Building2,
  Users,
  FileText,
  Shield,
  Ban,
  CheckCircle,
  Calendar,
  CreditCard,
  Loader2,
} from 'lucide-react';
import { toast } from 'sonner';
import type { Plan } from '@/lib/api';

interface TenantDetail {
  id: string;
  ruc: string;
  razonSocial: string;
  nombreComercial: string | null;
  direccion: string | null;
  isActive: boolean;
  environment: string;
  createdAt: string;
  updatedAt: string;
  hasCertificate: boolean;
  certificateExpiresAt: string | null;
  plan: { id: string; name: string; priceMonthly: number; maxDocumentsPerMonth: number } | null;
  users: {
    id: string;
    email: string;
    fullName: string | null;
    role: string;
    isActive: boolean;
    lastLoginAt: string | null;
  }[];
  stats: { totalDocuments: number; monthDocuments: number };
}

export default function TenantDetailPage() {
  const params = useParams();
  const router = useRouter();
  const { user: boUser } = useBackofficeAuth();
  const [tenant, setTenant] = useState<TenantDetail | null>(null);
  const [plans, setPlans] = useState<Plan[]>([]);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState('');

  const tenantId = params.id as string;
  const isSuperadmin = boUser?.role === 'superadmin';

  useEffect(() => {
    Promise.all([
      api.get<TenantDetail>(`/v1/backoffice/tenants/${tenantId}`),
      api.get<Plan[]>('/v1/plans'),
    ])
      .then(([t, p]) => {
        setTenant(t);
        setPlans(p);
      })
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [tenantId]);

  const handleSuspend = async () => {
    setActionLoading('suspend');
    try {
      await api.put(`/v1/backoffice/tenants/${tenantId}/suspend`, {});
      setTenant((prev) => (prev ? { ...prev, isActive: false } : prev));
      toast.success('Tenant suspendido');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    } finally {
      setActionLoading('');
    }
  };

  const handleActivate = async () => {
    setActionLoading('activate');
    try {
      await api.put(`/v1/backoffice/tenants/${tenantId}/activate`, {});
      setTenant((prev) => (prev ? { ...prev, isActive: true } : prev));
      toast.success('Tenant activado');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    } finally {
      setActionLoading('');
    }
  };

  const handleChangePlan = async (planId: string | null) => {
    if (!planId) return;
    setActionLoading('plan');
    try {
      await api.put(`/v1/backoffice/tenants/${tenantId}/plan`, { planId });
      const updated = await api.get<TenantDetail>(`/v1/backoffice/tenants/${tenantId}`);
      setTenant(updated);
      toast.success('Plan actualizado');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    } finally {
      setActionLoading('');
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-indigo-500 border-t-transparent" />
      </div>
    );
  }

  if (!tenant) {
    return (
      <div className="text-center py-20">
        <p className="text-red-400">Tenant no encontrado</p>
        <Button variant="outline" onClick={() => router.back()} className="mt-4 border-slate-700 text-slate-300">
          Volver
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div className="flex items-start gap-4">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => router.push('/backoffice/tenants')}
            className="text-slate-400 hover:text-white hover:bg-slate-800"
          >
            <ArrowLeft className="h-4 w-4 mr-1" /> Volver
          </Button>
          <div>
            <div className="flex items-center gap-3">
              <h1 className="text-2xl font-bold text-white">{tenant.razonSocial}</h1>
              <span
                className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
                  tenant.isActive ? 'bg-emerald-950 text-emerald-300' : 'bg-red-950 text-red-300'
                }`}
              >
                {tenant.isActive ? 'Activo' : 'Suspendido'}
              </span>
            </div>
            {tenant.nombreComercial && (
              <p className="text-sm text-slate-400 mt-1">{tenant.nombreComercial}</p>
            )}
          </div>
        </div>

        {/* Actions */}
        {isSuperadmin && (
          <div className="flex gap-2">
            {tenant.isActive ? (
              <Button
                variant="outline"
                size="sm"
                onClick={handleSuspend}
                disabled={!!actionLoading}
                className="border-red-800 text-red-400 hover:bg-red-950 hover:text-red-300"
              >
                {actionLoading === 'suspend' ? (
                  <Loader2 className="h-4 w-4 animate-spin mr-1" />
                ) : (
                  <Ban className="h-4 w-4 mr-1" />
                )}
                Suspender
              </Button>
            ) : (
              <Button
                variant="outline"
                size="sm"
                onClick={handleActivate}
                disabled={!!actionLoading}
                className="border-emerald-800 text-emerald-400 hover:bg-emerald-950 hover:text-emerald-300"
              >
                {actionLoading === 'activate' ? (
                  <Loader2 className="h-4 w-4 animate-spin mr-1" />
                ) : (
                  <CheckCircle className="h-4 w-4 mr-1" />
                )}
                Activar
              </Button>
            )}
          </div>
        )}
      </div>

      {/* Info Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card className="bg-slate-900 border-slate-800">
          <CardContent className="p-4 flex items-center gap-3">
            <Building2 className="h-5 w-5 text-blue-400" />
            <div>
              <p className="text-xs text-slate-500">RUC</p>
              <p className="text-sm font-mono text-white">{tenant.ruc}</p>
            </div>
          </CardContent>
        </Card>
        <Card className="bg-slate-900 border-slate-800">
          <CardContent className="p-4 flex items-center gap-3">
            <Users className="h-5 w-5 text-purple-400" />
            <div>
              <p className="text-xs text-slate-500">Usuarios</p>
              <p className="text-sm font-bold text-white">{tenant.users.length}</p>
            </div>
          </CardContent>
        </Card>
        <Card className="bg-slate-900 border-slate-800">
          <CardContent className="p-4 flex items-center gap-3">
            <FileText className="h-5 w-5 text-indigo-400" />
            <div>
              <p className="text-xs text-slate-500">Docs Total / Mes</p>
              <p className="text-sm font-bold text-white">
                {tenant.stats.totalDocuments} / {tenant.stats.monthDocuments}
              </p>
            </div>
          </CardContent>
        </Card>
        <Card className="bg-slate-900 border-slate-800">
          <CardContent className="p-4 flex items-center gap-3">
            <Shield className="h-5 w-5 text-amber-400" />
            <div>
              <p className="text-xs text-slate-500">Certificado</p>
              <p className="text-sm text-white">
                {tenant.hasCertificate ? 'Instalado' : 'Sin certificado'}
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Plan */}
        <Card className="bg-slate-900 border-slate-800">
          <CardHeader>
            <CardTitle className="text-white text-lg flex items-center gap-2">
              <CreditCard className="h-5 w-5 text-indigo-400" /> Plan
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="rounded-lg bg-slate-800/50 p-4">
              <p className="text-lg font-bold text-white">{tenant.plan?.name ?? 'Sin plan'}</p>
              {tenant.plan && (
                <div className="mt-2 flex gap-4 text-sm text-slate-400">
                  <span>S/ {tenant.plan.priceMonthly}/mes</span>
                  <span>{tenant.plan.maxDocumentsPerMonth} docs/mes</span>
                </div>
              )}
            </div>

            {isSuperadmin && (
              <div className="flex items-center gap-3">
                <Select onValueChange={handleChangePlan} disabled={!!actionLoading}>
                  <SelectTrigger className="bg-slate-800 border-slate-700 text-white">
                    <SelectValue placeholder="Cambiar plan..." />
                  </SelectTrigger>
                  <SelectContent className="bg-slate-800 border-slate-700">
                    {plans.map((p) => (
                      <SelectItem key={p.id} value={p.id} className="text-slate-200 focus:bg-slate-700">
                        {p.name} — S/ {p.priceMonthly}/mes
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {actionLoading === 'plan' && (
                  <Loader2 className="h-4 w-4 animate-spin text-indigo-400" />
                )}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Dates */}
        <Card className="bg-slate-900 border-slate-800">
          <CardHeader>
            <CardTitle className="text-white text-lg flex items-center gap-2">
              <Calendar className="h-5 w-5 text-cyan-400" /> Información
            </CardTitle>
          </CardHeader>
          <CardContent>
            <dl className="space-y-3">
              {tenant.direccion && (
                <div>
                  <dt className="text-xs text-slate-500">Dirección</dt>
                  <dd className="text-sm text-slate-200">{tenant.direccion}</dd>
                </div>
              )}
              <div>
                <dt className="text-xs text-slate-500">Entorno</dt>
                <dd className="text-sm text-slate-200 capitalize">{tenant.environment}</dd>
              </div>
              <div>
                <dt className="text-xs text-slate-500">Registrado</dt>
                <dd className="text-sm text-slate-200">
                  {new Date(tenant.createdAt).toLocaleString('es-PE')}
                </dd>
              </div>
              <div>
                <dt className="text-xs text-slate-500">Última actualización</dt>
                <dd className="text-sm text-slate-200">
                  {new Date(tenant.updatedAt).toLocaleString('es-PE')}
                </dd>
              </div>
              {tenant.certificateExpiresAt && (
                <div>
                  <dt className="text-xs text-slate-500">Certificado expira</dt>
                  <dd className="text-sm text-amber-300">
                    {new Date(tenant.certificateExpiresAt).toLocaleDateString('es-PE')}
                  </dd>
                </div>
              )}
            </dl>
          </CardContent>
        </Card>
      </div>

      {/* Users Table */}
      <Card className="bg-slate-900 border-slate-800">
        <CardHeader>
          <CardTitle className="text-white text-lg">Usuarios del Tenant</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-slate-800 bg-slate-800/50">
                  <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Email</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Nombre</th>
                  <th className="px-4 py-3 text-center text-xs font-medium text-slate-400 uppercase">Rol</th>
                  <th className="px-4 py-3 text-center text-xs font-medium text-slate-400 uppercase">Estado</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Último login</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800">
                {tenant.users.map((u) => (
                  <tr key={u.id} className="hover:bg-slate-800/50">
                    <td className="px-4 py-3 text-slate-200">{u.email}</td>
                    <td className="px-4 py-3 text-slate-300">{u.fullName ?? '—'}</td>
                    <td className="px-4 py-3 text-center">
                      <span className="inline-flex items-center rounded-md bg-slate-800 px-2 py-0.5 text-xs font-medium text-slate-300 capitalize">
                        {u.role}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-center">
                      <span
                        className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
                          u.isActive ? 'bg-emerald-950 text-emerald-300' : 'bg-red-950 text-red-300'
                        }`}
                      >
                        {u.isActive ? 'Activo' : 'Inactivo'}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-slate-500 text-xs">
                      {u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleString('es-PE') : 'Nunca'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
