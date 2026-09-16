# Plantilla: Room de suite con contrato compartido

Referencia real: `core/database/src/androidTest/kotlin/pe/marketjoya/core/database/outbox/RoomPendingSyncWriterTest.kt`.

```kotlin
package pe.marketjoya.core.database.outbox

@RunWith(AndroidJUnit4::class)
class RoomPendingSyncWriterTest : PendingSyncWriterContract() {
    private val roomWriter = RoomPendingSyncWriter(database.pendingSyncOperations())

    override val writer = roomWriter

    override suspend fun storedFor(clientMutationId: ClientMutationId): List<PendingSyncOperation> =
        listOfNotNull(roomWriter.getByMutationId(clientMutationId))

    companion object {
        private lateinit var database: MarketjoyaDatabase

        @JvmStatic @BeforeClass
        fun openDatabase() {
            database = Room
                .inMemoryDatabaseBuilder(ApplicationProvider.getApplicationContext(), MarketjoyaDatabase::class.java)
                .build()
        }

        @JvmStatic @AfterClass
        fun closeDatabase() { database.close() }
    }
}
```

- El contrato (`:core:testing`) crea ids con `LocalId.random()` / `ClientMutationId.random()`: la base compartida no necesita limpieza.
- El mismo contrato corre en JVM con `FakePendingSyncWriterTest`.
- `androidTestImplementation(project(":core:testing"))` en el módulo Room.
- No abras la base en `@Before`.
