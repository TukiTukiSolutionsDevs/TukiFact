// ─── Enums ────────────────────────────────────────────────────────────────────

export type DocumentType = 'factura' | 'boleta' | 'nota_credito' | 'nota_debito';

export type DocumentStatus = 'pending' | 'sent' | 'accepted' | 'rejected' | 'voided';

export type CustomerDocumentType = 'RUC' | 'DNI' | 'CE' | 'PASAPORTE';

// ─── Config ───────────────────────────────────────────────────────────────────

export interface TukiFactConfig {
  apiKey: string;
  /** Override base URL. Defaults to https://api.tukifact.net.pe */
  baseUrl?: string;
  /** API version. Defaults to 'v1' */
  version?: 'v1';
  /** Request timeout in ms. Defaults to 30000 */
  timeout?: number;
  /** Use sandbox environment (https://sandbox.tukifact.net.pe) */
  sandbox?: boolean;
}

// ─── Errors ───────────────────────────────────────────────────────────────────

export interface ApiError {
  code: string;
  message: string;
  details?: Record<string, unknown>;
}

// ─── Pagination ───────────────────────────────────────────────────────────────

export interface PaginatedResponse<T> {
  data: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// ─── Documents ────────────────────────────────────────────────────────────────

export interface DocumentItem {
  description: string;
  quantity: number;
  unitPrice: number;
  unitCode?: string;
  igvRate?: number;
  discount?: number;
}

export interface DocumentCreateRequest {
  type: DocumentType;
  series: string;
  customerId: string;
  /** ISO 8601 date string (YYYY-MM-DD) */
  issueDate: string;
  items: DocumentItem[];
  dueDate?: string;
  currency?: string;
  notes?: string;
  referenceDocumentId?: string;
}

export interface SunatResponse {
  accepted: boolean;
  code?: string;
  description?: string;
  observations?: string[];
}

export interface Document {
  id: string;
  type: DocumentType;
  series: string;
  correlative: number;
  /** e.g. "F001-00000001" */
  fullNumber: string;
  customerId: string;
  issueDate: string;
  currency: string;
  subtotal: number;
  igv: number;
  total: number;
  status: DocumentStatus;
  items: DocumentItem[];
  createdAt: string;
  updatedAt: string;
  dueDate?: string;
  notes?: string;
  referenceDocumentId?: string;
}

export interface DocumentResponse {
  document: Document;
  sunat?: SunatResponse;
}

// ─── Customers ────────────────────────────────────────────────────────────────

export interface Customer {
  id: string;
  documentType: CustomerDocumentType;
  documentNumber: string;
  legalName: string;
  createdAt: string;
  updatedAt: string;
  tradeName?: string;
  address?: string;
  district?: string;
  province?: string;
  department?: string;
  email?: string;
  phone?: string;
}

// ─── Series ───────────────────────────────────────────────────────────────────

export interface Series {
  id: string;
  type: DocumentType;
  /** e.g. "F001", "B001" */
  prefix: string;
  currentCorrelative: number;
  isActive: boolean;
}
