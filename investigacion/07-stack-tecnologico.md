# 07 - Stack Tecnológico y Decisiones

## Stack Confirmado

| Capa | Tecnología | Versión | Justificación |
|------|-----------|---------|---------------|
| Core API | C# / ASP.NET Core | .NET 10 LTS (10.0.3) | Equipo lo conoce, mejor soporte XML/SOAP, middleware pipeline, DI nativo |
| AI Service | Python / FastAPI | 3.12+ / latest | Ecosistema IA sin rival, WebSocket nativo, async first |
| Frontend | Next.js | 16.x (latest) | App Router, SSR, middleware auth, Server Components |
| Database | PostgreSQL | 18 | RLS nativo para multi-tenant, JSONB, performance |
| Document Storage | MinIO | Latest | S3-compatible, Docker native, para XML/PDF/CDR |
| Message Queue | NATS | Latest + JetStream | Persistencia, Queue Groups, clientes .NET y Python |
| Containers | Docker + Compose | Latest | Desarrollo local + staging |
| CI/CD | GitHub Actions | — | CI básico para MVP |

## Decisiones Técnicas con Justificación

### ¿Por qué .NET 10 y no Rust?
- **Equipo**: 2-3 devs que ya conocen .NET. Nadie conoce UBL 2.1.
- **XML/SOAP**: C# tiene System.Xml, XmlSerializer, WCF/SOAP client nativos. Rust requiere crates limitados.
- **Curva**: UBL 2.1 ya es complejo. Rust + UBL = doble curva de aprendizaje = proyecto muerto.
- **Timeline**: .NET → MVP en ~4 meses. Rust → ~8-10 meses.
- **Rust futuro**: Puede entrar después para firma digital de alto rendimiento o validador XML optimizado.

### ¿Por qué PostgreSQL 18 y no otro?
- **RLS (Row Level Security)**: Multi-tenant sin necesidad de múltiples DBs. CREATE POLICY con USING/WITH CHECK.
- **JSONB**: Flexible para metadata, leyendas, propiedades adicionales de ítems.
- **Madurez**: 30+ años, confiable, extensible.
- **Docker**: Imagen oficial con health checks (pg_isready).

### ¿Por qué NATS y no RabbitMQ?
- **JetStream**: Persistencia nativa sin plugins.
- **Performance**: Millones de mensajes por segundo.
- **Clientes**: NATS.Net oficial para .NET, nats.py para Python.
- **Queue Groups**: Load balancing automático entre workers.
- **Simplicidad**: Menos overhead operativo que RabbitMQ.

### ¿Por qué MinIO y no S3 directo?
- **Local development**: Funciona en Docker sin cuenta AWS.
- **S3-compatible**: Mismo SDK, migrar a S3 real cuando escale.
- **Control**: Datos en tu infraestructura, no en terceros.

### ¿Por qué Next.js y no SvelteKit?
- **Ecosistema**: Más componentes, más librerías UI, más devs disponibles.
- **Auth**: Middleware nativo para route protection.
- **SSR**: Server Components para dashboard con datos sensibles.
- **Vercel**: Deployment trivial cuando quieras ir a producción rápido.

### ¿Por qué FastAPI separado para IA?
- **Aislamiento**: IA es un servicio independiente. Si falla, facturación sigue funcionando.
- **Ecosystem**: LangChain, OpenAI SDK, Anthropic SDK — todo en Python.
- **WebSocket**: FastAPI tiene WebSocket nativo con Depends() para DI.
- **Escalamiento**: IA se escala independiente del core.

## Librerías Clave por Servicio

### .NET 10 (Core API)
- `Microsoft.AspNetCore` — Web API
- `Npgsql.EntityFrameworkCore.PostgreSQL` — EF Core para PostgreSQL
- `System.Xml.Linq` — Generación XML UBL 2.1
- `System.Security.Cryptography.Xml` — Firma digital XMLDSig
- `NATS.Net` — Cliente NATS JetStream
- `Minio` — Cliente MinIO S3
- `Swashbuckle.AspNetCore` — Swagger/OpenAPI
- `BCrypt.Net-Next` — Hashing de passwords
- `System.IdentityModel.Tokens.Jwt` — JWT auth

### Python (AI Service)
- `fastapi` — API + WebSocket
- `uvicorn` — ASGI server
- `openai` — SDK OpenAI
- `anthropic` — SDK Anthropic
- `nats-py` — Cliente NATS
- `sqlalchemy` — ORM para PostgreSQL (read-only)
- `pydantic` — Validación de datos

### Next.js (Frontend)
- `next` — Framework
- `tailwindcss` — Styling
- `shadcn/ui` — Componentes UI
- `next-auth` o custom JWT — Auth
- `react-query` / `swr` — Data fetching
- `recharts` — Gráficos del dashboard
