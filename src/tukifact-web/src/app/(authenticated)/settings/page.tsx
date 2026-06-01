'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Section } from '@/components/ui/section';
import { StatusBadge } from '@/components/ui/status-badge';
import {
  Building,
  FileKey,
  Globe,
  Upload,
  Trash2,
  Shield,
  Save,
  Brain,
  Search,
  Loader2,
  CheckCircle2,
  XCircle,
  Zap,
  AlertCircle,
} from 'lucide-react';
import { toast } from 'sonner';

interface ServiceConfig {
  lookupProvider: string;
  lookupApiKeyConfigured: boolean;
  aiProvider: string;
  aiApiKeyConfigured: boolean;
  aiModel: string | null;
}

interface ProviderInfo {
  id: string;
  name: string;
  url?: string;
  freeTier?: string;
  paidFrom?: string;
  models?: string[];
}

interface ProvidersData {
  lookup: ProviderInfo[];
  ai: ProviderInfo[];
}

interface ModelTestResult {
  model: string;
  status: 'active' | 'error';
  response: string;
}

interface AiTestResponse {
  provider: string;
  models: ModelTestResult[];
}

interface TenantInfo {
  id: string;
  ruc: string;
  razonSocial: string;
  nombreComercial: string | null;
  direccion: string | null;
  departamento: string | null;
  provincia: string | null;
  distrito: string | null;
  environment: string;
  planName: string;
  planMaxDocs: number;
  hasCertificate: boolean;
  certificateExpiresAt: string | null;
  hasSunatCredentials: boolean;
  primaryColor: string;
  createdAt: string;
}

function SectionIcon({ icon: Icon, color }: { icon: React.ElementType; color: string }) {
  return (
    <span
      className="flex h-10 w-10 items-center justify-center rounded-xl shrink-0"
      style={{
        background: `color-mix(in oklch, ${color} 14%, transparent)`,
        color,
      }}
    >
      <Icon className="h-5 w-5" />
    </span>
  );
}

function SelectField({
  value,
  onChange,
  children,
  disabled,
  id,
}: {
  value: string;
  onChange: (v: string) => void;
  children: React.ReactNode;
  disabled?: boolean;
  id?: string;
}) {
  return (
    <select
      id={id}
      className="w-full h-10 rounded-[var(--radius-md)] border px-3 t-body-sm transition-colors"
      style={{
        background: 'var(--card)',
        borderColor: 'var(--border)',
        color: 'var(--foreground)',
      }}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      disabled={disabled}
    >
      {children}
    </select>
  );
}

function fieldLabel(text: string) {
  return (
    <Label className="t-overline mb-1.5 block" style={{ color: 'var(--muted-foreground)' }}>
      {text}
    </Label>
  );
}

export default function SettingsPage() {
  const [tenant, setTenant] = useState<TenantInfo | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [certPassword, setCertPassword] = useState('');
  const [serviceConfig, setServiceConfig] = useState<ServiceConfig | null>(null);
  const [providers, setProviders] = useState<ProvidersData | null>(null);
  const [serviceForm, setServiceForm] = useState({
    lookupProvider: 'none',
    lookupApiKey: '',
    aiProvider: 'none',
    aiApiKey: '',
    aiModel: '',
  });
  const [isSavingLookup, setIsSavingLookup] = useState(false);
  const [isSavingAi, setIsSavingAi] = useState(false);
  const [isTestingAi, setIsTestingAi] = useState(false);
  const [aiTestResults, setAiTestResults] = useState<ModelTestResult[] | null>(null);
  const [editForm, setEditForm] = useState({
    nombreComercial: '',
    direccion: '',
    departamento: '',
    provincia: '',
    distrito: '',
  });

  const fetchTenant = async () => {
    try {
      const data = await api.get<TenantInfo>('/v1/tenant');
      setTenant(data);
      setEditForm({
        nombreComercial: data.nombreComercial || '',
        direccion: data.direccion || '',
        departamento: data.departamento || '',
        provincia: data.provincia || '',
        distrito: data.distrito || '',
      });
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al cargar tenant');
    } finally {
      setIsLoading(false);
    }
  };

  const fetchServiceConfig = async () => {
    try {
      const [config, provs] = await Promise.all([
        api.get<ServiceConfig>('/v1/services/config'),
        api.get<ProvidersData>('/v1/services/providers'),
      ]);
      setServiceConfig(config);
      setProviders(provs);
      setServiceForm({
        lookupProvider: config.lookupProvider,
        lookupApiKey: '',
        aiProvider: config.aiProvider,
        aiApiKey: '',
        aiModel: config.aiModel || '',
      });
    } catch {
      /* primera vez sin config */
    }
  };

  useEffect(() => {
    fetchTenant();
    fetchServiceConfig();
  }, []);

  const handleSaveLookup = async () => {
    setIsSavingLookup(true);
    try {
      await api.put('/v1/services/config', {
        lookupProvider: serviceForm.lookupProvider,
        ...(serviceForm.lookupApiKey ? { lookupApiKey: serviceForm.lookupApiKey } : {}),
      });
      toast.success('Proveedor de datos guardado');
      setServiceForm((f) => ({ ...f, lookupApiKey: '' }));
      fetchServiceConfig();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    } finally {
      setIsSavingLookup(false);
    }
  };

  const handleTestAi = async () => {
    setIsTestingAi(true);
    setAiTestResults(null);
    try {
      const data = await api.post<AiTestResponse>('/v1/services/ai/test', {});
      setAiTestResults(data.models);
      const active = data.models.filter((m) => m.status === 'active').length;
      toast.success(`Test completado: ${active}/${data.models.length} modelos activos`);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al testear');
    } finally {
      setIsTestingAi(false);
    }
  };

  const handleSaveAi = async () => {
    setIsSavingAi(true);
    try {
      await api.put('/v1/services/config', {
        aiProvider: serviceForm.aiProvider,
        ...(serviceForm.aiApiKey ? { aiApiKey: serviceForm.aiApiKey } : {}),
        ...(serviceForm.aiModel ? { aiModel: serviceForm.aiModel } : {}),
      });
      toast.success('Proveedor de IA guardado');
      setServiceForm((f) => ({ ...f, aiApiKey: '' }));
      fetchServiceConfig();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    } finally {
      setIsSavingAi(false);
    }
  };

  const handleSave = async () => {
    setIsSaving(true);
    try {
      await api.put('/v1/tenant', editForm);
      toast.success('Datos actualizados');
      fetchTenant();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    } finally {
      setIsSaving(false);
    }
  };

  const handleCertUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (!certPassword) {
      toast.error('Ingresá la contraseña del certificado');
      return;
    }
    setIsUploading(true);
    try {
      const formData = new FormData();
      formData.append('file', file);
      formData.append('password', certPassword);
      const token = api.getToken();
      const res = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL || ''}/v1/tenant/certificate`,
        {
          method: 'POST',
          headers: { Authorization: `Bearer ${token}` },
          body: formData,
        }
      );
      const data = await res.json();
      if (!res.ok) throw new Error(data.error || 'Error');
      toast.success(`Certificado cargado: ${data.subject}`);
      setCertPassword('');
      fetchTenant();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al cargar certificado');
    } finally {
      setIsUploading(false);
      e.target.value = '';
    }
  };

  const handleRemoveCert = async () => {
    try {
      await api.delete('/v1/tenant/certificate');
      toast.success('Certificado eliminado');
      fetchTenant();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    }
  };

  const toggleEnvironment = async () => {
    if (!tenant) return;
    const newEnv = tenant.environment === 'beta' ? 'production' : 'beta';
    try {
      await api.put('/v1/tenant/environment', { environment: newEnv });
      toast.success(`Entorno cambiado a ${newEnv}`);
      fetchTenant();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-[var(--gap-cards,1.5rem)] max-w-3xl">
        {[1, 2, 3].map((i) => (
          <div
            key={i}
            className="h-48 rounded-[var(--radius-lg)] animate-pulse"
            style={{ background: 'var(--muted)' }}
          />
        ))}
      </div>
    );
  }

  if (!tenant) {
    return (
      <Section>
        <p className="t-body-sm" style={{ color: 'var(--muted-foreground)' }}>
          Error cargando datos.
        </p>
      </Section>
    );
  }

  const lookupProviderInfo = providers?.lookup.find((p) => p.id === serviceForm.lookupProvider);
  const aiProviderInfo = providers?.ai.find((p) => p.id === serviceForm.aiProvider);
  const aiConfiguredInfo = providers?.ai.find((p) => p.id === serviceConfig?.aiProvider);

  return (
    <div className="space-y-[var(--gap-cards,1.5rem)] max-w-3xl">
      <header>
        <h1 className="t-display-lg m-0">Empresa</h1>
        <p className="t-body-sm m-0 mt-1" style={{ color: 'var(--muted-foreground)' }}>
          Datos fiscales, certificado, entorno SUNAT y servicios externos.
        </p>
      </header>

      <Section
        title="Datos de la empresa"
        desc={`RUC ${tenant.ruc} — ${tenant.razonSocial}`}
      >
        <div className="flex items-start gap-3 mb-5">
          <SectionIcon icon={Building} color="var(--info)" />
          <div className="grid grid-cols-2 gap-x-6 gap-y-2 flex-1 t-body-sm">
            <div>
              <span className="t-overline" style={{ color: 'var(--muted-foreground)' }}>
                RUC
              </span>
              <p className="m-0 mt-0.5 mono tnum font-medium">{tenant.ruc}</p>
            </div>
            <div>
              <span className="t-overline" style={{ color: 'var(--muted-foreground)' }}>
                Razón social
              </span>
              <p className="m-0 mt-0.5 font-medium">{tenant.razonSocial}</p>
            </div>
            <div>
              <span className="t-overline" style={{ color: 'var(--muted-foreground)' }}>
                Plan
              </span>
              <p className="m-0 mt-0.5">
                <span
                  className="inline-flex items-center rounded-full px-2 py-0.5 t-caption font-semibold"
                  style={{
                    color: 'var(--accent)',
                    background: 'color-mix(in oklch, var(--accent) 14%, transparent)',
                  }}
                >
                  {tenant.planName}
                </span>{' '}
                <span style={{ color: 'var(--muted-foreground)' }}>
                  ({tenant.planMaxDocs} docs/mes)
                </span>
              </p>
            </div>
            <div>
              <span className="t-overline" style={{ color: 'var(--muted-foreground)' }}>
                Desde
              </span>
              <p className="m-0 mt-0.5 tnum">
                {new Date(tenant.createdAt).toLocaleDateString('es-PE')}
              </p>
            </div>
          </div>
        </div>

        <div
          className="border-t pt-5 space-y-3"
          style={{ borderColor: 'var(--border)' }}
        >
          <div className="grid grid-cols-2 gap-3">
            <div>
              {fieldLabel('Nombre comercial')}
              <Input
                value={editForm.nombreComercial}
                onChange={(e) =>
                  setEditForm((f) => ({ ...f, nombreComercial: e.target.value }))
                }
              />
            </div>
            <div>
              {fieldLabel('Dirección')}
              <Input
                value={editForm.direccion}
                onChange={(e) => setEditForm((f) => ({ ...f, direccion: e.target.value }))}
              />
            </div>
          </div>
          <div className="grid grid-cols-3 gap-3">
            <div>
              {fieldLabel('Departamento')}
              <Input
                value={editForm.departamento}
                onChange={(e) =>
                  setEditForm((f) => ({ ...f, departamento: e.target.value }))
                }
              />
            </div>
            <div>
              {fieldLabel('Provincia')}
              <Input
                value={editForm.provincia}
                onChange={(e) => setEditForm((f) => ({ ...f, provincia: e.target.value }))}
              />
            </div>
            <div>
              {fieldLabel('Distrito')}
              <Input
                value={editForm.distrito}
                onChange={(e) => setEditForm((f) => ({ ...f, distrito: e.target.value }))}
              />
            </div>
          </div>
          <Button onClick={handleSave} disabled={isSaving}>
            <Save className="h-4 w-4 mr-2" />
            {isSaving ? 'Guardando…' : 'Guardar cambios'}
          </Button>
        </div>
      </Section>

      <Section
        title="Certificado digital"
        desc="Requerido para firmar comprobantes electrónicos ante SUNAT."
        right={tenant.hasCertificate ? <StatusBadge status="active" /> : null}
      >
        <div className="flex items-start gap-3">
          <SectionIcon icon={FileKey} color="var(--warning)" />
          <div className="flex-1">
            {tenant.hasCertificate ? (
              <div
                className="flex items-center justify-between rounded-[var(--radius-md)] p-3"
                style={{
                  background: 'color-mix(in oklch, var(--success) 10%, transparent)',
                  border:
                    '1px solid color-mix(in oklch, var(--success) 30%, transparent)',
                }}
              >
                <div>
                  <p
                    className="t-body-sm font-medium m-0"
                    style={{ color: 'var(--success)' }}
                  >
                    Certificado configurado
                  </p>
                  {tenant.certificateExpiresAt && (
                    <p className="t-caption m-0 mt-0.5 tnum">
                      Expira:{' '}
                      {new Date(tenant.certificateExpiresAt).toLocaleDateString('es-PE')}
                    </p>
                  )}
                </div>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleRemoveCert}
                  style={{ color: 'var(--danger)' }}
                >
                  <Trash2 className="h-4 w-4 mr-1" /> Eliminar
                </Button>
              </div>
            ) : (
              <div className="space-y-3">
                <div>
                  {fieldLabel('Contraseña del certificado')}
                  <Input
                    type="password"
                    placeholder="Contraseña del .pfx o .pem"
                    value={certPassword}
                    onChange={(e) => setCertPassword(e.target.value)}
                  />
                </div>
                <div>
                  {fieldLabel('Archivo')}
                  <label
                    className="flex items-center justify-center gap-2 p-6 rounded-[var(--radius-md)] border-2 border-dashed cursor-pointer transition-colors"
                    style={{ borderColor: 'var(--border)' }}
                  >
                    <Upload
                      className="h-5 w-5"
                      style={{ color: 'var(--muted-foreground)' }}
                    />
                    <span
                      className="t-body-sm"
                      style={{ color: 'var(--muted-foreground)' }}
                    >
                      {isUploading
                        ? 'Subiendo…'
                        : 'Hacé click para subir .pfx, .p12 o .pem'}
                    </span>
                    <input
                      type="file"
                      className="hidden"
                      accept=".pfx,.p12,.pem"
                      onChange={handleCertUpload}
                      disabled={isUploading || !certPassword}
                    />
                  </label>
                </div>
              </div>
            )}
          </div>
        </div>
      </Section>

      <Section
        title="Entorno SUNAT"
        desc="Beta envía a servidor de pruebas. Producción emite con valor fiscal."
      >
        <div className="flex items-start gap-3">
          <SectionIcon icon={Globe} color="var(--success)" />
          <div className="flex-1 flex items-center justify-between gap-3">
            <div>
              <p className="t-body-sm font-medium m-0">Entorno actual</p>
              <p
                className="t-caption m-0 mt-0.5"
                style={{ color: 'var(--muted-foreground)' }}
              >
                {tenant.environment === 'beta'
                  ? 'Los documentos se envían al servidor SUNAT de pruebas.'
                  : 'Emisión real a SUNAT producción.'}
              </p>
            </div>
            <div className="flex items-center gap-3 shrink-0">
              <span
                className="inline-flex items-center rounded-full px-2.5 py-0.5 t-caption font-semibold"
                style={{
                  color:
                    tenant.environment === 'production'
                      ? 'var(--success)'
                      : 'var(--warning)',
                  background:
                    tenant.environment === 'production'
                      ? 'color-mix(in oklch, var(--success) 14%, transparent)'
                      : 'color-mix(in oklch, var(--warning) 14%, transparent)',
                }}
              >
                {tenant.environment === 'beta' ? 'Beta (pruebas)' : 'Producción'}
              </span>
              <Button variant="outline" size="sm" onClick={toggleEnvironment}>
                Cambiar a {tenant.environment === 'beta' ? 'Producción' : 'Beta'}
              </Button>
            </div>
          </div>
        </div>
      </Section>

      <Section title="Seguridad" desc="Identificadores y credenciales del tenant.">
        <div className="flex items-start gap-3">
          <SectionIcon icon={Shield} color="oklch(0.6 0.18 295)" />
          <div className="flex-1 t-body-sm space-y-2">
            <div className="flex items-center justify-between">
              <span style={{ color: 'var(--muted-foreground)' }}>Tenant ID</span>
              <span className="mono tnum t-caption">{tenant.id}</span>
            </div>
            <div className="flex items-center justify-between">
              <span style={{ color: 'var(--muted-foreground)' }}>
                Credenciales SUNAT
              </span>
              {tenant.hasSunatCredentials ? (
                <StatusBadge status="active" label="Configuradas" />
              ) : (
                <span
                  className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 t-caption font-semibold"
                  style={{
                    color: 'var(--slate-500)',
                    background: 'color-mix(in oklch, var(--slate-500) 14%, transparent)',
                  }}
                >
                  No configuradas
                </span>
              )}
            </div>
          </div>
        </div>
      </Section>

      <Section
        title="Consulta DNI / RUC"
        desc="Autocompletá datos de clientes al emitir comprobantes con un proveedor externo."
      >
        <div className="flex items-start gap-3">
          <SectionIcon icon={Search} color="var(--info)" />
          <div className="flex-1 space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div>
                {fieldLabel('Proveedor de datos')}
                <SelectField
                  value={serviceForm.lookupProvider}
                  onChange={(v) =>
                    setServiceForm((f) => ({ ...f, lookupProvider: v }))
                  }
                >
                  <option value="none">Seleccionar proveedor…</option>
                  {providers?.lookup.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.name} — {p.freeTier}
                      {serviceConfig?.lookupProvider === p.id &&
                      serviceConfig?.lookupApiKeyConfigured
                        ? ' ✓ Key cargada'
                        : ''}
                    </option>
                  ))}
                </SelectField>
              </div>
              <div>
                {fieldLabel('API Key / Token')}
                <Input
                  type="password"
                  placeholder={
                    serviceConfig?.lookupApiKeyConfigured
                      ? '••••••••• (ya configurada — escribí para cambiar)'
                      : 'Pegá tu API key acá'
                  }
                  value={serviceForm.lookupApiKey}
                  onChange={(e) =>
                    setServiceForm((f) => ({ ...f, lookupApiKey: e.target.value }))
                  }
                />
              </div>
            </div>
            {serviceForm.lookupProvider !== 'none' && lookupProviderInfo && (
              <div
                className="flex items-center justify-between rounded-[var(--radius-md)] p-3 gap-3"
                style={{ background: 'var(--muted)' }}
              >
                <p
                  className="t-caption m-0"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  Obtené tu key en{' '}
                  <a
                    href={lookupProviderInfo.url}
                    target="_blank"
                    rel="noopener"
                    className="underline font-medium"
                    style={{ color: 'var(--info)' }}
                  >
                    {lookupProviderInfo.url}
                  </a>{' '}
                  — desde {lookupProviderInfo.paidFrom}
                </p>
                {serviceConfig?.lookupApiKeyConfigured &&
                  serviceConfig?.lookupProvider === serviceForm.lookupProvider && (
                    <StatusBadge status="active" />
                  )}
              </div>
            )}
            <Button onClick={handleSaveLookup} disabled={isSavingLookup} size="sm">
              <Save className="h-4 w-4 mr-2" />
              {isSavingLookup ? 'Guardando…' : 'Guardar proveedor de datos'}
            </Button>
          </div>
        </div>
      </Section>

      <Section
        title="Asistente IA (copiloto)"
        desc="Conectá tu cuenta de Gemini, Claude, Grok, DeepSeek u OpenAI. TukiFact no cobra extra."
      >
        <div className="flex items-start gap-3">
          <SectionIcon icon={Brain} color="oklch(0.6 0.18 295)" />
          <div className="flex-1 space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
              <div>
                {fieldLabel('Proveedor IA')}
                <SelectField
                  value={serviceForm.aiProvider}
                  onChange={(v) => {
                    const providerModels = providers?.ai.find((p) => p.id === v)?.models;
                    setServiceForm((f) => ({
                      ...f,
                      aiProvider: v,
                      aiModel: providerModels?.[0] || '',
                    }));
                  }}
                >
                  <option value="none">Seleccionar proveedor…</option>
                  {providers?.ai.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.name}
                      {serviceConfig?.aiProvider === p.id &&
                      serviceConfig?.aiApiKeyConfigured
                        ? ' ✓ Key cargada'
                        : ''}
                    </option>
                  ))}
                </SelectField>
              </div>
              <div>
                {fieldLabel('Modelo')}
                <SelectField
                  value={serviceForm.aiModel}
                  onChange={(v) => setServiceForm((f) => ({ ...f, aiModel: v }))}
                  disabled={serviceForm.aiProvider === 'none'}
                >
                  <option value="">Seleccionar modelo…</option>
                  {aiProviderInfo?.models?.map((m) => (
                    <option key={m} value={m}>
                      {m}
                    </option>
                  ))}
                </SelectField>
              </div>
              <div>
                {fieldLabel('API Key')}
                <Input
                  type="password"
                  placeholder={
                    serviceConfig?.aiApiKeyConfigured
                      ? '••••••••• (ya configurada)'
                      : 'Pegá tu API key acá'
                  }
                  value={serviceForm.aiApiKey}
                  onChange={(e) =>
                    setServiceForm((f) => ({ ...f, aiApiKey: e.target.value }))
                  }
                  disabled={serviceForm.aiProvider === 'none'}
                />
              </div>
            </div>
            {serviceConfig?.aiApiKeyConfigured &&
              serviceConfig?.aiProvider === serviceForm.aiProvider &&
              serviceForm.aiProvider !== 'none' && (
                <div
                  className="flex items-center gap-2 rounded-[var(--radius-md)] p-3"
                  style={{ background: 'var(--muted)' }}
                >
                  <StatusBadge status="active" />
                  <p
                    className="t-caption m-0"
                    style={{ color: 'var(--muted-foreground)' }}
                  >
                    Usando{' '}
                    <span style={{ color: 'var(--foreground)' }} className="font-medium">
                      {aiConfiguredInfo?.name}
                    </span>{' '}
                    con modelo{' '}
                    <span
                      className="mono font-medium"
                      style={{ color: 'var(--foreground)' }}
                    >
                      {serviceConfig.aiModel}
                    </span>
                  </p>
                </div>
              )}
            <div className="flex gap-2 flex-wrap">
              <Button onClick={handleSaveAi} disabled={isSavingAi} size="sm">
                <Save className="h-4 w-4 mr-2" />
                {isSavingAi ? 'Guardando…' : 'Guardar proveedor de IA'}
              </Button>
              {serviceConfig?.aiApiKeyConfigured && (
                <Button
                  onClick={handleTestAi}
                  disabled={isTestingAi}
                  variant="outline"
                  size="sm"
                >
                  {isTestingAi ? (
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  ) : (
                    <Zap className="h-4 w-4 mr-2" />
                  )}
                  {isTestingAi ? 'Testeando modelos…' : 'Test de Key'}
                </Button>
              )}
            </div>

            {aiTestResults && (
              <div
                className="rounded-[var(--radius-md)] border overflow-hidden"
                style={{ borderColor: 'var(--border)' }}
              >
                <div
                  className="px-3 py-2 border-b"
                  style={{
                    background: 'var(--muted)',
                    borderColor: 'var(--border)',
                  }}
                >
                  <p
                    className="t-caption font-semibold m-0"
                    style={{ color: 'var(--muted-foreground)' }}
                  >
                    Estado de modelos — {aiConfiguredInfo?.name}
                  </p>
                </div>
                <div className="divide-y" style={{ borderColor: 'var(--border)' }}>
                  {aiTestResults.map((m) => (
                    <div
                      key={m.model}
                      className="flex items-center justify-between px-3 py-2"
                      style={{ borderColor: 'var(--border)' }}
                    >
                      <div className="flex items-center gap-2">
                        {m.status === 'active' ? (
                          <CheckCircle2
                            className="h-4 w-4"
                            style={{ color: 'var(--success)' }}
                          />
                        ) : (
                          <XCircle
                            className="h-4 w-4"
                            style={{ color: 'var(--danger)' }}
                          />
                        )}
                        <span className="t-body-sm mono">{m.model}</span>
                      </div>
                      <span
                        className="inline-flex items-center rounded-full px-2 py-0.5 t-caption font-semibold"
                        style={{
                          color:
                            m.status === 'active' ? 'var(--success)' : 'var(--danger)',
                          background: `color-mix(in oklch, ${
                            m.status === 'active' ? 'var(--success)' : 'var(--danger)'
                          } 14%, transparent)`,
                        }}
                      >
                        {m.status === 'active' ? 'Activo' : 'Error'}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            )}

            <div
              className="flex items-start gap-2 rounded-[var(--radius-md)] p-3 t-caption"
              style={{
                background: 'color-mix(in oklch, var(--info) 8%, transparent)',
                border: '1px solid color-mix(in oklch, var(--info) 25%, transparent)',
              }}
            >
              <AlertCircle
                className="h-3.5 w-3.5 shrink-0 mt-0.5"
                style={{ color: 'var(--info)' }}
              />
              <p className="m-0" style={{ color: 'var(--muted-foreground)' }}>
                Tu API key se guarda cifrada y solo se usa para hacer las llamadas en tu
                nombre.
              </p>
            </div>
          </div>
        </div>
      </Section>
    </div>
  );
}
