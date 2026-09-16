# Checklist: verificación Android

- [ ] Tests de dominio/UseCase del slice (`../../mobile-testing/SKILL.md`).
- [ ] Fake del puerto externo en `:core:testing` (contrato compartido si hay adapter Room).
- [ ] Koin `verify` de la app pasa si se registró un módulo nuevo.
- [ ] Room/MockWebServer: una instancia por suite, no por `@Test`.
- [ ] Informar comandos no ejecutados y el motivo (ej. sin emulador, Maestro no instalado).

```bash
# desde MarketjoyaMobile/ (JAVA_HOME al JBR de Android Studio o JDK 21 si java no está en PATH)
./gradlew verify                                   # gate CI: check de todos los módulos + build-logic + APKs debug
./gradlew :app-pos:assembleDebug
./gradlew :app-preventa:assembleDebug
./gradlew connectedDebugAndroidTest                # emulador/dispositivo: Room, Compose
./gradlew spotlessApply                            # formatear antes de verify
```
