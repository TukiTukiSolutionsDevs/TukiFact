'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Building2, Users, FileText, TrendingUp, Activity, AlertTriangle } from 'lucide-react';

interface DashboardData {
  totalTenants: number;
  activeTenants: number;
  suspendedTenants: number;
  totalUsers: number;
  totalDocuments: number;
  todayDocuments: number;
  monthDocuments: number;
  recentTenants: {
    id: string;
    ruc: string;
    razonSocial: string;
    isActive: boolean;
    createdAt: string;
  }[];
  tenantsByPlan: { plan: string; count: number }[];
}

export default function BackofficeDashboardPage() {
  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api
      .get<DashboardData>('/v1/backoffice/dashboard')
      .then(setData)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold text-white">Dashboard</h1>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Card key={i} className="bg-slate-900 border-slate-800">
              <CardContent className="p-6">
                <div className="h-16 animate-pulse bg-slate-800 rounded" />
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  if (!data) return <p className="text-red-400">Error cargando dashboard</p>;

  const stats = [
    {
      label: 'Tenants Totales',
      value: data.totalTenants,
      icon: Building2,
      color: 'text-blue-400',
      bg: 'bg-blue-950',
    },
    {
      label: 'Tenants Activos',
      value: data.activeTenants,
      icon: Activity,
      color: 'text-emerald-400',
      bg: 'bg-emerald-950',
    },
    {
      label: 'Suspendidos',
      value: data.suspendedTenants,
      icon: AlertTriangle,
      color: 'text-amber-400',
      bg: 'bg-amber-950',
    },
    {
      label: 'Usuarios Totales',
      value: data.totalUsers,
      icon: Users,
      color: 'text-purple-400',
      bg: 'bg-purple-950',
    },
    {
      label: 'Documentos Totales',
      value: data.totalDocuments,
      icon: FileText,
      color: 'text-indigo-400',
      bg: 'bg-indigo-950',
    },
    {
      label: 'Docs Hoy',
      value: data.todayDocuments,
      icon: TrendingUp,
      color: 'text-cyan-400',
      bg: 'bg-cyan-950',
    },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-white">Dashboard</h1>
        <p className="text-sm text-slate-400 mt-1">Vista global de la plataforma TukiFact</p>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {stats.map(({ label, value, icon: Icon, color, bg }) => (
          <Card key={label} className="bg-slate-900 border-slate-800">
            <CardContent className="p-5 flex items-center gap-4">
              <div className={`flex h-12 w-12 items-center justify-center rounded-lg ${bg}`}>
                <Icon className={`h-6 w-6 ${color}`} />
              </div>
              <div>
                <p className="text-sm text-slate-400">{label}</p>
                <p className="text-2xl font-bold text-white">{value.toLocaleString()}</p>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Recent Tenants */}
        <Card className="bg-slate-900 border-slate-800">
          <CardHeader>
            <CardTitle className="text-white text-lg">Tenants Recientes</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {data.recentTenants.map((t) => (
                <div
                  key={t.id}
                  className="flex items-center justify-between rounded-lg bg-slate-800/50 px-4 py-3"
                >
                  <div>
                    <p className="text-sm font-medium text-slate-200">{t.razonSocial}</p>
                    <p className="text-xs text-slate-500">RUC: {t.ruc}</p>
                  </div>
                  <div className="flex items-center gap-2">
                    <span
                      className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
                        t.isActive
                          ? 'bg-emerald-950 text-emerald-300'
                          : 'bg-red-950 text-red-300'
                      }`}
                    >
                      {t.isActive ? 'Activo' : 'Suspendido'}
                    </span>
                  </div>
                </div>
              ))}
              {data.recentTenants.length === 0 && (
                <p className="text-sm text-slate-500 text-center py-4">No hay tenants registrados</p>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Tenants by Plan */}
        <Card className="bg-slate-900 border-slate-800">
          <CardHeader>
            <CardTitle className="text-white text-lg">Distribución por Plan</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {data.tenantsByPlan.map(({ plan, count }) => (
                <div
                  key={plan}
                  className="flex items-center justify-between rounded-lg bg-slate-800/50 px-4 py-3"
                >
                  <span className="text-sm font-medium text-slate-200">{plan}</span>
                  <span className="text-sm font-bold text-indigo-300">{count}</span>
                </div>
              ))}
              {data.tenantsByPlan.length === 0 && (
                <p className="text-sm text-slate-500 text-center py-4">Sin datos de planes</p>
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Month docs highlight */}
      <Card className="bg-gradient-to-r from-indigo-950/50 to-slate-900 border-slate-800">
        <CardContent className="p-5 flex items-center gap-4">
          <FileText className="h-8 w-8 text-indigo-400" />
          <div>
            <p className="text-sm text-slate-400">Documentos este mes</p>
            <p className="text-3xl font-bold text-white">{data.monthDocuments.toLocaleString()}</p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
