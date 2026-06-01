'use client';

import { useEffect, useState } from 'react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Loader2, Search } from 'lucide-react';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import { cn } from '@/lib/utils';

export interface LookupStatus {
  configured: boolean;
  provider: string;
  providerName: string;
}

export interface LookupResult {
  name?: string;
  fullName?: string;
  firstName?: string;
  lastName?: string;
  motherLastName?: string;
  address?: string;
}

interface SunatLookupProps {
  docType: string;
  value: string;
  onChange: (v: string) => void;
  onResolve?: (data: { name: string; address?: string }) => void;
  placeholder?: string;
  className?: string;
  disabled?: boolean;
}

export function useSunatLookupStatus() {
  const [status, setStatus] = useState<LookupStatus | null>(null);
  useEffect(() => {
    api
      .get<LookupStatus>('/v1/services/lookup/status')
      .then(setStatus)
      .catch(() => {});
  }, []);
  return status;
}

export function SunatLookup({
  docType,
  value,
  onChange,
  onResolve,
  placeholder,
  className,
  disabled,
}: SunatLookupProps) {
  const [searching, setSearching] = useState(false);
  const status = useSunatLookupStatus();

  const canLookup = docType === '6' || docType === '1';
  const expectedLen = docType === '6' ? 11 : 8;
  const maxLength = docType === '6' ? 11 : docType === '1' ? 8 : 12;

  const lookup = async () => {
    if (!canLookup) return;
    if (value.length !== expectedLen) {
      toast.error(`El número debe tener ${expectedLen} dígitos`);
      return;
    }
    setSearching(true);
    try {
      const endpoint = docType === '6' ? 'ruc' : 'dni';
      const data = await api.get<LookupResult>(`/v1/services/lookup/${endpoint}/${value}`);
      const name =
        data.name ||
        data.fullName ||
        [data.firstName, data.lastName, data.motherLastName].filter(Boolean).join(' ') ||
        '';
      if (!name) {
        toast.error('No se encontraron datos para ese número');
        return;
      }
      onResolve?.({ name, address: data.address });
      toast.success(`Datos encontrados: ${name}`);
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Error al consultar datos';
      if (msg.includes('No hay proveedor')) {
        toast.error('Configura un proveedor de datos en Configuración → Servicios Externos');
      } else {
        toast.error(msg);
      }
    } finally {
      setSearching(false);
    }
  };

  return (
    <div className={cn('flex gap-2', className)}>
      <Input
        placeholder={placeholder}
        value={value}
        maxLength={maxLength}
        onChange={(e) => onChange(e.target.value.replace(/\s/g, ''))}
        className="mono"
        disabled={disabled}
        onKeyDown={(e) => {
          if (e.key === 'Enter' && canLookup) {
            e.preventDefault();
            lookup();
          }
        }}
      />
      {canLookup && (
        <Button
          type="button"
          variant="outline"
          disabled={searching || disabled || !status?.configured}
          title={
            status?.configured
              ? `Buscar con ${status.providerName}`
              : 'Configura un proveedor en Ajustes → Servicios Externos'
          }
          onClick={lookup}
        >
          {searching ? (
            <>
              <Loader2 className="h-4 w-4 mr-2 animate-spin" /> Buscando…
            </>
          ) : (
            <>
              <Search className="h-4 w-4 mr-2" /> Buscar
            </>
          )}
        </Button>
      )}
    </div>
  );
}
