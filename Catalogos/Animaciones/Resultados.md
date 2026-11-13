# Animaciones propias y seguimiento de cámara — 12/11/2026

## Estado de entrega

**Entrega completa: 100 animaciones propias, 800 frames, cero identidades pendientes.** Se incorporaron cien PNG originales, cien perfiles independientes y todos sus `.meta`. Se completaron `caballo_090` a `caballo_100` tras renovarse el límite de generación. No se duplicaron animaciones ni se usaron sustitutos para completar el conteo.

La integración está **activada** en `Assets/Resources/HorseVisualCatalog.asset` (`catalogAnimationsEnabled: 1`) después de validar los cien perfiles y las regresiones. Los recursos y el código del proyecto principal coinciden por hash con la copia aislada validada. Los 356 archivos de los 89 perfiles anteriores permanecen intactos; se añadieron los 44 archivos de los once perfiles restantes. Se verificaron 200 GUID únicos de PNG/perfil y las referencias a su splash y sus ocho frames propios.

No se eliminaron recursos antiguos. Permanecen los 28 sprites legacy y las doce animaciones recuperadas con sus 96 frames. No hubo commit, push, nueva build ni modificación del guardado personal.

## Producción e integración

- [Estado.csv](Estado.csv): las cien identidades y sus recursos activos; cero pendientes de producción.
- [Produccion.json](Produccion.json): prompts exactos, referencias, correcciones y SHA-256 de cada PNG y splash.
- PNG: `Assets/Art/HorseRaceCatalog/caballo_001.png` a `caballo_100.png`.
- Perfiles: `Assets/Resources/HorseRaceProfiles/<catalogIdentityId>.asset`.

Cada spritesheet se generó con `image_gen.imagegen`, usando el splash de su propia identidad como referencia de personaje. El piloto de Rayo v2 aporta únicamente la secuencia de fases, escala y estilo; no se reutilizaron ni recolorearon los doce spritesheets antiguos. Se mantuvieron los PNG devueltos por la herramienta, sin retoque por scripts. Los scripts de imagen solo elaboran auditorías y contactos de revisión.

El generador produjo hojas de **1774×887**, aunque el prompt pedía 2048×1024. Sus ocho celdas de origen miden nominalmente 443,5×443,5. Unity importa atlas **1024×512 BC7**, con ocho sprites **256×256**, pivote central, filtro Point, Clamp, sin mipmaps ni lectura CPU, y reproducción a **9 FPS**. No se entregan frames de 512×512. La comparación de coordenadas importadas admite 0,01 píxel por redondeo del escalado.

`HorseVisualCatalog.ResolveForRace` carga el perfil de la identidad persistida y verifica su splash, familia y referencias. Cuando la integración está activada, un recurso nuevo ausente se informa como error y no se sustituye por una animación vieja. `HorseVisualAnimator` conserva su perfil enlazado al iniciar, reproduce las ocho fases y restaura el splash propio al parar. Se usa el animador existente, sin segundo controlador ni cambios en cálculos de carrera.

La restauración central conserva `catalogIdentityId`, `visualProfileId`, ID de instancia, nombre y estadísticas. También contempla las instantáneas de participantes, la oferta de subasta y su historial al cargar. La migración de los doce perfiles reconocidos usa su familia de pelaje, conserva la instancia y es idempotente. Los `legacy:*` desconocidos mantienen el respaldo anterior. La migración nueva está activa junto con el catálogo completo. El arreglo de doce perfiles anteriores se conserva para compatibilidad; los cien perfiles de carrera nuevos se resuelven por ID desde `Resources/HorseRaceProfiles`.

## Revisión artística y límites

Se compararon los cien contactos con sus splash y se inspeccionaron alfa, márgenes, ocho imágenes distintas, pelaje, mantilla y extremidades. Las correcciones del generador eliminaron medias blancas añadidas en Centella (`005`), fondos grisáceos en Pegaso (`006`), Aquiles (`030`) y Tesoro (`096`), y medias excesivamente blancas en Vigía (`100`), conservando sus marcas crema cortas. La auditoría final no detecta fondos visibles en los márgenes, recortes de silueta ni fases idénticas.

Las alertas heurísticas de patas blancas en `007`, `026`, `034`, `049`, `070`, `098` y `100` se revisaron contra la referencia: corresponden a marcas claras/crema de la fuente o pequeños brillos de cascos tras las correcciones. No se trataron como prueba automática de un pelaje incorrecto. El arte generado puede presentar pequeñas variaciones de contorno, luz y anatomía entre fases; la revisión de contactos no demuestra precisión anatómica perfecta. Se revisaron poses y encuadres en renders de Unity; la valoración final de naturalidad del loop y legibilidad en la pantalla del jugador sigue siendo manual. Estas animaciones nuevas no equivalen a los originales perdidos.

Se renderizaron Tesoro (`096`), Sereno (`090`), Solano (`092`), Victoria (`099`) y Vigía (`100`) sobre la pista existente en 1920×1080, 1600×900, 1366×768 y 1280×720. Se inspeccionaron proporciones, transparencia, encuadre y comparación de reposo/carrera. Las quince capturas incluyen el reposo en las cuatro resoluciones, las ocho poses en 1920×1080 y la primera en las otras tres; muestran poses controladas en la UI real. La prueba de competición independiente comprueba la reproducción temporal y el resultado de una carrera completa. No se rediseñó la pista ni el Canvas.

Los cien atlas cargados reportaron **104.947.200 bytes (100,09 MiB)** en el Editor con gráficos. Es una medida de esos atlas, no de toda la RAM/VRAM del juego. No demuestra FPS ni rendimiento de una build Windows.

## Corrección encontrada al validar el catálogo completo

`JsonUtility` reconstruía el rival persistente nulo como un `CaballoData` inline vacío: nombre e identidades vacíos, estadísticas cero y `spriteIndex: -1`. `CrearCaballo()` le asignaba un GUID y normalizaba sus valores; la comprobación anterior aceptaba ese objeto como un rival real. Al preparar otra carrera podía aparecer un participante sin identidad y sin animación de catálogo.

`GameManager.CargarPartida` comprueba ahora que el DTO tenga datos de rival (nombre, identidad visual/catálogo o índice legacy) antes de construirlo. Un GUID por sí solo no justifica restaurar un objeto vacío. La prueba reproduce el JSON vacío, comprueba dos ciclos de guardado/carga sin reintroducirlo y confirma que un rival legacy legítimo conserva su ID, nombre, sprite y velocidad. No se cambió el formato de guardado ni se alteraron las estadísticas de caballos reales o los cálculos de competición.

## Cámara: causa y cambio

**Actualización posterior:** el modelo de cámara de esta etapa fue sustituido tras revisar un nuevo video. El diagnóstico vigente, el mapeo absoluto fijo y las pruebas se documentan en [CAMARA_ITEMS.md](../../CAMARA_ITEMS.md). Los párrafos siguientes registran el controlador anterior de esta etapa y no describen el código actual.

El controlador anterior tenía una zona muerta: después de recalibrar con el cursor por encima del 95% horizontal, moverlo más a la derecha devolvía el ángulo anterior. Se reprodujo con una referencia al 98% y no alcanzaba +90°. También combinaba lectura de posición, reposicionamiento del cursor, `SmoothDamp` y un segundo límite de velocidad.

`CameraController.cs` usa ahora un único mapeo de posición anclado al ángulo actual, con lectura del Input System, normalización por resolución y un solo límite angular de 360°/s. Se eliminó el recentrado mediante `WarpCursorPosition` y la zona muerta del 5%. Los extremos siguen siendo alcanzables tras recalibrar; solo una referencia exactamente en el borde se traslada al primer punto distinto.

Al abrir una UI, Escape, perder foco o cambiar resolución se invalida la referencia, conservando la orientación. Al volver se toma una primera muestra sin rotar. Escape sobre otra interfaz mantiene el bloqueo hasta regresar realmente al Hub. Se conservan posición fija, FOV y límites −90°/+90°, −25°/+30°. No se cambió `hub.unity` respecto del inicio de esta tarea ni se añadió otro controlador.

La comodidad física, Alt+Tab real, cambios reales entre ventana/pantalla completa, varios monitores y reanudación con mouse físico **siguen pendientes**. El pase automatizado no se presenta como validación completa de esos comportamientos.

## Saldo negativo

Se revisó el video de gameplay de 79,73 segundos, 1916×874 a 30 FPS, mediante decodificación batch sin audio ni reproductor interactivo. Se observa saldo 350 → 150 → −50 tras derrotas; la subasta muestra puja y reserva cero. La lectura del guardado personal posterior mostró saldo −250, sin reserva ni puja activa; ese archivo solo se leyó.

La causa reproducida es la **penalización existente de $200 al perder una carrera**, que actualmente puede descontarse aunque haya menos de $200 disponibles. En una partida de prueba, 150 − 200 = −50 y ese saldo se conserva al cargar. No se confirmó doble cobro de subasta: las pujas descuentan únicamente el incremento reservado; cierre, devolución, entrega y carga no repiten descuentos ni devoluciones. Con saldo negativo no se acepta una puja nueva.

No se cambió la penalización, el dinero inicial, los premios ni el saldo de la partida personal. Impedir toda deuda sería una decisión económica adicional; este trabajo conserva la regla existente e identifica su consecuencia.

## Pruebas ejecutadas

Unity 6000.3.13f1, copia `C:\Caballos\Validation\MainMenu`, batch oculto, guardado y preferencias nuevos por ejecución. No se utilizó Computer Use, mouse ni teclado. La primera ejecución restringida no logró conectarse al servicio de licencia; se detuvo únicamente ese proceso batch propio y se repitió fuera del sandbox. Las ejecuciones finales terminaron con código 0. Se deshabilitó Burst únicamente en los procesos de validación, sin cambiar la configuración del proyecto.

| Prueba | Resultado real |
|---|---|
| Autoría/importación | 100 perfiles, 800 referencias de frame; compilación del Editor correcta |
| `AnimacionesCatalogoProbe.Run` | **2384 comprobaciones**: cien perfiles, ochocientas fases, referencias propias, pivotes, importación, JSON, migración, rival vacío/legacy, entrega de subasta, selección activa, guardado/carga y una carrera normal completa; todos vieron sus ocho fases y restauraron su splash |
| `AnimacionesRenderProbe.Run` | **15 capturas**, cinco identidades del lote final y cuatro resoluciones; reposo y poses renderizados a archivos sin entrada física |
| `CatalogosProbe.CompletoSinImportar` | **1240 comprobaciones**: cien identidades y splashes, ochenta compras, sesenta y cinco equipables, quince consumibles, bonificaciones, límites, reversión ante fallo de escritura, subastas de las cien identidades, persistencia y carrera real |
| `CorreccionCamaraProbe.Run` | **2205 comprobaciones**: reproducción del defecto anterior, extremos, intermedios, movimientos lentos/rápidos, 30/60/144 FPS, pausas largas, foco simulado, cambio de tamaño e interfaces/Escape superpuestas; incluye las cuatro resoluciones anteriores y 1916×874 |
| `EconomiaSubastaProbe.Run` | **22 comprobaciones** de reservas, aumentos, rechazo sin saldo, doble clic, cierre, devoluciones, persistencia y reproducción del negativo por penalización |
| `BetaJugableValidation.Run` | Escenarios **A–G** aprobados: apuestas, carrera con/sin apuesta, cambio de carrera, equipo, resultados, persistencia y liquidación única; con el catálogo nuevo activado |
| `SubastasProbe.Run` | **163 comprobaciones** aprobadas de regresión de subastas; con el catálogo nuevo activado |
| `CatalogosWindowsProbe.Run` | **27 ensamblados** compilados para Windows; sin ejecutable |
| `CatalogosAssetProbe.Run` | **12 comprobaciones** de escenas, referencias de prefabs y doce perfiles anteriores |

La prueba completa usa una parrilla normal del catálogo, sin restringirla al lote de 89 ni excluir arbitrariamente a un rival persistente. La prueba parcial anterior de 2150 comprobaciones se conserva como antecedente y no sustituye el pase completo. Se corrigieron dos defectos iniciales del test (igualdad exacta de coordenadas fraccionales e IDs de instancia no válidos); el rival vacío fue un defecto real de restauración, corregido en producción y cubierto por pruebas.

Los registros detallados y contactos permanecen en `C:\Caballos\Validation\AnimacionCamara`, los quince renders finales en su subcarpeta `Render100` y los logs en `C:\Caballos\Validation\HubRefresh` (`animaciones-catalogo-final.log` y los archivos `*-100-*`). Las fuentes de comprobación y los resultados se conservan en [Validacion](Validacion), fuera de `Assets`, para evitar añadir herramientas temporales al juego. La copia anterior del estado parcial está respaldada en `Validation\AnimacionCamara\AntesCompletar100`.

## Comprobaciones manuales pendientes

1. Valorar la naturalidad del galope/loop, continuidad y legibilidad en la pantalla del jugador. Las ocho fases, pivotes, correspondencias y reproducción real ya se validaron automáticamente.
2. Comprobar mouse físico, comodidad de cámara y cambios reales de foco/ventana/pantalla completa, sin usar control remoto del escritorio.
3. Medir rendimiento y comportamiento en Player Windows cuando se autorice una nueva build. La compilación de scripts actual no genera ni ejecuta un `.exe`.

No quedan perfiles ni frames pendientes de generar o integrar. Estas comprobaciones manuales no se presentan como aprobadas.

## Archivos de juego modificados en esta tarea

`CameraController.cs`, `HorseVisualProfile.cs`, `HorseVisualCatalog.cs`, `HorseVisualAnimator.cs`, `CatalogoCaballos.cs`, `RecursosCatalogo.cs`, `GameManager.cs`, `GameManager.Carrera.cs`, `GameManager.Subastas.cs` y `Assets/Resources/HorseVisualCatalog.asset` (interruptor en 1). Nuevas carpetas `Assets/Art/HorseRaceCatalog` y `Assets/Resources/HorseRaceProfiles`, con sus `.meta`; documentación en `Catalogos/Animaciones` y actualización de `CATALOGOS.md`.

Los cambios de GameManager se limitan a restauración/identificación visual, instantáneas al cargar y protección frente al rival vacío deserializado; no cambian fórmulas de carrera ni aritmética económica. En la finalización del lote solo se añadieron los once recursos restantes, se activó el catálogo y se incorporó esa protección de carga. No se tocaron pistas, posiciones del Hub, escenas, diseños 2D, iluminación ni el ejecutable anterior.
