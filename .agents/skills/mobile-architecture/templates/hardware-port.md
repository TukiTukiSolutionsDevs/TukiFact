# Plantilla: puerto de hardware

Estado: la API de los puertos (balanza, impresora, terminal de pago, ubicación) y la forma de `ScaleReading` son Pendiente de decisión. Esta plantilla muestra la separación, no un contrato acordado.

```kotlin
// :hardware (Kotlin/JVM)
package pe.marketjoya.hardware.scale

interface ScalePort {
    val state: StateFlow<DeviceConnectionState>
    // lecturas, connect/disconnect: forma Pendiente de decisión
}

// Módulo Android (ubicación del adapter: Pendiente de decisión)
class UsbScaleAdapter(
    private val transport: UsbSerialTransport,
    private val parser: ScaleProtocolParser,
) : ScalePort {
    // UsbManager y bytes solo aquí.
}
```

El UseCase depende del puerto, no del adapter. El fake del puerto se crea en `:core:testing` junto con el puerto.
