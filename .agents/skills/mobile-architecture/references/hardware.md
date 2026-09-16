# Hardware

Solo vía puertos. SDK y protocolo viven en el adapter. I/O fuera de Main (`DispatcherProvider.io`). Timeouts y backoff.

## Estado actual

- `:hardware` es Kotlin/JVM (sin Android) y hoy solo contiene `DeviceConnectionState`: `Disconnected`, `Discovering`, `Connecting`, `Connected`, `Error`.
- Adapters con `UsbManager`/Bluetooth viven en módulos Android, nunca en `:hardware`.
- Konsist `DomainPurity` impide imports Android en `:hardware`.

## Pendiente de decisión

- Forma de `ScaleReading` y API de `ScalePort`.
- API de puertos de impresora, terminal de pago y ubicación (`LocationPort`).
- Módulo donde viven los adapters Android de hardware.
- Fakes de hardware: se crean en `:core:testing` junto con cada puerto; hoy no existen.

## Forma esperada

```text
<Device>Port (:hardware) <- <Device>Adapter (Android) <- Transport + ProtocolParser
```

## USB / serial

- `UsbManager` solo en el adapter.
- Transporte ≠ parser.
- Permisos USB explícitos.
- Bytes crudos no llegan a ViewModel (Konsist `ViewModelDependencies` bloquea `android.hardware.usb.`).
- VID/PID/protocolo = config del adapter, no dominio.

## Bluetooth

- Discovery / pairing / connection separados.
- Permisos mínimos. Cancelar scan al salir.
- Estados: `DeviceConnectionState`.
- No scan permanente. No socket en Main.

## Memoria

- No Activity/View/Composable en singleton.
- `Application` Context cuando corresponda.
- Desregistrar listeners. No `System.gc()`.
- Filtrar streams de hardware antes de UI.

## Prohibido

ViewModel parseando RS232. Hardware con reglas de venta/stock/fiscal.
