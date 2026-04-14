'use client';

import { useEffect, useState, useRef } from 'react';
import { api } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import {
  Shield, Upload, Trash2, CheckCircle, AlertTriangle,
  XCircle, Loader2, Key, Globe,
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
    } catch (err) { console.error(err); }
    finally { setLoading(false); }
  };

  useEffect(() => { fetchStatus(); }, []);

  const handleUpload = async () => {
    const file = fileRef.current?.files?.[0];
    if (!file) { toast.error('Seleccioná un archivo .pfx o .p12'); return; }
    if (!password) { toast.error('Ingresá la contraseña del certificado'); return; }

    setUploading(true);
    try {
      const formData = new FormData();
      formData.append('file', file);
      formData.append('password', password);

      const res = await fetch('/v1/certificate/upload', {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${localStorage.getItem('access_token')}` },
        body: formData,
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.error || 'Error subiendo certificado');

      toast.success(data.message);
      setPassword('');
      if (fileRef.current) fileRef.current.value = '';
      fetchStatus();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    } finally { setUploading(false); }
  };

  const handleDelete = async () => {
    if (!confirm('Eliminar certificado digital?')) return;
    try {
      await api.delete('/v1/certificate');
      toast.success('Certificado eliminado');
      fetchStatus();
    } catch (err) { toast.error(err instanceof Error ? err.message : 'Error'); }
  };

  const handleSaveSunat = async (e: React.FormEvent) => {
    e.preventDefault();
    setSavingCreds(true);
    try {
      await api.put('/v1/certificate/sunat-credentials', { sunatUser, sunatPassword: sunatPass });
      toast.success('Credenciales SUNAT guardadas');
      fetchStatus();
    } catch (err) { toast.error(err instanceof Error ? err.message : 'Error'); }
    finally { setSavingCreds(false); }
  };

  const handleChangeEnv = async (env: string) => {
    setChangingEnv(true);
    try {
      await api.put('/v1/certificate/environment', { environment: env });
      toast.success(`Entorno cambiado a ${env}`);
      fetchStatus();
    } catch (err) { toast.error(err instanceof Error ? err.message : 'Error'); }
    finally { setChangingEnv(false); }
  };

  if (loading) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">Certificado Digital</h1>
        <div className="animate-pulse space-y-4">
          <div className="h-32 bg-muted rounded-lg" />
          <div className="h-48 bg-muted rounded-lg" />
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Certificado Digital & SUNAT</h1>
        <p className="text-sm text-muted-foreground">Configurá tu certificado digital y credenciales SUNAT</p>
      </div>

      {/* Certificate Status */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Shield className="h-5 w-5" /> Estado del Certificado
          </CardTitle>
        </CardHeader>
        <CardContent>
          {status?.hasCertificate ? (
            <div className="space-y-4">
              <div className="flex items-center gap-3 rounded-lg border p-4">
                {status.isExpired ? (
                  <XCircle className="h-8 w-8 text-destructive shrink-0" />
                ) : status.daysUntilExpiry && status.daysUntilExpiry < 30 ? (
                  <AlertTriangle className="h-8 w-8 text-amber-500 shrink-0" />
                ) : (
                  <CheckCircle className="h-8 w-8 text-emerald-500 shrink-0" />
                )}
                <div>
                  <p className="font-medium">
                    {status.isExpired ? 'Certificado EXPIRADO' :
                     status.daysUntilExpiry && status.daysUntilExpiry < 30 ? 'Certificado por expirar' :
                     'Certificado válido'}
                  </p>
                  <p className="text-sm text-muted-foreground">
                    {status.expiresAt ? `Expira: ${new Date(status.expiresAt).toLocaleDateString('es-PE')}` : ''}
                    {status.daysUntilExpiry !== null ? ` (${status.daysUntilExpiry} días)` : ''}
                  </p>
                </div>
                <Button variant="outline" size="sm" onClick={handleDelete} className="ml-auto text-destructive">
                  <Trash2 className="h-4 w-4 mr-1" /> Eliminar
                </Button>
              </div>
            </div>
          ) : (
            <div className="space-y-4">
              <div className="flex items-center gap-3 rounded-lg border border-dashed p-4 text-muted-foreground">
                <Shield className="h-8 w-8 opacity-50" />
                <div>
                  <p className="font-medium text-foreground">Sin certificado</p>
                  <p className="text-sm">Subí tu archivo .pfx o .p12 para firmar comprobantes</p>
                </div>
              </div>

              <div className="space-y-3">
                <div className="space-y-1">
                  <Label>Archivo (.pfx / .p12)</Label>
                  <Input type="file" accept=".pfx,.p12" ref={fileRef} />
                </div>
                <div className="space-y-1">
                  <Label>Contraseña del certificado</Label>
                  <Input type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="Ingresá la contraseña" />
                </div>
                <Button onClick={handleUpload} disabled={uploading} className="w-full">
                  {uploading ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <Upload className="h-4 w-4 mr-2" />}
                  {uploading ? 'Subiendo...' : 'Subir Certificado'}
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* SUNAT Credentials */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Key className="h-5 w-5" /> Credenciales SUNAT
            </CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSaveSunat} className="space-y-3">
              <div className="flex items-center gap-2 mb-2">
                <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
                  status?.hasSunatCredentials ? 'bg-emerald-50 dark:bg-emerald-950 text-emerald-700 dark:text-emerald-300' : 'bg-amber-50 dark:bg-amber-950 text-amber-700 dark:text-amber-300'
                }`}>
                  {status?.hasSunatCredentials ? 'Configuradas' : 'No configuradas'}
                </span>
              </div>
              <div className="space-y-1">
                <Label>Usuario SOL</Label>
                <Input value={sunatUser} onChange={(e) => setSunatUser(e.target.value)} placeholder="MODDATOS" required />
              </div>
              <div className="space-y-1">
                <Label>Clave SOL</Label>
                <Input type="password" value={sunatPass} onChange={(e) => setSunatPass(e.target.value)} placeholder="••••••" required />
              </div>
              <Button type="submit" className="w-full" disabled={savingCreds}>
                {savingCreds ? 'Guardando...' : 'Guardar Credenciales'}
              </Button>
            </form>
          </CardContent>
        </Card>

        {/* Environment */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Globe className="h-5 w-5" /> Entorno de Emisión
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-sm text-muted-foreground">
              Actualmente en: <span className="font-bold text-foreground capitalize">{status?.environment}</span>
            </p>
            <div className="grid grid-cols-2 gap-3">
              <Button
                variant={status?.environment === 'beta' ? 'default' : 'outline'}
                onClick={() => handleChangeEnv('beta')}
                disabled={changingEnv || status?.environment === 'beta'}
                className="w-full"
              >
                Beta (Pruebas)
              </Button>
              <Button
                variant={status?.environment === 'production' ? 'default' : 'outline'}
                onClick={() => handleChangeEnv('production')}
                disabled={changingEnv || status?.environment === 'production'}
                className="w-full"
              >
                Producción
              </Button>
            </div>
            <p className="text-xs text-muted-foreground">
              Para cambiar a producción necesitás certificado digital y credenciales SUNAT configuradas.
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
