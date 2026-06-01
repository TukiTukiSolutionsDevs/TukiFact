'use client';

import { useEffect, useState, useRef } from 'react';
import { api } from '@/lib/api';
import { Section } from '@/components/ui/section';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { StatusBadge } from '@/components/ui/status-badge';
import {
  ShieldCheck,
  Upload,
  Trash2,
  CheckCircle2,
  AlertTriangle,
  XCircle,
  Loader2,
  Key,
  Globe,
  CircleHelp,
} from 'lucide-react';
import { toast } from 'sonner';

interface CertStatus {
  hasCertificate: boolean;
  expiresAt: string | null;
  isExpired: boolean;
  daysUntilExpiry: number | null;
  environment: string;
  hasSunatCredentials: boolean;
}

const ENV_LABEL: Record<string, string> = {
  beta: 'Beta (pruebas)',
  production: 'Producción',
};

function DL({
  label,
  value,
  mono,
}: {
  label: string;
  value: React.ReactNode;
  mono?: boolean;
}) {
  return (
    <div>
      <div className="t-caption mb-0.5" style={{ color: 'var(--muted-foreground)' }}>
        {label}
      </div>
      <div className={`t-body-sm font-medium ${mono ? 'mono tnum' : ''}`}>{value}</div>
    </div>
  );
}

export default function CertificatePage() {
  const [status, setStatus] = useState<CertStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [password, setPassword] = useState('');
  const [sunatUser, setSunatUser] = useState('');
  const [sunatPass, setSunatPass] = useState('');
  const [savingCreds, setSavingCreds] = useState(false);
  const [changingEnv, setChangingEnv] = useState(false);
  const fileRef = useRef<HTMLInputElement>(null);

  const fetchStatus = async () => {
    try {
      const res = await api.get<CertStatus>('/v1/certificate/status');
      setStatus(res);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchStatus();
  }, []);

  const handleUpload = async () => {
    const file = fileRef.current?.files?.[0];
    if (!file) {
      toast.error('Selecciona un archivo .pfx o .p12');
      return;
    }
    if (!password) {
      toast.error('Ingresa la contraseña del certificado');
      return;
    }
    setUploading(true);
    try {
      const formData = new FormData();
      formData.append('file', file);
      formData.append('password', password);
      const res = await fetch('/v1/certificate/upload', {
        method: 'POST',
        headers: { Authorization: `Bearer ${localStorage.getItem('access_token')}` },
        body: formData,
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.error || 'Error subiendo certificado');
      toast.success(data.message || 'Certificado instalado');
      setPassword('');
      if (fileRef.current) fileRef.current.value = '';
      fetchStatus();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    } finally {
      setUploading(false);
    }
  };

  const handleDelete = async () => {
    if (!confirm('¿Eliminar el certificado digital? Esta acción no se puede deshacer.')) return;
    try {
      await api.delete('/v1/certificate');
      toast.success('Certificado eliminado');
      fetchStatus();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    }
  };

  const handleSaveSunat = async (e: React.FormEvent) => {
    e.preventDefault();
    setSavingCreds(true);
    try {
      await api.put('/v1/certificate/sunat-credentials', {
        sunatUser,
        sunatPassword: sunatPass,
      });
      toast.success('Credenciales SUNAT guardadas');
      setSunatPass('');
      fetchStatus();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    } finally {
      setSavingCreds(false);
    }
  };

  const handleChangeEnv = async (env: string) => {
    setChangingEnv(true);
    try {
      await api.put('/v1/certificate/environment', { environment: env });
      toast.success(`Entorno cambiado a ${ENV_LABEL[env] ?? env}`);
      fetchStatus();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    } finally {
      setChangingEnv(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center gap-3 p-6 text-[var(--muted-foreground)]">
        <Loader2 className="h-4 w-4 animate-spin" />
        <span className="t-body-sm">Cargando configuración…</span>
      </div>
    );
  }

  const certHealth = !status?.hasCertificate
    ? 'missing'
    : status.isExpired
      ? 'expired'
      : (status.daysUntilExpiry ?? Infinity) < 30
        ? 'expiring'
        : 'ok';

  return (
    <div>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0">Certificado digital y SUNAT</h1>
          <p className="t-body mt-1.5 mb-0" style={{ color: 'var(--muted-foreground)' }}>
            Configura tu certificado digital y las credenciales SOL para firmar y enviar tus
            comprobantes.
          </p>
        </div>
      </div>

      <div className="max-w-4xl flex flex-col gap-[var(--gap-cards)]">
        {/* Certificate status */}
        <Section>
          {status?.hasCertificate ? (
            <div>
              <div className="flex items-start gap-4 flex-wrap">
                <span
                  className="h-12 w-12 rounded-[var(--radius-lg)] shrink-0 flex items-center justify-center"
                  style={{
                    background:
                      certHealth === 'ok'
                        ? 'color-mix(in oklch, var(--success) 14%, transparent)'
                        : certHealth === 'expiring'
                          ? 'color-mix(in oklch, var(--warning) 14%, transparent)'
                          : 'color-mix(in oklch, var(--danger) 14%, transparent)',
                    color:
                      certHealth === 'ok'
                        ? 'var(--success)'
                        : certHealth === 'expiring'
                          ? 'var(--warning)'
                          : 'var(--danger)',
                  }}
                >
                  {certHealth === 'ok' ? (
                    <ShieldCheck className="h-6 w-6" />
                  ) : certHealth === 'expiring' ? (
                    <AlertTriangle className="h-6 w-6" />
                  ) : (
                    <XCircle className="h-6 w-6" />
                  )}
                </span>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="t-h3 m-0">
                      {certHealth === 'expired'
                        ? 'Certificado expirado'
                        : certHealth === 'expiring'
                          ? 'Certificado por expirar'
                          : 'Certificado activo'}
                    </span>
                    {certHealth === 'ok' && <StatusBadge status="active" label="Vigente" />}
                    {certHealth === 'expiring' && <StatusBadge status="pending" label="Por expirar" />}
                    {certHealth === 'expired' && <StatusBadge status="expired" />}
                  </div>
                  <p className="t-body-sm m-0 mt-0.5" style={{ color: 'var(--muted-foreground)' }}>
                    Certificado digital tributario (.pfx) que firma cada comprobante antes del
                    envío a SUNAT.
                  </p>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-x-6 gap-y-3 mt-4">
                    {status.expiresAt && (
                      <DL
                        label="Válido hasta"
                        value={new Date(status.expiresAt).toLocaleDateString('es-PE', {
                          day: '2-digit',
                          month: 'short',
                          year: 'numeric',
                        })}
                        mono
                      />
                    )}
                    {status.daysUntilExpiry !== null && (
                      <DL
                        label="Días restantes"
                        value={
                          <span
                            style={{
                              color:
                                certHealth === 'expiring'
                                  ? 'var(--warning)'
                                  : certHealth === 'expired'
                                    ? 'var(--danger)'
                                    : 'var(--foreground)',
                            }}
                          >
                            {status.daysUntilExpiry}
                          </span>
                        }
                        mono
                      />
                    )}
                  </div>
                </div>
              </div>
              <div
                className="flex gap-2 mt-5 pt-5 flex-wrap"
                style={{ borderTop: '1px solid var(--border)' }}
              >
                <Button variant="outline" onClick={() => fileRef.current?.click()}>
                  <Upload className="h-4 w-4 mr-2" /> Reemplazar certificado
                </Button>
                <Button
                  variant="ghost"
                  onClick={handleDelete}
                  style={{ color: 'var(--danger)' }}
                >
                  <Trash2 className="h-4 w-4 mr-2" /> Eliminar
                </Button>
              </div>
              <input
                type="file"
                accept=".pfx,.p12"
                ref={fileRef}
                className="hidden"
                onChange={() => {
                  if (fileRef.current?.files?.[0]) {
                    const pwd = prompt('Contraseña del certificado:') || '';
                    if (pwd) {
                      setPassword(pwd);
                      setTimeout(handleUpload, 0);
                    }
                  }
                }}
              />
            </div>
          ) : (
            <div className="flex flex-col gap-5">
              <div className="flex items-start gap-4">
                <span
                  className="h-12 w-12 rounded-[var(--radius-lg)] shrink-0 flex items-center justify-center"
                  style={{ background: 'var(--muted)', color: 'var(--muted-foreground)' }}
                >
                  <ShieldCheck className="h-6 w-6" />
                </span>
                <div className="flex-1">
                  <h3 className="t-h3 m-0">Sin certificado instalado</h3>
                  <p
                    className="t-body-sm m-0 mt-0.5"
                    style={{ color: 'var(--muted-foreground)' }}
                  >
                    Sube tu archivo .pfx o .p12 emitido por una autoridad certificadora autorizada
                    por INDECOPI.
                  </p>
                </div>
              </div>

              <label
                className="rounded-[var(--radius-lg)] border-2 border-dashed flex flex-col items-center justify-center gap-2 p-8 cursor-pointer transition-colors hover:bg-[var(--muted)]"
                style={{ borderColor: 'var(--border)' }}
              >
                <Upload className="h-7 w-7" style={{ color: 'var(--muted-foreground)' }} />
                <div className="t-body-sm font-semibold">Haz clic para seleccionar tu archivo</div>
                <div className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
                  .pfx o .p12 · máx. 5 MB
                </div>
                <input
                  type="file"
                  accept=".pfx,.p12"
                  ref={fileRef}
                  className="hidden"
                />
              </label>

              <div>
                <Label className="t-label mb-1.5 block">Contraseña del certificado</Label>
                <Input
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••"
                />
              </div>

              <Button
                onClick={handleUpload}
                disabled={uploading}
                style={{
                  background: 'var(--accent)',
                  color: 'var(--accent-foreground)',
                  fontWeight: 600,
                }}
              >
                {uploading ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Subiendo…
                  </>
                ) : (
                  <>
                    <Upload className="h-4 w-4 mr-2" /> Instalar certificado
                  </>
                )}
              </Button>
            </div>
          )}
        </Section>

        {/* SUNAT credentials + Environment */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-[var(--gap-cards)]">
          <Section
            title="Credenciales SUNAT (SOL)"
            desc="Usuario y clave SOL para autenticar tus envíos."
            right={
              status?.hasSunatCredentials ? (
                <StatusBadge status="active" label="Configuradas" />
              ) : (
                <StatusBadge status="pending" label="No configuradas" />
              )
            }
          >
            <form onSubmit={handleSaveSunat} className="flex flex-col gap-4">
              <div>
                <Label className="t-label mb-1.5 block">Usuario SOL</Label>
                <Input
                  value={sunatUser}
                  onChange={(e) => setSunatUser(e.target.value)}
                  placeholder="MODDATOS"
                  className="mono"
                  required
                />
              </div>
              <div>
                <Label className="t-label mb-1.5 block">Clave SOL</Label>
                <Input
                  type="password"
                  value={sunatPass}
                  onChange={(e) => setSunatPass(e.target.value)}
                  placeholder="••••••••"
                  required
                />
              </div>
              <Button type="submit" disabled={savingCreds}>
                {savingCreds ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Guardando…
                  </>
                ) : (
                  <>
                    <Key className="h-4 w-4 mr-2" /> Guardar credenciales
                  </>
                )}
              </Button>
            </form>
          </Section>

          <Section
            title="Entorno de emisión"
            desc={`Actualmente en ${ENV_LABEL[status?.environment ?? 'beta']}.`}
          >
            <div className="grid grid-cols-2 gap-3 mb-4">
              <Button
                variant={status?.environment === 'beta' ? 'default' : 'outline'}
                onClick={() => handleChangeEnv('beta')}
                disabled={changingEnv || status?.environment === 'beta'}
              >
                <Globe className="h-4 w-4 mr-2" /> Beta
              </Button>
              <Button
                variant={status?.environment === 'production' ? 'default' : 'outline'}
                onClick={() => handleChangeEnv('production')}
                disabled={changingEnv || status?.environment === 'production'}
                style={
                  status?.environment !== 'production'
                    ? {
                        background: 'var(--accent)',
                        color: 'var(--accent-foreground)',
                        fontWeight: 600,
                      }
                    : undefined
                }
              >
                <CheckCircle2 className="h-4 w-4 mr-2" /> Producción
              </Button>
            </div>
            <p className="t-body-sm m-0" style={{ color: 'var(--muted-foreground)' }}>
              Para emitir en producción necesitas certificado válido y credenciales SOL
              configuradas.
            </p>
          </Section>
        </div>

        {/* Help tip */}
        <div
          className="flex items-start gap-2.5 p-4 rounded-[var(--radius-md)]"
          style={{
            background: 'color-mix(in oklch, var(--info) 10%, transparent)',
            border: '1px solid color-mix(in oklch, var(--info) 25%, transparent)',
          }}
        >
          <CircleHelp
            className="h-4 w-4 shrink-0 mt-0.5"
            style={{ color: 'var(--info)' }}
          />
          <p className="t-body-sm m-0">
            ¿No tienes un certificado digital? Lo puedes adquirir con una autoridad certificadora
            autorizada por INDECOPI (RENIEC, LLAMA.PE, Camerfirma, etc.). Es un trámite anual.
          </p>
        </div>
      </div>
    </div>
  );
}
