'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import Image from 'next/image';
import { GoogleLogin, type CredentialResponse } from '@react-oauth/google';
import { Mail, Lock, Eye, EyeOff, Building2 } from 'lucide-react';
import { useAuth, type TenantChoice } from '@/lib/auth-context';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { HeroPanel } from '@/components/auth/HeroPanel';
import { AuthInput } from '@/components/auth/AuthInput';
import { toast } from 'sonner';

export default function LoginPage() {
  const { login, loginWithGoogle, loginWithGoogleAtTenant } = useAuth();
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(false);
  const [showPwd, setShowPwd] = useState(false);
  const [remember, setRemember] = useState(true);
  const [form, setForm] = useState({ email: '', password: '', tenantId: '' });
  const [pickerOpen, setPickerOpen] = useState(false);
  const [pickerTenants, setPickerTenants] = useState<TenantChoice[]>([]);
  const [pendingIdToken, setPendingIdToken] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    try {
      await login(form.email, form.password, form.tenantId);
      toast.success('Sesión iniciada correctamente');
      router.push('/dashboard');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al iniciar sesión');
    } finally {
      setIsLoading(false);
    }
  };

  const handleGoogleSuccess = async (cred: CredentialResponse) => {
    if (!cred.credential) {
      toast.error('No se recibió el token de Google');
      return;
    }
    setIsLoading(true);
    try {
      const outcome = await loginWithGoogle(cred.credential);
      if (outcome.kind === 'ok') {
        toast.success('Sesión iniciada con Google');
        router.push('/dashboard');
        return;
      }
      if (outcome.kind === 'needs-register') {
        // Hand the Google credential to /register so the wizard skips authentication
        // and only asks for the missing company data.
        sessionStorage.setItem('tukifact:pending-google-token', cred.credential);
        toast.info(`Aún no tenés cuenta con ${outcome.prompt.email}. Vamos a crearla.`);
        router.push('/register?from=google');
        return;
      }
      // pick-tenant
      setPendingIdToken(cred.credential);
      setPickerTenants(outcome.tenants);
      setPickerOpen(true);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al iniciar sesión con Google');
    } finally {
      setIsLoading(false);
    }
  };

  const pickTenant = async (choice: TenantChoice) => {
    if (!pendingIdToken) return;
    setIsLoading(true);
    try {
      await loginWithGoogleAtTenant(pendingIdToken, choice.tenantId);
      setPickerOpen(false);
      toast.success(`Sesión iniciada en ${choice.razonSocial}`);
      router.push('/dashboard');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'No se pudo iniciar sesión');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen grid grid-cols-1 lg:grid-cols-2 bg-background">
      <HeroPanel
        headline="SUNAT al día. Tú, tranquilo."
        sub="Emite, valida y gestiona tus comprobantes electrónicos desde un solo lugar."
      />

      <div className="flex items-center justify-center px-6 py-10 lg:px-12 lg:py-12 relative">
        <div className="lg:hidden absolute top-6 left-6">
          <Image src="/logo.png" alt="TukiFact" width={140} height={44} className="object-contain" priority />
        </div>

        <div className="w-full max-w-[400px]">
          <h2 className="t-display-lg m-0">Bienvenido de vuelta</h2>
          <p className="t-body mt-1.5 mb-7" style={{ color: 'var(--muted-foreground)' }}>
            Inicia sesión para gestionar tus comprobantes.
          </p>

          <div className="flex justify-center">
            <GoogleLogin
              onSuccess={handleGoogleSuccess}
              onError={() => toast.error('No se pudo iniciar sesión con Google')}
              useOneTap={false}
              text="continue_with"
              shape="rectangular"
              locale="es"
              width="400"
            />
          </div>

          <div className="flex items-center gap-3 my-5">
            <div className="flex-1 h-px bg-border" />
            <span className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
              o con email
            </span>
            <div className="flex-1 h-px bg-border" />
          </div>

          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <AuthInput
              label="ID de Empresa"
              leadingIcon={Building2}
              placeholder="UUID de tu empresa"
              helper="Pídelo a tu administrador o cópialo de tu invitación."
              value={form.tenantId}
              onChange={(e) => setForm((f) => ({ ...f, tenantId: e.target.value }))}
              required
            />
            <AuthInput
              label="Email"
              type="email"
              leadingIcon={Mail}
              placeholder="tu@empresa.pe"
              value={form.email}
              onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))}
              required
            />
            <AuthInput
              label="Contraseña"
              type={showPwd ? 'text' : 'password'}
              leadingIcon={Lock}
              trailingIcon={showPwd ? EyeOff : Eye}
              onTrailingClick={() => setShowPwd((v) => !v)}
              value={form.password}
              onChange={(e) => setForm((f) => ({ ...f, password: e.target.value }))}
              required
            />

            <div className="flex items-center justify-between">
              <label className="inline-flex items-center gap-2 cursor-pointer select-none">
                <input
                  type="checkbox"
                  checked={remember}
                  onChange={(e) => setRemember(e.target.checked)}
                  className="h-4 w-4 rounded border-[var(--input)] accent-[var(--brand-ink)]"
                />
                <span className="t-body-sm">Recordarme</span>
              </label>
              <Link href="#" className="t-body-sm font-medium" style={{ color: 'var(--info)' }}>
                ¿Olvidaste tu contraseña?
              </Link>
            </div>

            <Button
              type="submit"
              size="lg"
              disabled={isLoading}
              className="w-full h-12 mt-1"
            >
              {isLoading ? 'Ingresando...' : 'Iniciar sesión'}
            </Button>
          </form>

          <p className="t-body-sm text-center mt-6" style={{ color: 'var(--muted-foreground)' }}>
            ¿No tienes cuenta?{' '}
            <Link
              href="/register"
              className="font-semibold"
              style={{ color: 'var(--brand-toucan-orange)' }}
            >
              Crea una
            </Link>
          </p>
        </div>
      </div>

      <Dialog open={pickerOpen} onOpenChange={setPickerOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Elige tu empresa</DialogTitle>
            <DialogDescription>
              Tu cuenta de Google está asociada a varias empresas. Selecciona con cuál quieres iniciar sesión.
            </DialogDescription>
          </DialogHeader>
          <div className="flex flex-col gap-2 mt-4">
            {pickerTenants.map((t) => (
              <button
                key={t.tenantId}
                type="button"
                onClick={() => pickTenant(t)}
                disabled={isLoading}
                className="text-left rounded-md border p-3 hover:bg-muted transition disabled:opacity-50"
              >
                <div className="font-medium">{t.razonSocial}</div>
                <div className="t-body-sm mono" style={{ color: 'var(--muted-foreground)' }}>
                  RUC {t.ruc}
                </div>
              </button>
            ))}
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
