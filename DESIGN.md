# TukiFact — DESIGN.md

> **Audiencia:** Cloud Design / Stitch · Claude Code · diseñadores humanos.
>
> **Propósito:** Especificación completa pantalla-por-pantalla del portal cliente (tenant portal) de TukiFact. Para cada ruta autenticada lista los campos reales que expone el backend, los endpoints REST que consume, el modelo de validación, los servicios externos invocados (firma XML, envío SUNAT, lookup RUC/DNI, tipo de cambio, email, MinIO, NATS), los estados del flujo, los edge cases y la UI propuesta siguiendo el sistema de diseño establecido.
>
> **Cómo usar este documento:**
> 1. Si vas a generar UI nueva → ubica la ruta en la **sección 8** (Inventario por pantalla) y arma la pantalla a partir de los **bloques pre-armados** definidos en la sección 1-7.
> 2. Si vas a refactorizar UI existente → cruza los campos del backend de la sección 8 contra los inputs de la pantalla actual; lo que falte en el FE pero exista en el backend es deuda visible.
> 3. Si vas a pedir un cambio al backend → la sección 8 indica qué controllers/services/entities tocar (con `file:line` exacto).
>
> **Convenciones:**
> - Idioma del copy de UI: **español Perú (`es-PE`)**.
> - Identificadores de código en inglés.
> - Rutas relativas al repo root `/Users/soulkin/Documents/TukiFact/`.
> - Mapeo de tokens visuales a CSS vars vive en `DESIGN-CLIENT.md` (sección 2). Este documento NO duplica tokens — solo los referencia.
>
> **Estado del audit:** 2026-05-29. Tras este audit, las 22 rutas del portal cliente quedan inventariadas con su contraparte de backend. Cualquier divergencia futura (campo nuevo, endpoint nuevo, estado nuevo) debe versionarse en este documento.

---

## Contenido

1. **Convenciones globales** — idioma, mono/tnum, estados, tabla nativa, loading, confirmaciones, toasts.
2. **Layout primitives** — `<PageHeader>`, `<FilterToolbar>`, `<DataTable>` (patrón), `<PaginationFooter>`.
3. **Form primitives** — `<Section>`, `<PillGroup>`, `<StickySummary>`, auto-bridge numérico, SUNAT lookup, items table.
4. **Status primitives** — `<StatusBadge>`, `<KpiCard>`, auto-refresh banner.
5. **Empty states** — inicial / filtrado / cargando.
6. **Layout patterns por tipo de pantalla** — lista+modal, lista+full-page form, detalle SUNAT, configuración, read-only timeline.
7. **Inputs especiales** — numérico con moneda, fecha con icono, etc.
8. **Inventario por pantalla** (22 rutas, agrupadas):
   - **Grupo A — Emisión SUNAT core**: `/documents`, `/documents/new`, `/documents/[id]`, `/despatch-advices`, `/despatch-advices/new`, `/despatch-advices/[id]`, `/recurring-invoices`, `/recurring-invoices/new`
   - **Grupo B — Cotizaciones, percepciones, retenciones, anulados, tipos de cambio**: `/quotations` (+new+detail), `/perceptions` (+new+detail), `/retentions` (+new+detail), `/voided`, `/exchange-rates`
   - **Grupo C — Catálogo, reportes, dashboard, welcome**: `/dashboard`, `/welcome`, `/products`, `/customers`, `/series`, `/catalogs`, `/reports`
   - **Grupo D — Integración y administración**: `/api-keys`, `/webhooks`, `/ai`, `/users`, `/certificate`, `/plan`, `/audit-log`, `/settings`

---

# DESIGN.md — Contratos de componentes y patrones

> Este documento complementa `DESIGN-CLIENT.md` (sistema de diseño, marca, tokens, shell). Aquí se documentan los **patrones de pantalla y los componentes de alto nivel** que la app cliente usa para resolver casos repetidos (formularios largos, tablas con filtros, inventarios CRUD, flujos SUNAT con sticky summary, etc.).
>
> Para Cloud Design / Stitch: usa este archivo como **biblioteca de bloques pre-armados**. Cada bloque tiene contrato declarativo (props), pinta visual y código TSX de referencia. Cuando se diseñe una pantalla nueva, primero se compone con estos bloques antes de inventar uno nuevo.

---

## 0. Convenciones globales

- **Idioma:** todo el copy va en español Perú (`es-PE`). "vos" NO. "tú" y "usted" en contextos formales (legales, errores). Lenguaje fiscal cuando aplique: RUC, boleta, factura, comprobante, guía, SUNAT.
- **Mono y tnum**: TODOS los números (correlativos, RUC, fechas, totales) usan `mono` + `tnum` para alinearse en columnas.
- **Estados primarios de comprobante**: `draft` · `signed` · `sent` · `accepted` · `rejected` · `voided` · `pending_ticket`. Mapeo a colores en `StatusBadge`.
- **Tabla = `<table>` nativo**, no shadcn `<Table>`. Headers en `t-overline` con `background: var(--muted)`. Filas con `borderTop: 1px solid var(--border)`. Esto es más performante y permite tnum sin overrides.
- **Loading = `Loader2 animate-spin`**, no `Skeleton`. El skeleton genera CLS en filas con altura variable.
- **Confirmación destructiva** → `confirm(\`¿Eliminar X "$nombre"?\`)` nativo. Modal de confirm solo cuando hay 2+ campos a llenar.
- **Toasts** → `sonner` (`import { toast } from 'sonner'`). 4 s default, 7 s errores. Mensaje en una línea, sin punto final.

---

## 1. Layout primitives

### 1.1 `<PageHeader>`

Encabezado canónico de cada ruta autenticada.

**Props (contrato):**
```ts
{
  title: string;          // h1 — usa <PageHeader.Title> internamente
  description?: string;   // body muted debajo del título
  actions?: ReactNode;    // botón(es) a la derecha — primary action típicamente
}
```

**Visual:**
- Container: `flex items-start justify-between gap-4 flex-wrap mb-6`
- Título: `<h1 class="t-display-lg m-0">{title}</h1>`
- Descripción: `<p class="t-body mt-1.5 mb-0" style="color: var(--muted-foreground)">{description}</p>`
- Acciones: alineadas a la derecha, wrap en mobile

**Ejemplo:**
```tsx
<PageHeader
  title="Catálogo de productos"
  description={`${count} productos en tu catálogo.`}
  actions={
    <Button style={{ background: 'var(--accent)', color: 'var(--accent-foreground)', fontWeight: 600 }}>
      <Plus className="h-4 w-4 mr-2" /> Nuevo producto
    </Button>
  }
/>
```

### 1.2 `<FilterToolbar>`

Toolbar de filtros — search + chips + opcionales (date range, sort, etc.).

**Props:**
```ts
{
  search?: { value: string; onChange: (v: string) => void; placeholder?: string };
  statusChips?: { value: string; label: string; active: boolean; onClick: () => void }[];
  dateRange?: { from: string; to: string; onChange: (from, to) => void };
  extras?: ReactNode;     // export, refresh, etc.
  onClear?: () => void;   // muestra "Limpiar" cuando algún filtro está activo
}
```

**Visual:**
- Card con `rounded-[var(--radius-lg)] border bg-card p-4 mb-[var(--gap-cards)]`
- Sombra `var(--shadow-xs)`
- Layout: `flex flex-wrap items-end gap-3`
- Search ocupa `flex-1 min-w-[200px]` con ícono `Search` leading
- Chips de status: pills `px-2.5 py-1.5` con fondo `color-mix(in oklch, var(--accent) 18%, transparent)` cuando activo
- Botón "Limpiar" `ghost sm` aparece solo si `onClear` está definido y hay filtros activos

### 1.3 `<DataTable>` (patrón, no componente)

Patrón canónico de tabla — implementado como `<table>` nativo, no envuelto en un componente.

**Estructura:**
```html
<section class="rounded-[var(--radius-lg)] border bg-card overflow-hidden mb-[var(--gap-cards)]"
         style="box-shadow: var(--shadow-xs)">
  <table class="w-full">
    <thead>
      <tr class="t-overline" style="color: var(--muted-foreground); background: var(--muted)">
        <th class="text-left py-2.5 pl-6 pr-2 w-XX">Header</th>
        ...
      </tr>
    </thead>
    <tbody>
      <tr style="border-top: 1px solid var(--border)">
        <td class="py-3 pl-6 pr-2">...</td>
      </tr>
    </tbody>
  </table>
</section>
```

**Reglas:**
- Header de columna numérica → `text-right`
- Cell numérico → `mono tnum text-right`
- Cell de ID/correlativo → `mono t-body-sm font-semibold`
- Cell con doble línea (nombre + sub-info) → `<div class="t-body-sm">Main</div><div class="t-caption" style="color: var(--muted-foreground)">Sub</div>`
- Last column = actions → `text-right`, ancho fijo (24 px por acción + 4 px gap)
- Cell de fecha → `mono tnum`

### 1.4 `<PaginationFooter>`

**Visual:**
```tsx
<div class="flex items-center justify-between flex-wrap gap-3">
  <p class="t-body-sm" style="color: var(--muted-foreground)">
    Página <span class="mono tnum font-semibold">{page}</span> de{' '}
    <span class="mono tnum font-semibold">{totalPages}</span> ·{' '}
    <span class="mono tnum">{totalCount}</span> total
  </p>
  <div class="flex gap-2">
    <Button variant="outline" size="sm" disabled={page <= 1} onClick={prev}>
      <ChevronLeft class="h-4 w-4 mr-1" /> Anterior
    </Button>
    <Button variant="outline" size="sm" disabled={page >= totalPages} onClick={next}>
      Siguiente <ChevronRight class="h-4 w-4 ml-1" />
    </Button>
  </div>
</div>
```

---

## 2. Form primitives

### 2.1 `<Section>` (form section card)

Container de sección dentro de un formulario largo.

```tsx
function Section({ title, desc, right, children }) {
  return (
    <section
      className="rounded-[var(--radius-lg)] border bg-card p-6"
      style={{ boxShadow: 'var(--shadow-xs)' }}
    >
      <div className="flex items-start justify-between gap-3 mb-4">
        <div>
          <h2 className="t-h2 m-0">{title}</h2>
          {desc && (
            <p className="t-body-sm m-0 mt-0.5" style={{ color: 'var(--muted-foreground)' }}>
              {desc}
            </p>
          )}
        </div>
        {right}
      </div>
      {children}
    </section>
  );
}
```

**Uso:** envuelve cada grupo lógico de un formulario largo. Ejemplos: `<Section title="Cliente">`, `<Section title="Detalles del traslado">`.

### 2.2 `<PillGroup>` — Selector de enum corto

Reemplaza `<Select>` para enums de ≤5 opciones donde el icono ayuda a discriminar.

**Props:**
```ts
{
  value: T;
  onChange: (v: T) => void;
  options: { value: T; label: string; sub?: string; icon: ElementType }[];
  cols?: 2 | 3 | 4 | 5;   // grid columns; mobile siempre 2
}
```

**Visual:**
- Grid `gap-2`
- Cada pill: `rounded-[var(--radius-md)] border px-3 py-2.5` con ícono 32 px en cuadrado de fondo accent cuando active
- Active: `background: color-mix(in oklch, var(--accent) 18%, transparent)` + `borderColor: var(--accent)`
- Inactive: `background: var(--card)` + `borderColor: var(--border)`

**Uso típico:**
- Tipo de documento (Factura/Boleta)
- Moneda (PEN/USD)
- Tipo de doc cliente (RUC/DNI/CE/Pasaporte/Sin doc)
- Frecuencia (Diaria/Semanal/Quincenal/Mensual/Anual)
- Tipo IGV (Gravado/Exonerado/Inafecto)
- Modalidad transporte (Privado/Público)

### 2.3 `<StickySummary>` — Resumen sticky derecho

Container del lado derecho en layouts de formulario 2/3 + 1/3.

**Visual:**
```tsx
<aside className="lg:col-span-1">
  <div className="rounded-[var(--radius-lg)] border bg-card p-6 lg:sticky lg:top-20"
       style={{ boxShadow: 'var(--shadow-xs)' }}>
    <h2 className="t-h2 m-0 mb-4">Resumen</h2>
    {/* Stack de datos clave + acciones grandes al fondo */}
  </div>
</aside>
```

**Contenido típico (de arriba a abajo):**
1. 2-3 datos clave del formulario (Cliente, Tipo, Modalidad)
2. Bloque destacado (Origen → Destino, Cadencia, etc.)
3. Mini-stats (cantidad de líneas, peso, total)
4. Tabla de totales (Gravado / IGV / Total)
5. Botón submit `size lg w-full h-12`
6. Botón cancelar `outline lg w-full h-12`
7. Microcopy explicando qué pasa después (`t-caption text-center`)

### 2.4 Auto-bridge numérico (helper UX)

Patrón para campos que se derivan entre sí.

**Ejemplo: precio sin IGV ↔ precio con IGV (IGV 18%)**

```tsx
const lastEditedRef = useRef<'net' | 'gross' | null>(null);

useEffect(() => {
  const rate = igvType === '10' ? 1.18 : 1;
  if (lastEditedRef.current === 'net' && !isNaN(net)) {
    setForm(f => ({ ...f, gross: (net * rate).toFixed(2) }));
  } else if (lastEditedRef.current === 'gross' && !isNaN(gross)) {
    setForm(f => ({ ...f, net: (gross / rate).toFixed(2) }));
  }
}, [net, gross, igvType]);
```

**Reglas:**
- Solo el campo editado por última vez calcula el otro (evita loop infinito).
- Si el tipo de IGV cambia → recalcular desde el último editado.
- Mostrar hint debajo: `"Editas uno y el otro se calcula con IGV 18%"` o `"Sin IGV: ambos precios son iguales"` según el modo.

### 2.5 SUNAT lookup integrado

Botón inline al lado del campo de número de documento que llama al proveedor configurado y autocompleta razón social + dirección.

**Layout:**
```tsx
<Label className="t-label mb-1.5 block">Número de documento</Label>
<div className="flex gap-2">
  <Input
    placeholder={docTypeLabel}
    value={docNumber}
    maxLength={docType === '6' ? 11 : 8}
    onChange={...}
    className="mono"
  />
  {(docType === '6' || docType === '1') && (
    <Button
      type="button"
      variant="outline"
      disabled={isLookingUp || !lookupStatus?.configured}
      title={lookupStatus?.configured ? `Buscar con ${lookupStatus.providerName}` : 'Configura un proveedor en Ajustes'}
      onClick={lookup}
    >
      {isLookingUp ? (
        <><Loader2 className="h-4 w-4 mr-2 animate-spin" /> Buscando…</>
      ) : (
        <><Search className="h-4 w-4 mr-2" /> Buscar</>
      )}
    </Button>
  )}
</div>
```

**API contract:**
- `GET /v1/services/lookup/status` → `{ configured, provider, providerName }`
- `GET /v1/services/lookup/ruc/{ruc}` → `{ name, address, ... }` (o variantes para DNI)
- En error mostrar `toast.error` apuntando a Ajustes → Servicios Externos si el provider no está configurado.

### 2.6 Items table dentro de form

Para forms que llevan una tabla de items (Documents, Quotations, Despatch Advices, Recurring Invoices).

**Estructura:**
```tsx
<Section title="Items" desc={`${items.length} ${items.length === 1 ? 'línea' : 'líneas'}`}
  right={<Button type="button" variant="outline" size="sm" onClick={addItem}>
    <Plus className="h-4 w-4 mr-1.5" /> Agregar línea
  </Button>}>
  <div className="-mx-6 overflow-x-auto">
    <table className="w-full">
      <thead>
        <tr className="t-overline" style={{ color: 'var(--muted-foreground)', background: 'var(--muted)' }}>
          <th className="text-left py-2.5 pl-6 pr-2 w-10">#</th>
          {/* columns */}
        </tr>
      </thead>
      <tbody>
        {items.map((item, idx) => (
          <tr key={idx} style={{ borderTop: '1px solid var(--border)' }}>
            <td className="py-3 pl-6 pr-2 t-body-sm tnum" style={{ color: 'var(--muted-foreground)' }}>
              {idx + 1}
            </td>
            {/* cells with inline <Input>/<Select> */}
            <td className="py-3 pl-2 pr-6 text-right">
              {items.length > 1 && (
                <button type="button" onClick={() => removeItem(idx)}
                        className="h-8 w-8 inline-flex items-center justify-center rounded-md hover:bg-[var(--muted)]"
                        style={{ color: 'var(--danger)' }}>
                  <Trash2 className="h-4 w-4" />
                </button>
              )}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  </div>
  <button type="button" onClick={addItem}
    className="mt-4 w-full rounded-[var(--radius-md)] border-2 border-dashed py-3 t-body-sm font-medium hover:bg-[var(--muted)] flex items-center justify-center gap-2"
    style={{ borderColor: 'var(--border)', color: 'var(--muted-foreground)' }}>
    <Plus className="h-4 w-4" /> Agregar otra línea
  </button>
</Section>
```

**Reglas:**
- `# `de fila es la primera columna, ancho 40 px, muted.
- Última columna = trash icon button, ancho 40 px, solo visible si `items.length > 1`.
- Bajo la tabla, dashed-border button para agregar línea (más descubrible que solo "+ Agregar arriba").

---

## 3. Status primitives

### 3.1 `<StatusBadge>`

Pill con color OKLCH `color-mix` + icono lucide. Único contrato visual para estados en toda la app.

**Mapping canónico:**
```ts
const STATUS: Record<string, { label: string; color: string; icon: ElementType }> = {
  // Comprobantes
  draft:          { label: 'Borrador',           color: 'var(--slate-500)', icon: FileText },
  signed:         { label: 'Firmado',            color: 'var(--info)',      icon: CheckCircle2 },
  sent:           { label: 'Enviado',            color: 'var(--warning)',   icon: Clock },
  accepted:       { label: 'Aceptado',           color: 'var(--success)',   icon: CheckCircle2 },
  rejected:       { label: 'Rechazado',          color: 'var(--danger)',    icon: XCircle },
  voided:         { label: 'Anulado',            color: 'var(--slate-500)', icon: Ban },
  pending_ticket: { label: 'Pendiente ticket',   color: 'var(--warning)',   icon: Clock },

  // Recurrentes
  active:         { label: 'Activa',             color: 'var(--success)',   icon: CheckCircle2 },
  paused:         { label: 'Pausada',            color: 'var(--warning)',   icon: Pause },
  cancelled:      { label: 'Cancelada',          color: 'var(--danger)',    icon: XCircle },
  completed:      { label: 'Completada',         color: 'var(--info)',      icon: CheckCircle2 },

  // Comunicaciones de baja
  pending:        { label: 'Pendiente ticket',   color: 'var(--warning)',   icon: Clock },
  error:          { label: 'Error',              color: 'var(--danger)',    icon: AlertTriangle },
};
```

**Render:**
```tsx
<span className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 t-caption font-semibold whitespace-nowrap"
      style={{
        color: info.color,
        background: `color-mix(in oklch, ${info.color} 14%, transparent)`,
      }}>
  <Icon className="h-3 w-3" />
  {info.label}
</span>
```

### 3.2 `<KpiCard>`

Card para métricas en dashboard / reports.

**Props:**
```ts
{
  label: string;      // pequeña, muted
  value: string;      // grande, mono tnum
  icon: ElementType;
  accent: string;     // CSS variable, ej. 'var(--success)'
  span?: 1 | 2;       // ocupa 1 o 2 columnas del grid
}
```

**Visual:**
- Container `rounded-[var(--radius-lg)] border bg-card p-5 flex items-center gap-3.5`
- Icono: `h-11 w-11 rounded-xl` con fondo `color-mix(in oklch, ${accent} 14%, transparent)` y color `${accent}`
- Label: `t-caption muted-foreground`
- Value: `t-num-md mono tnum`

### 3.3 Auto-refresh banner (para listados con estados pendientes)

Cuando una pantalla muestra un listado que tiene items en estado terminal pendiente (ticket SUNAT, polling de respuesta), debe:
- Auto-poll cada 30s en silencio
- Mostrar indicador inline en el descriptor: `<RefreshCw className={isRefreshing ? 'animate-spin' : ''} />`
- Botón "Actualizar" manual siempre visible

**Detección:**
```tsx
const hasPending = items.some(i => i.status === 'pending' || i.status === 'sent');
useEffect(() => {
  if (!hasPending) return;
  const id = setInterval(() => fetchData(true), 30_000);
  return () => clearInterval(id);
}, [hasPending]);
```

---

## 4. Empty states

### 4.1 Inicial (catálogo vacío)

Cuando el usuario nunca ha creado nada de este tipo.

```tsx
<div className="p-10 text-center">
  <div className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl"
       style={{ background: 'color-mix(in oklch, var(--accent) 14%, transparent)' }}>
    <DomainIcon className="h-8 w-8" style={{ color: 'var(--brand-ink)' }} />
  </div>
  <h2 className="t-h1 m-0">{title}</h2>
  <p className="t-body mt-2 mb-4 max-w-[440px] mx-auto" style={{ color: 'var(--muted-foreground)' }}>
    {description}
  </p>
  <Button onClick={onPrimaryAction} style={{ background: 'var(--accent)', color: 'var(--accent-foreground)', fontWeight: 600 }}>
    <Plus className="h-4 w-4 mr-2" /> {primaryActionLabel}
  </Button>
</div>
```

**Copy guidelines:**
- Title sin signos: "Tu catálogo está vacío", "Aún no has anulado comprobantes"
- Description explica el valor del feature, NO la mecánica
- CTA en imperativo: "Crear primer producto", "Emitir guía", "Programar recurrente"

### 4.2 Filtrado (con resultados vacíos por filtro)

Cuando hay datos pero el filtro actual no los muestra.

```tsx
<div className="p-10 text-center">
  <Inbox className="h-10 w-10 mx-auto mb-3" style={{ color: 'var(--slate-400)' }} />
  <p className="t-body m-0 font-semibold">{title}</p>
  <p className="t-body-sm mt-1 mb-4" style={{ color: 'var(--muted-foreground)' }}>
    {suggestion}
  </p>
  <Button variant="outline" onClick={clearFilters}>Limpiar filtros</Button>
</div>
```

**Copy guidelines:**
- Si la búsqueda es libre, mostrar el término entre comillas: `Sin resultados para "iphone"`.
- Suggestion: "Prueba con otro término o limpia los filtros."

### 4.3 Cargando

Estado de loading siempre con `Loader2 animate-spin` + texto descriptivo. NO skeleton.

```tsx
<div className="flex items-center gap-3 p-6 text-[var(--muted-foreground)]">
  <Loader2 className="h-4 w-4 animate-spin" />
  <span className="t-body-sm">Cargando comprobantes…</span>
</div>
```

---

## 5. Layout patterns por tipo de pantalla

### 5.1 Pantalla lista + CRUD por modal (productos, clientes, series)

```
PageHeader (title + count + "Nuevo X" button que abre Dialog)
FilterToolbar (search debounced 300ms + chips opcionales)
DataTable / EmptyState
PaginationFooter (si totalPages > 1)
```

Modal de Create/Edit: `<Dialog max-w-2xl>` con secciones internas si tiene 5+ campos.

### 5.2 Pantalla lista + Create en ruta dedicada (documents, despatch-advices, recurring-invoices)

```
PageHeader (title + count + "Nuevo X" button → Link href)
FilterToolbar (search + chips + dates + extras)
DataTable / EmptyState
PaginationFooter
```

Create page (full-page form):
```
PageHeader (title + descripción del propósito del flujo)
Grid lg:grid-cols-3 gap-cards
  lg:col-span-2:
    Section "Cliente"
    Section "Detalles"
    Section "Items" (con items table)
    Section "Observaciones"
  lg:col-span-1:
    StickySummary
      datos clave
      totales
      Submit + Cancel
      microcopy
```

### 5.3 Pantalla de detalle con acciones SUNAT

```
PageHeader (volver + breadcrumb + número del comprobante mono + StatusBadge)
Grid lg:grid-cols-3 gap-cards
  lg:col-span-2:
    Section "Datos generales"
    Section "Items" (read-only)
    Section "Totales"
    Section "Historial SUNAT" (timeline con tickets, CDR, errores)
  lg:col-span-1:
    StickySummary (acciones disponibles según estado)
      - Si draft → "Enviar a SUNAT"
      - Si accepted → "Descargar XML", "Descargar PDF", "Descargar CDR", "Anular"
      - Si rejected → "Reintentar"
      - Si pending_ticket → "Consultar ticket"
```

### 5.4 Pantalla de configuración (settings, certificate, plan)

```
PageHeader (title + descripción)
Grid grid-cols-1 lg:grid-cols-3 gap-cards
  Por sección:
    lg:col-span-1:
      <div sticky>
        <h3>Section title</h3>
        <p muted>Section description (qué hace, por qué importa)</p>
      </div>
    lg:col-span-2:
      Section (form fields)
```

### 5.5 Pantalla read-only con timeline (voided, audit-log, webhooks deliveries)

```
PageHeader (title + descripción + Actualizar button)
FilterToolbar
DataTable o TimelineList
EmptyState
```

---

## 6. Inputs especiales

### 6.1 Input numérico con moneda

```tsx
<div className="relative">
  <span className="absolute left-3 top-1/2 -translate-y-1/2 t-body-sm mono pointer-events-none"
        style={{ color: 'var(--muted-foreground)' }}>
    {currency === 'USD' ? '$' : 'S/'}
  </span>
  <Input
    type="number"
    step="0.01"
    min="0"
    inputMode="decimal"
    value={value}
    onChange={onChange}
    className="mono tnum text-right pl-8"
    placeholder="0.00"
    required
  />
</div>
```

### 6.2 Input fecha con icono

```tsx
<div className="relative">
  <Calendar className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 pointer-events-none"
            style={{ color: 'var(--muted-foreground)' }} />
  <Input type="date" value={date} onChange={onChange} className="pl-9 mono" />
</div>
```

### 6.3 Input con icono inline genérico

Para Email/Phone/MapPin/Hash/etc. mismo patrón con el icono lucide correspondiente.

---

## 7. Naming conventions para Cloud Design / Stitch

Cuando se generen pantallas que serán llevadas a TSX, usar estos nombres de componentes para que el mapeo sea directo:

| Bloque visual | Componente TSX | Archivo |
|---|---|---|
| Page header | `<PageHeader>` | nuevo, `app-shell/PageHeader.tsx` |
| Filter toolbar | `<FilterToolbar>` | nuevo, `app-shell/FilterToolbar.tsx` |
| Status pill | `<StatusBadge>` | nuevo, `ui/StatusBadge.tsx` |
| Enum pill group | `<PillGroup>` | nuevo, `ui/PillGroup.tsx` |
| Form section card | `<Section>` | nuevo, `ui/Section.tsx` |
| Sticky summary | `<StickySummary>` | nuevo, `ui/StickySummary.tsx` |
| KPI card | `<KpiCard>` | nuevo, `ui/KpiCard.tsx` |
| Date range presets | `<DateRangePresets>` | nuevo, `ui/DateRangePresets.tsx` |
| Pagination footer | `<PaginationFooter>` | nuevo, `ui/PaginationFooter.tsx` |
| SUNAT lookup widget | `<SunatLookup>` | nuevo, `ui/SunatLookup.tsx` |
| Empty state inicial | `<EmptyStateInitial>` | nuevo, `ui/EmptyStateInitial.tsx` |
| Empty state filtrado | `<EmptyStateFiltered>` | nuevo, `ui/EmptyStateFiltered.tsx` |

Todos estos componentes actualmente están **inline** en las pantallas existentes (despatch-advices, recurring-invoices, products, customers, reports, voided). El próximo paso de refactor es extraerlos a `ui/` para que Stitch los pueda nombrar uniformemente.

---

## 8. Inventario por pantalla

A continuación, cada pantalla del portal cliente con su inventario de campos del backend + UI propuesta. Generado a partir del audit del 2026-05-29.
# Group A — Core SUNAT Emission Flows · Audit DESIGN

Auditoría por pantalla del backend que sirve los flujos centrales de emisión electrónica:
comprobantes (Factura/Boleta/NC/ND), guías de remisión electrónicas (GRE) y facturación
recurrente. Cada sección documenta los endpoints REST, DTOs, validaciones, integraciones
SUNAT y UI propuesta siguiendo `DESIGN-CLIENT.md`.

---

## /documents — Comprobantes

### Propósito
Pantalla de bandeja principal de comprobantes emitidos (Factura, Boleta, NC, ND). Lista
paginada con filtros por tipo, estado y rango de fechas, y permite descargar el PDF o
abrir el detalle de cada documento.

### Endpoints REST consumidos
- `GET /v1/documents?page&pageSize&documentType&status` — lista paginada filtrable
- `GET /v1/documents/{id}/pdf` — descarga PDF (Bearer token)

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/DocumentsController.cs:73` (`List`)
- Service: `src/TukiFact.Infrastructure/Services/DocumentService.cs:154` (`ListAsync`)
- Service interface: `src/TukiFact.Application/Interfaces/IDocumentService.cs:9`
- Entity: `src/TukiFact.Domain/Entities/Document.cs:3`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/DocumentRepository.cs:22` (`GetByTenantAsync`)
- Validator: no tiene validador para la query — FLAG (el controller solo clampea `page`/`pageSize`)

### Campos del backend — Request Create
No aplica (esta es la pantalla de listado). Ver `/documents/new`.

### Campos del backend — Request Update
No aplica.

### Campos del backend — Response
Cada fila de `data[]` es un `DocumentResponse` (`src/TukiFact.Application/DTOs/Documents/DocumentResponse.cs:3`):

| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `id` | Guid | No | UUID del documento |
| `documentType` | string | No | Catálogo 01 SUNAT (01/03/07/08) |
| `documentTypeName` | string | No | Nombre humano ("Factura", "Boleta"…) |
| `serie` | string | No | Serie (F001, B001, FC01, FD01) |
| `correlative` | long | No | Correlativo numérico |
| `fullNumber` | string | No | `{Serie}-{Correlative:D8}` |
| `issueDate` | DateOnly | No | Fecha de emisión |
| `dueDate` | DateOnly | Sí | Fecha de vencimiento |
| `currency` | string | No | PEN / USD |
| `customerDocType` | string | No | Catálogo 06 (6/1/4/7/0) |
| `customerDocNumber` | string | No | Número de doc del receptor |
| `customerName` | string | No | Razón social o nombre |
| `operacionGravada` | decimal | No | Base imponible gravada |
| `operacionExonerada` | decimal | No | Base exonerada |
| `operacionInafecta` | decimal | No | Base inafecta |
| `subtotal` | decimal | No | Suma de bases (calculado en mapper) |
| `igv` | decimal | No | IGV 18% calculado |
| `total` | decimal | No | Importe total |
| `status` | string | No | draft / signed / sent / accepted / rejected / voided |
| `sunatResponseCode` | string | Sí | Código respuesta SUNAT |
| `sunatResponseDescription` | string | Sí | Descripción SUNAT |
| `hashCode` | string | Sí | Digest XML firmado |
| `xmlUrl` | string | Sí | Ruta MinIO del XML firmado |
| `pdfUrl` | string | Sí | Ruta MinIO del PDF |
| `notes` | string | Sí | Observaciones |
| `createdAt` | DateTimeOffset | No | Timestamp creación |
| `items[]` | DocumentItemResponse | No | Líneas del comprobante |

Wrap de paginación (`DocumentsController.cs:91`):
- `data[]` — array de respuestas
- `pagination.page`, `pageSize`, `totalCount`, `totalPages`

### Enums / Catálogos SUNAT relevantes
- `documentType` (filtro): 01=Factura, 03=Boleta, 07=Nota de Crédito, 08=Nota de Débito
- `status` (filtro): `draft`, `signed`, `sent`, `accepted`, `rejected`, `voided`, `observed` (constantes en `DocumentStatus` enum, ver `src/TukiFact.Domain/Enums/`)
- `customerDocType` (mostrado en tabla): 6=RUC, 1=DNI, 4=CE, 7=Pasaporte, 0=Sin doc

### Servicios externos invocados (durante create/update)
- SUNAT firmado XML: NO (solo lectura)
- SUNAT envío: NO
- SUNAT polling de ticket: NO
- Lookup RUC/DNI: NO
- Tipo de cambio (SBS): NO
- Email cliente: NO
- MinIO/storage: SÍ — para descarga PDF on-demand (`DownloadPdf` en `DocumentsController.cs:166`)
- NATS / event publisher: NO

### Validación oficial del backend
- `page` se fuerza a ≥ 1, `pageSize` se clampea a [1, 100] (`DocumentsController.cs:83-85`)
- No hay validador de los filtros `documentType` / `status` — se aceptan strings arbitrarios; el repo simplemente filtra por `==` (no enforced — FLAG)
- Tenant scoping en repo: `query.Where(d => d.TenantId == tenantId)` (`DocumentRepository.cs:29`)

### UI propuesto siguiendo DESIGN-CLIENT
- Page header (`t-display-lg`): "Comprobantes" + sub: `{totalCount} documentos emitidos`
- Filter toolbar:
  - PillGroup compacto de tipo: `Todos · Factura · Boleta · NC · ND` (5 opciones → PillGroup, no Select)
  - PillGroup de status: `Todos · Aceptado · Rechazado · Anulado · Borrador · Enviado` (6 opciones — frontera; aceptable PillGroup)
  - Date range `Desde / Hasta` con presets opcionales (Hoy, Esta semana, Este mes, Últimos 30 días)
  - Botón "Limpiar" cuando hay filtros activos
- Tabla:
  - `Número` (mono, font-semibold, primary)
  - `Tipo` (badge outline)
  - `Fecha` (mono tnum, formato `dd MMM yyyy`)
  - `Cliente` (texto truncado max-w-[260px]) + sub mono con doc en t-caption
  - `Total` (tnum mono, alineado a derecha, font-semibold)
  - `Estado` (StatusBadge con color por estado: accepted=success, rejected=danger, voided=slate, draft=warning, sent=info)
  - `Acciones` (ghost iconos: ver / descargar PDF / descargar XML)
- Empty states:
  - Inicial: ilustración + `Aún no has emitido comprobantes` + CTA `Emitir comprobante`
  - Filtrado: `Sin resultados con esos filtros` + botón `Limpiar`
- Loading: skeleton de 5 filas
- Error: toast inferior + retry inline
- PillGroup candidates: tipo (4) y estado (6 — frontera)
- Select candidates: ninguno
- SUNAT lookup: no aplica
- Bulk actions: NO actualmente (gap UX)
- Row actions: Ver, Descargar PDF, Descargar XML
- Accesibilidad: filas con `role="link"` semántico y `aria-label="Ver comprobante {fullNumber}"`; tab order: filtros → tabla → paginación

### Estados / flujo
Solo lectura — no hay transiciones de estado en esta pantalla. Status de cada documento se muestra como badge.

### Edge cases / gotchas del backend
- Tenant scoping: SÍ — todas las queries filtran por `TenantId` del JWT (`DocumentsController.cs:87` vía `ITenantProvider`)
- Idempotencia: no aplica (GET)
- Soft-delete vs hard-delete: no implementado — `Document` no tiene `IsDeleted` ni `DeletedAt`
- Currency handling: el campo `currency` se persiste literal (`PEN`/`USD`), no se convierte; el formato visual usa `Intl.NumberFormat` del cliente
- Timezone: `IssueDate` es `DateOnly` (sin tz) — el frontend hace `new Date(date + 'T00:00:00')` que asume hora local, no UTC; revisar si hay drift en zonas no-Lima — FLAG menor
- Audit trail: no se registra acceso de lectura
- Sin paginación cursor-based — `offset/limit` puede degradarse en tenants grandes con muchos documentos

### Navegación adyacente
- ¿Desde dónde se llega?: ítem "Comprobantes" en el sidebar de `(authenticated)/layout.tsx`
- ¿A dónde se puede ir?:
  - `Emitir Comprobante` (header) → `/documents/new`
  - Click en fila → `/documents/{id}`
  - Acción descarga PDF → `fetch` con Bearer + blob download
- ¿Bulk actions?: no
- ¿Row actions?: ver detalle, descargar PDF, descargar XML

---

## /documents/new — Emitir comprobante (Factura o Boleta)

### Propósito
Formulario para crear y enviar a SUNAT un nuevo comprobante electrónico (Factura tipo 01
o Boleta tipo 03). El flujo es síncrono: el backend firma el XML, lo envía a SUNAT por
SOAP y persiste el CDR antes de responder.

### Endpoints REST consumidos
- `GET /v1/series` — para listar series activas del tenant (no es de este grupo pero se consume)
- `GET /v1/services/lookup/status` — verificar si hay proveedor RUC/DNI configurado
- `GET /v1/services/lookup/{ruc|dni}/{numero}` — autocompletar nombre/dirección (opcional)
- `POST /v1/documents` — crear y emitir comprobante

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/DocumentsController.cs:33` (`Emit`)
- Service: `src/TukiFact.Infrastructure/Services/DocumentService.cs:49` (`EmitAsync`)
- Service interface: `src/TukiFact.Application/Interfaces/IDocumentService.cs:7`
- Entity (cabecera): `src/TukiFact.Domain/Entities/Document.cs:3`
- Entity (líneas): `src/TukiFact.Domain/Entities/DocumentItem.cs:3`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/DocumentRepository.cs:51` (`CreateAsync`)
- Validator: no tiene validador formal — FLAG. Las validaciones están dispersas en `DocumentService.EmitAsync` (líneas 49–73) y dependen de constraints del entity. Comparar con `DespatchAdviceValidator` y `RecurringInvoiceValidator` que sí existen.

### Campos del backend — Request Create
`CreateDocumentRequest` (`src/TukiFact.Application/DTOs/Documents/CreateDocumentRequest.cs:3`):

| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `documentType` | string | Sí | Debe corresponder a Series existente | Catálogo 01 SUNAT (01/03 en este formulario) | PillGroup (Factura / Boleta) |
| `serie` | string | Sí | Series.IsActive debe ser true; serie debe existir | Identificador serie (F001, B001) | Select con autoselección de la primera activa |
| `issueDate` | DateOnly? | No (default hoy UTC) | — | Fecha de emisión | Input fecha (default hoy) |
| `dueDate` | DateOnly? | No | — | Fecha de vencimiento del documento | Input fecha (opcional) |
| `currency` | string | No (default PEN) | — | PEN o USD | PillGroup (PEN / USD) |
| `customerDocType` | string | Sí | — | Catálogo 06 (6/1/4/7/0) | PillGroup 5 opciones (RUC/DNI/CE/PAS/Sin) |
| `customerDocNumber` | string | Sí | No enforced — el cliente debe respetar longitud | Número del documento | Input mono con `maxLength` según tipo |
| `customerName` | string | Sí | No enforced | Razón social o nombre | Input texto |
| `customerAddress` | string? | No | — | Dirección fiscal | Input texto |
| `customerEmail` | string? | No | — | Si se llena, se enviaría PDF/XML por correo (no implementado en EmitAsync — FLAG) | Input email |
| `notes` | string? | No | — | Observaciones libres del comprobante | Textarea |
| `purchaseOrder` | string? | No | — | Nº de orden de compra del cliente | Input texto (faltante en UI actual — FLAG menor) |
| `items[]` | List | Sí | `Items.Count >= 1` enforced en `DocumentService.cs:56` | Líneas del comprobante | Tabla editable |

`CreateDocumentItemRequest` (mismo archivo, línea 19):

| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `productCode` | string? | No | — | SKU interno del tenant | Input mono |
| `sunatProductCode` | string? | No | — | Código catálogo 25 SUNAT | Input mono / SUNAT lookup |
| `description` | string | Sí | No enforced | Descripción del ítem | Input texto |
| `quantity` | decimal | Sí | No enforced (> 0 implícito) | Cantidad | Input número tnum |
| `unitMeasure` | string | Sí (default `NIU`) | — | Catálogo 03 (NIU/ZZ/KGM…) | Input mono / Select |
| `unitPrice` | decimal | Sí | No enforced (≥ 0) | Precio unitario SIN IGV | Input número tnum |
| `igvType` | string | Sí | — | Catálogo 07 (10=Gravado, 20=Exonerado, 30=Inafecto, 21=Gratuito) | Select |
| `discount` | decimal | No (default 0) | — | Descuento del ítem | Input número tnum (faltante en UI actual — FLAG) |

### Campos del backend — Request Update
Update no implementado (solo se puede crear o anular vía `/v1/voided-documents`).

### Campos del backend — Response
`DocumentResponse` retornado tras emisión exitosa (ver tabla en `/documents` arriba).
Particularidades post-emisión:
- `status` reflejará: `accepted` (CDR positivo), `rejected` (CDR negativo), o `sent` (timeout / fallo de red — pendiente reintentar)
- `hashCode` poblado si el certificado del tenant está configurado
- `xmlUrl` y `pdfUrl` apuntan a rutas MinIO (`{bucket}/{key}`)
- `sunatResponseCode` y `sunatResponseDescription` siempre se intentan poblar

### Enums / Catálogos SUNAT relevantes
- `documentType`: 01=Factura, 03=Boleta, 07=NC, 08=ND
- `customerDocType` (cat. 06 SUNAT): 6=RUC, 1=DNI, 4=Carné Extranjería, 7=Pasaporte, 0=Sin documento
- `igvType` (cat. 07 SUNAT — afectación IGV): 10=Gravado, 20=Exonerado, 30=Inafecto, 21=Gratuito
- `unitMeasure` (cat. 03 SUNAT — unidades): NIU=Unidad, ZZ=Servicios, KGM=Kg, MTR=Metro, LTR=Litro
- `currency` (ISO 4217): PEN, USD

### Servicios externos invocados (durante create/update)
- SUNAT firmado XML: SÍ — `IXmlSigningService.SignXml` en `DocumentService.cs:85` (solo si `tenant.CertificateData != null`)
- SUNAT envío (SOAP): SÍ — síncrono al submit, vía `ISunatClient.SendDocumentAsync` en `DocumentService.cs:112`
- SUNAT polling de ticket: NO (para Factura/Boleta el endpoint SOAP `sendBill` es síncrono y devuelve CDR inmediatamente; polling solo aplica a GRE)
- Lookup RUC/DNI: SÍ — en el formulario (opcional, `/v1/services/lookup/...`)
- Tipo de cambio (SBS): NO en este flujo (no se persiste `ExchangeRate` desde el form, aunque el entity lo soporta — FLAG)
- Email cliente: NO — `CustomerEmail` se persiste pero `DocumentService.EmitAsync` no envía correo. Los `EventHandlers/` (ver `DocumentCreatedHandler`, `DocumentSentHandler`) existen pero `EmitAsync` no publica ningún evento — FLAG fuerte
- MinIO/storage: SÍ — XML firmado (`UploadXmlAsync` en `DocumentService.cs:104`) y CDR (`UploadCdrAsync` en `DocumentService.cs:123`)
- NATS / event publisher: NO en `EmitAsync` (no se invoca `IEventPublisher`) — FLAG: los handlers existen pero nunca reciben evento de emisión exitosa

### Validación oficial del backend
- `Items.Count >= 1` — enforced (`DocumentService.cs:56-57`)
- `Series` debe existir para el `(tenantId, documentType, serie)` — enforced (línea 60)
- `Series.IsActive == true` — enforced (línea 63)
- Tenant debe existir — enforced (línea 52)
- Cálculo IGV server-side: `IgvRate=0.18m` aplicado solo a `IgvType=="10"`, con `Math.Round(..., 2)` por ítem (líneas 200–205)
- Otras reglas (longitud doc, formato serie F001/B001, RUC mod-11, email válido): no enforced en backend para `/v1/documents` — FLAG fuerte (comparar con validators de despatch y recurring)

### UI propuesto siguiendo DESIGN-CLIENT
- Page header (`t-display-lg`): "Emitir comprobante" + sub: "Crea una factura o boleta y envíala a SUNAT."
- Layout: full-page 2/3 + sticky 1/3 (left form, right sticky summary) — implementado
- Form sections (orden recomendado):
  1. `¿Qué vas a emitir?` — PillGroup tipo (Factura/Boleta), Select serie con preview del próximo correlativo, PillGroup moneda
  2. `Cliente` — PillGroup tipo doc (5 opciones), Input doc con botón lookup, Input nombre, Input dirección, Input email opcional
  3. `Productos y servicios` — Tabla editable con `Descripción / Cant / P.Unit / IGV / Subtotal / Quitar`
  4. `Observaciones` — Textarea
  5. *(faltante)* `Orden de compra` — Input texto opcional para `purchaseOrder` — FLAG
  6. *(faltante para multi-moneda)* `Tipo de cambio` cuando `currency=USD` — Input número con botón "Obtener de SBS" — FLAG
- PillGroup candidates:
  - `documentType` (2)
  - `customerDocType` (5)
  - `currency` (2)
- Select candidates:
  - `serie` (variable según tenant)
  - `igvType` por ítem (3 — PillGroup también funciona)
- SUNAT lookup: SÍ — embedded en el bloque Cliente (botón "Buscar" condicional a `customerDocType ∈ {6,1}` y `lookupStatus.configured`)
- Auto-bridge / smart helpers:
  - Auto-selección de primera serie activa al cambiar tipo
  - Auto-asignación de `defaultCustomerDoc` al cambiar tipo (Factura→RUC, Boleta→DNI)
  - Preview "Próximo: F001-00000123" desde `Series.currentCorrelative + 1`
  - Cálculo client-side de gravada/exonerada/inafecta/IGV/total en el sticky summary
  - Auto-uppercase en nombre cuando `customerDocType=6` (RUC)
- Empty states:
  - Sin series para el tipo seleccionado: card border-dashed con CTA → `/series`
  - Sin ítems: el submit se deshabilita si `total === 0`
- Loading: spinner inline en botón submit ("Emitiendo…")
- Error feedback: toast con mensaje del backend
- Confirmaciones destructivas: solo "Quitar línea" (botón ghost rojo, sin dialog — gesto reversible)
- Accesibilidad: labels asociados a inputs, `aria-label` en botones de icono, focus visible en PillGroup

### Estados / flujo
Después del submit (`POST /v1/documents`):
1. Backend valida tenant, series, items
2. Persiste como `status=draft`
3. Construye UBL XML (`IUblBuilder.BuildInvoiceXml`)
4. Firma XML con certificado del tenant → `status=signed` (si falla la firma, sigue sin firmar pero loguea warning — FLAG)
5. Sube XML firmado a MinIO
6. Envía a SUNAT vía SOAP `sendBill` (sincrónico)
7. Si CDR positivo → `status=accepted`, descarga CDR, sube a MinIO
8. Si CDR negativo → `status=rejected`
9. Si excepción de red → `status=sent` (pendiente reintento — FLAG: no hay worker que reintente automáticamente)
10. Genera `QrData` (texto QR concatenado)
11. Persiste cambios
12. Responde 201 Created con `DocumentResponse`
13. Email cliente: NO se envía (gap)
14. NATS: no se publica evento (gap)
15. Frontend hace `router.push(/documents/{id})`

### Edge cases / gotchas del backend
- Tenant scoping: SÍ — derivado del JWT (`ITenantProvider.GetCurrentTenantId()`)
- Idempotencia: NO — un retry del cliente generaría doble correlativo. El correlativo lo asigna `ISeriesRepository.GetNextCorrelativeAsync` que probablemente usa MAX+1 (revisar `SeriesRepository`)
- Soft-delete: no aplica (no se borra)
- Currency handling: si `currency=USD`, el campo `ExchangeRate` del entity queda en NULL — UBL probablemente rompa o SUNAT rechace. FLAG: falta integración con SBS
- Timezone: `IssueDate` defaultea a `DateOnly.FromDateTime(DateTime.UtcNow)` — para tenants en Lima puede emitir con fecha de ayer/hoy según hora UTC. FLAG: SUNAT espera fecha Lima
- Audit trail: NO se registra en `IAuditLogRepository` desde `DocumentService.EmitAsync` (comparar con `DespatchAdviceService` que sí lo hace) — FLAG
- Sin transacción explícita en `EmitAsync` — los pasos (create → sign → upload → send → update) son secuenciales con `await`s independientes; un crash a mitad deja estado inconsistente (XML subido sin estado actualizado, etc.)
- `QrData` se genera con interpolación de strings — si `hashCode==null` queda como `...||` vacío al final, lo cual viola la spec SUNAT QR — FLAG menor

### Navegación adyacente
- ¿Desde dónde se llega?: botón "Emitir Comprobante" desde `/documents` (lista)
- ¿A dónde se puede ir?:
  - Submit exitoso → `/documents/{id}` (detalle)
  - `Administrar series` (link en sticky aside) → `/series`
  - Si no hay series del tipo: link CTA → `/series` para crear
- Bulk: no aplica
- Row actions: agregar/quitar línea, lookup RUC/DNI

---

## /documents/[id] — Detalle de comprobante (con flujo de anulación)

### Propósito
Vista de detalle de un comprobante emitido: cabecera con estado SUNAT, datos del receptor,
líneas, totales, observaciones, descarga de XML/PDF, y acciones contextuales
(emitir Nota de Crédito, Anular).

### Endpoints REST consumidos
- `GET /v1/documents/{id}` — obtener detalle completo
- `GET /v1/documents/{id}/xml` — descargar XML firmado (Bearer)
- `GET /v1/documents/{id}/pdf` — descargar PDF (Bearer)
- `POST /v1/voided-documents` — anular comprobante (Comunicación de Baja)

### Backend behind
- Controller (detalle): `src/TukiFact.Api/Controllers/DocumentsController.cs:63` (`GetById`)
- Controller (XML): `src/TukiFact.Api/Controllers/DocumentsController.cs:107` (`DownloadXml`)
- Controller (PDF): `src/TukiFact.Api/Controllers/DocumentsController.cs:165` (`DownloadPdf`)
- Controller (anular): `src/TukiFact.Api/Controllers/VoidedDocumentsController.cs:38` (`VoidDocument`) — requiere rol `admin`
- Service detalle: `src/TukiFact.Infrastructure/Services/DocumentService.cs:148` (`GetByIdAsync`)
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/DocumentRepository.cs:16` (`GetByIdWithItemsAsync`)
- Entity anulación: `src/TukiFact.Domain/Entities/VoidedDocument.cs:3`
- Repository anulación: `src/TukiFact.Infrastructure/Persistence/Repositories/VoidedDocumentRepository.cs:22`
- Validator: no tiene validador formal — FLAG. Solo se enforza `Document.Status == Accepted` en `VoidedDocumentsController.cs:46`

### Campos del backend — Request Create
Solo aplica al flujo de anulación. `VoidDocumentRequest` (`src/TukiFact.Application/DTOs/Documents/VoidDocumentRequest.cs:3`):

| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `documentId` | Guid | Sí | Documento debe existir y estar en status `accepted` | ID del comprobante a anular | (oculto, viene del contexto) |
| `voidReason` | string | Sí | No enforced (el FE valida no-vacío) | Motivo de anulación libre | Textarea con placeholder |

### Campos del backend — Request Update
Update no implementado (solo se puede emitir NC/ND o anular).

### Campos del backend — Response
- `GET /v1/documents/{id}` retorna `DocumentResponse` completo con `items[]` (ver tabla en `/documents`)
- `POST /v1/voided-documents` retorna `VoidedDocumentResponse` (`VoidDocumentRequest.cs:8`):

| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `id` | Guid | No | UUID del ticket de baja |
| `ticketNumber` | string | No | `RA-{yyyyMMdd}-{seq:D3}` |
| `status` | string | No | pending / processing / accepted / rejected |
| `sunatTicket` | string | Sí | Ticket asignado por SUNAT (RA es asíncrono) |
| `sunatResponseCode` | string | Sí | Código respuesta tras polling |
| `sunatResponseDescription` | string | Sí | Descripción respuesta SUNAT |
| `createdAt` | DateTimeOffset | No | Timestamp creación |

### Enums / Catálogos SUNAT relevantes
- `status` documento: `draft`, `signed`, `sent`, `accepted`, `rejected`, `voided`, `observed`
- `ticketType` (Comunicación de Baja): `RA` (Comunicación de Baja para FE/NC/ND), `RC` (Resumen Diario para boletas) — actualmente solo se usa `RA`
- `voidedDocument.status`: `pending`, `processing`, `accepted`, `rejected`

### Servicios externos invocados (durante create/update)
- SUNAT firmado XML: parcial — la anulación crea el registro local pero no genera XML 30/RA firmado ni lo envía a SUNAT en el controller actual — FLAG fuerte (gap funcional crítico)
- SUNAT envío: NO (en el flujo de void actual)
- SUNAT polling de ticket: NO implementado en el endpoint actual
- Lookup RUC/DNI: NO
- Tipo de cambio: NO
- Email cliente: NO
- MinIO/storage: NO en void; SÍ en download XML/PDF (`storageService.DownloadAsync` en `DocumentsController.cs:116`)
- NATS / event publisher: NO

### Validación oficial del backend
- Documento debe existir (`VoidedDocumentsController.cs:43`)
- `document.Status == DocumentStatus.Accepted` — enforced (línea 46) — solo se anulan aceptados
- Endpoint requiere rol `admin` (`VoidedDocumentsController.cs:14`)
- `voidReason` no enforced server-side (el FE valida `.trim()` no vacío en `[id]/page.tsx:71`)

### UI propuesto siguiendo DESIGN-CLIENT
- Page header con `t-display-lg`: `{fullNumber}` + sub: `{documentTypeName}` + StatusBadge prominente
- Toolbar de acciones (right-aligned, wrap):
  - `Nota de Crédito` (outline, secondary action) — solo si `status=accepted` y `documentType ∈ {01,03}`
  - `Anular` (outline rojo, destructive) — solo si `isAdmin && status=accepted`
  - `XML` (outline)
  - `PDF` (primary)
- Banner status SUNAT (card top): icono + label + `{sunatResponseCode}: {sunatResponseDescription}` mono
- Layout 2-col:
  - `Receptor` — labels mono para doc, plain para nombre
  - `Detalle` — fecha, moneda, hash (mono, truncado)
- Items: tabla read-only con tnum mono en columnas numéricas
- Totals: card final con detalle por base + IGV + total destacado (`t-num-lg`)
- Notes: card opcional si `notes` está poblado
- Dialog de anulación:
  - `max-w-2xl` modal con título "Anular Documento"
  - Descripción con interpolación: "Esta acción no se puede deshacer. El documento {fullNumber} será anulado ante SUNAT."
  - Textarea para `voidReason` (3 rows)
  - Footer: `Cancelar` (outline) + `Confirmar Anulación` (destructive, disabled mientras `!voidReason.trim()` o `isVoiding`)
- Confirmaciones destructivas: SÍ — dialog obligatorio antes de anular
- Loading: skeleton al cargar; spinner inline durante anulación
- Accesibilidad: dialog con focus trap (provee `<Dialog>` de shadcn), botón Anular con `aria-label` claro

### Estados / flujo
- GET es síncrono read-only
- POST `/v1/voided-documents`:
  1. Backend valida documento existe + `status=accepted`
  2. Genera `ticketNumber=RA-{yyyyMMdd}-{seq:D3}` (secuencial por tenant + fecha)
  3. Persiste `VoidedDocument` con `status=pending`
  4. Actualiza el `Document.Status = Voided`
  5. Responde 201 Created
  6. **NO** se envía XML 30 a SUNAT (gap crítico — el flujo asíncrono real con `getStatus` polling no está implementado)
  7. **NO** se publica evento NATS ni email
- Frontend recarga `loadDoc()` para reflejar nuevo status

### Edge cases / gotchas del backend
- Tenant scoping: SÍ — `GetByIdAsync` no filtra por tenant en repo (FLAG menor — el documento podría leerse si se conoce el UUID; revisar `DocumentRepository.GetByIdAsync` línea 13)
- Idempotencia: el endpoint de anulación NO es idempotente — si se llama dos veces, el segundo POST fallaría con `status != Accepted` (que ahora es `Voided`), pero genera un ticket vacío inicialmente
- Soft-delete: no aplica
- Currency handling: solo se muestra, no se convierte
- Timezone: `today = DateOnly.FromDateTime(DateTime.UtcNow)` en `VoidedDocumentsController.cs:49` — el ticket usa fecha UTC, no Lima — FLAG menor
- Audit trail: NO se registra en `IAuditLogRepository` desde `VoidedDocumentsController` — FLAG (comparar con GRE que sí lo hace)
- Reverso/anulación: una vez `Voided` no hay forma de revertir el estado
- Permisos: `Anular` requiere rol `admin`; el FE oculta el botón si `user.role !== 'admin'` (`[id]/page.tsx:87-88`)

### Navegación adyacente
- ¿Desde dónde se llega?: click en fila desde `/documents` o vía `router.push` post-emisión desde `/documents/new`
- ¿A dónde se puede ir?:
  - `Volver` → `/documents`
  - `Nota de Crédito` → `/documents/credit-note?ref={id}`
  - `XML` / `PDF` → descarga blob
  - Tras anular → permanece en la misma página con status actualizado
- Bulk: no aplica
- Row actions: no aplica (vista de un solo documento)

---

## /despatch-advices — Guías de remisión

### Propósito
Bandeja de guías de remisión electrónicas (GRE Remitente tipo 09, GRE Transportista tipo 31).
Lista paginada con filtros por estado + rango de fechas + búsqueda client-side. Permite ver
modalidad de transporte, trayecto origen→destino y CDR.

### Endpoints REST consumidos
- `GET /v1/despatch-advices?page&pageSize&status&dateFrom&dateTo` — lista paginada

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/DespatchAdviceController.cs:154` (`List`)
- Service: `src/TukiFact.Infrastructure/Services/DespatchAdviceService.cs:305` (`ListAsync`)
- Service interface: `src/TukiFact.Application/Interfaces/IDespatchAdviceService.cs:23`
- Entity: `src/TukiFact.Domain/Entities/DespatchAdvice.cs:3`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/DespatchAdviceRepository.cs:23` (`ListAsync`)
- Validator: no tiene validador para query — FLAG menor

### Campos del backend — Request Create
No aplica (lista).

### Campos del backend — Request Update
No aplica.

### Campos del backend — Response
Wrap `{ data[], pagination }`. Cada elemento es un `DespatchAdviceResponse` (`src/TukiFact.Application/DTOs/DespatchAdvices/DespatchAdviceResponse.cs:3`). Campos relevantes para la lista:

| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `id` | Guid | No | UUID |
| `documentType` | string | No | 09 (Remitente) o 31 (Transportista) |
| `documentTypeName` | string | No | Nombre humano |
| `serie` | string | No | Tnnn / Vnnn |
| `correlative` | long | No | Correlativo |
| `fullNumber` | string | No | Tnnn-00000001 |
| `issueDate` | DateOnly | No | Fecha emisión |
| `transferStartDate` | DateOnly | No | Fecha inicio traslado |
| `transportMode` | string | No | 01=Público, 02=Privado |
| `transportModeName` | string | No | Nombre humano |
| `recipientDocType` | string | No | 6/1/4/7/0 |
| `recipientDocNumber` | string | No | Doc destinatario |
| `recipientName` | string | No | Nombre destinatario |
| `originUbigeo` | string | No | 6 dígitos INEI |
| `destinationUbigeo` | string | No | 6 dígitos INEI |
| `status` | string | No | draft / signed / sent / accepted / rejected / cancelled |
| `sunatTicket` | string | Sí | Ticket SUNAT (para GRE es asíncrono) |
| `xmlUrl`, `pdfUrl`, `cdrUrl` | string | Sí | Rutas MinIO |
| `items[]` | DespatchAdviceItemResponse | No | Líneas |

(Ver DTO completo para campos de carrier/driver/vehicle/destination expandidos.)

### Enums / Catálogos SUNAT relevantes
- `documentType`: 09=GRE Remitente, 31=GRE Transportista
- `transportMode` (cat. 18 SUNAT): 01=Transporte Público, 02=Transporte Privado
- `status` (TukiFact-specific): `draft`, `signed`, `sent`, `accepted`, `rejected`, `pending_ticket`, `cancelled` (los últimos 2 los maneja el FE como variantes del status del backend)
- `recipientDocType` (cat. 06): 6/1/4/7/0

### Servicios externos invocados (durante create/update)
- Todos NO (es solo lectura)

### Validación oficial del backend
- `page` clampeado a [1, +∞), `pageSize` clampeado a [1, 100] (`DespatchAdviceController.cs:164-166`)
- Filtros `documentType`/`status` no validados — se aceptan strings arbitrarios
- Tenant scoping: SÍ — `query.Where(d => d.TenantId == tenantId)` (`DespatchAdviceRepository.cs:30`)

### UI propuesto siguiendo DESIGN-CLIENT
- Page header (`t-display-lg`): "Guías de remisión" + sub: `{totalCount} guías en tu cuenta.`
- Filter toolbar:
  - Input search con icono lupa (filtro **client-side**: nº, destinatario, RUC, ubigeo)
  - PillGroup status: `Todas / Borradores / Enviadas · pendientes CDR / Aceptadas / Rechazadas / Anuladas` (6 chips)
  - Date range `Desde / Hasta`
  - `Limpiar` ghost button si hay filtros
- Tabla:
  - `Número` (mono semibold) + sub `GRE Remitente / Transportista` en t-caption mono
  - `Fecha` (mono `dd MMM yyyy`)
  - `Destinatario` truncado max-w-[200px] + sub doc en mono
  - `Modalidad` con icono `Truck` (privado) o `Bus` (público) + label
  - `Trayecto` con ubigeo origen + arrow + ubigeo destino (mono, colores info/success)
  - `Estado` con StatusBadge + chevron derecha
- Empty states:
  - Sin guías: ilustración + "Aún no has emitido guías" + CTA
  - Filtrado: "Sin resultados con esos filtros" + Limpiar
- Loading: spinner inline + texto "Cargando guías…"
- PillGroup candidates: status (6 — frontera, aceptable)
- Select candidates: ninguno (todos los filtros son pills o date pickers)
- SUNAT lookup: no aplica
- Auto-bridge: search es 100% client-side encima de los resultados paginados del backend — FLAG: el search no llega al backend, así que no encuentra coincidencias en otras páginas
- Confirmaciones destructivas: no aplica en la lista
- Accesibilidad: `<button>` semánticos para los chips de status, tabla con thead, filas clickeables con cursor pointer

### Estados / flujo
Solo lectura. Status es el persistido en backend, con dos casos especiales que el FE mapea:
- `pending_ticket` lo dibuja como `Pendiente ticket` aunque el backend usa solo `sent`
- `cancelled` solo aparece cuando se anuló localmente vía `/cancel`

### Edge cases / gotchas del backend
- Tenant scoping: SÍ — siempre filtra por tenant
- Idempotencia: GET trivial
- Soft-delete: no aplica
- Currency handling: no aplica (la GRE no tiene moneda)
- Timezone: `IssueDate` es `DateOnly`; el FE usa `T00:00:00` local
- Audit trail: la lectura no se registra
- Search client-side: limitado a la página actual — FLAG (no encuentra GREs en otras páginas)

### Navegación adyacente
- Desde: sidebar → "Guías de remisión"
- A: `Nueva guía` → `/despatch-advices/new`; click fila → `/despatch-advices/{id}`
- Bulk: no implementado
- Row actions: solo navegación al detalle

---

## /despatch-advices/new — Nueva guía de remisión

### Propósito
Formulario complejo para crear una GRE en estado borrador. La emisión a SUNAT es un paso
separado desde el detalle (no se emite en este submit). Captura destinatario, motivo,
modalidad de transporte (privado/público con campos condicionales), trayecto, conductor o
transportista, y mercadería.

### Endpoints REST consumidos
- `GET /v1/services/lookup/status` — verificar proveedor RUC/DNI
- `GET /v1/services/lookup/{ruc|dni}/{numero}` — autocompletar destinatario
- `POST /v1/despatch-advices` — crear borrador

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/DespatchAdviceController.cs:40` (`Create`)
- Service: `src/TukiFact.Infrastructure/Services/DespatchAdviceService.cs:56` (`CreateAsync`)
- Entity: `src/TukiFact.Domain/Entities/DespatchAdvice.cs:3`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/DespatchAdviceRepository.cs:66` (`AddAsync` con retry de correlativo)
- Validator: `src/TukiFact.Application/Validation/DespatchAdviceValidator.cs:28` (`Validate`) — invocado desde `DespatchAdviceService.cs:58`

### Campos del backend — Request Create
`CreateDespatchAdviceRequest` (`src/TukiFact.Application/DTOs/DespatchAdvices/CreateDespatchAdviceRequest.cs:3`):

| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `documentType` | string | Sí | Debe ser `09` o `31` | Tipo GRE | PillGroup 2 opciones |
| `serie` | string | Sí | `^T\d{3}$` si docType=09; `^V\d{3}$` si docType=31 | Serie | Input mono auto-uppercase, max 4 |
| `issueDate` | DateOnly? | No (default hoy UTC) | — | Fecha emisión | Input fecha |
| `issueTime` | TimeOnly? | No | — | Hora emisión | Input hora (faltante en UI — FLAG menor) |
| `transferStartDate` | DateOnly | Sí | `>= issueDate` | Fecha inicio traslado | Input fecha |
| `transferReasonCode` | string | Sí | ∈ {01,02,04,08,09,13,14,18} (cat. 20 SUNAT) | Motivo traslado | Select con código mono |
| `transferReasonDescription` | string | Sí | ≤ 100 chars | Descripción motivo | Input texto (auto-llenado desde catálogo) |
| `note` | string? | No | ≤ 500 chars | Observaciones | Textarea |
| `grossWeight` | decimal | Sí | `> 0` | Peso bruto total | Input número tnum con sufijo KG |
| `weightUnitCode` | string | No (default KGM) | ∈ {KGM, TNE, GRM} | Unidad peso | Sufijo fijo "KG" en UI actual (no editable — FLAG) |
| `totalPackages` | int | Sí | `>= 1` | Número de bultos | Input número |
| `transportMode` | string | Sí | ∈ {01, 02} | Modalidad | PillGroup 2 opciones (Privado/Público) |
| `carrierDocType` | string? | Cond. (si 01) | ∈ {1,4,6,7} | Tipo doc transportista | (auto a `6`) |
| `carrierDocNumber` | string? | Cond. (si 01) | 11 dígitos + mod-11 SUNAT | RUC transportista | Input mono |
| `carrierName` | string? | Cond. (si 01) | No vacío | Razón social transportista | Input texto uppercase |
| `carrierMtcNumber` | string? | No | — | Nº MTC | Input mono (faltante en UI — FLAG) |
| `driverDocType` | string? | Cond. (si 02) | — | Tipo doc conductor | (auto a `1` DNI) |
| `driverDocNumber` | string? | Cond. (si 02) | 8 dígitos | DNI conductor | Input mono |
| `driverName` | string? | Cond. (si 02) | No vacío | Nombre conductor | Input texto |
| `driverLicense` | string? | No | — | Nº licencia | Input mono uppercase |
| `vehiclePlate` | string? | Cond. (si 02) | Regex `^[A-Z0-9]{3}-?[A-Z0-9]{3,4}$` | Placa | Input mono uppercase |
| `secondaryVehiclePlate` | string? | No | — | Placa secundaria | Input mono (faltante en UI — FLAG) |
| `recipientDocType` | string | Sí | ∈ {0,1,4,6,7} (cat. 06) | Tipo doc destinatario | PillGroup 5 opciones |
| `recipientDocNumber` | string | Sí | Según tipo: 11d (RUC mod-11), 8d (DNI), libre (CE/PAS), N/A (sin doc) | Doc destinatario | Input mono |
| `recipientName` | string | Sí | ≤ 200 chars | Razón social/Nombre | Input texto |
| `originUbigeo` | string | Sí | 6 dígitos | Ubigeo INEI origen | Input mono tnum max 6 |
| `originAddress` | string | Sí | ≤ 300 chars | Dirección origen | Input texto |
| `destinationUbigeo` | string | Sí | 6 dígitos | Ubigeo INEI destino | Input mono tnum max 6 |
| `destinationAddress` | string | Sí | ≤ 300 chars | Dirección destino | Input texto |
| `relatedDocType` | string? | No | — | Tipo doc relacionado (factura) | Select / Input (faltante en UI — FLAG) |
| `relatedDocNumber` | string? | No | — | Nº doc relacionado | Input mono (faltante en UI — FLAG) |
| `items[]` | List | Sí | ≥ 1 | Líneas mercadería | Tabla editable |

`CreateDespatchAdviceItemRequest`:

| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `description` | string | Sí | ≤ 250 chars | Descripción mercadería | Input texto |
| `productCode` | string? | No | — | Código interno | Input mono |
| `quantity` | decimal | Sí | `> 0` | Cantidad | Input número tnum |
| `unitCode` | string | Sí | ≤ 4 chars | Catálogo 03 (NIU/KGM/LTR…) | Input mono center max 4 |

### Campos del backend — Request Update
Update no implementado. Las únicas transiciones son: `/emit` (draft→sent/rejected) y `/cancel` (accepted/sent→cancelled).

### Campos del backend — Response
`DespatchAdviceResponse` (ver tabla en `/despatch-advices`). Tras `Create` el `status` es siempre `draft` y los campos SUNAT (`sunatTicket`, `sunatResponseCode`, `xmlUrl`, `pdfUrl`, `cdrUrl`) están nulos hasta que se llame `/emit`.

### Enums / Catálogos SUNAT relevantes
- `documentType`: 09=Remitente, 31=Transportista
- `transferReasonCode` (cat. 20 SUNAT): 01=Venta, 02=Compra, 04=Traslado entre establecimientos, 08=Importación, 09=Exportación, 13=Otros, 14=Venta sujeta a confirmación, 18=Traslado emisor itinerante
- `transportMode` (cat. 18 SUNAT): 01=Público, 02=Privado
- `recipientDocType` (cat. 06): 6=RUC, 1=DNI, 4=CE, 7=Pasaporte, 0=Sin doc
- `weightUnitCode` (cat. 03): KGM=Kilogramo, TNE=Tonelada, GRM=Gramo
- `unitCode` por ítem (cat. 03): NIU=Unidad, KGM=Kg, LTR=Litro…

### Servicios externos invocados (durante create/update)
- SUNAT firmado XML: NO en `Create` (solo guarda borrador). SÍ en `/emit` posterior
- SUNAT envío: NO en `Create`
- SUNAT polling de ticket: NO en `Create`
- Lookup RUC/DNI: SÍ (opcional, autocompletar destinatario)
- Tipo de cambio: NO
- Email cliente: NO
- MinIO/storage: NO en `Create`
- NATS / event publisher: NO directo; sí se escribe `AuditLog` con action `despatch.created` (`DespatchAdviceService.cs:115`)

### Validación oficial del backend
Todas las reglas del validador `DespatchAdviceValidator` (`DespatchAdviceValidator.cs:28`) — agregadas en una sola lista que se lanza como `InvalidOperationException` con todas las violaciones concatenadas por `·`:

- DocumentType ∈ {09, 31}
- Serie no vacía y regex según tipo
- `transferStartDate >= issueDate`
- TransferReason ∈ catálogo
- TransferReasonDescription no vacío + ≤ 100
- Note ≤ 500
- GrossWeight > 0
- WeightUnitCode ∈ {KGM, TNE, GRM}
- TotalPackages ≥ 1
- TransportMode ∈ {01, 02}
- **Si transportMode=02**: driverDocNumber 8 dígitos, driverName no vacío, vehiclePlate formato `ABC-123`
- **Si transportMode=01**: carrierDocNumber 11d + mod-11, carrierName no vacío, carrierDocType ∈ {1,4,6,7}
- RecipientDocType ∈ {0,1,4,6,7}
- **Si recipient=RUC**: 11d + mod-11
- **Si recipient=DNI**: 8d
- RecipientName no vacío + ≤ 200
- OriginUbigeo / DestinationUbigeo: 6 dígitos
- OriginAddress / DestinationAddress: no vacíos + ≤ 300
- Al menos 1 item
- Por ítem: description no vacío + ≤ 250, quantity > 0, unitCode no vacío + ≤ 4

### UI propuesto siguiendo DESIGN-CLIENT
- Page header (`t-display-lg`): "Nueva guía de remisión" + sub explicativo
- Layout 2/3 + sticky 1/3
- Form sections:
  1. `Destinatario` — PillGroup tipo doc (5 col-2 grid), Input doc + lookup, Input nombre, Input serie con hint
  2. `Detalles del traslado` — PillGroup modalidad (2), Select motivo (8 opciones — Select correcto), Input fecha, Input peso con sufijo KG, Input bultos
  3. `Trayecto` — split origen/destino con iconos `MapPin` (origen, info) y `PackageCheck` (destino, success), Input dirección + Input ubigeo cada lado
  4. Conditional: `Conductor y vehículo` (si modalidad=02) o `Transportista` (si modalidad=01)
  5. `Mercadería a trasladar` — tabla editable
  6. `Observaciones` — Textarea
- PillGroup candidates: `documentType` (2), `transportMode` (2), `recipientDocType` (5)
- Select candidates: `transferReasonCode` (8 — Select por orden y código mono)
- SUNAT lookup: SÍ — en el bloque Destinatario
- Auto-bridge / smart helpers:
  - Auto-aplica `destinationAddress` desde lookup si está vacío
  - Auto-mapea `transferReasonDescription` cuando cambia el código del select
  - Auto-uppercase en serie / placa / licencia
  - Strip non-digits en ubigeos y DNI
  - Limpieza condicional: si modalidad cambia, los campos del bloque inverso van a `null` al submit (línea 322–331 del FE)
- Empty states: tabla siempre tiene al menos una línea
- Loading: spinner en botón "Crear guía"
- Error feedback: toast con el primer error o el mensaje del backend
- Confirmaciones destructivas: solo "Quitar línea" (sin dialog, gesto reversible)
- Faltantes en UI actual vs DTO — FLAGs menores: `issueTime`, `carrierMtcNumber`, `secondaryVehiclePlate`, `relatedDocType`/`relatedDocNumber`, `weightUnitCode` selectable
- Accesibilidad: labels asociados, `aria-label` en botones de icono, focus visible

### Estados / flujo
Después del submit (`POST /v1/despatch-advices`):
1. Validador agrupado del backend acumula todos los errores
2. Si hay errores → 400 con string concatenado
3. Si OK → asigna correlativo atómico (retry hasta 8 veces en colisión)
4. Persiste con `status=draft`
5. Escribe `AuditLog` action `despatch.created`
6. Responde 201 con DTO
7. Frontend redirige a `/despatch-advices/{id}` (donde se podrá emitir)
8. **NO** envía a SUNAT, **NO** firma XML, **NO** envía email, **NO** publica NATS

### Edge cases / gotchas del backend
- Tenant scoping: SÍ (derivado del JWT)
- Idempotencia: NO (sin idempotency-key) — un retry desde el cliente generaría doble guía
- Correlative race: SÍ — retry hasta 8 veces ante colisión unique (`DespatchAdviceRepository.cs:66`) — robusto
- Soft-delete: no
- Currency: no aplica
- Timezone: `issueDate` defaultea a UTC — para tenants en Lima en horario de cierre podría caer en día siguiente — FLAG menor
- Audit trail: SÍ — `WriteAuditAsync` invocado tras crear (línea 115)
- Sin transacción explícita en `AddAsync` por el retry loop — sí está implícita en `SaveChangesAsync`

### Navegación adyacente
- Desde: botón "Nueva guía" en `/despatch-advices` o sidebar
- A: tras crear → `/despatch-advices/{id}` (detalle, donde se emite)
- `Cancelar` → `/despatch-advices` (lista)
- Bulk: no aplica
- Row actions: agregar/quitar línea, lookup destinatario

---

## /despatch-advices/[id] — Detalle de guía de remisión

### Propósito
Vista de detalle de una GRE en cualquier estado. Permite emitir (firmar + enviar a SUNAT)
si está en borrador, refrescar status si SUNAT aún no devolvió CDR, anular si fue
aceptada, y descargar XML / CDR / PDF.

### Endpoints REST consumidos
- `GET /v1/despatch-advices/{id}` — detalle
- `POST /v1/despatch-advices/{id}/emit` — firmar + enviar + polling CDR
- `POST /v1/despatch-advices/{id}/refresh-status` — reconsultar CDR vía ticket
- `POST /v1/despatch-advices/{id}/cancel` — anulación local + audit

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/DespatchAdviceController.cs:92` (`GetById`), `:66` (`Emit`), `:132` (`RefreshStatus`), `:105` (`Cancel`)
- Service (emit con polling): `src/TukiFact.Infrastructure/Services/DespatchAdviceService.cs:122` (`EmitAsync`)
- Service (refresh): `src/TukiFact.Infrastructure/Services/DespatchAdviceService.cs:206` (`RefreshStatusAsync`)
- Service (cancel): `src/TukiFact.Infrastructure/Services/DespatchAdviceService.cs:245` (`CancelAsync`)
- Service (polling helper): `src/TukiFact.Infrastructure/Services/DespatchAdviceService.cs:322` (`TryFetchCdrAsync`) — backoff de 2,3,5,8,13s
- Entity: `src/TukiFact.Domain/Entities/DespatchAdvice.cs:3`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/DespatchAdviceRepository.cs:17` (`GetByIdWithItemsAsync`)
- XML builder: `IGreXmlBuilder` (impl. en `GreXmlBuilder.cs`)
- SUNAT client GRE (REST OAuth2): `IGreSunatClient` (impl. en `GreSunatClient.cs`)
- Validator: `DespatchAdviceValidator` aplica solo en `CreateAsync`; las transiciones de estado se enforzan inline en el servicio

### Campos del backend — Request Create
No aplica (vista detalle). Para `cancel`:

`DespatchAdviceController.CancelRequest` (`DespatchAdviceController.cs:124`):

| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `reason` | string? | No | Si vacío, backend usa "Anulación registrada por el contribuyente" | Motivo libre de anulación local | Textarea (3 rows) |

Para `emit` y `refresh-status`: body vacío `{}`.

### Campos del backend — Request Update
No existe endpoint Update. Las transiciones permitidas son:
- `draft → sent / rejected` (vía `/emit`)
- `sent → accepted / rejected` (vía polling inline o `/refresh-status`)
- `accepted / sent → cancelled` (vía `/cancel`)

### Campos del backend — Response
`DespatchAdviceResponse` completo (ver tabla en `/despatch-advices`). En este detalle se muestran adicionalmente todos los campos del receptor, trayecto, conductor o transportista, items.

### Enums / Catálogos SUNAT relevantes
Mismos que `/despatch-advices/new` + status del servicio:
- `status` final: `accepted`, `rejected`, `cancelled`
- `status` intermedios: `draft`, `signed`, `sent`
- El FE mapea `pending_ticket` como variante visual

### Servicios externos invocados (durante emit)
- SUNAT firmado XML: SÍ — `IXmlSigningService.SignXml` (`DespatchAdviceService.cs:146`), enforced (no se procede sin certificado válido)
- SUNAT envío: SÍ — `IGreSunatClient.SendDespatchAdviceAsync` (REST OAuth2, asincrono → devuelve ticket; `DespatchAdviceService.cs:164`)
- SUNAT polling de ticket: SÍ — inline tras emit con backoff exponencial 2,3,5,8,13s (`DespatchAdviceService.cs:322`); si timeout → status queda `sent` y el usuario puede llamar `/refresh-status`
- Lookup RUC/DNI: NO (ya se hizo en `/new`)
- Tipo de cambio: NO aplica
- Email cliente: NO
- MinIO/storage: SÍ — XML firmado, CDR (tras polling exitoso), PDF (tras accepted)
- NATS / event publisher: NO — solo escribe `AuditLog` con actions: `despatch.created`, `despatch.{accepted|rejected|sent}`, `despatch.refreshed`, `despatch.cancelled`

### Validación oficial del backend
Para `/emit`:
- Status actual debe ser `draft` (línea 130)
- Pertenencia al tenant (línea 127)
- Certificado digital presente + no expirado (`EnsureSigningRequirements` línea 368)
- Contraseña certificado presente
- Credenciales GRE (`GreClientId`, `GreClientSecret`) presentes
- Credenciales SOL (`SunatUser`, `SunatPasswordEncrypted`) presentes (`EnsureSunatCredentials` línea 383)

Para `/cancel`:
- Pertenencia al tenant
- No se puede anular si ya `cancelled` (línea 253)
- No se puede anular `draft` (línea 256) — mensaje: "es un borrador — no necesita anulación"
- No se puede anular `rejected` (línea 262) — mensaje: "fue rechazada por SUNAT, ya no está vigente"

Para `/refresh-status`:
- Debe tener `sunatTicket` poblado (línea 214)
- Si ya está en estado terminal (`accepted`/`rejected`) retorna inmediato (línea 218)

### UI propuesto siguiendo DESIGN-CLIENT
- Page header con `{fullNumber}` mono `t-h3` + StatusBadge inline + sub explicativo con fecha emisión y fecha traslado
- Toolbar de acciones (right-aligned, wrap):
  - `XML` / `CDR` / `PDF` (outline) — visibles solo si las URLs respectivas existen
  - `Refrescar estado` (outline, con `RefreshCw` spinning) — solo si `status=sent` y `sunatTicket`
  - `Anular` (outline rojo) — solo si `status ∈ {accepted, sent}`
  - `Emitir a SUNAT` (primary acento) — solo si `status=draft`
- Banner SUNAT response (si `sunatResponseCode` existe): color-mix bg + StatusIcon + código mono + mensaje + ticket mono + hint si `status=sent` indicando que es polling pendiente
- Layout 2/3 + sticky 1/3:
  - Left:
    - `Destinatario`: 3 fields (tipo, doc, nombre)
    - `Trayecto`: dos cards (origen info / destino success) con dirección y ubigeo
    - `Detalles del traslado`: motivo (código mono + descripción), modalidad con icono, fecha inicio, peso + unidad, bultos
    - `Conductor y vehículo` o `Transportista` (condicional)
    - `Mercadería`: tabla read-only con líneas
  - Right sticky:
    - `Estado actual` card con color-mix del status
    - Fields del número, tipo, fechas, ticket
    - Card resumen origen→destino
    - Stats bultos / peso
    - Counter de líneas
- Confirmaciones destructivas: SÍ — dialog para `Anular` con título "Anular guía de remisión", descripción que explica que es solo local (la formal en SOL), textarea opcional para motivo, footer con `Volver` + `Confirmar anulación` (destructive)
- Loading: spinner inline durante emit, refresh, anulación
- Empty state: card "Guía no encontrada" si 404
- Accesibilidad: dialog con focus trap, botones con `aria-label`, badges con texto suficiente

### Estados / flujo
**`/emit` (síncrono pero asíncrono lógico):**
1. Valida estado = draft
2. Carga tenant, valida certificado y credenciales SUNAT
3. Construye XML (`GreXmlBuilder.BuildDespatchAdviceXml`)
4. Firma con certificado (obligatorio — no procede sin)
5. Sube XML firmado a MinIO
6. OAuth2 token GRE con credenciales del tenant
7. POST a SUNAT GRE REST → devuelve `ticket`
8. Si error en send → `status=rejected`, AuditLog `despatch.rejected`, return
9. Si OK → `status=sent`, guarda ticket
10. Polling inline 5 intentos con backoff 2+3+5+8+13s (~31s total max)
11. Cada intento: `GetTicketStatusAsync` → si CDR → guarda en MinIO, `status=accepted/rejected`, genera PDF si accepted
12. Si polling timeout → `status=sent`, usuario debe refrescar manualmente
13. AuditLog `despatch.{status}` con ticket

**`/refresh-status`:**
1. Re-OAuth, re-poll una sola vez (`TryFetchCdrAsync` con backoff completo)
2. Actualiza estado y AuditLog `despatch.refreshed`

**`/cancel` (local únicamente):**
1. Valida transiciones
2. `status=cancelled`
3. Append "[ANULADA] {reason}" al `Note`
4. AuditLog `despatch.cancelled`
5. **NO** envía Comunicación de Baja a SUNAT — el usuario debe hacerlo manualmente en SOL portal (advertencia en UI)

### Edge cases / gotchas del backend
- Tenant scoping: SÍ — `GetByIdWithItemsAsync` carga sin filtro pero `EmitAsync`/`CancelAsync`/`RefreshStatusAsync` validan `entity.TenantId == tenantId` (líneas 127, 211, 250)
- IDOR mitigado: `GetByIdAsync` retorna null si `entity.TenantId != tenantId` (`DespatchAdviceService.cs:300`) — buen patrón, no leakea existencia
- Idempotencia: NO — un doble `/emit` puede generar doble envío (revisar protección, pero el state-check `status=draft` impide la segunda llamada)
- Soft-delete: no
- Currency: no aplica
- Timezone: `IssueTime` se setea con `TimeOnly.FromDateTime(DateTime.UtcNow)` — no usa hora Lima — FLAG
- Audit trail: SÍ — todos los flujos críticos llaman `WriteAuditAsync` (excelente)
- Cancel formal: TODO documentado en código (línea 272–275) — falta enviar el XML de Comunicación de Baja a SUNAT GRE
- PDF generation: only si `status=accepted` y dentro de try/catch — fallos no bloquean el flujo (línea 394)
- Polling timeout (~31s): aceptable para UX síncrona pero podría exceder Cloudflare 100s limit; si SUNAT está lento, queda `sent` y el usuario refresca

### Navegación adyacente
- Desde: click fila en `/despatch-advices` o redirect post-create
- A: `Volver a guías` → `/despatch-advices`; descargas XML/CDR/PDF
- Bulk: no aplica
- Row actions: no aplica (vista única)

---

## /recurring-invoices — Facturación recurrente

### Propósito
Bandeja de programaciones de facturación recurrente: cada fila representa una plantilla
que el scheduler ejecutará en su fecha de próxima emisión. Permite pausar / reanudar /
cancelar inline.

### Endpoints REST consumidos
- `GET /v1/recurring-invoices?page&pageSize&status` — lista paginada
- `PUT /v1/recurring-invoices/{id}` — actualizar status (active/paused/cancelled) o EndDate o notes

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/RecurringInvoicesController.cs:76` (`List`), `:86` (`Update`)
- Service / scheduler: `src/TukiFact.Infrastructure/Services/RecurringInvoiceScheduler.cs` (BackgroundService que dispara emisiones)
- Entity: `src/TukiFact.Domain/Entities/RecurringInvoice.cs:3`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/RecurringInvoiceRepository.cs:18` (`ListAsync`), `:57` (`UpdateAsync`)
- Calculator: `src/TukiFact.Domain/Services/RecurringScheduleCalculator.cs` (utilidades de cálculo de fechas)
- Validator: `src/TukiFact.Application/Validation/RecurringInvoiceValidator.cs:27` (solo aplica en Create)

### Campos del backend — Request Create
No aplica (lista). Ver `/recurring-invoices/new`.

### Campos del backend — Request Update
`UpdateRecurringInvoiceRequest` (`src/TukiFact.Application/DTOs/RecurringInvoices/RecurringInvoiceDTOs.cs:23`):

| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `status` | string? | No | Cliente pasa `active`/`paused`/`cancelled` | Cambio de estado | Botones inline (no editable como texto) |
| `endDate` | DateOnly? | No | — | Cambiar fecha de fin | Input fecha (no implementado en lista — FLAG) |
| `notes` | string? | No | — | Editar notas | Textarea (no implementado en lista — FLAG) |

Reglas de transición en `RecurringInvoicesController.cs:94-112`:
- Si `status="paused"` o `"cancelled"` → setea `NextEmissionDate=null`
- Si `status="active"` y `NextEmissionDate==null` (resume) → recalcula con `RecurringScheduleCalculator.NextAfter`, limpia `ConsecutiveFailures` y `LastError`

### Campos del backend — Response
Wrap custom `{ items[], totalCount, page, pageSize }` (NO usa `pagination` como otros endpoints — inconsistencia, ver observaciones transversales).

Cada item es un `RecurringInvoiceResponse` (`RecurringInvoiceDTOs.cs:29`):

| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `id` | Guid | No | UUID programación |
| `documentType` | string | No | 01 / 03 |
| `serie` | string | No | F001 / B001 |
| `customerDocType` | string | No | 6/1/4/7 |
| `customerDocNumber` | string | No | Doc cliente |
| `customerName` | string | No | Razón social/Nombre |
| `customerAddress` | string | Sí | Dirección |
| `customerEmail` | string | Sí | Email |
| `currency` | string | No | PEN/USD |
| `frequency` | string | No | daily/weekly/biweekly/monthly/yearly |
| `dayOfMonth` | int | Sí | 1–28 (si monthly) |
| `dayOfWeek` | int | Sí | 0–6 (si weekly) |
| `startDate` | DateOnly | No | Fecha inicio |
| `endDate` | DateOnly | Sí | Fecha fin opcional |
| `nextEmissionDate` | DateOnly | Sí | Próxima emisión calculada |
| `status` | string | No | active/paused/cancelled/completed |
| `emittedCount` | int | No | Cuántas emisiones ejecutadas |
| `lastEmittedDate` | DateOnly | Sí | Última emisión exitosa |
| `consecutiveFailures` | int | No | Fallos consecutivos (auto-pause threshold) |
| `lastError` | string | Sí | Último error SUNAT/red |
| `notes` | string | Sí | Notas |
| `createdAt` | DateTimeOffset | No | Timestamp creación |

### Enums / Catálogos SUNAT relevantes
- `documentType`: 01=Factura, 03=Boleta
- `frequency` (TukiFact-specific): `daily`, `weekly`, `biweekly`, `monthly`, `yearly`
- `dayOfWeek`: 0=Domingo … 6=Sábado
- `dayOfMonth`: 1–28 (para garantizar que cae todos los meses)
- `status`: `active`, `paused`, `cancelled`, `completed`
- `customerDocType` (cat. 06): 6/1/4/7 (no 0 — recurring siempre requiere ID identificable)
- `currency`: PEN/USD

### Servicios externos invocados (durante create/update)
- SUNAT firmado XML: NO en lista/update — sí cuando el scheduler dispara emisión (vía `DocumentService.EmitAsync`)
- SUNAT envío: NO en lista; sí en scheduler
- SUNAT polling: NO en lista
- Lookup RUC/DNI: NO en lista
- Tipo de cambio: NO
- Email cliente: NO en lista; el scheduler usa `CustomerEmail` (vista solo del template)
- MinIO/storage: NO
- NATS / event publisher: NO

### Validación oficial del backend
Para `/list`: solo clampea page/pageSize implícitamente.

Para `/update`:
- `recurring.TenantId == GetTenantId()` — enforced (`RecurringInvoicesController.cs:91`) — retorna 404 (no 403) para no leakear existencia
- Transición `paused/cancelled` → null `NextEmissionDate`
- Transición resume → recalcula próxima fecha
- No valida que el nuevo `status` esté en el set permitido — FLAG menor (cliente puede mandar string arbitrario)

### UI propuesto siguiendo DESIGN-CLIENT
- Page header (`t-display-lg`): "Facturación recurrente" + sub: `{totalCount} programaciones en tu cuenta.`
- Filter toolbar:
  - Input search (filtro **client-side**: cliente, doc, serie)
  - PillGroup status: `Todas / Activas / Pausadas / Completadas / Canceladas` (5 chips)
  - `Limpiar` cuando hay filtros
- Tabla:
  - `Tipo` (badge con `DOC_TYPE_LABEL`)
  - `Serie` (mono semibold)
  - `Cliente` truncado + sub doc en mono + (si `consecutiveFailures > 0`) alert inline con `AlertTriangle` rojo: "Último intento falló (N seguidos)" con tooltip = lastError
  - `Frecuencia` (label humano)
  - `Próxima emisión` (mono tnum + icono `CalendarClock`)
  - `Emitidas` (tnum mono right)
  - `Estado` (StatusBadge)
  - `Acciones`: ghost icons inline
    - `Pause` si `status=active`
    - `Play` si `status=paused`
    - `XCircle` rojo si `status ∈ {active, paused}` (cancelar)
- Empty states:
  - Sin programaciones: ilustración + "Aún no tienes facturación recurrente" + CTA
  - Filtrado: "Sin resultados con esos filtros" + Limpiar
- PillGroup candidates: status (5)
- Select candidates: ninguno
- SUNAT lookup: no aplica
- Auto-bridge: search client-side (FLAG — limitado a página actual)
- Confirmaciones destructivas: `Cancelar` (`XCircle`) — el FE actual NO pide confirmación; debería usar dialog. FLAG fuerte (acción destructiva sin confirmar)
- Loading: spinner inline + texto
- Accesibilidad: botones acción con `title` (tooltip), `aria-label` mejorable, badges con texto

### Estados / flujo
Update inline desde la lista:
1. Click pausar → `PUT /v1/recurring-invoices/{id}` body `{ status: "paused" }`
2. Backend setea status + `NextEmissionDate=null`
3. Refetch lista
4. Análogo para reanudar (recalcula próxima fecha) y cancelar (terminal — no se puede revertir)

### Edge cases / gotchas del backend
- Tenant scoping: SÍ — claim `tenant_id` del JWT (`RecurringInvoicesController.cs:27`)
- Idempotencia: PUT es naturalmente idempotente para status
- Soft-delete: no — `cancelled` es soft, no hay hard-delete
- Currency: el `currency` se hereda al documento emitido por el scheduler
- Timezone: `RecurringScheduleCalculator.TodayInLima()` se usa para resume — buen patrón (a diferencia de otros flujos que usan UTC)
- Audit trail: NO se llama `IAuditLogRepository` en el PUT — FLAG
- Concurrency: el scheduler usa `TryClaimAsync` (`RecurringInvoiceRepository.cs:64`) con `ProcessingLockUntil` — bien diseñado contra deploy overlap

### Navegación adyacente
- Desde: sidebar → "Facturación recurrente"
- A: `Nueva recurrente` → `/recurring-invoices/new`; no hay detalle navegable (gap UX — FLAG)
- Bulk: no
- Row actions: pausar/reanudar/cancelar (sin confirm dialog — FLAG)

---

## /recurring-invoices/new — Nueva facturación recurrente

### Propósito
Formulario para programar la emisión automática de un comprobante. El scheduler en
background (`RecurringInvoiceScheduler`) ejecutará la emisión en cada fecha objetivo
usando esta plantilla.

### Endpoints REST consumidos
- `GET /v1/services/lookup/status`
- `GET /v1/services/lookup/{ruc|dni}/{numero}` — autocompletar cliente
- `POST /v1/recurring-invoices` — crear programación

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/RecurringInvoicesController.cs:29` (`Create`)
- Entity: `src/TukiFact.Domain/Entities/RecurringInvoice.cs:3`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/RecurringInvoiceRepository.cs:51` (`AddAsync`)
- Validator: `src/TukiFact.Application/Validation/RecurringInvoiceValidator.cs:27` (`Validate`) — invocado en `RecurringInvoicesController.cs:32`
- Scheduler (consumidor): `src/TukiFact.Infrastructure/Services/RecurringInvoiceScheduler.cs`

### Campos del backend — Request Create
`CreateRecurringInvoiceRequest` (`src/TukiFact.Application/DTOs/RecurringInvoices/RecurringInvoiceDTOs.cs:5`):

| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `documentType` | string | Sí | ∈ {01, 03} | Tipo comprobante a emitir | PillGroup 2 |
| `serie` | string | Sí | `^F\d{3}$` si 01, `^B\d{3}$` si 03 | Serie | Input mono uppercase, max 4 |
| `customerDocType` | string | Sí | ∈ {0,1,4,6,7}; si docType=01 debe ser `6` | Tipo doc cliente | PillGroup 4 (no incluye 0 — coherente) |
| `customerDocNumber` | string | Sí | 11d si 6, 8d si 1 | Doc cliente | Input mono |
| `customerName` | string | Sí | ≤ 200 chars | Nombre/Razón social | Input texto |
| `customerAddress` | string? | No | ≤ 300 chars | Dirección | Input texto |
| `customerEmail` | string? | No | Email regex + ≤ 200 chars | Email cliente | Input email |
| `currency` | string | No (default PEN) | ∈ {PEN, USD} | Moneda | PillGroup 2 |
| `frequency` | string | Sí | ∈ {daily,weekly,biweekly,monthly,yearly} | Cadencia | PillGroup 5 |
| `dayOfMonth` | int? | Cond. (si monthly) | 1–28 | Día del mes | Input número min1 max28 |
| `dayOfWeek` | int? | Cond. (si weekly) | 0–6 | Día semana | Select 7 opciones |
| `startDate` | DateOnly | Sí | `>= hoy` | Fecha inicio | Input fecha |
| `endDate` | DateOnly? | No | `>= startDate` si presente | Fecha fin opcional | Input fecha |
| `notes` | string? | No | ≤ 500 chars | Notas que aparecen en cada emisión | Textarea |
| `items[]` | List | Sí | ≥ 1 | Plantilla de líneas | Tabla editable |

`items[]` reutiliza `CreateDocumentItemRequest`:

| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `description` | string | Sí | ≤ 250 chars | Descripción | Input texto |
| `quantity` | decimal | Sí | > 0 | Cantidad | Input número tnum |
| `unitMeasure` | string | Sí (default NIU) | — | Catálogo 03 | Input mono / Select |
| `unitPrice` | decimal | Sí | ≥ 0 | Precio unitario sin IGV | Input número con prefijo S// $ |
| `igvType` | string | Sí | ∈ {10, 20, 30} | Catálogo 07 | Select |
| `productCode`, `sunatProductCode`, `discount` | — | No | — | Heredados pero no expuestos en UI actual | — |

### Campos del backend — Request Update
Ver `/recurring-invoices` — solo `status`, `endDate`, `notes`.

### Campos del backend — Response
`RecurringInvoiceResponse` (ver tabla en `/recurring-invoices`). Tras create:
- `nextEmissionDate = startDate`
- `status = "active"`
- `emittedCount = 0`
- `consecutiveFailures = 0`

### Enums / Catálogos SUNAT relevantes
- `documentType`: 01=Factura, 03=Boleta
- `customerDocType` (cat. 06): 6=RUC, 1=DNI, 4=CE, 7=Pasaporte (no 0 — recurring requiere identificación estable)
- `igvType` por ítem (cat. 07): 10=Gravado, 20=Exonerado, 30=Inafecto
- `currency`: PEN, USD
- `frequency` (TukiFact): daily/weekly/biweekly/monthly/yearly

### Servicios externos invocados (durante create/update)
- SUNAT firmado XML: NO en create (la emisión real la dispara el scheduler después)
- SUNAT envío: NO en create
- SUNAT polling: NO en create
- Lookup RUC/DNI: SÍ (opcional)
- Tipo de cambio: NO (gap — si `currency=USD`, el scheduler tendrá el mismo problema que `/documents/new`)
- Email cliente: NO en create; SÍ en cada emisión del scheduler (usa `CustomerEmail`)
- MinIO/storage: NO en create
- NATS: NO

### Validación oficial del backend
Validador `RecurringInvoiceValidator.Validate` agrega todos los errores (`RecurringInvoiceValidator.cs:27`):

- DocumentType ∈ {01, 03}
- Serie regex según tipo (F001/B001)
- CustomerDocType ∈ {0,1,4,6,7}
- Si docType=01 → customerDocType debe ser 6 (RUC obligatorio para factura)
- Si customerDocType=6 → 11 dígitos (NO valida mod-11 — FLAG menor, comparar con despatch que sí lo hace)
- Si customerDocType=1 → 8 dígitos
- CustomerName no vacío + ≤ 200
- CustomerAddress ≤ 300
- CustomerEmail regex + ≤ 200 (si presente)
- Currency ∈ {PEN, USD}
- Frequency ∈ catálogo
- Si monthly → dayOfMonth ∈ [1, 28]
- Si weekly → dayOfWeek ∈ [0, 6]
- StartDate >= hoy (UTC — FLAG menor)
- EndDate >= StartDate
- Notes ≤ 500
- Items ≥ 1; por ítem: description no vacío + ≤ 250, quantity > 0, unitPrice ≥ 0, igvType ∈ {10,20,30}

### UI propuesto siguiendo DESIGN-CLIENT
- Page header (`t-display-lg`): "Nueva facturación recurrente" + sub
- Layout 2/3 + sticky 1/3
- Form sections:
  1. `Comprobante` — PillGroup tipo (2), Input serie con hint, PillGroup moneda (2)
  2. `Cliente` — PillGroup tipo doc (4 col), Input doc + lookup, Input nombre, dirección, email
  3. `Programación` — PillGroup frecuencia (5 col-5), conditional Input día del mes (1–28) o Select día semana, Input fecha inicio, Input fecha fin
  4. `Plantilla de items` — tabla editable con descripción / cantidad / precio + prefijo moneda / IGV
  5. `Notas` — Textarea
- PillGroup candidates: `documentType` (2), `customerDocType` (4), `currency` (2), `frequency` (5 — frontera, aceptable)
- Select candidates: `dayOfWeek` (7), `igvType` por ítem (3)
- SUNAT lookup: SÍ — en Cliente
- Auto-bridge / smart helpers:
  - Auto-align serie prefix (F/B) cuando cambia documentType (`new/page.tsx:244-250`)
  - Cálculo client-side de totales preview (gravado/exonerado/inafecto/IGV/total) en sticky
  - `cadenceText` humano en el sticky ("Cada mes, día 1", "Cada semana, lunes", etc.)
- Empty states: tabla siempre tiene una línea inicial
- Loading: spinner en submit
- Error feedback: toast
- Confirmaciones destructivas: solo "Quitar línea"
- Faltantes vs DTO — FLAGs menores: `productCode`, `discount`, `sunatProductCode` no expuestos en UI

### Estados / flujo
Después del submit:
1. Validador agrupado del backend → 400 con `{ errors: [...] }` si hay violaciones
2. Crea `RecurringInvoice` con `Status=active`, `NextEmissionDate=StartDate`, items serializados como JSON en `ItemsJson`
3. Persiste
4. Responde 201 con el DTO
5. Frontend redirige a `/recurring-invoices` (lista)
6. **El scheduler (`RecurringInvoiceScheduler` BackgroundService) iterará cada tick, llamará `GetDueForEmissionAsync`, hará `TryClaimAsync` (lock optimista), llamará `ReserveEmissionAsync` (transacción), invocará `IDocumentService.EmitAsync` con el snapshot del template, actualizará `EmittedCount` / `LastEmittedDate` / `ConsecutiveFailures` / `LastError`, y avanzará `NextEmissionDate` con `RecurringScheduleCalculator.NextAfter`**
7. Auto-pause: tras N fallos consecutivos el scheduler puede pausar (verificar implementación)
8. Email cliente: SÍ — se intenta enviar PDF/XML al `customerEmail` tras cada emisión exitosa
9. **NO** publica NATS en create

### Edge cases / gotchas del backend
- Tenant scoping: SÍ — JWT claim
- Idempotencia create: NO — un retry crearía duplicado. El scheduler sí es idempotente por la transacción `ReserveEmissionAsync` con unique constraint en `(RecurringInvoiceId, TargetDate)`
- Soft-delete: no
- Currency: si USD y no hay tipo de cambio, las emisiones fallarán — FLAG
- Timezone: `RecurringScheduleCalculator.TodayInLima()` se usa para resume; pero en `RecurringInvoiceValidator.cs:112` `today = DateOnly.FromDateTime(DateTime.UtcNow)` — inconsistencia UTC vs Lima dentro del mismo flujo — FLAG
- Audit trail: NO se escribe en create — FLAG
- Concurrency scheduler: `TryClaimAsync` con `ProcessingLockUntil` previene doble emit en deploy overlap
- Retry policy: fallos suben `ConsecutiveFailures` y guardan `LastError`; auto-pause threshold verificar en `RecurringInvoiceScheduler.cs`

### Navegación adyacente
- Desde: botón "Nueva recurrente" en `/recurring-invoices`
- A: tras crear → `/recurring-invoices` (lista, no hay vista detalle — gap UX)
- `Cancelar` → `/recurring-invoices`
- Bulk: no aplica
- Row actions: agregar/quitar línea, lookup cliente

---

## Group A — Observaciones transversales

Patrones compartidos y observaciones encontradas en estos 8 flujos:

### Patrones comunes
1. **Tenant scoping consistente**: todos los endpoints derivan `tenantId` del JWT vía `ITenantProvider.GetCurrentTenantId()` o claim `tenant_id` directo. Las queries siempre filtran por `TenantId` en el repo.
2. **Repository pattern uniforme**: cada agregado tiene `IXxxRepository` con `GetByIdAsync`, `ListAsync`/`GetByTenantAsync`, `CreateAsync`/`AddAsync`, `UpdateAsync`. Implementaciones EF Core con `Include(...)` para items.
3. **Paginación**: page+pageSize con clamp en controller (`page ≥ 1`, `pageSize ∈ [1, 100]`) — bien.
4. **Series correlativa**: el documento y la GRE usan correlativos numéricos por serie con `{Serie}-{Correlative:D8}` como `FullNumber` computado. La GRE tiene retry de hasta 8 veces en colisión de unique constraint (`DespatchAdviceRepository.cs:76`); el flujo de Document no muestra ese retry — FLAG inconsistencia.
5. **MinIO storage paths**: XML / CDR / PDF se guardan como `{bucket}/{key}` y se devuelven como string en el response; download del FE es `fetch` con `Authorization` Bearer + `Blob`.
6. **Lookup RUC/DNI**: los 3 formularios (documents/new, despatch-advices/new, recurring-invoices/new) replican el mismo bloque de `useLookup()` con `/v1/services/lookup/status` y `/v1/services/lookup/{type}/{number}` — candidato a hook reutilizable.
7. **PillGroup component**: reimplementado localmente en cada `new/page.tsx` con pequeñas variaciones (cols). Candidato a componente compartido del design system.
8. **AuditLog**: GRE lo registra exhaustivamente (`despatch.created/accepted/rejected/sent/refreshed/cancelled`); Documents y RecurringInvoices NO escriben audit. FLAG inconsistencia.
9. **Catálogos SUNAT inline**: el FE hardcodea catálogos (DOC_TYPES, IGV, FREQUENCIES) en cada página. Existe `CatalogsController` en el backend pero no se consume desde estos formularios — FLAG (single-source-of-truth).

### Inconsistencias detectadas

| Tema | Documents | DespatchAdvices | RecurringInvoices |
|---|---|---|---|
| **Validador formal** | ❌ ninguno | ✅ `DespatchAdviceValidator` | ✅ `RecurringInvoiceValidator` |
| **Audit log** | ❌ no escribe | ✅ todos los flujos | ❌ no escribe |
| **Wrap respuesta lista** | `{ data, pagination: { page, pageSize, totalCount, totalPages } }` | mismo | `{ items, totalCount, page, pageSize }` ← inconsistencia |
| **Retry correlativo** | ❌ no visible (depende de `SeriesRepository.GetNextCorrelativeAsync`) | ✅ retry hasta 8 con detección PG 23505 | N/A |
| **Timezone (today)** | UTC (`DateTime.UtcNow`) | UTC | UTC en validador, Lima en calculator del scheduler — incongruente intra-flujo |
| **Email cliente al emitir** | ❌ no envía (gap) | ❌ no envía | ✅ scheduler usa CustomerEmail |
| **NATS publish on emit** | ❌ no se publica (handlers existen huérfanos) | ❌ no se publica | ❌ no se publica |
| **PDF post-emisión** | sí, on-demand vía `/pdf` | sí, generado tras `accepted` y subido a MinIO | depende del flujo emit (via DocumentService) |
| **Confirmación destructiva en UI** | ✅ dialog para Anular | ✅ dialog para Anular | ❌ Cancelar inline sin confirm |
| **Mod-11 RUC** | ❌ no enforced | ✅ enforced | ❌ solo longitud 11 enforced |

### Gaps funcionales relevantes (FLAGs fuertes)

1. **Voided document flow incompleto**: `VoidedDocumentsController.VoidDocument` crea el registro local pero NO construye el XML 30/RA firmado, NO lo envía a SUNAT, y NO hay polling de ticket. Es un placeholder funcional.
2. **Cancel GRE incompleto**: `DespatchAdviceService.CancelAsync` tiene `TODO` explícito (línea 272–275) — solo anula localmente sin Comunicación de Baja formal.
3. **Tipo de cambio SBS**: ningún flujo invoca `IExchangeRateService` durante la emisión, aunque la interfaz existe. Si `currency=USD`, las emisiones probablemente fallen por validación SUNAT.
4. **Eventos NATS huérfanos**: existen `DocumentCreatedHandler`, `DocumentSentHandler`, `DocumentFailedHandler`, `NotificationEventHandler` y `IEventPublisher`, pero `DocumentService.EmitAsync` y `DespatchAdviceService.EmitAsync` NUNCA publican eventos. Los handlers están desconectados de la realidad.
5. **Email cliente para `/documents`**: el campo `CustomerEmail` se persiste pero `EmitAsync` no envía nada. Recurring sí lo hace.
6. **Audit gap en Documents y RecurringInvoices**: ambos modifican estado significativo sin registrar en `IAuditLogRepository`.
7. **`PUT /v1/recurring-invoices/{id}`** acepta cualquier string en `status` sin validar contra el set permitido.
8. **Fecha UTC vs Lima**: peligro de off-by-one en fechas de emisión para tenants peruanos cerca de medianoche local.
9. **Sin idempotency-key en POSTs**: ningún endpoint de creación soporta idempotency-key — un retry de cliente crea duplicados.
10. **Search client-side limitado**: en `/despatch-advices` y `/recurring-invoices`, el search filtra solo la página actual; no llega al backend.

### Recomendaciones inmediatas para DESIGN.md (sin entrar en código)

- **Componente shared `<PillGroup>`** en el design system, con props `cols`, `options`, `value`, `onChange`, soportando ícono + sub.
- **Hook compartido `useSunatLookup()`** que encapsule el patrón `lookupStatus + lookup(docType, docNumber)`.
- **Hook `usePaginatedList<T>(endpoint, filters)`** que normalice los dos wraps de paginación incongruentes.
- **Componente `<StatusBadge>`** con mapa centralizado de estado→color/icono compartido entre Documents / GRE / Recurring.
- **Catálogos SUNAT desde el backend**: consumir `CatalogsController` en lugar de hardcodear en el FE, para sincronizar nuevos códigos.
- **Toolbar de filtros estándar**: PillGroup row + date range + search + Limpiar como bloque reutilizable.
- **Sticky summary aside** como patrón establecido para todos los formularios largos (2/3 + 1/3).
- **Confirm dialogs en cualquier acción destructiva**, incluso inline en listas.
# Group B — Cotizaciones · Percepciones/Retenciones · Voided · Exchange Rates

Auditoría per-pantalla. Rutas relativas al repo root `TukiFact/`. Frontend en `src/tukifact-web/src/app/(authenticated)/`. Texto en es-PE.

---

## /quotations — Cotizaciones

### Propósito
Listar todas las cotizaciones del tenant, con paginación y filtro por estado. Punto de entrada al flujo de propuesta-de-venta-antes-de-emitir.

### Endpoints REST consumidos
- `GET /v1/quotations?page&pageSize&status` — listado paginado por tenant

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/QuotationsController.cs:106` (`List`)
- Service: no existe servicio dedicado — la lógica vive en el controller
- Entity: `src/TukiFact.Domain/Entities/Quotation.cs:3`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/QuotationRepository.cs:19` (`ListAsync`)
- Validator: no tiene validador — **FLAG**

### Campos del backend — Request Create
No aplica (pantalla de lectura). Ver `/quotations/new`.

### Campos del backend — Request Update
No aplica.

### Campos del backend — Response
Cada item del listado es `QuotationResponse` (`src/TukiFact.Application/DTOs/Quotations/QuotationDTOs.cs:28`):

| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| id | Guid | no | PK |
| quotationNumber | string | no | Numeración interna `COT-000123` |
| correlative | long | no | Correlativo numérico del tenant |
| issueDate | DateOnly | no | Fecha de emisión |
| validUntil | DateOnly | no | Hasta cuándo es válida la propuesta |
| customerDocType | string | no | Catálogo SUNAT 06 (1=DNI, 6=RUC, …) |
| customerDocNumber | string | no | RUC/DNI/CE del cliente |
| customerName | string | no | Razón social o nombres |
| customerEmail | string | sí | Email del cliente |
| currency | string | no | PEN / USD |
| subtotal | decimal(14,2) | no | Subtotal sin IGV |
| igv | decimal(14,2) | no | IGV consolidado |
| total | decimal(14,2) | no | Total cotizado |
| status | string | no | draft / sent / approved / invoiced / cancelled / expired |
| invoiceDocumentId | Guid | sí | Documento factura generado |
| invoiceDocumentNumber | string | sí | Número completo (F001-…) si fue convertida |
| pdfUrl | string | sí | PDF de la cotización |
| notes | string | sí | Observaciones |
| createdAt | DateTimeOffset | no | Fecha de creación |
| items | List\<QuotationItemResponse\> | no | Líneas |

Envuelto en `{ items, totalCount, page, pageSize }` (controller línea 115).

### Enums / Catálogos SUNAT relevantes
- **Estados de cotización** (string libre, default `draft`): `draft`, `sent`, `approved`, `invoiced`, `cancelled`, `expired`.
- **Catálogo SUNAT 06** (Tipo de documento de identidad): 0 sin doc, 1 DNI, 4 CE, 6 RUC, 7 Pasaporte.

### Servicios externos invocados (durante create/update)
No aplica en listado. Ningún side-effect.

### Validación oficial del backend
No hay validador FluentValidation. Las únicas reglas son las del entity / migration:
- `Status` libre, max 20 chars (no se enforce el conjunto enumerado — **FLAG**: cualquier string entra).
- `pageSize` no tiene cap superior (**FLAG**: posible DoS por `pageSize=1000000`).

### UI propuesto siguiendo DESIGN-CLIENT
- Page header `t-display-lg` con conteo total y CTA `Nueva Cotización` (ya existe en clásico — falta migrar a tipografía DESIGN).
- Toolbar pegada al header: chip-group de estados (`Todos · Borrador · Enviada · Aprobada · Facturada · Cancelada`) + búsqueda debounced por `quotationNumber` / `customerName` / `customerDocNumber`. Hoy solo hay un `<Select>` de estado y no hay search.
- Tabla con cabeceras `t-overline`, hairlines de 1px, filas con hover, `tnum mono` para montos y números.
- `StatusBadge` con tokens OKLCH (`--accent` para aprobada/facturada, `--warning` para enviada, `--danger` para cancelada, `--muted` para borrador). Reemplazar `<Badge variant=…>`.
- Empty state inicial: ícono `FileSpreadsheet`, copy "Aún no has cotizado a un cliente" + CTA primario `Nueva cotización`.
- Empty state filtrado: ícono `Inbox`, copy "Sin resultados con esos filtros" + `Limpiar filtros`.
- Smart helper: chip `Vencen pronto (≤7 días)` que filtre `validUntil <= today+7 && status in (sent, approved)`.

### Estados / flujo
Solo lectura. Click en fila navega a `/quotations/[id]` (controller línea 144 del frontend). No se firma ni se envía nada a SUNAT desde acá: una cotización es un documento interno; solo se sube a SUNAT cuando se ejecuta `convert-to-invoice` y la factura resultante sí se firma + envía.

### Edge cases / gotchas del backend
- Tenant scoping: filtra por `q.TenantId == tenantId` en repo línea 24. El JWT debe traer claim `tenant_id` (controller línea 30).
- Idempotencia: ninguna; el listado puede ser inconsistente entre páginas si se inserta en paralelo (no usa snapshot isolation).
- Currency: mezclado PEN/USD en el listado, el frontend formatea con `Intl.NumberFormat` del row → muestra símbolos distintos por fila. OK.
- Timezone: `CreatedAt` es UTC pero se ordena `OrderByDescending(CreatedAt)`; al usuario se le muestra `IssueDate` (DateOnly, sin TZ).
- Audit: cada Quotation guarda `CreatedByUserId` (no expuesto en la response).

### Navegación adyacente
- Desde sidebar → "Cotizaciones".
- A `/quotations/new` vía botón "Nueva Cotización".
- A `/quotations/[id]` vía click en fila o ícono ojo.

---

## /quotations/new — Nueva Cotización

### Propósito
Formulario full-page para crear una cotización: cliente + vigencia + ítems + notas/T&C, con lookup RUC/DNI opcional. Calcula totales en vivo.

### Endpoints REST consumidos
- `GET /v1/services/lookup/status` — saber si hay proveedor de lookup configurado (badge en sección Cliente)
- `GET /v1/services/lookup/ruc/{ruc}` — autocompletar razón social y dirección por RUC
- `GET /v1/services/lookup/dni/{dni}` — autocompletar nombres por DNI
- `POST /v1/quotations` — crear la cotización

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/QuotationsController.cs:32` (`Create`)
- Lookup controller: `src/TukiFact.Api/Controllers/ExternalServicesController.cs:170` (`LookupRuc`), `:194` (`LookupDni`), `:281` (`LookupStatus`)
- Service: no hay servicio — toda la lógica de cálculo de items/IGV vive en el controller (líneas 58–91)
- Entity: `src/TukiFact.Domain/Entities/Quotation.cs:3` + `QuotationItem` en `:58`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/QuotationRepository.cs:52` (`AddAsync`) + `:44` (`GetNextCorrelativeAsync`)
- Validator: no tiene validador — **FLAG**

### Campos del backend — Request Create
`CreateQuotationRequest` (`src/TukiFact.Application/DTOs/Quotations/QuotationDTOs.cs:3`):

| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| issueDate | DateOnly? | no | si null → `DateOnly.FromDateTime(DateTime.UtcNow)` (ctrl :43) | Fecha de emisión | Input fecha (oculto, default hoy) |
| validUntil | DateOnly | sí | sin chequeo `>= issueDate` (**FLAG**) | Hasta cuándo vale la propuesta | Input fecha |
| customerDocType | string | sí | max 1 char (cfg :22). Sin enforce de catálogo (**FLAG**) | Catálogo SUNAT 06 | PillGroup `[RUC · DNI · CE · Pasaporte · Sin doc]` |
| customerDocNumber | string | sí | max 15 chars. Sin chequeo de largo por tipo (**FLAG**) | Documento del cliente | Input mono con botón "Buscar" si tipo ∈ {6,1} |
| customerName | string | sí | max 200 | Razón social o nombres | Input texto |
| customerAddress | string? | no | max 300 | Domicilio | Input texto |
| customerEmail | string? | no | max 200. Sin regex de email (**FLAG**) | Email del cliente | Input email |
| customerPhone | string? | no | max 20 | Teléfono | Input texto |
| currency | string | sí | max 3, default "PEN" | Moneda | Select `PEN / USD` |
| notes | string? | no | max 1000 | Notas que aparecen en el PDF | Textarea |
| termsAndConditions | string? | no | max 2000 | Forma de pago, garantía, etc. | Textarea |
| items | List\<CreateQuotationItemRequest\> | sí | no se valida `Count >= 1` (**FLAG**) | Líneas | Tabla editable |

`CreateQuotationItemRequest` (`:18`):

| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| productCode | string? | no | max 30 | Código interno (no SUNAT) | Input mono |
| description | string | sí | max 500 | Descripción del bien/servicio | Input texto |
| quantity | decimal | sí | precision (14,4). Sin `> 0` (**FLAG**) | Cantidad | Input número |
| unitMeasure | string | sí | max 5, default "NIU" | Catálogo SUNAT 03 UN/ECE | Input mono / Select |
| unitPrice | decimal | sí | precision (14,4). Sin `>= 0` | Precio unitario sin IGV | Input número |
| igvType | string | sí | max 2 | "10"=Gravado, "20"=Exonerado, "30"=Inafecto. Hardcodeado a 0.18 si "10" (ctrl :66) | PillGroup o Select |
| discount | decimal | no, default 0 | precision (14,2) | Descuento por línea | Input número |

### Campos del backend — Request Update
Update no implementado para Quotation (solo `PUT /v1/quotations/{id}/status` cambia el campo `Status`). Ver detalle.

### Campos del backend — Response
Devuelve `QuotationResponse` (mismo schema que en `/quotations`). El controller setea `QuotationNumber = $"COT-{correlative:D6}"` (línea 41) y calcula `Subtotal`, `Igv`, `Total`, `TotalDiscount` server-side (líneas 88–91). Sin XML, sin SUNAT, sin CDR — los campos `XmlUrl/CdrUrl` no existen en Quotation.

### Enums / Catálogos SUNAT relevantes
- **Catálogo 01 — Tipo de operación / IGV** (`IgvType`): 10 Gravado · 20 Exonerado · 30 Inafecto · 40 Exportación. (Aquí solo se contemplan 10/20/30; "40" no aparece en el frontend pero el backend lo acepta — **FLAG**).
- **Catálogo 03 — Unidad de medida UN/ECE**: NIU (unidad), ZZ, KGM, etc.
- **Catálogo 06 — Tipo de documento de identidad**: 0 sin doc, 1 DNI, 4 CE, 6 RUC, 7 Pasaporte.
- **Moneda** ISO 4217: PEN, USD.
- **Estado de cotización** (no SUNAT): draft, sent, approved, invoiced, cancelled, expired.

### Servicios externos invocados (durante create/update)
- SUNAT firmado XML: **NO** (cotización es interna)
- SUNAT envío (SOAP/REST): **NO**
- SUNAT polling de ticket: **NO**
- Lookup RUC/DNI: **SÍ** (vía `/v1/services/lookup/{ruc,dni}/{n}`), opcional y previo al submit
- Tipo de cambio (SBS): **NO** (la cotización no convierte moneda)
- Email cliente: **NO** (el campo `customerEmail` se guarda pero no se envía mail; ver controller — no llama a `IEmailService`)
- MinIO/storage: **NO**
- NATS / event publisher: **NO**

### Validación oficial del backend
**No hay validador FluentValidation.** Las únicas reglas son las del EF Configuration (`QuotationConfiguration.cs`):
- `QuotationNumber` required, max 20
- `CustomerDocType` required, max 1
- `CustomerDocNumber` required, max 15
- `CustomerName` required, max 200
- `Currency` max 3, default PEN
- `Subtotal/Igv/Total` precision (14,2)
- Unique index `(TenantId, QuotationNumber)` (cfg :56)

Reglas **no enforced** que faltan:
- `ValidUntil >= IssueDate` (**FLAG**)
- `Items.Count >= 1` (**FLAG**)
- `Quantity > 0`, `UnitPrice >= 0` por línea (**FLAG**)
- `Status ∈ {draft, sent, approved, invoiced, cancelled, expired}` (**FLAG**)
- Email format (**FLAG**)
- RUC = 11 dígitos numéricos, DNI = 8 dígitos (solo se valida en el frontend al hacer lookup, no al hacer POST)

### UI propuesto siguiendo DESIGN-CLIENT
Esta pantalla ya está bastante alineada con DESIGN-CLIENT (PillGroup, secciones, sticky summary, `t-display-lg`). Mantener y reforzar:
- Form 2/3 (cliente · vigencia · ítems · notas) + sticky 1/3 (resumen con `t-num-lg` para total).
- PillGroup para `customerDocType` (5 opciones) — ya implementado.
- PillGroup para `igvType` (3 opciones) en lugar del `<Select>` actual.
- SUNAT lookup en banner secundario cuando `lookupStatus.configured === false`, con CTA "Configurar en Ajustes → Servicios Externos".
- Auto-bridge: si el RUC ya existe como `Customer` del tenant (catálogo de clientes), prellenar TODO + chip "Cliente existente — actualizar".
- Smart helper: input de `validUntil` con quick-chips `+7d · +15d (default) · +30d · Fin de mes`.
- Smart helper en tabla de ítems: botón "Importar desde catálogo de productos" (productos del tenant).
- Validación inline antes del submit (mostrar `t-caption` rojo bajo cada input).

### Estados / flujo
Después del submit:
1. Backend obtiene siguiente correlativo por tenant (no por serie — Quotation no tiene serie).
2. Calcula totales por ítem y consolidados.
3. INSERT en `quotations` + `quotation_items`.
4. Devuelve `201 Created` con la response completa.
5. Frontend redirige a `/quotations/[id]`.

No hay draft → signed → sent → accepted. El status nace `draft` y solo cambia por acciones manuales en el detalle.

### Edge cases / gotchas del backend
- **Race condition en correlativo**: `GetNextCorrelativeAsync` hace `MAX(Correlative) + 1` sin lock (`QuotationRepository.cs:44`). Dos requests concurrentes pueden generar el mismo número → choca con unique index `(TenantId, QuotationNumber)` y la segunda revienta. **FLAG**: necesita advisory lock o columna `nextval` tipo sequence.
- Tenant scoping: por claim `tenant_id` del JWT; no se valida que el `CreatedByUserId` pertenezca al mismo tenant (asumido por el middleware de auth).
- Currency: `Currency` se acepta cualquier 3 chars; no hay enforce ISO 4217.
- IGV hardcoded: 18% literal (`IgvRate = 0.18m`, ctrl :18). Si SUNAT cambia la tasa, hay que recompilar.
- Timezone: `IssueDate` se setea con `DateTime.UtcNow` cuando no se envía — el tenant peruano (UTC-5) puede ver "ayer" si emite tarde-noche. **FLAG**.
- Sin idempotency key: doble click del botón puede crear 2 cotizaciones distintas.

### Navegación adyacente
- Desde `/quotations` botón "Nueva Cotización".
- Tras éxito → `/quotations/[id]`.
- Cancelar → `/quotations`.

---

## /quotations/[id] — Detalle de Cotización

### Propósito
Mostrar la cotización completa, permitir avanzar el estado (`draft → sent → approved`) y convertirla a factura electrónica (**diferenciador clave**). También permite cancelarla.

### Endpoints REST consumidos
- `GET /v1/quotations/{id}` — leer detalle
- `PUT /v1/quotations/{id}/status` body `{ status }` — cambiar estado
- `POST /v1/quotations/{id}/convert-to-invoice` body `{ serie, documentType }` — generar factura

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/QuotationsController.cs:99` (`GetById`), `:119` (`UpdateStatus`), `:132` (`ConvertToInvoice`)
- Service: `IDocumentService.EmitAsync` (invocado desde `convert-to-invoice`, ctrl :163)
- Entity: `src/TukiFact.Domain/Entities/Quotation.cs:3`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/QuotationRepository.cs:13` (`GetByIdWithItemsAsync`), `:58` (`UpdateAsync`)
- Validator: no tiene validador — **FLAG**

### Campos del backend — Request Create
No aplica (es detalle).

### Campos del backend — Request Update
Dos DTOs muy delgados:

**`UpdateQuotationStatusRequest`** (`QuotationsController.cs:193`):

| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| status | string | sí | sin enforce de conjunto (**FLAG**) | Nuevo estado | (interno) botones contextuales |

**`ConvertToInvoiceRequest`** (`QuotationsController.cs:194`):

| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| serie | string | sí | sin chequeo de existencia (**FLAG**: si la serie no está dada de alta en `series` table, igual se intentará emitir) | Serie de factura destino (F001/B001) | Input mono con autocompletado desde `/v1/series` |
| documentType | string? | no | default "01" (factura) | "01"=Factura, "03"=Boleta | PillGroup `[Factura · Boleta]` |

### Campos del backend — Response
`GetById` → `QuotationResponse` (mismo schema).

`UpdateStatus` → `QuotationResponse` actualizado.

`ConvertToInvoice` → objeto compuesto `{ quotation: QuotationResponse, invoice: DocumentResponse }` (ctrl :174). El `invoice.fullNumber` se muestra en banner verde de éxito.

### Enums / Catálogos SUNAT relevantes
- **Catálogo 01** — Tipo de comprobante de pago: 01 Factura · 03 Boleta · 07 NC · 08 ND.
- **Estados de cotización**: draft → sent → approved → invoiced (terminal) · cancelled (terminal) · expired.
- Transiciones permitidas en `ConvertToInvoice`: `draft | sent | approved` (front filtra con `canConvert`, ctrl :138 valida server-side `Status != "invoiced"`).

### Servicios externos invocados (durante create/update)
**Endpoints `GetById` y `UpdateStatus`**: ninguno.

**Endpoint `ConvertToInvoice`** (delega en `IDocumentService.EmitAsync`):
- SUNAT firmado XML: **SÍ** (lo hace el `DocumentService` interno)
- SUNAT envío (SOAP/REST): **SÍ** (igual que cualquier factura nueva)
- SUNAT polling de ticket: **NO** (factura/boleta usa SOAP síncrono, no ticket)
- Lookup RUC/DNI: **NO** (datos ya están en la cotización)
- Tipo de cambio (SBS): depende de `DocumentService` — si la cotización fue en USD, sí
- Email cliente: depende de `DocumentService`
- MinIO/storage: **SÍ** (XML/CDR de la factura emitida)
- NATS / event publisher: depende de `DocumentService`

### Validación oficial del backend
Sin FluentValidation. Reglas codificadas en `ConvertToInvoice`:
- `quotation.TenantId == GetTenantId()` (ctrl :137) — tenant scoping
- `quotation.Status != "invoiced"` (ctrl :138) — idempotencia manual

Reglas **no enforced**:
- Transición de estado válida: cualquier string entra a `UpdateStatus`. Hoy el front evita transiciones inválidas pero un cliente API directo puede saltar de `draft` a `expired`.
- Que la serie exista en `series` del tenant.
- Que la cotización no esté `expired` antes de convertir (sin chequeo de `validUntil < today`).

### UI propuesto siguiendo DESIGN-CLIENT
- Page header con breadcrumb `Cotizaciones › COT-000123`, `t-display-lg` para el número.
- Status pill OKLCH al lado del número.
- Toolbar de acciones contextuales según `status`:
  - `draft`: `[Enviar al cliente] [Convertir a factura] [Cancelar]`
  - `sent`: `[Marcar como aprobada] [Convertir a factura] [Cancelar]`
  - `approved`: `[Convertir a factura]` (CTA primario) `[Cancelar]`
  - `invoiced`: `[Ver factura F001-…]` (link a `/documents/[id]`), sin acciones.
  - `cancelled`/`expired`: solo lectura.
- Grid 2 columnas: Cliente · Totales (ya existe).
- Tabla de items con `t-overline`, `tnum mono`, hairlines.
- Modal `max-w-2xl` para "Convertir a factura": PillGroup `[Factura · Boleta]` + serie con autocompletado desde el endpoint de series.
- Smart helper: badge `Vence en 3 días` si `validUntil - today <= 7`.
- Banner verde estándar "Convertida a factura …" ya existe, falta migrar a tokens (`--success` en vez de `bg-green-50`).

### Estados / flujo
- `draft` (default al crear) → `sent` → `approved` → `invoiced` (terminal).
- En cualquier momento (excepto `invoiced`/`cancelled`): `cancelled`.
- `expired` lo asigna implícitamente el cliente (filtro) — **el backend NO lo asigna automáticamente** (no hay worker que recorra `validUntil < today AND status IN (draft, sent, approved)` y los marque). **FLAG**.
- Al convertir: crea Document tipo 01 vía `IDocumentService.EmitAsync` (firma + envía SUNAT), guarda `InvoiceDocumentId` y `InvoiceDocumentNumber`, cambia `Status = "invoiced"`.

### Edge cases / gotchas del backend
- **`expired` jamás se setea automáticamente** — solo es un valor que el filtro del listado podría mostrar. Falta worker.
- **`Forbid()` cuando el tenant del JWT no coincide con el tenant de la cotización**: en `UpdateStatus` y `ConvertToInvoice` (`:124`, `:137`). El `GetById` **no** valida tenant (**FLAG**: información leak por GUID enumeration si se hace un GET directo).
- Doble conversión: si dos requests `convert-to-invoice` llegan a la vez, ambos pasan el check `Status != "invoiced"` y emitirán dos facturas distintas a SUNAT. No hay lock. **FLAG**.
- `MapToResponse` no incluye `TermsAndConditions` ni `ValidUntil` no — sí lo incluye (`q.ValidUntil`, línea 182). OK.
- Currency: la factura hereda `Currency` de la cotización (ctrl :152). Si era USD, la cotización conservaba precios sin convertir; al emitir la factura el `DocumentService` sí maneja TC.

### Navegación adyacente
- Desde `/quotations` por click en fila.
- Hacia `/quotations` con flecha back.
- Hacia `/documents/[invoiceDocumentId]` cuando ya fue convertida (link a implementar en banner verde).

---

## /perceptions — Percepciones (SUNAT tipo 40)

### Propósito
Listar comprobantes de percepción (CPE tipo 40) emitidos por el tenant en su rol de Agente de Percepción. Muestra serie-correlativo, cliente, régimen aplicado y total percibido.

### Endpoints REST consumidos
- `GET /v1/perceptions?page&pageSize&status` — listado paginado

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/PerceptionsController.cs:168` (`List`)
- Service: no existe servicio dedicado — lógica en controller. XML builder: `src/TukiFact.Infrastructure/Services/PerceptionXmlBuilder.cs`
- Entity: `src/TukiFact.Domain/Entities/PerceptionDocument.cs:3`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/PerceptionRepository.cs:19` (`ListAsync`)
- Validator: no tiene validador — **FLAG**

### Campos del backend — Request Create
No aplica. Ver `/perceptions/new`.

### Campos del backend — Request Update
No aplica.

### Campos del backend — Response
Cada item es `PerceptionResponse` (`src/TukiFact.Application/DTOs/Perceptions/PerceptionDTOs.cs:30`):

| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| id | Guid | no | PK |
| serie | string | no | Serie del CPE percepción (P001) |
| correlative | long | no | Correlativo dentro de la serie |
| fullNumber | string | no | `${Serie}-${Correlative:D8}` (computado) |
| issueDate | DateOnly | no | Fecha de emisión |
| customerDocType | string | no | Catálogo 06 (default 6=RUC) |
| customerDocNumber | string | no | RUC del comprador |
| customerName | string | no | Razón social del comprador |
| regimeCode | string | no | Catálogo SUNAT 22 (01/02/03) |
| perceptionPercent | decimal(5,2) | no | Tasa aplicada (2.00 / 1.00 / 0.50) |
| totalInvoiceAmount | decimal(14,2) | no | Total cobrado antes de percepción |
| totalPerceived | decimal(14,2) | no | Total percibido |
| totalCollected | decimal(14,2) | no | Total cobrado al comprador (invoice + percepción) |
| currency | string | no | PEN (la percepción siempre se liquida en PEN) |
| status | string | no | draft / signed / sent / accepted / rejected |
| sunatResponseCode | string | sí | Código de respuesta CDR SUNAT |
| sunatResponseDescription | string | sí | Descripción CDR |
| xmlUrl | string | sí | URL MinIO al XML firmado |
| pdfUrl | string | sí | URL MinIO al PDF |
| createdAt | DateTimeOffset | no | |
| references | List\<PerceptionReferenceResponse\> | no | Documentos referenciados (no se traen en list, sí en detalle) |

Envuelto en `{ items, totalCount, page, pageSize }`.

### Enums / Catálogos SUNAT relevantes
- **Catálogo 22 — Régimen de percepción** (`RegimeCode`):
  - `01` Venta interna — 2%
  - `02` Combustible — 1%
  - `03` Importación / Comprobante de Pago — 0.5%
- **Estados** (string libre, no enum): `draft`, `signed`, `sent`, `accepted`, `rejected`.
- **Tipo de documento SUNAT**: `40` Percepción (hardcoded en entidad).

### Servicios externos invocados (durante create/update)
No aplica en listado.

### Validación oficial del backend
Sin FluentValidation. Reglas EF (`PerceptionDocumentConfiguration.cs`):
- Unique `(TenantId, Serie, Correlative)` (cfg :56)
- `PerceptionPercent` precision (5,2)
- `Serie` max 4

Reglas **no enforced**:
- `pageSize` sin cap (**FLAG**)
- `status` filter acepta cualquier string

### UI propuesto siguiendo DESIGN-CLIENT
- Page header `t-display-lg` con conteo y CTA `Nueva Percepción`.
- Toolbar: chip-group de estados (`Todos · Borrador · Firmado · Enviado · Aceptado · Rechazado`) + búsqueda por `fullNumber` / `customerDocNumber` / `customerName`.
- Tabla con columnas: `# Número (mono)` · `Fecha` · `Cliente` · `Régimen (pill 2%/1%/0.5%)` · `Total Percibido (tnum mono)` · `Estado (StatusBadge OKLCH)`.
- Empty state: ícono `ShieldAlert`, copy "Aún no emitiste comprobantes de percepción" + CTA primario, microcopy explicando "Solo aplica si SUNAT te designó Agente de Percepción".
- Smart helper: filtro rápido por régimen (chip `Solo 2% Venta interna`).
- Auto-bridge: si el tenant no tiene certificado activo, banner amarillo "Configura tu certificado en /certificate antes de emitir percepciones".

### Estados / flujo
Solo lectura. Listado refleja el último estado guardado por el flujo POST (ver `/perceptions/new`). No hay polling — el CDR de SUNAT para tipo 40 es **síncrono** en el endpoint `otroscpe` (mismo flujo que retención), por lo que el row aparece directamente como `accepted`/`rejected` tras la emisión.

### Edge cases / gotchas del backend
- Tenant scoping: filtra por `p.TenantId == tenantId` (repo :24).
- Correlativo único por `(tenant, serie)` → no choca entre series P001/P002.
- Currency: aunque el campo existe, SUNAT exige que la percepción se liquide en PEN; el backend no enforce eso (**FLAG**).
- Status puede quedar `signed` si el envío a SUNAT falla pero el firmado fue OK (controller :154 lo deja en `Sent` cuando hay excepción de red → **bug**: el status `Sent` no cuadra con el flujo descrito, debería ser `Signed` para reintento posterior).

### Navegación adyacente
- Desde sidebar → "Percepciones".
- A `/perceptions/new`.
- **`/perceptions/[id]` no existe** en el frontend — **FLAG**: el listado no es clickeable y no hay forma de ver el XML/CDR/PDF ni la lista de referencias desde la UI.

---

## /perceptions/new — Nueva Percepción

### Propósito
Formulario para emitir un CPE tipo 40 contra SUNAT: identificar al comprador, indicar régimen + tasa, y referenciar uno o más documentos cobrados (facturas/boletas). Calcula percepción por línea y total.

### Endpoints REST consumidos
- `POST /v1/perceptions` — crear, firmar, enviar a SUNAT en una sola llamada (síncrono)

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/PerceptionsController.cs:50` (`Create`)
- Service: `IPerceptionXmlBuilder.BuildPerceptionXml` (`src/TukiFact.Infrastructure/Services/PerceptionXmlBuilder.cs`), `IXmlSigningService.SignXml`, `ISunatClient.SendDocumentAsync`, `IStorageService.UploadXmlAsync` + `UploadCdrAsync`
- Entity: `src/TukiFact.Domain/Entities/PerceptionDocument.cs:3` + `PerceptionDocumentReference` en `:57`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/PerceptionRepository.cs:52` (`AddAsync`), `:44` (`GetNextCorrelativeAsync`)
- Validator: no tiene validador — **FLAG**

### Campos del backend — Request Create
`CreatePerceptionRequest` (`src/TukiFact.Application/DTOs/Perceptions/PerceptionDTOs.cs:3`):

| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| serie | string | sí | max 4, single char P (por convención, no enforced) | Serie del CPE (P001…) | Input mono con autocompletado desde `/v1/series` |
| issueDate | DateOnly? | no | default `UtcNow` (ctrl :64) | Fecha de emisión | Input fecha (default hoy) |
| customerDocType | string | sí | max 1, default "6" | Catálogo 06 | Select / PillGroup (típicamente RUC) |
| customerDocNumber | string | sí | max 15 | RUC del comprador | Input mono con botón "Buscar" |
| customerName | string | sí | max 200 | Razón social | Input texto |
| customerAddress | string? | no | max 300 | Domicilio | Input texto |
| regimeCode | string | sí | max 2 | Catálogo 22 | PillGroup `[01 Venta 2% · 02 Combustible 1% · 03 CdP 0.5%]` |
| perceptionPercent | decimal | sí | precision (5,2). Sin enforce de coincidir con `regimeCode` (**FLAG**) | Tasa | Input número (auto-derivado del régimen) |
| currency | string | sí | max 3, default PEN. SUNAT exige PEN (**FLAG**: backend acepta USD) | Moneda | Select fijo en PEN |
| notes | string? | no | max 500 | Observaciones | Textarea |
| references | List\<CreatePerceptionReferenceRequest\> | sí | `Count >= 1` no enforced (**FLAG**) | Documentos cobrados | Array editable |

`CreatePerceptionReferenceRequest` (`:17`):

| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| documentType | string | sí | max 2 | Catálogo 01 (01=Factura, 03=Boleta) | Select |
| documentNumber | string | sí | max 20, formato `XNNN-NNNNNNNN` no validado (**FLAG**) | Número completo de la factura/boleta | Input mono |
| documentDate | DateOnly | sí | — | Fecha del documento referenciado | Input fecha |
| invoiceAmount | decimal | sí | precision (14,2). Sin `>= 0` | Monto total de la factura | Input número |
| invoiceCurrency | string | sí | max 3 | Moneda de la factura referenciada | Select |
| collectionDate | DateOnly | sí | — | Fecha del cobro | Input fecha |
| collectionNumber | int | sí, default 1 | — | Número del cobro (si hay cobros parciales) | Input número |
| collectionAmount | decimal | sí | precision (14,2) | Monto del cobro (base de percepción) | Input número |
| exchangeRate | decimal? | no | precision (10,4) | TC del día del cobro si moneda ≠ PEN | Input número con auto-fill desde `/v1/utils/exchange-rate` |
| exchangeRateDate | DateOnly? | no | — | Fecha del TC | Input fecha |

### Campos del backend — Request Update
Update no implementado. Una vez emitida la percepción, no se puede modificar — solo anular vía Comunicación de Baja (**no implementada para percepciones**, ver `/voided` que solo cubre documents tipo 01/03/07/08).

### Campos del backend — Response
`PerceptionResponse` (ver `/perceptions`). Las `references` se incluyen completas tras crear.

### Enums / Catálogos SUNAT relevantes
- **Catálogo 22** (régimen percepción): 01 · 02 · 03.
- **Catálogo 01** (tipo doc referenciado): 01 Factura · 03 Boleta.
- **Catálogo 06** (tipo doc identidad): típicamente 6=RUC.
- **Moneda ISO 4217**: PEN (debería ser obligatoria).

### Servicios externos invocados (durante create/update)
- SUNAT firmado XML: **SÍ** — en `PerceptionsController.cs:116-128` vía `IXmlSigningService.SignXml` (UBL 2.0).
- SUNAT envío (SOAP/REST): **SÍ** — `ISunatClient.SendDocumentAsync(ruc, "40", fullNumber, zipBytes, ct)` en ctrl :140. Endpoint `otroscpe`.
- SUNAT polling de ticket: **NO** — `otroscpe` responde con CDR síncrono.
- Lookup RUC/DNI: **NO** desde este endpoint; el frontend podría llamarlo previo al submit (hoy no lo hace).
- Tipo de cambio (SBS): **NO** automático — `exchangeRate` se envía manualmente por línea. Falta auto-fill (**FLAG**).
- Email cliente: **NO**.
- MinIO/storage: **SÍ** — `UploadXmlAsync` (ctrl :133) y `UploadCdrAsync` si vino CDR (ctrl :148).
- NATS / event publisher: **NO** (no se publica evento).

### Validación oficial del backend
Sin FluentValidation. Reglas EF:
- `Serie` max 4, required
- `CustomerDocNumber` max 15, required
- `CustomerName` max 200, required
- `RegimeCode` max 2, required
- `PerceptionPercent` precision (5,2)
- `(TenantId, Serie, Correlative)` unique

Reglas **no enforced** críticas:
- `references.Count >= 1` — emitir percepción sin referencias rompe XML (**FLAG**)
- `regimeCode ∈ {01, 02, 03}` (**FLAG**)
- `perceptionPercent` coincide con régimen (2 / 1 / 0.5)
- `currency = "PEN"` (SUNAT lo exige)
- `collectionAmount > 0` por línea
- `documentNumber` con formato SUNAT (regex `^[A-Z]\d{3}-\d{1,8}$`)
- Existencia del certificado: si `tenant.CertificateData is null`, el documento queda sin firmar y SUNAT lo rechaza (ctrl :116 lo silencia con `LogWarning`, no devuelve 400) — **FLAG**

### UI propuesto siguiendo DESIGN-CLIENT
- Page header `t-display-lg` con descripción "Comprobante de percepción a comprador designado".
- Form 2/3 + sticky 1/3:
  - **Izquierda**: secciones `Cliente · Régimen · Documentos relacionados · Notas`.
  - **Derecha sticky**: resumen con `Total cobrado`, `Total percibido` (destacado `t-num-lg`), `Total cobrado al cliente`, CTA `Emitir percepción`.
- PillGroup para régimen (3 opciones) con auto-set del `perceptionPercent` (ya funciona en el state pero como Select).
- SUNAT lookup en sección Cliente (con badge `apis.net.pe` cuando configurado).
- Auto-bridge en cada referencia: si el `documentNumber` existe en `documents` del tenant, prellenar `documentDate` + `invoiceAmount` + `invoiceCurrency` con un chip "Documento encontrado".
- Auto-bridge para TC: si la factura referenciada está en USD, llamar `/v1/utils/exchange-rate?date={collectionDate}&currency=USD` y prellenar `exchangeRate`.
- Empty state cuando `references.length === 0`: card grande "Agregá al menos un documento cobrado" + ejemplo visual.
- Validación inline antes de submit (front-side).

### Estados / flujo
1. Cálculo client-side del `perceivedAmount = collectionAmount * (percent/100)` por línea (también lo recalcula el server, ctrl :80).
2. POST → backend:
   - Genera correlativo `MAX + 1` por `(tenant, serie)`.
   - INSERT `perception_documents` + `perception_document_references`.
   - Construye XML UBL 2.0 → firma con cert tenant → guarda en MinIO.
   - Envía a SUNAT `otroscpe` (síncrono) → recibe CDR.
   - UPDATE status a `accepted` / `rejected` con `sunatResponseCode/Description`.
3. Frontend muestra toast y redirige a `/perceptions` (no a detalle — **inconsistencia**, ver `/perceptions/new/page.tsx:97`).

### Edge cases / gotchas del backend
- **Mismo race condition en correlativo** (no advisory lock).
- Si `CertificateData` es null, el doc se guarda en `draft` (sin firma) y aún así se intenta enviar a SUNAT → SUNAT rechaza → status `rejected`. **FLAG**: debería 400 temprano.
- Si la red falla durante `SendDocumentAsync`, el status queda `sent` (ctrl :154) → **discrepancia con `signed`/`rejected`**, e implica que necesitaría un worker de reintento que **no existe**.
- No hay event publisher tras emisión (no se notifica a webhooks).
- El RUC del tenant viaja en plano al SUNAT client; el cert se desencripta con `_secrets.Unprotect(...)` en memoria — OK.
- `MapToResponse` no incluye `CustomerAddress` ni `Notes` (línea 181) → se pierden en la respuesta.

### Navegación adyacente
- Desde `/perceptions` botón "Nueva Percepción".
- Tras éxito → `/perceptions` (no a detalle porque no existe).
- Cancelar → `/perceptions`.

---

## /perceptions/[id] — Detalle Percepción

**No implementado** en frontend (directorio no existe).

El backend sí expone `GET /v1/perceptions/{id}` (`PerceptionsController.cs:161`) que devuelve la percepción con todas sus references. **FLAG**: implementar pantalla análoga a `/quotations/[id]` con:
- Header con `fullNumber` + StatusBadge OKLCH.
- Card cliente · card totales (3 montos: invoice/perceived/collected).
- Tabla de references con `t-overline`.
- Footer SUNAT: badge con `sunatResponseCode`, descripción, botones `Descargar XML` / `Descargar CDR` (usando `xmlUrl` / `cdrUrl`).
- Si `status === "rejected"`: banner rojo con la descripción CDR.

---

## /retentions — Retenciones (SUNAT tipo 20)

### Propósito
Listar comprobantes de retención (CPE tipo 20) emitidos por el tenant en su rol de Agente de Retención. Estructura espejo de `/perceptions` pero del lado del pagador (proveedor en vez de cliente).

### Endpoints REST consumidos
- `GET /v1/retentions?page&pageSize&status` — listado paginado

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/RetentionsController.cs:170` (`List`)
- Service: XML builder en `src/TukiFact.Infrastructure/Services/RetentionXmlBuilder.cs`
- Entity: `src/TukiFact.Domain/Entities/RetentionDocument.cs:3`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/RetentionRepository.cs:19` (`ListAsync`)
- Validator: no tiene validador — **FLAG**

### Campos del backend — Request Create
No aplica. Ver `/retentions/new`.

### Campos del backend — Request Update
No aplica.

### Campos del backend — Response
Cada item es `RetentionResponse` (`src/TukiFact.Application/DTOs/Retentions/RetentionDTOs.cs:30`):

| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| id | Guid | no | PK |
| serie | string | no | R001 |
| correlative | long | no | Correlativo por serie |
| fullNumber | string | no | `${Serie}-${Correlative:D8}` |
| issueDate | DateOnly | no | |
| supplierDocType | string | no | Cat. 06 (default 6=RUC) |
| supplierDocNumber | string | no | RUC del proveedor |
| supplierName | string | no | Razón social del proveedor |
| regimeCode | string | no | Catálogo 23 |
| retentionPercent | decimal(5,2) | no | Tasa (3.00 / 6.00) |
| totalInvoiceAmount | decimal(14,2) | no | Total pagado antes de retención |
| totalRetained | decimal(14,2) | no | Total retenido |
| totalPaid | decimal(14,2) | no | Neto pagado al proveedor |
| currency | string | no | PEN |
| status | string | no | draft / signed / sent / accepted / rejected |
| sunatResponseCode | string | sí | |
| sunatResponseDescription | string | sí | |
| xmlUrl | string | sí | |
| pdfUrl | string | sí | |
| createdAt | DateTimeOffset | no | |
| references | List | no | |

### Enums / Catálogos SUNAT relevantes
- **Catálogo 23 — Régimen de retención**:
  - `01` Tasa 3% (régimen general)
  - `02` Tasa 6% (operaciones específicas)
- **Tipo SUNAT**: `20` Retención.
- **Estados**: draft / signed / sent / accepted / rejected.

### Servicios externos invocados (durante create/update)
No aplica en listado.

### Validación oficial del backend
Sin FluentValidation. Mismo conjunto de reglas EF que perception (ver `RetentionDocumentConfiguration.cs`). Mismo gap: `pageSize` sin cap.

### UI propuesto siguiendo DESIGN-CLIENT
Idéntico al de `/perceptions` pero con tono "proveedor": header "Retenciones", empty state `ShieldCheck` "Aún no emitiste comprobantes de retención", microcopy "Solo aplica si SUNAT te designó Agente de Retención", chips de régimen `[Todos · 3% · 6%]`.

### Estados / flujo
Solo lectura. Misma observación que percepciones: `otroscpe` síncrono → row aparece directo en `accepted`/`rejected`.

### Edge cases / gotchas del backend
Idénticos a `/perceptions` (mismo patrón heredado): race condition en correlativo, status `sent` cuando red falla, sin event publisher, etc. (ver observaciones transversales abajo).

### Navegación adyacente
- Sidebar → "Retenciones".
- A `/retentions/new`.
- **`/retentions/[id]` no existe** — **FLAG**.

---

## /retentions/new — Nueva Retención

### Propósito
Formulario para emitir un CPE tipo 20 contra SUNAT: identificar al proveedor, régimen + tasa, y referenciar facturas pagadas. Calcula retención por línea y total neto pagado.

### Endpoints REST consumidos
- `POST /v1/retentions` — crear, firmar, enviar a SUNAT (síncrono)

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/RetentionsController.cs:50` (`Create`)
- Service: `IRetentionXmlBuilder`, `IXmlSigningService`, `ISunatClient`, `IStorageService`
- Entity: `src/TukiFact.Domain/Entities/RetentionDocument.cs:3` + `RetentionDocumentReference` en `:57`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/RetentionRepository.cs:52` (`AddAsync`)
- Validator: no tiene validador — **FLAG**

### Campos del backend — Request Create
`CreateRetentionRequest` (`src/TukiFact.Application/DTOs/Retentions/RetentionDTOs.cs:3`):

| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| serie | string | sí | max 4 (típicamente R001) | Serie del CPE retención | Input mono con autocompletado |
| issueDate | DateOnly? | no | default `UtcNow` | Fecha emisión | Input fecha |
| supplierDocType | string | sí | max 1, default "6" | Catálogo 06 (típicamente RUC) | Select |
| supplierDocNumber | string | sí | max 15 | RUC proveedor | Input mono + botón "Buscar" |
| supplierName | string | sí | max 200 | Razón social proveedor | Input texto |
| supplierAddress | string? | no | max 300 | | Input texto |
| regimeCode | string | sí | max 2 | Catálogo 23 | PillGroup `[01 Tasa 3% · 02 Tasa 6%]` |
| retentionPercent | decimal | sí | precision (5,2). Sin enforce de coincidir con régimen (**FLAG**) | Tasa | Input número (auto-derivado) |
| currency | string | sí | max 3, default PEN | Moneda | Select fijo PEN |
| notes | string? | no | max 500 | Observaciones | Textarea |
| references | List | sí | sin `Count >= 1` enforced (**FLAG**) | Facturas pagadas | Array editable |

`CreateRetentionReferenceRequest` (`:17`):

| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| documentType | string | sí | max 2 | Cat. 01 (01=Factura, 03=Boleta) | Select |
| documentNumber | string | sí | max 20 | F001-00000100 | Input mono |
| documentDate | DateOnly | sí | — | Fecha factura | Input fecha |
| invoiceAmount | decimal | sí | precision (14,2) | Monto factura | Input número |
| invoiceCurrency | string | sí | max 3 | | Select |
| paymentDate | DateOnly | sí | — | Fecha pago | Input fecha |
| paymentNumber | int | sí, default 1 | — | Nro. del pago (parciales) | Input número |
| paymentAmount | decimal | sí | precision (14,2) | Monto pagado (base retención) | Input número |
| exchangeRate | decimal? | no | precision (10,4) | TC si moneda ≠ PEN | Input número con auto-fill |
| exchangeRateDate | DateOnly? | no | — | | Input fecha |

### Campos del backend — Request Update
Update no implementado.

### Campos del backend — Response
`RetentionResponse` (ver `/retentions`).

### Enums / Catálogos SUNAT relevantes
- **Catálogo 23** (régimen retención): 01 (3%), 02 (6%).
- **Catálogo 01** (tipo doc): 01 Factura, 03 Boleta.
- **Catálogo 06** (tipo doc identidad): 6=RUC predominante.

### Servicios externos invocados (durante create/update)
- SUNAT firmado XML: **SÍ** — `RetentionsController.cs:117-131`.
- SUNAT envío (SOAP/REST): **SÍ** — `ISunatClient.SendDocumentAsync(ruc, "20", fullNumber, zip, ct)` en `:142`. Endpoint `otroscpe`.
- SUNAT polling de ticket: **NO** (síncrono).
- Lookup RUC/DNI: **NO** desde el endpoint backend (sí podría desde el front, hoy no se hace).
- Tipo de cambio (SBS): **NO** automático (campo manual).
- Email proveedor: **NO**.
- MinIO/storage: **SÍ** — `UploadXmlAsync` (`:135`), `UploadCdrAsync` (`:150`).
- NATS / event publisher: **NO**.

### Validación oficial del backend
Misma situación que percepción. Sin FluentValidation. Reglas EF en `RetentionDocumentConfiguration.cs`. Faltan:
- `references.Count >= 1`
- `regimeCode ∈ {01, 02}`
- `retentionPercent` coincide con régimen (3 o 6)
- Existencia certificado
- Formato `documentNumber`

### UI propuesto siguiendo DESIGN-CLIENT
Análoga a `/perceptions/new` pero con tono "proveedor". Cambios:
- PillGroup régimen con 2 opciones (más ancho cada una).
- Resumen sticky muestra `Total pagado factura · Retenido · Neto al proveedor`.
- Auto-bridge: si el `documentNumber` existe en `documents` recibidos (compras) del tenant — futuro endpoint —, prellenar.
- Auto-bridge TC para refs en USD.

### Estados / flujo
Idéntico a percepción:
1. Correlativo `MAX+1` por `(tenant, serie)`.
2. INSERT + XML UBL → firma → MinIO.
3. SUNAT síncrono → CDR → status final.
4. Redirige a `/retentions` (mismo bug — no a detalle inexistente).

### Edge cases / gotchas del backend
Idénticos a percepción.

### Navegación adyacente
- Desde `/retentions` botón "Nueva Retención".
- Tras éxito → `/retentions`.
- Cancelar → `/retentions`.

---

## /retentions/[id] — Detalle Retención

**No implementado**. Mismo gap que `/perceptions/[id]`. Backend expone `GET /v1/retentions/{id}` (`RetentionsController.cs:163`). **FLAG**: implementar.

---

## /voided — Comunicaciones de Baja

### Propósito
Histórico de comunicaciones de baja (tickets RA) enviadas a SUNAT para anular facturas/boletas previamente aceptadas. Auto-refresca cada 30s mientras haya tickets `pending`/`sent`.

### Endpoints REST consumidos
- `GET /v1/voided-documents` — listado completo del tenant (sin paginación)

⚠️ **El frontend NO consume `POST /v1/voided-documents`** desde esta pantalla. La anulación se inicia desde el detalle del documento (no auditado en este grupo). La pantalla es solo histórico.

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/VoidedDocumentsController.cs:94` (`List`), `:37` (`VoidDocument` POST)
- Service: no hay servicio dedicado. El POST hace inline: actualiza el `Document.Status = Voided`, genera ticket `RA-YYYYMMDD-NNN`, INSERT VoidedDocument con `Status = "pending"`.
- Entity: `src/TukiFact.Domain/Entities/VoidedDocument.cs:3`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/VoidedDocumentRepository.cs:16` (`GetByTenantAsync`), `:22` (`CreateAsync`)
- Validator: no tiene validador — **FLAG**
- Tenant provider: `Domain.Interfaces.ITenantProvider` (vs `User.FindFirstValue("tenant_id")` en otros controllers — **inconsistencia**)

### Campos del backend — Request Create
`VoidDocumentRequest` (`src/TukiFact.Application/DTOs/Documents/VoidDocumentRequest.cs:3`) — no se invoca desde esta pantalla pero relevante para contexto:

| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| documentId | Guid | sí | document debe estar en `Accepted` (ctrl :46) | ID del documento a anular | (interno) |
| voidReason | string | sí | sin max length enforced (**FLAG**) | Motivo SUNAT (cat. 21) | Textarea con select de motivos predefinidos |

### Campos del backend — Request Update
Update no implementado vía API pública (el worker SUNAT actualiza directamente la entity al recibir el CDR del ticket — flujo asíncrono).

### Campos del backend — Response
`VoidedDocumentResponse` (`src/TukiFact.Application/DTOs/Documents/VoidDocumentRequest.cs:8`):

| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| id | Guid | no | PK |
| ticketNumber | string | no | Ticket interno `RA-20260530-001` |
| status | string | no | pending / processing / accepted / rejected / sent / error |
| sunatTicket | string | sí | Ticket asignado por SUNAT al recibir el RA |
| sunatResponseCode | string | sí | Código respuesta CDR |
| sunatResponseDescription | string | sí | Descripción CDR |
| createdAt | DateTimeOffset | no | |

⚠️ El controller `[Authorize(Roles = "admin")]` — **solo admins pueden listar/crear** (`VoidedDocumentsController.cs:14`). El frontend no valida esto antes de pedir la página.

### Enums / Catálogos SUNAT relevantes
- **Tipos de ticket**:
  - `RA` Comunicación de Baja (anular factura/NC/ND ya aceptada)
  - `RC` Resumen Diario (consolidado de boletas/NCBoleta)
- **Estados**: pending → processing → accepted / rejected. (También `sent` y `error` aparecen en el `STATUS` del frontend pero no se setean explícitamente en el controller actual — probablemente vienen de un worker.)
- **Catálogo SUNAT 21 — Motivos de anulación** (no codificado, texto libre en `voidReason`).

### Servicios externos invocados (durante create/update)
**Endpoint `List`**: ninguno.

**Endpoint `VoidDocument` (POST)** (no llamado desde esta pantalla):
- SUNAT firmado XML: **diferido** — el controller solo crea el VoidedDocument en `pending`; el firmado + envío del XML RA lo hace un worker (no auditado aquí).
- SUNAT envío: **NO inmediato** (es asíncrono, vía ticket).
- SUNAT polling: **SÍ por worker** — el flujo SUNAT para RA usa endpoint `sendSummary` que devuelve un ticket, y luego `getStatus(ticket)` para CDR. El frontend hace polling propio (30s) para reflejar el cambio.
- MinIO/storage: el worker subirá XML/CDR cuando los reciba.

### Validación oficial del backend
Sin FluentValidation. Reglas inline en `VoidDocumentsController.VoidDocument`:
- `document is null` → 404
- `document.Status != Accepted` → 400 (ctrl :46)
- `[Authorize(Roles = "admin")]`

Reglas **no enforced**:
- `voidReason` debe tener una longitud mínima (SUNAT exige ≥1 char real)
- Plazo legal: SUNAT permite anular hasta el día 7 del mes siguiente. **No se valida** — **FLAG** crítico de cumplimiento.
- No se chequea que el mismo documento no se anule dos veces (chequeo de duplicado por `documentId`).
- Listado sin paginación: si el tenant tiene 50k anulaciones, el endpoint devuelve TODO — **FLAG**.

### UI propuesto siguiendo DESIGN-CLIENT
Esta pantalla **ya sigue DESIGN-CLIENT bastante bien**:
- ✅ Page header con `t-display-lg`
- ✅ Toolbar con chips de estado + búsqueda
- ✅ Tabla con `t-overline`, hairlines, `mono`/`tnum`
- ✅ StatusBadge con tokens OKLCH (`--warning`, `--success`, `--danger`, `--info`)
- ✅ Empty state inicial vs filtrado diferenciados
- ✅ Smart helper: auto-refresh cada 30s cuando hay pendientes

Mejoras restantes:
- Paginación server-side (cuando el backend la soporte).
- Botón "Reintentar" en filas `error` (requiere endpoint backend).
- Click en fila → modal/drawer con el `ItemsJson` parseado (qué documentos anula este ticket).
- Filtro por rango de fechas.
- Banner si el usuario no es admin (no debería ver esta pantalla).

### Estados / flujo
- POST inicial → `pending` (controller no envía a SUNAT en el mismo request; solo registra).
- Worker recoge `pending`, firma XML RA, llama SUNAT `sendSummary` → asigna `sunatTicket` → `Status = "processing"` o `"sent"`.
- Worker hace polling `getStatus(sunatTicket)` → al recibir CDR setea `sunatResponseCode/Description` y `Status = "accepted" | "rejected"`.
- El frontend hace su propio polling cada 30s para refrescar.

### Edge cases / gotchas del backend
- **Inconsistencia de tenant provider**: este controller usa `ITenantProvider.GetCurrentTenantId()` mientras `Quotations/Perceptions/Retentions` usan `User.FindFirstValue("tenant_id")` directo. Ambos resuelven a lo mismo pero el patrón no está unificado — **FLAG**.
- **Sin paginación**: `GetByTenantAsync` trae TODO (`VoidedDocumentRepository.cs:16-20`). Para tenants con alta facturación esto es ineficiente.
- **Auto-refresh client-side cada 30s**: bien para baja escala, pero no usa SSE/WebSocket. Cualquier filtro/búsqueda no se preserva en URL.
- **Ticket interno vs SUNAT ticket**: `ticketNumber` = interno (RA-YYYYMMDD-NNN), `sunatTicket` = el de SUNAT. La UI los muestra como columnas separadas — claro.
- **Idempotencia**: si el frontend invoca POST dos veces al mismo documento, ambos pasan el `document.Status == Accepted` la primera vez; la segunda falla porque ya está `Voided`. OK por accidente.
- **Authorization rol "admin"**: si el usuario no tiene rol admin recibe 403 (Forbid). El frontend no maneja ese caso visualmente.

### Navegación adyacente
- Desde sidebar → "Comunicaciones de baja".
- **No hay link desde aquí al documento original** (solo se ve `ticketNumber` interno). El `ItemsJson` tiene `documentType + serie + correlative + fullNumber + reason` pero no el `documentId` — **FLAG**: imposible navegar al detalle del documento anulado desde esta pantalla.

---

## /exchange-rates — Tipo de Cambio

### Propósito
Consultar tipo de cambio oficial (SBS vía apis.net.pe) para una fecha y moneda específicas. Incluye convertidor amount→amount y modo "últimos 7 días" (7 fetches en serie). Cachéa cada respuesta en `exchange_rates`.

### Endpoints REST consumidos
- `GET /v1/utils/exchange-rate?date={YYYY-MM-DD}&currency={USD|EUR}` — leer TC. Cachea diariamente.

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/UtilsController.cs:71` (`GetExchangeRate`)
- Service: `IExchangeRateService` — impl en `src/TukiFact.Infrastructure/Services/ExchangeRateService.cs:14`
- Entity: `src/TukiFact.Domain/Entities/ExchangeRate.cs:3`
- Repository: directo via `AppDbContext.ExchangeRates` (no hay repo dedicado — **FLAG menor**: rompe el patrón del resto de la solución)
- Validator: no aplica (GET sin body)

### Campos del backend — Request Create
No aplica. El "create" implícito ocurre cuando el service cachea un nuevo rate (`FetchAndSaveRateAsync`, service :44), no expuesto al cliente.

### Campos del backend — Request Update
No aplica. El service hace upsert internamente.

### Campos del backend — Response
El controller no devuelve la entity directa sino un objeto anónimo (ctrl :85):

| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| date | DateOnly | no | Fecha del TC |
| currency | string | no | USD / EUR |
| buyRate | decimal(10,4) | no | TC compra |
| sellRate | decimal(10,4) | no | TC venta (el oficial SUNAT para facturación) |
| source | string | no | "SBS" |
| fetchedAt | DateTimeOffset | no | Cuándo se cacheó |

Si no hay TC para esa fecha (fin de semana, feriado) → `404 NotFound { error: "Tipo de cambio no disponible para …" }`.

Si apis.net.pe falla → `503 { error: "No se pudo obtener el tipo de cambio" }`.

### Enums / Catálogos SUNAT relevantes
- **Monedas ISO 4217**: USD, EUR (las únicas para las que apis.net.pe SBS tiene endpoint).
- **Fuente** (`Source`): fijo "SBS" (Superintendencia de Banca y Seguros del Perú; SUNAT acepta esta fuente para conversiones).

### Servicios externos invocados (durante create/update)
- SUNAT firmado XML: **NO**
- SUNAT envío: **NO**
- SUNAT polling: **NO**
- Lookup RUC/DNI: **NO**
- Tipo de cambio (SBS): **SÍ** — `https://api.apis.net.pe/v2/sunat/tipo-cambio?fecha=YYYY-MM-DD` (service :19). HttpClient nombrado `"ApisNetPe"`.
- Email cliente: **NO**
- MinIO/storage: **NO**
- NATS / event publisher: **NO**

### Validación oficial del backend
- `date` se parsea con `DateOnly.Parse(date)` — si no es ISO `YYYY-MM-DD` revienta con `FormatException` (no atrapada → 500). **FLAG**.
- `currency` default "USD" sin enforce de catálogo (acepta cualquier 3 chars).

Reglas **no enforced**:
- `date` no puede ser futuro (apis.net.pe rechaza, pero el backend reenvía el error como 503).
- `currency ∈ {USD, EUR}` (cualquier otra genera 503).

### UI propuesto siguiendo DESIGN-CLIENT
Esta pantalla **no sigue aún DESIGN-CLIENT** (usa `Card/CardHeader/CardTitle` shadcn + `text-2xl font-bold` en vez de `t-display-lg`). Propuesto:
- Page header `t-display-lg` "Tipo de cambio" + microcopy "Cotización oficial SBS para facturación electrónica".
- Toolbar pegada: PillGroup moneda `[USD · EUR]` + Input fecha con quick-chips `[Hoy · Ayer · -7d]` + CTA `Consultar` + CTA secundario `Últimos 7 días`.
- Card grande split 50/50 cuando hay rate:
  - Izquierda: TC del día (Compra/Venta) con `t-num-lg mono tnum`, badge `--success` para compra y `--info` para venta, badge "SBS" pequeña con timestamp `fetchedAt`.
  - Derecha: convertidor con PillGroup `[Venta · Compra]` y resultados grandes mono.
- Tabla "Historial" (cliente-side, solo últimas 30 consultas en memoria — **FLAG menor**: no es histórico real persistido entre sesiones; al refrescar se pierde) con `t-overline`, hairlines, montos `tnum mono` y colores por dirección.
- Empty state cuando no hay rate (fin de semana): card con copy "No hay cotización SBS para sábados, domingos ni feriados" + sugerencia "Probá con el viernes anterior" + chip `Buscar viernes`.
- Smart helper: si la fecha es futura, deshabilitar `Consultar` y mostrar tooltip.

### Estados / flujo
1. Usuario elige fecha + moneda → click "Consultar".
2. Backend revisa cache (`exchange_rates` table) por `(date, currency)`.
3. Si está cacheado → devuelve.
4. Si no → llama apis.net.pe → upsert en cache → devuelve.
5. Frontend acumula en `history[]` (max 30) en memoria.

El convertidor recalcula en cliente cada vez que cambia `amount` o `direction`. Ningún cálculo server-side.

### Edge cases / gotchas del backend
- **Sin tenant scoping**: la tabla `exchange_rates` es global, compartida entre todos los tenants (no tiene `TenantId`). Tiene sentido: el TC SBS es público y universal, una sola query por día/moneda sirve para todos. ✅
- **Cache stale en mismo día**: si SBS publica un TC distinto más tarde (rare), el cache no se invalida. `FetchAndSaveRateAsync` sí hace upsert pero solo se llama si no hay cache.
- **Sin retry**: si apis.net.pe está caído, error 503 directo. No hay fallback a TC del día anterior.
- **`decimal.Parse(string)` sin culture**: en `ExchangeRateService.cs:59` parsea `"3.5270"` — depende de la cultura del proceso. Si el container corre en cultura europea (`,` decimal), revienta. **FLAG**.
- **No publica evento**: no hay NATS event "exchange_rate.updated" → otros workers no pueden reaccionar.
- **`history` solo client-side**: refresh de la página vacía el historial.
- **`fetchWeek` hace 7 calls en serie** desde el navegador (no en backend) → 7 round-trips. Podría optimizarse con un endpoint `/v1/utils/exchange-rate/range?from=&to=`.

### Navegación adyacente
- Desde sidebar → "Tipo de Cambio".
- **No hay deep-links a esta pantalla** desde formularios de retención/percepción/documents al elegir USD — sería un buen smart-link (botón "Ver TC del día" en cada form que use moneda extranjera).

---

## Group B — Observaciones transversales

### 1. Patrón compartido Percepción vs Retención (estructuralmente idénticos)
Las dos pantallas/entidades son **espejo casi exacto**:

| Aspecto | Perception | Retention |
|---|---|---|
| Tipo SUNAT | 40 | 20 |
| Catálogo régimen | 22 | 23 |
| Serie convención | P001 | R001 |
| Contraparte | Cliente (customer) | Proveedor (supplier) |
| Trigger | Cobro de venta | Pago a proveedor |
| Tasas válidas | 2 / 1 / 0.5 | 3 / 6 |
| Suma intermedia | `perceivedAmount = collection * %` | `retainedAmount = payment * %` |
| Total mostrado en list | `totalPerceived` | `totalRetained` |
| Default % | 2.00 | 3.00 |
| SUNAT endpoint | `otroscpe` (síncrono) | `otroscpe` (síncrono) |

Esta simetría justifica una **componentización común** del frontend: un `<FinancialDocForm kind="perception|retention">` con renombrado de labels (`Cliente`/`Proveedor`, `Cobro`/`Pago`, `Percepción`/`Retención`). Hoy son dos archivos prácticamente duplicados (`perceptions/new/page.tsx` y `retentions/new/page.tsx`, ambos 263 líneas con 90% código idéntico).

Backend ya tiene **dos controllers, dos services de XML, dos repos** muy paralelos — **FLAG**: oportunidad de refactor a una jerarquía `AgentDocumentController<TEntity>` o al menos compartir helpers `CreateZip`, `GetTenantId`, manejo CDR.

### 2. Ausencia total de FluentValidation
Ninguna de las pantallas auditadas tiene validador en `src/TukiFact.Application/Validation/`. Solo `RecurringInvoiceValidator.cs` y `DespatchAdviceValidator.cs` existen. Esto significa:
- Reglas duplicadas entre front y back (o sólo en front).
- Mensajes de error genéricos (`InvalidOperationException`, EF constraints) en lugar de mensajes en es-PE.
- Imposible diferenciar 400 (data inválida del usuario) de 500 (bug interno).

**FLAG**: crear `CreateQuotationValidator`, `CreatePerceptionValidator`, `CreateRetentionValidator`, `VoidDocumentValidator`.

### 3. Race condition en `GetNextCorrelativeAsync`
Patrón repetido en los 3 repos:

```csharp
var max = await ctx.Xxx.Where(...).MaxAsync(x => (long?)x.Correlative);
return (max ?? 0) + 1;
```

Sin advisory lock, sin sequence DB, sin `SERIALIZABLE`. Concurrencia → conflict en unique index → 500 al usuario.

**FLAG crítico**: migrar a sequences PostgreSQL por `(tenant, serie)` o usar `pg_advisory_xact_lock(hashtext(tenant || serie))` antes del MAX.

### 4. Status `sent` como estado de error encubierto
Tanto en perception como retention, cuando SUNAT no responde:

```csharp
catch (Exception ex)
{
    perception.Status = DocumentStatus.Sent;  // ← engañoso
}
```

Esto pinta como "Enviado" algo que **falló al enviarse**. El usuario cree que está OK pero no hay CDR.

**FLAG**: crear estado nuevo `signed_pending_send` o `send_error`, y un worker de reintento. Hoy esos documentos quedan huérfanos.

### 5. Falta `/[id]` para percepción y retención
- `/quotations/[id]` existe (rica, con acciones).
- `/perceptions/[id]` no existe.
- `/retentions/[id]` no existe.

Aún así el backend expone los GET. La consecuencia: el usuario no puede descargar XML/CDR/PDF, no puede ver las references después de emitir, no puede reintentar SUNAT si quedó en `rejected`. **FLAG**: implementar ambas con el mismo patrón que `/quotations/[id]`.

### 6. Inconsistencia en obtención de tenant
- Quotations/Perceptions/Retentions: `Guid.Parse(User.FindFirstValue("tenant_id")!)` (con `!` que tira NRE si no hay claim)
- VoidedDocuments: `_tenantProvider.GetCurrentTenantId()`

Unificar bajo `ITenantProvider` (testable, mockeable, sin null-bang). **FLAG**.

### 7. Ausencia de event publishing
Ninguno de los endpoints de creación publica evento NATS:
- No se notifica a webhooks tras emisión de percepción/retención.
- No se actualiza dashboard en tiempo real.
- No se dispara el envío de email al cliente.

**FLAG**: añadir `IEventPublisher.Publish("perception.emitted", ...)` etc.

### 8. Sin paginación en `/voided-documents`
A diferencia de quotations/perceptions/retentions (todos paginan), voided trae el listado completo. Inconsistencia + riesgo de payload grande. **FLAG**.

### 9. CSS legacy vs DESIGN-CLIENT
Pantallas migradas a tokens DESIGN-CLIENT (`t-display-lg`, `var(--accent)`, `t-overline`, PillGroup):
- ✅ `/quotations/new`
- ✅ `/voided`

Pantallas aún con shadcn defaults (`text-2xl font-bold`, `<Card>`, `<Badge variant>`):
- ❌ `/quotations` (lista)
- ❌ `/quotations/[id]`
- ❌ `/perceptions` (lista)
- ❌ `/perceptions/new`
- ❌ `/retentions` (lista)
- ❌ `/retentions/new`
- ❌ `/exchange-rates`

**FLAG**: 7 pantallas pendientes de migración a DESIGN-CLIENT.

### 10. Hardcodes de tasa
- IGV 18% literal en `QuotationsController.cs:18` (`IgvRate = 0.18m`).
- Percepción defaults en entity (`2.00`), regimen→tasa map duplicado en frontend (`regimePercent`).
- Retención defaults en entity (`3.00`), idem.

**FLAG**: extraer a `appsettings.json` o tabla `tax_rates` para que cambios fiscales no requieran deploy.

### 11. Catálogos SUNAT no centralizados
Los códigos de catálogo (01, 06, 22, 23) están como string libre en DTOs, sin enum ni clase compartida. Hardcodeados en frontend (`CUSTOMER_DOC_TYPES`, `regimePercent`).

**FLAG**: centralizar en `TukiFact.Domain/SunatCatalogs/` con clases estáticas tipadas (similar al `DocumentStatus` que ya existe).

### 12. Currency check ausente
`Perception.Currency` y `Retention.Currency` permiten USD pero SUNAT exige PEN. El backend no enforce. **FLAG**: validar `currency == "PEN"` para tipos 20/40.
# Group C — Catálogo, Series, Reports, Dashboard, Welcome

Auditoría de pantallas en es-PE. Rutas relativas al repo root `/Users/soulkin/Documents/TukiFact/`.

---

## /dashboard — Resumen y KPIs

### Propósito
Pantalla de entrada del usuario autenticado. Muestra un saludo personalizado y resumen del mes (ventas, comprobantes emitidos, pendientes SUNAT, rechazos), un gráfico de barras de ventas mensuales del año, una dona con la distribución por estado SUNAT, los últimos 5 comprobantes y el desglose por tipo de comprobante.

### Endpoints REST consumidos
- `GET /v1/dashboard` — KPIs agregados del tenant (hoy, mes, año, byType, byStatus, monthlySales).
- `GET /v1/documents?pageSize=5&page=1` — últimos 5 comprobantes para el listado lateral.

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/DashboardController.cs:11`
- Service: `src/TukiFact.Infrastructure/Services/DashboardService.cs:9`
- Service interface: `src/TukiFact.Application/Interfaces/IDashboardService.cs:1`
- DTOs: `src/TukiFact.Application/DTOs/Dashboard/DashboardDto.cs:3`
- Entity origen de los datos: `src/TukiFact.Domain/Entities/Document.cs` (agregaciones sobre `Documents`).
- Repository: no hay repo dedicado — el service consulta `AppDbContext.Documents` directamente.
- Validator: no aplica (solo lectura).

### Campos del backend — Request Create
N/A — endpoint es solo `GET`, sin body.

### Campos del backend — Request Update
N/A — endpoint es solo `GET`, sin body.

### Campos del backend — Response (`DashboardResponse`)
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `today` | `DashboardSummary` | No | Resumen del día actual (UTC). |
| `thisMonth` | `DashboardSummary` | No | Resumen desde el primer día del mes actual. |
| `thisYear` | `DashboardSummary` | No | Resumen desde el 1 de enero del año actual. |
| `byType` | `List<DocumentsByType>` | No | Agregado por tipo de comprobante (01/03/07/08) sobre TODOS los documentos del tenant. |
| `byStatus` | `List<DocumentsByStatus>` | No | Conteo por estado (`draft`, `signed`, `sent`, `accepted`, `rejected`, `voided`). |
| `monthlySales` | `List<MonthlySales>` | No | Ventas mensuales agrupadas por año-mes desde el inicio del año. |

`DashboardSummary` (`DashboardDto.cs:12`):
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `totalDocuments` | `int` | No | Conteo total de documentos en el período. |
| `totalAmount` | `decimal` | No | Suma de `Document.Total`. |
| `totalIgv` | `decimal` | No | Suma de `Document.Igv`. |
| `accepted` | `int` | No | Documentos con `status == "accepted"`. |
| `rejected` | `int` | No | Documentos con `status == "rejected"`. |
| `pending` | `int` | No | Documentos con `status` en (`draft`, `sent`, `signed`). |

`DocumentsByType` (`DashboardDto.cs:21`):
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `documentType` | `string` | No | Código SUNAT (`01`, `03`, `07`, `08`). |
| `name` | `string` | No | Nombre legible vía `DocumentType.GetName()` (ej. "FACTURA ELECTRÓNICA"). |
| `count` | `int` | No | Cantidad de documentos de ese tipo. |
| `total` | `decimal` | No | Suma de `Total`. |

`DocumentsByStatus` (`DashboardDto.cs:22`):
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `status` | `string` | No | Valor literal del estado. |
| `count` | `int` | No | Cantidad. |

`MonthlySales` (`DashboardDto.cs:23`):
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `month` | `string` | No | Formato `YYYY-MM`. |
| `year` | `int` | No | Año (el frontend lo usa como `month` numérico — ver gotchas). |
| `count` | `int` | No | Cantidad de comprobantes del mes. |
| `total` | `decimal` | No | Suma de `Total` del mes. |

### Detalle de KPI cards y widgets

| Card / Widget | Endpoint | Cálculo / origen |
|---|---|---|
| **VENTAS DEL MES** (S/) | `/v1/dashboard` → `thisMonth.totalAmount` | Suma de `Document.Total` con `IssueDate >= primer día del mes UTC`. Delta % vs mes anterior calculado client-side comparando los últimos dos elementos de `monthlySales` (`page.tsx:297`). |
| **COMPROBANTES EMITIDOS** | `/v1/dashboard` → `thisMonth.totalDocuments` | Conteo total del mes. Delta % vs mes anterior, igual lógica que ventas (`page.tsx:307`). |
| **PENDIENTES SUNAT** | `/v1/dashboard` → `thisMonth.pending` | Conteo de docs con `status` en (`draft`, `sent`, `signed`) del mes. |
| **RECHAZOS** | `/v1/dashboard` → `thisMonth.rejected` | Conteo de docs con `status == "rejected"` del mes. |
| **Bar chart "Ventas mensuales"** | `/v1/dashboard` → `monthlySales` | Recharts `<BarChart>` con `dataKey="total"` por mes del año actual. Tooltip muestra total y `count`. |
| **Donut "Estado SUNAT"** | `/v1/dashboard` → `byStatus` | Recharts `<PieChart>` filtrando solo estados con `value > 0`. |
| **Lista "Últimos comprobantes"** | `/v1/documents?pageSize=5&page=1` | Render de `fullNumber`, `documentTypeName`, `customerName`, `currency + total`, `status`. |
| **Bars "Por tipo de comprobante"** | `/v1/dashboard` → `byType` | Barras horizontales con `total` y `count` por tipo, escala relativa al max. |
| **Banner "Pregúntale a TukiFact"** | — (estático) | Tres atajos a `/ai?q=<preset>`. |

### Quick actions (botones del header)
- **Exportar** → botón `<Button variant="outline">` sin handler conectado (FLAG: el botón no tiene `onClick`; UI presente pero no funcional).
- **Emitir comprobante** → `<Link href="/documents/new">`.

### Enums / Catálogos SUNAT relevantes
- Catálogo 01 (tipo de documento): `01`, `03`, `07`, `08` via `DocumentType` (`Domain/Enums/DocumentType.cs:3`).
- Estados internos (NO SUNAT): `draft`, `signed`, `sent`, `accepted`, `rejected`, `observed`, `voided` (`Domain/Enums/DocumentStatus.cs:3`).

### Servicios externos invocados
- SUNAT: NO (solo lee `Document.Status` que sí refleja resultado de envío SUNAT).
- Lookup: NO.
- ExchangeRate: NO.
- Email: NO.
- MinIO: NO.
- NATS: NO.

### Validación oficial del backend
No tiene validador — endpoint solo lee. `tenantId` se obtiene desde `ITenantProvider`, no del request.

### UI propuesto siguiendo DESIGN-CLIENT
- **Page header**: saludo `Hola, {firstName} 👋` + fecha actual en es-PE + subtítulo "Aquí está el resumen de tu facturación". Acciones a la derecha: botón "Exportar" (outline) y "Emitir comprobante" (accent yellow).
- **Toolbar / filtros**: ninguno (FLAG: no hay selector de período — todos los KPIs son fijos a "mes actual UTC").
- **Layout**: grid 4 columnas de KPI cards → row con bar chart (col-span 2) + dona (col-span 1) → row con tabla últimos comprobantes + bars por tipo → banner AI full-width.
- **PillGroup / Select**: ninguno.
- **Empty states**: por widget — `<EmptyState icon={Inbox} title body cta>` para chart, dona, últimos comprobantes y byType.
- **Smart helpers**: deltas verde/rojo con flecha por KPI, sparkline en cards de ventas y comprobantes (usa `monthlySales`), tooltip rich en hover de bar chart.

### Estados / flujo
1. Carga: spinner "Cargando datos…" mientras `Promise.allSettled` resuelve ambos endpoints.
2. Loaded: muestra cards con datos; si un endpoint falla, el otro sigue renderizando (allSettled).
3. Empty: cada widget tiene su propio empty state si su slice está vacía.

### Edge cases / gotchas del backend
- `DashboardService.GetSummary` (`DashboardService.cs:54`) categoriza como **pendientes** a `draft + sent + signed`; los estados `observed` no caen en ningún bucket — ni `accepted`, ni `rejected`, ni `pending`. FLAG: `observed` se pierde de los KPIs.
- `MonthlySales.Year` en el DTO trae el año, pero el frontend (`dashboard/page.tsx:291`) lo usa como índice de mes vía `MONTHS[m.month - 1]` — el frontend asume un campo `month`. **FLAG**: incoherencia entre DTO (`record MonthlySales(string Month, int Year, ...)`) y el uso TS (`m.month`). El TS probablemente recibe `month` por serialización camelCase con valor del año (?), o existe un mapping/desincronización que necesita revisarse.
- Períodos calculados con `DateTime.UtcNow`, no con la zona horaria de Lima — un comprobante emitido en la noche-Lima puede caer en el día UTC siguiente.
- `byType` y `byStatus` son sobre TODO el histórico del tenant, no solo del mes.
- No usa `IDocumentRepository`; agrega directo sobre `AppDbContext` — los filtros multi-tenant son responsabilidad del service, no de un global query filter en este punto.

### Navegación adyacente
- `/documents/new` (Emitir), `/documents` (Ver todos), `/ai` (Banner AI).

---

## /welcome — Onboarding post-registro

### Propósito
Pantalla checklist mostrada después del registro para guiar al usuario por los 4 pasos críticos: empresa registrada, crear series, emitir primer comprobante y configurar certificado digital.

### Endpoints REST consumidos
- `GET /v1/series` — verifica si el tenant ya creó al menos una serie.
- `GET /v1/documents?pageSize=1` — verifica si ya emitió algún comprobante (lee `pagination.totalCount`).

### Backend behind
- Controllers: `src/TukiFact.Api/Controllers/SeriesController.cs:11` (GET) y `src/TukiFact.Api/Controllers/DocumentsController.cs` (GET list).
- Service: ninguno (solo lectura de existencia).
- Entity: `src/TukiFact.Domain/Entities/Series.cs:3`, `src/TukiFact.Domain/Entities/Document.cs`.
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/SeriesRepository.cs:7`.
- Validator: no aplica.

### Campos del backend — Request Create
N/A — esta pantalla no crea recursos; solo redirige al usuario a las rutas reales.

### Campos del backend — Request Update
Update no implementado — `/welcome` no actualiza nada.

### Campos del backend — Response
Solo consume responses ya documentados en otras secciones:
- `GET /v1/series` → `SeriesResponse[]` (ver sección /series).
- `GET /v1/documents` → `PaginatedResponse<DocumentResponse>` (ver sección /reports).

### Pasos del onboarding (definidos client-side en `welcome/page.tsx:37`)

| ID | Título | Descripción | Check | Endpoint backing |
|---|---|---|---|---|
| `company` | Registrar Empresa | "Tu empresa ya está registrada con su RUC" | `!!user?.tenantId` | Ninguno — se infiere del JWT/contexto auth. |
| `series` | Crear Series | "Agrega al menos una serie (ej: F001 para Facturas)" | `series.length > 0` | `GET /v1/series` |
| `document` | Emitir Primer Comprobante | "Emite tu primera factura o boleta electrónica" | `pagination.totalCount > 0` | `GET /v1/documents?pageSize=1` |
| `certificate` | Configurar Certificado Digital | "Sube tu certificado para firmar comprobantes (opcional en beta)" | `false` (hardcoded — siempre incompleto) | Ninguno — **FLAG**: no consulta `/v1/certificate` u otro endpoint real. |

### ¿Tiene formularios? ¿Llama a endpoints de configuración?
No tiene formularios. Solo lee estados y redirige con `router.push(step.href)` a las rutas reales (`/settings`, `/series`, `/documents/new`). No llama endpoints de configuración.

### ¿Cómo se decide si se muestra o no?
**FLAG**: la ruta `/welcome` existe pero no se encontró lógica de redirección automática post-registro en el código auditado (no hay middleware ni guard). Hoy el usuario tiene que navegar manualmente. Cuando `completedCount >= 3` aparece el botón "Ir al Dashboard" pero no es auto-redirect. Ver `welcome/page.tsx:181`.

### Enums / Catálogos SUNAT relevantes
Ninguno propio. Las verificaciones se apoyan en `Series` y `Document`.

### Servicios externos invocados
- SUNAT: NO.
- Lookup: NO.
- ExchangeRate: NO.
- Email: NO.
- MinIO: NO.
- NATS: NO.

### Validación oficial del backend
No tiene validador propio. Cada endpoint consumido ya tiene `[Authorize]` exigido por `SeriesController` y `DocumentsController`.

### UI propuesto siguiendo DESIGN-CLIENT
- **Page header**: branding "T" en cuadrado azul + h1 "Bienvenido a TukiFact" centrado + subtítulo motivacional.
- **Progress card**: barra de progreso `0–100%` con leyenda `X/4 completados`.
- **Steps**: lista vertical de cards con ícono + título + descripción + check/circle + botón "Ir →" si incompleto.
- **CTA final**: botón "Ir al Dashboard" condicional cuando `completedCount >= 3`.
- **Smart helpers**: card pintada en verde-suave cuando el paso está done; ícono `<CheckCircle/>` reemplaza al `<Circle/>`.

### Estados / flujo
1. Mount → corre `runChecks()` que invoca cada `step.check()` en serie.
2. Mientras `isLoading`: cards sin botón "Ir".
3. Cuando termina: cards muestran estado real; el botón final aparece solo si >=3 completados.

### Edge cases / gotchas del backend
- Paso `certificate` siempre devuelve `false` — la pantalla **nunca** alcanza 4/4 hasta que el frontend conecte un endpoint real (probablemente `/v1/certificate/status`).
- El paso `series.check` y `document.check` corren en serie con `for...of await` — si la API es lenta, la pantalla puede tardar. Aceptable porque son 2 GETs ligeros.
- Si `GET /v1/series` o `/v1/documents` falla, el `catch` devuelve `false` → el step se marca incompleto, no se notifica error al usuario.

### Navegación adyacente
- `/settings` (paso `company` y `certificate`), `/series` (paso `series`), `/documents/new` (paso `document`), `/dashboard` (CTA final).

---

## /products — Catálogo de productos

### Propósito
CRUD de productos / servicios reutilizables en comprobantes. Maneja código interno (SKU), código SUNAT opcional (catálogo 25), descripción, precios sin/con IGV con auto-bridge, tipo de IGV, unidad de medida, moneda y clasificación.

### Endpoints REST consumidos
- `GET /v1/products?search=&category=&isActive=&page=&pageSize=` — listado paginado con filtros.
- `GET /v1/products/{id}` — detalle (no usado por la UI actual).
- `POST /v1/products` — crear.
- `PUT /v1/products/{id}` — actualizar.
- `DELETE /v1/products/{id}` — eliminar.

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/ProductsController.cs:13`
- Service: no tiene service — el controller habla directo con `AppDbContext`.
- Entity: `src/TukiFact.Domain/Entities/Product.cs:3`
- Configuration: `src/TukiFact.Infrastructure/Persistence/Configurations/ProductConfiguration.cs:7`
- Repository: no implementado — uso directo de `DbSet<Product>`.
- Validator: no tiene validador — FLAG.

### Campos del backend — Request Create (`CreateProductRequest`, `ProductsController.cs:24`)
| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `code` | `string` | Sí | Único por tenant (índice `TenantId+Code`); max 50 (config). Sin validación de longitud en el controller. | SKU interno. | `<Input className="mono">` con placeholder `PROD-001`. |
| `description` | `string` | Sí | Max 500 (config). No validado en controller. | Texto que aparece en el comprobante. | `<Input>` 1 línea. |
| `unitPrice` | `decimal` | Sí | `precision(18,4)`. No validado >= 0 en backend. | Precio SIN IGV. | `<Input type="number" step="0.01">` con prefijo de moneda. |
| `unitPriceWithIgv` | `decimal` | Sí | `precision(18,4)`. No validado >= 0. | Precio CON IGV. Auto-bridge en UI según `igvType`. | Igual al anterior. |
| `sunatCode` | `string?` | No | Max 20 (config). | Código UNSPSC (catálogo SUNAT 25). | `<Input className="mono">` opcional. |
| `currency` | `string` | No, default `"PEN"` | Max 3 (config). No valida contra ISO 4217 ni catálogo 02. | Moneda. | `PillGroup` PEN / USD. |
| `igvType` | `string` | No, default `"10"` | Max 2. No valida contra catálogo. | `10`=Gravado, `20`=Exonerado, `30`=Inafecto, `21`=Gratuito. | `PillGroup` 3 opciones (la UI no expone `21`). |
| `unitMeasure` | `string` | No, default `"NIU"` | Max 10. No valida contra catálogo 03. | NIU, ZZ, KGM, LTR, MTR. | `<Select>` con UNITS. |
| `category` | `string?` | No | Max 100. | Clasificación libre del usuario. | `<Input>` libre. |
| `brand` | `string?` | No | Max 100. | Marca libre. | `<Input>` libre. |

### Campos del backend — Request Update (`UpdateProductRequest`, `ProductsController.cs:37`)
| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `description` | `string?` | No | Solo se asigna si != null. | Editable. | `<Input>`. |
| `unitPrice` | `decimal?` | No | `HasValue` ⇒ asigna. | Editable. | numeric. |
| `unitPriceWithIgv` | `decimal?` | No | idem. | Editable. | numeric. |
| `sunatCode` | `string?` | No | Se asigna si != null (no permite *vaciar* a null tras tener valor: enviar `""` lo dejaría como cadena vacía, FLAG). | Editable. | input. |
| `currency` | `string?` | No | idem. | Editable. | pill. |
| `igvType` | `string?` | No | idem. | Editable. | pill. |
| `unitMeasure` | `string?` | No | idem. | Editable. | select. |
| `category` | `string?` | No | idem. | Editable. | input. |
| `brand` | `string?` | No | idem. | Editable. | input. |
| `isActive` | `bool?` | No | `HasValue` ⇒ asigna. | Activación/baja lógica. | toggle (no expuesto hoy en la UI). |

Nota: **`code` no es editable**. El controller no lo expone en `UpdateProductRequest`. La UI bloquea el input con `disabled={!!editId}` (`products/page.tsx:358`).

### Campos del backend — Response (list endpoint)
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `id` | `Guid` | No | UUID generado por Postgres. |
| `code` | `string` | No | SKU. |
| `sunatCode` | `string?` | Sí | Código SUNAT opcional. |
| `description` | `string` | No | Descripción. |
| `unitPrice` | `decimal` | No | Precio sin IGV. |
| `unitPriceWithIgv` | `decimal` | No | Precio con IGV. |
| `currency` | `string` | No | Moneda (3 chars). |
| `igvType` | `string` | No | Código IGV. |
| `unitMeasure` | `string` | No | Unidad SUNAT. |
| `category` | `string?` | Sí | Clasificación. |
| `brand` | `string?` | Sí | Marca. |
| `isActive` | `bool` | No | Estado. |
| `createdAt` | `DateTimeOffset` | No | Timestamp creación. |

`GET /v1/products/{id}` agrega `updatedAt`.

Sobre del listado:
```json
{ "data": [...], "pagination": { "page", "pageSize", "totalCount", "totalPages" } }
```

### Enums / Catálogos SUNAT relevantes
- Catálogo 25 (productos UNSPSC) → `sunatCode`. No validado.
- Catálogo 03 (unidad de medida) → `unitMeasure`. No validado.
- Catálogo 07 (tipo de afectación IGV) → `igvType`. Códigos `10/20/30/21` usados, no validados contra catálogo en runtime.
- Catálogo 02 (moneda) → `currency`. No validado.

### Servicios externos invocados
- SUNAT: NO.
- Lookup: NO.
- ExchangeRate: NO.
- Email: NO.
- MinIO: NO.
- NATS: NO.

### Validación oficial del backend
- `code` único por tenant: chequeado vía `AnyAsync` en `Create` (`ProductsController.cs:117`) → `409 Conflict` si duplica.
- `[Authorize]` por defecto + roles: `Create`/`Update` requieren `admin,emisor`; `Delete` requiere `admin`.
- Filtro multi-tenant: `TenantId == tenantId` en todas las queries.
- Longitudes y precisión vienen de `ProductConfiguration` pero **no** se validan en runtime — un payload con `code` de 60 caracteres revienta en SaveChanges, no antes.
- **FLAG: no se valida que `unitPrice <= unitPriceWithIgv`, ni que `igvType ∈ {10,20,30,21}`, ni que `currency ∈ {PEN,USD,...}`, ni que `unitMeasure ∈ catálogo 03`. Solo la UI valida `>= 0` y descripción/código no vacíos.**

### UI propuesto siguiendo DESIGN-CLIENT
- **Page header**: `Catálogo de productos` + subtítulo con conteo total + botón accent "Nuevo producto".
- **Toolbar / filtros**: card con `<Input>` de búsqueda (icon `Search`) debounce 300ms. No expone `category` ni `isActive` (FLAG: existen en API pero no en UI).
- **Tabla**: columnas Código (+ sunatCode debajo), Descripción (+ category·brand), Precio (con IGV grande, sin IGV pequeño), badge IGV, Unidad mono, acciones.
- **Modal de form**: dos columnas para Código + Código SUNAT, descripción full-width, PillGroup 3 cols IGV, dos columnas precios con auto-bridge cuando `igvType==10`, PillGroup 2 cols Moneda + Select Unidad, Categoría + Marca.
- **PillGroup / Select**: PillGroup `IGV_TYPES` (Gravado/Exonerado/Inafecto), PillGroup `CURRENCIES` (PEN/USD), Select `UNITS` (NIU/ZZ/KGM/LTR/MTR).
- **Empty states**: vacío total → card amarilla con icono Package + "Tu catálogo está vacío" + CTA "Crear primer producto". Búsqueda sin resultados → estado neutral con "Sin resultados para …".
- **Smart helpers**: auto-bridge precio neto ↔ bruto según IGV 18% en tiempo real (`useRef<'net'|'gross'>` para evitar feedback loop); símbolo de moneda dentro del input; caption explicando el bridge según `igvType`.

### Estados / flujo
- List → debounced search → re-fetch.
- Crear: form → `POST` → toast success → cierra dialog → refetch.
- Editar: pre-rellena form con producto, deshabilita `code`, `PUT` parcial → toast → refetch.
- Eliminar: `confirm()` nativo del navegador → `DELETE` → refetch. FLAG: el modelo no soporta soft-delete real (hay `IsActive` pero `DELETE` hace `Remove`, no toggle).

### Edge cases / gotchas del backend
- `Update` no permite vaciar campos opcionales a null: solo asigna si el cliente envía `!= null`. Para "limpiar" categoría hay que enviar `""`, lo cual deja string vacío en DB.
- `Delete` es **hard delete** — si el producto está referenciado por algún `DocumentItem`, fallará por FK constraint (depende del config). FLAG: confirmar comportamiento en `DocumentItemConfiguration`.
- `CreateProductRequest` tiene defaults (`Currency="PEN"`, `IgvType="10"`, `UnitMeasure="NIU"`) — un payload sin esos campos los completa.
- No hay endpoint `GET /v1/products/search` ni `/by-code` para autocompletar en facturas — el lookup se hace vía el filtro `search` del listado.
- No hay validador FluentValidation (a diferencia de `DespatchAdviceValidator` y `RecurringInvoiceValidator` que sí existen para otras entidades).

### Navegación adyacente
- Sin links externos en la página; los productos se reusan desde `/documents/new` (no auditado aquí) vía búsqueda.

---

## /customers — Directorio de clientes

### Propósito
CRUD de clientes y proveedores. Soporta RUC, DNI, CE, Pasaporte y "Sin documento" para consumidores eventuales. Integra lookup externo (RUC/DNI) para autocompletar nombre y dirección.

### Endpoints REST consumidos
- `GET /v1/customers?search=&docType=&category=&isActive=&page=&pageSize=` — listado paginado.
- `GET /v1/customers/{id}` — detalle.
- `GET /v1/customers/search?docNumber=` — búsqueda exacta por número de documento (usado por flujos de comprobantes; no por esta UI).
- `POST /v1/customers` — crear.
- `PUT /v1/customers/{id}` — actualizar.
- `DELETE /v1/customers/{id}` — eliminar.
- `GET /v1/services/lookup/status` — estado del proveedor de lookup configurado.
- `GET /v1/services/lookup/ruc/{number}` — autocompletar por RUC.
- `GET /v1/services/lookup/dni/{number}` — autocompletar por DNI.

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/CustomersController.cs:13`
- Lookup controller: `src/TukiFact.Api/Controllers/ExternalServicesController.cs:20` (rutas `/v1/services/lookup/*`).
- Service: no tiene service — controller toca `AppDbContext` directo.
- Entity: `src/TukiFact.Domain/Entities/Customer.cs:3`
- Configuration: `src/TukiFact.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs:7`
- Repository: no implementado.
- Validator: no tiene validador — FLAG.

### Campos del backend — Request Create (`CreateCustomerRequest`, `CustomersController.cs:24`)
| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `docType` | `string` | Sí | Max 2 (config). No valida contra catálogo 06. | `6`=RUC, `1`=DNI, `4`=CE, `7`=Pasaporte, `0`=Sin doc. | PillGroup 5 opciones. |
| `docNumber` | `string` | Sí en backend; UI permite vacío si `docType==0`. | Max 20 (config). Único por tenant (índice `TenantId+DocNumber`). No valida longitud por tipo de doc. | Número del documento. | `<Input className="mono">` + botón lookup para `docType==6\|1`. |
| `name` | `string` | Sí | Max 300. | Razón social o nombre. | `<Input>` con uppercase si `docType==6`. |
| `email` | `string?` | No | Max 200. No valida formato email. | Contacto. | `<Input type="email">` con icon Mail. |
| `phone` | `string?` | No | Max 30. | Contacto. | input con icon Phone. |
| `address` | `string?` | No | Max 500. | Dirección. | input con icon MapPin. |
| `ubigeo` | `string?` | No | Max 6. No valida contra `Ubigeo` table. | Código de ubigeo SUNAT. | UI no lo expone — FLAG (hueco). |
| `departamento` | `string?` | No | Max 100. | Auto-rellenable por lookup RUC. | UI no expone — FLAG. |
| `provincia` | `string?` | No | Max 100. | idem. | UI no expone — FLAG. |
| `distrito` | `string?` | No | Max 100. | idem. | UI no expone — FLAG. |
| `category` | `string?` | No | Max 50. | Clasificación libre. | input libre. |
| `notes` | `string?` | No | Max 1000. | Notas internas. | textarea (UI hoy lo manda como string vacío y no lo expone — FLAG: existe en API pero no en form). |

### Campos del backend — Request Update (`UpdateCustomerRequest`, `CustomersController.cs:39`)
| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `name` | `string?` | No | Solo asigna si != null. | Editable. | input. |
| `email` | `string?` | No | idem. | Editable. | input. |
| `phone` | `string?` | No | idem. | Editable. | input. |
| `address` | `string?` | No | idem. | Editable. | input. |
| `ubigeo` | `string?` | No | idem. | Editable. | input. |
| `departamento` | `string?` | No | idem. | Editable. | input. |
| `provincia` | `string?` | No | idem. | Editable. | input. |
| `distrito` | `string?` | No | idem. | Editable. | input. |
| `category` | `string?` | No | idem. | Editable. | input. |
| `notes` | `string?` | No | idem. | Editable. | textarea. |
| `isActive` | `bool?` | No | `HasValue` ⇒ asigna. | Activación/baja lógica. | toggle. |

`docType` y `docNumber` **no son editables** — la UI lo bloquea con `disabled={!!editId}` (`customers/page.tsx:369`).

### Campos del backend — Response (list endpoint)
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `id` | `Guid` | No | UUID. |
| `docType` | `string` | No | Catálogo 06. |
| `docNumber` | `string` | No | Número. |
| `name` | `string` | No | Razón social / nombre. |
| `email` | `string?` | Sí | — |
| `phone` | `string?` | Sí | — |
| `address` | `string?` | Sí | — |
| `category` | `string?` | Sí | — |
| `isActive` | `bool` | No | — |
| `createdAt` | `DateTimeOffset` | No | — |

`GET /{id}` agrega `ubigeo`, `departamento`, `provincia`, `distrito`, `notes`, `updatedAt`.
`GET /search?docNumber=` agrega solo: `id, docType, docNumber, name, email, phone, address`.

### Enums / Catálogos SUNAT relevantes
- Catálogo 06 (tipo de documento de identidad): `6`, `1`, `4`, `7`, `0`. **No validado por backend**.
- Tabla `Ubigeo` (`AppDbContext.Ubigeos`) cargada en `Domain/Entities/Ubigeo.cs` — relacionable con `Customer.Ubigeo` pero no enforced.

### Servicios externos invocados
- SUNAT: NO directamente (los providers de lookup terminan llamando RUC/RENIEC, pero la API SUNAT oficial no se invoca desde esta pantalla).
- Lookup: **SÍ** — vía `/v1/services/lookup/{ruc|dni}/{number}`. Proveedores soportados: `apiperu`, `migo`, `peruapi`, `apis_net` (`ExternalServicesController.cs:333`). Cada tenant configura su API key.
- ExchangeRate: NO.
- Email: NO.
- MinIO: NO.
- NATS: NO.

### Validación oficial del backend
- `[Authorize]` por defecto; `Create`/`Update` requieren `admin,emisor`; `Delete` requiere `admin`.
- `docNumber` único por tenant: chequeado vía `AnyAsync` en `Create` (`CustomersController.cs:140`) → `409 Conflict` si duplica.
- Multi-tenant filter por `TenantId`.
- Lookup RUC requiere 11 dígitos (`ExternalServicesController.cs:173`).
- Lookup DNI requiere 8 dígitos (`ExternalServicesController.cs:197`).
- Lookup require proveedor configurado en `TenantServiceConfig` → `400 Bad Request` si no.
- **FLAG**: no valida formato de email, no valida longitud por tipo de documento (RUC=11, DNI=8 no se chequea en `POST /v1/customers`), no valida `docType` contra catálogo, no normaliza `docNumber` (espacios), no requiere `ubigeo` para clientes RUC con dirección.

### UI propuesto siguiendo DESIGN-CLIENT
- **Page header**: `Directorio de clientes` + conteo + botón accent "Nuevo cliente".
- **Toolbar**: card con búsqueda debounced 300ms por nombre/RUC/DNI.
- **Tabla**: columnas Documento (badge tipo + número), Nombre + dirección, Contacto (email + phone), Categoría (badge info azul), acciones.
- **Modal de form**: PillGroup 5 cols tipo de documento, input número de doc + botón "Buscar" si `docType ∈ {6,1}` (deshabilitado si lookup no configurado), Nombre (uppercase si RUC), grid Email + Teléfono con icons, Dirección con icon, Categoría libre + caption explicando para filtros.
- **PillGroup / Select**: PillGroup `DOC_TYPES` 5 opciones.
- **Empty states**: vacío total → card con icono Users + CTA "Crear primer cliente"; búsqueda sin resultados → "Sin resultados para …" + botón limpiar.
- **Smart helpers**: lookup auto-fill de nombre + dirección desde proveedor configurado; tooltip explicativo en el botón si no hay proveedor; toast guía a Configuración → Servicios Externos si falla.

### Estados / flujo
- List con debounce → re-fetch.
- Crear: pick `docType` → `docNumber` con maxLength dinámico → opcional Buscar → Nombre, contacto, dirección, categoría → `POST` → toast → refetch.
- Editar: pre-rellena, deshabilita docType/docNumber, `PUT` parcial.
- Eliminar: `confirm()` → `DELETE` → refetch.
- Lookup: muestra spinner en el botón Buscar; en error con "No hay proveedor" redirige verbalmente al setting.

### Edge cases / gotchas del backend
- `notes` existe en backend pero la UI lo deja siempre `""` (`customers/page.tsx:200`) — los notes nunca se guardan desde esta pantalla.
- `ubigeo`, `departamento`, `provincia`, `distrito` no se exponen en el form — si el lookup RUC los devuelve, la UI sólo persiste `address`. **FLAG**.
- `Delete` es hard delete; si el cliente fue usado en un comprobante, podría haber FK constraint dependiendo de `DocumentConfiguration`.
- `Update` no permite poner a null campos opcionales — patrón idéntico al de productos.
- `GET /v1/customers/search?docNumber=` lookup exacto, no parcial. No usado por la UI actual de `/customers`.

### Navegación adyacente
- Sin enlaces; se usa desde flujo `/documents/new`. La UI menciona "Configuración → Servicios Externos" en tooltip pero no enlaza.

---

## /series — Gestión de series (F001, B001, T001…)

### Propósito
Administra los códigos de serie SUNAT por tipo de comprobante (`01` factura, `03` boleta, `07` nota de crédito, `08` nota de débito) y su correlativo actual. Las series son la base para numerar comprobantes.

### Endpoints REST consumidos
- `GET /v1/series` — lista series activas del tenant ordenadas por `documentType`, `serie`.
- `POST /v1/series` — crea serie.
- `PUT /v1/series/{id}` — actualiza `IsActive` y/o `EmissionPoint`.

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/SeriesController.cs:13`
- Service: no tiene service capa; el controller usa el repo directo.
- Entity: `src/TukiFact.Domain/Entities/Series.cs:3`
- Configuration: `src/TukiFact.Infrastructure/Persistence/Configurations/SeriesConfiguration.cs:7`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/SeriesRepository.cs:7`
- Repository interface: `src/TukiFact.Application/Interfaces/ISeriesRepository.cs:5`
- DTOs: `src/TukiFact.Application/DTOs/Series/SeriesDto.cs:3`
- Validator: no tiene validador — FLAG.

### Campos del backend — Request Create (`CreateSeriesRequest`, `SeriesDto.cs:3`)
| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `documentType` | `string` | Sí | Max 2 (config). No valida contra `DocumentType.IsValid()` en `POST`. | Tipo SUNAT (`01`, `03`, `07`, `08`). | PillGroup 2 cols 4 opciones. |
| `serie` | `string` | Sí | Max 4 (config); único `(TenantId, DocumentType, Serie)`. No hay regex en backend; la UI exige `/^[BFR][A-Z0-9]{3}$/`. | Código de serie (F001, B001, FC01, etc.). | `<Input className="mono" maxLength={4}>` con validación regex client-side. |
| `emissionPoint` | `string` | No, default `"PRINCIPAL"` | Max 50. | Punto de emisión / sucursal. | input. |

### Campos del backend — Request Update (`UpdateSeriesRequest`, `SeriesController.cs:74`)
| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `isActive` | `bool?` | No | `HasValue` ⇒ asigna. | Activación / baja lógica. | toggle (UI hoy NO lo expone — FLAG: la UI lista pero no permite editar). |
| `emissionPoint` | `string?` | No | != null ⇒ asigna. | Editable. | input (UI NO lo expone). |

**FLAG**: la pantalla actual sólo lista — no implementa la edición ni botón para `PUT`. Existe el endpoint pero la UI no lo consume.

### Campos del backend — Response (`SeriesResponse`, `SeriesDto.cs:9`)
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `id` | `Guid` | No | UUID. |
| `documentType` | `string` | No | Tipo SUNAT. |
| `serie` | `string` | No | Código serie. |
| `currentCorrelative` | `long` | No | Último correlativo emitido. El próximo será `+1`. |
| `emissionPoint` | `string` | No | Punto de emisión. |
| `isActive` | `bool` | No | Estado. |
| `createdAt` | `DateTimeOffset` | No | Timestamp. |

### Enums / Catálogos SUNAT relevantes
- Catálogo 01 (tipo de documento). Las 4 opciones SUNAT que esta pantalla expone: `01`, `03`, `07`, `08`. La entidad acepta también `RC`/`RA` (resumen/comunicación) según comentario en `Series.cs:7`, pero la UI no los expone.

### Servicios externos invocados
- SUNAT: NO (la serie se persiste localmente; SUNAT recibe el correlativo en el momento de emitir el comprobante).
- Lookup: NO.
- ExchangeRate: NO.
- Email: NO.
- MinIO: NO.
- NATS: NO.

### Validación oficial del backend
- `[Authorize]` por defecto; `Create`/`Update` requieren `admin`.
- Unicidad `(TenantId, DocumentType, Serie)`: chequeada vía `GetByTypeAndSerieAsync` en `Create` → `409 Conflict` si existe (`SeriesController.cs:42`).
- Atomicidad de correlativo: `GetNextCorrelativeAsync` (`SeriesRepository.cs:35`) hace `UPDATE … SET CurrentCorrelative = CurrentCorrelative + 1 RETURNING …` en una sola sentencia SQL para evitar carreras.
- **FLAG**: el controller NO valida que `documentType ∈ DocumentType.All` ni que `serie` cumpla el patrón SUNAT (`[BFR][A-Z0-9]{3}`). La validación de regex es solo client-side.
- **FLAG**: el listado del repo (`GetByTenantAsync`) filtra `IsActive` — un PUT que ponga `IsActive=false` la oculta de la lista. La UI no notifica esto.

### UI propuesto siguiendo DESIGN-CLIENT
- **Page header**: `Series` + subtítulo educativo SUNAT + botón "Nueva serie" (oculto en empty state porque hay CTAs por tipo).
- **Toolbar / filtros**: ninguno; la lista es manejable (4–8 series por tenant en promedio).
- **Layout**: agrupado por tipo de comprobante con `useMemo` (`series/page.tsx:301`). Cada grupo es una `<section>` con tabla; tipos sin series muestran card dashed con CTA "Crear F001" (sugerido).
- **Modal de form**: PillGroup 2 cols 4 tipos de comprobante con sugerencia mono (F001/B001/FC01/FD01), Input mono maxLength=4 con caption regex, Input emissionPoint con default `PRINCIPAL`, preview "El primer comprobante será F001-00000001" con badge verde.
- **PillGroup / Select**: PillGroup 4 tipos.
- **Empty states**: hero con icono Hash + 4 cards quickstart (uno por tipo) que abren el modal con preset.
- **Smart helpers**: regex live con feedback rojo si la serie no es válida; preview del próximo comprobante; helper card al final "¿Y si me equivoco con un correlativo?" explicando que se gestiona automáticamente.

### Estados / flujo
1. Mount → `GET /v1/series` → set state.
2. Empty → hero + 4 quickstart cards.
3. Non-empty → agrupado por tipo; cada tipo sin series muestra dashed card.
4. Crear: pick tipo → autosuggest serie → submit `POST` → toast → refetch.
5. **No implementado**: edición de `IsActive`/`EmissionPoint`. El endpoint existe pero la UI no lo invoca.

### Edge cases / gotchas del backend
- `Series` entity admite `documentType` arbitrario (string libre). El comentario menciona `RC`, `RA` (resúmenes diarios / comunicaciones de baja) pero esos NO están en `DocumentType.All` que usa el dashboard.
- `GetByTenantAsync` (`SeriesRepository.cs:17`) **filtra `IsActive` solamente**, no devuelve series desactivadas — la UI nunca verá una serie desactivada.
- `Delete` no existe — no se pueden eliminar series desde la API; sólo desactivar vía `PUT`.
- El correlativo SQL raw asume nombre de tabla `series` con columna `"CurrentCorrelative"` quoted (Postgres case-sensitive) — depende de la convención de naming de EF.
- Cualquier `documentType` que no sea `"01" | "03" | "07" | "08"` se guarda igual sin error. Cuidado al permitirlo desde una UI futura.

### Navegación adyacente
- El paso `series` de `/welcome` enlaza acá.
- `/documents/new` consume `GET /v1/series` para poblar el dropdown.

---

## /catalogs — Explorador de catálogos SUNAT

### Propósito
Explorador read-only de los catálogos SUNAT cargados en la base. Permite buscar por número/nombre de catálogo, expandir cada uno para ver sus códigos, y consultar la tabla aparte de detracciones (catálogo 54).

### Endpoints REST consumidos
- `GET /v1/catalogs` — lista todos los catálogos activos con `codesCount`.
- `GET /v1/catalogs/{catalogNumber}` — devuelve los códigos activos de un catálogo.
- `GET /v1/catalogs/detractions` — lista códigos de detracción (catálogo 54).

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/CatalogsController.cs:11`
- Service: no tiene service; controller habla directo con `AppDbContext`.
- Entities: `src/TukiFact.Domain/Entities/SunatCatalog.cs:3`, `src/TukiFact.Domain/Entities/SunatCatalogCode.cs:3`, `src/TukiFact.Domain/Entities/DetractionCode.cs:3`.
- Configurations: `src/TukiFact.Infrastructure/Persistence/Configurations/SunatCatalogConfiguration.cs:7`, `SunatCatalogCodeConfiguration.cs:7`, `DetractionCodeConfiguration.cs:7`.
- Repository: no implementado.
- Validator: no aplica (solo lectura).

### Campos del backend — Request Create
N/A — pantalla solo de consulta. Los catálogos se cargan vía seed/migration; el usuario no los modifica.

### Campos del backend — Request Update
Update no implementado.

### Campos del backend — Response

`GET /v1/catalogs` (`CatalogsController.cs:24`):
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `catalogNumber` | `string` (max 5) | No | Número SUNAT (`01`, `02`, `06`, `07`, `54`, …). |
| `name` | `string` | No | Nombre legible. |
| `description` | `string?` | Sí | Descripción opcional. |
| `codesCount` | `int` | No | Conteo de `Codes.IsActive`. |

`GET /v1/catalogs/{catalogNumber}` (`CatalogsController.cs:44`):
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `catalogNumber` | `string` | No | — |
| `name` | `string` | No | — |
| `description` | `string?` | Sí | — |
| `codes` | `[{ code, description }]` | No | Códigos activos ordenados por `SortOrder`. |

`GET /v1/catalogs/detractions` (`CatalogsController.cs:70`):
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `code` | `string` (max 3) | No | Código SPOT (ej. `037`). |
| `description` | `string` | No | Descripción. |
| `percentage` | `decimal(5,2)` | No | Tasa porcentual. |
| `annex` | `string` (max 5) | No | Anexo `I`, `II`, `III`. |

### Catálogos que muestra (según lo que carguen las migrations seed)
La UI no asume catálogos específicos; muestra **todos** los que estén activos en `sunat_catalogs`. Catálogos típicos SUNAT que el modelo soporta y la convención de la app espera:
- Catálogo 01 — Tipo de documento (factura, boleta, NC, ND).
- Catálogo 02 — Tipo de moneda.
- Catálogo 03 — Unidad de medida.
- Catálogo 06 — Tipo de documento de identidad.
- Catálogo 07 — Tipo de afectación IGV.
- Catálogo 17 — Motivo de traslado (GRE).
- Catálogo 20 — Motivo de notas de crédito.
- Catálogo 25 — Códigos de productos UNSPSC.
- Catálogo 51 — Tipos de operación.
- Catálogo 52 — Leyendas obligatorias.
- Catálogo 53 — Códigos de cargo/descuento.
- **Catálogo 54** — Detracciones (SPOT), pero servido por endpoint dedicado `/v1/catalogs/detractions`.

(La lista exacta depende del seed cargado. La pantalla los enumera dinámicamente.)

### ¿Read-only o se pueden ingresar custom codes?
**Read-only puro**. El controller solo expone GETs. La UI lo enfatiza con el helper card "Los catálogos los gestiona SUNAT, no tú". No existe endpoint para crear/editar `SunatCatalog`, `SunatCatalogCode` ni `DetractionCode` desde la app (la carga es por migration/seed).

### Enums / Catálogos SUNAT relevantes
Todos los del modelo `SunatCatalog`. Detracciones (catálogo 54) en tabla separada `detraction_codes` con campos `Code`, `Description`, `Percentage`, `Annex`, `IsActive`, `ValidFrom`, `ValidUntil` (estos dos últimos no expuestos por el endpoint).

### Servicios externos invocados
- SUNAT: NO (los datos son locales seedados, no live).
- Lookup: NO.
- ExchangeRate: NO.
- Email: NO.
- MinIO: NO.
- NATS: NO.

### Validación oficial del backend
- `[Authorize]` por defecto.
- Solo lectura, no hay reglas de validación de input.
- `{catalogNumber}` que no exista → `404 NotFound`.

### UI propuesto siguiendo DESIGN-CLIENT
- **Page header**: `Catálogos SUNAT` + subtítulo "Tablas de referencia oficiales".
- **KPIs row**: 3 cards (cantidad de catálogos, total de códigos, cantidad de detracciones) con iconos BookOpen, Hash, Receipt.
- **Toolbar**: `<Input>` de búsqueda inline por número/nombre.
- **Layout**: card "Catálogos de facturación" con `<Accordion>` lazy-load — al expandir un item, se hace `GET /v1/catalogs/{num}` y se cachea en `catalogDetails`.
- **Inside accordion**: filter input de códigos + tabla scrollable max-h 400px con sticky header.
- **Detractions section**: tabla full-width Code / Descripción / % / Anexo.
- **PillGroup / Select**: ninguno.
- **Empty states**: si no hay catálogos cargados (raro) → hero con icono Inbox; si la búsqueda no matches → "Sin resultados".
- **Smart helpers**: badge azul con conteo de códigos por catálogo; helper info al final explicando que la app valida los códigos automáticamente al emitir.

### Estados / flujo
1. Mount → `Promise.all([catalogs, detractions])` → set state.
2. Search top filtra `catalogs` client-side.
3. Expandir item → si no cached → `GET /v1/catalogs/{num}` → cachea.
4. Filter codes dentro del accordion → filtra client-side por `code`/`description`.

### Edge cases / gotchas del backend
- `codesCount` se calcula con `c.Codes.Count(cc => cc.IsActive)` — códigos desactivados no se cuentan.
- `GET /v1/catalogs/{num}` incluye solo `Codes` activos vía `Include(c => c.Codes.Where(cc => cc.IsActive).OrderBy(cc => cc.SortOrder))` (`CatalogsController.cs:48`).
- Detracciones: el endpoint no respeta `ValidFrom`/`ValidUntil` — devuelve todo lo activo; un código con `ValidUntil` pasado seguirá apareciendo. **FLAG**.
- `expandedCatalog` es `number[]` (índice del array filtrado), no `catalogNumber` — si el usuario cambia el `searchTerm` mientras hay un item expandido, los índices se desincronizan. **FLAG** menor.

### Navegación adyacente
- Sin links salientes. Es página de referencia consultada desde otras pantallas mentalmente.

---

## /reports — Reportes y export CSV

### Propósito
Análisis temporal de comprobantes emitidos. Permite filtrar por rango de fechas (presets o manual), ver KPIs agregados client-side, un gráfico de barras por tipo de comprobante (Recharts), una tabla completa del periodo y exportar todo como CSV con BOM UTF-8.

### Endpoints REST consumidos
- `GET /v1/documents?page=1&pageSize=500&dateFrom={YYYY-MM-DD}&dateTo={YYYY-MM-DD}` — listado del periodo.

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/DocumentsController.cs:10`
- Service: `src/TukiFact.Infrastructure/Services/DocumentService.cs` (no auditado aquí — pertenece a Group A/B).
- Entity: `src/TukiFact.Domain/Entities/Document.cs`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/DocumentRepository.cs`
- Validator: existe `DespatchAdviceValidator` y `RecurringInvoiceValidator` pero no documentos. FLAG si se considera.

### Campos del backend — Request Create
N/A — endpoint consumido es solo `GET`.

### Campos del backend — Request Update
Update no implementado.

### Campos del backend — Response (relevantes para esta pantalla)

`PaginatedResponse<DocumentResponse>`:
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `data` | `DocumentResponse[]` | No | Comprobantes del periodo (max 100 según backend pero la UI pide 500 — ver gotchas). |
| `pagination.page` | `int` | No | — |
| `pagination.pageSize` | `int` | No | — |
| `pagination.totalCount` | `int` | No | — |
| `pagination.totalPages` | `int` | No | — |

`DocumentResponse` (los campos que la UI lee directamente):
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `id` | `Guid` | No | — |
| `fullNumber` | `string` | No | `Serie-Correlativo` (ej. `F001-00000123`). |
| `documentType` | `string` | No | `01`/`03`/`07`/`08`. |
| `issueDate` | `DateOnly` (string `YYYY-MM-DD`) | No | Fecha de emisión. |
| `customerDocType` | `string` | No | — |
| `customerDocNumber` | `string` | No | — |
| `customerName` | `string` | No | — |
| `currency` | `string` | No | `PEN`/`USD`. |
| `operacionGravada` | `decimal` | No | Base imponible gravada. |
| `operacionExonerada` | `decimal` | No | Base exonerada. |
| `operacionInafecta` | `decimal` | No | Base inafecta. |
| `igv` | `decimal` | No | Monto del IGV. |
| `total` | `decimal` | No | Total del comprobante. |
| `status` | `string` | No | `draft`/`signed`/`sent`/`accepted`/`rejected`/`observed`/`voided`. |

### KPIs computados client-side (`reports/page.tsx:280`)

| KPI | Cálculo | Filtro |
|---|---|---|
| `totalVentas` | `sum(d.total)` | Solo `status == "accepted"`. |
| `totalIgv` | `sum(d.igv)` | Solo `accepted`. |
| `totalGravada` | `sum(d.operacionGravada)` | Solo `accepted` (mostrado en tfoot). |
| `totalDocs` | `documents.length` | Todos. |
| `totalAceptados` | `count(status=='accepted')` | — |
| `totalAnulados` | `count(status=='voided')` | — |
| `totalRechazados` | `count(status=='rejected')` | — |

KPI cards renderizados:
- Total ventas (aceptadas) → `totalVentas` (S/).
- IGV recaudado → `totalIgv` (S/).
- Aceptados → `totalAceptados` (entero).
- Anulados / rechazados → `${totalAnulados} / ${totalRechazados}`.

### Chart Recharts

`<BarChart>` doble eje (`reports/page.tsx:485`):
- `data`: array `[{ name: 'Factura' | 'Boleta' | 'Nota crédito' | 'Nota débito', Documentos: count, Total: sum_total }]` con solo comprobantes `accepted`.
- `<XAxis dataKey="name">`.
- `<YAxis yAxisId="left">` para `Documentos`.
- `<YAxis yAxisId="right" orientation="right">` para `Total`.
- `<Bar yAxisId="right" dataKey="Total" fill="var(--brand-toucan-orange)">`.
- `<Bar yAxisId="left" dataKey="Documentos" fill="var(--brand-toucan-yellow)">`.
- Tooltip con `formatter` que aplica `fmt(value)` (currency `PEN`) cuando el key es `"Total"`.
- Legend visible.

### Export CSV (`reports/page.tsx:190`)

- **Encoding**: UTF-8 con BOM (`\uFEFF`) — abre correctamente en Excel-ES.
- **Separador**: coma `,`.
- **Quote escape**: si el valor contiene `"`, `,`, `\n` o `;` se envuelve en `"` y se duplican las comillas internas (`escapeCsv` en `reports/page.tsx:183`).
- **Filename**: `reporte-comprobantes-{dateFrom}_{dateTo}.csv`.

Columnas (en orden):
| # | Header | Origen |
|---|---|---|
| 1 | `Numero` | `d.fullNumber` |
| 2 | `Tipo` | `DOC_TYPE_LABELS[d.documentType]` (Factura/Boleta/Nota crédito/Nota débito) |
| 3 | `Fecha emision` | `d.issueDate` (string `YYYY-MM-DD`) |
| 4 | `Cliente Doc` | `${d.customerDocType} ${d.customerDocNumber}` |
| 5 | `Cliente Nombre` | `d.customerName` |
| 6 | `Moneda` | `d.currency` |
| 7 | `Gravado` | `d.operacionGravada.toFixed(2)` |
| 8 | `Exonerado` | `d.operacionExonerada.toFixed(2)` |
| 9 | `Inafecto` | `d.operacionInafecta.toFixed(2)` |
| 10 | `IGV` | `d.igv.toFixed(2)` |
| 11 | `Total` | `d.total.toFixed(2)` |
| 12 | `Estado` | `d.status` (raw, sin traducir) |

### Enums / Catálogos SUNAT relevantes
- Catálogo 01 → `documentType` con mapeo a labels.
- Catálogo 02 → `currency`.
- Catálogo 07 vía bases imponibles (Gravado/Exonerado/Inafecto).
- Estados internos `DocumentStatus` (`Domain/Enums/DocumentStatus.cs:3`).

### Servicios externos invocados
- SUNAT: NO directamente (el `status` refleja resultado de envío SUNAT hecho en otro flujo).
- Lookup: NO.
- ExchangeRate: NO.
- Email: NO.
- MinIO: NO.
- NATS: NO.

### Validación oficial del backend
- `[Authorize]` por defecto.
- `pageSize` se **clampa a 100** en el controller (`if (pageSize > 100) pageSize = 100;`).
- Filtro multi-tenant por `TenantId` en `DocumentRepository.ListAsync`.
- `dateFrom`/`dateTo` son `DateOnly?` opcionales.

### UI propuesto siguiendo DESIGN-CLIENT
- **Page header**: `Reportes` + subtítulo de análisis.
- **Toolbar / filtros**: card con grupo de inputs `Desde`/`Hasta` (type date con icon Calendar), botón "Filtrar" / "Cargando…", grupo de chips preset (Hoy, Últimos 7, Este mes, Últimos 30, Mes pasado, Este año), botón "Exportar CSV" alineado a la derecha (disabled si no hay data).
- **Layout**: KPIs `grid-cols-6` (2-2-1-1) → chart card → tabla card con tfoot de totales.
- **PillGroup / Select**: chips preset (estilo PillGroup minimalista en filtros).
- **Empty states**: pre-fetch hero "Elige un periodo para generar tu reporte" con icon BarChart3 + CTA narrativa.
- Sin datos en el periodo seleccionado → tabla con estado "Sin comprobantes en este periodo".
- **Smart helpers**: presets de fechas relativas; tfoot con totales del periodo solo aceptados; chart con tooltip que formatea Total como moneda; export con feedback `toast.success(N filas exportadas)`.

### Estados / flujo
1. Mount: defaults a "Este mes" (preset `month`), no hace fetch automático (`hasFetched=false`).
2. Pre-fetch: muestra hero.
3. Click "Filtrar" → `GET /v1/documents?…` con `pageSize=500` → renderiza KPIs/chart/tabla.
4. Click "Exportar CSV" → llama `downloadCsv` → toast.
5. Cambiar fecha manual → reset `activePreset=''`.
6. Cambiar preset → recalcula `dateFrom`/`dateTo`, NO refetch hasta click "Filtrar".

### Edge cases / gotchas del backend
- **`pageSize=500` propuesto por la UI** pero el controller lo recorta a 100 (`DocumentsController.cs:85`). Esto significa que reportes con >100 comprobantes en el periodo **están truncados silenciosamente**. La UI ni siquiera muestra advertencia ni hace paginación. **FLAG crítico para reportes/exports**.
- `kpis.totalGravada` en tfoot solo cuenta `accepted`, pero la tabla muestra todos los estados — el subtotal de la base imponible NO cuadra con la suma visual de las celdas "Base imponible" porque incluye rechazados/borradores en la columna pero no en el footer.
- Estados como `signed`/`sent`/`observed` no se agrupan en ningún KPI — se ven en la tabla pero no en cards.
- El CSV exporta `d.status` en raw (inglés). Si se abrirá en Excel para usuarios finales, podría ser más amigable traducirlo. **FLAG menor**.
- No hay filtro por tipo de comprobante ni por cliente — solo por fecha.
- `issueDate` viene como string `YYYY-MM-DD`; la UI hace `new Date(doc.issueDate + 'T00:00:00')` para mostrar en formato local — esto previene off-by-one por timezone.

### Navegación adyacente
- Sin links salientes. Punto terminal de análisis.

---

## Group C — Observaciones transversales

Patrones detectados a lo largo de Catálogo / Series / Reports / Dashboard / Welcome.

### Patrones de arquitectura
- **CRUD simple sin Service layer**: `ProductsController`, `CustomersController`, `CatalogsController` hablan **directo con `AppDbContext`**. Solo `Dashboard` y `Series` usan service/repo. Si Group A/B introdujo capas más estrictas, este grupo es la zona "plana" del codebase. Buen patrón para CRUD trivial, mala señal si se necesita lógica de negocio (ej. soft-delete, eventos, auditoría).
- **PaginatedResponse no estandarizado**: Productos/Customers devuelven `{ data, pagination }` armado inline por cada controller. No hay `PaginatedResponse<T>` reutilizable en `Application/DTOs`. Documents sí tiene una forma propia (`items` o `data`). FLAG: convergencia de contrato pendiente.
- **No hay validadores FluentValidation para Group C** (`Application/Validation/` solo tiene `DespatchAdviceValidator` y `RecurringInvoiceValidator`). Toda la validación de input cae en el controller (chequeo de unicidad y rol) o en EF (longitudes via SaveChanges). Esto deja huecos como: tipos de doc inválidos aceptados, IGV codes fuera del catálogo, monedas arbitrarias, etc.
- **Multi-tenancy manual**: cada query filtra `c.TenantId == _tenantProvider.GetCurrentTenantId()`. No hay global query filter de EF. Funciona, pero un olvido = leak entre tenants. Ya hay constancia de que todos los endpoints auditados lo respetan, pero es responsabilidad por convención.
- **`[Authorize]` a nivel controller + roles por acción**: `admin` solo elimina, `admin,emisor` crea/edita, cualquier rol autenticado lista. Salvo `Series` donde tanto Create como Update requieren `admin`.

### Patrones de pantalla
- **CRUD pages (products, customers)** comparten patrón idéntico: page header con conteo, dialog modal de create/edit, debounced search 300ms, tabla con badges/iconos, empty states diferenciados (sin data vs sin resultados de búsqueda), paginación inferior.
- **`PillGroup` interno**: tanto `/products` como `/customers` como `/series` reimplementan **su propio** `PillGroup` local (no es shared component). Tres implementaciones casi idénticas. Candidato a extraer a `@/components/ui/pill-group` siguiendo DESIGN-CLIENT.
- **Auto-bridge price**: el patrón de `useRef<'net'|'gross'>` para evitar feedback loops en `useEffect` es exclusivo de products (no se usa en quotations / documents según lo auditado aquí).
- **Lookup integration**: solo customers la usa hoy. El patrón "`api.get('/v1/services/{x}/status')` → habilitar/deshabilitar botón de lookup" se puede generalizar para futuras integraciones (ExchangeRate, etc.).
- **Estados internos serializados en string**: `status`, `igvType`, `docType`, `documentType` son todos strings sin enum en backend (excepto los static class helpers). Decisión deliberada para no acoplar a esquema SUNAT. Las constantes están en `Domain/Enums/*.cs` como `public const string`.

### Patrones de datos
- Todas las entities de este grupo usan `Guid` con `gen_random_uuid()` server-side.
- Soft-delete vía `IsActive`: presente en Product, Customer, Series, SunatCatalog, SunatCatalogCode, DetractionCode. Pero los endpoints **DELETE hacen Remove**, no toggle. Inconsistencia: el flag IsActive existe pero hard delete sigue siendo la operación expuesta.
- `CreatedAt` / `UpdatedAt` en todas las entities editables (no en SunatCatalog/SunatCatalogCode/DetractionCode/Series — Series solo tiene CreatedAt).
- Configuraciones aplican `HasDefaultValueSql("now()")` y `HasDefaultValueSql("gen_random_uuid()")` consistente.

### Carencias y FLAGs a llevarse al DESIGN.md global
1. **Catálogos no validados runtime**: `igvType`, `docType` (customer), `documentType` (series), `currency`, `unitMeasure` aceptan cualquier string en backend. Es UX/UI quien filtra. Refactor sugerido: middleware o atributo `[SunatCatalogValue("07")]` que valida contra la DB.
2. **`PageSize` clamp silencioso a 100 en `/v1/documents`** rompe `/reports` con >100 docs en el periodo.
3. **`MonthlySales` DTO incoherente** con el uso TS del dashboard (`m.month` vs `m.year` numérico). Validar serialización.
4. **`Dashboard.byType`/`byStatus`** son histórico total, no del periodo del KPI — fácil de malinterpretar visualmente.
5. **`/welcome` sin auto-redirect** y con check de `certificate` hardcoded a `false`.
6. **`/series` no implementa UI de Update** (endpoint existe).
7. **`/customers` no expone `notes`, `ubigeo`, `departamento`, `provincia`, `distrito`** — la API los tiene, la UI los ignora. El lookup RUC los devuelve y se pierden.
8. **`/products` no expone filtros `category` ni `isActive`** del API.
9. **Hard delete en todas las pantallas CRUD** mezcla con `IsActive` flag — patrón ambiguo.
10. **`/reports` CSV exporta `status` raw en inglés** — UX inconsistente con el resto de la UI es-PE.

# Group D — Integración, Administración, Configuración

Auditoría del backend TukiFact para las pantallas del grupo D. Repo root: `/Users/soulkin/Documents/TukiFact`.
Todas las rutas son **relativas** al repo root. Números de línea verificados sobre el árbol actual.

---

## /api-keys — API Keys (Integración para desarrolladores)

### Propósito
Permite al administrador del tenant generar, listar y revocar API Keys utilizadas por integraciones externas (SDK, ERP, scripts) para autenticarse contra la API REST de TukiFact con un prefijo público y un secreto opaco hashado en servidor.

### Endpoints REST consumidos
- `GET /v1/api-keys` — lista todas las API Keys del tenant (sin exponer el secreto, solo el prefijo).
- `POST /v1/api-keys` — genera una nueva API Key; **el secreto en texto plano viene en la respuesta y solo se muestra una vez**.
- `DELETE /v1/api-keys/{id}` — revoca (soft) marcando `IsActive = false`.

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/ApiKeysController.cs:14`
- Service (si existe): no tiene service dedicado — lógica de generación inline en el controller (`ApiKeysController.cs:42-54`)
- Entity: `src/TukiFact.Domain/Entities/ApiKey.cs:3`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/ApiKeyRepository.cs:7` (interfaz `src/TukiFact.Application/Interfaces/IApiKeyRepository.cs:5`)
- Validator (si existe): no tiene validador — FLAG (no hay FluentValidation; `request.Name` y `request.Permissions` se aceptan sin verificación de longitud ni catálogo)
- DTOs: `src/TukiFact.Application/DTOs/ApiKeys/CreateApiKeyRequest.cs:3` (`CreateApiKeyRequest`, `ApiKeyResponse`)

### Campos del backend — Request Create
| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `Name` | `string` | Sí (no enforced) | no enforced — controller no verifica null/empty | Nombre legible para identificar la integración ("Mi ERP", "Webhook QA") | `Input` text, autofocus, requerido del lado cliente |
| `Permissions` | `string[]` | Sí (no enforced) | no enforced — se serializa a JSONB tal cual lo manda el cliente | Scopes lógicos. UI usa `emit`, `query`, `void`; backend acepta cualquier string | `PillGroup` con tres chips: Emitir / Consultar / Anular |

### Campos del backend — Request Update
Update no implementado. Solo existen Create y Revoke (DELETE = soft revoke).

### Campos del backend — Response
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `Id` | `Guid` | No | Identificador interno de la API Key |
| `KeyPrefix` | `string` | No | Prefijo público (11 chars: `tk_` + 8 hex). Visible siempre para identificar la key sin exponer el secreto |
| `Name` | `string?` | Sí | Nombre que dio el admin |
| `Permissions` | `string[]` | No | Scopes (deserializado de JSONB) |
| `IsActive` | `bool` | No | `false` indica que fue revocada |
| `LastUsedAt` | `DateTimeOffset?` | Sí | Última vez que se usó la key contra la API |
| `CreatedAt` | `DateTimeOffset` | No | Fecha de creación |
| `PlainTextKey` | `string?` | Sí | **Solo presente en la respuesta de Create**. Formato `tk_<48 hex>` (`ApiKeysController.cs:43`) |

### Enums / Catálogos SUNAT relevantes
No aplica directamente — los scopes son una lista interna definida solo en UI (`emit`, `query`, `void`). El backend acepta strings arbitrarios → FLAG (falta catálogo de scopes en `src/TukiFact.Domain/Enums/`).

### Servicios externos invocados
- Stripe / billing provider: NO
- Email: NO
- MinIO: NO
- Crypto / signing test: SÍ — `RandomNumberGenerator.GetBytes(24)` para el secreto y `SHA256.HashData` para el hash que se persiste (`ApiKeysController.cs:43-44`)
- Audit log writer: SÍ — `AuditMiddleware` mapea `POST /api-keys` → `apikey.generated` (`src/TukiFact.Api/Middleware/AuditMiddleware.cs:63`); `DELETE` → `delete.deleted` (sin mapeo específico)
- Webhook delivery worker: NO

### Validación oficial del backend
- `[Authorize(Roles = "admin")]` a nivel controller (`ApiKeysController.cs:13`).
- `_tenantProvider.GetCurrentTenantId()` asegura aislamiento por tenant (`ApiKeysController.cs:28, 40`).
- Generación de secreto con CSRNG (`RandomNumberGenerator.GetBytes(24)`) → no enforced del lado HTTP, pero garantizado por código.
- Hash SHA-256 hex lower del raw key, persistido en `KeyHash` (`ApiKeys.KeyHash` nunca expuesto en respuestas).
- `KeyPrefix` calculado como los primeros 11 chars del raw key (`tk_` + 8 hex).
- Validación de `Name` no vacío: no enforced.
- Validación de `Permissions` contra catálogo: no enforced.
- Tamaño máximo de `Permissions[]`: no enforced.
- Revoke es soft (`IsActive = false`), no borra el registro — permite auditar quién usó qué key.

### UI propuesto siguiendo DESIGN-CLIENT
- **Page header**: "API Keys" + subtítulo "Gestioná las claves de integración a la API REST". Botón primario `Generar API Key`.
- **Toolbar**: filtro por estado (`Activas` / `Revocadas` / `Todas`) y búsqueda por nombre o prefijo.
- **Tabla**: columnas `Prefijo`, `Nombre`, `Permisos`, `Estado`, `Último uso`, `Creada`, acciones.
- **PillGroup** para permisos (Emitir, Consultar, Anular) usando colores del sistema (verde/azul/ámbar).
- **Sensible defaults**: scope `query` preseleccionado.
- **Smart helpers**: botón "Copiar prefijo" inline; en el modal de creación, mostrar inline el formato esperado del key (`tk_…`).
- **Empty state**: "Aún no generaste ninguna API Key" + CTA + link a docs (cuando exista `/developers`).
- **Destructive confirms**: `AlertDialog` para revoke con texto "Revocar la API Key '{name}' ({prefix}…)? Las integraciones que la usan dejarán de funcionar."
- **Reveal modal** (post-create): banner ámbar "Guardá esta clave ahora. Por seguridad no podremos mostrártela de nuevo." + botón copiar al portapapeles + botón "Entendido".
- **ARIA**: `role="dialog"` en modal, `aria-live="polite"` para el banner de reveal, foco en el botón de copiar al abrir.

### Estados / flujo
1. Admin abre el modal y completa nombre + scopes.
2. `POST /v1/api-keys` crea el registro (hash persistido) y devuelve `PlainTextKey` una sola vez.
3. UI muestra modal de reveal; al cerrar, se descarta del estado local.
4. Listado se refresca; el secreto **no** vuelve a estar disponible en ningún endpoint.
5. Cualquier llamada futura a la API REST con esa key actualiza `LastUsedAt` (responsabilidad del middleware de autenticación API-key, fuera de scope).
6. Revoke marca `IsActive = false`; la key queda visible en la tabla con badge "Revocada" pero deja de validar.

### Edge cases / gotchas del backend
- **Permisos**: solo `admin` (controller-level `[Authorize(Roles = "admin")]`).
- **Auditoría**: el create queda como `apikey.generated` en `AuditLog`; el revoke queda como `delete.deleted` (mapping genérico, FLAG: el `AuditMiddleware` no tiene branch específico para `DELETE /api-keys`).
- **Idempotencia**: no aplica — cada POST genera una key nueva, no hay `Idempotency-Key` header.
- **Secret rotation**: no existe endpoint dedicado. El flujo es revocar + crear una nueva (no hay reuso del mismo `KeyPrefix`).
- **Plan limits enforcement**: no enforced — no hay límite de API Keys por plan.
- **El plaintext key viaja en el body** de la respuesta de create — el cliente debe estar sobre TLS y no loguear el body.
- **Falta validar longitud máxima de `Name`**: el modelo no la fija (ver `ApiKey.Name` en `src/TukiFact.Domain/Entities/ApiKey.cs:9`).
- **`LastUsedAt` no se actualiza desde este controller** — depende del middleware que valida la key en runtime.

### Navegación adyacente
- `/webhooks` — complemento natural para integraciones.
- `/audit-log` — para ver qué admin generó/revocó cada key.
- `/developers` (área pública en `app/developers/`) — documentación de la API.

---

## /webhooks — Webhooks (Notificaciones push)

### Propósito
Permite al administrador suscribirse a eventos del tenant (creación, aceptación, rechazo, anulación de documentos) entregándolos por HTTP POST firmado con HMAC-SHA256 al endpoint configurado, con reintentos y registro de cada delivery.

### Endpoints REST consumidos
- `GET /v1/webhooks` — lista configuraciones del tenant.
- `POST /v1/webhooks` — crea una configuración; **el `Secret` HMAC se devuelve una sola vez**.
- `PUT /v1/webhooks/{id}` — actualiza URL / events / IsActive / MaxRetries.
- `DELETE /v1/webhooks/{id}` — borrado **duro** (no soft, ver gotchas).
- `GET /v1/webhooks/{id}/deliveries` — últimas 50 entregas (`WebhooksController.cs:87`).

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/WebhooksController.cs:15`
- Service: `src/TukiFact.Infrastructure/Services/WebhookDeliveryService.cs:11` (worker que firma y entrega los eventos)
- Entity: `src/TukiFact.Domain/Entities/WebhookConfig.cs:3` y `src/TukiFact.Domain/Entities/WebhookDelivery.cs:3`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/WebhookRepository.cs:8` y `src/TukiFact.Infrastructure/Persistence/Repositories/WebhookDeliveryRepository.cs:7`
- Validator: no tiene validador — FLAG (no se valida formato URL, no se valida que los events estén en el catálogo `document.*`)
- DTOs: `src/TukiFact.Application/DTOs/Webhooks/WebhookDto.cs:3`

### Campos del backend — Request Create
| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `Url` | `string` | Sí (no enforced) | no enforced — no hay validación de URL bien formada ni de scheme `https` | Endpoint donde se POSTea el payload | `Input` tipo URL con `pattern="https?://.+"` cliente |
| `Events` | `string[]` | Sí (no enforced) | no enforced — se serializa a JSON tal cual | Lista de eventos a suscribir | `PillGroup` multi-select sobre catálogo conocido |
| `MaxRetries` | `int` | No (default 3 en DTO record) | no enforced — admite cualquier int, incluyendo negativos | Reintentos antes de marcar `failed` | `NumberInput` con min=0 max=10 client-side |

### Campos del backend — Request Update
| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `Url` | `string?` | No | no enforced | Patch parcial: solo se actualiza si no es null (`WebhooksController.cs:68`) | `Input` URL |
| `Events` | `string[]?` | No | no enforced | Reemplaza la lista completa | `PillGroup` |
| `IsActive` | `bool?` | No | no enforced | Pausar entregas sin eliminar | `Switch` |
| `MaxRetries` | `int?` | No | no enforced | Cambiar política de reintentos | `NumberInput` |

### Campos del backend — Response
**`WebhookConfigResponse`** (`WebhookDto.cs:6`):
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `Id` | `Guid` | No | Identificador |
| `Url` | `string` | No | Endpoint suscrito |
| `Events` | `string[]` | No | Eventos suscritos |
| `IsActive` | `bool` | No | Si entrega o no |
| `MaxRetries` | `int` | No | Política de reintentos |
| `LastTriggeredAt` | `DateTimeOffset?` | Sí | Último trigger exitoso |
| `CreatedAt` | `DateTimeOffset` | No | |

**Respuesta de Create** (objeto anónimo, `WebhooksController.cs:54`) incluye además `Secret: string` (hex de 64 chars) **solo en esa única respuesta**.

**`WebhookDeliveryResponse`** (`WebhookDto.cs:11`):
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `Id` | `Guid` | No | Id del delivery |
| `EventType` | `string` | No | Evento entregado |
| `Status` | `string` | No | `pending` / `delivered` / `failed` (`WebhookConfig.cs` — comentario inline) |
| `Attempt` | `int` | No | Número de intento (1..MaxRetries) |
| `ResponseStatus` | `string?` | Sí | HTTP status del receptor |
| `CreatedAt` | `DateTimeOffset` | No | Cuándo se intentó |

### Enums / Catálogos SUNAT relevantes
No aplica SUNAT. Catálogo interno de eventos (documentado en `WebhookConfig.cs:9`):
- `document.created`
- `document.accepted`
- `document.rejected`
- `document.voided`

(Catálogo definido como comentario, **no como enum** — FLAG.)

### Servicios externos invocados
- Stripe / billing provider: NO
- Email: NO
- MinIO: NO
- Crypto / signing test: SÍ — `HMACSHA256` para firmar el body (`WebhookDeliveryService.cs:99-105`); `RandomNumberGenerator.GetBytes(32)` para el secret (`WebhooksController.cs:42`)
- Audit log writer: SÍ — `webhook.created` para POST, `delete.deleted` para DELETE, `put.updated` para PUT (`AuditMiddleware.cs:65-67`)
- Webhook delivery worker: SÍ — `WebhookDeliveryService.DeliverEventAsync` se dispara desde donde se publican los eventos de dominio (worker corre fire-and-forget en `Task.Run` con backoff exponencial 2^attempt segundos)

### Validación oficial del backend
- `[Authorize(Roles = "admin")]` a nivel controller (`WebhooksController.cs:14`).
- Aislamiento por tenant en GET/POST; **gotcha**: `PUT` y `DELETE` **no filtran por tenant** (`WebhooksController.cs:65-66, 80`) → cualquier admin podría editar un webhook de otro tenant si conoce el GUID (FLAG, ver edge cases).
- URL bien formada: no enforced.
- Scheme `https` obligatorio: no enforced.
- Eventos en catálogo: no enforced.
- `MaxRetries >= 0`: no enforced.
- Generación de secret HMAC con CSRNG: aplicada en código.

### UI propuesto siguiendo DESIGN-CLIENT
- **Page header**: "Webhooks" + subtítulo "Recibí notificaciones HTTP cuando ocurren eventos en tu cuenta". Botón primario `Nuevo Webhook`.
- **Toolbar**: filtro por evento, switch "Solo activos".
- **Tabla**: `URL`, `Eventos` (pills), `Estado`, `Reintentos`, `Último trigger`, acciones (`Ver entregas`, `Editar`, `Eliminar`).
- **Side panel / Drawer** al hacer click en una fila: muestra deliveries (timeline con badge de status code, request id, timestamp). Botón "Reintentar manualmente" (FLAG: endpoint no existe, dejarlo como TODO).
- **PillGroup** para eventos con colores semánticos: `created` (azul), `accepted` (verde), `rejected` (rojo), `voided` (gris).
- **Sensible defaults**: en create, preseleccionar `document.accepted` + `document.rejected`; `MaxRetries=3`.
- **Smart helpers**:
  - botón "Test delivery" en create (FLAG: endpoint no existe);
  - mostrar inline ejemplo del header `X-TukiFact-Signature: sha256=<hex>` y del `X-TukiFact-Event` (`WebhookDeliveryService.cs:62-64`);
  - link a docs de verificación HMAC.
- **Empty state**: "Aún no configuraste webhooks" + CTA.
- **Destructive confirms**: `AlertDialog` para delete: "Eliminar el webhook a {url}? Se borrará el historial de entregas y no podrás recuperarlo."
- **Reveal modal** del `Secret` post-create (mismo patrón que API Keys).
- **ARIA**: `aria-live="polite"` para el banner del secret; tabla de deliveries con `<caption>` accesible.

### Estados / flujo
1. Admin crea webhook → backend genera `Secret` (32 bytes hex) y lo devuelve una sola vez.
2. Cuando ocurre un evento de dominio (e.g. documento aceptado por SUNAT), un publisher llama `WebhookDeliveryService.DeliverEventAsync(tenantId, eventType, payload)`.
3. Por cada `WebhookConfig` activo suscrito al evento (`WebhookRepository.GetActiveByEventAsync`), se crea un `WebhookDelivery` en estado `pending`.
4. `Task.Run` envía el POST con headers `X-TukiFact-Event`, `X-TukiFact-Signature` (HMAC), `X-TukiFact-Delivery` (id), reintenta hasta `MaxRetries` con backoff `2^attempt` segundos (`WebhookDeliveryService.cs:93`).
5. En éxito actualiza `WebhookConfig.LastTriggeredAt`. En fallo definitivo deja `Status = failed`.
6. UI consulta `GET /v1/webhooks/{id}/deliveries` para mostrar las últimas 50 entregas.

### Edge cases / gotchas del backend
- **Permisos**: solo `admin`.
- **Auditoría**: registra create/update/delete vía middleware. No registra entregas individuales (eso queda en `WebhookDelivery`).
- **Idempotencia**: el receptor debe deduplicar por `X-TukiFact-Delivery` (no enforced en backend; documentar en UI/SDK).
- **Secret rotation**: no implementado. La única forma es eliminar y crear de nuevo → pierde `LastTriggeredAt` e historial de deliveries (cascade delete probablemente).
- **PUT / DELETE no filtran por TenantId** — FLAG de seguridad cross-tenant.
- **Delivery worker corre fire-and-forget** con `Task.Run`; **no persiste el queue** → si el proceso se reinicia mid-retry, el delivery queda en estado `pending` o `failed` sin reintento (`WebhookDeliveryService.cs:49`).
- **Backoff `2^attempt`** crece rápido (2s, 4s, 8s, 16s…) — adecuado para 3 retries, no para 10.
- **`MaxRetries=0`** dejaría la entrega sin intentos (loop nunca corre).
- **Payload se trunca a 1000 chars** en `ResponseBody` (`WebhookDeliveryService.cs:70`).
- **No hay paginación** en `/deliveries` — devuelve hardcoded 50 (`WebhooksController.cs:87`).

### Navegación adyacente
- `/api-keys` (integraciones).
- `/audit-log` (quién creó/modificó cada webhook).
- `/documents` (origen de los eventos `document.*`).

---

## /ai — Copiloto IA (Asistente conversacional)

### Propósito
Chat conversacional que proxea consultas del usuario a un proveedor de IA configurado por el propio tenant (Gemini, Claude, Grok, DeepSeek u OpenAI) con un system prompt experto en facturación electrónica SUNAT. TukiFact **no cobra por IA** — cada tenant trae su propia API key.

### Endpoints REST consumidos
- `GET /v1/services/ai/status` — devuelve `configured`, `provider`, `model` para el widget.
- `POST /v1/services/ai/chat` — envía un mensaje al proveedor configurado y devuelve la respuesta sintetizada.
- (Soporte desde Settings) `POST /v1/services/ai/test` — admin only, prueba la API key contra todos los modelos del proveedor.
- (Soporte desde Settings) `GET /v1/services/providers` — catálogo público de proveedores + modelos.

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/ExternalServicesController.cs:20`
- Service (si existe): no tiene service dedicado — implementación inline en el controller (`CallAiProvider` en `ExternalServicesController.cs:455`)
- Entity: `src/TukiFact.Domain/Entities/TenantServiceConfig.cs:11` (almacena `AiProvider`, `AiApiKey`, `AiModel`)
- Repository: no tiene repository dedicado — acceso directo a `AppDbContext.TenantServiceConfigs` desde el controller
- Validator: no tiene validador — FLAG (proveedor sí se valida contra whitelist en `ExternalServicesController.cs:100`, modelo se acepta sin verificar contra el catálogo)
- DTOs: `UpdateServiceConfigRequest` y `AiChatRequest` definidos como records en `ExternalServicesController.cs:597, 605`

### Campos del backend — Request Create
Configuración del proveedor se hace por `PUT /v1/services/config` (cubierto en `/settings`). Para la pantalla de chat:

**`AiChatRequest`**:
| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `Message` | `string` | Sí (no enforced — no se valida vacío) | no enforced | Pregunta del usuario en lenguaje natural | `Textarea` con `Enter` para enviar, `Shift+Enter` para nueva línea |

### Campos del backend — Request Update
No aplica para `/ai` — la pantalla es un chat efímero, no persiste mensajes.

### Campos del backend — Response
**`POST /v1/services/ai/chat`** (objeto anónimo, `ExternalServicesController.cs:233`):
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `response` | `string` | No | Texto generado por el modelo (extraído según el formato del proveedor) |
| `provider` | `string` | No | Proveedor que respondió (`gemini`, `claude`, …) |
| `model` | `string?` | Sí | Modelo concreto utilizado |

**`GET /v1/services/ai/status`** (`ExternalServicesController.cs:312`):
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `configured` | `bool` | No | Si hay proveedor + key configurada |
| `provider` | `string` | No | "none" si no está configurado |
| `model` | `string?` | Sí | Modelo activo |

### Enums / Catálogos SUNAT relevantes
No aplica SUNAT. Catálogos internos hardcoded en `ExternalServicesController.cs`:
- **Proveedores válidos** (`:100`): `none`, `gemini`, `claude`, `grok`, `deepseek`, `openai`.
- **Modelos por proveedor** (`:138-161`):
  - `gemini`: `gemini-3.1-pro-preview`, `gemini-3-flash`, `gemini-2.5-pro`, `gemini-2.5-flash`
  - `claude`: `claude-sonnet-4-6`, `claude-opus-4-6`, `claude-sonnet-4-5-20250929`, `claude-opus-4-5-20251101`, `claude-haiku-4-5-20251001`, `claude-sonnet-4-20250514`
  - `grok`: `grok-4.20`, `grok-4.1`, `grok-3`, `grok-3-mini`
  - `deepseek`: `deepseek-chat`, `deepseek-reasoner`
  - `openai`: `gpt-5.3-codex`, `gpt-5.2`, `gpt-5`, `gpt-5-mini`, `gpt-4.1`, `gpt-4.1-mini`, `o3`, `o3-mini`

### Servicios externos invocados
- Stripe / billing provider: NO
- Email: NO
- MinIO: NO
- Crypto / signing test: NO
- Audit log writer: SÍ — `AuditMiddleware` audita `POST /services/ai/chat` como `post.action` con entityType `Unknown` (mapping genérico, FLAG)
- Webhook delivery worker: NO
- **Proveedores LLM externos** (HTTP outbound):
  - `https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent` (Gemini)
  - `https://api.anthropic.com/v1/messages` (Claude, headers `x-api-key` + `anthropic-version: 2023-06-01`)
  - `https://api.x.ai/v1/chat/completions` (Grok)
  - `https://api.deepseek.com/chat/completions` (DeepSeek)
  - `https://api.openai.com/v1/chat/completions` (OpenAI)
  - Timeout 30s por request (`ExternalServicesController.cs:458`)

### Validación oficial del backend
- `[Authorize]` a nivel controller (`ExternalServicesController.cs:19`) — cualquier usuario autenticado puede chatear (no solo admin).
- Para `POST /v1/services/ai/test`: `[Authorize(Roles = "admin")]` (`ExternalServicesController.cs:244`).
- Requiere `TenantServiceConfig` con `AiProvider != "none"` y `AiApiKey` no vacío; si no, devuelve `BadRequest` con mensaje en español.
- System prompt fijo (`ExternalServicesController.cs:460`): "Eres un asistente experto en facturación electrónica peruana (SUNAT)…".
- `max_tokens=1024` solo en Claude (`:486`); el resto no fija límite explícito → potencial gasto del lado del tenant.
- No persiste historial de mensajes ni hace logging del contenido (solo log de errores del proveedor).

### UI propuesto siguiendo DESIGN-CLIENT
- **Page header**: "Copiloto IA" + chip con `provider/model` activo (badge verde si `configured`, gris si no). Botón `Configurar IA` enlazando a `/settings`.
- **Chat container** full-height con scroll automático al final.
- **Mensajes**: avatar + bubble. Asistente alineado a la izquierda con badge `provider/model` en respuestas.
- **Sensible defaults**: mensaje de bienvenida con 3 sugerencias chip ("¿Cómo emitir una factura?", "¿Tipos de IGV?", "¿Cómo anular un comprobante?").
- **Smart helpers**:
  - si `configured=false`, mostrar un `EmptyState` central con CTA "Configurá tu proveedor de IA";
  - persistir la conversación solo en estado local (no en backend);
  - botón "Reiniciar conversación".
- **Empty state**: mensaje del asistente como onboarding.
- **Destructive confirms**: no aplica (sin persistencia).
- **ARIA**: `aria-live="polite"` en el contenedor de mensajes para anunciar nuevas respuestas; `role="log"`; input con `aria-label="Pregunta sobre facturación electrónica"`.

### Estados / flujo
1. Pantalla carga `GET /v1/services/ai/status`. Si `configured=false`, muestra CTA a `/settings`.
2. Usuario escribe y envía → `POST /v1/services/ai/chat { Message }`.
3. Backend lee `TenantServiceConfig`, arma el body según el proveedor, envía con `HttpClient` (timeout 30s).
4. Extrae el texto de la respuesta con `ExtractAiResponse` (formato por proveedor: `candidates[0].content.parts[0].text` para Gemini, `content[0].text` para Claude, `choices[0].message.content` para los OpenAI-compatible).
5. Devuelve `{ response, provider, model }` → UI renderiza con badge.
6. Errores del proveedor → 502 con `error` en español.

### Edge cases / gotchas del backend
- **Permisos**: chat es para cualquier usuario autenticado (no solo admin); el `test` sí es admin-only.
- **Auditoría**: queda como `post.action` genérico (FLAG: el `AuditMiddleware` no mapea `/services/ai/chat` a una acción específica).
- **Idempotencia**: no aplica.
- **Secret rotation**: la `AiApiKey` se guarda **sin cifrar** (TODO comentario en `:107`) — FLAG crítico de seguridad. `TenantServiceConfig.AiApiKey` se persiste tal cual.
- **Plan limits enforcement**: no hay rate-limit aplicado por TukiFact; el tenant paga directamente al proveedor.
- **El RUC del cliente no se envía al proveedor** (sin contexto del tenant en el prompt) — al subir contexto luego, considerar PII.
- **Timeout fijo 30s** — algunos modelos `reasoner`/`o3` pueden tardar más.
- **No hay streaming** — respuesta llega entera al final.
- **`/ai/test` no expone el resultado completo** — trunca a 50 chars en éxito y 100 en error.

### Navegación adyacente
- `/settings` (configurar proveedor + API key).
- Widget flotante en otras pantallas (que también consume `/v1/services/ai/status` y `/v1/services/ai/chat`).

---

## /users — Usuarios del tenant

### Propósito
Gestión de usuarios pertenecientes al tenant: crear con email + contraseña, asignar rol (`admin`, `emisor`, `consulta`), activar/desactivar y editar datos. Es admin-only.

### Endpoints REST consumidos
- `GET /v1/users` — lista usuarios del tenant.
- `POST /v1/users` — crea usuario con password hasheada.
- `PUT /v1/users/{id}` — actualiza `FullName`, `Role`, `IsActive` (parcial).
- `DELETE /v1/users/{id}` — **soft delete** (marca `IsActive = false`, no borra el registro).

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/UsersController.cs:14`
- Service (si existe): no tiene service dedicado — el hashing de password se delega en `IPasswordHasher` (`src/TukiFact.Application/Interfaces/IPasswordHasher.cs`)
- Entity: `src/TukiFact.Domain/Entities/User.cs:3`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/UserRepository.cs:7` (interfaz `src/TukiFact.Application/Interfaces/IUserRepository.cs:5`)
- Validator: no tiene validador (FluentValidation) — validación inline en controller con `UserRole.IsValid` (`UsersController.cs:42`)
- Enum de roles: `src/TukiFact.Domain/Enums/UserRole.cs:3`
- DTOs: `src/TukiFact.Application/DTOs/Users/UserDto.cs:3`

### Campos del backend — Request Create
| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `Email` | `string` | Sí (no enforced) | no enforced — no se valida formato email; sí se verifica unicidad por tenant (`UsersController.cs:45`) | Email del usuario, login key | `Input` type=email |
| `Password` | `string` | Sí (no enforced) | no enforced — sin política de complejidad ni longitud mínima | Se hashea con `IPasswordHasher.Hash` | `Input` type=password con indicador de fortaleza (cliente) |
| `FullName` | `string` | No (DTO no lo marca nullable pero entity sí) | no enforced | Nombre legible | `Input` text |
| `Role` | `string` | No (default `"emisor"` en DTO) | enforced — `UserRole.IsValid` contra `[admin, emisor, consulta]` | Rol que define `[Authorize(Roles = …)]` en otros controllers | `Select` con 3 opciones |

### Campos del backend — Request Update
| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `FullName` | `string?` | No | no enforced | Patch parcial | `Input` |
| `Role` | `string?` | No | enforced — `UserRole.IsValid` (`UsersController.cs:73`) | Rol nuevo | `Select` |
| `IsActive` | `bool?` | No | no enforced | Activar / desactivar sin borrar | `Switch` |

**Update no implementa cambio de password.** Para eso existirá flujo de "forgot password" en `/auth` (fuera de scope).

### Campos del backend — Response
**`UserResponse`** (`UserDto.cs:16`):
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `Id` | `Guid` | No | Identificador |
| `Email` | `string` | No | |
| `FullName` | `string?` | Sí | |
| `Role` | `string` | No | Uno de `admin/emisor/consulta` |
| `IsActive` | `bool` | No | |
| `LastLoginAt` | `DateTimeOffset?` | Sí | Última vez que hizo login |
| `CreatedAt` | `DateTimeOffset` | No | |

### Enums / Catálogos SUNAT relevantes
No aplica SUNAT. Catálogo de roles definido en `src/TukiFact.Domain/Enums/UserRole.cs:3`:
- `admin` — acceso completo (todos los controllers admin-only).
- `emisor` — emisión y consulta de documentos.
- `consulta` — solo lectura.

No hay catálogo de permisos granular (scopes) — el control es solo por rol.

### Servicios externos invocados
- Stripe / billing provider: NO
- Email: NO — **no hay invitación por email** (FLAG, el admin debe comunicar la password manualmente)
- MinIO: NO
- Crypto / signing test: NO
- Audit log writer: SÍ — `user.created` para POST (`AuditMiddleware.cs:62`), `put.updated` para PUT, `delete.deleted` para DELETE
- Webhook delivery worker: NO

### Validación oficial del backend
- `[Authorize(Roles = "admin")]` a nivel controller (`UsersController.cs:13`).
- `_tenantProvider.GetCurrentTenantId()` aísla por tenant en GET/POST (`UsersController.cs:30, 40`).
- **`PUT` y `DELETE` no filtran por TenantId** (`UsersController.cs:67, 86`) → mismo problema cross-tenant que Webhooks → FLAG.
- Rol válido enforced en POST y PUT.
- Unicidad de email por tenant enforced en POST (`UsersController.cs:45-47`) → 409 Conflict.
- Política de password: **no enforced**.
- Email format: no enforced.
- Bloqueo de auto-desactivación del admin que invoca: no enforced (podría dejarse fuera del sistema a sí mismo).
- 2FA: no implementado.

### UI propuesto siguiendo DESIGN-CLIENT
- **Page header**: "Usuarios" + subtítulo "Gestioná los miembros de tu empresa". Botón primario `Crear Usuario`.
- **Toolbar**: filtro por rol (`PillGroup`: Todos / Admin / Emisor / Consulta), filtro por estado (Activos / Inactivos), search por email/nombre.
- **Tabla**: `Email`, `Nombre`, `Rol` (badge color por rol), `Estado`, `Último acceso`, `Creado`, acciones (editar rol, activar/desactivar, eliminar).
- **Sensible defaults**: en create, `Role=emisor` preseleccionado.
- **Smart helpers**:
  - badge "Vos" en la fila del usuario actual, deshabilitar destructive actions sobre sí mismo;
  - tooltip explicando qué permite cada rol;
  - generador de password seguro inline (con botón "Copiar").
- **Empty state**: "Solo estás vos en la empresa" + CTA crear.
- **Destructive confirms**: `AlertDialog` "Desactivar a {email}? Podrá ser reactivado luego sin perder su historial."
- **ARIA**: tabla con `<caption>`, badges con `aria-label` describiendo el rol completo.

### Estados / flujo
1. Admin abre modal `Crear Usuario`, llena email + password + nombre + rol.
2. `POST /v1/users` valida rol y unicidad, hashea password, crea registro.
3. El usuario nuevo recibe la password **out-of-band** (FLAG: no hay email).
4. Login posterior (en `/login`) actualiza `LastLoginAt` (responsabilidad de `AuthController`).
5. Admin puede editar rol o desactivar; el desactivado pierde acceso al próximo login (refresh token no invalidado automáticamente — verificar fuera de scope).

### Edge cases / gotchas del backend
- **Permisos**: solo `admin`.
- **Auditoría**: create / update / delete sí auditados; **login no se audita aquí** (lo audita el middleware sobre `POST /auth/login`).
- **Idempotencia**: no aplica.
- **Soft delete vs hard delete**: solo soft delete (`IsActive = false`). No hay endpoint de hard delete (`UsersController.cs:84-92`). Aceptable para historial pero implica que el email queda "ocupado" en el tenant.
- **2FA**: no implementado.
- **Password reset**: no implementado en este controller (delegado a `/auth/forgot-password`, fuera de scope).
- **PUT y DELETE no filtran TenantId** → FLAG cross-tenant.
- **Cambio de email**: no soportado (UpdateUserRequest no incluye `Email`).
- **Admin no puede crearse a sí mismo como `consulta` y bloquearse**: no enforced.
- **Sin verificación de email**: el email se da por válido sin confirmación.

### Navegación adyacente
- `/audit-log` (acciones de usuarios).
- `/settings` (datos generales).
- `/login` (flujo de login).

---

## /certificate — Certificado Digital SUNAT

### Propósito
Subir, validar y administrar el certificado digital `.pfx`/`.p12` (PKCS#12) requerido para firmar comprobantes electrónicos ante SUNAT; gestionar credenciales SOL y conmutar el entorno (`beta`/`production`).

> **Nota**: existen dos controllers que cubren responsabilidades casi idénticas (FLAG transversal). Esta pantalla del frontend consume `/v1/certificate/*`.

### Endpoints REST consumidos
- `GET /v1/certificate/status` — estado del certificado actual (hasCertificate, expiresAt, daysUntilExpiry, environment, hasSunatCredentials).
- `POST /v1/certificate/upload` — multipart con `file` + `password`. Acepta `.pfx` / `.p12`.
- `DELETE /v1/certificate` — elimina el certificado del tenant.
- `PUT /v1/certificate/sunat-credentials` — guarda `SunatUser` + `SunatPassword` (cifrada).
- `PUT /v1/certificate/environment` — cambia entre `beta` y `production`.

> El frontend de `/settings` consume las variantes `/v1/tenant/certificate` (`TenantController`), que aceptan adicionalmente `.pem`.

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/CertificateController.cs:14`
- Controller alternativo: `src/TukiFact.Api/Controllers/TenantController.cs:11` (endpoints `POST/DELETE /v1/tenant/certificate`, `PUT /v1/tenant/environment`)
- Service (si existe): no tiene service dedicado — validación inline con `X509CertificateLoader.LoadPkcs12` (`CertificateController.cs:82`)
- Entity: `src/TukiFact.Domain/Entities/Tenant.cs:3` (campos `CertificateData`, `CertificatePasswordEncrypted`, `CertificateExpiresAt`, `SunatUser`, `SunatPasswordEncrypted`, `Environment`)
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/TenantRepository.cs:7` (en `CertificateController` se usa `AppDbContext` directo)
- Validator: no tiene validador (FluentValidation) — validación inline en controller
- Cifrado de password: `ISecretProtector` (`src/TukiFact.Application/Interfaces/ISecretProtector.cs`)

### Campos del backend — Request Create
**`POST /v1/certificate/upload`** (multipart/form-data):
| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `file` | `IFormFile` | Sí | enforced — null/empty → 400 (`CertificateController.cs:57`); extensión `.pfx` o `.p12` (`:61`); tamaño máx 5 MB (`[RequestSizeLimit]` en `:51`) | Archivo PKCS#12 con clave privada | `<input type="file">` con `accept=".pfx,.p12"` |
| `password` | `string` | Sí (`[FromForm]`) | enforced — si no abre el .pfx → 400 "verificá la contraseña" (`:117`) | Contraseña del .pfx | `Input` type=password |

**Validaciones adicionales sobre el contenido**:
- enforced: `!cert.HasPrivateKey` → 400 (`:91`).
- enforced: `cert.NotAfter < UtcNow` → 400 "ya expiró" (`:94`).
- **warning (no enforced)**: si el RUC del tenant no aparece en `cert.Subject` → log Warning (`:104`) pero la subida procede.

**`PUT /v1/certificate/sunat-credentials`** (`SunatCredentialsRequest` en `:216`):
| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `SunatUser` | `string` | Sí (no enforced) | no enforced | Usuario SOL secundario | `Input` text |
| `SunatPassword` | `string` | Sí (no enforced) | no enforced | Clave SOL — se cifra con `ISecretProtector.Protect` antes de persistir | `Input` type=password |

**`PUT /v1/certificate/environment`** (`ChangeEnvironmentRequest` en `:217`):
| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `Environment` | `string` | Sí | enforced — debe ser `"beta"` o `"production"` (`:191`); para `"production"` además exige `CertificateData != null` y `SunatUser` no vacío (`:199-204`) | Conmuta servidor SUNAT | `RadioGroup` Beta / Producción |

### Campos del backend — Request Update
Update parcial conceptualmente cubierto por los PUT individuales (credenciales, environment).

### Campos del backend — Response
**`GET /v1/certificate/status`** (objeto anónimo, `CertificateController.cs:37`):
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `hasCertificate` | `bool` | No | `tenant.CertificateData != null` |
| `expiresAt` | `DateTimeOffset?` | Sí | `tenant.CertificateExpiresAt` |
| `isExpired` | `bool` | No | True si `expiresAt < UtcNow` |
| `daysUntilExpiry` | `int?` | Sí | Días restantes |
| `environment` | `string` | No | `"beta"` o `"production"` |
| `hasSunatCredentials` | `bool` | No | `tenant.SunatUser` no vacío |

**`POST /v1/certificate/upload`** (objeto anónimo, `:133`):
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `message` | `string` | No | "Certificado subido correctamente" |
| `subject` | `string` | No | Subject DN del cert (`cert.Subject`) |
| `issuer` | `string` | No | Issuer DN |
| `serialNumber` | `string` | No | Serial |
| `thumbprint` | `string` | No | SHA-1 thumbprint (hex) |
| `issuedAt` | `DateTimeOffset` | No | `NotBefore` |
| `expiresAt` | `DateTimeOffset` | No | `NotAfter` |
| `daysUntilExpiry` | `int` | No | |
| `warning` | `string?` | Sí | Mensaje si `daysUntilExpiry < 30` |
| `hasPrivateKey` | `bool` | No | Verificado en validación |
| `rucMatch` | `bool` | No | `subject.Contains(tenant.Ruc)` |

### Enums / Catálogos SUNAT relevantes
- **Tipo de archivo aceptado**: `.pfx`, `.p12` (PKCS#12) en `/v1/certificate`; en `/v1/tenant/certificate` también `.pem`.
- **Environment**: `"beta"` (homologación SUNAT) y `"production"` (envíos reales). Valor por defecto `"beta"` (`Tenant.cs:28`).
- **Tipo de certificado**: no se persiste explícitamente — se infiere por extensión.

### Servicios externos invocados
- Stripe / billing provider: NO
- Email: NO — falta notificación por proximidad de expiración (FLAG)
- MinIO: NO — el certificado se guarda como `bytea` en la fila `tenants` (no en bucket)
- Crypto / signing test: SÍ — `X509CertificateLoader.LoadPkcs12` para parsear y validar (`:82`); `ISecretProtector.Protect` para cifrar password (`:126`)
- Audit log writer: SÍ — `POST /certificate/upload` queda como `post.action` con entityType `Unknown` (mapping genérico — FLAG, no hay branch específico para certificado en `AuditMiddleware`)
- Webhook delivery worker: NO

### Validación oficial del backend
- `[Authorize(Roles = "admin")]` a nivel controller (`CertificateController.cs:13`).
- Tamaño máx 5 MB (`[RequestSizeLimit(5 * 1024 * 1024)]`).
- Extensión obligatoria `.pfx` o `.p12`.
- El archivo debe abrir con la password provista.
- Debe contener clave privada (`HasPrivateKey`).
- No debe estar expirado.
- Para cambiar a `production`: certificado + credenciales SUNAT obligatorios.
- RUC en subject: **warning only**, no bloqueante.
- No hay endpoint para regenerar / rotar — el flujo es DELETE + POST de nuevo.
- No se valida cadena de confianza ni issuer contra SUNAT.
- No se valida que `Environment != current` en el PUT (no es un error, es idempotente).

### UI propuesto siguiendo DESIGN-CLIENT
- **Page header**: "Certificado Digital & SUNAT" + subtítulo "Configurá tu certificado y entorno de emisión".
- **Status card** prominente:
  - badge verde "Vigente" / ámbar "Próximo a expirar" (<30 días) / rojo "Expirado";
  - countdown de días con barra de progreso;
  - botones secundarios `Eliminar`, `Reemplazar`.
- **Upload form** (cuando no hay cert): dropzone con `accept=".pfx,.p12"`, input de password con toggle visibility, botón `Subir Certificado`.
- **Credentials card**: input usuario SOL + clave SOL + badge "Configuradas" / "No configuradas".
- **Environment card**: `RadioGroup` Beta / Producción con descripción inline; deshabilitar Producción si faltan prerequisites con tooltip explicativo.
- **Sensible defaults**: si se sube cert nuevo y password viene vacía, mensaje claro.
- **Smart helpers**:
  - test inline post-upload mostrando `subject`, `issuer`, `serialNumber`, `validez`;
  - banner ámbar si `daysUntilExpiry < 30`;
  - banner rojo si `isExpired`;
  - tooltip "El RUC del subject debe coincidir con el tenant" (warning si no).
- **Empty state**: "Aún no subiste tu certificado digital. Sin él no podés emitir comprobantes a SUNAT."
- **Destructive confirms**:
  - `AlertDialog` para DELETE certificate: "Eliminar el certificado? No podrás emitir comprobantes hasta subir uno nuevo.";
  - confirmación al cambiar a Producción: "Vas a emitir comprobantes reales. Esta acción tiene efectos fiscales."
- **ARIA**: countdown con `aria-live="polite"`, status badges con `aria-label`.

### Estados / flujo
1. Admin sube `.pfx` + password.
2. Backend valida formato, abre el cert, verifica `HasPrivateKey` y `NotAfter > UtcNow`.
3. Persiste `CertificateData` (bytea), `CertificatePasswordEncrypted` (DPAPI / Data Protection), `CertificateExpiresAt`.
4. Responde con metadata + warning si <30 días.
5. Admin completa credenciales SOL.
6. Admin cambia environment a `production` → backend valida prerequisites.
7. Cualquier emisión posterior firma con este cert (responsabilidad de `IXmlSigningService`, fuera de scope).

### Edge cases / gotchas del backend
- **Permisos**: solo `admin`.
- **Auditoría**: queda como `post.action` (FLAG, sin mapping específico).
- **Idempotencia**: subir el mismo cert dos veces es seguro (sobrescribe).
- **Secret rotation**: manual (DELETE + POST). No hay endpoint dedicado de rotación ni alerta automática.
- **Cert expiry warnings**: solo se devuelve en la respuesta del upload o vía `/status`. No hay job que avise por email/webhook.
- **Plan limits enforcement**: no aplica.
- **Coexistencia de dos controllers** (`/v1/certificate` vs `/v1/tenant/certificate`) → confusión y posible divergencia. El de tenant acepta `.pem` y el otro no.
- **`.pem` formato especial**: en `TenantController` se guarda como text y se prefija con `"PEM:"` en el password encrypted (`TenantController.cs:134`).
- **Bytes en bytea**: el cert vive en la fila del tenant; bases grandes podrían sufrir si hay muchos tenants con upload concurrente.
- **No se verifica cadena de confianza** contra issuers autorizados por SUNAT.
- **No se valida que el cert sea de tipo `Sello Empresarial`** (requisito SUNAT, no enforced).

### Navegación adyacente
- `/settings` (variante alternativa del mismo flujo).
- `/audit-log` (quién subió/eliminó).
- `/documents` (depende de este certificado para firmar).

---

## /plan — Plan y Facturación

### Propósito
Pantalla informativa que lista los planes disponibles y permite contactar al equipo de ventas para hacer upgrade. **El billing no está integrado a ningún provider** — el upgrade real se gestiona manualmente.

### Endpoints REST consumidos
- `GET /v1/plans` — lista de planes activos.
- `GET /v1/tenants/me` — **endpoint no implementado** (el frontend lo llama y silencia el error; FLAG). La ruta real para datos del tenant es `GET /v1/tenant`.

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/PlansController.cs:8`
- Service (si existe): no tiene service dedicado
- Entity: `src/TukiFact.Domain/Entities/Plan.cs:3`; `src/TukiFact.Domain/Entities/Subscription.cs:8` (entity definido pero **sin controller propio** — FLAG)
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/PlanRepository.cs:7` (interfaz `src/TukiFact.Application/Interfaces/IPlanRepository.cs:5`)
- Validator (si existe): no tiene validador — solo expone GET
- DTOs: respuesta inline como objeto anónimo en `PlansController.cs:18`

### Campos del backend — Request Create
Create no implementado vía API (los planes se siembran en migration / seed). Solo el backoffice (super-admin) puede crear planes — ver `BackofficeController` fuera de scope.

### Campos del backend — Request Update
Update no implementado vía API.

### Campos del backend — Response
**`GET /v1/plans`** (objeto anónimo por plan, `PlansController.cs:19`):
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `Id` | `Guid` | No | |
| `Name` | `string` | No | Nombre del plan ("Free", "Starter", "Pro") |
| `PriceMonthly` | `decimal` | No | Precio mensual (USD según UI) |
| `MaxDocumentsPerMonth` | `int` | No | Quota de documentos. `0` o `-1` = ilimitado (interpretación del frontend) |
| `Features` | `object` | No | JSONB deserializado (claves como `api`, `support`, `ai`, `users`, `series`, `webhooks`) |
| `IsActive` | `bool` | No | |

### Enums / Catálogos SUNAT relevantes
No aplica SUNAT. **Ciclos de billing**: solo mensual (`PriceMonthly`). No hay anual/trimestral.

**Features conocidas** (catálogo informal en `Plan.Features` JSONB; UI mapea labels en `plan/page.tsx:14`):
- `api` — acceso a API REST
- `support` — soporte técnico
- `ai` — IA para facturas
- `users` — cantidad / tipo de usuarios
- `series` — cantidad de series
- `webhooks` — habilitado o no

### Servicios externos invocados
- Stripe / billing provider: **NO** — comentario en `Subscription.cs:5` confirma "Payment gateway integration (Stripe/MercadoPago) is POST-DEPLOY".
- Email: NO desde backend (el frontend abre `mailto:ventas@tukifact.pe`).
- MinIO: NO
- Crypto / signing test: NO
- Audit log writer: NO escribe nada relacionado al plan desde el frontend (no hay POST/PUT/DELETE).
- Webhook delivery worker: NO

### Validación oficial del backend
- `PlansController` **no tiene `[Authorize]`** (`PlansController.cs:7`) — endpoint público, accesible sin login.
- No expone uso actual (`DocumentsUsedThisMonth`) — está en `Subscription` pero no se expone.
- No expone el plan activo del tenant aquí — se obtiene vía `GET /v1/tenant` (`PlanName`, `PlanMaxDocs`).

### UI propuesto siguiendo DESIGN-CLIENT
- **Page header**: "Plan y Facturación" + subtítulo "Tu suscripción y límites de uso".
- **Plan actual** destacado: card con badge color del plan, precio, quota, **uso actual** (consumir endpoint dedicado FLAG: no existe), barra de progreso `usado/total`.
- **Grid de planes**: 3 cards (Free / Starter / Pro), CTA según contexto:
  - `Plan Actual` deshabilitado en el card actual;
  - `Contactar ventas` abriendo `mailto:ventas@tukifact.pe` con subject prellenado.
- **PillGroup**: filtro mensual/anual (FLAG: backend no soporta).
- **Sensible defaults**: ordenar por precio ascendente.
- **Smart helpers**:
  - tooltip por feature explicando qué incluye;
  - badge "Más popular" en plan medio (decisión de marketing);
  - banner ámbar si el tenant superó >80% de su quota (depende de endpoint de uso).
- **Empty state**: "No hay planes disponibles, contactá al equipo de ventas".
- **Destructive confirms**: no aplica (no hay cancelación de plan desde acá).
- **ARIA**: tabla comparativa accesible; CTAs con `aria-label` explícito.

### Estados / flujo
1. Usuario abre `/plan`.
2. `GET /v1/plans` lista planes activos.
3. Para identificar el plan actual: idealmente `GET /v1/tenant` devuelve `PlanName` y `PlanMaxDocs` (`TenantController.cs:51-52`). El frontend actual intenta `/v1/tenants/me` y silencia el error — **FLAG, hay que cambiar a `/v1/tenant`**.
4. Upgrade abre cliente de correo del usuario — el cambio de plan se aplica manualmente desde el backoffice.

### Edge cases / gotchas del backend
- **Permisos**: GET es público (sin `[Authorize]`).
- **Auditoría**: no aplica.
- **Idempotencia**: no aplica.
- **Plan limits enforcement**:
  - `Plan.MaxDocumentsPerMonth` es campo informativo;
  - **el enforcement real** del límite no está implementado en este controller — debería verificarse en cada `POST /v1/documents`. Hay que confirmar fuera de scope.
- **Upgrade flow**: vía email manual (no Stripe). El comentario en `Subscription.cs:6` lo dice explícito.
- **`Subscription` entity sin controller** → toda la lógica de ciclos, `next_billing_date`, `documents_used` queda muerta hasta postdeploy.
- **El frontend convierte `priceMonthly` a USD pero el backend no fija moneda** → FLAG, asunción del cliente.
- **`Features` es `object`** → el cliente debe ser defensivo si la forma cambia.

### Navegación adyacente
- `/settings` (datos del tenant).
- `/audit-log` (cambios de plan harían fila acá si fuera vía API — actualmente no).

---

## /audit-log — Registro de Auditoría

### Propósito
Visualizar el historial de acciones de escritura realizadas dentro del tenant (creaciones, ediciones, eliminaciones), con paginación y filtros básicos. Es admin-only.

### Endpoints REST consumidos
- `GET /v1/audit-log?page=&pageSize=&action=&entityType=` — lista paginada.

### Backend behind
- Controller: `src/TukiFact.Api/Controllers/AuditLogController.cs:12`
- Service (si existe): no tiene service dedicado
- Entity: `src/TukiFact.Domain/Entities/AuditLog.cs:3`
- Repository: `src/TukiFact.Infrastructure/Persistence/Repositories/AuditLogRepository.cs:7` (interfaz `src/TukiFact.Application/Interfaces/IAuditLogRepository.cs:5`)
- Validator: no tiene validador (solo GET; query params se pasan tal cual)
- Writer principal: `src/TukiFact.Api/Middleware/AuditMiddleware.cs:8` (escribe automáticamente desde POST/PUT/DELETE/PATCH exitosos)
- Writer secundario: `src/TukiFact.Infrastructure/Services/DespatchAdviceService.cs:414` (escribe manualmente para casos especiales como anulación de GRE)
- DTO: `src/TukiFact.Application/DTOs/AuditLog/AuditLogDto.cs:3`

### Campos del backend — Request Create
La creación de entradas es interna (middleware + services). No hay endpoint `POST` público.

### Campos del backend — Request Update
N/A (audit log es append-only).

### Campos del backend — Response
**Query params**:
- `page` (int, default 1)
- `pageSize` (int, default 30)
- `action` (string?)
- `entityType` (string?)

**Body** (`AuditLogController.cs:31`):
```
{
  data: AuditLogResponse[],
  pagination: { page, pageSize, totalCount, totalPages }
}
```

**`AuditLogResponse`** (`AuditLogDto.cs:3`):
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `Id` | `Guid` | No | |
| `Action` | `string` | No | Verbo dot-namespaced (`document.created`, `user.login`, …) |
| `EntityType` | `string` | No | `Document`, `User`, `ApiKey`, `Series`, `Webhook`, `VoidedDocument`, `Auth`, `Unknown` |
| `EntityId` | `Guid?` | Sí | Id del recurso afectado (lo escribe el service, el middleware no lo setea) |
| `Details` | `string?` | Sí | JSON serializado libre: `{"method":"POST","path":"/v1/...","status":200}` desde el middleware |
| `UserId` | `Guid?` | Sí | Sub del JWT |
| `IpAddress` | `string?` | Sí | `Connection.RemoteIpAddress` |
| `CreatedAt` | `DateTimeOffset` | No | |

> Nota: el `UserAgent` se persiste en la entity (`AuditLog.cs:13`) pero **no se expone en la response DTO** (FLAG).

### Enums / Catálogos SUNAT relevantes
No aplica SUNAT. Catálogo de acciones (parcial, según `AuditMiddleware.MapPathToAction` en `AuditMiddleware.cs:54`):
- `tenant.registered`, `user.login`, `user.created`
- `document.created`, `creditnote.created`, `debitnote.created`, `document.voided`
- `apikey.generated`, `series.created`, `webhook.created`
- Genéricos: `put.updated`, `delete.deleted`, `post.action`

Catálogo de entity types (`AuditMiddleware.cs:71`):
- `Document`, `User`, `ApiKey`, `Series`, `Webhook`, `VoidedDocument`, `Auth`, `Unknown`.

(Ninguno de estos catálogos vive como enum — son strings. FLAG.)

### Servicios externos invocados
- Stripe / billing provider: NO
- Email: NO
- MinIO: NO
- Crypto / signing test: NO
- Audit log writer: **es el writer / reader de sí mismo**.
- Webhook delivery worker: NO

### Validación oficial del backend
- `[Authorize(Roles = "admin")]` a nivel controller (`AuditLogController.cs:11`).
- `_tenantProvider.GetCurrentTenantId()` aísla por tenant en el query (`AuditLogController.cs:29`).
- `page < 1` o `pageSize < 1`: no enforced (podría dar SQL error o pageSize 0).
- `pageSize` máximo: no enforced (FLAG: alguien puede pedir pageSize=10000 y degradar la DB).
- Catálogo de `action` / `entityType` en filtros: no enforced contra whitelist.
- El middleware **no audita**:
  - GETs (intencional);
  - paths que arrancan con `/health`, `/openapi`, `/api/ping`;
  - responses con status >= 400;
  - requests sin tenantId (`AuditMiddleware.cs:30`).

### UI propuesto siguiendo DESIGN-CLIENT
- **Page header**: "Audit Log" + subtítulo "Registro de acciones de escritura".
- **Toolbar / filtros**:
  - `Select` por `entityType` (Documents / Users / Auth / API Keys / Webhooks / Series / Todos);
  - `Select` por `action` (poblar con valores únicos del primer fetch);
  - date range picker `desde`/`hasta` (FLAG: backend no soporta — habría que agregar);
  - search por user/IP (FLAG: backend no soporta);
- **Tabla**: `Acción` (badge color por familia), `Tipo`, `Usuario` (resolver email del UserId — FLAG: el backend no hace join), `IP`, `Fecha`.
- **Drawer al click** mostrando `Details` parseado como JSON pretty + `UserAgent` (necesita exponerlo).
- **PillGroup** para selección rápida de entityType.
- **Sensible defaults**: pageSize=20 (frontend pide 20), ordenar desc por fecha.
- **Smart helpers**:
  - chips de color consistentes con la acción (verde=create, ámbar=update, rojo=delete);
  - export a CSV (FLAG: endpoint no existe);
  - link directo al recurso (`/documents/{entityId}`) cuando `EntityType=Document` y el `EntityId` no es null.
- **Empty state**: "Aún no hay actividad registrada".
- **Destructive confirms**: no aplica (read-only).
- **ARIA**: paginación con `aria-label="Página siguiente / anterior"`, fila con `aria-rowindex`.

### Estados / flujo
1. Cada request mutativa exitosa pasa por `AuditMiddleware`, que mapea `(method, path) → (action, entityType)` y persiste un `AuditLog` con userId, IP, UserAgent y un `Details` JSON minimal.
2. Algunos services (e.g. `DespatchAdviceService`) escriben entradas manuales más ricas en `Details` y con `EntityId` real.
3. Admin abre `/audit-log` → `GET /v1/audit-log` paginado.
4. Filtros opcionales por `action` o `entityType`.

### Edge cases / gotchas del backend
- **Permisos**: solo `admin`.
- **Auditoría de sí misma**: el GET no se audita (es GET).
- **Idempotencia**: append-only.
- **`EntityId` queda null** para la mayoría de entradas del middleware (no lo extrae del response). Solo los writers manuales lo setean.
- **`UserAgent` se persiste pero no se expone** vía el DTO actual — FLAG.
- **`pageSize` sin máximo** → riesgo de DoS interno.
- **Sin filtro por rango de fechas** → FLAG.
- **Sin filtro por `UserId`** → FLAG (no se puede ver "qué hizo el usuario X").
- **Acciones agrupadas como `put.updated` / `delete.deleted`** son poco semánticas — habría que ampliar el mapping del middleware.
- **El middleware swallowea excepciones** (`AuditMiddleware.cs:51`) → si falla la escritura, la request sigue, pero el log queda perdido (silencioso).
- **`DELETE` de webhook / api-key / user** no genera un mapping dedicado → entry ambigua.
- **No retention policy** (no hay TTL ni archival).

### Navegación adyacente
- `/users`, `/api-keys`, `/webhooks`, `/series`, `/documents` — origen de los registros.
- `/settings` (cambios de tenant).

---

## /settings — Configuración (Datos generales + servicios externos)

### Propósito
Pantalla central de configuración del tenant: datos de la empresa, certificado digital, entorno SUNAT, datos de seguridad, y configuración de servicios externos (proveedor de DNI/RUC y proveedor de IA, con sus propias API keys).

### Endpoints REST consumidos
- `GET /v1/tenant` — datos de la empresa + estado de cert/cred/plan.
- `PUT /v1/tenant` — actualizar `NombreComercial`, `Direccion`, `Ubigeo`, `Departamento`, `Provincia`, `Distrito`, `PrimaryColor`, `SunatUser` (admin only).
- `POST /v1/tenant/certificate` — multipart, acepta `.pfx` / `.p12` / `.pem` (admin only).
- `DELETE /v1/tenant/certificate` — eliminar cert (admin only).
- `PUT /v1/tenant/environment` — cambiar entorno (admin only).
- `GET /v1/services/config` — config actual de servicios externos (lookup + AI).
- `PUT /v1/services/config` — actualizar config (admin only).
- `GET /v1/services/providers` — catálogo público de proveedores + modelos.
- `POST /v1/services/ai/test` — admin only, prueba la AI key contra todos los modelos.

### Backend behind
- Controllers:
  - `src/TukiFact.Api/Controllers/TenantController.cs:11`
  - `src/TukiFact.Api/Controllers/ExternalServicesController.cs:20`
- Service (si existe): no tiene service dedicado para tenant; lookup/AI con HTTP outbound inline en `ExternalServicesController.CallLookupProvider` (`:333`) y `CallAiProvider` (`:455`)
- Entity:
  - `src/TukiFact.Domain/Entities/Tenant.cs:3`
  - `src/TukiFact.Domain/Entities/TenantServiceConfig.cs:11`
- Repository:
  - `src/TukiFact.Infrastructure/Persistence/Repositories/TenantRepository.cs:7`
  - `TenantServiceConfig` se accede directamente vía `AppDbContext.TenantServiceConfigs` (sin repository dedicado — FLAG)
- Validator: no tiene validador FluentValidation — validaciones inline en controllers
- Secret protection: `ISecretProtector` para `CertificatePasswordEncrypted` y `SunatPasswordEncrypted`. **`LookupApiKey` y `AiApiKey` se guardan en texto plano** (`ExternalServicesController.cs:96, 107` con TODOs visibles) → FLAG crítico.

### Campos del backend — Request Create
Create del tenant ocurre en registro (`/auth/register`, fuera de scope). En `/settings` no se crea nada nuevo.

### Campos del backend — Request Update
**`PUT /v1/tenant`** — `UpdateTenantRequest` (`TenantController.cs:199`):
| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `NombreComercial` | `string?` | No | no enforced | Nombre comercial visible en PDFs | `Input` text |
| `Direccion` | `string?` | No | no enforced | Dirección fiscal completa | `Input` text |
| `Ubigeo` | `string?` | No | no enforced — no se valida contra tabla `Ubigeos` | Código ubigeo INEI (6 dígitos) | `UbigeoLookup` con autocomplete |
| `Departamento` | `string?` | No | no enforced | Nombre departamento | derivable de ubigeo |
| `Provincia` | `string?` | No | no enforced | Nombre provincia | derivable de ubigeo |
| `Distrito` | `string?` | No | no enforced | Nombre distrito | derivable de ubigeo |
| `PrimaryColor` | `string?` | No | no enforced — no se valida formato hex | Color para branding del PDF | `ColorPicker` |
| `SunatUser` | `string?` | No | no enforced | Usuario SOL secundario | `Input` text |

**Read-only en el backend** (no incluidos en `UpdateTenantRequest`): `Ruc`, `RazonSocial`, `LogoUrl`, `PlanId`, `IsActive`, `CreatedAt`, `Environment`, `CertificateData`.

**`PUT /v1/services/config`** — `UpdateServiceConfigRequest` (`ExternalServicesController.cs:597`):
| Campo | Tipo C# | Obligatorio | Validación | Semántica | UI sugerida |
|---|---|---|---|---|---|
| `LookupProvider` | `string?` | No | enforced — whitelist `none/apiperu/migo/peruapi/apis_net` (`:89`) | Proveedor para DNI/RUC | `Select` |
| `LookupApiKey` | `string?` | No | no enforced — guardada en texto plano | Token del proveedor | `Input` type=password |
| `AiProvider` | `string?` | No | enforced — whitelist `none/gemini/claude/grok/deepseek/openai` (`:100`) | Proveedor LLM | `Select` |
| `AiApiKey` | `string?` | No | no enforced — guardada en texto plano | API key LLM | `Input` type=password |
| `AiModel` | `string?` | No | no enforced — no se verifica contra catálogo de modelos | Modelo concreto | `Select` dependiente del provider |

### Campos del backend — Response
**`GET /v1/tenant`** (objeto anónimo, `TenantController.cs:36`):
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `Id` | `Guid` | No | Tenant id |
| `Ruc` | `string` | No | RUC 11 dígitos |
| `RazonSocial` | `string` | No | Razón social oficial |
| `NombreComercial` | `string?` | Sí | |
| `Direccion` | `string?` | Sí | |
| `Ubigeo` | `string?` | Sí | 6 dígitos INEI |
| `Departamento` | `string?` | Sí | |
| `Provincia` | `string?` | Sí | |
| `Distrito` | `string?` | Sí | |
| `LogoUrl` | `string?` | Sí | Storage URL |
| `PrimaryColor` | `string` | No | hex, default `"#1a73e8"` |
| `Environment` | `string` | No | `"beta"` o `"production"` |
| `IsActive` | `bool` | No | |
| `PlanName` | `string` | No | Plan name o `"Free"` si null |
| `PlanMaxDocs` | `int` | No | `50` si plan null |
| `HasCertificate` | `bool` | No | |
| `CertificateExpiresAt` | `DateTimeOffset?` | Sí | |
| `HasSunatCredentials` | `bool` | No | |
| `CreatedAt` | `DateTimeOffset` | No | |

> El response **no expone** `GreClientId`, `GreClientSecret`, `SunatUser` (oculto por diseño aunque podría exponerse — FLAG).

**`GET /v1/services/config`** (`ExternalServicesController.cs:61`):
| Campo | Tipo | Nullable | Significado |
|---|---|---|---|
| `lookupProvider` | `string` | No | |
| `lookupApiKeyConfigured` | `bool` | No | Solo indica presencia |
| `aiProvider` | `string` | No | |
| `aiApiKeyConfigured` | `bool` | No | |
| `aiModel` | `string?` | Sí | |

**`GET /v1/services/providers`** — catálogo público:
```
{ lookup: [{id, name, url, freeTier, paidFrom}], ai: [{id, name, models: string[]}] }
```

### Enums / Catálogos SUNAT relevantes
- **Ubigeo INEI**: hay `src/TukiFact.Domain/Entities/Ubigeo.cs` — entity persistida con códigos y departamentos. **No hay endpoint que la sirva** (FLAG: el frontend no tiene autocomplete real).
- **Departamentos**: idem, vive solo como columna de `Ubigeo`.
- **Environment**: `"beta"` / `"production"`.
- **PlanName**: depende del seed (no hay enum).

### Servicios externos invocados
- Stripe / billing provider: NO
- Email: NO directamente; el frontend abre `mailto:` para contactar ventas.
- MinIO: NO desde estos controllers (el logo se sube por otro flujo si existiera).
- Crypto / signing test: SÍ — al subir certificado (`X509CertificateLoader`) y al cifrar passwords con `ISecretProtector`.
- Audit log writer: SÍ — todas las mutaciones son auditadas vía middleware (entries `put.updated` / `post.action`).
- Webhook delivery worker: NO directamente.
- HTTP outbound a proveedores LLM / lookup desde `ExternalServicesController` (con timeouts 15s lookup / 30s AI).

### Validación oficial del backend
- `GET /v1/tenant`: `[Authorize]` (cualquier usuario autenticado).
- `PUT /v1/tenant`: `[Authorize(Roles = "admin")]`.
- `POST /v1/tenant/certificate`: admin only + `RequestSizeLimit 5MB` + extensión `.pfx`/`.p12`/`.pem`.
- `PUT /v1/tenant/environment`: admin only + enum `beta`/`production`.
- `PUT /v1/services/config`: admin only + whitelist proveedores.
- `POST /v1/services/ai/test`: admin only.
- `GET /v1/services/providers`: **`[AllowAnonymous]`** (`ExternalServicesController.cs:124`).
- Ubigeo no validado contra tabla.
- Color hex no validado.
- `LookupApiKey` / `AiApiKey` se persisten en plano (FLAG crítico).
- Cambio de RUC: no permitido vía API.

### UI propuesto siguiendo DESIGN-CLIENT
Esta es la pantalla más grande. Dividir en secciones (cards) con anclas:

1. **Datos de la empresa**
   - card con `RUC` y `Razón Social` read-only en grande;
   - inputs `NombreComercial`, `Direccion`;
   - **smart helper**: `UbigeoLookup` con autocomplete contra `/v1/utils/ubigeo` (FLAG: endpoint no existe, crear);
   - `ColorPicker` con preview en miniatura del PDF;
   - botón "Guardar Cambios" con estado de saving.
2. **Certificado Digital** — ver sección `/certificate` (con cuidado: la UI de `/settings` apunta a `/v1/tenant/certificate` y acepta `.pem` además de `.pfx`/`.p12`).
3. **Entorno SUNAT** — toggle Beta/Producción con confirm destructive.
4. **Seguridad** — read-only: TenantId, badge de credenciales SUNAT.
5. **Servicios Externos / Lookup DNI/RUC**
   - `Select` proveedor (con info de free tier y paid from);
   - `Input` API key con placeholder "ya configurada" si está;
   - link a la doc del proveedor;
   - badge "✓ Activo" si guardada.
6. **Servicios Externos / Copiloto IA**
   - `Select` proveedor → `Select` modelo dependiente → `Input` API key;
   - botón "Test de Key" que llama `/v1/services/ai/test` y muestra tabla con estado de cada modelo;
   - badge "✓ Activo" con resumen.

**Sensible defaults**: env Beta por defecto, `PrimaryColor=#1a73e8`.

**Smart helpers**:
- **RUC autocomplete**: si el lookup provider está configurado, autocompletar `RazonSocial` etc. al registrarse (no aplica acá porque ya está creado, pero relevante para `/customers`);
- **Cert validity test inline** post-upload (cubierto en `/certificate`);
- **Validation del Ubigeo** contra `Ubigeo` entity (FLAG: endpoint).

**Empty states**:
- "Aún no configuraste tu proveedor de IA. El Copiloto no funcionará hasta que conectes una cuenta.";
- "Datos incompletos: configurá ubigeo para que aparezca en los PDFs."

**Destructive confirms**:
- delete certificate;
- toggle production environment;
- cambiar API key configurada (warning: invalidará la actual).

**ARIA**:
- secciones con `<h2>` y `aria-labelledby`;
- inputs de password con `aria-describedby` apuntando al hint inline;
- test de keys con `aria-live="polite"` en la tabla de resultados.

### Estados / flujo
1. Carga inicial → `GET /v1/tenant` + `GET /v1/services/config` + `GET /v1/services/providers`.
2. Admin edita datos generales → `PUT /v1/tenant` (200 OK).
3. Admin sube certificado → `POST /v1/tenant/certificate` (multipart) → status se refresca con `GET /v1/tenant`.
4. Admin cambia entorno → `PUT /v1/tenant/environment`.
5. Admin configura lookup/AI → `PUT /v1/services/config`.
6. Admin testea AI key → `POST /v1/services/ai/test` → tabla de modelos.

### Edge cases / gotchas del backend
- **Permisos**: GET es para cualquier autenticado; mutaciones son admin only.
- **Auditoría**: todas las mutaciones quedan en `AuditLog` (genéricas `put.updated`).
- **Idempotencia**: PUTs son naturalmente idempotentes; POST de certificado sobreescribe el actual.
- **Secret rotation**:
  - cert: manual (DELETE + POST);
  - SUNAT pass: PUT lo sobreescribe (cifrado);
  - **Lookup/AI API keys: persistidas en plano** — FLAG CRÍTICO (comentarios `// TODO: encrypt at rest` en líneas `:96` y `:107`).
- **Cert expiry warnings**: solo via `GET /v1/certificate/status` / `GET /v1/tenant`. No hay job.
- **Plan limits enforcement**: ver sección `/plan`.
- **`PUT /v1/tenant` no permite cambiar `RUC` ni `RazonSocial`**: correcto (datos SUNAT).
- **No hay validación del formato Ubigeo** ni cross-check con `Ubigeo` entity.
- **`PrimaryColor` sin validación** podría romper la generación del PDF si llega un valor inválido.
- **Coexistencia de `/v1/certificate/*` y `/v1/tenant/certificate`**: dos APIs hacen casi lo mismo; el frontend de `/settings` usa la variante de tenant.
- **`SunatUser` se actualiza vía `PUT /v1/tenant`** (en `UpdateTenantRequest`) **y** vía `PUT /v1/certificate/sunat-credentials` — ambos rutas tocan el mismo campo, riesgo de divergencia.

### Navegación adyacente
- `/certificate`, `/users`, `/api-keys`, `/webhooks`, `/audit-log`, `/plan`.
- `/ai` (depende de configuración acá).

---

## Group D — Observaciones transversales

### Patrones comunes
1. **Admin-only por defecto**: 5 de 8 controllers están bloqueados con `[Authorize(Roles = "admin")]` a nivel clase (`ApiKeys`, `Webhooks`, `Users`, `Certificate`, `AuditLog`). `Tenant` mezcla por endpoint. `Plans` no requiere auth. `ExternalServices` requiere autenticación general con admin solo para mutaciones críticas.
2. **Sin services dedicados**: la lógica vive inline en los controllers. Solo `WebhookDeliveryService` y `DespatchAdviceService` siguen el patrón service. → FLAG arquitectónico para refactor (CQRS / MediatR está disponible en la solución a juzgar por el árbol).
3. **Sin validators FluentValidation**: solo dos validators existen en `src/TukiFact.Application/Validation/` (`DespatchAdviceValidator`, `RecurringInvoiceValidator`). Toda la validación de Group D es inline → FLAG (inconsistencia).
4. **Aislamiento por tenant inconsistente**: `WebhookRepository`, `UserRepository`, `Certificate*`/`Tenant*` no filtran por TenantId en sus métodos `GetByIdAsync`. Los controllers de Webhooks y Users dependen de que el caller envíe el GUID correcto, **abriendo riesgo cross-tenant** si un admin malicioso adivina IDs. → FLAG de seguridad.
5. **Auditoría parcial via middleware**: el `AuditMiddleware` cubre la mayoría de mutaciones, pero los mappings son genéricos (`put.updated`, `delete.deleted`, `post.action`). `EntityId` no se extrae del response. `UserAgent` se persiste pero no se expone. → FLAG para enriquecer el mapping.
6. **Catálogos hardcoded en código C#** (proveedores LLM, modelos, eventos de webhook) en lugar de tablas / enums. Cambiar requiere deploy. → FLAG.
7. **Cifrado inconsistente de secretos**: passwords (cert, SUNAT) usan `ISecretProtector`. Las API keys de servicios externos (`LookupApiKey`, `AiApiKey`) se persisten en texto plano con `// TODO: encrypt at rest`. → FLAG CRÍTICO de seguridad / compliance.
8. **Pattern de "secret revealed once"**: API Keys y Webhooks lo hacen bien (plaintext solo en respuesta de Create). El reveal modal en UI es consistente.
9. **Doble controller para certificado** (`CertificateController` + `TenantController`) con APIs ligeramente diferentes (`.pem` aceptado solo en uno). → FLAG de duplicación.
10. **Hard-deletes vs soft-deletes inconsistentes**: Users y ApiKeys son soft delete (`IsActive=false`). Webhooks son hard delete. → FLAG (definir convención).

### Qué falta para producción
- **Billing real**: no hay Stripe/MercadoPago integrado. `Subscription` entity está hidratada pero sin controller; el flujo de upgrade es `mailto:`. Implementar webhook de Stripe + tracker de `DocumentsUsedThisMonth`.
- **Enforcement de quota de plan**: nadie chequea `Plan.MaxDocumentsPerMonth` en el `POST /v1/documents`. Hay que agregar guard en el service / middleware de emisión.
- **Email transaccional**: no hay invitación de usuario por email, ni "tu certificado expira en 30 días", ni "tu plan está al 80%", ni "se generó una API Key en tu cuenta". Hay `IEmailService` y `EmailLog` entity — verificar implementación y conectar.
- **Cifrado de API keys** de servicios externos (`LookupApiKey`, `AiApiKey`) con `ISecretProtector`.
- **Endpoint de ubigeo lookup** (`GET /v1/utils/ubigeo?q=...`) consumiendo la entity `Ubigeo`. Hoy el frontend pide al admin tipear depto/prov/dist a mano.
- **Filtros completos en `/audit-log`**: rango de fechas, filtro por usuario, exposición de `UserAgent`, export CSV.
- **Validación de scope en API Keys**: catálogo enforced contra `[emit, query, void]`; ahora se acepta cualquier string.
- **Validación de URL https + catálogo de events** en Webhooks. Test delivery + reintento manual.
- **Aislamiento tenant en GetById** para Users, Webhooks, Tenant. Hoy se filtra solo en GetAll / GetByTenant.
- **Filtro `pageSize` máximo** en `/audit-log` (y en otras listas paginadas).
- **2FA / verificación de email** para usuarios — críticos para producción.
- **Rotación de secretos**: endpoints dedicados (`POST /v1/api-keys/{id}/rotate`, `POST /v1/webhooks/{id}/rotate-secret`).
- **Cert expiry warnings**: job scheduled (hangfire/quartz/cron) que mande email cuando `daysUntilExpiry <= 30`.
- **Consolidar `/v1/certificate` y `/v1/tenant/certificate`** en un único set de endpoints.
- **Persistencia del queue de webhooks**: hoy `Task.Run` fire-and-forget; un reinicio del proceso pierde retries pendientes. Mover a un BackgroundService con outbox/queue.
- **Eliminar el llamado a `/v1/tenants/me`** en la pantalla `/plan` (ese endpoint no existe; debería ser `/v1/tenant`).
- **Catálogos como enums / tablas** (acciones de audit, scopes de API key, eventos de webhook, proveedores LLM, modelos LLM).

