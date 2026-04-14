# Mega-Sprint 3: Backoffice Pro

> **Tema**: Operaciones SaaS — Impersonar, MRR, Suscripciones, Config Global
> **Estimacion**: ~2-3 dias
> **Prioridad**: MEDIA — para operar como SaaS de verdad

---

## Contexto

Ya existe:
- `BackofficeController.cs` — dashboard global, tenants CRUD, documents search, employees list
- `BackofficeAuthController.cs` — login separado para platform_users
- `PlatformUser.cs` — entity con roles (superadmin, support, ops)
- 8 paginas backoffice frontend completas
- 9 endpoints backoffice operativos

Lo que FALTA: herramientas avanzadas para operar la plataforma.

---

## Tareas

### M3.1 — CRUD Empleados Plataforma (MEDIO)
**Dependencias**: ninguna
**Archivos a crear/modificar**:
- `src/TukiFact.Api/Controllers/BackofficeController.cs` — agregar POST/PUT/DELETE employees
- `src/tukifact-web/src/app/backoffice/(panel)/employees/page.tsx` — agregar dialogs

**Endpoints nuevos**:
```
POST   /v1/backoffice/employees          — crear empleado
PUT    /v1/backoffice/employees/{id}     — editar empleado
DELETE /v1/backoffice/employees/{id}     — desactivar empleado
PUT    /v1/backoffice/employees/{id}/role — cambiar rol
```

**Roles plataforma**:
```
superadmin  — todo (CRUD tenants, empleados, config global)
support     — ver tenants, impersonar, buscar documentos (NO puede suspender)
ops         — ver dashboard, reportes (solo lectura)
billing     — gestionar suscripciones, cobros (nuevo rol)
```

**Frontend**:
- Dialog "Nuevo Empleado" (nombre, email, password, rol)
- Tabla con acciones: editar, cambiar rol, desactivar
- Solo superadmin puede crear/editar empleados

### M3.2 — Impersonar Tenant (MEDIO)
**Dependencias**: ninguna
**Archivos a crear/modificar**:
- `src/TukiFact.Api/Controllers/BackofficeController.cs` — endpoint impersonate
- `src/TukiFact.Infrastructure/Services/JwtService.cs` — generar JWT con tenant_id temporal
- `src/tukifact-web/src/app/backoffice/(panel)/tenants/[id]/page.tsx` — boton impersonar

**Endpoint**:
```
POST /v1/backoffice/tenants/{id}/impersonate
  → Genera JWT temporal (15 min TTL) con:
    - tenant_id del tenant seleccionado
    - platform_user_id del admin que impersona
    - claim "impersonating": true
    - role: "admin" (del tenant)
  → Retorna: { token, expires_at, tenant_name, redirect_url }
```

**Seguridad**:
- Solo superadmin y support pueden impersonar
- JWT con TTL corto (15 min)
- AuditLog registra quien impersono a quien
- Banner visible en UI: "Estas viendo como [Empresa X]" con boton "Volver al backoffice"
- NO puede: cambiar password, eliminar datos, cambiar plan

**Frontend flow**:
```
Backoffice → Detalle Tenant → Boton "Ver como este tenant"
  → Redirect al portal tenant con JWT temporal
  → Banner amarillo arriba: "Impersonando: Empresa X (quedan 14:32)"
  → Boton "Volver al backoffice"
```

### M3.3 — Logs Actividad Backoffice (BAJO)
**Dependencias**: ninguna
**Archivos a crear/modificar**:
- `src/TukiFact.Domain/Entities/PlatformAuditLog.cs` — nueva entidad
- `src/TukiFact.Api/Controllers/BackofficeController.cs` — endpoint logs
- `src/tukifact-web/src/app/backoffice/(panel)/activity/page.tsx` — nueva pagina

**Que registra**:
```
- Login backoffice (quien, cuando, IP)
- Impersonacion (quien impersono a que tenant)
- Cambio de plan de tenant
- Suspension/activacion de tenant
- CRUD empleados
- Cambios en config global
```

**Endpoint**:
```
GET /v1/backoffice/activity-log?page=1&pageSize=30&action=impersonate&user_id=...
```

### M3.4 — Reportes Plataforma (MEDIO)
**Dependencias**: ninguna
**Archivos a crear/modificar**:
- `src/TukiFact.Api/Controllers/BackofficeController.cs` — endpoint reports
- `src/tukifact-web/src/app/backoffice/(panel)/reports/page.tsx` — nueva pagina

**Metricas SaaS**:
```
MRR (Monthly Recurring Revenue):
  - Total MRR = SUM(plan.price) de tenants activos
  - MRR por plan (desglosado)
  - MRR trend (ultimos 12 meses)

Churn:
  - Tenants que cancelaron este mes
  - Churn rate = cancelados / activos inicio mes
  - Revenue churn = MRR perdido

Growth:
  - Nuevos tenants este mes
  - Net revenue = new MRR - churned MRR
  - Growth rate

Usage:
  - Documentos emitidos hoy/semana/mes (global)
  - Top 10 tenants por volumen
  - Documentos por tipo (pie chart)
  - Uso vs limite de plan (quien esta cerca del limite)
```

**Endpoint**:
```
GET /v1/backoffice/reports/mrr          — MRR actual + historico
GET /v1/backoffice/reports/churn        — churn metrics
GET /v1/backoffice/reports/growth       — growth metrics
GET /v1/backoffice/reports/usage        — usage global + top tenants
```

### M3.5 — Gestion Suscripciones (ALTO)
**Dependencias**: ninguna
**Archivos a crear/modificar**:
- `src/TukiFact.Domain/Entities/Subscription.cs` — nueva entidad
- `src/TukiFact.Domain/Entities/Invoice.cs` — factura de plataforma (no SUNAT, billing interno)
- `src/TukiFact.Infrastructure/Services/SubscriptionService.cs`
- `src/TukiFact.Infrastructure/Services/BillingScheduler.cs` — BackgroundService
- `src/TukiFact.Api/Controllers/BackofficeController.cs` — endpoints suscripciones

**Subscription Entity**:
```csharp
public class Subscription
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public string Status { get; set; }        // active, past_due, cancelled, trial
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public DateTimeOffset NextBillingDate { get; set; }
    public decimal MonthlyAmount { get; set; }
    public int DocumentsUsedThisMonth { get; set; }
    public int DocumentsLimit { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

**Flujos**:
```
Nuevo tenant → plan Free → sin cobro
Upgrade a pago → crear Subscription + proximo cobro en 30d
Downgrade → cambio al final del periodo actual
Limite alcanzado → bloquear emision + notificacion + email
Pago vencido → 3 dias gracia → suspender tenant
Cancelacion → activo hasta fin periodo → luego read-only
```

**Nota**: Integracion con pasarela de pago (Stripe/MercadoPago) es POST-DEPLOY.
Por ahora: gestion manual desde backoffice (marcar pagado, extender, etc.)

### M3.6 — Config Global Plataforma (BAJO)
**Dependencias**: ninguna
**Archivos a crear/modificar**:
- `src/TukiFact.Domain/Entities/PlatformConfig.cs` — nueva entidad (key-value)
- `src/TukiFact.Api/Controllers/BackofficeController.cs` — endpoints config
- `src/tukifact-web/src/app/backoffice/(panel)/settings/page.tsx` — nueva pagina

**Settings globales**:
```
maintenance_mode: false          — modo mantenimiento (503 para todos)
registration_enabled: true       — permitir registro nuevos tenants
default_plan: "free"             — plan por defecto al registrar
max_free_documents: 50           — limite documentos plan free
trial_days: 14                   — dias de prueba planes pago
sunat_beta_mode: true            — forzar beta para todos (testing)
email_provider: "resend"         — proveedor email plataforma
support_email: "soporte@tukifact.net.pe"
```

**Frontend**: formulario con los settings, solo superadmin.

---

## Criterios de Completado

- [ ] CRUD completo de empleados plataforma con 4 roles
- [ ] Impersonar tenant funciona con JWT temporal + banner + auditoria
- [ ] Activity log registra todas las acciones del backoffice
- [ ] Dashboard reportes con MRR, churn, growth, usage
- [ ] Subscription entity y flujos basicos (sin pasarela aun)
- [ ] Config global editable desde backoffice
- [ ] 6+ nuevas paginas backoffice frontend
- [ ] 0 errores lint, 0 warnings
- [ ] Build limpio frontend + backend

---

## Nuevas Paginas Backoffice

```
/backoffice/employees     — CRUD empleados (upgrade de la existente)
/backoffice/activity      — logs de actividad
/backoffice/reports       — MRR, churn, growth, usage
/backoffice/subscriptions — gestionar suscripciones
/backoffice/settings      — config global plataforma
```
