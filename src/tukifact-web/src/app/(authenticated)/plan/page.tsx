'use client';

import { useEffect, useState, useCallback } from 'react';
import Script from 'next/script';
import { api, type Plan } from '@/lib/api';
import { useAuth } from '@/lib/auth-context';
import { Section } from '@/components/ui/section';
import { Button } from '@/components/ui/button';
import { StatusBadge } from '@/components/ui/status-badge';
import {
  CheckCircle2,
  Crown,
  Mail,
  Loader2,
  X,
  Sparkles,
} from 'lucide-react';
import { toast } from 'sonner';
import { cn } from '@/lib/utils';

const CULQI_PUBLIC_KEY = process.env.NEXT_PUBLIC_CULQI_PUBLIC_KEY;

interface Subscription {
  id: string;
  planId: string;
  planName: string;
  status: string;
  monthlyAmount: number;
  documentsLimit: number;
  documentsUsedThisMonth: number;
  nextBillingDate: string;
  lastChargedAt: string | null;
  isCulqiManaged: boolean;
}

// Loaded by checkout-js script.
declare global {
  interface Window {
    Culqi?: {
      publicKey: string;
      settings: (config: Record<string, unknown>) => void;
      options: (opts: Record<string, unknown>) => void;
      open: () => void;
      token?: { id: string };
      error?: { user_message?: string; merchant_message?: string };
    };
    culqi?: () => void;
  }
}

const FEATURE_LABELS: Record<string, string> = {
  api: 'API REST',
  support: 'Soporte técnico',
  ai: 'Asistente IA',
  users: 'Usuarios incluidos',
  series: 'Series',
  webhooks: 'Webhooks',
  guides: 'Guías de remisión',
  multi_branch: 'Multi-sucursal',
};

interface TenantInfo {
  currentPlanId?: string;
  planName?: string;
}

const formatPrice = (price: number) => {
  if (price === 0) return 'Gratis';
  return new Intl.NumberFormat('es-PE', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 0,
  }).format(price);
};

const formatDocs = (max: number) => {
  if (max === 0 || max === -1) return 'Ilimitados';
  return new Intl.NumberFormat('es-PE').format(max);
};

const renderFeatureValue = (value: unknown) => {
  if (typeof value === 'boolean') return value ? '✓' : '—';
  if (typeof value === 'number') return new Intl.NumberFormat('es-PE').format(value);
  if (typeof value === 'string') return value;
  return String(value);
};

export default function PlanPage() {
  const { user } = useAuth();
  const [plans, setPlans] = useState<Plan[]>([]);
  const [tenant, setTenant] = useState<TenantInfo | null>(null);
  const [subscription, setSubscription] = useState<Subscription | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [pendingPlanId, setPendingPlanId] = useState<string | null>(null);

  const reloadSubscription = useCallback(async () => {
    try {
      const sub = await api.get<Subscription | null>('/v1/billing/subscription');
      setSubscription(sub);
    } catch {
      setSubscription(null);
    }
  }, []);

  useEffect(() => {
    const load = async () => {
      setIsLoading(true);
      try {
        const data = await api.get<Plan[]>('/v1/plans');
        setPlans(data.filter((p) => p.isActive));
      } catch (err) {
        toast.error(err instanceof Error ? err.message : 'Error al cargar planes');
      }
      try {
        const t = await api.get<TenantInfo>('/v1/tenants/me');
        setTenant(t);
      } catch {
        // optional endpoint
      }
      await reloadSubscription();
      setIsLoading(false);
    };
    load();
  }, [reloadSubscription]);

  const currentPlan = tenant?.currentPlanId
    ? plans.find((p) => p.id === tenant.currentPlanId)
    : (plans[0] ?? null);

  const handleSubscribe = (plan: Plan) => {
    if (!CULQI_PUBLIC_KEY) {
      toast.error('La pasarela de pago no está configurada. Contactá a ventas@tukifact.com.pe.');
      return;
    }
    if (!window.Culqi) {
      toast.error('El widget de pago todavía no cargó. Probá de nuevo en unos segundos.');
      return;
    }
    if (!user?.email) {
      toast.error('Iniciá sesión para suscribirte.');
      return;
    }

    setPendingPlanId(plan.id);

    window.Culqi.publicKey = CULQI_PUBLIC_KEY;
    window.Culqi.settings({
      title: 'TukiFact',
      currency: 'PEN',
      amount: Math.round(plan.priceMonthly * 100),
      order: '',
      description: `Suscripción mensual ${plan.name}`,
    });
    window.Culqi.options({
      lang: 'es',
      installments: false,
      paymentMethods: { tarjeta: true, yape: false, bancaMovil: false, agente: false, billetera: false, cuotealo: false },
    });

    window.culqi = async () => {
      try {
        const token = window.Culqi?.token?.id;
        if (!token) {
          const err = window.Culqi?.error;
          toast.error(err?.user_message ?? err?.merchant_message ?? 'No se pudo tokenizar la tarjeta.');
          return;
        }
        const fullName = (user.fullName ?? user.email).split(' ');
        const firstName = fullName[0] ?? user.email;
        const lastName = fullName.slice(1).join(' ') || 'TukiFact';
        await api.post('/v1/billing/subscribe', {
          token,
          planId: plan.id,
          firstName,
          lastName,
          email: user.email,
          phoneNumber: null,
          countryCode: 'PE',
        });
        toast.success(`Suscripción al plan ${plan.name} activa.`);
        await reloadSubscription();
        const t = await api.get<TenantInfo>('/v1/tenants/me').catch(() => null);
        if (t) setTenant(t);
      } catch (err) {
        toast.error(err instanceof Error ? err.message : 'No se pudo registrar la suscripción.');
      } finally {
        setPendingPlanId(null);
      }
    };

    window.Culqi.open();
  };

  const handleCancel = async () => {
    if (!subscription) return;
    if (!confirm(`¿Cancelar la suscripción al plan ${subscription.planName}? Vas a volver al plan Free.`)) return;
    try {
      await api.post('/v1/billing/cancel', { reason: 'user_initiated' });
      toast.success('Suscripción cancelada.');
      await reloadSubscription();
      const t = await api.get<TenantInfo>('/v1/tenants/me').catch(() => null);
      if (t) setTenant(t);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'No se pudo cancelar la suscripción.');
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center gap-3 p-6 text-[var(--muted-foreground)]">
        <Loader2 className="h-4 w-4 animate-spin" />
        <span className="t-body-sm">Cargando planes…</span>
      </div>
    );
  }

  return (
    <div>
      {CULQI_PUBLIC_KEY && (
        <Script src="https://checkout.culqi.com/js/v4" strategy="afterInteractive" />
      )}
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0">Plan y facturación</h1>
          <p className="t-body mt-1.5 mb-0" style={{ color: 'var(--muted-foreground)' }}>
            Gestiona tu suscripción a TukiFact y los límites de uso.
          </p>
        </div>
        {subscription?.isCulqiManaged && subscription.status !== 'cancelled' && (
          <Button variant="outline" onClick={handleCancel}>
            <X className="h-4 w-4 mr-2" /> Cancelar suscripción
          </Button>
        )}
      </div>

      {subscription && subscription.status === 'past_due' && (
        <Section className="mb-[var(--gap-cards)] bg-amber-50 dark:bg-amber-950/30">
          <p className="t-body m-0">
            <strong>Tu último cobro falló.</strong> Culqi reintentará automáticamente. Si querés
            actualizar la tarjeta, cancelá la suscripción actual y volvé a suscribirte.
          </p>
        </Section>
      )}

      {/* Current plan summary */}
      {currentPlan && (
        <Section className="mb-[var(--gap-cards)]">
          <div className="flex items-center justify-between gap-4 flex-wrap">
            <div className="flex items-center gap-3.5">
              <span
                className="h-11 w-11 rounded-[var(--radius-lg)] flex items-center justify-center"
                style={{
                  background: 'color-mix(in oklch, var(--accent) 18%, transparent)',
                  color: 'var(--brand-ink)',
                }}
              >
                <Crown className="h-5 w-5" />
              </span>
              <div>
                <div className="flex items-center gap-2">
                  <span className="t-h3 m-0">Plan {currentPlan.name}</span>
                  <StatusBadge status="active" />
                </div>
                <p
                  className="t-body-sm m-0 mt-0.5"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  {formatPrice(currentPlan.priceMonthly)} / mes ·{' '}
                  {formatDocs(currentPlan.maxDocumentsPerMonth)} documentos/mes
                </p>
              </div>
            </div>
          </div>
        </Section>
      )}

      {/* Plans grid */}
      <h2 className="t-h2 m-0 mb-3">Planes disponibles</h2>

      {plans.length === 0 ? (
        <div
          className="rounded-[var(--radius-lg)] border-2 border-dashed p-12 text-center"
          style={{ borderColor: 'var(--border)', color: 'var(--muted-foreground)' }}
        >
          No hay planes disponibles en este momento.
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-[var(--gap-cards)]">
          {plans.map((plan, idx) => {
            const isCurrent = plan.id === currentPlan?.id;
            const isFeatured = idx === 1 && plans.length >= 3;
            const features = plan.features as Record<string, unknown>;

            return (
              <div
                key={plan.id}
                className={cn(
                  'relative rounded-[var(--radius-lg)] border bg-card p-6 flex flex-col gap-4'
                )}
                style={{
                  boxShadow: isFeatured ? 'var(--shadow-md)' : 'var(--shadow-xs)',
                  borderColor: isFeatured ? 'var(--accent)' : 'var(--border)',
                  borderWidth: isFeatured ? '1.5px' : '1px',
                }}
              >
                {isFeatured && (
                  <span
                    className="absolute t-caption font-bold px-2.5 py-1 rounded-full uppercase tracking-wider"
                    style={{
                      top: -11,
                      left: 24,
                      background: 'var(--accent)',
                      color: 'var(--brand-ink)',
                      fontSize: 11,
                    }}
                  >
                    Recomendado
                  </span>
                )}

                <div>
                  <div className="flex items-center justify-between gap-2 mb-1">
                    <span className="t-h3 m-0">{plan.name}</span>
                    {isCurrent && <StatusBadge status="active" label="Plan actual" />}
                  </div>
                  <div className="flex items-baseline gap-1.5 mt-1">
                    <span className="t-body-sm font-semibold" style={{ color: 'var(--muted-foreground)' }}>
                      {plan.priceMonthly === 0 ? '' : '$'}
                    </span>
                    <span className="t-num-md mono">
                      {plan.priceMonthly === 0 ? 'Gratis' : plan.priceMonthly}
                    </span>
                    {plan.priceMonthly > 0 && (
                      <span className="t-body-sm" style={{ color: 'var(--muted-foreground)' }}>
                        / mes
                      </span>
                    )}
                  </div>
                </div>

                {isCurrent ? (
                  <Button variant="outline" disabled>
                    Plan actual
                  </Button>
                ) : plan.priceMonthly === 0 ? (
                  <Button variant="outline" disabled>
                    <Mail className="h-4 w-4 mr-2" /> Solo por cancelación
                  </Button>
                ) : (
                  <Button
                    variant={isFeatured ? 'default' : 'outline'}
                    onClick={() => handleSubscribe(plan)}
                    disabled={pendingPlanId === plan.id}
                    style={
                      isFeatured
                        ? {
                            background: 'var(--accent)',
                            color: 'var(--accent-foreground)',
                            fontWeight: 600,
                          }
                        : undefined
                    }
                  >
                    {pendingPlanId === plan.id ? (
                      <>
                        <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Procesando…
                      </>
                    ) : isFeatured ? (
                      <>
                        <Sparkles className="h-4 w-4 mr-2" /> Suscribirme al plan {plan.name}
                      </>
                    ) : (
                      <>Suscribirme</>
                    )}
                  </Button>
                )}

                <div className="flex flex-col gap-2 pt-1">
                  <FeatureRow
                    label={
                      <>
                        <strong>{formatDocs(plan.maxDocumentsPerMonth)}</strong> documentos / mes
                      </>
                    }
                    enabled
                  />
                  {Object.entries(features).map(([key, val]) => {
                    const label = FEATURE_LABELS[key] ?? key;
                    const isEnabled = val !== false && val !== 0 && val !== '0';
                    return (
                      <FeatureRow
                        key={key}
                        enabled={isEnabled}
                        label={
                          <>
                            {label}
                            {typeof val !== 'boolean' && val !== null && (
                              <span
                                className="ml-1 mono tnum"
                                style={{ color: 'var(--muted-foreground)' }}
                              >
                                ({renderFeatureValue(val)})
                              </span>
                            )}
                          </>
                        }
                      />
                    );
                  })}
                </div>
              </div>
            );
          })}
        </div>
      )}

      <p
        className="t-caption mt-6 text-center"
        style={{ color: 'var(--muted-foreground)' }}
      >
        Los precios están en USD. Para facturación personalizada, contacta al equipo de ventas.
      </p>
    </div>
  );
}

function FeatureRow({ label, enabled }: { label: React.ReactNode; enabled: boolean }) {
  return (
    <div className="flex items-center gap-2 t-body-sm">
      {enabled ? (
        <CheckCircle2
          className="h-4 w-4 shrink-0"
          style={{ color: 'var(--success)' }}
        />
      ) : (
        <X
          className="h-4 w-4 shrink-0"
          style={{ color: 'var(--muted-foreground)' }}
        />
      )}
      <span style={{ color: enabled ? 'var(--foreground)' : 'var(--muted-foreground)' }}>
        {label}
      </span>
    </div>
  );
}
