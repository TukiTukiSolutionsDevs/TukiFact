'use client';

import { useEffect, useState, useCallback, useMemo } from 'react';
import { api } from '@/lib/api';
import { Input } from '@/components/ui/input';
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '@/components/ui/accordion';
import {
  BookOpen,
  Search,
  Hash,
  Receipt,
  Loader2,
  ChevronRight,
  Info,
  Inbox,
} from 'lucide-react';

interface CatalogSummary {
  catalogNumber: string;
  name: string;
  description: string | null;
  codesCount: number;
}

interface CatalogCode {
  code: string;
  description: string;
}

interface CatalogDetail {
  catalogNumber: string;
  name: string;
  description: string | null;
  codes: CatalogCode[];
}

interface DetractionCode {
  code: string;
  description: string;
  percentage: number;
  annex: string;
}

const fmt = (n: number) => new Intl.NumberFormat('es-PE').format(n);

function Kpi({
  eyebrow,
  value,
  caption,
  icon: Icon,
  color,
}: {
  eyebrow: string;
  value: number;
  caption: string;
  icon: React.ElementType;
  color: string;
}) {
  return (
    <div
      className="rounded-[var(--radius-lg)] border bg-card p-5 flex flex-col gap-2.5"
      style={{ boxShadow: 'var(--shadow-xs)' }}
    >
      <div className="flex items-start justify-between gap-2">
        <span className="t-overline" style={{ color: 'var(--muted-foreground)' }}>
          {eyebrow}
        </span>
        <span
          className="flex h-7 w-7 items-center justify-center rounded-md"
          style={{ background: `color-mix(in oklch, ${color} 14%, transparent)`, color }}
        >
          <Icon className="h-3.5 w-3.5" />
        </span>
      </div>
      <span className="t-num-lg">{fmt(value)}</span>
      <span className="t-caption" style={{ color: 'var(--muted-foreground)' }}>
        {caption}
      </span>
    </div>
  );
}

export default function CatalogsPage() {
  const [catalogs, setCatalogs] = useState<CatalogSummary[]>([]);
  const [detractions, setDetractions] = useState<DetractionCode[]>([]);
  const [expandedCatalog, setExpandedCatalog] = useState<number[]>([]);
  const [catalogDetails, setCatalogDetails] = useState<Record<string, CatalogDetail>>({});
  const [loadingCatalog, setLoadingCatalog] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [codeSearch, setCodeSearch] = useState('');

  const fetchCatalogs = useCallback(async () => {
    setIsLoading(true);
    try {
      const [cats, dets] = await Promise.all([
        api.get<CatalogSummary[]>('/v1/catalogs'),
        api.get<DetractionCode[]>('/v1/catalogs/detractions'),
      ]);
      setCatalogs(cats);
      setDetractions(dets);
    } catch (err) {
      console.error('Error loading catalogs:', err);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchCatalogs();
  }, [fetchCatalogs]);

  const loadCatalogCodes = async (catalogNumber: string) => {
    if (catalogDetails[catalogNumber]) return;
    setLoadingCatalog(catalogNumber);
    try {
      const detail = await api.get<CatalogDetail>(`/v1/catalogs/${catalogNumber}`);
      setCatalogDetails((prev) => ({ ...prev, [catalogNumber]: detail }));
    } catch (err) {
      console.error(`Error loading catalog ${catalogNumber}:`, err);
    } finally {
      setLoadingCatalog('');
    }
  };

  const filteredCatalogs = useMemo(
    () =>
      catalogs.filter(
        (c) =>
          !searchTerm ||
          c.catalogNumber.includes(searchTerm) ||
          c.name.toLowerCase().includes(searchTerm.toLowerCase())
      ),
    [catalogs, searchTerm]
  );

  const handleAccordionChange = (value: number[]) => {
    setExpandedCatalog(value);
    if (value.length > 0) {
      const idx = value[value.length - 1]!;
      const cat = filteredCatalogs[idx];
      if (cat) loadCatalogCodes(cat.catalogNumber);
    }
    setCodeSearch('');
  };

  const totalCodes = catalogs.reduce((sum, c) => sum + c.codesCount, 0);

  const isEmpty = !isLoading && catalogs.length === 0 && detractions.length === 0;

  return (
    <div>
      {/* Page header */}
      <div className="flex items-start justify-between gap-4 flex-wrap mb-6">
        <div className="min-w-0">
          <h1 className="t-display-lg m-0">Catálogos SUNAT</h1>
          <p className="t-body mt-1.5 mb-0" style={{ color: 'var(--muted-foreground)' }}>
            Tablas de referencia oficiales para tu facturación electrónica.
          </p>
        </div>
      </div>

      {/* KPIs */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-[var(--gap-cards)] mb-[var(--gap-cards)]">
        <Kpi
          eyebrow="CATÁLOGOS"
          value={catalogs.length}
          caption="tablas de referencia"
          icon={BookOpen}
          color="var(--info)"
        />
        <Kpi
          eyebrow="CÓDIGOS"
          value={totalCodes}
          caption="códigos activos"
          icon={Hash}
          color="var(--brand-toucan-yellow)"
        />
        <Kpi
          eyebrow="DETRACCIONES"
          value={detractions.length}
          caption="códigos SPOT activos"
          icon={Receipt}
          color="var(--brand-toucan-orange)"
        />
      </div>

      {isLoading ? (
        <div
          className="rounded-[var(--radius-lg)] border bg-card p-6"
          style={{ boxShadow: 'var(--shadow-xs)' }}
        >
          <div className="flex items-center gap-3 text-[var(--muted-foreground)]">
            <Loader2 className="h-4 w-4 animate-spin" />
            <span className="t-body-sm">Cargando catálogos…</span>
          </div>
        </div>
      ) : isEmpty ? (
        <div
          className="rounded-[var(--radius-lg)] border bg-card overflow-hidden text-center"
          style={{ boxShadow: 'var(--shadow-xs)' }}
        >
          <div className="p-10">
            <div
              className="mx-auto mb-4 flex h-16 w-16 items-center justify-center rounded-2xl"
              style={{ background: 'color-mix(in oklch, var(--info) 14%, transparent)' }}
            >
              <Inbox className="h-8 w-8" style={{ color: 'var(--info)' }} />
            </div>
            <h2 className="t-h1 m-0">Aún no hay catálogos cargados</h2>
            <p
              className="t-body mt-2 mb-0 max-w-[480px] mx-auto"
              style={{ color: 'var(--muted-foreground)' }}
            >
              Los catálogos SUNAT (códigos de IGV, tipos de comprobante, unidades de medida,
              monedas, etc.) se cargan cuando tu cuenta queda activa. Si necesitas usarlos antes,
              contáctanos.
            </p>
          </div>
        </div>
      ) : (
        <>
          {/* Search */}
          <div className="relative mb-[var(--gap-cards)]">
            <Search
              className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 pointer-events-none"
              style={{ color: 'var(--muted-foreground)' }}
            />
            <Input
              placeholder="Buscar catálogo por número o nombre…"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-9"
            />
          </div>

          {/* Catalogs accordion */}
          {filteredCatalogs.length === 0 ? (
            <div
              className="rounded-[var(--radius-lg)] border bg-card p-8 text-center"
              style={{ boxShadow: 'var(--shadow-xs)' }}
            >
              <Search
                className="h-8 w-8 mx-auto mb-2"
                style={{ color: 'var(--slate-400)' }}
              />
              <p className="t-body m-0 font-semibold">Sin resultados</p>
              <p className="t-body-sm mt-1 mb-0" style={{ color: 'var(--muted-foreground)' }}>
                No encontramos catálogos para "{searchTerm}".
              </p>
            </div>
          ) : (
            <section
              className="rounded-[var(--radius-lg)] border bg-card overflow-hidden mb-[var(--gap-cards)]"
              style={{ boxShadow: 'var(--shadow-xs)' }}
            >
              <header
                className="px-6 py-4 border-b"
                style={{ borderColor: 'var(--border)' }}
              >
                <h2 className="t-h2 m-0">Catálogos de facturación</h2>
                <p
                  className="t-body-sm m-0 mt-0.5"
                  style={{ color: 'var(--muted-foreground)' }}
                >
                  {filteredCatalogs.length}{' '}
                  {filteredCatalogs.length === 1 ? 'catálogo' : 'catálogos'}. Haz click para ver
                  sus códigos.
                </p>
              </header>

              <Accordion value={expandedCatalog} onValueChange={handleAccordionChange}>
                {filteredCatalogs.map((catalog, index) => {
                  const detail = catalogDetails[catalog.catalogNumber];
                  return (
                    <AccordionItem
                      key={catalog.catalogNumber}
                      value={index}
                      className="border-0"
                      style={{ borderTop: index > 0 ? '1px solid var(--border)' : undefined }}
                    >
                      <AccordionTrigger className="hover:no-underline px-6 py-4">
                        <div className="flex items-center gap-3 text-left w-full">
                          <span
                            className="mono t-caption font-bold tnum px-2.5 py-0.5 rounded-full shrink-0"
                            style={{
                              background: 'var(--muted)',
                              color: 'var(--foreground)',
                            }}
                          >
                            {catalog.catalogNumber}
                          </span>
                          <div className="min-w-0 flex-1">
                            <span className="t-body font-semibold">{catalog.name}</span>
                            {catalog.description && (
                              <p
                                className="t-caption m-0 mt-0.5"
                                style={{ color: 'var(--muted-foreground)' }}
                              >
                                {catalog.description}
                              </p>
                            )}
                          </div>
                          <span
                            className="t-caption tnum font-semibold shrink-0 px-2.5 py-0.5 rounded-full"
                            style={{
                              background: 'color-mix(in oklch, var(--info) 12%, transparent)',
                              color: 'var(--info)',
                            }}
                          >
                            {fmt(catalog.codesCount)} {catalog.codesCount === 1 ? 'código' : 'códigos'}
                          </span>
                        </div>
                      </AccordionTrigger>
                      <AccordionContent>
                        <div className="px-6 pb-6">
                          {loadingCatalog === catalog.catalogNumber ? (
                            <div
                              className="flex items-center gap-2 py-4"
                              style={{ color: 'var(--muted-foreground)' }}
                            >
                              <Loader2 className="h-4 w-4 animate-spin" />
                              <span className="t-body-sm">Cargando códigos…</span>
                            </div>
                          ) : detail ? (
                            <>
                              <div className="relative mb-3">
                                <Search
                                  className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 pointer-events-none"
                                  style={{ color: 'var(--muted-foreground)' }}
                                />
                                <Input
                                  placeholder="Filtrar códigos…"
                                  value={codeSearch}
                                  onChange={(e) => setCodeSearch(e.target.value)}
                                  className="pl-9"
                                />
                              </div>
                              <div
                                className="max-h-[400px] overflow-y-auto rounded-[var(--radius-md)] border"
                                style={{ borderColor: 'var(--border)' }}
                              >
                                <table className="w-full">
                                  <thead className="sticky top-0">
                                    <tr
                                      className="t-overline"
                                      style={{
                                        color: 'var(--muted-foreground)',
                                        background: 'var(--muted)',
                                      }}
                                    >
                                      <th className="text-left py-2.5 pl-4 pr-2 w-[120px]">
                                        Código
                                      </th>
                                      <th className="text-left py-2.5 px-2">Descripción</th>
                                    </tr>
                                  </thead>
                                  <tbody>
                                    {detail.codes
                                      .filter(
                                        (c) =>
                                          !codeSearch ||
                                          c.code.includes(codeSearch) ||
                                          c.description
                                            .toLowerCase()
                                            .includes(codeSearch.toLowerCase())
                                      )
                                      .map((code, i) => (
                                        <tr
                                          key={code.code}
                                          style={{
                                            borderTop: i > 0 ? '1px solid var(--border)' : undefined,
                                          }}
                                        >
                                          <td className="py-2.5 pl-4 pr-2 mono t-body-sm font-semibold">
                                            {code.code}
                                          </td>
                                          <td
                                            className="py-2.5 px-2 t-body-sm"
                                            style={{ color: 'var(--foreground)' }}
                                          >
                                            {code.description}
                                          </td>
                                        </tr>
                                      ))}
                                  </tbody>
                                </table>
                              </div>
                            </>
                          ) : null}
                        </div>
                      </AccordionContent>
                    </AccordionItem>
                  );
                })}
              </Accordion>
            </section>
          )}

          {/* Detractions */}
          {detractions.length > 0 && (
            <section
              className="rounded-[var(--radius-lg)] border bg-card overflow-hidden"
              style={{ boxShadow: 'var(--shadow-xs)' }}
            >
              <header
                className="px-6 py-4 border-b flex items-center gap-3"
                style={{ borderColor: 'var(--border)' }}
              >
                <span
                  className="flex h-9 w-9 items-center justify-center rounded-md"
                  style={{
                    background: 'color-mix(in oklch, var(--brand-toucan-orange) 14%, transparent)',
                    color: 'var(--brand-toucan-orange)',
                  }}
                >
                  <Receipt className="h-4 w-4" />
                </span>
                <div className="flex-1 min-w-0">
                  <h2 className="t-h2 m-0">Códigos de detracción (SPOT)</h2>
                  <p className="t-caption m-0" style={{ color: 'var(--muted-foreground)' }}>
                    Sistema de Pago de Obligaciones Tributarias · Catálogo N.° 54
                  </p>
                </div>
              </header>
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr
                      className="t-overline"
                      style={{ color: 'var(--muted-foreground)', background: 'var(--muted)' }}
                    >
                      <th className="text-left py-2.5 pl-6 pr-2 w-[100px]">Código</th>
                      <th className="text-left py-2.5 px-2">Descripción</th>
                      <th className="text-right py-2.5 px-2 w-[110px]">Porcentaje</th>
                      <th className="text-left py-2.5 pr-6 pl-2 w-[90px]">Anexo</th>
                    </tr>
                  </thead>
                  <tbody>
                    {detractions.map((d, i) => (
                      <tr
                        key={d.code}
                        style={{ borderTop: i > 0 ? '1px solid var(--border)' : undefined }}
                      >
                        <td className="py-2.5 pl-6 pr-2 mono t-body-sm font-semibold">{d.code}</td>
                        <td className="py-2.5 px-2 t-body-sm">{d.description}</td>
                        <td className="py-2.5 px-2 text-right mono tnum t-body-sm font-semibold">
                          {d.percentage}%
                        </td>
                        <td className="py-2.5 pr-6 pl-2">
                          <span
                            className="t-caption font-semibold px-2 py-0.5 rounded-full"
                            style={{
                              background: 'var(--muted)',
                              color: 'var(--muted-foreground)',
                            }}
                          >
                            {d.annex}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>
          )}

          {/* Helper note */}
          <div
            className="mt-[var(--gap-cards)] rounded-[var(--radius-lg)] border p-4 flex items-start gap-3"
            style={{
              background: 'color-mix(in oklch, var(--info) 6%, transparent)',
              borderColor: 'color-mix(in oklch, var(--info) 25%, transparent)',
            }}
          >
            <Info className="h-5 w-5 shrink-0 mt-0.5" style={{ color: 'var(--info)' }} />
            <div>
              <p className="t-body-sm m-0 font-semibold">
                Los catálogos los gestiona SUNAT, no tú.
              </p>
              <p className="t-body-sm m-0 mt-0.5" style={{ color: 'var(--muted-foreground)' }}>
                Esta página es solo de consulta. Cuando emites un comprobante, TukiFact valida
                automáticamente que los códigos que uses estén vigentes.
              </p>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
