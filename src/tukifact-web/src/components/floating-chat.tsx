'use client';

import { useState, useRef, useEffect } from 'react';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { MessageCircle, X, Send, Bot, User, Loader2, Settings } from 'lucide-react';
import Link from 'next/link';

interface Message {
  role: 'user' | 'assistant';
  content: string;
  provider?: string;
  model?: string;
}

interface AiStatus {
  configured: boolean;
  provider: string;
  model: string | null;
}

export function FloatingChat() {
  const [isOpen, setIsOpen] = useState(false);
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [aiStatus, setAiStatus] = useState<AiStatus | null>(null);
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    api.get<AiStatus>('/v1/services/ai/status').then(setAiStatus).catch(() => {});
  }, []);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const sendMessage = async (text: string) => {
    if (!text.trim()) return;
    setMessages(prev => [...prev, { role: 'user', content: text }]);
    setInput('');
    setIsLoading(true);

    try {
      const data = await api.post<{ response: string; provider: string; model: string }>('/v1/services/ai/chat', { message: text });
      setMessages(prev => [...prev, {
        role: 'assistant',
        content: data.response,
        provider: data.provider,
        model: data.model,
      }]);
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Error';
      setMessages(prev => [...prev, { role: 'assistant', content: msg.includes('No hay proveedor') ? 'Configura un proveedor de IA en Configuracion > Servicios Externos.' : msg }]);
    } finally {
      setIsLoading(false);
    }
  };

  const providerLabel = aiStatus?.configured
    ? `${aiStatus.provider} / ${aiStatus.model}`
    : null;

  return (
    <>
      {/* Floating Button */}
      {!isOpen && (
        <button
          onClick={() => setIsOpen(true)}
          className="fixed bottom-6 right-6 z-50 h-14 w-14 rounded-full bg-purple-600 text-white shadow-lg hover:bg-purple-700 transition-all hover:scale-105 flex items-center justify-center"
        >
          <MessageCircle className="h-6 w-6" />
        </button>
      )}

      {/* Chat Panel */}
      {isOpen && (
        <div className="fixed bottom-6 right-6 z-50 w-96 h-[520px] bg-background border rounded-2xl shadow-2xl flex flex-col overflow-hidden">
          {/* Header */}
          <div className="flex items-center justify-between px-4 py-3 border-b bg-purple-600 text-white rounded-t-2xl">
            <div className="flex items-center gap-2">
              <Bot className="h-5 w-5" />
              <div>
                <p className="text-sm font-semibold">Copiloto IA</p>
                {providerLabel ? (
                  <p className="text-[10px] opacity-80">{providerLabel}</p>
                ) : (
                  <p className="text-[10px] opacity-80">Sin configurar</p>
                )}
              </div>
            </div>
            <button onClick={() => setIsOpen(false)} className="hover:bg-white/20 rounded-lg p-1">
              <X className="h-4 w-4" />
            </button>
          </div>

          {/* Messages */}
          <div className="flex-1 overflow-y-auto p-3 space-y-3">
            {messages.length === 0 && (
              <div className="text-center py-8 text-muted-foreground text-sm">
                {aiStatus?.configured ? (
                  <>
                    <Bot className="h-8 w-8 mx-auto mb-2 opacity-40" />
                    <p>Preguntame sobre facturación electrónica</p>
                    <div className="mt-3 flex flex-wrap gap-1.5 justify-center">
                      {['¿Tipos de IGV?', '¿Cómo anular?', '¿Qué es una NC?'].map(s => (
                        <button key={s} onClick={() => sendMessage(s)}
                          className="text-xs px-2.5 py-1 rounded-full border hover:bg-muted transition">{s}</button>
                      ))}
                    </div>
                  </>
                ) : (
                  <>
                    <Settings className="h-8 w-8 mx-auto mb-2 opacity-40" />
                    <p>Configura un proveedor de IA</p>
                    <Link href="/settings" onClick={() => setIsOpen(false)}>
                      <Button variant="outline" size="sm" className="mt-2 text-xs">Ir a Configuración</Button>
                    </Link>
                  </>
                )}
              </div>
            )}
            {messages.map((msg, i) => (
              <div key={i} className={`flex gap-2 ${msg.role === 'user' ? 'justify-end' : ''}`}>
                {msg.role === 'assistant' && (
                  <div className="shrink-0 w-6 h-6 rounded-full bg-purple-100 dark:bg-purple-900 flex items-center justify-center">
                    <Bot className="h-3 w-3 text-purple-600" />
                  </div>
                )}
                <div className={`max-w-[80%] rounded-xl px-3 py-2 text-xs ${
                  msg.role === 'user' ? 'bg-purple-600 text-white' : 'bg-muted'
                }`}>
                  <div className="whitespace-pre-wrap">{msg.content}</div>
                  {msg.provider && (
                    <Badge variant="secondary" className="mt-1 text-[9px] h-4">{msg.provider}/{msg.model}</Badge>
                  )}
                </div>
                {msg.role === 'user' && (
                  <div className="shrink-0 w-6 h-6 rounded-full bg-blue-100 dark:bg-blue-900 flex items-center justify-center">
                    <User className="h-3 w-3 text-blue-600" />
                  </div>
                )}
              </div>
            ))}
            {isLoading && (
              <div className="flex gap-2">
                <div className="w-6 h-6 rounded-full bg-purple-100 dark:bg-purple-900 flex items-center justify-center">
                  <Bot className="h-3 w-3 text-purple-600" />
                </div>
                <div className="bg-muted rounded-xl px-3 py-2">
                  <Loader2 className="h-3 w-3 animate-spin" />
                </div>
              </div>
            )}
            <div ref={bottomRef} />
          </div>

          {/* Input */}
          <div className="p-3 border-t">
            <form onSubmit={e => { e.preventDefault(); sendMessage(input); }} className="flex gap-2">
              <Input
                value={input}
                onChange={e => setInput(e.target.value)}
                placeholder="Escribe tu pregunta..."
                className="text-xs h-9"
                disabled={isLoading || !aiStatus?.configured}
              />
              <Button type="submit" size="icon" className="h-9 w-9 shrink-0" disabled={isLoading || !input.trim()}>
                <Send className="h-3.5 w-3.5" />
              </Button>
            </form>
          </div>
        </div>
      )}
    </>
  );
}
