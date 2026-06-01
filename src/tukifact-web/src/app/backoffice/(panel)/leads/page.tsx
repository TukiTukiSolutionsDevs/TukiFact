'use client';

import { useEffect, useState, useCallback } from 'react';
import { api } from '@/lib/api';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Button } from '@/components/ui/button';
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
  SheetFooter,
} from '@/components/ui/sheet';
import { toast } from 'sonner';
import { Search, Inbox, Filter, Mail, Building2, Phone, Calendar } from 'lucide-react';

const STATUSES = ['new', 'contacted', 'qualified', 'dropped'] as const;
type Status = (typeof STATUSES)[number];

const STATUS_LABEL: Record<Status, string> = {
  new: 'Nuevo',
  contacted: 'Contactado',
  qualified: 'Calificado',
  dropped: 'Descartado',
};

const STATUS_CLASS: Record<Status, string> = {
  new: 'bg-indigo-600/20 text-indigo-300 ring-1 ring-indigo-500/30',
  contacted: 'bg-amber-600/20 text-amber-300 ring-1 ring-amber-500/30',
  qualified: 'bg-emerald-600/20 text-emerald-300 ring-1 ring-emerald-500/30',
  dropped: 'bg-slate-600/20 text-slate-400 ring-1 ring-slate-500/30',
};

interface Lead {
  id: string;
  name: string;
  email: string;
  company: string | null;
  phone: string | null;
  reason: string;
  message: string;
  source: string;
  status: string;
  notes: string | null;
  createdAt: string;
  contactedAt: string | null;
}

interface LeadsResponse {
  items: Lead[];
  total: number;
  page: number;
  pageSize: number;
}

const PAGE_SIZE = 20;

function formatDate(iso: string) {
  return new Date(iso).toLocaleString('es-PE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function normalizeStatus(s: string): Status {
  return (STATUSES as readonly string[]).includes(s) ? (s as Status) : 'new';
}

export default function LeadsPage() {
  const [leads, setLeads] = useState<Lead[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<Status | 'all'>('all');
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState<Lead | null>(null);
  const [editStatus, setEditStatus] = useState<Status>('new');
  const [editNotes, setEditNotes] = useState('');
  const [saving, setSaving] = useState(false);

  const fetchLeads = useCallback(
    async (p: number) => {
      setLoading(true);
      try {
        const params = new URLSearchParams({ page: String(p), pageSize: String(PAGE_SIZE) });
        if (search) params.set('search', search);
        if (statusFilter !== 'all') params.set('status', statusFilter);
        const res = await api.get<LeadsResponse>(`/v1/backoffice/leads?${params}`);
        setLeads(res.items);
        setTotal(res.total);
        setPage(res.page);
      } catch (err) {
        toast.error(err instanceof Error ? err.message : 'Error al cargar leads');
      } finally {
        setLoading(false);
      }
    },
    [search, statusFilter]
  );

  useEffect(() => {
    fetchLeads(1);
  }, [fetchLeads]);

  const openLead = (l: Lead) => {
    setSelected(l);
    setEditStatus(normalizeStatus(l.status));
    setEditNotes(l.notes ?? '');
  };

  const save = async () => {
    if (!selected) return;
    setSaving(true);
    try {
      const updated = await api.patch<Lead>(`/v1/backoffice/leads/${selected.id}`, {
        status: editStatus,
        notes: editNotes,
      });
      setLeads((prev) => prev.map((l) => (l.id === updated.id ? updated : l)));
      toast.success('Lead actualizado');
      setSelected(null);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    } finally {
      setSaving(false);
    }
  };

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-white">Leads</h1>
        <p className="text-sm text-slate-400 mt-1">
          {total} contacto{total !== 1 ? 's' : ''} desde el sitio público
        </p>
      </div>

      <div className="flex flex-col sm:flex-row gap-3">
        <form
          onSubmit={(e) => {
            e.preventDefault();
            fetchLeads(1);
          }}
          className="flex-1 flex gap-2"
        >
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-500" />
            <Input
              placeholder="Buscar por email, nombre o empresa..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-9 bg-slate-900 border-slate-700 text-white placeholder:text-slate-500"
            />
          </div>
          <Button type="submit" variant="outline" className="border-slate-700 text-slate-300 hover:bg-slate-800">
            <Search className="h-4 w-4" />
          </Button>
        </form>

        <div className="flex flex-wrap gap-2">
          <Button
            variant={statusFilter === 'all' ? 'default' : 'outline'}
            size="sm"
            onClick={() => setStatusFilter('all')}
            className={
              statusFilter === 'all'
                ? 'bg-indigo-600 text-white'
                : 'border-slate-700 text-slate-400 hover:bg-slate-800'
            }
          >
            <Filter className="h-3 w-3 mr-1" /> Todos
          </Button>
          {STATUSES.map((s) => (
            <Button
              key={s}
              variant={statusFilter === s ? 'default' : 'outline'}
              size="sm"
              onClick={() => setStatusFilter(s)}
              className={
                statusFilter === s
                  ? 'bg-indigo-600 text-white'
                  : 'border-slate-700 text-slate-400 hover:bg-slate-800'
              }
            >
              {STATUS_LABEL[s]}
            </Button>
          ))}
        </div>
      </div>

      <Card className="bg-slate-900 border-slate-800 overflow-hidden">
        <CardContent className="p-0">
          {loading ? (
            <div className="p-8 text-center">
              <div className="h-6 w-6 animate-spin rounded-full border-2 border-indigo-500 border-t-transparent mx-auto" />
            </div>
          ) : leads.length === 0 ? (
            <div className="p-8 text-center text-slate-500">
              <Inbox className="h-10 w-10 mx-auto mb-2 opacity-50" />
              <p>
                No hay leads
                {statusFilter !== 'all' ? ` con estado "${STATUS_LABEL[statusFilter as Status]}"` : ''}
              </p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-slate-800 bg-slate-800/50">
                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Contacto</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Empresa</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Motivo</th>
                    <th className="px-4 py-3 text-center text-xs font-medium text-slate-400 uppercase">Estado</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Creado</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-800">
                  {leads.map((l) => {
                    const status = normalizeStatus(l.status);
                    return (
                      <tr
                        key={l.id}
                        onClick={() => openLead(l)}
                        className="hover:bg-slate-800/50 transition-colors cursor-pointer"
                      >
                        <td className="px-4 py-3">
                          <div className="font-medium text-slate-200">{l.name}</div>
                          <div className="text-xs text-slate-500">{l.email}</div>
                        </td>
                        <td className="px-4 py-3 text-slate-300">
                          {l.company ?? <span className="text-slate-600">—</span>}
                        </td>
                        <td className="px-4 py-3 text-slate-400 capitalize">{l.reason}</td>
                        <td className="px-4 py-3 text-center">
                          <span
                            className={`inline-flex items-center rounded-md px-2 py-1 text-xs font-medium ${STATUS_CLASS[status]}`}
                          >
                            {STATUS_LABEL[status]}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-slate-400 text-xs whitespace-nowrap">
                          {formatDate(l.createdAt)}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      {totalPages > 1 && (
        <div className="flex items-center justify-between text-sm text-slate-400">
          <span>
            Página {page} de {totalPages}
          </span>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={page <= 1 || loading}
              onClick={() => fetchLeads(page - 1)}
              className="border-slate-700 text-slate-300 hover:bg-slate-800"
            >
              Anterior
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={page >= totalPages || loading}
              onClick={() => fetchLeads(page + 1)}
              className="border-slate-700 text-slate-300 hover:bg-slate-800"
            >
              Siguiente
            </Button>
          </div>
        </div>
      )}

      <Sheet open={!!selected} onOpenChange={(o) => !o && setSelected(null)}>
        <SheetContent className="w-full sm:max-w-md bg-slate-900 border-slate-800 text-slate-200">
          {selected && (
            <>
              <SheetHeader>
                <SheetTitle className="text-white">{selected.name}</SheetTitle>
                <SheetDescription className="text-slate-400">
                  Lead recibido el {formatDate(selected.createdAt)}
                </SheetDescription>
              </SheetHeader>

              <div className="px-4 py-4 space-y-4 overflow-y-auto">
                <div className="space-y-2 text-sm">
                  <div className="flex items-start gap-2">
                    <Mail className="h-4 w-4 text-slate-500 mt-0.5 shrink-0" />
                    <a
                      href={`mailto:${selected.email}`}
                      className="text-indigo-300 hover:text-indigo-200 break-all"
                    >
                      {selected.email}
                    </a>
                  </div>
                  {selected.company && (
                    <div className="flex items-start gap-2">
                      <Building2 className="h-4 w-4 text-slate-500 mt-0.5 shrink-0" />
                      <span>{selected.company}</span>
                    </div>
                  )}
                  {selected.phone && (
                    <div className="flex items-start gap-2">
                      <Phone className="h-4 w-4 text-slate-500 mt-0.5 shrink-0" />
                      <span>{selected.phone}</span>
                    </div>
                  )}
                  {selected.contactedAt && (
                    <div className="flex items-start gap-2">
                      <Calendar className="h-4 w-4 text-slate-500 mt-0.5 shrink-0" />
                      <span className="text-slate-400">
                        Contactado: {formatDate(selected.contactedAt)}
                      </span>
                    </div>
                  )}
                </div>

                <div className="rounded-md border border-slate-800 bg-slate-950 p-3 text-sm">
                  <p className="text-xs uppercase font-medium text-slate-500 mb-1">
                    Motivo: {selected.reason}
                  </p>
                  <p className="text-slate-300 whitespace-pre-wrap">{selected.message}</p>
                </div>

                <div>
                  <label className="text-xs font-medium text-slate-400 uppercase">Estado</label>
                  <div className="mt-2 grid grid-cols-2 gap-2">
                    {STATUSES.map((s) => (
                      <button
                        key={s}
                        type="button"
                        onClick={() => setEditStatus(s)}
                        className={`rounded-md px-3 py-2 text-sm font-medium transition-colors ${
                          editStatus === s
                            ? STATUS_CLASS[s]
                            : 'border border-slate-700 text-slate-400 hover:bg-slate-800'
                        }`}
                      >
                        {STATUS_LABEL[s]}
                      </button>
                    ))}
                  </div>
                </div>

                <div>
                  <label className="text-xs font-medium text-slate-400 uppercase">Notas internas</label>
                  <Textarea
                    value={editNotes}
                    onChange={(e) => setEditNotes(e.target.value)}
                    placeholder="Comentarios del equipo de ventas..."
                    rows={5}
                    className="mt-2 bg-slate-950 border-slate-700 text-white placeholder:text-slate-500"
                  />
                </div>

                <p className="text-xs text-slate-500">
                  Origen: <span className="font-mono">{selected.source}</span>
                </p>
              </div>

              <SheetFooter>
                <Button
                  variant="outline"
                  onClick={() => setSelected(null)}
                  className="border-slate-700 text-slate-300 hover:bg-slate-800"
                >
                  Cancelar
                </Button>
                <Button
                  onClick={save}
                  disabled={saving}
                  className="bg-indigo-600 hover:bg-indigo-700 text-white"
                >
                  {saving ? 'Guardando…' : 'Guardar'}
                </Button>
              </SheetFooter>
            </>
          )}
        </SheetContent>
      </Sheet>
    </div>
  );
}
