'use client';

import { useEffect, useState } from 'react';
import { api, type Plan } from '@/lib/api';
import { useAuth } from '@/lib/auth-context';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { toast } from 'sonner';
import { CheckCircle2, Zap, Mail } from 'lucide-react';

// Feature key → label mapping
const FEATURE_LABELS: Record<string, string> = {
  api: 'Acceso a API REST',
  support: 'Soporte técnico',
  ai: 'IA para facturas',
  users: 'Usuarios',
  series: 'Series',
  webhooks: 'Webhooks',
};

interface TenantInfo {
  currentPlanId?: string;
  planName?: string;
}

function formatPrice(price: number): string {
  if (price === 0) return 'Gratis';
  return new Intl.NumberFormat('es-PE', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 0,
  }).format(price);
}

function formatDocs(max: number): string {
  if (max === 0 || max === -1) return 'Ilimitados';
  return new Intl.NumberFormat('es-PE').format(max);
}

function renderFeatureValue(key: string, value: unknown): string {
  if (typeof value === 'boolean') return value ? '✓' : '—';
  if (typeof value === 'number') return new Intl.NumberFormat('es-PE').format(value);
  if (typeof value === 'string') return value;
  return String(value);
}

function PlanSkeleton() {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      {Array.from({ length: 3 }).map((_, i) => (
        <Card key={i} className="animate-pulse">
          <CardHeader>
            <div className="h-5 bg-muted rounded w-24 mb-2" />
            <div className="h-8 bg-muted rounded w-16" />
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {Array.from({ length: 4 }).map((_, j) => (
                <div key={j} className="h-4 bg-muted rounded w-full" />
              ))}
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

export default function PlanPage() {
  const { user } = useAuth();
  const [plans, setPlans] = useState<Plan[]>([]);
  const [tenant, setTenant] = useState<TenantInfo | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      setIsLoading(true);
      try {
        const data = await api.get<Plan[]>('/v1/plans');
        setPlans(data.filter((p) => p.isActive));
      } catch (err) {
        toast.error(err instanceof Error ? err.message : 'Error al cargar planes');
      }

      // Try to get tenant/current plan info — optional endpoint
      try {
        const t = await api.get<TenantInfo>('/v1/tenants/me');
        setTenant(t);
      } catch {
        // Not critical — some tenants might not expose this
      }

      setIsLoading(false);
    };
    load();
  }, []);

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Plan y Facturación</h1>
          <p className="text-muted-foreground">Gestiona tu suscripción y límites</p>
        </div>
        <PlanSkeleton />
      </div>
    );
  }

  // Try to identify current plan: from tenant endpoint or match by plan name
  const currentPlanId = tenant?.currentPlanId;
  // Fallback: first plan if no tenant info
  const currentPlan = currentPlanId
    ? plans.find((p) => p.id === currentPlanId)
    : plans[0] ?? null;

  return (
    <div className="space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Plan y Facturación</h1>
        <p className="text-muted-foreground">Gestiona tu suscripción y límites de uso</p>
      </div>

      {/* Current Plan Summary */}
      {currentPlan && (
        <Card className="border-blue-200 dark:border-blue-800 bg-blue-50/50 dark:bg-blue-950/20">
          <CardHeader>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <Zap className="h-5 w-5 text-blue-600" />
                <div>
                  <CardTitle className="text-base">Plan Actual</CardTitle>
                  <CardDescription>Tu suscripción activa</CardDescription>
                </div>
              </div>
              <Badge className="bg-blue-600 hover:bg-blue-600 text-white">{currentPlan.name}</Badge>
            </div>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <p className="text-muted-foreground">Precio mensual</p>
                <p className="font-semibold text-lg">{formatPrice(currentPlan.priceMonthly)}</p>
              </div>
              <div>
                <p className="text-muted-foreground">Documentos / mes</p>
                <p className="font-semibold text-lg">
                  {formatDocs(currentPlan.maxDocumentsPerMonth)}
                </p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      <Separator />

      {/* Plans Grid */}
      <div>
        <h2 className="text-lg font-semibold mb-4">Planes Disponibles</h2>
        {plans.length === 0 ? (
          <div className="rounded-lg border-2 border-dashed p-12 text-center text-muted-foreground">
            <p>No hay planes disponibles en este momento.</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {plans.map((plan) => {
              const isCurrent = plan.id === currentPlan?.id;
              const features = plan.features as Record<string, unknown>;

              return (
                <Card
                  key={plan.id}
                  className={
                    isCurrent
                      ? 'ring-2 ring-blue-500 border-blue-200 dark:border-blue-800'
                      : ''
                  }
                >
                  <CardHeader>
                    <div className="flex items-center justify-between">
                      <CardTitle className="text-base">{plan.name}</CardTitle>
                      {isCurrent && (
                        <Badge className="bg-blue-600 text-white text-xs">Plan Actual</Badge>
                      )}
                    </div>
                    <div className="mt-2">
                      <span className="text-3xl font-bold">
                        {plan.priceMonthly === 0 ? 'Gratis' : `$${plan.priceMonthly}`}
                      </span>
                      {plan.priceMonthly > 0 && (
                        <span className="text-muted-foreground text-sm"> / mes</span>
                      )}
                    </div>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    {/* Docs limit */}
                    <div className="flex items-center gap-2 text-sm">
                      <CheckCircle2 className="h-4 w-4 text-green-500 shrink-0" />
                      <span>
                        <strong>{formatDocs(plan.maxDocumentsPerMonth)}</strong> documentos/mes
                      </span>
                    </div>

                    {/* Features */}
                    {Object.entries(features).length > 0 && (
                      <ul className="space-y-2">
                        {Object.entries(features).map(([key, val]) => {
                          const label = FEATURE_LABELS[key] ?? key;
                          const displayVal = renderFeatureValue(key, val);
                          const isDisabled = val === false || val === 0;
                          return (
                            <li
                              key={key}
                              className={`flex items-center gap-2 text-sm ${
                                isDisabled ? 'text-muted-foreground' : ''
                              }`}
                            >
                              {isDisabled ? (
                                <span className="h-4 w-4 flex items-center justify-center text-muted-foreground shrink-0 font-bold">
                                  —
                                </span>
                              ) : (
                                <CheckCircle2 className="h-4 w-4 text-green-500 shrink-0" />
                              )}
                              <span>
                                {label}
                                {typeof val !== 'boolean' && (
                                  <span className="text-muted-foreground ml-1">
                                    ({displayVal})
                                  </span>
                                )}
                              </span>
                            </li>
                          );
                        })}
                      </ul>
                    )}

                    <Separator />

                    {/* CTA */}
                    {isCurrent ? (
                      <Button variant="outline" className="w-full" disabled>
                        Plan Actual
                      </Button>
                    ) : (
                      <Button
                        variant="outline"
                        className="w-full"
                        onClick={() => {
                          const subject = encodeURIComponent(`Upgrade a plan ${plan.name}`);
                          const body = encodeURIComponent(
                            `Hola,\n\nQuiero hacer upgrade al plan ${plan.name}.\n\nMi cuenta: ${user?.email}\nTenant: ${user?.tenantId}`
                          );
                          window.open(`mailto:ventas@tukifact.pe?subject=${subject}&body=${body}`, '_blank');
                        }}
                      >
                        <Mail className="mr-2 h-4 w-4" />
                        Contactar para upgrade
                      </Button>
                    )}
                  </CardContent>
                </Card>
              );
            })}
          </div>
        )}
      </div>

      {/* Footer note */}
      <p className="text-xs text-muted-foreground">
        Los precios están en USD. Para cambios de plan o facturación personalizada, contactá al equipo de ventas.
      </p>
    </div>
  );
}
