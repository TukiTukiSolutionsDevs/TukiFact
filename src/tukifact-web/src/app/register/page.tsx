'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import Image from 'next/image';
import { useAuth } from '@/lib/auth-context';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { toast } from 'sonner';

export default function RegisterPage() {
  const { register } = useAuth();
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(false);
  const [form, setForm] = useState({
    ruc: '',
    razonSocial: '',
    nombreComercial: '',
    direccion: '',
    adminEmail: '',
    adminPassword: '',
    adminFullName: '',
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (form.ruc.length !== 11) {
      toast.error('RUC debe tener 11 dígitos');
      return;
    }
    setIsLoading(true);
    try {
      await register(form);
      toast.success('Empresa registrada exitosamente');
      router.push('/dashboard');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error al registrar');
    } finally {
      setIsLoading(false);
    }
  };

  const update =
    (field: string) => (e: React.ChangeEvent<HTMLInputElement>) =>
      setForm((f) => ({ ...f, [field]: e.target.value }));

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100 px-4 py-8">
      <Card className="w-full max-w-lg">
        <CardHeader className="text-center">
          <Image src="/logo.png" alt="TukiFact" width={200} height={60} className="mx-auto mb-4 object-contain" />
          <CardTitle className="text-2xl">Registrar Empresa</CardTitle>
          <CardDescription>Comienza a emitir comprobantes electrónicos</CardDescription>
        </CardHeader>
        <form onSubmit={handleSubmit}>
          <CardContent className="space-y-4">
            <h3 className="font-semibold text-sm text-muted-foreground uppercase tracking-wider">
              Datos de la Empresa
            </h3>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label htmlFor="ruc">RUC *</Label>
                <Input
                  id="ruc"
                  placeholder="20XXXXXXXXX"
                  maxLength={11}
                  value={form.ruc}
                  onChange={update('ruc')}
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="razonSocial">Razón Social *</Label>
                <Input
                  id="razonSocial"
                  placeholder="Mi Empresa SAC"
                  value={form.razonSocial}
                  onChange={update('razonSocial')}
                  required
                />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label htmlFor="nombreComercial">Nombre Comercial</Label>
                <Input
                  id="nombreComercial"
                  placeholder="Mi Marca"
                  value={form.nombreComercial}
                  onChange={update('nombreComercial')}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="direccion">Dirección</Label>
                <Input
                  id="direccion"
                  placeholder="Av. Principal 123, Lima"
                  value={form.direccion}
                  onChange={update('direccion')}
                />
              </div>
            </div>

            <Separator />
            <h3 className="font-semibold text-sm text-muted-foreground uppercase tracking-wider">
              Cuenta Administrador
            </h3>

            <div className="space-y-2">
              <Label htmlFor="adminFullName">Nombre Completo *</Label>
              <Input
                id="adminFullName"
                placeholder="Juan Pérez"
                value={form.adminFullName}
                onChange={update('adminFullName')}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="adminEmail">Email *</Label>
              <Input
                id="adminEmail"
                type="email"
                placeholder="admin@miempresa.pe"
                value={form.adminEmail}
                onChange={update('adminEmail')}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="adminPassword">Contraseña *</Label>
              <Input
                id="adminPassword"
                type="password"
                placeholder="Mínimo 8 caracteres"
                value={form.adminPassword}
                onChange={update('adminPassword')}
                required
                minLength={8}
              />
            </div>
          </CardContent>
          <CardFooter className="flex flex-col gap-3 border-t bg-muted/50 p-4">
            <Button type="submit" className="w-full" disabled={isLoading}>
              {isLoading ? 'Registrando...' : 'Registrar Empresa'}
            </Button>
            <p className="text-sm text-muted-foreground">
              ¿Ya tienes cuenta?{' '}
              <Link href="/login" className="text-blue-600 hover:underline">
                Inicia sesión
              </Link>
            </p>
          </CardFooter>
        </form>
      </Card>
    </div>
  );
}
