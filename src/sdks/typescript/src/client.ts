import type {
  TukiFactConfig, AuthResponse, DocumentResponse, CreateDocumentRequest,
  CreateCreditNoteRequest, PaginatedResponse, DashboardResponse,
  SeriesResponse, PlanResponse,
} from './types';

export class TukiFactClient {
  private baseUrl: string;
  private apiKey?: string;
  private accessToken?: string;
  private tenantId?: string;
  private timeout: number;

  constructor(config: TukiFactConfig = {}) {
    this.baseUrl = (config.baseUrl || 'https://tukifact.net.pe').replace(/\/$/, '');
    this.apiKey = config.apiKey;
    this.accessToken = config.accessToken;
    this.tenantId = config.tenantId;
    this.timeout = config.timeout || 30000;
  }

  // === Auth ===
  async login(email: string, password: string, tenantId: string): Promise<AuthResponse> {
    const res = await this.post<AuthResponse>('/v1/auth/login', { email, password, tenantId });
    this.accessToken = res.accessToken;
    this.tenantId = res.user.tenantId;
    return res;
  }

  // === Documents ===
  async emitDocument(request: CreateDocumentRequest): Promise<DocumentResponse> {
    return this.post<DocumentResponse>('/v1/documents', request);
  }

  async emitCreditNote(request: CreateCreditNoteRequest): Promise<DocumentResponse> {
    return this.post<DocumentResponse>('/v1/documents/credit-note', request);
  }

  async getDocument(id: string): Promise<DocumentResponse> {
    return this.get<DocumentResponse>(`/v1/documents/${id}`);
  }

  async listDocuments(params: {
    page?: number; pageSize?: number; documentType?: string; status?: string;
    dateFrom?: string; dateTo?: string;
  } = {}): Promise<PaginatedResponse<DocumentResponse>> {
    const qs = new URLSearchParams();
    Object.entries(params).forEach(([k, v]) => { if (v !== undefined) qs.set(k, String(v)); });
    return this.get<PaginatedResponse<DocumentResponse>>(`/v1/documents?${qs}`);
  }

  async downloadPdf(id: string): Promise<ArrayBuffer> {
    return this.getBlob(`/v1/documents/${id}/pdf`);
  }

  async downloadXml(id: string): Promise<ArrayBuffer> {
    return this.getBlob(`/v1/documents/${id}/xml`);
  }

  // === Dashboard ===
  async getDashboard(): Promise<DashboardResponse> {
    return this.get<DashboardResponse>('/v1/dashboard');
  }

  // === Series ===
  async listSeries(): Promise<SeriesResponse[]> {
    return this.get<SeriesResponse[]>('/v1/series');
  }

  async createSeries(documentType: string, serie: string): Promise<SeriesResponse> {
    return this.post<SeriesResponse>('/v1/series', { documentType, serie });
  }

  // === Plans ===
  async listPlans(): Promise<PlanResponse[]> {
    return this.get<PlanResponse[]>('/v1/plans');
  }

  // === Void ===
  async voidDocument(documentId: string, reason: string): Promise<unknown> {
    return this.post('/v1/voided-documents', { documentId, voidReason: reason });
  }

  // === HTTP helpers ===
  private async request<T>(path: string, init: RequestInit): Promise<T> {
    const headers: Record<string, string> = { 'Content-Type': 'application/json' };
    if (this.accessToken) headers['Authorization'] = `Bearer ${this.accessToken}`;
    if (this.apiKey) headers['X-Api-Key'] = this.apiKey;
    if (this.tenantId) headers['X-Tenant-Id'] = this.tenantId;

    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), this.timeout);

    try {
      const res = await fetch(`${this.baseUrl}${path}`, { ...init, headers: { ...headers, ...(init.headers as Record<string, string> || {}) }, signal: controller.signal });
      if (!res.ok) {
        const err = await res.json().catch(() => ({ error: `HTTP ${res.status}` })) as { error?: string };
        throw new Error(err.error || `HTTP ${res.status}`);
      }
      if (res.status === 204) return {} as T;
      return res.json() as Promise<T>;
    } finally { clearTimeout(timer); }
  }

  private get<T>(path: string) { return this.request<T>(path, { method: 'GET' }); }
  private post<T>(path: string, body: unknown) { return this.request<T>(path, { method: 'POST', body: JSON.stringify(body) }); }

  private async getBlob(path: string): Promise<ArrayBuffer> {
    const headers: Record<string, string> = {};
    if (this.accessToken) headers['Authorization'] = `Bearer ${this.accessToken}`;
    if (this.apiKey) headers['X-Api-Key'] = this.apiKey;

    const res = await fetch(`${this.baseUrl}${path}`, { headers });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return res.arrayBuffer();
  }
}
