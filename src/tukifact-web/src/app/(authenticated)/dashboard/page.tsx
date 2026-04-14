'use client';

import { useEffect, useState } from 'react';
import { api, type DashboardResponse } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { FileText, TrendingUp, CheckCircle, XCircle } from 'lucide-react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
} from 'recharts';

const formatCurrency = (amount: number) =>
  new Intl.NumberFormat('es-PE', { style: 'currency', currency: 'PEN' }).format(amount);

const STATUS_COLORS: Record<string, string> = {
  accepted: '#22c55e',
  rejected: '#ef4444',
  voided: '#6b7280',
  draft: '#f59e0b',
  sent: '#3b82f6',
  signed: '#8b5cf6',
};

const MONTHS = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];

export default function DashboardPage() {
  const [data, setData] = useState<DashboardResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    api
      .get<DashboardResponse>('/v1/dashboard')
      .then(setData)
      .catch(console.error)
      .finally(() => setIsLoading(false));
  }, []);

  if (isLoading) return <DashboardSkeleton />;
  if (!data) return <p className="text-muted-foreground">Error cargando dashboard</p>;

  const monthlyData = data.monthlySales.map((m) => ({
    name: MONTHS[m.month - 1],
    total: m.total,
    count: m.count,
  }));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Dashboard</h1>
        <p className="text-muted-foreground">Resumen de tu facturación electrónica</p>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <KPICard
          title="Hoy"
          icon={FileText}
          value={formatCurrency(data.today.totalAmount)}
          subtitle={`${data.today.totalDocuments} documentos`}
          badge={data.today.accepted > 0 ? `${data.today.accepted} aceptados` : undefined}
        />
        <KPICard
          title="Este Mes"
          icon={TrendingUp}
          value={formatCurrency(data.thisMonth.totalAmount)}
          subtitle={`${data.thisMonth.totalDocuments} documentos`}
          badge={`IGV: ${formatCurrency(data.thisMonth.totalIgv)}`}
        />
        <KPICard
          title="Aceptados"
          icon={CheckCircle}
          value={String(data.thisMonth.accepted)}
          subtitle="Este mes"
          variant="success"
        />
        <KPICard
          title="Rechazados"
          icon={XCircle}
          value={String(data.thisMonth.rejected)}
          subtitle="Este mes"
          variant={data.thisMonth.rejected > 0 ? 'danger' : 'default'}
        />
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        {/* Monthly Sales */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="text-base">Ventas Mensuales</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-[300px]">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={monthlyData}>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                  <XAxis dataKey="name" className="text-xs" />
                  <YAxis
                    className="text-xs"
                    tickFormatter={(v: number) => `S/${(v / 1000).toFixed(0)}k`}
                  />
                  <Tooltip formatter={(v) => typeof v === 'number' ? formatCurrency(v) : v} />
                  <Bar dataKey="total" fill="#3b82f6" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        {/* By Status */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Por Estado</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-[200px]">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={data.byStatus}
                    dataKey="count"
                    nameKey="status"
                    cx="50%"
                    cy="50%"
                    outerRadius={80}
                    label={({ name, value }) => `${name ?? ''}: ${value ?? ''}`}
                  >
                    {data.byStatus.map((entry) => (
                      <Cell
                        key={entry.status}
                        fill={STATUS_COLORS[entry.status] || '#94a3b8'}
                      />
                    ))}
                  </Pie>
                  <Tooltip />
                </PieChart>
              </ResponsiveContainer>
            </div>
            <div className="mt-4 space-y-2">
              {data.byType.map((t) => (
                <div key={t.documentType} className="flex justify-between items-center text-sm">
                  <span className="text-muted-foreground">{t.name}</span>
                  <span className="font-medium">
                    {t.count} — {formatCurrency(t.total)}
                  </span>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

function KPICard({
  title,
  icon: Icon,
  value,
  subtitle,
  badge,
  variant = 'default',
}: {
  title: string;
  icon: React.ElementType;
  value: string;
  subtitle: string;
  badge?: string;
  variant?: 'default' | 'success' | 'danger';
}) {
  const colors = {
    default: 'text-blue-600 bg-blue-50',
    success: 'text-green-600 bg-green-50',
    danger: 'text-red-600 bg-red-50',
  };
  return (
    <Card>
      <CardContent className="pt-6">
        <div className="flex items-center justify-between">
          <div className={`p-2 rounded-lg ${colors[variant]}`}>
            <Icon className="h-5 w-5" />
          </div>
          {badge && (
            <Badge variant="secondary" className="text-xs">
              {badge}
            </Badge>
          )}
        </div>
        <div className="mt-3">
          <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider">{title}</p>
          <p className="text-2xl font-bold">{value}</p>
          <p className="text-sm text-muted-foreground">{subtitle}</p>
        </div>
      </CardContent>
    </Card>
  );
}

function DashboardSkeleton() {
  return (
    <div className="space-y-6">
      <div className="h-8 w-48 bg-muted animate-pulse rounded" />
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {[1, 2, 3, 4].map((i) => (
          <div key={i} className="h-32 bg-muted animate-pulse rounded-lg" />
        ))}
      </div>
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <div className="lg:col-span-2 h-80 bg-muted animate-pulse rounded-lg" />
        <div className="h-80 bg-muted animate-pulse rounded-lg" />
      </div>
    </div>
  );
}
