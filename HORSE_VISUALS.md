# Recuperación provisional de los 12 perfiles visuales

## Alcance

Se reutilizan `Caballo`, `GameManager`, `GameData`, `SaveSystem` y `CarreraUI`.
No se cambian la simulación, estadísticas, premios, progresión, pistas, cámaras,
Canvas, escenas ni prefabs. No se eliminan los 28 sprites legacy.

Los recursos proceden de `recuperacion_caballos_12_animaciones_provisional.zip`.
Los frames recuperados NO equivalen a los originales perdidos. No se generaron
imágenes ni se retocaron los PNG de origen.

## Catálogo y reproducción

- `Assets/Resources/HorseVisualCatalog.asset` contiene 12 perfiles.
- `Assets/Resources/HorseProfiles/horse_01.asset` a `horse_12.asset` guardan el ID
  estable, sprite base, ocho frames ordenados, FPS y procedencia provisional.
- El perfil `horse_02` usa exclusivamente los ocho PNG de `master`; los otros
  once usan los frames correspondientes de `video`.
- Se importan 116 PNG: 12 bases, 96 frames de video y ocho maestros. Los ocho
  frames de video del 02 se conservan pero no se usan en su perfil. El catálogo
  referencia 108 sprites: 12 bases y 96 frames de carrera.
- Importación: Sprite Single, Point, sin mipmaps, sin compresión, 100 PPU,
  pivote central y malla Full Rect. Cada celda conserva sus 512 × 512 píxeles.
- `HorseVisualAnimator` cambia el sprite del `Image` existente; no aplica root
  motion ni modifica posición, escala, carril o estadísticas.
- FPS inicial: 9, configurable por perfil. Ciclo lógico: 8/9 s. Iniciar una
  carrera reinicia la reproducción; terminarla o desactivar el objeto restaura
  el sprite base del mismo perfil.
- Reconstruir el catálogo conserva los FPS configurados de los perfiles válidos.

## Persistencia y compatibilidad

`visualProfileId` es independiente del ID individual del caballo y de su carril.
Se asigna a caballos nuevos y se incluye en `CaballoData`, tanto para jugador
como para rival persistente. Distintos caballos pueden compartir un perfil.

El formato JSON conserva la versión 1: el campo añadido es opcional. Al cargar
un caballo anterior sin perfil se asigna `legacy:<spriteIndex>` y se mantiene
su sprite exacto. Guardar y volver a cargar conserva esa migración. Los caballos
legacy continúan estáticos: no se sustituyen automáticamente por una de las
doce apariencias nuevas ni se les asigna una animación aproximada.

Un perfil desconocido conserva su ID y utiliza el mecanismo legacy de respaldo
con advertencia. No se modifica el orden del array `spritesCaballos` de la escena.

El proyecto ya tenía `reiniciarPartidaAlIniciarEnEditor` activado. Esa opción
inicia deliberadamente una campaña nueva al entrar en Play Mode. No se cambió:
para comprobar persistencia manual entre sesiones hay que desactivarla en el
GameManager. Las pruebas automáticas llaman directamente al guardado/carga.

## Validación automática sin escritorio

Versión: Unity 6000.3.13f1, obtenida de `ProjectSettings/ProjectVersion.txt`.
Se ejecuta sobre `C:\Caballos\Validation\HorseVisuals`, una copia independiente
de Assets, Packages y ProjectSettings. No se copia Library ni Temp. No se cierra
ni controla el editor abierto en la carpeta principal.

Entrada: `HorseVisualRecovery.Validate`, con `-batchmode -nographics`.
Este método entra en Play Mode, espera a que terminen las pruebas y sale con
`EditorApplication.Exit`. No se añade `-quit`, que terminaría el proceso antes
de completar estas pruebas asíncronas. No se usa NUnit ni se presentan estas
comprobaciones como resultados de `-runTests`.

Los guardados se redirigen SOLO en el editor mediante
`CABALLOS_VALIDATION_SAVE_PATH` o una ruta temporal de SessionState. En una
compilación del juego se mantiene `Application.persistentDataPath`.
Para repetir la prueba batch, usar una ruta de guardado de prueba nueva:

```powershell
$env:CABALLOS_VALIDATION_SAVE_PATH = 'C:\Caballos\Validation\ValidationSave\Run-' + [guid]::NewGuid().ToString('N')
$validationProcess = Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe' -ArgumentList @(
    '-batchmode', '-nographics',
    '-projectPath', 'C:\Caballos\Validation\HorseVisuals',
    '-executeMethod', 'HorseVisualRecovery.Validate',
    '-logFile', 'C:\Caballos\Validation\unity-recheck.log'
) -WindowStyle Hidden -PassThru
$validationProcess.WaitForExit()
$validationProcess.ExitCode
```

Pruebas implementadas:

1. Catálogo completo, 12 IDs únicos y ocho referencias distintas por perfil.
2. Ruta exacta de cada base y cada frame, orden 01–08 y prioridad del maestro 02.
3. Dimensiones, pivote, PPU, transparencia y configuración pixel art en Unity.
4. Serialización/deserialización JSON de las 12 identidades.
5. Generación real de rivales repetida 20 veces.
6. Guardado y carga a disco mediante GameManager y SaveSystem de los doce
   perfiles, comprobando ID individual, identidad, sprite, estadísticas, dinero
   y rival persistente.
7. Guardados antiguos con los 28 sprites reales: carga, migración, guardado y
   segunda carga sin cambiar las apariencias; también rival legacy.
8. Dos carreras reales de CarreraUI con seis participantes cada una. Se observa
   que cada Image reproduce únicamente los frames de su perfil, que se recorren
   los ocho frames y que al terminar se restaura el sprite base correcto.

Resultados: `VisualValidation/results.txt`. La comprobación de PNG y los límites
de transparencia están en `png-audit.csv`; medidas de alineación en
`alignment-audit.csv`. `Audit-Png.ps1` es una auditoría de lectura para Windows
PowerShell 5.1 con System.Drawing; no modifica imágenes.

Resultado final ejecutado: compilación C# correcta, 498 comprobaciones de Edit
Mode y ambas carreras de Play Mode aprobadas; proceso Unity terminado con código
0. Sin errores del juego en la ejecución final. Se verificó igualdad SHA-256 de
308 archivos de código/recursos entre la copia principal y la validada, y se
comprobó que los 116 PNG mantienen los bytes de los recursos recuperados. Véanse
`VisualValidation/asset-verification.txt` y `VisualValidation/unity-batch.log`.

## Incidencias y límites

- Se corrigió el error de compilación del importador: `spriteMeshType` se
  configura mediante `TextureImporterSettings`, compatible con esta versión.
- El editor headless arroja `ArgumentOutOfRangeException` desde
  `UnityEditor.Search.SearchInit.IndexationOnStartup`. Se registra por separado
  en `editor-diagnostics.txt`. La exclusión se limita a esa traza del buscador;
  los errores y excepciones del juego siguen haciendo fallar la prueba.
- Una ejecución sobre un directorio de guardado de prueba reutilizado falló al
  reemplazar un JSON. Un ensayo aislado de File.Replace con/sin backup funcionó,
  al igual que las escrituras sucesivas de la prueba completa en un directorio
  nuevo. No se identificó una causa definitiva ni se cambió la lógica de
  reemplazo de SaveSystem para ocultar ese incidente.
- Los 116 PNG son 512×512, tienen transparencia y no tocan los bordes externos
  con píxeles visibles. Esto no descarta recortes ya presentes en la fuente.
- Los frames del video muestran menor detalle y halos, especialmente alrededor
  de patas/crines. La selección de fases procede de una recuperación aproximada.
- El ancho visible de las bases va de 438 a 476 px; los frames de video suelen
  medir 447 px y el maestro 458 px. Puede percibirse un cambio de tamaño al pasar
  de base a carrera. No se alteraron los PNG para disimularlo.
- En el maestro 02, el borde superior visible oscila entre Y=126 y 130 y el
  inferior entre Y=371 y 395. Parte es movimiento de patas; las métricas no
  sustituyen la revisión de alineación durante la reproducción.

## Pendiente de revisión manual en Unity

El modo `-nographics` valida lógica y referencias, pero no el resultado renderizado.
Quedan pendientes: escala aparente dentro del Canvas, halos contra varios fondos,
alineación visual de cascos/cuerpo, fluidez del salto 08→01 y valoración de 9 FPS.
No se afirma que esas comprobaciones visuales se hayan realizado en esta etapa.

Esta etapa se validó antes del checkpoint documentado en CheckpointValidation/README.md. La recuperación visual queda marcada como
provisional y las etapas de pistas/cámara/categorías siguen fuera de alcance.
