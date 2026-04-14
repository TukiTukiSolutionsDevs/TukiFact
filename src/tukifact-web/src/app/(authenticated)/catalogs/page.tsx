'use client';

import { useEffect, useState, useCallback } from 'react';
import { api } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';
import {
  Accordion, AccordionContent, AccordionItem, AccordionTrigger,
} from '@/components/ui/accordion';
import { BookOpen, Search, Hash, ListTree, Loader2 } from 'lucide-react';

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

  useEffect(() => { fetchCatalogs(); }, [fetchCatalogs]);

  const loadCatalogCodes = async (catalogNumber: string) => {
    if (catalogDetails[catalogNumber]) return;
    setLoadingCatalog(catalogNumber);
    try {
      const detail = await api.get<CatalogDetail>(`/v1/catalogs/${catalogNumber}`);
      setCatalogDetails(prev => ({ ...prev, [catalogNumber]: detail }));
    } catch (err) {
      console.error(`Error loading catalog ${catalogNumber}:`, err);
    } finally {
      setLoadingCatalog('');
    }
  };

  const handleAccordionChange = (value: number[]) => {
    setExpandedCatalog(value);
    if (value.length > 0) {
      const idx = value[value.length - 1];
      const cat = filteredCatalogs[idx];
      if (cat) loadCatalogCodes(cat.catalogNumber);
    }
    setCodeSearch('');
  };

  const filteredCatalogs = catalogs.filter(c =>
    !searchTerm ||
    c.catalogNumber.includes(searchTerm) ||
    c.name.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const totalCodes = catalogs.reduce((sum, c) => sum + c.codesCount, 0);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Catalogos SUNAT</h1>
          <p className="text-muted-foreground">
            Tablas de referencia para facturacion electronica
          </p>
        </div>
      </div>

      {/* Stats */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Catalogos</CardTitle>
            <BookOpen className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{catalogs.length}</div>
            <p className="text-xs text-muted-foreground">tablas de referencia</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Codigos</CardTitle>
            <Hash className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{totalCodes}</div>
            <p className="text-xs text-muted-foreground">codigos activos</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Detracciones</CardTitle>
            <ListTree className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{detractions.length}</div>
            <p className="text-xs text-muted-foreground">codigos SPOT activos</p>
          </CardContent>
        </Card>
      </div>

      {/* Search */}
      <div className="relative">
        <Search className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
        <Input
          placeholder="Buscar catalogo por numero o nombre..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="pl-9"
        />
      </div>

      {/* Catalogs Accordion */}
      {isLoading ? (
        <Card>
          <CardContent className="py-12 text-center">
            <Loader2 className="mx-auto h-8 w-8 animate-spin text-muted-foreground" />
            <p className="mt-2 text-muted-foreground">Cargando catalogos...</p>
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardHeader>
            <CardTitle>Catalogos de Facturacion</CardTitle>
            <CardDescription>
              Click en un catalogo para ver sus codigos. {filteredCatalogs.length} catalogos encontrados.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Accordion
              value={expandedCatalog}
              onValueChange={handleAccordionChange}
            >
              {filteredCatalogs.map((catalog, index) => (
                <AccordionItem key={catalog.catalogNumber} value={index}>
                  <AccordionTrigger className="hover:no-underline">
                    <div className="flex items-center gap-3 text-left">
                      <Badge variant="secondary" className="font-mono shrink-0">
                        N.° {catalog.catalogNumber}
                      </Badge>
                      <div>
                        <span className="font-medium">{catalog.name}</span>
                        {catalog.description && (
                          <p className="text-xs text-muted-foreground mt-0.5">
                            {catalog.description}
                          </p>
                        )}
                      </div>
                      <Badge variant="outline" className="ml-auto shrink-0">
                        {catalog.codesCount} codigos
                      </Badge>
                    </div>
                  </AccordionTrigger>
                  <AccordionContent>
                    {loadingCatalog === catalog.catalogNumber ? (
                      <div className="flex items-center justify-center py-6">
                        <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
                        <span className="ml-2 text-sm text-muted-foreground">Cargando codigos...</span>
                      </div>
                    ) : catalogDetails[catalog.catalogNumber] ? (
                      <div className="space-y-3">
                        <div className="relative">
                          <Search className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
                          <Input
                            placeholder="Filtrar codigos..."
                            value={codeSearch}
                            onChange={(e) => setCodeSearch(e.target.value)}
                            className="pl-9 h-8 text-sm"
                          />
                        </div>
                        <div className="max-h-[400px] overflow-y-auto rounded-md border">
                          <Table>
                            <TableHeader>
                              <TableRow>
                                <TableHead className="w-[100px]">Codigo</TableHead>
                                <TableHead>Descripcion</TableHead>
                              </TableRow>
                            </TableHeader>
                            <TableBody>
                              {catalogDetails[catalog.catalogNumber].codes
                                .filter(c =>
                                  !codeSearch ||
                                  c.code.includes(codeSearch) ||
                                  c.description.toLowerCase().includes(codeSearch.toLowerCase())
                                )
                                .map((code) => (
                                  <TableRow key={code.code}>
                                    <TableCell className="font-mono font-medium">
                                      {code.code}
                                    </TableCell>
                                    <TableCell>{code.description}</TableCell>
                                  </TableRow>
                                ))}
                            </TableBody>
                          </Table>
                        </div>
                      </div>
                    ) : null}
                  </AccordionContent>
                </AccordionItem>
              ))}
            </Accordion>
          </CardContent>
        </Card>
      )}

      {/* Detraction Codes */}
      {detractions.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <ListTree className="h-5 w-5" />
              Codigos de Detraccion (SPOT)
            </CardTitle>
            <CardDescription>
              Sistema de Pago de Obligaciones Tributarias — Catalogo N.° 54
            </CardDescription>
          </CardHeader>
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[80px]">Codigo</TableHead>
                  <TableHead>Descripcion</TableHead>
                  <TableHead className="w-[100px] text-right">Porcentaje</TableHead>
                  <TableHead className="w-[80px]">Anexo</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {detractions.map((d) => (
                  <TableRow key={d.code}>
                    <TableCell className="font-mono font-medium">{d.code}</TableCell>
                    <TableCell>{d.description}</TableCell>
                    <TableCell className="text-right">
                      <Badge variant="secondary">{d.percentage}%</Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline">{d.annex}</Badge>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
