# TukiFact — Sistema de Diseño (Cliente / Tenant)

> Documento maestro para el **portal de clientes** de TukiFact (login, registro y panel del tenant). El backoffice tiene su propio documento (`DESIGN-ADMIN.md`). Este archivo es la fuente de verdad para Claude Design / Stitch y para cualquier IA o diseñador humano que regenere pantallas.

---

## 1. Marca

### 1.1 Identidad
- **Nombre:** TukiFact
- **Tagline:** "Facturación Inteligente"
- **Personalidad:** profesional, confiable, accesible, peruano. Hablamos de impuestos y SUNAT — no podemos sonar a juguete. Pero somos amigables: nuestro mascot es un tucán, no un candado.
- **Tono UI:** claro, denso pero respirable, calmado. Los acentos amarillo/naranja se usan con disciplina: una sola pieza dorada por pantalla manda la jerarquía.

### 1.2 Logo
- **Archivo principal:** `/logo.png` (1258×398, transparente, lockup horizontal). Es el lockup completo: tucán + ícono de factura + wordmark "TukiFact" + tagline "FACTURACIÓN INTELIGENTE".
- **Ícono cuadrado:** `/icon.png` (512×512, transparente). Solo el tucán + factura — para favicon, avatars, OG image cuadrada.
- **Tamaños mínimos:**
  - Lockup horizontal: ancho ≥ 120 px en pantalla.
  - Ícono cuadrado: ≥ 24 px.
- **Área de protección:** padding igual a la altura de la "T" de TukiFact alrededor del lockup.
- **Sobre fondo oscuro:** el lockup base ya funciona (texto negro pasa a ser ilegible). En oscuro, usar versión invertida — por ahora, sobre cualquier hero oscuro usar el **icon.png** + wordmark blanco al lado.
- **NO HACER:**
  - No rotar, distorsionar, cambiar colores del tucán.
  - No reemplazar la familia tipográfica del wordmark con la del sistema.
  - No usar el tucán suelto sin la factura (forma una sola pieza).

### 1.3 Voz / copy
- **Idioma:** español (Perú).
- **Persona:** tratar de "tú" a usuarios finales del tenant; usar lenguaje fiscal correcto cuando aplique ("RUC", "boleta", "guía de remisión", "comprobante", "SUNAT").
- **Errores:** explicar qué pasó y qué hacer. Nunca mostrar stack traces. Ejemplo correcto: "No pudimos validar tu RUC ante SUNAT. Verifica el número o vuelve a intentar en unos minutos."
- **Estados vacíos:** una línea de qué falta + un CTA primario.

---

## 2. Tokens de diseño

> Todos los tokens viven en `src/app/globals.css` como variables CSS bajo `:root` y `.dark`. Trabajamos en **OKLCH** (Tailwind v4 lo soporta nativo). Los hex que listo son aproximaciones útiles para mockups.

### 2.1 Color — primitivos

#### Marca
```
--brand-toucan-yellow:  oklch(0.85 0.17 88)    /* #FFC72C  — amarillo del cap del tucán + "Fact" */
--brand-toucan-orange:  oklch(0.69 0.20 42)    /* #FF6B1F  — cola y signo $ */
--brand-ink:            oklch(0.18 0.01 264)   /* #0F1115  — negro del cuerpo del tucán */
--brand-paper:          oklch(0.99 0.003 264)  /* #FAFAFB  — blanco hueso del documento */
```

#### Neutrales (slate)
```
--slate-50:  oklch(0.985 0.003 264)   /* #FAFBFC */
--slate-100: oklch(0.965 0.005 264)   /* #F1F2F5 */
--slate-200: oklch(0.925 0.008 264)   /* #E3E5EA */
--slate-300: oklch(0.875 0.011 264)   /* #CFD2DA */
--slate-400: oklch(0.72  0.012 264)   /* #9298A4 */
--slate-500: oklch(0.58  0.013 264)   /* #6B7280 */
--slate-600: oklch(0.46  0.012 264)   /* #4B5260 */
--slate-700: oklch(0.36  0.011 264)   /* #383D49 */
--slate-800: oklch(0.27  0.010 264)   /* #252934 */
--slate-900: oklch(0.18  0.009 264)   /* #131722 */
--slate-950: oklch(0.12  0.008 264)   /* #0A0D14 */
```

#### Estado
```
--success: oklch(0.66 0.14 152)   /* #1FA968 */
--warning: oklch(0.80 0.16 78)    /* #E8A91A */
--danger:  oklch(0.60 0.21 28)    /* #DB3D2C */
--info:    oklch(0.62 0.14 240)   /* #2A7CD8 */
```

### 2.2 Color — tokens semánticos

#### Tema claro (default)
```
--background:           var(--slate-50)
--foreground:           var(--slate-900)
--card:                 #ffffff
--card-foreground:      var(--slate-900)
--muted:                var(--slate-100)
--muted-foreground:     var(--slate-500)
--border:               var(--slate-200)
--input:                var(--slate-200)
--ring:                 var(--brand-toucan-yellow)
--primary:              var(--brand-ink)         /* Botones CTA primarios = negro grafito */
--primary-foreground:   #ffffff
--secondary:            var(--slate-100)
--secondary-foreground: var(--slate-900)
--accent:               var(--brand-toucan-yellow)  /* Highlights, badges destacados */
--accent-foreground:    var(--brand-ink)
--destructive:          var(--danger)
--destructive-foreground: #ffffff

/* Sidebar */
--sidebar:                       #ffffff
--sidebar-foreground:            var(--slate-700)
--sidebar-primary:               var(--brand-ink)
--sidebar-primary-foreground:    #ffffff
--sidebar-accent:                var(--slate-100)
--sidebar-accent-foreground:     var(--brand-ink)
--sidebar-border:                var(--slate-200)
--sidebar-ring:                  var(--brand-toucan-yellow)
```

#### Tema oscuro
```
--background:           var(--slate-950)
--foreground:           var(--slate-100)
--card:                 var(--slate-900)
--card-foreground:      var(--slate-100)
--muted:                var(--slate-800)
--muted-foreground:     var(--slate-400)
--border:               var(--slate-800)
--input:                var(--slate-800)
--ring:                 var(--brand-toucan-yellow)
--primary:              var(--brand-toucan-yellow)  /* En oscuro, el amarillo manda */
--primary-foreground:   var(--brand-ink)
--secondary:            var(--slate-800)
--secondary-foreground: var(--slate-100)
--accent:               var(--brand-toucan-orange)
--accent-foreground:    #ffffff

--sidebar:                       var(--slate-900)
--sidebar-foreground:            var(--slate-300)
--sidebar-primary:               var(--brand-toucan-yellow)
--sidebar-primary-foreground:    var(--brand-ink)
--sidebar-accent:                var(--slate-800)
--sidebar-accent-foreground:     #ffffff
--sidebar-border:                var(--slate-800)
```

### 2.3 Tipografía
- **Familia primaria:** Geist Sans (ya cargada por Next). Fallback: `Inter`, `system-ui`, `sans-serif`.
- **Familia mono:** Geist Mono. Uso: RUC, IDs, números de comprobante, totales en estado de cuenta.
- **Escala** (tamaño / line-height / weight):
  ```
  display-2xl  → 48 / 56 / 700   // Hero marketing solamente
  display-xl   → 36 / 44 / 700   // H1 de página
  display-lg   → 30 / 38 / 600   // H1 de modal grande
  h1           → 24 / 32 / 600   // Título de sección
  h2           → 20 / 28 / 600   // Título de card / subsección
  h3           → 18 / 26 / 600
  body-lg      → 16 / 24 / 400
  body         → 14 / 22 / 400   // Default app
  body-sm      → 13 / 20 / 400
  label        → 13 / 20 / 500   // Labels de form
  caption      → 12 / 18 / 500
  overline     → 11 / 16 / 600 letter-spacing 0.06em uppercase
  num-lg       → 28 / 34 / 600 tabular-nums  // KPIs
  num-md       → 18 / 24 / 600 tabular-nums  // totales de tabla
  ```
- **Tabular nums** obligatorio en cualquier columna numérica de tabla y en totales.

### 2.4 Espaciado
Base 4 px. Escala disponible (alineada con Tailwind): `0, 1(4), 2(8), 3(12), 4(16), 5(20), 6(24), 8(32), 10(40), 12(48), 16(64), 20(80), 24(96)`.
- **Padding interno de card:** 24 px (mobile 16 px).
- **Gap entre cards:** 16 px.
- **Container max-width contenido principal:** 1280 px (`max-w-7xl`).
- **Container max-width formularios:** 640 px (`max-w-2xl`).
- **Container max-width auth (login/registro):** 440 px (`max-w-md`).

### 2.5 Radios
```
--radius-sm:  6 px
--radius-md:  8 px        /* default de inputs y botones */
--radius-lg:  12 px       /* cards */
--radius-xl:  16 px       /* dialogs, popovers grandes */
--radius-2xl: 20 px       /* hero cards */
--radius-full: 9999 px    /* avatars, pills */
```
El tucán del logo tiene curvas suaves: nuestro radio default es **md (8 px)**. Nada cuadrado angular, nada de pill shape para botones (pill solo en chips/badges).

### 2.6 Sombras
```
--shadow-xs: 0 1px 2px 0 rgb(15 17 21 / 0.04)
--shadow-sm: 0 1px 3px 0 rgb(15 17 21 / 0.06), 0 1px 2px -1px rgb(15 17 21 / 0.06)
--shadow-md: 0 4px 6px -1px rgb(15 17 21 / 0.07), 0 2px 4px -2px rgb(15 17 21 / 0.06)
--shadow-lg: 0 10px 15px -3px rgb(15 17 21 / 0.08), 0 4px 6px -4px rgb(15 17 21 / 0.05)
--shadow-xl: 0 20px 25px -5px rgb(15 17 21 / 0.10), 0 8px 10px -6px rgb(15 17 21 / 0.06)
--shadow-focus-ring: 0 0 0 3px oklch(0.85 0.17 88 / 0.35)  /* amarillo TukiFact al 35% */
```
- Cards inactivas: `shadow-xs` + `border`. Solo eleva con `shadow-md` al hover si es clicable.
- Dialogs / popovers: `shadow-xl`.

### 2.7 Bordes y separadores
- **Width default:** 1 px. Nunca 2 px excepto en botones outlined activos.
- **Color:** `var(--border)`. En tarjetas con fondo `card`, usamos `border` + sombra; nunca solo sombra.

### 2.8 Motion
- **Durations:** 120 ms (microinteracciones — hover, focus), 200 ms (transición de estados — open/close dropdown), 300 ms (modales, drawers).
- **Easing:** `cubic-bezier(0.22, 1, 0.36, 1)` (out-expo) para entradas; `cubic-bezier(0.4, 0, 1, 1)` (in) para salidas.
- **Reduced motion:** respetar `prefers-reduced-motion`. Sustituir slides por fades de 100 ms.

---

## 3. Iconografía
- **Librería:** [Lucide React](https://lucide.dev). Stroke 1.5, tamaños 16 / 20 / 24 px.
- **Filled accent:** únicamente en estados activos del aside (ícono cambia a versión "rellena" sutil con `fill-current` + opacity 0.15 detrás).
- **Convención por dominio fiscal:** usar siempre el mismo ícono para el mismo concepto. Tabla de referencia:

| Concepto | Ícono Lucide |
|---|---|
| Dashboard | `LayoutDashboard` |
| Comprobantes / Documentos emitidos | `FileText` |
| Boletas | `Receipt` |
| Facturas | `FileSpreadsheet` |
| Notas de crédito/débito | `FileMinus` / `FilePlus` |
| Guías de remisión | `Truck` |
| Cotizaciones | `FileSearch` |
| Productos / Catálogo | `Package` |
| Clientes | `Users` |
| Plan / Suscripción | `Crown` |
| Series | `Hash` |
| Tipos de cambio | `ArrowRightLeft` |
| Webhooks | `Webhook` |
| API Keys | `KeyRound` |
| Certificado digital | `ShieldCheck` |
| Audit log | `ScrollText` |
| Reportes | `BarChart3` |
| Configuración | `Settings` |
| Usuarios del tenant | `UserCog` |
| Asistente IA | `Sparkles` |
| Recurrentes | `Repeat` |
| Voided / Anulados | `Ban` |
| Percepciones / Retenciones | `Percent` / `PercentSquare` |

---

## 4. Componentes base

Todos los componentes parten de **shadcn/ui** ya instalado. No reescribir desde cero — extender. Los archivos viven en `src/components/ui/`.

### 4.1 Botón (`<Button>`)
Variantes:
- `default` — fondo `primary` (negro grafito), texto blanco. **Uso:** acción principal de la pantalla (máximo 1 por vista). Hover: ligeramente más claro (`oklch(0.22 ...)`).
- `accent` — fondo `accent` (amarillo TukiFact), texto `ink`. **Uso:** acción destacada cuando el contexto la pide (ej. "Emitir comprobante" en dashboard). Máximo 1 por vista, exclusivo con `default`.
- `outline` — fondo transparente, `border` 1 px slate-300, texto foreground. **Uso:** acciones secundarias (cancelar, exportar).
- `ghost` — sin fondo, hover slate-100. **Uso:** acciones terciarias, toolbars, paginación.
- `link` — texto azul info, subrayado en hover. **Uso:** navegación inline.
- `destructive` — fondo `danger`, texto blanco. **Uso:** anular, eliminar, romper algo.
- `success` — fondo `success`, texto blanco. **Uso:** confirmar emisión exitosa (en flujos guiados).

Tamaños: `sm` (h-8 / 32 px), `md` (h-10 / 40 px default), `lg` (h-12 / 48 px), `icon` (cuadrado, mismo h).

Estados: hover (opacidad/lightness +4%), active (lightness -4%), focus (ring amarillo 3 px), disabled (opacity 50, cursor not-allowed), loading (spinner inline, texto se mantiene, ancho fijo).

### 4.2 Input (`<Input>`)
- Altura: 40 px default (`md`); `sm` 32 px para toolbars.
- Border 1 px `--input`, radius `md`.
- Padding horizontal: 12 px. Si tiene ícono leading/trailing: 36 px del lado correspondiente.
- Focus: border `ring`, box-shadow `--shadow-focus-ring`.
- Error: border `danger`, ring rojo.
- Label arriba del input (no floating). Helper text debajo en `body-sm` / `muted-foreground`. Error en `body-sm` / `danger`.
- **Variante numérica:** alinear a la derecha + `tabular-nums` + sufijo de moneda como `<span>` dentro del input box (`S/`, `USD`).

### 4.3 Card
- Background `card`, border 1 px `border`, radius `lg` (12 px), padding 24 px.
- Header con título h2 + opcional descripción `muted-foreground` body-sm.
- Footer separador 1 px arriba, padding 16 px 24 px, alineación derecha para acciones.
- Tarjetas KPI: padding 20 px, KPI value en `num-lg`, delta arriba a la derecha con flecha + color `success`/`danger`.

### 4.4 Badge
- Pill (radius-full), padding 2 px 10 px, `caption` weight 600.
- Variantes semánticas — fondo al 12% del color, texto al color sólido:
  - `pending` → warning
  - `accepted` → success
  - `rejected` → danger
  - `voided` → slate-500
  - `draft` → slate-400
  - `sent` → info
- **Nunca** dos badges adyacentes con el mismo color; si necesitas dos estados, usar uno como badge y otro como ícono.

### 4.5 Tabla
- Header sticky `slate-50` (claro) / `slate-900` (oscuro), text `caption` uppercase, color `muted-foreground`, padding 12 px 16 px.
- Row hover `slate-50`. Row seleccionada `slate-100` + barra izquierda de 3 px en `accent`.
- Cell padding 14 px 16 px. Borde divisorio inferior 1 px `border`.
- Numéricas: alineadas a la derecha + tabular-nums.
- Columna de acciones: ancho fijo 64 px, dropdown menu `MoreHorizontal`.
- Empty state: ilustración 96 px (línea, no full color) + título + descripción + CTA primario.
- Paginación: `< 1 2 3 ... > ` con ghost buttons, items-per-page select a la derecha.

### 4.6 Dialog / Sheet
- Dialog (modal centrado): `max-w-lg` por defecto, radius `xl`, padding 24 px, sombra `xl`, overlay `slate-950/40 backdrop-blur-sm`.
- Sheet (drawer derecho): para forms largos, ancho 480 px en mobile = full, en desktop fixed. Header sticky con cerrar `X` y CTA primario sticky al fondo.

### 4.7 Dropdown menu
- Background `popover` (= `card`), radius `lg`, padding 4 px, sombra `lg`.
- Items 36 px alto, padding 8 px 12 px, ícono leading 16 px gap 8 px.
- Separator `border` 1 px con margen 4 px vertical.

### 4.8 Toast / Sonner
- Position: bottom-right desktop, top-center mobile.
- Variantes: default (slate-900), success, warning, danger, info. Ícono leading 18 px.
- Duration: 4 s default, 7 s para errores.

### 4.9 Empty state
- Tarjeta centrada `max-w-md`, padding vertical 40 px.
- Ilustración 96 px (ícono Lucide gigante en `muted-foreground/30` + fondo decorativo).
- Título `h2`, descripción `body` `muted-foreground`, CTA primario abajo.

### 4.10 Skeleton (loading)
- Background `muted`, animación shimmer suave 1.5 s. Conservar altura de la pieza real.
- En tablas: 5 filas skeleton. En cards KPI: skeleton del número y de la etiqueta.

---

## 5. Layout — Shell autenticado

> Archivo: `src/app/(authenticated)/layout.tsx`. Es el shell visible después de login. Dos zonas: **aside fijo** (desktop) o **drawer** (mobile), y **content area** con topbar arriba.

### 5.1 Aside (sidebar)
- **Ancho:** 256 px expandido, 72 px colapsado. Toggle persistente (guardado en `localStorage`).
- **Fondo:** `sidebar` (blanco en claro, slate-900 en oscuro). Border derecho 1 px `sidebar-border`.
- **Sticky / scrollable:** `fixed` desktop, scroll interno si hay overflow.
- **Estructura vertical** (de arriba a abajo):
  1. **Brand header** (alto 64 px, padding 16 px 20 px, borde inferior)
     - Logo lockup `/logo.png` (h-8 = 32 px, ancho auto, `object-contain`).
     - Botón colapso al extremo derecho (ChevronLeft / ChevronRight, ghost icon button 32 px).
  2. **Switcher de empresa** (alto 56 px, padding 12 px 16 px, borde inferior, hover `sidebar-accent`)
     - Avatar 36 px con iniciales del nombre comercial sobre fondo `accent` (amarillo) + texto `ink` weight 700.
     - Stack: nombre comercial (`label` weight 600 truncate), RUC abajo en `caption` mono `muted-foreground`.
     - Chevron `ChevronsUpDown` al final. Click abre dropdown si el usuario tiene multi-tenant.
  3. **Acción primaria** (margin 16 px, alto 40 px)
     - Botón `accent` full-width: "Emitir comprobante" + ícono `Plus`. Es el atajo crítico del producto.
  4. **Navegación principal** (scrollable)
     - Grupos con label uppercase `overline` color `muted-foreground` padding 12 px 16 px 8 px.
     - Items: alto 40 px, padding 8 px 12 px, gap 12 px, radius `md`, margen horizontal 8 px.
       - Inactivo: ícono 18 px `slate-500`, label `body` `sidebar-foreground`. Hover: bg `sidebar-accent`, label `sidebar-accent-foreground`.
       - Activo: bg `sidebar-accent`, label `sidebar-accent-foreground` weight 600, ícono `ink`/`primary`. Barra izquierda 3 px `accent` (amarillo) extendida full-height del item.
       - Con badge (ej. "Pendientes 3"): badge `warning` pequeño a la derecha.
     - **Grupos sugeridos** (orden):
       - **Operación** → Dashboard, Documentos, Cotizaciones, Guías de remisión, Recurrentes
       - **Catálogo** → Productos, Clientes, Series
       - **Finanzas** → Reportes, Anulados, Tipos de cambio, Percepciones, Retenciones
       - **Integración** → API Keys, Webhooks, Asistente IA
       - **Configuración** → Empresa, Usuarios, Certificado digital, Plan, Audit log
  5. **Footer del aside** (sticky bottom, borde superior)
     - Card de plan: nombre de plan + barra de progreso de comprobantes usados/incluidos + link "Mejorar plan".
     - Avatar usuario + nombre + email truncate. Click abre dropdown (perfil, ajustes, soporte, cerrar sesión).

- **Colapsado (72 px):** solo íconos centrados, tooltip al hover. Logo se reemplaza por `/icon.png` 32 px. Switcher de empresa = solo avatar. Botón "Emitir comprobante" colapsa a icon button con `Plus`.

### 5.2 Topbar (encabezado del content area)
- Alto 64 px, sticky top, fondo `background` con `backdrop-blur-md` cuando hay scroll, border bottom 1 px al hacer scroll.
- Padding 0 24 px.
- Estructura horizontal (izquierda → derecha):
  - **Mobile:** botón hamburguesa (`Menu`) abre Sheet con el aside.
  - **Breadcrumb** (`body` color `muted-foreground`, separator `/` con `mx-1`). Última parte en `foreground` weight 600. Si la página tiene un H1 propio, breadcrumb va arriba pequeño y el H1 está en el content.
  - Spacer.
  - **Search global** (max-w 320 px, input `sm` con ícono `Search` leading, placeholder "Buscar comprobante, cliente, producto..." atajo visual ⌘K a la derecha). Abre command palette.
  - **Notificaciones** (ghost icon button con `Bell`, badge rojo numérico si hay sin leer).
  - **Toggle tema** (Sun/Moon).
  - **Avatar** (32 px con iniciales, dropdown con perfil/cerrar sesión).

### 5.3 Content area
- Background `background`.
- Padding 24 px desktop, 16 px mobile.
- Max-width 1280 px centrado (`max-w-7xl mx-auto`).
- Cada página comienza con un **Page Header**:
  - Título `display-lg`.
  - Descripción opcional `body` `muted-foreground` debajo.
  - Acciones a la derecha (filtros, primaryAction).
  - Tabs debajo si la página tiene secciones (`Tabs` shadcn, underline variant: underline 2 px `accent` debajo del tab activo).

---

## 6. Login

> Archivo: `src/app/login/page.tsx`.

### 6.1 Layout
- **Mobile / sm:** una columna, centrada, padding 24 px.
- **Desktop (≥ lg):** dos columnas 50/50.
  - **Izquierda:** panel oscuro (fondo `slate-950`) con:
    - Logo `/logo.png` arriba a la izquierda (h-10 invertido al blanco mediante `filter brightness-0 invert` si seguimos usando la versión negra; alternativa: cargar `/logo-light.png` si se genera).
    - Bloque hero centrado vertical: ilustración decorativa (puede ser solo el tucán de `/icon.png` 280 px) + headline grande (`display-xl` blanco) + tagline (body-lg slate-300).
    - Headlines rotativos sugeridos: "Tu facturación, lista en segundos." / "SUNAT al día. Tú, tranquilo."
    - Footer pequeño: enlaces a [/privacy](src/app/privacy/page.tsx) y [/terms](src/app/terms/page.tsx) en slate-400 caption.
  - **Derecha:** formulario, fondo `background`, padding 64 px.

### 6.2 Formulario
1. Título `display-lg` "Bienvenido de vuelta" + descripción `muted-foreground` "Inicia sesión para gestionar tus comprobantes".
2. **Botón Google primero** (variante `outline`, full-width, alto 48 px) — logo G a la izquierda, texto "Continuar con Google" — porque ya tenemos OAuth y es el camino más rápido.
3. Separador: línea horizontal con etiqueta "o con email" en el centro, `caption` `muted-foreground`.
4. Stack de inputs:
   - **ID de Empresa** (UUID) — `Input md`, helper text "Pídelo a tu administrador o cópialo de tu invitación".
   - **Email** — `Input md` con ícono `Mail` leading.
   - **Contraseña** — `Input md` type password, con ícono `Lock` leading e `Eye/EyeOff` trailing para mostrar.
5. Fila inferior: checkbox "Recordarme" a la izquierda, link "¿Olvidaste tu contraseña?" a la derecha.
6. **Botón primario** "Iniciar sesión" (`default`, full-width, lg 48 px). Estado loading con spinner.
7. Bloque inferior centrado: "¿No tienes cuenta? **Crea una**" con "Crea una" como link en accent.

### 6.3 Estados
- Error → banner rojo arriba del formulario (`bg-danger/10 border-danger text-danger` body-sm con ícono `AlertCircle`).
- Tenant picker (cuando Google login devuelve múltiples tenants) → Dialog ya implementado: lista de cards seleccionables con logo de empresa, nombre comercial, RUC. Selección dispara `loginWithGoogleAtTenant`.

---

## 7. Registro

> Archivo: `src/app/register/page.tsx`. Es el flujo más importante de adquisición. Hoy es un único form largo; lo dividimos en un **wizard de 2 pasos**.

### 7.1 Layout
- Mismo split que login (panel oscuro izquierda con hero, form derecha) — esto mantiene consistencia y refuerza marca.
- En el panel oscuro: además del headline, lista de 3 bullets con `CheckCircle2` accent: "Hasta 100 comprobantes gratis al mes", "Listo para SUNAT producción", "Soporte en Lima".

### 7.2 Stepper
Encima del formulario, **stepper horizontal compacto**:
- Paso 1 — "Tu empresa" (activo: círculo `accent` con número 1 en `ink`).
- Paso 2 — "Tu cuenta" (inactivo: círculo `slate-200` con número 2).
- Línea conectora 2 px entre los dos: progresa a `accent` cuando el paso 1 está completo.

### 7.3 Paso 1 — Empresa
- Botón Google arriba: si autentican con Google, **se salta el paso 2** (el email y nombre vienen del token) y pasan directo a confirmación.
- Separador "o registra con email".
- Inputs:
  - **RUC** — `Input md` mono tabular-nums, `maxLength 11`, helper "Debe tener 11 dígitos y empezar en 10 o 20", validación de longitud + check digit en blur. Al pasar valida → muestra check verde a la derecha + ícono `ShieldCheck`.
  - **Razón Social** — `Input md`, autouppercase visual (vía CSS `text-transform: uppercase`).
  - **Nombre Comercial** — `Input md`, opcional, helper "Cómo se conoce tu negocio (puede ser distinto a la razón social)".
  - **Dirección** — `Input md`, opcional pero sugerida.
- Acciones abajo: link "Ya tengo cuenta" (izq) + Botón `default` "Continuar" (der).

### 7.4 Paso 2 — Cuenta administrador
Si vienen por Google: se saltan estos campos (email/nombre del token) y solo deben aceptar T&C.
- Inputs:
  - **Nombre Completo** — `Input md`, helper "Como aparece en tu DNI".
  - **Email** — `Input md` con ícono `Mail` leading.
  - **Contraseña** — `Input md` type password, helper meter de fuerza (4 segmentos, weak/fair/good/strong), validador inline: 8+ chars, 1 mayúscula, 1 número.
  - **Confirmar contraseña** — `Input md`.
- Checkbox **obligatorio**: "Acepto los [Términos de Servicio](/terms) y la [Política de Privacidad](/privacy)" — link en accent.
- Checkbox opcional: "Quiero recibir novedades por email".
- Acciones: link "Volver" (izq) + Botón `accent` "Crear cuenta" (der, weight 600).

### 7.5 Confirmación post-registro
Card centrada, ilustración tucán con sobre + check, título "¡Bienvenido a TukiFact!" + body "Te enviamos un correo para verificar tu email. Mientras tanto, configuremos tu empresa." + CTA primario "Continuar" → lleva a `/welcome` (onboarding).

---

## 8. Dashboard

> Archivo: `src/app/(authenticated)/dashboard/page.tsx`.

### 8.1 Layout vertical (tope a fondo)
1. **Page header**
   - H1 `display-lg` "Hola, {nombre} 👋" — sin emoji si el usuario tiene `prefers-reduced-motion`.
   - Subtítulo `body` `muted-foreground` con la fecha actual en español: "Lunes, 28 de mayo".
   - Acciones derecha: Botón `outline` "Exportar" + Botón `accent` "Emitir comprobante".
2. **Fila KPIs** (grid 4 cols desktop, 2 cols tablet, 1 col mobile, gap 16 px). Cards KPI con:
   - Eyebrow `overline` ("VENTAS DEL MES", "COMPROBANTES EMITIDOS", "PENDIENTES SUNAT", "RECHAZOS").
   - Value `num-lg`. Para soles, prefijo `S/` en `body` muted, value tabular-nums.
   - Delta vs período anterior: pill pequeño con flecha + porcentaje (verde si positivo en ventas y emitidos, rojo si positivo en rechazos).
   - Sparkline mini (60 px alto, color matching: `accent` para ventas, `info` para emitidos, `warning` para pendientes, `danger` para rechazos).
3. **Fila 2 — split 2/3 + 1/3**
   - **Card izquierda (2/3)** "Ventas últimos 30 días" — chart de barras (Recharts) por día, color `brand-toucan-yellow`, hover muestra tooltip con total + nº comprobantes. Tabs arriba para switch 7d / 30d / 90d.
   - **Card derecha (1/3)** "Estado SUNAT" — donut chart: aceptados (success), pendientes (warning), rechazados (danger), anulados (slate). Leyenda debajo con valores tabulares.
4. **Fila 3 — split 1/2 + 1/2**
   - **Últimos comprobantes** — tabla compacta (5 filas): serie-número (mono), tipo (badge), cliente (truncate), total `num-md` derecha, estado (badge). Link "Ver todos" en footer.
   - **Clientes top del mes** — lista: avatar con iniciales, nombre, número de comprobantes, total (mono, derecha).
5. **Fila 4 — Asistente IA destacado**
   - Card ancho completo `bg-gradient-to-br from-slate-900 to-slate-800` (oscuro) con borde 1 px `accent/20`.
   - Izquierda: ícono `Sparkles` 32 px `accent` + título "Pregúntale a TukiFact" + body "Genera reportes, busca comprobantes o resuelve dudas SUNAT en lenguaje natural" + CTA `accent` "Abrir asistente".
   - Derecha (desktop): 3 chips de ejemplos clicables: "Resumen del mes", "Comprobantes rechazados hoy", "Cómo emitir nota de crédito".

### 8.2 Empty state (primer login)
Si no hay datos, los KPIs muestran `—` y el área principal se reemplaza por **onboarding inline**: 4 cards en grid 2×2 con checklist de pasos: "Sube tu certificado digital", "Configura tu serie", "Carga tu primer cliente", "Emite tu primer comprobante". Cada card con ícono, título, descripción 2 líneas, estado (pendiente/listo con check), CTA pequeño.

---

## 9. Páginas de contenido (patrón)

Aplica a: documents, products, customers, quotations, despatch-advices, recurring-invoices, voided.

### 9.1 Estructura
1. **Page header**
   - H1 + descripción + acciones derecha (filtros, importar, **CTA primario** ej. "Nuevo cliente").
2. **Filtros / toolbar** (card o barra)
   - Search input `sm` (320 px) izquierda con ícono `Search`.
   - Chips de filtros aplicables (Estado, Tipo, Fechas, Cliente). Active state: chip relleno `accent`. Inactive: outline.
   - DateRangePicker (sheet/popover) a la derecha + botón ghost "Limpiar filtros" si hay activos.
3. **Tabla** (sección 4.5).
4. **Bulk actions bar** (sticky bottom, aparece cuando hay rows seleccionadas): "{N} seleccionados" + acciones (Exportar, Anular, Eliminar).
5. **Empty state** cuando no hay registros que matcheen filtros: ilustración + "No encontramos resultados" + "Limpiar filtros" (ghost).
6. **Empty state global** cuando la cuenta no tiene NINGÚN registro: ilustración + descripción del concepto + CTA primario para crear el primero.

### 9.2 Página de detalle / nuevo / editar
- Layout 2 columnas desktop: contenido principal (2/3) + sidebar de acciones rápidas (1/3) con resumen + botones.
- En mobile: una columna, sidebar al fondo.
- **Sticky save bar** abajo con shadow-up cuando hay cambios sin guardar: "{X} cambios sin guardar" + Botón ghost "Descartar" + Botón `default` "Guardar".

---

## 10. Estados globales

### 10.1 Loading
- Skeletons en lugar de spinners centrados (excepto en login button).
- Topbar muestra una barra de progreso indeterminada `accent` 2 px en transiciones de página (similar a NProgress).

### 10.2 Error
- Banner inline al inicio del content area: `bg-danger/10 border-danger`, ícono `AlertOctagon`, título "Algo salió mal", descripción + botón ghost "Reintentar".
- Errores de página completa (5xx, 404): ilustración tucán con expresión confundida + código + descripción + CTA "Volver al dashboard".

### 10.3 Vacío
- Patrón en sección 4.9. Siempre con CTA.

### 10.4 Offline / NATS down
- Toast persistente arriba: "Estamos teniendo problemas para conectarnos. Reintentando..." color warning.

---

## 11. Responsive

Breakpoints (Tailwind defaults): `sm 640`, `md 768`, `lg 1024`, `xl 1280`, `2xl 1536`.

| Zona | < md | md – lg | ≥ lg |
|---|---|---|---|
| Aside | drawer (Sheet) abierto desde hamburguesa | drawer | fijo expandido / colapsado |
| Topbar search | ícono que abre sheet de search | input compacto | input completo |
| KPIs grid | 1 col | 2 col | 4 col |
| Tablas | cards apiladas con label arriba de cada valor | tabla scroll-x | tabla normal |
| Login/Register | 1 col | 1 col | 2 col split |
| Formularios | 1 col | 1 col | 2 col donde aplique |

---

## 12. Accesibilidad

- **Contraste mínimo:** AA (4.5:1 para texto normal, 3:1 para grande). El amarillo TukiFact sobre blanco NO cumple AA para texto — solo se usa como fondo con texto `ink` encima, o como acento decorativo (barras, íconos).
- **Foco visible siempre.** Ring amarillo 3 px nunca se remueve. En oscuro mantenemos el mismo ring.
- **Targets táctiles:** mínimo 40×40 px en mobile.
- **Form labels:** siempre asociadas con `htmlFor`. Helper text con `aria-describedby`. Errores con `aria-invalid` y mensaje asociado.
- **Tablas:** `<th scope="col">`. Sorting button con `aria-sort`.
- **Dialogs:** trap de foco, cerrar con Esc, retornar foco al trigger.
- **Iconos decorativos:** `aria-hidden="true"`. Iconos sin label visible: `aria-label`.
- **Tabular nums** para todos los números: `font-variant-numeric: tabular-nums`.
- **Idioma del documento:** `<html lang="es-PE">`.

---

## 13. Microinteracciones

- Botones primarios: scale 0.98 en press (`active:scale-[0.98] transition-transform duration-100`).
- Cards clicables: subtle lift `hover:-translate-y-0.5 hover:shadow-md transition`.
- Iconos del aside activo: pulse muy sutil al cargar la página (1 ciclo, 200 ms, scale 1 → 1.05 → 1).
- Toast de éxito tras emitir comprobante: confeti minimalista (5 partículas en `accent`+`brand-toucan-orange`, 600 ms). Respeta reduced-motion.

---

## 14. Tone & illustration

- **Ilustraciones secundarias** (empty states, errores): estilo *flat* con los mismos 4 colores del logo (negro, amarillo, naranja, blanco hueso). Stroke 2 px, esquinas suaves.
- **Mascot:** el tucán puede aparecer ocasionalmente en pantallas vacías y en confirmaciones — con expresiones (saludo, confundido, dormido). Usar con moderación: una vez por sesión máxima en cada estado, no en pantallas operativas (lista de comprobantes, formularios de emisión).

---

## 15. Checklist de implementación

Antes de mergear cualquier pantalla nueva:
- [ ] Usa tokens semánticos, NO hex directos.
- [ ] H1 único por página.
- [ ] Una sola acción `default` o `accent` primaria por vista.
- [ ] Estados: loading (skeleton), empty (con CTA), error (con reintentar).
- [ ] Responsive verificado en 375 px, 768 px, 1280 px.
- [ ] Foco visible en todos los interactivos.
- [ ] Labels y helpers en español Perú.
- [ ] Números fiscales en `tabular-nums` mono.
- [ ] Lighthouse a11y ≥ 95.

---

## 16. Roadmap de migración visual

Orden sugerido al refactorizar lo existente:
1. **Tokens** — actualizar `globals.css` con la paleta marca (sección 2). Esto repinta todo.
2. **Shell autenticado** — `(authenticated)/layout.tsx`: nuevo aside con switcher de empresa y CTA primario, nuevo topbar con breadcrumb + search global.
3. **Login** — split panel oscuro/claro.
4. **Registro** — wizard 2 pasos con stepper.
5. **Dashboard** — KPIs nuevos + chart + sección IA.
6. **Páginas de contenido** — pasar todas al patrón sección 9 (documents, products, customers primero).
7. **Settings / Plan / Certificate / Users** — formularios al patrón sección 9.2.
8. **AI / Welcome** — refinamiento final.

---

**Versión:** 1.0 (Cliente)
**Última actualización:** 2026-05-28
**Mantenedor:** Equipo TukiFact
