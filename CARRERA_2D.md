# Carrera 2D — 13/11/2026

## Cambios de esta etapa

Se conserva el cierre al resolver a todos los participantes, el orden interpolado de llegada, el podio, la clasificación completa y la liquidación única. Esta etapa agrega identificación del caballo, inscripción, rivales equilibrados, bienvenida persistente y un recorrido lateral con encuadre propio. No se modificó la cámara del Hub ni su escena.

### Identificación

Una flecha amarilla de geometría pixel art se crea como hija del actor cuyo ID coincide con el caballo del jugador. Aparece durante los primeros dos segundos de carrera y después sólo mientras se mantiene A. Al soltar A se oculta, salvo durante esos dos segundos. Su posición acompaña al actor y su carril, sin modificar estadísticas, velocidad ni progreso. No existe un indicador en cada rival. Se evita usar A para identificar si hay un campo de texto enfocado; no se encontró otro control de carrera asignado a esa tecla.

Las pruebas desplazan al jugador al índice 1 de la parrilla y cambian su posición/carril para comprobar la vinculación; esas modificaciones son fixtures de validación, no cambios de las escenas del juego.

### Inscripción y apuesta

Las tarifas se editan en `Assets/Resources/Carreras/Catalogo.asset`:

| Distancia | Días globales 1–7 | Desde el día global 8 |
| --- | ---: | ---: |
| 1200 m | $200 | $400 |
| 1600 m | $300 | $600 |
| 2100 m | $400 | $800 |
| 2400 m | $500 | $1.000 |

`DefinicionCarrera.inscripcionInicial` y `inscripcionDesdeDia8` permiten una tarifa propia; −1 usa la tabla por distancia. Copa Nocturna tiene explícitamente $500/$1.000. El fallback configurable para distancias nuevas es $500/$1.000. El cálculo usa el día global; el día 15 no vuelve a la tarifa inicial. Se conserva la oferta especial del séptimo día.

El selector muestra la inscripción en una línea separada del tipo y premio. El tablón y la preparación del televisor también muestran inscripción y premio por separado. Elegir, consultar y abrir la preparación no cobran. **La largada efectiva desde el botón del televisor cobra una sola vez** y guarda conjuntamente el saldo, el inicio y la marca de inscripción abonada. Si falta dinero informa el faltante; si falla el guardado revierte saldo y marcas. Reabrir o cargar una carrera iniciada no vuelve a cobrar.

En carreras nuevas, la inscripción **reemplaza** la penalización fija anterior de $200 por perder: no se cobran ambas cosas. El premio y las cuotas confirmadas permanecen como estaban. Los guardados con una carrera ya preparada antes de esta versión conservan sus condiciones anteriores: sin nueva inscripción, sin guion ni cambios de su parrilla, y con su integración de simulación y liquidación previas.

El saldo disponible ya excluye la reserva de subasta, cuyo sistema no se modificó. Una apuesta nueva no puede consumir la inscripción pendiente. Durante la primera carrera se protege además el importe mínimo de otra entrada ($200 con el catálogo actual). Después de una primera derrota, si la oferta no tiene ninguna opción pagable, se incluye la carrera normal más económica, manteniendo la especial semanal. Se comprobó el caso de debut con $400 reservados en subasta y apuesta al máximo permitido: perder deja $200 disponibles para disputar la iniciación, sin añadir dinero al jugador.

### Rivales y bienvenida

`bandasRivales` en el catálogo utiliza las estadísticas efectivas del caballo activo, incluyendo sus bonificaciones existentes. Sus valores iniciales son:

- Dispersión: ±1 punto.
- Ajuste inicial: −2; ajuste avanzado: +1 desde el día global 15.
- Especialidad por distancia: 1 punto; en corta favorece velocidad y en larga stamina.

La identidad del rival persistente se conserva; para la nueva competición sus atributos se encuadran en esas bandas. Las semillas, estadísticas, identidades y cuotas quedan congeladas al preparar la parrilla. Abrir tablón/TV o guardar y cargar no regenera participantes.

Una muestra reproducible de 60 carreras cortas normales, usando el motor y un caballo de velocidad 5/stamina 4, obtuvo 24 victorias y 36 derrotas. Es una comprobación de variedad de esta configuración, no una promesa de porcentaje para cualquier caballo, distancia o partida.

`inicioCarreras` guarda versión, cantidad de carreras completadas y resultado del debut. En partida nueva la primera carrera es normal. Si se pierde, la segunda se prepara como iniciación; si se gana, la segunda es normal. Desde la tercera no se aplica guion. Abandonar o recargar no incrementa el contador. Una partida anterior sin estos datos se considera migrada y no reactiva la bienvenida.

La iniciación bloquea apuestas desde la preparación, tanto en la UI como en la API. Mantiene inscripción y premio. Su parrilla guarda ritmos antes de largar: el jugador usa el 80% de la velocidad máxima correspondiente a su velocidad efectiva; los rivales, entre 82% y 94% de ese ritmo (configurable en el catálogo, limitado a 50–95%). Todos aceleran proporcionalmente a su ritmo durante medio segundo. En esa única carrera no interviene el azar de rendimiento: el jugador avanza más rápido desde el comienzo y cruza primero realmente. No se cambia el ganador en resultados ni se salta a la meta.

Las fórmulas normales de velocidad, energía, resistencia y suerte se conservaron. Las carreras nuevas se integran con pasos de 0,02 s, independientes del render. Las parrillas legacy mantienen la cadencia anterior; no se les aplica retroactivamente la configuración de bienvenida.

### Recorrido y presentación

`PistaLateralCarrera` crea `CarreraLateral2D` sólo dentro de la vista de carrera. Reutiliza los controles de inicio/volver y el texto de estado; reemplaza en ejecución la imagen estática anterior. No cambia el archivo de escena ni otros Canvas.

`MapaPistaCarrera` realiza la única conversión: `X = 320 + progreso * 24`, con progreso limitado a la distancia simulada. La meta dibujada utiliza exactamente `X(distancia)`. Se agrega un pequeño punto de referencia bajo cada caballo: ese punto competitivo coincide con la línea al registrar la llegada. El criterio no es la punta de la crin de un frame ni el borde de la pantalla. Se mantienen los sprites, identidades y animadores existentes.

El recorrido crece según los metros: césped, tribuna y baranda usan tramos repetidos, sin estirar una sola imagen hasta la meta. El encuadre sigue al jugador, dejándole normalmente 62% del ancho hacia delante, y se limita al escenario. Después de su llegada se queda cerca de la meta mientras continúan los demás. Los resultados no se abren antes del último.

Se separan césped/pista, baranda, tribuna y paisaje. La tribuna se desplaza al 70% del encuadre y el paisaje al 12%, con continuidad entre tramos. Banderines y espectadores adicionales tienen ciclos propios que cambian incluso cuando la cámara está quieta. Se reutiliza `Assets/Sprites/Fodos/sprite fondo.png`, sin editar el PNG ni su importador; se copia su textura en GPU para aplicar Point sólo a esta presentación.

## Validación

Unity 6000.3.13f1, en `C:\Caballos\Validation\MainMenu`, con `-batchmode`, procesos ocultos y rutas de guardado exclusivas. Los renders usan RenderTexture; no se enviaron eventos al teclado/mouse de Windows. La ejecución de Play Mode batch termina mediante `EditorApplication.Exit`, sin usar `-quit` prematuramente.

`Carrera2DProbe.Run` aprobó **354 comprobaciones de modelos** y **17.573 comprobaciones de integración** en la ejecución final registrada. La tecla A se prueba pasando su estado lógico al mismo método de presentación usado por `Update`; no se inyectan eventos al teclado físico. Incluye:

- Tarifas día 7/8/15, tarifa especial y una distancia nueva configurable.
- Bandas acotadas, posibilidad de ganar/perder, guion con las 16 velocidades en corta y larga.
- Inscripción única, reserva de subasta, límite de apuestas, falta de saldo, rollback de inicio fallido y JSON anterior conservado.
- Primera derrota seguida de victoria real en la segunda, primera victoria seguida de segunda normal, tercera sin guion, recarga y abandono/reanudación.
- Migración de una partida avanzada con apuesta y carrera iniciada: identidad, cuotas, parrilla, integración anterior y dinero conservados.
- Flecha por ID fuera del carril central; aparición inicial, mantener/soltar A y ausencia de cambios en el rendimiento.
- Cortas y largas con jugador primero/último, continuidad tras primeras llegadas, posiciones únicas, cruce de la referencia visible y la meta en coordenadas del Canvas, podio con splash correctos y pagos únicos.
- Renders a 1920×1080, 1600×900, 1366×768 y 1280×720; selector, tablón, preparación, flecha, pista y podio. Comparación de imágenes con cámara quieta para verificar animación ambiental real.

La beta A–G también pasó con carreras con/sin apuesta, equipo legacy, selección y persistencia; se actualizaron únicamente sus expectativas de saldo para contar la inscripción. `CarreraCierreProbe` aprobó **1.056 comprobaciones adicionales** de bandas por distancia, atributo y días 1/8/15/28, compiló **27 ensamblados para Windows** y aprobó **12 comprobaciones de referencias**. La regresión de los 100 perfiles/animadores aprobó **2.634 comprobaciones**, incluida una carrera real, sus ocho fases propias y restauración de identidad. Las ejecuciones y rutas se registran en `Validacion/Carrera2D/resultados.txt`.

La integridad por hashes confirma Hub, cámara, establo, tienda, código de subastas, animador y pantalla de resultados anteriores intactos; 100 perfiles, 800 frames y 100 splash conservados. El código principal coincide con la copia probada. No se alteró el ZIP anterior de la beta.

## Pendientes visuales y manuales

El paisaje de formas pixeladas, los espectadores adicionales y los banderines son **arte provisional hecho mediante geometría**, combinado con el público y césped recuperados. No son ilustraciones definitivas nuevas. Quedan pendientes fondos multicapa específicos por hipódromo y ciclos de público dibujados dedicados. Los encuentros entre tramos del recurso original requieren valoración artística, especialmente durante paneos prolongados.

Queda pendiente probar físicamente mantener/soltar A, la comodidad del seguimiento en ventana/pantalla completa y la legibilidad a escala/DPI del usuario. Los renders y eventos automáticos no sustituyen esa revisión. No se controló el escritorio ni se generó un nuevo ejecutable.

## Archivos

- Nuevos scripts: `Assets/Scripts/ReglasCarrera.cs`, `Assets/Scripts/PistaLateralCarrera.cs` y sus `.meta`.
- Simulación/estado: `CarreraUI.cs`, `CarreraProgramada.cs`, `GameManager.Carrera.cs`, `GameManager.cs`, `GameData.cs`.
- Catálogo/tarifas: `CatalogoCarreras.cs`, `Assets/Resources/Carreras/Catalogo.asset`.
- Presentación de inscripción: `SeleccionCarreraView.cs`, `TablonView.cs`, `TablonLayout.cs`.
- Validación existente: `Assets/Editor/BetaJugableValidation.cs`.
- Informe y pruebas reproducibles: este documento y `Validacion/Carrera2D`, fuera de Assets.

Objetos creados en ejecución: `CarreraLateral2D/ViewportPista`, `PaisajeParallax`, `TribunaParallax`, `Recorrido` con `TramosCesped`, `Baranda`, `Largada`, `Meta` y `Participantes`. Los actores contienen `PuntoCompetitivo`; sólo el del jugador contiene `IndicadorJugador`. Se usan RectTransform, RectMask2D, RawImage/Image y gráficos propios con CanvasRenderer. Los Button y TMP existentes del inicio se reutilizan; no se guardan estos cambios como modificaciones de la escena.

Este informe actualiza las condiciones económicas descritas en `CARRERAS_RESULTADOS.md`. Respaldos de esta etapa: `C:\Caballos\Validation\Carrera2D\Antes`; logs: `C:\Caballos\Validation\HubRefresh\Carrera2D-*.log`; renders: `C:\Caballos\Validation\Carrera2D`. Sin staging, commit, push ni build de distribución.
