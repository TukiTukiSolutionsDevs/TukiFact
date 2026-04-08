'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { api, type SeriesResponse } from '@/lib/api';
import { useAuth } from '@/lib/auth-context';
import {
  Card,
  CardContent,
} from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import {
  CheckCircle,
  Circle,
  ArrowRight,
  Building,
  ListOrdered,
  FileText,
  Settings,
} from 'lucide-react';

interface Step {
  id: string;
  title: string;
  description: string;
  icon: React.ElementType;
  href: string;
  check: () => Promise<boolean>;
}

export default function WelcomePage() {
  const { user } = useAuth();
  const router = useRouter();
  const [checks, setChecks] = useState<Record<string, boolean>>({});
  const [isLoading, setIsLoading] = useState(true);

  const steps: Step[] = [
    {
      id: 'company',
      title: 'Registrar Empresa',
      description: 'Tu empresa ya está registrada con su RUC',
      icon: Building,
      href: '/settings',
      check: async () => !!user?.tenantId,
    },
    {
      id: 'series',
      title: 'Crear Series',
      description: 'Agrega al menos una serie (ej: F001 para Facturas)',
      icon: ListOrdered,
      href: '/series',
      check: async () => {
        try {
          const series = await api.get<SeriesResponse[]>('/v1/series');
          return series.length > 0;
        } catch {
          return false;
        }
      },
    },
    {
      id: 'document',
      title: 'Emitir Primer Comprobante',
      description: 'Emite tu primera factura o boleta electrónica',
      icon: FileText,
      href: '/documents/new',
      check: async () => {
        try {
          const res = await api.get<{ pagination: { totalCount: number } }>(
            '/v1/documents?pageSize=1'
          );
          return res.pagination.totalCount > 0;
        } catch {
          return false;
        }
      },
    },
    {
      id: 'certificate',
      title: 'Configurar Certificado Digital',
      description: 'Sube tu certificado para firmar comprobantes (opcional en beta)',
      icon: Settings,
      href: '/settings',
      check: async () => false,
    },
  ];

  useEffect(() => {
    const runChecks = async () => {
      const results: Record<string, boolean> = {};
      for (const step of steps) {
        results[step.id] = await step.check();
      }
      setChecks(results);
      setIsLoading(false);
    };
    runChecks();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const completedCount = Object.values(checks).filter(Boolean).length;
  const progress = Math.round((completedCount / steps.length) * 100);

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div className="text-center">
        <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-blue-600 text-white font-bold text-2xl">
          T
        </div>
        <h1 className="text-3xl font-bold">Bienvenido a TukiFact</h1>
        <p className="text-muted-foreground mt-2">
          Completa estos pasos para empezar a emitir comprobantes electrónicos
        </p>
      </div>

      {/* Progress */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex items-center justify-between mb-2">
            <span className="text-sm font-medium">Progreso</span>
            <span className="text-sm text-muted-foreground">
              {completedCount}/{steps.length} completados
            </span>
          </div>
          <div className="w-full bg-muted rounded-full h-2.5">
            <div
              className="bg-blue-600 h-2.5 rounded-full transition-all duration-500"
              style={{ width: `${progress}%` }}
            />
          </div>
        </CardContent>
      </Card>

      {/* Steps */}
      <div className="space-y-3">
        {steps.map((step) => {
          const isDone = checks[step.id];
          const Icon = step.icon;
          return (
            <Card
              key={step.id}
              className={`transition-colors ${
                isDone
                  ? 'border-green-200 bg-green-50/50 dark:border-green-900 dark:bg-green-950/20'
                  : ''
              }`}
            >
              <CardContent className="pt-6">
                <div className="flex items-center gap-4">
                  <div
                    className={`p-2 rounded-lg ${
                      isDone
                        ? 'bg-green-100 text-green-600 dark:bg-green-900 dark:text-green-400'
                        : 'bg-muted text-muted-foreground'
                    }`}
                  >
                    <Icon className="h-5 w-5" />
                  </div>
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <h3 className="font-medium">{step.title}</h3>
                      {isDone ? (
                        <CheckCircle className="h-4 w-4 text-green-600" />
                      ) : (
                        <Circle className="h-4 w-4 text-muted-foreground" />
                      )}
                    </div>
                    <p className="text-sm text-muted-foreground">{step.description}</p>
                  </div>
                  {!isDone && !isLoading && (
                    <Button variant="outline" size="sm" onClick={() => router.push(step.href)}>
                      Ir <ArrowRight className="h-4 w-4 ml-1" />
                    </Button>
                  )}
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {completedCount >= 3 && (
        <div className="text-center">
          <Button size="lg" onClick={() => router.push('/dashboard')}>
            Ir al Dashboard <ArrowRight className="h-4 w-4 ml-2" />
          </Button>
        </div>
      )}
    </div>
  );
}
