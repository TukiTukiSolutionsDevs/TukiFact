# Seguridad Android

## Implementado

| Control | Dónde |
|---|---|
| Backup deshabilitado | `android:allowBackup="false"` + `backup_rules.xml` / `data_extraction_rules.xml` excluyen todo: las filas del outbox son del dispositivo |
| Solo TLS | `network_security_config.xml`: `cleartextTrafficPermitted="false"`, CA del sistema |
| R8 en release | `optimization { enable = true }`; keep rules en `src/main/keepRules/*.keep` |
| Firma | `marketjoya.signing.{storeFile,storePassword,keyAlias,keyPassword}` (Gradle properties de usuario) o `MARKETJOYA_SIGNING_*`; nunca en el repo. Sin ellas el APK release sale sin firmar. `*.jks`/`*.keystore` ignorados |

## Reglas

- No secretos hardcoded. No claves del facturador en el APK.
- No PAN/CVV. No tokens en logs.
- El backend autoriza reglas críticas (descuento, crédito, fiscal).
- Tokens solo en Infrastructure; nunca en Domain.
- Excepción cleartext para host de desarrollo: Pendiente de decisión (hoy no existe).
- Almacenamiento seguro de tokens: Pendiente de decisión.
