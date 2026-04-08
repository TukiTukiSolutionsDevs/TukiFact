# TukiFact — TestSprite Configuration

## Application URLs

| Service | URL | Type |
|---------|-----|------|
| **Frontend** | `http://localhost:3000` | Next.js 16 (App Router) |
| **Backend API** | `http://localhost:5100` | .NET 10 ASP.NET Core |
| **AI Service** | `http://localhost:8090` | Python FastAPI |
| **API Documentation** | `http://localhost:5100/scalar/v1` | Scalar (OpenAPI 3.1) |
| **AI Documentation** | `http://localhost:8090/docs` | Swagger/FastAPI |

## Authentication Credentials

### Admin (full access)
```
Tenant ID: fe044833-c789-41cc-b428-4fe1dde61f7e
Email:     admin@tukifact.net.pe
Password:  TukiAdmin2026!
Role:      admin
```

### Emisor (can emit documents)
```
Tenant ID: fe044833-c789-41cc-b428-4fe1dde61f7e
Email:     carlos@tukifact.net.pe
Password:  Carlos2026!
Role:      emisor
```

### Consulta (read-only)
```
Tenant ID: fe044833-c789-41cc-b428-4fe1dde61f7e
Email:     maria@tukifact.net.pe
Password:  Maria2026!
Role:      consulta
```

## Login Flow

All requests to protected endpoints require JWT authentication:

```bash
# 1. Login to get JWT
curl -X POST http://localhost:5100/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@tukifact.net.pe",
    "password": "TukiAdmin2026!",
    "tenantId": "fe044833-c789-41cc-b428-4fe1dde61f7e"
  }'
# Returns: { "accessToken": "eyJ...", "refreshToken": "...", "user": {...} }

# 2. Use JWT in subsequent requests
curl -H "Authorization: Bearer {accessToken}" http://localhost:5100/v1/documents
```

## Frontend Login Steps

1. Navigate to `http://localhost:3000/login`
2. Enter **Tenant ID**: `fe044833-c789-41cc-b428-4fe1dde61f7e`
3. Enter **Email**: `admin@tukifact.net.pe`
4. Enter **Password**: `TukiAdmin2026!`
5. Click "Iniciar Sesión"
6. Redirects to `/dashboard`

## Key Test Scenarios

### Critical Path: Emit Invoice (E2E)
1. Login as admin
2. Navigate to `/documents/new`
3. Select Type: Factura, Serie: F001
4. Enter customer: RUC=20100047218, Name=SODIMAC PERU S.A.
5. Add item: "Servicio de consultoría", Qty=1, Price=1000, IGV=Gravado
6. Click "Emitir Comprobante"
7. Verify: redirects to document detail, status=accepted, hash present

### Critical Path: Credit Note
1. Open an accepted factura detail (`/documents/{id}`)
2. Click "Emitir Nota de Crédito"
3. Select reason: "06 - Devolución total"
4. Verify items pre-populated
5. Emit → status=accepted

### Critical Path: Void Document
1. Open an accepted document detail
2. Click "Anular" (admin only)
3. Enter reason: "Error en datos"
4. Confirm → status changes to voided

### RBAC Verification
- Emisor CAN emit documents
- Emisor CANNOT access /users, /api-keys, /settings admin features
- Consulta CANNOT emit documents, only view
- Admin CAN do everything

### Certificate Upload
1. Go to `/settings`
2. Upload `tests/Certificados/Key_cert.pem` (no password needed)
3. Verify "Certificado configurado" shows with expiry date
4. Emit a new document → verify hash is present in response

## Existing Test Data

| Document | Customer | Amount | Status |
|----------|----------|--------|--------|
| F001-00000001 | SODIMAC PERU S.A. | S/ 26,078.00 | accepted |
| F001-00000002 | FALABELLA PERU S.A. | S/ 8,599.84 | accepted |
| F001-00000003 | INTERCORP RETAIL S.A. | USD 5,782.00 | accepted |
| B001-00000001 | JUAN PÉREZ GARCÍA | S/ 2,124.00 | accepted |

## API Endpoint Summary (35+ endpoints)

### Public (no auth)
- `GET /api/ping`
- `GET /health`, `/health/ready`, `/health/live`
- `GET /v1/plans`
- `POST /v1/auth/register`
- `POST /v1/auth/login`
- `POST /v1/auth/refresh`

### Authenticated (any role)
- `GET /v1/auth/me`
- `GET /v1/dashboard`
- `GET /v1/documents`, `GET /v1/documents/{id}`
- `GET /v1/documents/{id}/pdf`, `GET /v1/documents/{id}/xml`
- `GET /v1/series`

### Emisor + Admin
- `POST /v1/documents` (emit)
- `POST /v1/documents/credit-note`
- `POST /v1/documents/debit-note`

### Admin Only
- `CRUD /v1/users`
- `CRUD /v1/api-keys`
- `CRUD /v1/webhooks`
- `GET /v1/audit-log`
- `GET/PUT /v1/tenant`
- `POST/DELETE /v1/tenant/certificate`
- `PUT /v1/tenant/environment`
- `CRUD /v1/series`
- `POST /v1/voided-documents`

### AI Service (port 8090)
- `POST /v1/ai/validate`
- `POST /v1/ai/classify`
- `POST /v1/ai/extract`
- `POST /v1/ai/chat`
- `POST /v1/ai/analyze`
- `POST /v1/ai/reconcile`
- `GET /health`, `GET /health/ready`
