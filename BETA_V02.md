# Caballos 2.0 — Beta académica v0.2 para Windows

Entrega generada con Unity 6000.3.13f1 desde la copia local actual, incluidos sus cambios sin commit. No se realizó commit ni push. La Beta v0.1 no se sobrescribió.

## Archivo para entregar

**Entregar el ZIP completo, no el ejecutable suelto.**

- Ejecutable: `C:\Caballos\Builds\Caballos-2.0-Beta-v0.2\Caballos-2.0.exe`.
- ZIP: `C:\Caballos\Builds\Caballos-2.0-Beta-v0.2-Windows.zip`.
- Tamaño ZIP: **118.946.576 bytes**, 118,95 MB / 113,44 MiB.
- SHA-256 ZIP: `FCEC8112DB246CA25682B752096521739593A4C6B248D98B013028909ACBD3E9`.
- Distribución: 183 archivos, incluidos `_Data`, `UnityPlayer.dll`, `MonoBleedingEdge`, `D3D12`, dependencias generadas por Unity y `LEEME.txt`.
- Extracción de verificación: `C:\Caballos\Validation\BetaV02\ExtractedZip\Caballos-2.0-Beta-v0.2`.

## Aviso temporal de subasta

El aviso anterior marcaba `leido` sólo después de diez segundos y anulaba su identidad visible al salir del Hub. Al regresar encontraba el mismo aviso pendiente y reiniciaba la cuenta, por lo que podía seguir reapareciendo durante varios días.

Se modificó únicamente `Assets/Scripts/AvisosSubasta.cs` como código de producción:

- La lectura se guarda al mostrar por primera vez. Los resultados resueltos fuera del Hub permanecen pendientes hasta poder mostrarse.
- Cada aviso dispone de cinco segundos usando `Time.realtimeSinceStartupAsDouble`, sin depender del calendario ni de `Time.timeScale`.
- Los avisos se procesan en orden; el siguiente recibe un plazo propio. No existen coroutines antiguas que puedan ocultarlo.
- Si se abre un menú o se abandona el Hub, el aviso mostrado se retira y no reaparece al regresar. Tampoco reaparece al cargar o avanzar de día.
- Una carga/nueva partida invalida el panel anterior por referencia al estado de avisos.
- Si falla la escritura, no se muestra ni se consume el aviso; queda pendiente para cuando pueda guardarse.
- Se conservan el diseño y el texto dinámico. No se borra el resultado, ni se toca la entrega o el precio del caballo. Se reutiliza el campo `leido` existente; no se cambia el formato del guardado.

## Compilación

Se reutilizó el procedimiento de v0.1, con salida nueva, producto Caballos 2.0 y bundleVersion 0.2.0. La configuración de compilación se aplicó únicamente a la copia aislada `C:\Caballos\Validation\MainMenu`. Se cotejaron por SHA-256 los 1.520 archivos originales de Assets, Packages y ProjectSettings antes de las pruebas. Se apartaron restos de diagnóstico antiguos de esa copia, sin eliminarlos ni tocar el proyecto principal.

Resultado: **Succeeded**, **0 errores**, target `StandaloneWindows64`, `BuildOptions.None`, Development=False y Debugging=False. Los dos avisos de compilación corresponden al campo preexistente sin uso `GameManager.reiniciarPartidaAlIniciarEnEditor` (CS0414). Se validaron los doce perfiles recuperados y las cien identidades de carrera disponibles.

Escenas incluidas, en orden:

1. `Assets/Scenes/MainMenu.unity`.
2. `Assets/Scenes/hub.unity`.
3. `Assets/Scenes/SampleScene.unity`.

Se conservaron el backend y el renderizado existentes. La ventana inicial es de 1920×1080, redimensionable. Los símbolos `DoNotShip` se conservaron fuera de la distribución. No se incluyeron fuentes C#, guardados, cachés, logs, PDB ni verificadores de diagnóstico en el ZIP.

## Pruebas ejecutadas

- `AvisoBetaV02Probe`: **27 comprobaciones aprobadas**. Incluye una subasta realmente ganada con reserva de $1.150, entrega única, resultado persistente, pendiente fuera del Hub y carga previa a mostrar; visibilidad antes de los cinco segundos, desaparición con `Time.timeScale=0` sin avanzar de día; no repetición tras regresar, cargar y avanzar día. También cola de avisos, límites exactos, interrupción por menú, carga mientras está activo, nueva partida y fallo de escritura controlado.
- `SubastasProbe`: **163 comprobaciones aprobadas**, incluyendo reservas, IA, victoria/derrota, entrega única, identidad, guardado/carga, migración y cancelación. Se actualizó sólo su fixture aislada del cierre de campaña para esperar $50.000 conservados, acorde con la progresión actual que no descuenta metas semanales; no se modificó la economía del juego.
- `Carrera2DProbe`: **17.573 comprobaciones de integración aprobadas**, con verificaciones repetidas por tick; no son 17.573 casos independientes. Incluye todos los finalistas, clasificación, pago único, inscripción, reanudación, apuestas opcionales y victoria de iniciación.
- `BetaJugableValidation`: escenarios **A–G aprobados**, con y sin apuesta, persistencia, equipamiento, selección y liquidación única.
- `CatalogosAssetProbe`: **12 comprobaciones aprobadas** de referencias, escenas, prefabs y perfiles recuperados.
- Ejecutable PE AMD64. ZIP extraído aparte y los **183 archivos coinciden por SHA-256** con la distribución original; ejecutable y dependencias completos.
- **Arranque del ejecutable final extraído** observado durante 60 segundos con `-batchmode -nographics -noaudio`. Inicializó Unity y cargó los ensamblados sin excepciones de carga. No atendió `-quit`; al terminar la observación se detuvo únicamente ese proceso headless propio, PID 29756. No se presenta como prueba de salida normal mediante Escape.
- El archivo personal `gameData.json` permaneció sin cambios durante el arranque. Los 1.520 archivos del proyecto principal conservaron sus hashes durante las validaciones y la build. `git diff --check` sin errores. El ZIP v0.1 conserva el SHA-256 `E5D2CE8B9729EB9E83EEE9001A6E549451C509322C48CE76F2589F814E8A779F`.

Las regresiones funcionales se ejecutaron en Play Mode batch del Editor, con rutas de guardado y preferencias aisladas. La prueba del ejecutable de entrega cubre su arranque, no una partida completa standalone. No se controló el escritorio ni se cerró el Editor del usuario.

El probe de fallo de escritura provoca un error esperado y comprueba la recuperación. El primer intento de validación encontró una prueba antigua de cámara que dependía de archivos de comparación ajenos al proyecto; se apartó esa prueba de la copia aislada y la repetición pasó. No era un error de compilación del código de producción.

## Pendiente manual

Gráficos y fluidez del ejecutable, audio, mouse y teclado físicos, foco y cambios de resolución, salida normal mediante Escape/Salir y arranque en otra PC sin Unity. No se dan estas comprobaciones por aprobadas. La build se entrega como beta académica y conserva posibles defectos visuales conocidos.

## Archivos y evidencia

Además de `AvisosSubasta.cs`, se agregaron este informe y los scripts/reportes de `Validacion/BetaV02/`, fuera de Assets. Allí están el probe del aviso, el constructor, el runner, el empaquetador, el smoke test, LEEME y los resultados. Los logs completos y la copia de fuentes comprobadas permanecen en `C:\Caballos\Validation\BetaV02` (el aviso se registró en `C:\Caballos\Validation\HubRefresh\BetaV02-Aviso2.log`). Estas herramientas no forman parte del ZIP de entrega. El script original del aviso se preservó en `C:\Caballos\Validation\BetaV02\Antes`.

## Correspondencia con la publicación

La etiqueta `v0.2.0-beta` debe apuntar al commit que conserva los archivos de `Validacion/BetaV02/procedencia-build.json`, no al checkpoint previo que aún no incluía estos cambios. Este manifiesto conserva los hashes de los 1.520 archivos de entrada registrados antes de compilar, el hash del manifiesto original y el tamaño/SHA-256 del ZIP. Se comprobó nuevamente que el proyecto conserva esos mismos bytes; no se recompiló ni reempaquetó para publicar.

De esas entradas, tres son copias automáticas `_Recovery` no referenciadas por las escenas de build y se conservan sólo localmente. Las otras 1.517 se incluyen en el proyecto versionado. Los filtros de Git pueden normalizar finales de línea; los binarios se preservan mediante sus OID de Git LFS.

La receta `BetaV02Build.cs` fija producto, versión 0.2.0, resolución, modo de ventana y opciones de entrega en la copia aislada. Se registran aparte cuatro diferencias observadas después de compilar: esos ajustes de PlayerSettings y batching, las listas/prefiltros que Unity genera para URP y el registro UnityConnect. No hay diferencias de código C#, escenas ni texturas respecto de las fuentes registradas. Estos archivos derivados no se copian sobre la configuración de trabajo para publicar. El ZIP se adjunta exclusivamente como asset de la Release.
