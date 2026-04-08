export interface TukiFactConfig {
  baseUrl?: string;
  apiKey?: string;
  accessToken?: string;
  tenantId?: string;
  timeout?: number;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: { id: string; tenantId: string; email: string; fullName: string | null; role: string };
}

export interface CreateDocumentRequest {
  documentType: '01' | '03';
  serie: string;
  currency?: string;
  issueDate?: string;
  dueDate?: string;
  customerDocType: string;
  customerDocNumber: string;
  customerName: string;
  customerAddress?: string;
  customerEmail?: string;
  notes?: string;
  purchaseOrder?: string;
  items: CreateDocumentItemRequest[];
}

export interface CreateDocumentItemRequest {
  productCode?: string;
  sunatProductCode?: string;
  description: string;
  quantity: number;
  unitMeasure?: string;
  unitPrice: number;
  igvType?: '10' | '20' | '30';
  discount?: number;
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

export interface CreateCreditNoteRequest {
  serie: string;
  referenceDocumentId: string;
  creditNoteReason: string;
  description?: string;
  currency?: string;
  items: CreateDocumentItemRequest[];
}

export interface PaginatedResponse<T> {
  data: T[];
  pagination: { page: number; pageSize: number; totalCount: number; totalPages: number };
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
  id: string; documentType: string; serie: string; currentCorrelative: number;
  emissionPoint: string; isActive: boolean; createdAt: string;
}

export interface PlanResponse {
  id: string; name: string; priceMonthly: number; maxDocumentsPerMonth: number;
  features: Record<string, unknown>; isActive: boolean;
}
