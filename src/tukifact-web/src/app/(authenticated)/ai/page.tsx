'use client';

import { useState, useRef, useEffect } from 'react';
import Link from 'next/link';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Send,
  Sparkles,
  Settings,
  Loader2,
  FilePlus,
  BarChart3,
  Users,
  CircleHelp,
} from 'lucide-react';

interface AiStatus {
  configured: boolean;
  provider: string;
  model: string | null;
}

interface Message {
  role: 'user' | 'assistant';
  content: string;
  provider?: string;
  model?: string;
}

const SUGGESTIONS: { icon: React.ElementType; text: string }[] = [
  { icon: FilePlus, text: '¿Cómo emitir mi primera factura?' },
  { icon: BarChart3, text: '¿Cuánto facturé este mes?' },
  { icon: Users, text: '¿Quién es mi mejor cliente?' },
  { icon: CircleHelp, text: '¿Cómo anulo un comprobante?' },
];

// Minimal **bold** parser for the assistant bubble.
function Md({ text }: { text: string }) {
  const parts = text.split(/(\*\*[^*]+\*\*)/g);
  return (
    <>
      {parts.map((p, i) =>
        p.startsWith('**') ? (
          <strong key={i} className="font-semibold">
            {p.slice(2, -2)}
          </strong>
        ) : (
          <span key={i}>{p}</span>
        )
      )}
    </>
  );
}

const WELCOME =
  '¡Hola! Soy **Tuki**, tu asistente de facturación electrónica. Puedo ayudarte a emitir comprobantes, consultar tus ventas, encontrar clientes o productos, y resolver dudas sobre SUNAT.\n\nConéctame a tu proveedor de IA preferido (Gemini, Claude, Grok, DeepSeek o OpenAI) desde **Configuración → Servicios Externos**.';

export default function AIPage() {
  const [messages, setMessages] = useState<Message[]>([
    { role: 'assistant', content: WELCOME },
  ]);
  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [aiStatus, setAiStatus] = useState<AiStatus | null>(null);
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    api
      .get<AiStatus>('/v1/services/ai/status')
      .then(setAiStatus)
      .catch(() => {});
  }, []);

  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [messages, isLoading]);

  const sendMessage = async (text: string) => {
    const trimmed = text.trim();
    if (!trimmed || isLoading) return;
    setMessages((m) => [...m, { role: 'user', content: trimmed }]);
    setInput('');
    setIsLoading(true);

    try {
      const data = await api.post<{ response: string; provider: string; model: string }>(
        '/v1/services/ai/chat',
        { message: trimmed }
      );
      setMessages((m) => [
        ...m,
        { role: 'assistant', content: data.response, provider: data.provider, model: data.model },
      ]);
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Error al conectar con el servicio de IA';
      if (msg.includes('No hay proveedor') || msg.includes('no configurados')) {
        setMessages((m) => [
          ...m,
          {
            role: 'assistant',
            content:
              'No tienes un proveedor de IA configurado. Ve a **Configuración → Servicios Externos** para conectar tu cuenta y empezar a chatear.',
          },
        ]);
      } else {
        setMessages((m) => [...m, { role: 'assistant', content: msg }]);
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="flex flex-col" style={{ height: 'calc(100vh - var(--gap-cards) * 2 - 80px)', minHeight: 480 }}>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-4">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0 inline-flex items-center gap-2 flex-wrap">
            Asistente IA
            <Badge
              variant="outline"
              className="t-caption font-semibold"
              style={{
                background: 'color-mix(in oklch, var(--brand-toucan-orange) 14%, transparent)',
                color: 'var(--brand-toucan-orange)',
                borderColor: 'transparent',
              }}
            >
              Beta
            </Badge>
          </h1>
          <p className="t-body mt-1.5 mb-0" style={{ color: 'var(--muted-foreground)' }}>
            Pídele a Tuki que facture, consulte o te explique algo.{' '}
            {aiStatus?.configured ? (
              <span>
                Proveedor activo:{' '}
                <span className="mono font-semibold" style={{ color: 'var(--foreground)' }}>
                  {aiStatus.provider}
                  {aiStatus.model ? ` · ${aiStatus.model}` : ''}
                </span>
                .
              </span>
            ) : (
              <span style={{ color: 'var(--warning)' }}>
                Aún no hay un proveedor de IA configurado.
              </span>
            )}
          </p>
        </div>
        <Button variant="outline" size="sm" asChild>
          <Link href="/settings">
            <Settings className="h-4 w-4 mr-2" /> Configurar IA
          </Link>
        </Button>
      </div>

      {/* Chat card */}
      <section
        className="flex-1 rounded-[var(--radius-lg)] border bg-card flex flex-col overflow-hidden min-h-0"
        style={{ boxShadow: 'var(--shadow-xs)' }}
      >
        {/* Messages */}
        <div
          ref={scrollRef}
          className="flex-1 overflow-y-auto px-6 py-6 flex flex-col gap-4"
        >
          {messages.map((m, i) =>
            m.role === 'user' ? (
              <div key={i} className="self-end max-w-[78%]">
                <div
                  className="t-body-sm font-medium"
                  style={{
                    background: 'var(--accent)',
                    color: 'var(--accent-foreground)',
                    padding: '10px 14px',
                    borderRadius: '16px 16px 4px 16px',
                    lineHeight: '20px',
                  }}
                >
                  {m.content}
                </div>
              </div>
            ) : (
              <div key={i} className="self-start max-w-[82%] flex gap-2.5">
                <span
                  className="flex h-8 w-8 items-center justify-center rounded-full shrink-0 mt-0.5"
                  style={{
                    background: 'color-mix(in oklch, var(--accent) 22%, transparent)',
                  }}
                >
                  <Sparkles className="h-4 w-4" style={{ color: 'var(--brand-ink)' }} />
                </span>
                <div className="min-w-0 flex-1">
                  <div
                    className="t-body-sm whitespace-pre-wrap"
                    style={{
                      background: 'var(--muted)',
                      color: 'var(--foreground)',
                      padding: '10px 14px',
                      borderRadius: '4px 16px 16px 16px',
                      lineHeight: '21px',
                    }}
                  >
                    <Md text={m.content} />
                  </div>
                  {m.provider && (
                    <div className="mt-1.5">
                      <span
                        className="inline-flex t-caption mono"
                        style={{ color: 'var(--muted-foreground)' }}
                      >
                        {m.provider}
                        {m.model ? ` · ${m.model}` : ''}
                      </span>
                    </div>
                  )}
                </div>
              </div>
            )
          )}
          {isLoading && (
            <div className="self-start flex gap-2.5 items-center">
              <span
                className="flex h-8 w-8 items-center justify-center rounded-full shrink-0"
                style={{
                  background: 'color-mix(in oklch, var(--accent) 22%, transparent)',
                }}
              >
                <Sparkles className="h-4 w-4" style={{ color: 'var(--brand-ink)' }} />
              </span>
              <div
                className="flex gap-1"
                style={{
                  background: 'var(--muted)',
                  padding: '12px 16px',
                  borderRadius: '4px 16px 16px 16px',
                }}
              >
                {[0, 1, 2].map((d) => (
                  <span
                    key={d}
                    className="rounded-full"
                    style={{
                      width: 7,
                      height: 7,
                      background: 'var(--muted-foreground)',
                      animation: 'tf-typing 1s ease-in-out infinite',
                      animationDelay: `${d * 0.16}s`,
                    }}
                  />
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Suggestions at start */}
        {messages.length === 1 && !isLoading && (
          <div className="px-6 pb-2 flex gap-2 flex-wrap">
            {SUGGESTIONS.map((s) => {
              const Icon = s.icon;
              return (
                <button
                  key={s.text}
                  type="button"
                  onClick={() => sendMessage(s.text)}
                  className="inline-flex items-center gap-1.5 rounded-full border bg-card t-caption font-medium transition-colors hover:bg-[var(--muted)] px-3 py-2"
                  style={{ borderColor: 'var(--border)' }}
                >
                  <Icon className="h-3.5 w-3.5" style={{ color: 'var(--muted-foreground)' }} />
                  {s.text}
                </button>
              );
            })}
          </div>
        )}

        {/* Composer */}
        <div
          className="px-6 py-3.5"
          style={{ borderTop: '1px solid var(--border)', background: 'var(--background)' }}
        >
          <form
            onSubmit={(e) => {
              e.preventDefault();
              sendMessage(input);
            }}
            className="flex gap-2.5 items-end"
          >
            <textarea
              value={input}
              onChange={(e) => setInput(e.target.value)}
              rows={1}
              placeholder="Escribe un mensaje a Tuki…"
              onKeyDown={(e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                  e.preventDefault();
                  sendMessage(input);
                }
              }}
              className="flex-1 resize-none mono-disable"
              style={{
                maxHeight: 120,
                padding: '11px 14px',
                fontSize: 14,
                lineHeight: '20px',
                color: 'var(--foreground)',
                background: 'var(--card)',
                border: '1px solid var(--input)',
                borderRadius: 'var(--radius-lg)',
                outline: 'none',
                fontFamily: 'inherit',
              }}
              onFocus={(e) => {
                e.target.style.borderColor = 'var(--ring)';
                e.target.style.boxShadow = 'var(--shadow-focus-ring)';
              }}
              onBlur={(e) => {
                e.target.style.boxShadow = 'none';
                e.target.style.borderColor = 'var(--input)';
              }}
              disabled={isLoading}
            />
            <Button
              type="submit"
              disabled={!input.trim() || isLoading}
              style={{
                background: 'var(--accent)',
                color: 'var(--accent-foreground)',
                fontWeight: 600,
                height: 44,
                width: 44,
                padding: 0,
              }}
              aria-label="Enviar"
            >
              {isLoading ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <Send className="h-4 w-4" />
              )}
            </Button>
          </form>
          <p
            className="t-caption mt-2 text-center"
            style={{ color: 'var(--muted-foreground)' }}
          >
            Tuki puede cometer errores. Verifica los importes antes de emitir.
          </p>
        </div>
      </section>

      <style jsx>{`
        @keyframes tf-typing {
          0%,
          60%,
          100% {
            opacity: 0.3;
            transform: translateY(0);
          }
          30% {
            opacity: 1;
            transform: translateY(-3px);
          }
        }
      `}</style>
    </div>
  );
}
