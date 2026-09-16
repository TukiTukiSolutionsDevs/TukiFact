# Checklist: verificación Android

- [ ] Tests JVM del módulo tocado en verde.
- [ ] `androidTest` / Maestro solo si UI, Room o viaje crítico cambió.
- [ ] Room/MockWebServer no se recrean por método.
- [ ] Koin `verify` en verde si cambió un módulo DI.
- [ ] Comando no ejecutado + motivo (sin emulador, Maestro no instalado).

```bash
# desde MarketjoyaMobile/ (JAVA_HOME al JBR de Android Studio o JDK 21 si java no está en PATH)
./gradlew verify                                  # gate CI completo
./gradlew :core:database:test                     # JVM de un módulo
./gradlew :architecture-tests:test                # reglas Konsist
./gradlew connectedDebugAndroidTest               # emulador: Room, Compose
maestro test maestro/pos-smoke.yaml               # tras :app-pos:installDebug
```
