# Conectar TestSprite MCP a OpenCode

## Problema

OpenCode usa un schema estricto (`additionalProperties: false`) para la configuracion de MCPs.
Si usas un campo que no existe en el schema, como `env`, OpenCode muere con:

```
Error: Configuration is invalid at ~/.config/opencode/opencode.json
↳ Invalid input mcp.testsprite
```

## Solucion

El campo correcto para variables de entorno es `environment`, NO `env`.

## Configuracion correcta

Editar `~/.config/opencode/opencode.json` y agregar dentro del objeto `"mcp"`:

```json
"testsprite": {
  "command": [
    "npx",
    "@testsprite/testsprite-mcp@latest"
  ],
  "type": "local",
  "environment": {
    "API_KEY": "tu-api-key-de-testsprite-aca"
  }
}
```

## Campos validos para MCP local en OpenCode

Segun el schema en `https://opencode.ai/config.json`:

| Campo         | Tipo              | Requerido | Descripcion                        |
|---------------|-------------------|-----------|------------------------------------|
| `type`        | `"local"`         | Si        | Tipo de MCP                        |
| `command`     | `string[]`        | Si        | Comando y argumentos               |
| `environment` | `{string:string}` | No        | Variables de entorno               |
| `enabled`     | `boolean`         | No        | Activar/desactivar (default: true) |
| `timeout`     | `integer`         | No        | Timeout en ms (default: 5000)      |

**IMPORTANTE**: No se acepta ningun otro campo. `env`, `args`, `cwd` u otros campos van a romper la validacion.

## Campos validos para MCP remoto en OpenCode

| Campo     | Tipo       | Requerido | Descripcion                        |
|-----------|------------|-----------|------------------------------------|
| `type`    | `"remote"` | Si        | Tipo de MCP                        |
| `url`     | `string`   | Si        | URL del servidor MCP               |
| `enabled` | `boolean`  | No        | Activar/desactivar (default: true) |
| `headers` | `{string:string}` | No | Headers HTTP custom (ej: API keys) |

## Donde conseguir la API Key de TestSprite

1. Ir a la web de TestSprite y crear una cuenta
2. En el dashboard, generar una API key
3. La key tiene formato: `sk-user-...`
4. Pegarla en el campo `API_KEY` dentro de `environment`

## Ejemplo completo del bloque mcp en opencode.json

```json
{
  "mcp": {
    "testsprite": {
      "command": [
        "npx",
        "@testsprite/testsprite-mcp@latest"
      ],
      "type": "local",
      "environment": {
        "API_KEY": "sk-user-tu-key-aca"
      }
    }
  }
}
```

## Error comun

```
// MAL - OpenCode no reconoce "env"
"testsprite": {
  "command": ["npx", "@testsprite/testsprite-mcp@latest"],
  "type": "local",
  "env": { "API_KEY": "..." }    // <-- ROMPE
}

// BIEN - El campo correcto es "environment"
"testsprite": {
  "command": ["npx", "@testsprite/testsprite-mcp@latest"],
  "type": "local",
  "environment": { "API_KEY": "..." }  // <-- CORRECTO
}
```
