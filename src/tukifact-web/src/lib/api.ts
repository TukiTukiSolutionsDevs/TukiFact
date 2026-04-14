const API_BASE = process.env.NEXT_PUBLIC_API_URL || '';

interface ApiError {
  error: string;
  detail?: string;
}

class ApiClient {
  private accessToken: string | null = null;

  setToken(token: string | null) {
    this.accessToken = token;
    if (token) {
      localStorage.setItem('access_token', token);
    } else {
      localStorage.removeItem('access_token');
    }
  }

  getToken(): string | null {
    if (this.accessToken) return this.accessToken;
    if (typeof window !== 'undefined') {
      this.accessToken = localStorage.getItem('access_token');
    }
    return this.accessToken;
  }

  private async request<T>(path: string, options: RequestInit = {}): Promise<T> {
    const token = this.getToken();
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...((options.headers as Record<string, string>) || {}),
    };

    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const res = await fetch(`${API_BASE}${path}`, {
      ...options,
      headers,
    });

    if (res.status === 401) {
      const refreshed = await this.tryRefresh();
      if (refreshed) {
        headers['Authorization'] = `Bearer ${this.getToken()}`;
        const retry = await fetch(`${API_BASE}${path}`, { ...options, headers });
        if (!retry.ok) throw new Error(((await retry.json()) as ApiError).error || 'Request failed');
        return retry.json() as Promise<T>;
      }
      this.logout();
      if (typeof window !== 'undefined') window.location.href = '/login';
      throw new Error('Session expired');
    }

    if (!res.ok) {
      const error = await res.json().catch(() => ({ error: `HTTP ${res.status}` })) as ApiError;
      throw new Error(error.error || error.detail || `HTTP ${res.status}`);
    }

    if (res.status === 204) return {} as T;
    return res.json() as Promise<T>;
  }

  private async tryRefresh(): Promise<boolean> {
    const refreshToken = typeof window !== 'undefined' ? localStorage.getItem('refresh_token') : null;
    if (!refreshToken) return false;

    try {
      const res = await fetch(`${API_BASE}/v1/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      });
      if (!res.ok) return false;

      const data = (await res.json()) as AuthResponse;
      this.setToken(data.accessToken);
      localStorage.setItem('refresh_token', data.refreshToken);
      return true;
    } catch {
      return false;
    }
  }

  logout() {
    this.accessToken = null;
    if (typeof window !== 'undefined') {
      localStorage.removeItem('access_token');
      localStorage.removeItem('refresh_token');
      localStorage.removeItem('user');
    }
  }

  get<T>(path: string) {
    return this.request<T>(path);
  }
  post<T>(path: string, body: unknown) {
    return this.request<T>(path, { method: 'POST', body: JSON.stringify(body) });
  }
  put<T>(path: string, body: unknown) {
    return this.request<T>(path, { method: 'PUT', body: JSON.stringify(body) });
  }
  delete<T>(path: string) {
    return this.request<T>(path, { method: 'DELETE' });
  }
}

export const api = new ApiClient();

// Types
export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserInfo;
}

export interface UserInfo {
  id: string;
  tenantId: string;
  email: string;
  fullName: string | null;
  role: string;
}

export interface Plan {
  id: string;
  name: string;
  priceMonthly: number;
  maxDocumentsPerMonth: number;
  features: Record<string, unknown>;
  isActive: boolean;
}

export interface DocumentResponse {
  id: string;
  documentType: string;
  documentTypeName: string;
  serie: string;
  correlative: number;
  fullNumber: string;
  issueDate: string;
  dueDate: string | null;
  currency: string;
  customerDocType: string;
  customerDocNumber: string;
  customerName: string;
  operacionGravada: number;
  operacionExonerada: number;
  operacionInafecta: number;
  igv: number;
  total: number;
  status: string;
  sunatResponseCode: string | null;
  sunatResponseDescription: string | null;
  hashCode: string | null;
  xmlUrl: string | null;
  pdfUrl: string | null;
  notes: string | null;
  createdAt: string;
  items: DocumentItemResponse[];
}

export interface DocumentItemResponse {
  sequence: number;
  productCode: string | null;
  description: string;
  quantity: number;
  unitMeasure: string;
  unitPrice: number;
  unitPriceWithIgv: number;
  igvType: string;
  igvAmount: number;
  subtotal: number;
  total: number;
}

export interface PaginatedResponse<T> {
  data: T[];
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

export interface DashboardResponse {
  today: DashboardSummary;
  thisMonth: DashboardSummary;
  thisYear: DashboardSummary;
  byType: { documentType: string; name: string; count: number; total: number }[];
  byStatus: { status: string; count: number }[];
  monthlySales: { year: number; month: number; count: number; total: number }[];
}

export interface DashboardSummary {
  totalDocuments: number;
  totalAmount: number;
  totalIgv: number;
  accepted: number;
  rejected: number;
  pending: number;
}

export interface SeriesResponse {
  id: string;
  documentType: string;
  serie: string;
  currentCorrelative: number;
  emissionPoint: string;
  isActive: boolean;
  createdAt: string;
}

// === Batch A: Despatch Advices (GRE) ===
export interface DespatchAdviceResponse {
  id: string;
  documentType: string;
  serie: string;
  correlative: number;
  fullNumber: string;
  issueDate: string;
  issueTime: string;
  recipientDocType: string;
  recipientDocNumber: string;
  recipientName: string;
  transferReasonCode: string;
  transferReasonDescription: string;
  transferStartDate: string;
  transportMode: string;
  originAddress: string;
  originUbigeo: string;
  destinationAddress: string;
  destinationUbigeo: string;
  grossWeight: number;
  weightUnitCode: string;
  totalPackages: number;
  vehiclePlate: string | null;
  driverDocType: string | null;
  driverDocNumber: string | null;
  driverName: string | null;
  driverLicense: string | null;
  carrierDocType: string | null;
  carrierDocNumber: string | null;
  carrierName: string | null;
  status: string;
  sunatResponseCode: string | null;
  sunatResponseMessage: string | null;
  sunatTicket: string | null;
  xmlUrl: string | null;
  pdfUrl: string | null;
  cdrUrl: string | null;
  createdAt: string;
  items: DespatchAdviceItemResponse[];
}

export interface DespatchAdviceItemResponse {
  id: string;
  lineNumber: number;
  productCode: string | null;
  description: string;
  quantity: number;
  unitCode: string;
}

// === Batch C: Retentions ===
export interface RetentionResponse {
  id: string;
  serie: string;
  correlative: number;
  fullNumber: string;
  issueDate: string;
  supplierDocType: string;
  supplierDocNumber: string;
  supplierName: string;
  regimeCode: string;
  retentionPercent: number;
  totalInvoiceAmount: number;
  totalRetained: number;
  totalPaid: number;
  currency: string;
  status: string;
  sunatResponseCode: string | null;
  sunatResponseDescription: string | null;
  xmlUrl: string | null;
  pdfUrl: string | null;
  createdAt: string;
  references: RetentionReferenceResponse[];
}

export interface RetentionReferenceResponse {
  id: string;
  documentType: string;
  documentNumber: string;
  documentDate: string;
  invoiceAmount: number;
  paymentDate: string;
  paymentAmount: number;
  retainedAmount: number;
  netPaidAmount: number;
}

// === Batch C: Perceptions ===
export interface PerceptionResponse {
  id: string;
  serie: string;
  correlative: number;
  fullNumber: string;
  issueDate: string;
  customerDocType: string;
  customerDocNumber: string;
  customerName: string;
  regimeCode: string;
  perceptionPercent: number;
  totalInvoiceAmount: number;
  totalPerceived: number;
  totalCollected: number;
  currency: string;
  status: string;
  sunatResponseCode: string | null;
  sunatResponseDescription: string | null;
  xmlUrl: string | null;
  pdfUrl: string | null;
  createdAt: string;
  references: PerceptionReferenceResponse[];
}

export interface PerceptionReferenceResponse {
  id: string;
  documentType: string;
  documentNumber: string;
  documentDate: string;
  invoiceAmount: number;
  collectionDate: string;
  collectionAmount: number;
  perceivedAmount: number;
  totalCollectedAmount: number;
}

// === Batch C: Quotations ===
export interface QuotationResponse {
  id: string;
  quotationNumber: string;
  correlative: number;
  issueDate: string;
  validUntil: string;
  customerDocType: string;
  customerDocNumber: string;
  customerName: string;
  customerEmail: string | null;
  currency: string;
  subtotal: number;
  igv: number;
  total: number;
  status: string;
  invoiceDocumentId: string | null;
  invoiceDocumentNumber: string | null;
  pdfUrl: string | null;
  notes: string | null;
  createdAt: string;
  items: QuotationItemResponse[];
}

export interface QuotationItemResponse {
  sequence: number;
  productCode: string | null;
  description: string;
  quantity: number;
  unitMeasure: string;
  unitPrice: number;
  igvType: string;
  igvAmount: number;
  subtotal: number;
  total: number;
}

// === Batch C: Recurring Invoices ===
export interface RecurringInvoiceResponse {
  id: string;
  documentType: string;
  serie: string;
  customerDocType: string;
  customerDocNumber: string;
  customerName: string;
  customerEmail: string | null;
  currency: string;
  frequency: string;
  dayOfMonth: number | null;
  dayOfWeek: number | null;
  startDate: string;
  endDate: string | null;
  nextEmissionDate: string | null;
  status: string;
  emittedCount: number;
  lastEmittedDate: string | null;
  notes: string | null;
  createdAt: string;
}
