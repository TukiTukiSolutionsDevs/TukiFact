# Testing Android

Contrato runtime: `../mobile-testing/SKILL.md`.

Resumen: Domain/UseCase JVM con fakes de `:core:testing`; Room (`androidTest`) y MockWebServer una vez por suite; contrato compartido fake/Room; Koin `verify` por app; Compose selectivo; Maestro para viajes críticos. Gate: `./gradlew verify`.
