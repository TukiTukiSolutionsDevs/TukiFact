'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { Building, FileKey, Globe, Upload, Trash2, Shield, Save, Brain, Search, Loader2, CheckCircle2, XCircle, Zap } from 'lucide-react';
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

export default function SettingsPage() {
  const [tenant, setTenant] = useState<TenantInfo | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [certPassword, setCertPassword] = useState('');
  const [serviceConfig, setServiceConfig] = useState<ServiceConfig | null>(null);
  const [providers, setProviders] = useState<ProvidersData | null>(null);
  const [serviceForm, setServiceForm] = useState({
    lookupProvider: 'none', lookupApiKey: '',
    aiProvider: 'none', aiApiKey: '', aiModel: '',
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
      console.error(err);
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
    } catch { /* first time — no config yet */ }
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
      setServiceForm(f => ({ ...f, lookupApiKey: '' }));
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
      const active = data.models.filter(m => m.status === 'active').length;
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
      setServiceForm(f => ({ ...f, aiApiKey: '' }));
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
      toast.error('Ingresa la contraseña del certificado');
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
      <div className="space-y-4">
        {[1, 2, 3].map((i) => (
          <div key={i} className="h-48 bg-muted animate-pulse rounded-lg" />
        ))}
      </div>
    );
  }

  if (!tenant) {
    return <p className="text-muted-foreground">Error cargando datos</p>;
  }

  return (
    <div className="space-y-6 max-w-3xl">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Configuración</h1>
        <p className="text-muted-foreground">Datos de tu empresa y certificado digital</p>
      </div>

      {/* Company Data */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-3">
            <Building className="h-5 w-5 text-blue-600" />
            <div>
              <CardTitle className="text-base">Datos de la Empresa</CardTitle>
              <CardDescription>
                RUC: {tenant.ruc} &mdash; {tenant.razonSocial}
              </CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-muted-foreground">RUC</span>
              <p className="font-mono font-medium">{tenant.ruc}</p>
            </div>
            <div>
              <span className="text-muted-foreground">Razón Social</span>
              <p className="font-medium">{tenant.razonSocial}</p>
            </div>
            <div>
              <span className="text-muted-foreground">Plan</span>
              <p>
                <Badge variant="secondary">{tenant.planName}</Badge> ({tenant.planMaxDocs}{' '}
                docs/mes)
              </p>
            </div>
            <div>
              <span className="text-muted-foreground">Desde</span>
              <p>{new Date(tenant.createdAt).toLocaleDateString('es-PE')}</p>
            </div>
          </div>

          <Separator />

          <div className="space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label>Nombre Comercial</Label>
                <Input
                  value={editForm.nombreComercial}
                  onChange={(e) =>
                    setEditForm((f) => ({ ...f, nombreComercial: e.target.value }))
                  }
                />
              </div>
              <div>
                <Label>Dirección</Label>
                <Input
                  value={editForm.direccion}
                  onChange={(e) => setEditForm((f) => ({ ...f, direccion: e.target.value }))}
                />
              </div>
            </div>
            <div className="grid grid-cols-3 gap-3">
              <div>
                <Label>Departamento</Label>
                <Input
                  value={editForm.departamento}
                  onChange={(e) =>
                    setEditForm((f) => ({ ...f, departamento: e.target.value }))
                  }
                />
              </div>
              <div>
                <Label>Provincia</Label>
                <Input
                  value={editForm.provincia}
                  onChange={(e) => setEditForm((f) => ({ ...f, provincia: e.target.value }))}
                />
              </div>
              <div>
                <Label>Distrito</Label>
                <Input
                  value={editForm.distrito}
                  onChange={(e) => setEditForm((f) => ({ ...f, distrito: e.target.value }))}
                />
              </div>
            </div>
            <Button onClick={handleSave} disabled={isSaving}>
              <Save className="h-4 w-4 mr-2" />
              {isSaving ? 'Guardando...' : 'Guardar Cambios'}
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Digital Certificate */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-3">
            <FileKey className="h-5 w-5 text-amber-600" />
            <div>
              <CardTitle className="text-base">Certificado Digital</CardTitle>
              <CardDescription>
                Requerido para firmar comprobantes electrónicos ante SUNAT
              </CardDescription>
            </div>
            {tenant.hasCertificate && (
              <Badge className="ml-auto bg-green-100 text-green-700">Activo</Badge>
            )}
          </div>
        </CardHeader>
        <CardContent>
          {tenant.hasCertificate ? (
            <div className="space-y-3">
              <div className="flex items-center justify-between p-3 bg-green-50 dark:bg-green-950/20 rounded-lg border border-green-200 dark:border-green-800">
                <div>
                  <p className="text-sm font-medium text-green-700 dark:text-green-400">
                    Certificado configurado
                  </p>
                  {tenant.certificateExpiresAt && (
                    <p className="text-xs text-green-600 dark:text-green-500">
                      Expira:{' '}
                      {new Date(tenant.certificateExpiresAt).toLocaleDateString('es-PE')}
                    </p>
                  )}
                </div>
                <Button
                  variant="outline"
                  size="sm"
                  className="text-red-600"
                  onClick={handleRemoveCert}
                >
                  <Trash2 className="h-4 w-4 mr-1" /> Eliminar
                </Button>
              </div>
            </div>
          ) : (
            <div className="space-y-3">
              <div className="space-y-2">
                <Label>Contraseña del certificado</Label>
                <Input
                  type="password"
                  placeholder="Contraseña del .pfx o .pem"
                  value={certPassword}
                  onChange={(e) => setCertPassword(e.target.value)}
                />
              </div>
              <div>
                <Label>Archivo del certificado</Label>
                <div className="mt-1">
                  <label className="flex items-center justify-center gap-2 p-6 border-2 border-dashed rounded-lg cursor-pointer hover:border-blue-400 hover:bg-blue-50/50 dark:hover:bg-blue-950/20 transition-colors">
                    <Upload className="h-5 w-5 text-muted-foreground" />
                    <span className="text-sm text-muted-foreground">
                      {isUploading ? 'Subiendo...' : 'Haz clic para subir .pfx, .p12 o .pem'}
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
            </div>
          )}
        </CardContent>
      </Card>

      {/* SUNAT Environment */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-3">
            <Globe className="h-5 w-5 text-green-600" />
            <div>
              <CardTitle className="text-base">Entorno SUNAT</CardTitle>
              <CardDescription>Configura la conexión con SUNAT</CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium">Entorno actual</p>
              <p className="text-xs text-muted-foreground">
                {tenant.environment === 'beta'
                  ? 'Los documentos se envían al servidor de pruebas'
                  : 'Envío real a SUNAT producción'}
              </p>
            </div>
            <div className="flex items-center gap-3">
              <Badge variant={tenant.environment === 'production' ? 'default' : 'secondary'}>
                {tenant.environment === 'beta' ? 'Beta (Pruebas)' : 'Producción'}
              </Badge>
              <Button variant="outline" size="sm" onClick={toggleEnvironment}>
                Cambiar a {tenant.environment === 'beta' ? 'Producción' : 'Beta'}
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Security */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-3">
            <Shield className="h-5 w-5 text-purple-600" />
            <div>
              <CardTitle className="text-base">Seguridad</CardTitle>
              <CardDescription>Información de seguridad de tu cuenta</CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="text-sm space-y-2">
          <div className="flex justify-between">
            <span className="text-muted-foreground">Tenant ID</span>
            <span className="font-mono text-xs">{tenant.id}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Credenciales SUNAT</span>
            <Badge variant={tenant.hasSunatCredentials ? 'default' : 'secondary'}>
              {tenant.hasSunatCredentials ? 'Configuradas' : 'No configuradas'}
            </Badge>
          </div>
        </CardContent>
      </Card>

      {/* Lookup Provider — Consulta DNI/RUC */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-3">
            <Search className="h-5 w-5 text-blue-600" />
            <div>
              <CardTitle className="text-base">Consulta DNI / RUC</CardTitle>
              <CardDescription>Autocompleta datos de clientes al emitir comprobantes. Conecta tu proveedor de datos y TukiFact consultará automáticamente.</CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label className="text-sm">Proveedor de datos</Label>
              <select
                className="w-full h-9 rounded-lg border border-input bg-transparent px-3 text-sm"
                value={serviceForm.lookupProvider}
                onChange={e => setServiceForm(f => ({ ...f, lookupProvider: e.target.value }))}
              >
                <option value="none">Seleccionar proveedor...</option>
                {providers?.lookup.map(p => (
                  <option key={p.id} value={p.id}>
                    {p.name} — {p.freeTier}{serviceConfig?.lookupProvider === p.id && serviceConfig?.lookupApiKeyConfigured ? ' ✓ Key cargada' : ''}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-1.5">
              <Label className="text-sm">API Key / Token</Label>
              <Input
                type="password"
                placeholder={serviceConfig?.lookupApiKeyConfigured ? '••••••••••• (ya configurada — escribe para cambiar)' : 'Pega tu API key aquí'}
                value={serviceForm.lookupApiKey}
                onChange={e => setServiceForm(f => ({ ...f, lookupApiKey: e.target.value }))}
              />
            </div>
          </div>
          {serviceForm.lookupProvider !== 'none' && providers?.lookup && (
            <div className="flex items-center justify-between rounded-lg bg-muted/50 p-3">
              <p className="text-xs text-muted-foreground">
                Obtén tu key en{' '}
                <a href={providers.lookup.find(p => p.id === serviceForm.lookupProvider)?.url} target="_blank" rel="noopener" className="text-blue-600 underline font-medium">
                  {providers.lookup.find(p => p.id === serviceForm.lookupProvider)?.url}
                </a>
                {' '}— desde {providers.lookup.find(p => p.id === serviceForm.lookupProvider)?.paidFrom}
              </p>
              {serviceConfig?.lookupApiKeyConfigured && serviceConfig?.lookupProvider === serviceForm.lookupProvider && (
                <Badge variant="default" className="text-xs shrink-0">✓ Activo</Badge>
              )}
            </div>
          )}
          <Button onClick={handleSaveLookup} disabled={isSavingLookup} size="sm">
            <Save className="h-4 w-4 mr-2" />
            {isSavingLookup ? 'Guardando...' : 'Guardar Proveedor de Datos'}
          </Button>
        </CardContent>
      </Card>

      {/* AI Provider — Copiloto IA */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-3">
            <Brain className="h-5 w-5 text-purple-600" />
            <div>
              <CardTitle className="text-base">Asistente IA (Copiloto)</CardTitle>
              <CardDescription>Conecta tu propia cuenta de IA. TukiFact no cobra por esto — usás tu cuenta de Gemini, Claude, Grok, DeepSeek u OpenAI.</CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="space-y-1.5">
              <Label className="text-sm">Proveedor de IA</Label>
              <select
                className="w-full h-9 rounded-lg border border-input bg-transparent px-3 text-sm"
                value={serviceForm.aiProvider}
                onChange={e => {
                  const newProvider = e.target.value;
                  const providerModels = providers?.ai.find(p => p.id === newProvider)?.models;
                  setServiceForm(f => ({
                    ...f,
                    aiProvider: newProvider,
                    aiModel: providerModels?.[0] || '',
                  }));
                }}
              >
                <option value="none">Seleccionar proveedor...</option>
                {providers?.ai.map(p => (
                  <option key={p.id} value={p.id}>
                    {p.name}{serviceConfig?.aiProvider === p.id && serviceConfig?.aiApiKeyConfigured ? ' ✓ Key cargada' : ''}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-1.5">
              <Label className="text-sm">Modelo</Label>
              <select
                className="w-full h-9 rounded-lg border border-input bg-transparent px-3 text-sm"
                value={serviceForm.aiModel}
                onChange={e => setServiceForm(f => ({ ...f, aiModel: e.target.value }))}
                disabled={serviceForm.aiProvider === 'none'}
              >
                <option value="">Seleccionar modelo...</option>
                {providers?.ai.find(p => p.id === serviceForm.aiProvider)?.models?.map(m => (
                  <option key={m} value={m}>{m}</option>
                ))}
              </select>
            </div>
            <div className="space-y-1.5">
              <Label className="text-sm">API Key</Label>
              <Input
                type="password"
                placeholder={serviceConfig?.aiApiKeyConfigured ? '••••••••••• (ya configurada)' : 'Pega tu API key aquí'}
                value={serviceForm.aiApiKey}
                onChange={e => setServiceForm(f => ({ ...f, aiApiKey: e.target.value }))}
                disabled={serviceForm.aiProvider === 'none'}
              />
            </div>
          </div>
          {serviceConfig?.aiApiKeyConfigured && serviceConfig?.aiProvider === serviceForm.aiProvider && serviceForm.aiProvider !== 'none' && (
            <div className="flex items-center gap-2 rounded-lg bg-muted/50 p-3">
              <Badge variant="default" className="text-xs">✓ Activo</Badge>
              <p className="text-xs text-muted-foreground">
                Usando <span className="font-medium">{providers?.ai.find(p => p.id === serviceConfig.aiProvider)?.name}</span> con modelo <span className="font-medium">{serviceConfig.aiModel}</span>
              </p>
            </div>
          )}
          <div className="flex gap-2">
            <Button onClick={handleSaveAi} disabled={isSavingAi} size="sm">
              <Save className="h-4 w-4 mr-2" />
              {isSavingAi ? 'Guardando...' : 'Guardar Proveedor de IA'}
            </Button>
            {serviceConfig?.aiApiKeyConfigured && (
              <Button onClick={handleTestAi} disabled={isTestingAi} variant="outline" size="sm">
                {isTestingAi ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <Zap className="h-4 w-4 mr-2" />}
                {isTestingAi ? 'Testeando modelos...' : 'Test de Key'}
              </Button>
            )}
          </div>

          {/* Model Status Table */}
          {aiTestResults && (
            <div className="rounded-lg border overflow-hidden">
              <div className="bg-muted/50 px-3 py-2 border-b">
                <p className="text-xs font-medium">Estado de modelos — {providers?.ai.find(p => p.id === serviceConfig?.aiProvider)?.name}</p>
              </div>
              <div className="divide-y">
                {aiTestResults.map(m => (
                  <div key={m.model} className="flex items-center justify-between px-3 py-2">
                    <div className="flex items-center gap-2">
                      {m.status === 'active' ? (
                        <CheckCircle2 className="h-4 w-4 text-green-500" />
                      ) : (
                        <XCircle className="h-4 w-4 text-red-500" />
                      )}
                      <span className="text-sm font-mono">{m.model}</span>
                    </div>
                    <Badge variant={m.status === 'active' ? 'default' : 'destructive'} className="text-[10px]">
                      {m.status === 'active' ? 'Activo' : 'Error'}
                    </Badge>
                  </div>
                ))}
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
