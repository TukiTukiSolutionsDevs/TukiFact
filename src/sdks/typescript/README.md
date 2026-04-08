# @tukifact/sdk

SDK oficial de TukiFact para Node.js/TypeScript — Facturación Electrónica para Perú.

## Instalación

```bash
npm install @tukifact/sdk
```

## Uso Rápido

```typescript
import { TukiFactClient } from '@tukifact/sdk';

const client = new TukiFactClient({
  baseUrl: 'https://tukifact.net.pe',
  apiKey: 'tk_your_api_key_here',
  tenantId: 'your-tenant-uuid',
});

// Emitir factura
const factura = await client.emitDocument({
  documentType: '01',
  serie: 'F001',
  customerDocType: '6',
  customerDocNumber: '20100047218',
  customerName: 'CLIENTE SAC',
  items: [{
    description: 'Servicio de consultoría',
    quantity: 1,
    unitPrice: 1000,
    igvType: '10',
  }],
});

console.log(factura.fullNumber); // F001-00000001
console.log(factura.status);     // accepted
```
