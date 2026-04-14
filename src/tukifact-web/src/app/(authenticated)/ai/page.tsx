'use client';

import { useState, useRef, useEffect } from 'react';
import { api } from '@/lib/api';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Send, Bot, User, Sparkles, Settings } from 'lucide-react';
import Link from 'next/link';

interface AiStatus { configured: boolean; provider: string; model: string | null; }

interface Message {
  role: 'user' | 'assistant';
  content: string;
  provider?: string;
  model?: string;
  suggestions?: string[];
}

export default function AIPage() {
  const [messages, setMessages] = useState<Message[]>([{
    role: 'assistant',
    content: 'Hola! Soy el Copiloto de TukiFact. Puedo ayudarte con facturación electrónica, reglas SUNAT, IGV, series, y más.\n\nPara usar el copiloto necesitas configurar un proveedor de IA en Configuración → Servicios Externos.',
    suggestions: ['¿Cómo emitir una factura?', '¿Cuáles son los tipos de IGV?', '¿Cómo anular un comprobante?']
  }]);
  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [aiStatus, setAiStatus] = useState<AiStatus | null>(null);
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => { api.get<AiStatus>('/v1/services/ai/status').then(setAiStatus).catch(() => {}); }, []);
  useEffect(() => { bottomRef.current?.scrollIntoView({ behavior: 'smooth' }); }, [messages]);

  const sendMessage = async (text: string) => {
    if (!text.trim()) return;
    const userMsg: Message = { role: 'user', content: text };
    setMessages(prev => [...prev, userMsg]);
    setInput('');
    setIsLoading(true);

    try {
      const data = await api.post<{ response: string; provider: string; model: string }>('/v1/services/ai/chat', { message: text });
      const assistantMsg: Message = {
        role: 'assistant',
        content: data.response,
        provider: data.provider,
        model: data.model,
      };
      setMessages(prev => [...prev, assistantMsg]);
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Error al conectar con el servicio de IA';
      if (msg.includes('No hay proveedor') || msg.includes('no configurados')) {
        setMessages(prev => [...prev, {
          role: 'assistant',
          content: '⚠️ No tienes un proveedor de IA configurado.\n\nVe a **Configuración → Servicios Externos** para conectar tu cuenta de Gemini, Claude, Grok, DeepSeek u OpenAI.',
        }]);
      } else {
        setMessages(prev => [...prev, { role: 'assistant', content: `❌ ${msg}` }]);
      }
    } finally { setIsLoading(false); }
  };

  return (
    <div className="flex flex-col h-[calc(100vh-6rem)] max-w-3xl mx-auto">
      <div className="flex items-center gap-3 mb-4">
        <div className="p-2 rounded-lg bg-purple-100 text-purple-600 dark:bg-purple-900 dark:text-purple-400">
          <Sparkles className="h-5 w-5" />
        </div>
        <div className="flex-1">
          <h1 className="text-2xl font-bold">Copiloto IA</h1>
          <div className="flex items-center gap-2 mt-0.5">
            {aiStatus?.configured ? (
              <Badge variant="default" className="text-xs">
                {aiStatus.provider} / {aiStatus.model}
              </Badge>
            ) : (
              <Badge variant="secondary" className="text-xs">Sin proveedor configurado</Badge>
            )}
          </div>
        </div>
        <Link href="/settings">
          <Button variant="outline" size="sm"><Settings className="h-4 w-4 mr-1" /> Configurar IA</Button>
        </Link>
      </div>

      <Card className="flex-1 overflow-hidden flex flex-col">
        <CardContent className="flex-1 overflow-y-auto p-4 space-y-4">
          {messages.map((msg, i) => (
            <div key={i} className={`flex gap-3 ${msg.role === 'user' ? 'justify-end' : ''}`}>
              {msg.role === 'assistant' && (
                <div className="flex-shrink-0 w-8 h-8 rounded-full bg-purple-100 dark:bg-purple-900 flex items-center justify-center">
                  <Bot className="h-4 w-4 text-purple-600 dark:text-purple-400" />
                </div>
              )}
              <div className={`max-w-[80%] rounded-lg p-3 text-sm ${
                msg.role === 'user'
                  ? 'bg-blue-600 text-white'
                  : 'bg-muted'
              }`}>
                <div className="whitespace-pre-wrap">{msg.content}</div>
                {msg.provider && (
                  <div className="mt-2 flex gap-1">
                    <Badge variant="secondary" className="text-xs">{msg.provider} / {msg.model}</Badge>
                  </div>
                )}
                {msg.suggestions && msg.suggestions.length > 0 && (
                  <div className="mt-3 flex gap-2 flex-wrap">
                    {msg.suggestions.map(s => (
                      <Button key={s} variant="outline" size="sm" className="text-xs h-7"
                        onClick={() => sendMessage(s)}>{s}</Button>
                    ))}
                  </div>
                )}
              </div>
              {msg.role === 'user' && (
                <div className="flex-shrink-0 w-8 h-8 rounded-full bg-blue-100 dark:bg-blue-900 flex items-center justify-center">
                  <User className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                </div>
              )}
            </div>
          ))}
          {isLoading && (
            <div className="flex gap-3">
              <div className="w-8 h-8 rounded-full bg-purple-100 dark:bg-purple-900 flex items-center justify-center">
                <Bot className="h-4 w-4 text-purple-600 animate-pulse" />
              </div>
              <div className="bg-muted rounded-lg p-3">
                <div className="flex gap-1">
                  <div className="w-2 h-2 bg-muted-foreground/50 rounded-full animate-bounce" style={{animationDelay:'0ms'}} />
                  <div className="w-2 h-2 bg-muted-foreground/50 rounded-full animate-bounce" style={{animationDelay:'150ms'}} />
                  <div className="w-2 h-2 bg-muted-foreground/50 rounded-full animate-bounce" style={{animationDelay:'300ms'}} />
                </div>
              </div>
            </div>
          )}
          <div ref={bottomRef} />
        </CardContent>

        <div className="p-4 border-t">
          <form onSubmit={e => { e.preventDefault(); sendMessage(input); }} className="flex gap-2">
            <Input placeholder="Pregunta sobre facturación electrónica..." value={input}
              onChange={e => setInput(e.target.value)} disabled={isLoading} className="flex-1" />
            <Button type="submit" disabled={isLoading || !input.trim()}>
              <Send className="h-4 w-4" />
            </Button>
          </form>
        </div>
      </Card>
    </div>
  );
}
