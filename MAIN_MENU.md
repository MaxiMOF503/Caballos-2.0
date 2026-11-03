# Menú principal — Proyecto Caballos

## Escena y recursos

`Assets/Scenes/MainMenu.unity` es la escena habilitada de índice 0. Le sigue
`Assets/Scenes/hub.unity`. Se conserva `SampleScene.unity` y la configuración de
Input System que ya figuraba en EditorBuildSettings.

La portada se copia sin modificar sus bytes a `Assets/Art/MainMenu/MainMenu.png`.
Se importa como Sprite/Texture2D sin compresión. No se generó ni retocó arte.

Canvas de referencia 1920×1080, CanvasScaler Scale With Screen Size y fondo con
AspectRatioFitter Fit In Parent. La imagen completa conserva su aspecto 1659/948.
En 16:9 quedan márgenes verdes muy estrechos, no bandas negras ni recorte del
caballo/textos. Las cuatro zonas de interacción son hijos del rectángulo de la
imagen y usan anclas normalizadas, no posiciones absolutas de pantalla.

Los botones no tienen texto superpuesto: utilizan los textos dibujados en la
portada. Solo se ilumina un rombo independiente: botones transparentes con
Transition=None, sin rectángulos ni tintes de texto/fondo. Un coordinador mantiene
un único relleno activo entre mouse y teclado; al salir vuelve al foco real de
teclado si existe y, de lo contrario, se apaga. Ver MENU_DIAMOND_FILL.md.
Navegación explícita arriba/abajo y Enter mediante EventSystem/StandaloneInputModule
(el proyecto ya admite ambos sistemas de entrada). Si Cargar está deshabilitado,
la navegación salta esa opción. Settings/Confirmation bloquean las opciones de
fondo; Escape cierra los paneles sin enviar entradas al escritorio.

## Inicio y carga

`GameLaunch` transmite una intención de un solo uso; no transporta un GameManager
persistente entre escenas. GameManager consume esa intención antes de considerar
el reinicio de desarrollo del editor. Abrir el hub directamente conserva su flujo
de desarrollo anterior.

- **Nueva partida:** si existe cualquier JSON de partida, primero muestra la
  confirmación solicitada. Cancelar no escribe. Confirmar carga el hub y llama a
  `IniciarNuevaPartida`, reutilizando dinero inicial, día 1, estadísticas, caballos,
  tienda y guardado originales. No se borra previamente el archivo.
- **Cargar:** SaveSystem comprueba el JSON en lectura, sin crear backups ni
  sobrescribir archivos desde el menú. Si pasa, el hub llama a su `CargarPartida`
  existente. Se conservan identidad visual, estadísticas, dinero, inventario,
  equipamiento, campaña y rival persistente según el formato actual. Los
  contrincantes diarios se generan como antes; no se crea otro caballo jugador.
- Una carga explícita fallida vuelve al menú con mensaje. Nunca cae al camino
  automático de crear una campaña nueva. Se comprueba de nuevo el archivo en el
  hub para cubrir cambios posteriores al preflight.
- La carga explícita no guarda inmediatamente sobre el JSON. Las acciones de
  gameplay posteriores conservan sus puntos de guardado originales.
- **Transición:** fade-out/fade-in de 0,25 s cada uno, con un Canvas temporal que
  sobrevive al cambio de escena y se destruye al terminar. IsBusy impide cargas
  duplicadas. Ante fallo de carga se retira el fade y se informa el error.
- **Salir:** requiere confirmación; en una build llama a Application.Quit. En el
  editor muestra un mensaje sin cerrar ni detener la sesión del editor.

## Configuración

Panel verde oscuro con borde dorado y textos beige, dentro de la misma escena:

- Volumen general 0–100%, aplicado a AudioListener.volume.
- Pantalla completa en ventana sin bordes o modo ventana.
- Resoluciones informadas por Screen.resolutions, sin duplicados de refresco.
- Volver y Escape para cerrar el panel conservando lo aplicado.

PlayerPrefs guarda `Caballos.Settings.Volume`, `Fullscreen`, `Width` y `Height`.
Se aplican al iniciar. No se mezclan con el JSON de campaña. No había AudioMixer,
música ni canales separados de efectos; no se inventó un sistema adicional.
Las preferencias de pantalla no cambian la resolución durante ejecución en
Unity Editor ni durante pruebas batch.

## Archivos

Agregados:

- `Assets/Scripts/MainMenuController.cs`: botones, paneles, confirmaciones y
  componente temporal MenuSceneTransition.
- `Assets/Scripts/GameLaunch.cs`: intención de entrada y error de regreso al menú.
- `Assets/Scripts/MenuSettings.cs`: controles y preferencias simples.
- `Assets/Editor/MainMenuBuilder.cs`: generación reproducible de la escena.
- `Assets/Editor/MainMenuValidation.cs`: pruebas automáticas headless.
- Escena MainMenu, portada y sus metadatos.

Modificados para esta etapa:

- `Assets/Scripts/GameManager.cs`: entrada explícita Nueva/Cargar, sin cambiar la
  lógica interna de generación, estadísticas, economía, carreras o progresión.
- `Assets/Scripts/SaveSystem.cs`: preflight de lectura `PuedeCargar`; se mantiene
  el formato y mecanismo de escritura existentes.
- `ProjectSettings/EditorBuildSettings.asset`: orden e inclusión de escenas.

No se modificaron el hub, sus Canvas/prefabs, cámara, apuestas, pistas,
recompensas, simulación, progresión ni los 12 perfiles y sus animaciones.
Los cambios de la recuperación anterior se conservan y se incluyen en el checkpoint.

## Pruebas ejecutadas sin escritorio

Unity 6000.3.13f1, copia aislada `C:\Caballos\Validation\MainMenu`.
Solo archivos, terminal y procesos batch ocultos; no se controló el editor abierto,
mouse, teclado ni ventanas. Partidas de prueba y claves PlayerPrefs separadas.

Entrada `MainMenuValidation.Run`, `-batchmode -nographics`. La prueba es asíncrona,
por lo que el método sale con EditorApplication.Exit al terminar en vez de usar
`-quit` al inicio. Resultado: código 0.

- Compilación C# correcta y 84 comprobaciones de Edit Mode: referencias, build
  index, componentes sin scripts ausentes, hotspots relativos a la portada y
  cálculo de ajuste uniforme en 1920×1080, 1600×900, 1366×768 y 1280×720.
- Play Mode: menú inicial; ausencia de guardado; JSON corrupto sin alteraciones;
  confirmación/cancelación de nueva partida; protección de archivo válido.
- Eventos internos de navegación y puntero de Unity, no entradas físicas del
  sistema operativo. Apertura/cierre de configuración y confirmaciones.
- Nueva partida confirmada: hub original, día 1, dinero 1000, caballo y progresión
  iniciales existentes. Intentos repetidos no crean transiciones duplicadas.
- Carga real desde disco: día 4 y dinero 2468 de la fixture, ID/perfil, estadísticas,
  nivel/XP, puntos pendientes, inventario/equipo, rival y campaña conservados.
  El JSON no se modifica por la carga explícita. Referencias de cámara/UI intactas.
- Archivo dañado después del preflight: regreso al menú con mensaje, JSON sin
  reemplazar y eliminación del Canvas temporal de transición.
- Segundo proceso Unity con `-quit -executeMethod MainMenuValidation.CheckPersistedPreferences`:
  volumen, resolución y fullscreen sobreviven al reinicio. Código 0. Se eliminan
  únicamente las claves de preferencias de prueba.

Resultados completos en `MenuValidation/results.txt`; logs en esa carpeta.
Las pruebas usan executeMethod, no se presentan como una suite NUnit/runTests.

También se ejecutó `HorseVisualRecovery.Validate` con los scripts del menú ya
integrados: código 0, 498 comprobaciones de perfiles, persistencia y compatibilidad
legacy, y dos carreras cubriendo las doce identidades y sus 96 frames. Informe
en `MenuValidation/horse-regression.txt`. No se transfirieron cambios de perfiles
desde esa prueba a la copia principal.

Incidencias encontradas y corregidas:

- La importación inicial de la portada no produjo un Sprite: el builder ahora
  fija TextureImporterShape.Texture2D explícitamente.
- El color de vértice transparente impedía ver el hover: se conserva blanco y
  se aplica transparencia mediante los estados de Button/CanvasRenderer.
- Igual que en la validación anterior, una excepción de inicio del buscador de
  UnityEditor.Search se registra aparte en editor-diagnostics.txt. Solo se excluye
  esa traza concreta; los errores del juego hacen fallar las pruebas.

## Pendientes manuales

- Revisar el resultado renderizado y alineación final de las cuatro zonas en las
  resoluciones indicadas. La prueba de geometría no es una prueba visual.
- Comprobar hover, clic físico, flechas/Enter, Escape y foco al abrir dropdowns.
- Revisar legibilidad de paneles, márgenes verdes, intensidad del brillo y fades.
- Probar volumen con audio real y fullscreen/resoluciones en una build.
- Probar salida confirmada de una build standalone y funcionamiento visual de
  interacciones y Canvas del hub. No se cerró ninguna aplicación para probarlo.

El checkpoint que agrupa estas etapas se documenta en CheckpointValidation/README.md.
