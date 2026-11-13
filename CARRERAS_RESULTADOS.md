# Finalización y clasificación de carreras — 12/11/2026

La carrera espera al último participante antes de mostrar el resultado, liquidar apuestas, entregar experiencia y avanzar el calendario. Se conserva el motor de velocidad, aceleración, resistencia, recuperación y suerte de `EstadoCaballoCarrera`.

## Registro y cierre

`ClasificacionCarrera` recibe el lote completo de avances de cada frame. Para cada cruce calcula el tiempo de simulación acumulado más la fracción interpolada del frame. Ordena los cruces por ese tiempo sin redondear; un empate exacto dentro del frame se resuelve por ID ordinal estable, independientemente de la posición en la parrilla. La pantalla muestra milisegundos, sin utilizar ese redondeo para decidir posiciones.

Cada ID sólo se resuelve una vez. Las llegadas registradas conservan sus puestos y el modelo devuelve copias de los resultados para impedir que un consumidor altere su registro interno. Un caballo que llegó deja de actualizar su simulación, queda visualmente en la meta y restaura su sprite base mediante el animador existente. Los demás siguen corriendo y permanecen visibles en el recorrido completo. Si el jugador ya llegó, el texto de estado indica su puesto mientras espera.

La liquidación rechaza clasificaciones parciales, IDs repetidos o ajenos, posiciones discontinuas y tiempos inválidos o desordenados. El ganador y el mejor rival persistente se determinan por la clasificación completa; no se decide el rival por el progreso observado en la primera llegada.

## Protección excepcional

Un progreso no finito, negativo o que retrocede se registra con el ID afectado. También existe un límite de 60 segundos **de simulación sin avance**, y un límite total de `max(300, distanciaSimulada / 2.4 * 8 + 60)` segundos. El segundo incluye un margen de ocho veces la duración a la marcha mínima normal, más un minuto. La pausa no consume estos plazos. Se probaron carreras largas con las 16 velocidades y stamina mínima.

Los afectados figuran como **No finalizó (N/C)**, sin tiempo inventado, detrás de todos los que cruzaron normalmente. Entre afectados se usa el último progreso válido descendente y después el ID estable. Es un orden administrativo de clasificación, no una llegada ni un puesto de podio.

Si nadie logra una llegada válida, no se inventa un ganador: se devuelve la apuesta una sola vez, no se entrega premio ni XP y no se aplica la penalización normal de derrota. El calendario resuelve ese día una sola vez mediante su flujo existente. El nuevo estado `Anulada` se añadió al final del enum, conservando los valores serializados anteriores. Esta política sólo corresponde al fallo excepcional; los importes, cuotas y reglas de las carreras normales permanecen iguales.

## Presentación

`ResultadosCarreraView` construye `ResultadosCarrera` bajo el contenedor completo de la carrera. No se editaron escenas ni otros Canvas. Reutiliza la madera existente del establo y la fuente del juego, con bordes finos de bronce y texto claro. Sus anchors se expresan proporcionalmente sobre el contenedor, usando el CanvasScaler existente.

El ganador ocupa la tarjeta central más grande. Segundo y tercero aparecen a sus lados; con uno o dos participantes sólo se crean los puestos existentes. Cada tarjeta usa el nombre y el splash de su identidad real del catálogo nuevo, manteniendo proporciones. Si una identidad no tiene un splash válido propio, se omite la imagen; no se sustituye por un caballo de ejemplo ni por recursos legacy.

Debajo aparecen todos los participantes, tiempos, fila del jugador y su puesto personal. Las seis filas actuales caben completas; el contenedor admite desplazamiento si aumenta la cantidad. Los botones de resultados tienen `Transition.None`, sin brillo decorativo persistente.

## Economía, persistencia y calendario

Premios, apuesta confirmada por ID, cuota congelada, XP y cambio de día se procesan una sola vez después de resolver a todos. Se mantienen el premio de la definición, la penalización normal de $200 y la XP normal de 100/25. Se conserva la escritura final única del sistema existente, agrupando las llamadas de XP y calendario.

El guardado incorpora opcionalmente `ultimoResultadoCarrera`, con la parrilla original, identidades, ID del jugador y clasificación completa. Las partidas anteriores sin este campo siguen cargándose, sin fabricar resultados. La versión del guardado se conserva. No se guarda un resultado parcial durante la simulación.

Ante un fallo de escritura se conserva el archivo anterior, se mantiene el resultado en memoria y se ofrece **Reintentar guardado**. Ese botón sólo intenta guardar; no vuelve a liquidar ni avanza otro día. El retorno desde resultados queda habilitado al guardar correctamente. Escape conserva sus controles de salida y guardado existentes.

Si se cierra forzosamente el proceso antes de poder guardar, al cargar continúa vigente el comportamiento anterior: la carrera pendiente se vuelve a disputar desde el inicio con la parrilla, semillas y apuesta del último guardado. No se implementó persistencia de frames intermedios ni recuperación de memoria tras un cierre forzoso.

## Validación ejecutada

Unity **6000.3.13f1**, exclusivamente batch sobre `C:\Caballos\Validation\MainMenu`, con guardados separados y renders fuera de pantalla. No se utilizó el guardado personal.

| Validación | Resultado |
| --- | --- |
| Modelo de clasificación | **427 comprobaciones aprobadas**: cruces interpolados, empate por ID, llegadas próximas, inmutabilidad, duplicados, 400 parrillas reordenadas, errores/bloqueos y carreras largas con las 16 velocidades |
| Integración nueva en Play Mode batch | **161 comprobaciones aprobadas**: jugador primero y último, 1/2/6 participantes, carrera larga, parada individual, pantalla después del último, podio/splash/ranking, cuotas, pagos/XP/día únicos, JSON/carga anterior, fallo de escritura y botones de reintento |
| Animadores y catálogo | **2539 comprobaciones aprobadas** de los 100 perfiles y una carrera real: ocho fases propias, identidad fija, restauración del splash, guardado/carga |
| Beta A–G | **Aprobada**: selección, tablón, cambio de carrera, equipo legacy, dos carreras reales con y sin apuesta, persistencia y liquidación única |
| Scripts Windows | **27 ensamblados compilados**, sin generar ejecutable |
| Referencias | **12 comprobaciones aprobadas** de escenas/prefabs y perfiles anteriores |
| Integridad | Motor de simulación, Hub, cámara, animador, estadísticas, maniquí de Items, cursor y pausa conservados; 100 perfiles, 800 frames y 100 splash sin editar; `.meta` nuevos presentes y GUID únicos |

Se generaron e inspeccionaron renders de resultados en **1920×1080, 1600×900, 1366×768 y 1280×720**, sin texto desbordado ni márgenes laterales. También se renderizaron jugador último, uno/dos participantes y guardado pendiente. Las comprobaciones usan la simulación y las vistas de producción con fixtures de prueba aislados; los datos de prueba no se incorporan a escenas ni al guardado del jugador.

Durante el desarrollo se corrigió el anclaje inicial de la vista nueva, que estaba bajo el pequeño objeto del controlador en vez del contenedor completo. La prueba antigua de beta asumía que existían sólo cinco equipables; su fixture se acotó a los cinco legacy, que siguen disponibles en el catálogo ampliado. También se adaptó su liquidación forzada para proporcionar primero una clasificación completa y comprobar el rechazo de una parcial. No se cambió el catálogo ni la economía para satisfacer pruebas antiguas.

Las fuentes y evidencias reproducibles se conservan en `Validacion/Clasificacion`, fuera de Assets. Los logs completos están en `C:\Caballos\Validation\HubRefresh\Clasificacion-*.log`; renders y respaldos previos, en `C:\Caballos\Validation\Clasificacion`.

## Comprobación manual pendiente

Revisar en Unity con mouse físico la comodidad de lectura, la transición al podio, Escape sobre resultados y el retorno al Hub. El render y los eventos de botón se probaron automáticamente; no se controló el escritorio ni se sustituyó esa revisión de uso real. No se generó una nueva build de Windows.

## Archivos de esta etapa

- Nuevos: `Assets/Scripts/ClasificacionCarrera.cs`, `Assets/Scripts/ResultadosCarreraView.cs` y sus `.meta`.
- Integración: `Assets/Scripts/CarreraUI.cs`, `CarreraProgramada.cs`, `GameManager.Carrera.cs`, `GameManager.cs`, `GameData.cs`.
- Prueba existente adaptada: `Assets/Editor/BetaJugableValidation.cs`.
- Documentación y pruebas auxiliares: este archivo y `Validacion/Clasificacion`.

No se modificaron cámara del Hub, otras interfaces, escenas, animaciones ni fórmulas de atributos. Los cambios locales previos se conservaron. Sin staging, commit, push ni nueva build.
