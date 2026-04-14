# @tukifact/sdk

Official TypeScript/JavaScript SDK for the [TukiFact](https://tukifact.net.pe) electronic invoicing API (SUNAT, Peru).

## Install

```bash
npm install @tukifact/sdk
```

## Quick Start

```typescript
import { TukiFactClient } from '@tukifact/sdk';

const client = new TukiFactClient({ apiKey: 'YOUR_API_KEY' });

const { document } = await client.createDocument({
  type: 'factura',
  series: 'F001',
  customerId: 'cust_abc123',
  issueDate: '2026-04-14',
  currency: 'PEN',
  items: [
    {
      description: 'Servicio de consultoría',
      quantity: 1,
      unitPrice: 1000,
    },
  ],
});

console.log(document.fullNumber); // F001-00000001
```

## Sandbox

```typescript
const client = new TukiFactClient({
  apiKey: 'YOUR_SANDBOX_KEY',
  sandbox: true,
});
```

## Config Options

| Option    | Type    | Default                          | Description              |
|-----------|---------|----------------------------------|--------------------------|
| `apiKey`  | string  | required                         | Your TukiFact API key    |
| `baseUrl` | string  | `https://api.tukifact.net.pe`    | Override API base URL    |
| `version` | string  | `'v1'`                           | API version              |
| `timeout` | number  | `30000`                          | Request timeout (ms)     |
| `sandbox` | boolean | `false`                          | Use sandbox environment  |

## Error Handling

```typescript
import { TukiFactClient, TukiFactError } from '@tukifact/sdk';

try {
  await client.getDocument('nonexistent');
} catch (err) {
  if (err instanceof TukiFactError) {
    console.error(err.statusCode, err.message, err.details);
  }
}
```

## Download PDF / XML

```typescript
const pdf = await client.getDocumentPdf('doc_abc123');
await fs.writeFile('factura.pdf', pdf);

const xml = await client.getDocumentXml('doc_abc123');
```
