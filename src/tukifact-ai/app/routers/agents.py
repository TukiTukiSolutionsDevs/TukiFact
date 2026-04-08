"""AI Agent endpoints — Validate, Classify, Extract, Chat, Analyze, Reconcile."""

from fastapi import APIRouter, HTTPException
from pydantic import BaseModel

from app.agents.validator import ValidatorAgent
from app.agents.classifier import ClassifierAgent
from app.agents.extractor import ExtractorAgent
from app.agents.copilot import CopilotAgent
from app.agents.analyst import AnalystAgent
from app.agents.conciliator import ConciliatorAgent

router = APIRouter()

validator = ValidatorAgent()
classifier = ClassifierAgent()
extractor = ExtractorAgent()
copilot = CopilotAgent()
analyst = AnalystAgent()
conciliator = ConciliatorAgent()


# === Validator Agent ===

class ValidateRequest(BaseModel):
    document_type: str
    serie: str
    customer_doc_type: str
    customer_doc_number: str
    customer_name: str
    items: list[dict]
    currency: str = "PEN"


class ValidationResult(BaseModel):
    is_valid: bool
    errors: list[str]
    warnings: list[str]
    suggestions: list[str]


@router.post("/validate", response_model=ValidationResult)
async def validate_document(request: ValidateRequest):
    """Valida un comprobante antes de emitirlo. Detecta errores comunes."""
    result = await validator.validate(request.model_dump())
    return result


# === Classifier Agent ===

class ClassifyRequest(BaseModel):
    description: str
    unit_price: float | None = None
    customer_doc_type: str | None = None


class ClassificationResult(BaseModel):
    igv_type: str
    igv_type_name: str
    suggested_unit_measure: str
    suggested_sunat_code: str | None
    confidence: float
    reasoning: str


@router.post("/classify", response_model=ClassificationResult)
async def classify_item(request: ClassifyRequest):
    """Clasifica un ítem: tipo IGV, unidad de medida, código SUNAT."""
    result = await classifier.classify(request.model_dump())
    return result


# === Extractor Agent ===

class ExtractRequest(BaseModel):
    text: str  # OCR text or raw invoice text
    source_type: str = "text"  # "text", "ocr", "email"


class ExtractedDocument(BaseModel):
    document_type: str | None
    customer_doc_number: str | None
    customer_name: str | None
    customer_address: str | None
    items: list[dict]
    total: float | None
    currency: str
    confidence: float
    raw_fields: dict


@router.post("/extract", response_model=ExtractedDocument)
async def extract_document(request: ExtractRequest):
    """Extrae datos de un texto/OCR para crear un comprobante."""
    result = await extractor.extract(request.text, request.source_type)
    return result


# === Copilot Agent ===

class ChatRequest(BaseModel):
    message: str
    context: dict | None = None


class ChatResponse(BaseModel):
    response: str
    sources: list[str]
    confidence: float
    suggestions: list[str]


@router.post("/chat", response_model=ChatResponse)
async def chat(request: ChatRequest):
    """Asistente de facturación electrónica. Pregunta lo que necesites."""
    result = await copilot.chat(request.message, request.context)
    return result


# === Analyst Agent ===

class AnalyzeRequest(BaseModel):
    dashboard_data: dict


class AnalysisResult(BaseModel):
    insights: list[dict]
    recommendations: list[str]
    alerts: list[str]
    summary: str


@router.post("/analyze", response_model=AnalysisResult)
async def analyze(request: AnalyzeRequest):
    """Analiza datos de facturación y genera insights inteligentes."""
    result = await analyst.analyze(request.dashboard_data)
    return result


# === Conciliator Agent ===

class ReconcileRequest(BaseModel):
    documents: list[dict]
    payments: list[dict]
    tolerance: float = 0.05


class ReconcileResult(BaseModel):
    matched: list[dict]
    partial_matches: list[dict]
    unmatched_documents: list[dict]
    unmatched_payments: list[dict]
    summary: dict


@router.post("/reconcile", response_model=ReconcileResult)
async def reconcile(request: ReconcileRequest):
    """Cruza documentos emitidos con pagos bancarios."""
    result = await conciliator.reconcile(request.documents, request.payments, request.tolerance)
    return result
