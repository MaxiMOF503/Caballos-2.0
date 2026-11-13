# Corrección de cámara e Items — 12/11/2026

## Diagnóstico

Se revisó el video adjunto (26,30 s, 1918×870, 30 FPS) con Unity VideoPlayer batch, sin audio ni reproductor interactivo. Se inspeccionaron 27 fotogramas, incluidos los retornos de establo y tienda. El video muestra el comportamiento; la causa se confirmó inspeccionando el código y reproduciéndola automáticamente, sin suponer una trayectoria exacta del mouse a partir de las imágenes.

La cámara anterior tenía dos problemas que las pruebas anteriores de extremos no detectaban:

1. Al volver de una interfaz guardaba una referencia arbitraria del cursor y dividía el recorrido restante entre esa referencia y el borde. Con el cursor al 98% y yaw 0°, un píxel a la derecha en 1920 píxeles producía aproximadamente **2,35°**; después de recalibrar en el centro el mismo píxel producía aproximadamente **0,094°**. La sensibilidad dependía del lugar donde se cerraba el menú.
2. `MoveTowards` perseguía un objetivo absoluto con velocidad limitada. Al detener el mouse antes de alcanzar ese objetivo, la cámara continuaba girando. También introducía retraso entre puntero y orientación. La prueba reproduce ambas condiciones con el controlador anterior antes de verificar la sustitución.

El Input System instalado está habilitado junto con el sistema anterior (`activeInputHandler: 2`). El Hub nuevo lee exclusivamente `Mouse.current.position`; no mezcla esa lectura con ejes relativos o `Input.mousePosition`. El cursor personalizado solo cambia textura/visibilidad y consulta el mismo raycast del clic; no era la causa ni se modificó. `MenuPausa`, UIManager e Interactor se conservaron.

## Control implementado

`Assets/Scripts/CameraController.cs` utiliza un **mapeo absoluto fijo e invertible** del rectángulo de renderizado `Camera.pixelRect`. El extremo izquierdo corresponde a −90°, el derecho a +90° y el centro a 0°. Verticalmente, el centro corresponde a 0°, el borde inferior a +30° y el superior a −25°. Se contemplan dimensiones reales y desplazamiento del viewport.

Se eliminaron las referencias de sensibilidad variable, el objetivo diferido, `MoveTowards` y la velocidad acumulada del Hub. Una muestra válida aplica directamente su orientación; una posición inmóvil no produce giro, con independencia de FPS. Se conservan posición fija, FOV 60° y los límites solicitados. El comportamiento de cámaras que no usan `fixedHubNavigation` se mantiene separado y no interviene en este seguimiento.

El controlador tiene tres estados: suspendido, sincronizando y activo. Mientras exista cualquier interfaz, Escape —incluido sobre otra pantalla—, pérdida de foco o posición fuera del viewport, conserva el ángulo y suspende la lectura. No acumula movimientos. En el Editor solo habilita navegación con la Game View enfocada.

Al cerrar la última interfaz, recuperar foco, cambiar tamaño o volver a entrar al viewport, calcula mediante la conversión inversa el punto de pantalla equivalente al ángulo conservado. Solicita **una sola** llamada a `Mouse.WarpCursorPosition` en esa transición, no un recentrado continuo ni un cursor dibujado en Canvas. En Windows Player mantiene `CursorLockMode.Confined` durante la exploración y lo libera en interfaces o al perder foco; no utiliza `Locked`.

El código instalado de Input System (`Mouse.cs`, documentación de `WarpCursorPosition`) especifica coordenadas del área del jugador y actualización de `Pointer.position` en el siguiente input update. Por eso el controlador descarta lecturas anteriores a la sincronización, conserva el ángulo al confirmarla y admite movimiento posterior que ya haya superado el punto solicitado. El redondeo del cursor a píxeles enteros no provoca deriva al permanecer inmóvil. No se mantienen las ganancias recalibradas del modelo anterior.

**La ejecución batch nunca emite el warp ni cambia el cursor de Windows.** Las pruebas alimentan muestras y transiciones internas; la sincronización física con el sistema operativo, sus escalas DPI y foco real requiere comprobación manual. No se afirma que la cámara esté perfectamente validada con mouse físico por compilar o aprobar esas pruebas.

## Silueta original de Items

El recurso ya existía y seguía asignado a `spriteManiquiItems` en `hub.unity`:

`Assets/Sprites/caballos establo/4d15d7d2-7f84-4c0e-adab-5d5e12ceec8b.png`

Su GUID es `d82e88339aed7e1438918003c62809cc`. Su SHA-256 coincide exactamente con el OID LFS del archivo en `HEAD`: `05333661b5ac42345d1cd7076c52a12975b54f8593acea2e9925e752be2008c5`. No hubo que generar, descargar ni reconstruir una imagen.

La única línea cambiada en `EstabloView.cs` es la asignación del maniquí en `RefrescarItems`: vuelve a utilizar `spriteManiquiItems`, independientemente del caballo seleccionado. El título, la selección y las operaciones de equipo siguen utilizando `CaballoActual`. Stats, Up y Procrear mantienen sus representaciones dinámicas.

No se modificaron `EstabloView.Interfaz.cs`, las cuatro ranuras, sus líneas, sus RectTransform, arrastre/destino, precios, bonificaciones ni funciones de guardado. Se ejercitó el drag/drop real de Unity sobre Cabeza, Espalda, Patas delanteras y Patas traseras, comprobando que cada ítem se asigna al segundo caballo seleccionado y no al primero.

## Validación ejecutada

Unity 6000.3.13f1, copia aislada `C:\Caballos\Validation\MainMenu`, procesos batch ocultos, guardados y preferencias nuevos. Sin Computer Use, entradas físicas de mouse/teclado, cierre de aplicaciones del usuario ni nueva build. Burst se deshabilitó solo en los procesos de validación, sin cambiar la configuración del proyecto.

| Prueba | Resultado |
|---|---|
| `CamaraItemsProbe.Run` | **7812 comprobaciones aprobadas**: reproducción de los dos defectos anteriores; conversiones ida/vuelta; extremos, centro, movimientos lentos/rápidos y detención; sincronía tras distintas posiciones de menú; eventos anteriores/posteriores al warp; redondeo; foco simulado; viewport desplazado, cambio de tamaño y salida/reentrada; tienda, establo, tablón/selector y Escape superpuesto |
| Items, dentro de la misma prueba | Dos identidades distintas, silueta fija, cuatro drops reales, destino canónico correcto, costos y bonificaciones conservados, guardado/carga, cuatro ranuras, y splash dinámicos de Stats/Up/Procrear |
| `CursorProbe.Run` | **110 comprobaciones aprobadas** del cursor, sus estados, interfaces y correspondencia con los raycasts existentes |
| Compilación Windows | **27 ensamblados** compilados correctamente; no se generó un ejecutable |
| Referencias | **12 comprobaciones** de escenas/prefabs y perfiles anteriores, sin referencias perdidas |
| Integridad de caballos | 100 perfiles, 800 frames, sus 200 GUID de PNG/perfil y hashes conservados; catálogo activado, sin editar identidades ni animaciones |

Las conversiones se probaron en 1920×1080, 1600×900, 1366×768, 1280×720, 1918×870 (video), y un viewport con origen desplazado. Se guardaron e inspeccionaron renders de Items en las cuatro resoluciones solicitadas, con la silueta gris y equipo real. Se verificó que la escena del Hub y las dependencias de UI, cursor, interacción, economía y guardado no cambiaron.

Las fuentes y resultados reproducibles están en `Validacion/CamaraItems` dentro del repositorio, fuera de Assets. Los logs están en `C:\Caballos\Validation\HubRefresh\camara-items-*.log`; fotogramas y renders en `C:\Caballos\Validation\CamaraItems`. El controlador anterior solo se usó como referencia de prueba en la copia aislada; no se incorporó un segundo controlador al juego.

## Pendiente de prueba manual

Probar con mouse físico los barridos y la detención, ambos bordes, apertura/cierre de las interfaces y Escape, Alt+Tab, retorno del foco, ventana/pantalla completa y escalas DPI/monitores utilizados por el jugador. En particular, comprobar que la solicitud nativa de sincronización coloca el cursor correctamente en la Game View/ventana y que el primer movimiento mantiene la sensibilidad esperada. Las pruebas batch no sustituyen esta comprobación ni la valoración de comodidad.

## Archivos modificados

Solo dos scripts de juego: `Assets/Scripts/CameraController.cs` y `Assets/Scripts/EstabloView.cs`. Documentación: este informe y enlace de actualización en `Catalogos/Animaciones/Resultados.md`; pruebas auxiliares fuera de Assets. Los respaldos anteriores están en `C:\Caballos\Validation\CamaraItems\Antes`.

Sin cambios de escenas, Hub, menús, recursos gráficos, economía, subastas, carreras, guardados personales o ejecutable. Sin staging, commit ni push.
