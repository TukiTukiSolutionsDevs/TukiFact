import type {
  TukiFactConfig,
  Document,
  DocumentCreateRequest,
  DocumentResponse,
  Customer,
  Series,
  PaginatedResponse,
  ApiError,
} from './types';

const SDK_NAME = '@tukifact/sdk';
const SDK_VERSION = '0.1.0';
const DEFAULT_BASE_URL = 'https://api.tukifact.net.pe';
const SANDBOX_BASE_URL = 'https://sandbox.tukifact.net.pe';

// ─── Error ────────────────────────────────────────────────────────────────────

export class TukiFactError extends Error {
  public readonly statusCode: number;
  public readonly details?: ApiError;

  constructor(message: string, statusCode: number, details?: ApiError) {
    super(message);
    this.name = 'TukiFactError';
    this.statusCode = statusCode;
    this.details = details;
  }
}

// ─── Client ───────────────────────────────────────────────────────────────────

export class TukiFactClient {
  private readonly baseUrl: string;
  private readonly version: string;
  private readonly timeout: number;
  private readonly apiKey: string;

  constructor(config: TukiFactConfig) {
    this.apiKey = config.apiKey;
    this.version = config.version ?? 'v1';
    this.timeout = config.timeout ?? 30_000;
    this.baseUrl = config.sandbox
      ? SANDBOX_BASE_URL
      : (config.baseUrl ?? DEFAULT_BASE_URL);
  }

  private get defaultHeaders(): Record<string, string> {
    return {
      'Authorization': `Bearer ${this.apiKey}`,
      'Content-Type': 'application/json',
      'X-SDK': SDK_NAME,
      'X-SDK-Version': SDK_VERSION,
    };
  }

  private async request<T>(
    method: string,
    path: string,
    body?: unknown,
    params?: Record<string, string | number | boolean>,
  ): Promise<T> {
    const url = new URL(`${this.baseUrl}/api/${this.version}${path}`);
    if (params) {
      for (const [key, value] of Object.entries(params)) {
        url.searchParams.set(key, String(value));
      }
    }

    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), this.timeout);

    try {
      const res = await fetch(url.toString(), {
        method,
        headers: this.defaultHeaders,
        body: body !== undefined ? JSON.stringify(body) : undefined,
        signal: controller.signal,
      });

      if (!res.ok) {
        let error: ApiError | undefined;
        try {
          error = (await res.json()) as ApiError;
        } catch {
          // ignore parse failure
        }
        throw new TukiFactError(
          error?.message ?? `HTTP ${res.status}`,
          res.status,
          error,
        );
      }

      return (await res.json()) as T;
    } catch (err) {
      if (err instanceof TukiFactError) throw err;
      if ((err as Error).name === 'AbortError') {
        throw new TukiFactError('Request timed out', 408);
      }
      throw new TukiFactError((err as Error).message, 0);
    } finally {
      clearTimeout(timer);
    }
  }

  private async requestRaw(path: string): Promise<Uint8Array> {
    const url = `${this.baseUrl}/api/${this.version}${path}`;
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), this.timeout);

    try {
      const res = await fetch(url, {
        headers: this.defaultHeaders,
        signal: controller.signal,
      });

      if (!res.ok) {
        throw new TukiFactError(`HTTP ${res.status}`, res.status);
      }

      return new Uint8Array(await res.arrayBuffer());
    } catch (err) {
      if (err instanceof TukiFactError) throw err;
      if ((err as Error).name === 'AbortError') {
        throw new TukiFactError('Request timed out', 408);
      }
      throw new TukiFactError((err as Error).message, 0);
    } finally {
      clearTimeout(timer);
    }
  }

  // ── Documents ────────────────────────────────────────────────────────────────

  createDocument(data: DocumentCreateRequest): Promise<DocumentResponse> {
    return this.request<DocumentResponse>('POST', '/documents', data);
  }

  getDocument(id: string): Promise<DocumentResponse> {
    return this.request<DocumentResponse>('GET', `/documents/${id}`);
  }

  listDocuments(params?: {
    page?: number;
    pageSize?: number;
    type?: string;
    status?: string;
  }): Promise<PaginatedResponse<Document>> {
    return this.request<PaginatedResponse<Document>>(
      'GET',
      '/documents',
      undefined,
      params as Record<string, string | number | boolean> | undefined,
    );
  }

  getDocumentPdf(id: string): Promise<Uint8Array> {
    return this.requestRaw(`/documents/${id}/pdf`);
  }

  getDocumentXml(id: string): Promise<Uint8Array> {
    return this.requestRaw(`/documents/${id}/xml`);
  }

  voidDocument(id: string, reason: string): Promise<DocumentResponse> {
    return this.request<DocumentResponse>('POST', `/documents/${id}/void`, { reason });
  }

  // ── Customers ────────────────────────────────────────────────────────────────

  listCustomers(params?: {
    page?: number;
    pageSize?: number;
    search?: string;
  }): Promise<PaginatedResponse<Customer>> {
    return this.request<PaginatedResponse<Customer>>(
      'GET',
      '/customers',
      undefined,
      params as Record<string, string | number | boolean> | undefined,
    );
  }

  // ── Series ───────────────────────────────────────────────────────────────────

  listSeries(): Promise<Series[]> {
    return this.request<Series[]>('GET', '/series');
  }

  // ── Health ───────────────────────────────────────────────────────────────────

  health(): Promise<{ status: string; version: string }> {
    return this.request<{ status: string; version: string }>('GET', '/health');
  }
}
