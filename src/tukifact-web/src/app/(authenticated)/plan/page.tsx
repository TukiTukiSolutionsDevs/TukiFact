'use client';

import { useEffect, useState, useCallback } from 'react';
import Script from 'next/script';
import { api, type Plan } from '@/lib/api';
import { useAuth } from '@/lib/auth-context';
import { Section } from '@/components/ui/section';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { StatusBadge } from '@/components/ui/status-badge';
import {
  CheckCircle2,
  Crown,
  Mail,
  Loader2,
  X,
  Sparkles,
  ArrowUpRight,
  ArrowDownRight,
  AlertTriangle,
  Check,
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
      close?: () => void;
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
  planId?: string;
  planName?: string;
}

const formatPrice = (price: number) => {
  if (price === 0) return 'Gratis';
  return new Intl.NumberFormat('es-PE', {
    style: 'currency',
    currency: 'PEN',
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
  const [welcomePlan, setWelcomePlan] = useState<Plan | null>(null);
  const [cancelOpen, setCancelOpen] = useState(false);
  const [changePlanTarget, setChangePlanTarget] = useState<Plan | null>(null);
  const [isChangingPlan, setIsChangingPlan] = useState(false);

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
        const t = await api.get<TenantInfo>('/v1/tenant');
        setTenant(t);
      } catch {
        // optional — fall back to plans[0] for display
      }
      await reloadSubscription();
      setIsLoading(false);
    };
    load();
  }, [reloadSubscription]);

  const currentPlan = tenant?.planId
    ? (plans.find((p) => p.id === tenant.planId) ?? plans[0] ?? null)
    : (plans.find((p) => p.name === tenant?.planName) ?? plans[0] ?? null);

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
      const token = window.Culqi?.token?.id;
      if (!token) {
        const err = window.Culqi?.error;
        toast.error(err?.user_message ?? err?.merchant_message ?? 'No se pudo tokenizar la tarjeta.');
        window.Culqi?.close?.();
        setPendingPlanId(null);
        return;
      }
      // Close the Culqi modal as soon as we have the token — the backend call
      // can take a few seconds and the modal blocking the page is a bad UX.
      window.Culqi?.close?.();
      try {
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
        await reloadSubscription();
        const t = await api.get<TenantInfo>('/v1/tenant').catch(() => null);
        if (t) setTenant(t);
        setWelcomePlan(plan);
      } catch (err) {
        toast.error(err instanceof Error ? err.message : 'No se pudo registrar la suscripción.');
      } finally {
        setPendingPlanId(null);
      }
    };

    window.Culqi.open();
  };

  const performCancel = async () => {
    if (!subscription) return;
    try {
      await api.post('/v1/billing/cancel', { reason: 'user_initiated' });
      toast.success('Suscripción cancelada. Volviste al plan Gratis.');
      await reloadSubscription();
      const t = await api.get<TenantInfo>('/v1/tenant').catch(() => null);
      if (t) setTenant(t);
      setCancelOpen(false);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'No se pudo cancelar la suscripción.');
    }
  };

  const performChangePlan = async (target: Plan) => {
    setIsChangingPlan(true);
    try {
      await api.post('/v1/billing/change-plan', { newPlanId: target.id });
      await reloadSubscription();
      const t = await api.get<TenantInfo>('/v1/tenant').catch(() => null);
      if (t) setTenant(t);
      setChangePlanTarget(null);
      setWelcomePlan(target);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'No se pudo cambiar de plan.');
    } finally {
      setIsChangingPlan(false);
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
      {welcomePlan && (
        <WelcomeModal plan={welcomePlan} onClose={() => setWelcomePlan(null)} />
      )}
      {cancelOpen && subscription && (
        <CancelSubscriptionModal
          planName={subscription.planName}
          onCancel={() => setCancelOpen(false)}
          onConfirm={performCancel}
        />
      )}
      {changePlanTarget && currentPlan && (
        <ChangePlanModal
          currentPlan={currentPlan}
          targetPlan={changePlanTarget}
          isLoading={isChangingPlan}
          onCancel={() => setChangePlanTarget(null)}
          onConfirm={() => performChangePlan(changePlanTarget)}
        />
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
          <Button variant="outline" onClick={() => setCancelOpen(true)}>
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
            const isFeatured = idx === 1 && plans.length >= 3 && !isCurrent;
            const features = plan.features as Record<string, unknown>;
            const hasPaidSub = !!subscription?.isCulqiManaged && subscription.status !== 'cancelled';
            const isUpgrade = !!currentPlan && plan.priceMonthly > currentPlan.priceMonthly;

            return (
              <div
                key={plan.id}
                className={cn(
                  'relative rounded-[var(--radius-lg)] border p-6 flex flex-col gap-4 transition-colors'
                )}
                style={{
                  background: isCurrent
                    ? 'color-mix(in oklch, var(--success) 6%, var(--card))'
                    : 'var(--card)',
                  boxShadow: isCurrent
                    ? 'var(--shadow-md), 0 0 0 1px color-mix(in oklch, var(--success) 30%, transparent)'
                    : isFeatured
                      ? 'var(--shadow-md)'
                      : 'var(--shadow-xs)',
                  borderColor: isCurrent
                    ? 'var(--success)'
                    : isFeatured
                      ? 'var(--accent)'
                      : 'var(--border)',
                  borderWidth: isCurrent || isFeatured ? '2px' : '1px',
                }}
              >
                {isCurrent && (
                  <span
                    className="absolute t-caption font-bold px-2.5 py-1 rounded-full uppercase tracking-wider inline-flex items-center gap-1"
                    style={{
                      top: -12,
                      left: 24,
                      background: 'var(--success)',
                      color: 'white',
                      fontSize: 11,
                    }}
                  >
                    <Crown className="h-3 w-3" /> Tu plan actual
                  </span>
                )}
                {!isCurrent && isFeatured && (
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
                    <span className="t-h3 m-0 inline-flex items-center gap-2">
                      {plan.name}
                      {isCurrent && (
                        <Crown
                          className="h-4 w-4"
                          style={{ color: 'var(--success)' }}
                          aria-hidden
                        />
                      )}
                    </span>
                    {isCurrent && <StatusBadge status="active" label="Activo" />}
                  </div>
                  <div className="flex items-baseline gap-1.5 mt-1">
                    <span className="t-body-sm font-semibold" style={{ color: 'var(--muted-foreground)' }}>
                      {plan.priceMonthly === 0 ? '' : 'S/'}
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
                  <Button
                    variant="outline"
                    disabled
                    style={{
                      borderColor: 'var(--success)',
                      color: 'var(--success)',
                      background: 'color-mix(in oklch, var(--success) 10%, transparent)',
                      opacity: 1,
                    }}
                  >
                    <Check className="h-4 w-4 mr-2" /> Plan actual
                  </Button>
                ) : plan.priceMonthly === 0 ? (
                  hasPaidSub ? (
                    <Button variant="outline" onClick={() => setCancelOpen(true)}>
                      <ArrowDownRight className="h-4 w-4 mr-2" /> Bajar a Gratis
                    </Button>
                  ) : (
                    <Button variant="outline" disabled>
                      <Mail className="h-4 w-4 mr-2" /> Solo por cancelación
                    </Button>
                  )
                ) : hasPaidSub ? (
                  <Button
                    variant="outline"
                    onClick={() => setChangePlanTarget(plan)}
                    disabled={pendingPlanId === plan.id || isChangingPlan}
                  >
                    {isUpgrade ? (
                      <><ArrowUpRight className="h-4 w-4 mr-2" /> Subir a {plan.name}</>
                    ) : (
                      <><ArrowDownRight className="h-4 w-4 mr-2" /> Cambiar a {plan.name}</>
                    )}
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
        Los precios están en PEN (soles peruanos). Para facturación personalizada, contacta al equipo de ventas.
      </p>
    </div>
  );
}

function CancelSubscriptionModal({
  planName,
  onCancel,
  onConfirm,
}: {
  planName: string;
  onCancel: () => void;
  onConfirm: () => Promise<void>;
}) {
  const [typed, setTyped] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const CONFIRM_PHRASE = 'CANCELAR';
  const matches = typed.trim().toUpperCase() === CONFIRM_PHRASE;

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !submitting) onCancel();
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onCancel, submitting]);

  const handleConfirm = async () => {
    if (!matches || submitting) return;
    setSubmitting(true);
    try {
      await onConfirm();
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="cancel-sub-title"
      className="fixed inset-0 z-50 flex items-center justify-center p-4"
      style={{ background: 'color-mix(in oklch, var(--foreground) 50%, transparent)' }}
      onClick={() => !submitting && onCancel()}
    >
      <div
        className="relative w-full max-w-md rounded-[var(--radius-lg)] bg-card border p-6 flex flex-col gap-4"
        style={{ borderColor: 'var(--border)', boxShadow: 'var(--shadow-md)' }}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-start gap-3">
          <span
            className="shrink-0 h-10 w-10 rounded-full flex items-center justify-center"
            style={{
              background: 'color-mix(in oklch, var(--danger) 14%, transparent)',
              color: 'var(--danger)',
            }}
          >
            <AlertTriangle className="h-5 w-5" />
          </span>
          <div className="min-w-0">
            <h2 id="cancel-sub-title" className="t-h3 m-0">
              ¿Cancelar tu plan {planName}?
            </h2>
            <p className="t-body-sm mt-1 mb-0" style={{ color: 'var(--muted-foreground)' }}>
              Esta acción es <strong style={{ color: 'var(--foreground)' }}>inmediata</strong> y vas a:
            </p>
          </div>
        </div>

        <ul className="t-body-sm space-y-1.5 list-disc pl-5" style={{ color: 'var(--muted-foreground)' }}>
          <li>Perder el acceso a todos los beneficios del plan {planName}.</li>
          <li>Volver al plan <strong style={{ color: 'var(--foreground)' }}>Gratis</strong> (10 documentos/mes).</li>
          <li>No recibir reembolso de lo ya pagado por el mes en curso.</li>
        </ul>

        <div>
          <label className="t-body-sm font-medium block mb-1.5">
            Para confirmar, escribí{' '}
            <span
              className="mono font-bold px-1.5 py-0.5 rounded"
              style={{
                background: 'color-mix(in oklch, var(--danger) 12%, transparent)',
                color: 'var(--danger)',
              }}
            >
              {CONFIRM_PHRASE}
            </span>
          </label>
          <Input
            value={typed}
            onChange={(e) => setTyped(e.target.value)}
            placeholder={CONFIRM_PHRASE}
            autoFocus
            disabled={submitting}
            autoComplete="off"
            className="mono"
          />
        </div>

        <div className="flex gap-2 justify-end pt-1">
          <Button variant="outline" onClick={onCancel} disabled={submitting}>
            No, mantener mi plan
          </Button>
          <Button
            onClick={handleConfirm}
            disabled={!matches || submitting}
            style={{
              background: matches ? 'var(--danger)' : undefined,
              color: matches ? 'white' : undefined,
              opacity: matches ? 1 : 0.5,
            }}
          >
            {submitting ? (
              <><Loader2 className="h-4 w-4 mr-2 animate-spin" /> Cancelando…</>
            ) : (
              <>Sí, cancelar suscripción</>
            )}
          </Button>
        </div>
      </div>
    </div>
  );
}

function ChangePlanModal({
  currentPlan,
  targetPlan,
  isLoading,
  onCancel,
  onConfirm,
}: {
  currentPlan: Plan;
  targetPlan: Plan;
  isLoading: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}) {
  const isUpgrade = targetPlan.priceMonthly > currentPlan.priceMonthly;

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !isLoading) onCancel();
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onCancel, isLoading]);

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="change-plan-title"
      className="fixed inset-0 z-50 flex items-center justify-center p-4"
      style={{ background: 'color-mix(in oklch, var(--foreground) 50%, transparent)' }}
      onClick={() => !isLoading && onCancel()}
    >
      <div
        className="relative w-full max-w-md rounded-[var(--radius-lg)] bg-card border p-6 flex flex-col gap-4"
        style={{ borderColor: 'var(--border)', boxShadow: 'var(--shadow-md)' }}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-start gap-3">
          <span
            className="shrink-0 h-10 w-10 rounded-full flex items-center justify-center"
            style={{
              background: 'color-mix(in oklch, var(--accent) 18%, transparent)',
              color: 'var(--brand-ink)',
            }}
          >
            {isUpgrade ? <ArrowUpRight className="h-5 w-5" /> : <ArrowDownRight className="h-5 w-5" />}
          </span>
          <div className="min-w-0">
            <h2 id="change-plan-title" className="t-h3 m-0">
              {isUpgrade ? 'Subir' : 'Cambiar'} al plan {targetPlan.name}
            </h2>
            <p className="t-body-sm mt-1 mb-0" style={{ color: 'var(--muted-foreground)' }}>
              {currentPlan.name} → <strong style={{ color: 'var(--foreground)' }}>{targetPlan.name}</strong>
            </p>
          </div>
        </div>

        <div
          className="rounded-[var(--radius-md)] p-3 t-body-sm space-y-1.5"
          style={{
            background: 'color-mix(in oklch, var(--muted) 50%, transparent)',
          }}
        >
          <div className="flex justify-between">
            <span style={{ color: 'var(--muted-foreground)' }}>Cobro a partir de hoy</span>
            <span className="mono tnum font-semibold">S/ {targetPlan.priceMonthly} / mes</span>
          </div>
          <div className="flex justify-between">
            <span style={{ color: 'var(--muted-foreground)' }}>Documentos / mes</span>
            <span className="mono tnum">{formatDocs(targetPlan.maxDocumentsPerMonth)}</span>
          </div>
          <div className="flex justify-between">
            <span style={{ color: 'var(--muted-foreground)' }}>Método de pago</span>
            <span className="t-body-sm">Tarjeta guardada</span>
          </div>
        </div>

        <p className="t-caption m-0" style={{ color: 'var(--muted-foreground)' }}>
          Vamos a usar la tarjeta que ya tenés registrada. La suscripción a {currentPlan.name} se cancela
          automáticamente. {isUpgrade ? 'Ya quedás con la nueva quota mensual disponible.' : 'Conservás los documentos que ya emitiste este mes.'}
        </p>

        <div className="flex gap-2 justify-end pt-1">
          <Button variant="outline" onClick={onCancel} disabled={isLoading}>
            Cancelar
          </Button>
          <Button onClick={onConfirm} disabled={isLoading}>
            {isLoading ? (
              <><Loader2 className="h-4 w-4 mr-2 animate-spin" /> Cambiando…</>
            ) : (
              <>Confirmar cambio a {targetPlan.name}</>
            )}
          </Button>
        </div>
      </div>
    </div>
  );
}

function WelcomeModal({ plan, onClose }: { plan: Plan; onClose: () => void }) {
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onClose]);

  const features = plan.features as Record<string, unknown>;
  const highlightedFeatures = Object.entries(features)
    .filter(([, val]) => val !== false && val !== 0 && val !== '0')
    .slice(0, 6);

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="welcome-plan-title"
      className="fixed inset-0 z-50 flex items-center justify-center p-4"
      style={{ background: 'color-mix(in oklch, var(--foreground) 50%, transparent)' }}
      onClick={onClose}
    >
      <div
        className="relative w-full max-w-md rounded-[var(--radius-lg)] bg-card border p-7 flex flex-col items-center text-center"
        style={{ borderColor: 'var(--border)', boxShadow: 'var(--shadow-md)' }}
        onClick={(e) => e.stopPropagation()}
      >
        <button
          type="button"
          onClick={onClose}
          aria-label="Cerrar"
          className="absolute top-3 right-3 p-1.5 rounded-[var(--radius-md)] hover:bg-[color-mix(in_oklch,var(--foreground)_8%,transparent)]"
          style={{ color: 'var(--muted-foreground)' }}
        >
          <X className="h-4 w-4" />
        </button>

        <span
          className="h-14 w-14 rounded-full flex items-center justify-center mb-4"
          style={{
            background: 'color-mix(in oklch, var(--success) 18%, transparent)',
            color: 'var(--success)',
          }}
        >
          <CheckCircle2 className="h-7 w-7" />
        </span>

        <h2 id="welcome-plan-title" className="t-h2 m-0">
          ¡Bienvenido al plan {plan.name}!
        </h2>
        <p className="t-body mt-2 mb-0" style={{ color: 'var(--muted-foreground)' }}>
          Tu suscripción está activa. Vas a poder emitir hasta{' '}
          <strong style={{ color: 'var(--foreground)' }}>
            {formatDocs(plan.maxDocumentsPerMonth)}
          </strong>{' '}
          documentos al mes y aprovechar todos los beneficios incluidos.
        </p>

        {highlightedFeatures.length > 0 && (
          <div className="w-full mt-5 grid grid-cols-1 gap-2 text-left">
            {highlightedFeatures.map(([key, val]) => {
              const label = FEATURE_LABELS[key] ?? key;
              return (
                <FeatureRow
                  key={key}
                  enabled
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
        )}

        <Button onClick={onClose} className="mt-6 w-full">
          <Sparkles className="h-4 w-4 mr-2" /> Empezar a usar TukiFact {plan.name}
        </Button>
      </div>
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
