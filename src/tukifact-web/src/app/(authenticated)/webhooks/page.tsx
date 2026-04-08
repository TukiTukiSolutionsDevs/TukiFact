'use client';

import { useEffect, useState } from 'react';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Plus, Trash2, Webhook, Eye, Copy } from 'lucide-react';
import { toast } from 'sonner';

interface WebhookConfig {
  id: string;
  url: string;
  events: string[];
  isActive: boolean;
  maxRetries: number;
  lastTriggeredAt: string | null;
  createdAt: string;
  secret?: string;
}

interface WebhookDelivery {
  id: string;
  eventType: string;
  status: string;
  attempt: number;
  responseStatus: string | null;
  createdAt: string;
}

const EVENT_OPTIONS = [
  'document.created',
  'document.accepted',
  'document.rejected',
  'document.voided',
];

export default function WebhooksPage() {
  const [webhooks, setWebhooks] = useState<WebhookConfig[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [deliveries, setDeliveries] = useState<WebhookDelivery[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [newWebhook, setNewWebhook] = useState({
    url: '',
    events: ['document.created', 'document.accepted'],
    maxRetries: 3,
  });
  const [createdSecret, setCreatedSecret] = useState<string | null>(null);

  const fetchWebhooks = async () => {
    setIsLoading(true);
    try {
      setWebhooks(await api.get<WebhookConfig[]>('/v1/webhooks'));
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchWebhooks();
  }, []);

  const handleCreate = async () => {
    if (!newWebhook.url) {
      toast.error('URL requerida');
      return;
    }
    try {
      const res = await api.post<WebhookConfig & { secret: string }>('/v1/webhooks', newWebhook);
      setCreatedSecret(res.secret);
      toast.success('Webhook creado');
      setIsCreateOpen(false);
      setNewWebhook({ url: '', events: ['document.created', 'document.accepted'], maxRetries: 3 });
      fetchWebhooks();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await api.delete(`/v1/webhooks/${id}`);
      toast.success('Webhook eliminado');
      if (selectedId === id) {
        setSelectedId(null);
        setDeliveries([]);
      }
      fetchWebhooks();
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Error');
    }
  };

  const viewDeliveries = async (id: string) => {
    setSelectedId(id);
    try {
      setDeliveries(await api.get<WebhookDelivery[]>(`/v1/webhooks/${id}/deliveries`));
    } catch {
      setDeliveries([]);
    }
  };

  const toggleEvent = (event: string) => {
    setNewWebhook((w) => ({
      ...w,
      events: w.events.includes(event)
        ? w.events.filter((e) => e !== event)
        : [...w.events, event],
    }));
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Webhooks</h1>
          <p className="text-muted-foreground">Recibe notificaciones cuando ocurren eventos</p>
        </div>
        <Button onClick={() => setIsCreateOpen(true)}>
          <Plus className="mr-2 h-4 w-4" /> Nuevo Webhook
        </Button>
      </div>

      {createdSecret && (
        <Card className="border-amber-200 bg-amber-50 dark:bg-amber-950/20">
          <CardContent className="pt-6">
            <p className="text-sm font-medium text-amber-800 dark:text-amber-400 mb-2">
              Secret del webhook (solo se muestra una vez):
            </p>
            <div className="flex gap-2">
              <code className="flex-1 p-2 bg-white dark:bg-black rounded text-xs font-mono break-all">
                {createdSecret}
              </code>
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  navigator.clipboard.writeText(createdSecret);
                  toast.success('Copiado');
                }}
              >
                <Copy className="h-4 w-4" />
              </Button>
            </div>
            <Button
              variant="ghost"
              size="sm"
              className="mt-2 text-xs"
              onClick={() => setCreatedSecret(null)}
            >
              Cerrar
            </Button>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>URL</TableHead>
                <TableHead>Eventos</TableHead>
                <TableHead>Estado</TableHead>
                <TableHead>Retries</TableHead>
                <TableHead className="text-right">Acciones</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                Array.from({ length: 2 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 5 }).map((_, j) => (
                      <TableCell key={j}>
                        <div className="h-4 bg-muted animate-pulse rounded w-20" />
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              ) : webhooks.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} className="text-center py-8 text-muted-foreground">
                    <Webhook className="mx-auto h-8 w-8 mb-2 opacity-50" />
                    No hay webhooks configurados
                  </TableCell>
                </TableRow>
              ) : (
                webhooks.map((w) => (
                  <TableRow key={w.id}>
                    <TableCell className="font-mono text-xs max-w-[250px] truncate">
                      {w.url}
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-1 flex-wrap">
                        {w.events.map((e) => (
                          <Badge key={e} variant="outline" className="text-xs">
                            {e.split('.')[1]}
                          </Badge>
                        ))}
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant={w.isActive ? 'default' : 'secondary'}>
                        {w.isActive ? 'Activo' : 'Inactivo'}
                      </Badge>
                    </TableCell>
                    <TableCell>{w.maxRetries}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex gap-1 justify-end">
                        <Button variant="ghost" size="sm" onClick={() => viewDeliveries(w.id)}>
                          <Eye className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="text-red-500"
                          onClick={() => handleDelete(w.id)}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {selectedId && deliveries.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Entregas recientes</CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Evento</TableHead>
                  <TableHead>Estado</TableHead>
                  <TableHead>Intento</TableHead>
                  <TableHead>HTTP</TableHead>
                  <TableHead>Fecha</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {deliveries.map((d) => (
                  <TableRow key={d.id}>
                    <TableCell>
                      <Badge variant="outline">{d.eventType}</Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant={d.status === 'delivered' ? 'default' : 'destructive'}>
                        {d.status}
                      </Badge>
                    </TableCell>
                    <TableCell>{d.attempt}</TableCell>
                    <TableCell className="font-mono">{d.responseStatus || '-'}</TableCell>
                    <TableCell>{new Date(d.createdAt).toLocaleString('es-PE')}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}

      {/* Create Dialog */}
      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Nuevo Webhook</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label>URL del endpoint</Label>
              <Input
                placeholder="https://tu-servidor.com/webhook"
                value={newWebhook.url}
                onChange={(e) => setNewWebhook((w) => ({ ...w, url: e.target.value }))}
              />
            </div>
            <div>
              <Label>Eventos</Label>
              <div className="flex flex-wrap gap-2 mt-2">
                {EVENT_OPTIONS.map((ev) => (
                  <Button
                    key={ev}
                    variant={newWebhook.events.includes(ev) ? 'default' : 'outline'}
                    size="sm"
                    onClick={() => toggleEvent(ev)}
                  >
                    {ev.split('.')[1]}
                  </Button>
                ))}
              </div>
            </div>
            <Button onClick={handleCreate} className="w-full">
              Crear Webhook
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
