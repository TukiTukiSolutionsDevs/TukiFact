-- Catálogos SUNAT principales para facturación electrónica
-- Fuente: SUNAT cpe.sunat.gob.pe

-- Catálogos
INSERT INTO sunat_catalogs ("CatalogNumber", "Name", "Description", "IsActive") VALUES
('01', 'Tipo de Documento', 'Código de tipo de documento', true),
('06', 'Tipo de Documento de Identidad', 'Código de tipo de documento de identidad', true),
('07', 'Tipo de Afectación del IGV', 'Código de tipo de afectación del IGV por ítem', true),
('09', 'Tipo de Nota de Crédito', 'Código de tipo de nota de crédito', true),
('10', 'Tipo de Nota de Débito', 'Código de tipo de nota de débito', true),
('12', 'Código de Documento Relacionado', 'Tipo de documento relacionado', true),
('15', 'Código de Elemento Adicional', 'Elementos adicionales de la factura/boleta', true),
('16', 'Tipo de Precio de Venta', 'Código de tipo de precio de venta unitario', true),
('20', 'Motivo de Traslado', 'Código de motivo de traslado (GRE)', true),
('51', 'Tipo de Operación', 'Código de tipo de operación', true),
('52', 'Leyendas', 'Código de leyendas (notas especiales)', true),
('54', 'Códigos de Detracción', 'Bienes y servicios sujetos a detracción SPOT', true)
ON CONFLICT ("CatalogNumber") DO UPDATE SET
  "Name" = EXCLUDED."Name",
  "Description" = EXCLUDED."Description";

-- Cat. 01 — Tipo de Documento
INSERT INTO sunat_catalog_codes ("Id", "CatalogNumber", "Code", "Description", "SortOrder", "IsActive") VALUES
(gen_random_uuid(), '01', '01', 'Factura', 1, true),
(gen_random_uuid(), '01', '03', 'Boleta de Venta', 2, true),
(gen_random_uuid(), '01', '07', 'Nota de Crédito', 3, true),
(gen_random_uuid(), '01', '08', 'Nota de Débito', 4, true),
(gen_random_uuid(), '01', '09', 'Guía de Remisión Remitente', 5, true),
(gen_random_uuid(), '01', '20', 'Comprobante de Retención', 6, true),
(gen_random_uuid(), '01', '31', 'Guía de Remisión Transportista', 7, true),
(gen_random_uuid(), '01', '40', 'Comprobante de Percepción', 8, true)
ON CONFLICT ("CatalogNumber", "Code") DO NOTHING;

-- Cat. 06 — Tipo de Documento de Identidad
INSERT INTO sunat_catalog_codes ("Id", "CatalogNumber", "Code", "Description", "SortOrder", "IsActive") VALUES
(gen_random_uuid(), '06', '0', 'Sin documento / Otros', 1, true),
(gen_random_uuid(), '06', '1', 'DNI', 2, true),
(gen_random_uuid(), '06', '4', 'Carnet de Extranjería', 3, true),
(gen_random_uuid(), '06', '6', 'RUC', 4, true),
(gen_random_uuid(), '06', '7', 'Pasaporte', 5, true),
(gen_random_uuid(), '06', 'A', 'Cédula Diplomática', 6, true),
(gen_random_uuid(), '06', 'B', 'Doc. de identidad país de residencia', 7, true)
ON CONFLICT ("CatalogNumber", "Code") DO NOTHING;

-- Cat. 07 — Tipo de Afectación del IGV
INSERT INTO sunat_catalog_codes ("Id", "CatalogNumber", "Code", "Description", "SortOrder", "IsActive") VALUES
(gen_random_uuid(), '07', '10', 'Gravado - Operación Onerosa', 1, true),
(gen_random_uuid(), '07', '11', 'Gravado - Retiro por premio', 2, true),
(gen_random_uuid(), '07', '12', 'Gravado - Retiro por donación', 3, true),
(gen_random_uuid(), '07', '13', 'Gravado - Retiro', 4, true),
(gen_random_uuid(), '07', '14', 'Gravado - Retiro por publicidad', 5, true),
(gen_random_uuid(), '07', '15', 'Gravado - Bonificaciones', 6, true),
(gen_random_uuid(), '07', '16', 'Gravado - Retiro por entrega a trabajadores', 7, true),
(gen_random_uuid(), '07', '20', 'Exonerado - Operación Onerosa', 8, true),
(gen_random_uuid(), '07', '21', 'Exonerado - Transferencia Gratuita', 9, true),
(gen_random_uuid(), '07', '30', 'Inafecto - Operación Onerosa', 10, true),
(gen_random_uuid(), '07', '31', 'Inafecto - Retiro por Bonificación', 11, true),
(gen_random_uuid(), '07', '32', 'Inafecto - Retiro', 12, true),
(gen_random_uuid(), '07', '33', 'Inafecto - Retiro por Muestras Médicas', 13, true),
(gen_random_uuid(), '07', '34', 'Inafecto - Retiro por Convenio Colectivo', 14, true),
(gen_random_uuid(), '07', '35', 'Inafecto - Retiro por premio', 15, true),
(gen_random_uuid(), '07', '36', 'Inafecto - Retiro por publicidad', 16, true),
(gen_random_uuid(), '07', '40', 'Exportación de Bienes o Servicios', 17, true)
ON CONFLICT ("CatalogNumber", "Code") DO NOTHING;

-- Cat. 09 — Tipo de Nota de Crédito
INSERT INTO sunat_catalog_codes ("Id", "CatalogNumber", "Code", "Description", "SortOrder", "IsActive") VALUES
(gen_random_uuid(), '09', '01', 'Anulación de la operación', 1, true),
(gen_random_uuid(), '09', '02', 'Anulación por error en el RUC', 2, true),
(gen_random_uuid(), '09', '03', 'Corrección por error en la descripción', 3, true),
(gen_random_uuid(), '09', '04', 'Descuento global', 4, true),
(gen_random_uuid(), '09', '05', 'Descuento por ítem', 5, true),
(gen_random_uuid(), '09', '06', 'Devolución total', 6, true),
(gen_random_uuid(), '09', '07', 'Devolución por ítem', 7, true),
(gen_random_uuid(), '09', '08', 'Bonificación', 8, true),
(gen_random_uuid(), '09', '09', 'Disminución en el valor', 9, true),
(gen_random_uuid(), '09', '10', 'Otros conceptos', 10, true),
(gen_random_uuid(), '09', '11', 'Ajustes de operaciones de exportación', 11, true),
(gen_random_uuid(), '09', '12', 'Ajustes afectos al IVAP', 12, true),
(gen_random_uuid(), '09', '13', 'Corrección del monto neto pendiente de pago', 13, true)
ON CONFLICT ("CatalogNumber", "Code") DO NOTHING;

-- Cat. 10 — Tipo de Nota de Débito
INSERT INTO sunat_catalog_codes ("Id", "CatalogNumber", "Code", "Description", "SortOrder", "IsActive") VALUES
(gen_random_uuid(), '10', '01', 'Intereses por mora', 1, true),
(gen_random_uuid(), '10', '02', 'Aumento en el valor', 2, true),
(gen_random_uuid(), '10', '03', 'Penalidades / otros conceptos', 3, true),
(gen_random_uuid(), '10', '11', 'Ajustes de operaciones de exportación', 4, true),
(gen_random_uuid(), '10', '12', 'Ajustes afectos al IVAP', 5, true)
ON CONFLICT ("CatalogNumber", "Code") DO NOTHING;

-- Cat. 20 — Motivo de Traslado (GRE)
INSERT INTO sunat_catalog_codes ("Id", "CatalogNumber", "Code", "Description", "SortOrder", "IsActive") VALUES
(gen_random_uuid(), '20', '01', 'Venta', 1, true),
(gen_random_uuid(), '20', '02', 'Compra', 2, true),
(gen_random_uuid(), '20', '03', 'Venta con entrega a terceros', 3, true),
(gen_random_uuid(), '20', '04', 'Traslado entre establecimientos', 4, true),
(gen_random_uuid(), '20', '05', 'Consignación', 5, true),
(gen_random_uuid(), '20', '06', 'Devolución', 6, true),
(gen_random_uuid(), '20', '07', 'Recojo de bienes transformados', 7, true),
(gen_random_uuid(), '20', '08', 'Importación', 8, true),
(gen_random_uuid(), '20', '09', 'Exportación', 9, true),
(gen_random_uuid(), '20', '13', 'Otros', 10, true),
(gen_random_uuid(), '20', '14', 'Venta sujeta a confirmación del comprador', 11, true),
(gen_random_uuid(), '20', '17', 'Traslado emisor itinerante de CP', 12, true),
(gen_random_uuid(), '20', '18', 'Traslado a zona primaria', 13, true),
(gen_random_uuid(), '20', '19', 'Traslado para transformación', 14, true)
ON CONFLICT ("CatalogNumber", "Code") DO NOTHING;

-- Cat. 51 — Tipo de Operación
INSERT INTO sunat_catalog_codes ("Id", "CatalogNumber", "Code", "Description", "SortOrder", "IsActive") VALUES
(gen_random_uuid(), '51', '0101', 'Venta interna', 1, true),
(gen_random_uuid(), '51', '0112', 'Venta interna - Sustenta traslado de mercadería', 2, true),
(gen_random_uuid(), '51', '0113', 'Venta interna - Sustenta gastos deducibles', 3, true),
(gen_random_uuid(), '51', '0200', 'Exportación de bienes', 4, true),
(gen_random_uuid(), '51', '0401', 'Ventas no domiciliados', 5, true),
(gen_random_uuid(), '51', '1001', 'Operación sujeta a detracción', 6, true),
(gen_random_uuid(), '51', '1002', 'Operación sujeta a detracción - Recursos hidrobiológicos', 7, true),
(gen_random_uuid(), '51', '1003', 'Operación sujeta a detracción - Transporte pasajeros', 8, true),
(gen_random_uuid(), '51', '1004', 'Operación sujeta a detracción - Transporte carga', 9, true),
(gen_random_uuid(), '51', '2001', 'Operación sujeta a percepción', 10, true)
ON CONFLICT ("CatalogNumber", "Code") DO NOTHING;
