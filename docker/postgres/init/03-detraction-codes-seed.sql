-- Catálogo 54: Códigos de Bienes y Servicios sujetos a Detracción (SPOT)
-- Fuente: SUNAT R.S. 183-2004, R.S. 086-2025, R.S. 121-2025, R.S. 175-2025
-- Vigente 2025-2026

INSERT INTO detraction_codes ("Code", "Description", "Percentage", "Annex", "IsActive") VALUES
-- Anexo I — Bienes sujetos
('001', 'Azúcar y melaza de caña', 10.00, 'I', true),
('003', 'Alcohol etílico', 10.00, 'I', true),
('004', 'Recursos hidrobiológicos', 4.00, 'I', true),
('005', 'Maíz amarillo duro', 4.00, 'I', true),
('007', 'Caña de azúcar', 10.00, 'I', true),
('008', 'Madera', 4.00, 'I', true),
('009', 'Arena y piedra', 10.00, 'I', true),
('010', 'Residuos, subproductos, desechos', 15.00, 'I', true),
('011', 'Bienes gravados con IGV (renuncia exoneración)', 10.00, 'I', true),
('014', 'Carnes y despojos comestibles', 4.00, 'I', true),
('015', 'Abonos, cueros y pieles de origen animal', 15.00, 'I', true),
('016', 'Aceite de pescado', 10.00, 'I', true),
('017', 'Harina, polvo y pellets de pescado', 4.00, 'I', true),
('023', 'Leche', 4.00, 'I', true),
('024', 'Páprika y otros frutos capsicum', 10.00, 'I', true),
('025', 'Plomo', 15.00, 'I', true),
('029', 'Minerales metálicos no auríferos', 10.00, 'I', true),
('031', 'Oro gravado con IGV', 10.00, 'I', true),
('032', 'Bienes exonerados del IGV', 1.50, 'I', true),
('033', 'Oro y demás minerales metálicos exonerados del IGV', 1.50, 'I', true),
('034', 'Minerales no metálicos', 10.00, 'I', true),

-- Anexo III — Servicios sujetos
('012', 'Intermediación laboral y tercerización', 12.00, 'III', true),
('019', 'Arrendamiento de bienes', 10.00, 'III', true),
('020', 'Mantenimiento y reparación de bienes muebles', 12.00, 'III', true),
('021', 'Movimiento de carga', 10.00, 'III', true),
('022', 'Otros servicios empresariales', 12.00, 'III', true),
('026', 'Servicio de transporte de personas', 10.00, 'III', true),
('027', 'Servicio de transporte de carga', 4.00, 'III', true),
('030', 'Contratos de construcción', 4.00, 'III', true),
('037', 'Demás servicios gravados con IGV', 12.00, 'III', true)
ON CONFLICT ("Code") DO UPDATE SET
  "Description" = EXCLUDED."Description",
  "Percentage" = EXCLUDED."Percentage",
  "Annex" = EXCLUDED."Annex",
  "IsActive" = EXCLUDED."IsActive";
