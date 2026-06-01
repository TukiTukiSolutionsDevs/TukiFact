'use client';

import { useState, useMemo, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import Image from 'next/image';
import { GoogleLogin, type CredentialResponse } from '@react-oauth/google';
import {
  Mail,
  Lock,
  Eye,
  EyeOff,
  ShieldCheck,
  CheckCircle2,
  Check,
  ChevronRight,
  ChevronLeft,
  User,
  Building2,
  MapPin,
  Hash,
} from 'lucide-react';
import { useAuth } from '@/lib/auth-context';
import { Button } from '@/components/ui/button';
import { HeroPanel } from '@/components/auth/HeroPanel';
import { AuthInput } from '@/components/auth/AuthInput';
import { toast } from 'sonner';

interface GoogleProfile {
  email: string;
  name?: string;
}

function parseJwt(token: string): GoogleProfile | null {
  try {
    const payload = token.split('.')[1];
    const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
    const data = JSON.parse(json) as { email?: string; name?: string };
    if (!data.email) return null;
    return { email: data.email, name: data.name };
  } catch {
    return null;
  }
}

function Stepper({ step }: { step: 1 | 2 }) {
  const steps: [number, string][] = [
    [1, 'Tu empresa'],
    [2, 'Tu cuenta'],
  ];
  return (
    <div className="flex items-center mb-7">
      {steps.map(([n, label], i) => {
        const done = step > n;
        const active = step === n;
        return (
          <div
            key={n}
            className="flex items-center"
            style={{ flex: i < steps.length - 1 ? 1 : '0 0 auto' }}
          >
            <div className="flex items-center gap-2">
              <span
                className="flex h-7 w-7 items-center justify-center rounded-full text-[13px] font-bold transition-all"
                style={{
                  background: active || done ? 'var(--accent)' : 'var(--slate-200)',
                  color: active || done ? 'var(--brand-ink)' : 'var(--muted-foreground)',
                }}
              >
                {done ? <Check className="h-3.5 w-3.5" strokeWidth={3} /> : n}
              </span>
              <span
                className="t-label"
                style={{
                  color: active || done ? 'var(--foreground)' : 'var(--muted-foreground)',
                  fontWeight: active ? 600 : 500,
                }}
              >
                {label}
              </span>
            </div>
            {i < steps.length - 1 && (
              <div
                className="flex-1 mx-3 rounded-sm transition-colors"
                style={{
                  height: 2,
                  background: done ? 'var(--accent)' : 'var(--slate-200)',
                }}
              />
            )}
          </div>
        );
      })}
    </div>
  );
}

export default function RegisterPage() {
  const { register, registerWithGoogle } = useAuth();
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(false);
  const [step, setStep] = useState<1 | 2>(1);
  const [done, setDone] = useState(false);

  const [form, setForm] = useState({
    ruc: '',
    razonSocial: '',
    nombreComercial: '',
    direccion: '',
    adminEmail: '',
    adminPassword: '',
    adminPasswordConfirm: '',
    adminFullName: '',
  });
  const [showPwd, setShowPwd] = useState(false);
  const [terms, setTerms] = useState(false);
  const [news, setNews] = useState(true);
  const [googleIdToken, setGoogleIdToken] = useState<string | null>(null);
  const [googleProfile, setGoogleProfile] = useState<GoogleProfile | null>(null);

  const rucOk = /^(10|20)\d{9}$/.test(form.ruc);
  const step1Valid = rucOk && form.razonSocial.trim().length > 0;

  const strength = useMemo(() => {
    const p = form.adminPassword;
    return Math.min(
      4,
      (p.length >= 8 ? 1 : 0) +
        (/[A-Z]/.test(p) ? 1 : 0) +
        (/[0-9]/.test(p) ? 1 : 0) +
        (/[^A-Za-z0-9]/.test(p) ? 1 : 0)
    );
  }, [form.adminPassword]);
  const strengthLabel = ['', 'Débil', 'Aceptable', 'Buena', 'Fuerte'][strength];
  const strengthColor = [
    'var(--muted-foreground)',
    'var(--danger)',
    'var(--warning)',
    'var(--info)',
    'var(--success)',
  ][strength];

  const update =
    <K extends keyof typeof form>(field: K) =>
    (e: React.ChangeEvent<HTMLInputElement>) =>
      setForm((f) => ({ ...f, [field]: e.target.value }));

  const handleGoogleSuccess = (cred: CredentialResponse) => {
    if (!cred.credential) {
      toast.error('No se recibió el token de Google');
      return;
    }
    const profile = parseJwt(cred.credential);
    if (!profile) {
      toast.error('No se pudo leer tu información de Google');
      return;
    }
    setGoogleIdToken(cred.credential);
    setGoogleProfile(profile);
    setForm((f) => ({
      ...f,
      adminEmail: profile.email,
      adminFullName: profile.name ?? '',
    }));
    toast.success('Cuenta de Google vinculada — completa los datos de la empresa');
  };

  // When /login redirects here with ?from=google, pick up the pending id_token from
  // sessionStorage and pre-fill the form so the user only has to type RUC + Razón Social.
  // (Reading window.location avoids useSearchParams + Suspense plumbing.)
  useEffect(() => {
    if (typeof window === 'undefined') return;
    const params = new URLSearchParams(window.location.search);
    if (params.get('from') !== 'google') return;
    const token = sessionStorage.getItem('tukifact:pending-google-token');
    if (!token) return;
    sessionStorage.removeItem('tukifact:pending-google-token');
    handleGoogleSuccess({ credential: token } as CredentialResponse);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const clearGoogle = () => {
    setGoogleIdToken(null);
    setGoogleProfile(null);
    setForm((f) => ({ ...f, adminEmail: '', adminFullName: '', adminPassword: '', adminPasswordConfirm: '' }));
  };

  const onContinue = () => {
    if (!step1Valid) {
      toast.error('Completa RUC y Razón Social');
      return;
    }
    if (googleIdToken) {
      submit();
    } else {
      setStep(2);
    }
  };

  const submit = async () => {
    if (!googleIdToken) {
      if (form.adminPassword !== form.adminPasswordConfirm) {
        toast.error('Las contraseñas no coinciden');
        return;
      }
      if (strength < 2) {
        toast.error('Tu contraseña es muy débil');
        return;
      }
      if (!terms) {
        toast.error('Debes aceptar los términos para continuar');
        return;
      }
    }

    setIsLoading(true);
    try {
      if (googleIdToken) {
        await registerWithGoogle(googleIdToken, {
          ruc: form.ruc,
          razonSocial: form.razonSocial,
          nombreComercial: form.nombreComercial || undefined,
          direccion: form.direccion || undefined,
        });
      } else {
        await register({
          ruc: form.ruc,
          razonSocial: form.razonSocial,
          nombreComercial: form.nombreComercial,
          direccion: form.direccion,
          adminEmail: form.adminEmail,
          adminPassword: form.adminPassword,
          adminFullName: form.adminFullName,
        });
      }
      setDone(true);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al registrar');
    } finally {
      setIsLoading(false);
    }
  };

  if (done) {
    return (
      <div className="min-h-screen grid grid-cols-1 lg:grid-cols-2 bg-background">
        <HeroPanel
          headline="Empieza gratis hoy."
          sub="Tu facturación electrónica, lista para producción en minutos."
          bullets={[
            'Hasta 100 comprobantes gratis al mes',
            'Listo para SUNAT producción',
            'Soporte en Lima',
          ]}
        />
        <div className="flex items-center justify-center px-6 py-10 lg:px-12">
          <div
            className="w-full max-w-[420px] text-center rounded-[var(--radius-lg)] border bg-card p-10"
            style={{ boxShadow: 'var(--shadow-sm)' }}
          >
            <Image src="/icon.png" alt="" width={96} height={96} className="object-contain mx-auto mb-2" />
            <h2 className="t-h1 mt-2 mb-2">¡Bienvenido a TukiFact!</h2>
            <p className="t-body mx-auto mb-6" style={{ color: 'var(--muted-foreground)', maxWidth: 320 }}>
              Te enviamos un correo para verificar tu email. Mientras tanto, configuremos tu empresa.
            </p>
            <Button
              size="lg"
              className="w-full h-12 font-semibold"
              style={{ background: 'var(--accent)', color: 'var(--accent-foreground)' }}
              onClick={() => router.push('/dashboard')}
            >
              Continuar
            </Button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen grid grid-cols-1 lg:grid-cols-2 bg-background">
      <HeroPanel
        headline="Tu facturación, lista en segundos."
        sub="Crea tu cuenta y emite tu primer comprobante hoy mismo."
        bullets={[
          'Hasta 100 comprobantes gratis al mes',
          'Listo para SUNAT producción',
          'Soporte en Lima',
        ]}
      />

      <div className="flex items-center justify-center px-6 py-10 lg:px-12 lg:py-12 relative">
        <div className="lg:hidden absolute top-6 left-6">
          <Image src="/logo.png" alt="TukiFact" width={140} height={44} className="object-contain" priority />
        </div>

        <div className="w-full max-w-[420px]">
          <h2 className="t-display-lg m-0">Crea tu cuenta</h2>
          <p className="t-body mt-1.5 mb-7" style={{ color: 'var(--muted-foreground)' }}>
            {step === 1 ? 'Empecemos por los datos de tu empresa.' : 'Ahora configura tu acceso de administrador.'}
          </p>

          <Stepper step={step} />

          {step === 1 ? (
            <>
              {googleProfile ? (
                <div
                  className="flex items-center justify-between rounded-[var(--radius-md)] border p-3 mb-4"
                  style={{
                    background: 'color-mix(in oklch, var(--info) 8%, transparent)',
                    borderColor: 'color-mix(in oklch, var(--info) 35%, transparent)',
                  }}
                >
                  <div className="min-w-0">
                    <div className="t-label" style={{ color: 'var(--info)' }}>
                      Vinculado con Google
                    </div>
                    <div className="t-body-sm truncate" style={{ color: 'var(--info)' }}>
                      {googleProfile.email}
                    </div>
                  </div>
                  <Button type="button" variant="ghost" size="sm" onClick={clearGoogle}>
                    Quitar
                  </Button>
                </div>
              ) : (
                <>
                  <div className="flex justify-center">
                    <GoogleLogin
                      onSuccess={handleGoogleSuccess}
                      onError={() => toast.error('No se pudo conectar con Google')}
                      useOneTap={false}
                      text="signup_with"
                      shape="rectangular"
                      locale="es"
                      width="400"
                    />
                  </div>
                  <div className="flex items-center gap-3 my-5">
                    <div className="flex-1 h-px bg-border" />
                    <span className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
                      o registra con email
                    </span>
                    <div className="flex-1 h-px bg-border" />
                  </div>
                </>
              )}

              <div className="flex flex-col gap-4">
                <AuthInput
                  label="RUC"
                  numeric
                  inputMode="numeric"
                  maxLength={11}
                  leadingIcon={Hash}
                  placeholder="20XXXXXXXXX"
                  value={form.ruc}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, ruc: e.target.value.replace(/\D/g, '') }))
                  }
                  trailingIcon={rucOk ? ShieldCheck : undefined}
                  helper={rucOk ? undefined : 'Debe tener 11 dígitos y empezar en 10 o 20.'}
                  style={rucOk ? { borderColor: 'var(--success)' } : undefined}
                />
                {rucOk && (
                  <div
                    className="t-body-sm flex items-center gap-1.5 -mt-2"
                    style={{ color: 'var(--success)' }}
                  >
                    <CheckCircle2 className="h-4 w-4" /> RUC válido.
                  </div>
                )}
                <AuthInput
                  label="Razón Social"
                  leadingIcon={Building2}
                  placeholder="MI EMPRESA SAC"
                  value={form.razonSocial}
                  onChange={update('razonSocial')}
                  style={{ textTransform: 'uppercase' }}
                />
                <AuthInput
                  label="Nombre Comercial"
                  placeholder="Cómo te conocen"
                  helper="Opcional. Puede ser distinto a la razón social."
                  value={form.nombreComercial}
                  onChange={update('nombreComercial')}
                />
                <AuthInput
                  label="Dirección fiscal"
                  leadingIcon={MapPin}
                  placeholder="Av. Ejemplo 123, Lima"
                  value={form.direccion}
                  onChange={update('direccion')}
                />
              </div>

              <div className="flex items-center justify-between mt-6">
                <Link href="/login" className="t-body-sm font-medium" style={{ color: 'var(--info)' }}>
                  Ya tengo cuenta
                </Link>
                <Button onClick={onContinue} disabled={!step1Valid || isLoading}>
                  {googleIdToken ? (isLoading ? 'Creando...' : 'Crear cuenta') : 'Continuar'}
                  {!googleIdToken && <ChevronRight className="ml-1 h-4 w-4" />}
                </Button>
              </div>
            </>
          ) : (
            <>
              <div className="flex flex-col gap-4">
                <AuthInput
                  label="Nombre Completo"
                  leadingIcon={User}
                  placeholder="Como aparece en tu DNI"
                  value={form.adminFullName}
                  onChange={update('adminFullName')}
                />
                <AuthInput
                  label="Email"
                  type="email"
                  leadingIcon={Mail}
                  placeholder="tu@empresa.pe"
                  value={form.adminEmail}
                  onChange={update('adminEmail')}
                />
                <div>
                  <AuthInput
                    label="Contraseña"
                    type={showPwd ? 'text' : 'password'}
                    leadingIcon={Lock}
                    trailingIcon={showPwd ? EyeOff : Eye}
                    onTrailingClick={() => setShowPwd((v) => !v)}
                    placeholder="8+ caracteres, 1 mayúscula, 1 número"
                    value={form.adminPassword}
                    onChange={update('adminPassword')}
                  />
                  {form.adminPassword && (
                    <div className="flex items-center gap-2 mt-2">
                      <div className="flex gap-1 flex-1">
                        {[1, 2, 3, 4].map((i) => (
                          <div
                            key={i}
                            className="flex-1 rounded-sm transition-colors"
                            style={{
                              height: 4,
                              background: i <= strength ? strengthColor : 'var(--muted)',
                            }}
                          />
                        ))}
                      </div>
                      <span
                        className="t-caption text-right"
                        style={{ color: strengthColor, fontWeight: 600, minWidth: 56 }}
                      >
                        {strengthLabel}
                      </span>
                    </div>
                  )}
                </div>
                <AuthInput
                  label="Confirmar contraseña"
                  type="password"
                  leadingIcon={Lock}
                  placeholder="Repite tu contraseña"
                  value={form.adminPasswordConfirm}
                  onChange={update('adminPasswordConfirm')}
                  error={
                    form.adminPasswordConfirm &&
                    form.adminPasswordConfirm !== form.adminPassword
                      ? 'No coincide con la contraseña'
                      : undefined
                  }
                />

                <div className="flex flex-col gap-3 mt-1">
                  <label className="inline-flex items-start gap-2 cursor-pointer select-none">
                    <input
                      type="checkbox"
                      checked={terms}
                      onChange={(e) => setTerms(e.target.checked)}
                      className="h-4 w-4 mt-0.5 rounded border-[var(--input)] accent-[var(--brand-ink)]"
                    />
                    <span className="t-body-sm">
                      Acepto los{' '}
                      <Link href="/terms" className="font-medium" style={{ color: 'var(--info)' }}>
                        Términos de Servicio
                      </Link>{' '}
                      y la{' '}
                      <Link href="/privacy" className="font-medium" style={{ color: 'var(--info)' }}>
                        Política de Privacidad
                      </Link>
                      .
                    </span>
                  </label>
                  <label className="inline-flex items-start gap-2 cursor-pointer select-none">
                    <input
                      type="checkbox"
                      checked={news}
                      onChange={(e) => setNews(e.target.checked)}
                      className="h-4 w-4 mt-0.5 rounded border-[var(--input)] accent-[var(--brand-ink)]"
                    />
                    <span className="t-body-sm">Quiero recibir novedades por email.</span>
                  </label>
                </div>
              </div>

              <div className="flex items-center justify-between mt-6">
                <Button variant="ghost" onClick={() => setStep(1)}>
                  <ChevronLeft className="mr-1 h-4 w-4" /> Volver
                </Button>
                <Button
                  onClick={submit}
                  disabled={!terms || isLoading}
                  style={{ background: 'var(--accent)', color: 'var(--accent-foreground)', fontWeight: 600 }}
                >
                  {isLoading ? 'Creando...' : 'Crear cuenta'}
                </Button>
              </div>
            </>
          )}

          <p className="t-caption text-center mt-6" style={{ color: 'var(--muted-foreground)' }}>
            ¿Ya tienes cuenta?{' '}
            <Link href="/login" className="font-semibold" style={{ color: 'var(--brand-toucan-orange)' }}>
              Inicia sesión
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
