# Checkpoint: recuperación visual y menú principal

## Actualización del checkpoint de beta — 6 de noviembre de 2026

Se conservan el establo con Stats, Up, Procrear e Items, sus cuatro ranuras y
arrastre de equipamiento, los doce perfiles provisionales y los recursos legacy.
La crianza continúa deshabilitada; este checkpoint no la presenta como terminada.
Se incluyen la tienda y su vendedor recortado, los recursos del selector de
carreras, el tablón compartiendo la madera del selector, el flujo
tablón → selección → apuesta opcional → televisor, y la cámara absoluta del Hub.

Para este checkpoint se revisaron los resultados recientes de la copia aislada:

- Tienda: seis ofertas, hover, selección, confirmación, doble clic, saldo, stock,
  reroll, equipamiento y JSON; render en cuatro resoluciones.
- Vendedor: transparencia y proporción, con comparación de píxeles del resto
  de la tienda en 1920×1080, 1600×900, 1366×768 y 1280×720.
- Beta: escenarios A–G, carreras con y sin apuesta, persistencia, cambio de
  selección y liquidación única.
- Cámara: 662 comprobaciones con coordenadas simuladas en 1920×1080,
  1600×900 y 1280×720, límites, foco y apertura/cierre de cuatro interacciones.
- La compilación Windows de la tarea de cámara del 6 de noviembre terminó con
  código 0. No se generó otra build al preparar este checkpoint.

La revisión estática del checkpoint encontró los doce perfiles, cada base y sus
ocho frames asociados, sus metas y los nuevos recursos de UI. No hay GUIDs
duplicados ni referencias de recursos nuevos sin resolver. Se revisaron LFS,
exclusiones y posibles credenciales en los archivos pendientes.

Quedan once GUIDs antiguos sin resolver en `PC_Renderer.asset` (recursos de
depuración de probe volumes) y `DefaultVolumeProfile.asset` (componentes
OutlineVolumeComponent, TestAnimationCurveVolumeComponent, OasisFogVolumeComponent
y TestVolume). Ya estaban en el commit anterior y permanecen en la copia que
superó las pruebas y la compilación. No se cambian configuraciones URP para este
respaldo. El warning CS0414 de `reiniciarPartidaAlIniciarEnEditor` tampoco impide
compilar. El confinamiento del puntero físico en Windows sigue pendiente de
comprobación manual.

Los binarios de validación, logs, guardados y respaldos locales quedan fuera del
repositorio. Unity vuelve a serializar espacios después de valores YAML vacíos;
`.gitattributes` permite ese formato en `.meta` y `.asset`, manteniendo los
controles de espacios finales en los scripts. No se modifica lógica del juego
durante la preparación del checkpoint.

## Validación histórica del checkpoint de recuperación

Validado el 3 de noviembre de 2026 con Unity **6000.3.13f1**.
Este checkpoint conserva la recuperación provisional, sin presentar los frames
extraídos del video como equivalentes a los originales perdidos.

## Contenido y apertura en otra computadora

- Assets, Packages y ProjectSettings, con los metadatos Unity correspondientes.
- Doce perfiles, catálogo, doce bases y ocho frames seleccionados por perfil.
  El 02 utiliza el maestro recuperado. Se conservan también sus frames de video.
- Identidad visual persistente, reproducción durante carrera y compatibilidad
  con los 28 sprites legacy, que permanecen en el proyecto.
- MainMenu como primera escena de build, hub conservado, inicio/carga protegidos,
  preferencias y transiciones, y selección con rellenos de rombo exclusivos.
- Scripts de generación y validación reutilizables en Assets/Editor y documentación.

Es necesario tener Git LFS disponible al clonar: los PNG, modelos y otros binarios
usan los filtros de .gitattributes. Si no se descargaron automáticamente, ejecutar
`git lfs pull`. Abrir la carpeta con Unity 6000.3.13f1 y permitir que Unity restaure
los paquetes y genere su Library local. No se requieren las copias de validación,
partidas de prueba ni archivos de solución generados del equipo anterior.

## Validación previa al commit

Se sincronizaron Assets, Packages y ProjectSettings a una copia aislada y se
ejecutaron procesos Unity ocultos con `-batchmode -nographics`, sin controlar el
escritorio ni cerrar el editor del usuario. Los datos y preferencias de prueba
utilizaron rutas/claves propias.

| Entrada executeMethod | Resultado |
|---|---|
| MainMenuValidation.Run | Código 0; 89 comprobaciones Edit Mode y pruebas Play Mode de inicio, carga, corrupción, confirmaciones, datos persistidos y transiciones |
| HorseVisualRecovery.Validate | Código 0; 498 comprobaciones de perfiles y dos carreras cubriendo los 12 perfiles/96 frames; guardado/carga y 28 sprites legacy |
| MainMenuHoverValidation.Run | Código 0; rellenos sólidos, inicio vacío, cuatro opciones, 400 transiciones rápidas, mouse/teclado, raycasts y clics |
| MainMenuValidation.CheckPersistedPreferences | Código 0; preferencias conservadas entre procesos; claves de prueba eliminadas |

La prueba de preferencias usa `-quit`; las de Play Mode asíncronas salen mediante
EditorApplication.Exit al finalizar. Son verificadores executeMethod, no resultados
de NUnit/runTests. No hubo errores críticos de compilación ni assets indispensables
ausentes. Se revisaron metas, GUIDs, referencias de perfiles, filtros LFS y Git.

La excepción conocida del índice UnityEditor.Search en headless se registra aparte
y no se confunde con errores del juego; estos últimos hacen fallar las pruebas.
Los logs crudos y diagnósticos locales no forman parte del checkpoint.

## Límites pendientes de revisión manual

Las pruebas sin gráficos no validan el aspecto final renderizado, halos/calidad de
los frames provisionales, mezcla del fondo reconstruido en el primer rombo, ni
los controles físicos, cambios de pantalla y salida en una build standalone.
Estos límites están detallados en HORSE_VISUALS.md, MAIN_MENU.md y
MENU_DIAMOND_FILL.md. No se modificaron funcionalidades para preparar el checkpoint.
