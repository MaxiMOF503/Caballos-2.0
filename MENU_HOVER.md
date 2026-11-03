# Corrección de selección del menú

Registro de la etapa anterior. El estado actual de rellenos sólidos y la corrección
de la marca incrustada en Nueva partida se describen en MENU_DIAMOND_FILL.md.

Se eliminó Color Tint de las cuatro opciones de MainMenu. Los Button usan
Transition=None y sus Image permanecen habilitados, con alfa cero y raycastTarget
activo. No se tiñen rectángulos, texto ni fondo, tampoco al pulsar o deshabilitar.

GameObjects modificados dentro de MainMenuCanvas/Background/MenuButtons:

- MenuButtons: componente nuevo MenuDiamondSelection, único dueño del brillo.
- Button_NewGame, Button_LoadGame, Button_Settings y Button_Exit: Button sin
  transición, Image transparente y componente MenuDiamondOption para observar
  eventos de puntero/selección sin sustituir los callbacks de clic.
- Cada Button tiene un hijo nuevo DiamondGlow, con RectTransform, CanvasRenderer
  y MenuDiamondGlow. Los cuatro quedan desactivados en la escena guardada.

El indicador es un anillo de rombo con gradiente de alfa, dibujado con geometría
UGUI independiente. No es una imagen rectangular y no recibe raycasts. No se
modificó ni regeneró la portada. Centros medidos en el PNG 1659×948, desde arriba:

| Opción | Centro X | Centro Y |
|---|---:|---:|
| Nueva partida | 105 | 397 |
| Cargar | 105 | 479 |
| Configuración | 105,5 | 552,5 |
| Salir | 106 | 628 |

Cada indicador ocupa una celda de 48×48 píxeles fuente, con anclas relativas a su
botón, que ya está anclado al rectángulo real de la portada. CanvasScaler y
AspectRatioFitter se mantienen sin cambios. El brillo no alcanza los textos.

El coordinador apaga todos los indicadores antes de activar uno. Entrar/mover el
puntero toma el control visual; salir lo apaga y no recupera una selección vieja.
Las flechas toman el control mediante los eventos Move/Select de Unity. Los
Select programáticos del inicio o de un clic no dejan brillos persistentes. Las
salidas tardías del botón anterior no apagan el nuevo. Abrir paneles o bloquear
las opciones limpia el indicador y su modo de selección anterior.

Archivos nuevos:

- Assets/Scripts/MenuDiamondGlow.cs
- Assets/Scripts/MenuDiamondSelection.cs
- Assets/Scripts/MenuDiamondOption.cs
- Assets/Editor/MainMenuHoverSetup.cs
- Assets/Editor/MainMenuHoverValidation.cs

Archivos existentes modificados:

- Assets/Scenes/MainMenu.unity: solamente los cuatro botones, MenuButtons y los
  nuevos indicadores/componentes. Los demás bloques YAML existentes se conservan.
- Assets/Editor/MainMenuBuilder.cs: generar el menú reproduce la nueva presentación.
- Assets/Editor/MainMenuValidation.cs: sustituye la expectativa antigua de Color
  Tint por Button transparente con Transition=None.
- MAIN_MENU.md: descripción actualizada de la selección.

MainMenuController, GameLaunch, MenuSettings, SaveSystem, GameManager, escenas
restantes, portada, cámaras, otros Canvas, perfiles y animaciones no se alteraron.

## Pruebas ejecutadas

Unity 6000.3.13f1 en copia aislada, mediante batchmode/nographics y
MainMenuHoverValidation.Run. Código final 0; no se utilizó el escritorio.

- Las cuatro opciones: entrar ilumina solo su rombo; salir apaga todos.
- 400 transiciones rápidas, incluyendo entrar en el nuevo antes de recibir la
  salida tardía del anterior. Se comprueba exclusividad tras cada evento.
- Flechas, hover después de teclado, movimiento de puntero sin un nuevo Enter,
  salida sin recuperar selección vieja y apertura/cierre de configuración.
- Opciones deshabilitadas y grupo bloqueado no mantienen indicadores.
- Raycasts reales de GraphicRaycaster alcanzan los cuatro Image transparentes;
  los indicadores no interceptan el puntero.
- Eventos PointerClick de Unity ejecutan las cuatro funciones existentes:
  confirmación de Nueva partida, validación de Cargar, Configuración y confirmación
  de Salir. Se usan archivos de prueba aislados; no se modifica una partida real.
- Centros y tamaño uniforme comprobados matemáticamente para 1920×1080,
  1600×900, 1366×768 y 1280×720. Topología del anillo verificada, sin quad de fondo.
- Comparación de hashes de archivos protegidos y auditoría de bloques de escena.

El primer verificador de malla encontró una ambigüedad de reflexión entre
sobrecargas heredadas de OnPopulateMesh; se corrigió en la prueba con DeclaredOnly.
También se corrigió el orden de using del editor al añadir la preservación de
bloques YAML. La compilación final y todas las pruebas anteriores pasan.

Resultados en HoverValidation/results.txt y verification.txt. La excepción
conocida de inicio de UnityEditor.Search, si aparece, se registra aparte como en
las pruebas anteriores; los errores del juego siguen haciendo fallar la ejecución.

Pendiente manual: valorar intensidad/suavidad y alineación renderizada del brillo
con el mouse real. Las pruebas de eventos, geometría y raycasts no se presentan
como una inspección visual de pantalla. El checkpoint se documenta en CheckpointValidation/README.md.
