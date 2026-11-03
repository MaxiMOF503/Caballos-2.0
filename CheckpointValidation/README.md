# Checkpoint: recuperación visual y menú principal

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
