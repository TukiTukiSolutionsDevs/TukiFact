# Checklist: hardware

- [ ] Puerto en `:hardware` (Kotlin/JVM, sin Android). API del puerto acordada; si no, "Pendiente de decisión".
- [ ] SDK/`UsbManager`/Bluetooth solo en el adapter Android.
- [ ] Transporte separado del parser.
- [ ] Estado con `DeviceConnectionState` + timeout/backoff.
- [ ] I/O fuera de Main (`DispatcherProvider.io`); streams filtrados antes de UI.
- [ ] Fake del puerto en `:core:testing` creado junto con el puerto.
- [ ] Permisos mínimos; scan/socket cancelados al salir.
- [ ] El ViewModel no ve bytes.
