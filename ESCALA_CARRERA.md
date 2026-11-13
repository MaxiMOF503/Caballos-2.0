# Escala y encuadre de carrera — 13/11/2026

Se trabajó con Unity 6000.3.13f1 mediante archivos y procesos batch ocultos, en `C:\Caballos\Validation\MainMenu`, con guardados y preferencias aislados. No se controló el escritorio ni se generó un ejecutable. No se hizo commit ni push.

## Causa y corrección

La previa y la llegada mostraban el splash rectangular del catálogo; el galope mostraba celdas cuadradas con mucho margen transparente. `Image.preserveAspect` respetaba cada rectángulo completo, sin compensar el área realmente ocupada por el caballo. Por ejemplo, en caballo_001 la unión de sus ocho siluetas ocupa aproximadamente el 74 % del ancho y el 55 % de la altura de una celda. Cambiar de splash a esa celda reducía mucho su tamaño aparente. Los frames comparten PPU 100 y pivote central; no había un cambio de `localScale` al iniciar el galope.

En la pista, previa y llegada usan ahora la tercera pose de la propia animación. El galope conserva sus ocho sprites originales y 9 FPS. Se mide una sola unión de los ocho frames por identidad, se fija una altura de referencia de 148 unidades y se conserva exactamente la misma transformación durante todas las fases y al detenerse. No se estira el sprite, no se recalcula su tamaño según la pose y no se cambian texturas, PPU ni importadores. Las patas recogidas durante la suspensión siguen acortando naturalmente la silueta; esto no es un cambio de escala.

`LimitesVisualesCarrera.json` contiene las medidas de las 100 identidades actuales, calculadas con alfa >= 128 y margen adicional de cuatro unidades. Una máscara fija mantiene fuera del dibujo los residuos externos de las celdas. Los perfiles legacy animados conservan una celda estable si no tienen medidas; los sprites antiguos sin animación conservan su sprite y layout anterior. El splash de ficha, establo y podio continúa siendo el original del catálogo. La identidad y las estadísticas guardadas no se modifican.

En la primera ejecución, una prueba detectó que `JsonUtility` no leía los campos x/y/width/height en un `Rect` directamente. Se corrigió utilizando un DTO con campos públicos y construyendo el Rect explícitamente. La prueba final valida que todas las medidas sean positivas.

## Encuadre y marcadores

En el lienzo de referencia 1920×1080, el viewport pasó de `(24,142,1872,810)` a `(24,110,1872,890)`: su borde superior subió **32 píxeles** y ganó **80 píxeles de altura**. El encabezado queda debajo del HUD semanal; ayuda, inscripción y premio ocupan el pie.

Los carriles se ordenan desde arriba, con separación de 174 unidades para caballos de referencia 148 y espacio para la flecha. La altura del recorrido se adapta a la cantidad real de participantes, dejando tribuna y carteles visibles. Se probaron parrillas de 5, 6 y 8: ocho es una fixture de validación, no un aumento del catálogo de carreras. Con seis participantes, la referencia visual resulta aproximadamente 106 píxeles de alto a 1080p, frente a unos 77 del galope anterior en el ejemplo medido; varía según la silueta y la cantidad de carriles.

`PuntoCompetitivo` permanece como un RectTransform sin Graphic: sirve para comprobar la meta sin dibujar puntos. La única marca dibujada es la flecha del ID real del jugador, sobre su caballo, durante dos segundos iniciales o mientras se mantiene A. La flecha se ajustó para ocupar el espacio entre carriles. No se alteró la conversión distancia→posición, el punto competitivo, los cálculos, ni la cámara del Hub.

## Cambios de esta tarea

- `Assets/Scripts/PresentacionCaballoCarrera.cs` y su `.meta`: medidas, escala uniforme y máscara de presentación.
- `Assets/Resources/LimitesVisualesCarrera.json` y su `.meta`: límites de las 100 identidades.
- `Assets/Scripts/HorseVisualAnimator.cs`: opción de presentación de pista y pose común de reposo/llegada; el enlace habitual de dos argumentos conserva su comportamiento.
- `Assets/Scripts/CarreraUI.cs`: aplica esta presentación, distribuye los carriles y bloquea el avance de simulación si la partida ya está derrotada.
- `Assets/Scripts/PistaLateralCarrera.cs`: viewport, alturas adaptativas, HUD y ubicación de la flecha.
- Este documento y `Validacion/EscalaFinal/`: probes, resultados y capturas seleccionadas fuera de Assets.

La comparación con la copia anterior a esta tarea muestra cambios únicamente en los tres scripts existentes anteriores; los otros 52 archivos respaldados, incluida la escena hub.unity y CameraController.cs, coinciden. Se conservaron los cambios locales que ya existían. Los 100 atlas y 100 splashes coinciden con los hashes registrados en Produccion.json y conservan sus .meta. No se borraron recursos legacy.

## Pruebas ejecutadas

| Comprobación | Resultado |
| --- | --- |
| Compilación de Player Windows, sin ejecutable | 27 ensamblados, incluido Assembly-CSharp; sin errores |
| Escenas, prefabs y perfiles recuperados | 12 comprobaciones de referencias aprobadas |
| Presentación | 100 perfiles × ocho fases sin resize, previa/llegada, legacy, flecha, carriles, meta y liquidación; detalle en escala-pruebas.txt |
| Animaciones e identidad | 2824 comprobaciones; ocho fases de cada identidad, guardado/carga, adquisición y carrera real |
| Carrera y economía | 17547 comprobaciones de integración, incluidas verificaciones repetidas por tick; no representan 17547 casos independientes |
| Progresión semanal | 78 comprobaciones aprobadas |
| Integridad y alcance | 200 imágenes verificadas por SHA256; fuentes principales iguales a la copia validada; git diff --check sin errores |

Los renders de las parrillas de 5, 6 y 8 se generaron fuera de pantalla en 1920×1080, 1600×900, 1366×768 y 1280×720. Se inspeccionaron capturas de previa, fases de galope y primera llegada; no se observan puntos gráficos extra ni superposición de siluetas. Se conservaron siete capturas representativas en `Validacion/EscalaFinal/`; el conjunto completo queda en `C:\Caballos\Validation\EscalaFinal`.

Las regresiones cubrieron clasificación completa, continuidad después del primer finalista, jugador primero y último, podio con identidad propia, meta gráfica igual al punto competitivo, pagos y avance de día únicos. También tarifas 200/300/400/500 y su duplicación desde el día 8, cobro al inicio, reanudación sin recobro, fallo de guardado con reversión, apuestas opcionales y reserva de inscripción. Se mantuvieron las bandas de rivales y la victoria de segunda carrera tras primera derrota, persistente y sin apuestas; primera victoria no activa el guion.

Progresión: nueva partida con 2500, metas 3000/6000/15000, cierres de días 7/14/21 con meta−1/meta/meta+700, sin descontar meta, premio antes de evaluar el cierre, reserva de subasta separada de pérdidas, saldo cero permitido, total negativo confirmado y derrota persistente. Se invocaron los dos botones de Game Over y se comprobó el bloqueo de Escape. Las pruebas utilizan dinero y archivos aislados, sin tocar la partida personal.

La revisión final detectó que la simulación podía seguir detrás de Game Over si aparecía una derrota durante una carrera activa. Se agregó una guarda de `Derrotado` al avance de CarreraUI. Una prueba confirma saldo negativo durante una carrera, intenta avanzar diez segundos y verifica que ninguna distancia, premio ni día cambie. No se alteran las fórmulas económicas ni de rendimiento.

La compilación Windows conserva una advertencia CS0414 preexistente sobre un campo destinado al Editor. El probe de fallos de guardado provoca deliberadamente un error de escritura y verifica que no se cobre; no es un fallo inesperado del juego.

## Pendiente manual

Comprobar la sensación de continuidad de los dibujos durante reproducción normal y la tecla A física, incluyendo pulsación/soltado y foco. Se validó automáticamente la lógica con el estado de tecla como parámetro, sin simular entradas. No se hizo una prueba de ejecutable de Windows porque esta tarea prohíbe generar la build. Los posibles defectos artísticos internos de los frames existentes no se regeneraron ni se presentan como originales perdidos.

## Repetición de pruebas

Los `.cs` de `Validacion/EscalaFinal/` son herramientas de Editor para copiar a Assets/Editor **de una copia aislada**, no scripts de juego. Ejecutar con el runner aislado de `C:\Caballos\Validation\HubRefresh\Run.ps1` y los métodos `EscalaPresentacionProbe.Run`, `SemanaProbe.Run`, `Carrera2DProbe.Run`, `AnimacionesCatalogoProbe.Run`, `CatalogosWindowsProbe.Run` y `CatalogosAssetProbe.Run`. Los cuatro primeros requieren Play Mode batch (`-Play`); usar `-Graphics` para renders fuera de pantalla. El runner crea rutas de guardado y preferencias únicas y no utiliza el proyecto principal.
