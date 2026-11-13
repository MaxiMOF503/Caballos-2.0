# Repetir las comprobaciones

Estos auxiliares están fuera de `Assets` para que no se compilen ni se incluyan en el juego. Para repetir las pruebas, copiarlos a `Assets/Editor` de una copia aislada del proyecto, conservando `Assets`, `Packages` y `ProjectSettings`. No copiarlos al proyecto abierto por el jugador.

Las pruebas de Play Mode requieren dos variables de entorno: `CABALLOS_VALIDATION_SAVE_PATH`, con una carpeta nueva debajo de `C:\Caballos\Validation\ValidationSave\`, y `CABALLOS_VALIDATION_PREFS_PREFIX`, con un valor único para cada ejecución. Los auxiliares comprueban la ruta antes de iniciar y rechazan un guardado ya existente. No usar el guardado personal como fixture.

Usar Unity 6000.3.13f1 con `-batchmode -projectPath <copia-aislada> -logFile <archivo>` y uno de estos métodos:

- `CatalogosProbe.Completo`: importa las imágenes y comprueba catálogos completos, compras, equipamiento, consumibles, persistencia y memoria. Añadir `-executeMethod CatalogosProbe.Completo`, sin `-quit`; el auxiliar sale al terminar Play Mode.
- `CatalogosUIProbe.Run`: compra, confirmación, consumo y equipamiento desde UI; revisa los 80 detalles en cuatro resoluciones y escribe renders a archivos. Sin `-quit` ni `-nographics`.
- `CatalogosWindowsProbe.Run`: compila únicamente los scripts de Player Windows; añadir `-quit -nographics`. No crea un ejecutable.
- `CatalogosAssetProbe.Run`: revisa scripts y referencias de escenas/prefabs, y los doce perfiles. Añadir `-quit -nographics`.
- `CatalogosBalanceProbe.Run`: evalúa combinaciones de equipo inicial y simula carreras sintéticas. Añadir `-quit -nographics`.

El destino de los informes está definido en cada auxiliar debajo de `C:\Caballos\Validation\Catalogos`. Para medir BC7 y renderizar imágenes, omitir `-nographics` y ejecutar el proceso con ventana oculta. Ningún auxiliar simula entradas del sistema operativo.

En el entorno de validación se detectó un fallo 4551 al cargar una DLL de la caché Burst JIT. Las ejecuciones finales añadieron `--burst-disable-compilation` al comando del proceso aislado; no se alteraron los ajustes del proyecto principal. Los auxiliares de Play Mode esperan dos segundos después del inicio de escena antes de comenzar las compras. Consultar [Resultados.md](Resultados.md) para el alcance y las incidencias de las ejecuciones anteriores.

La prueba de reversión provoca deliberadamente un error de escritura creando un directorio temporal donde se esperaba un archivo. El error de acceso denegado correspondiente forma parte de la prueba; después se verifica que el caballo y el inventario se restauraron y que un nuevo intento puede guardarse correctamente.

La compilación de scripts no valida por sí sola la ejecución del Player ni el empaquetado final de Resources. Esa comprobación requiere la próxima build de Windows. El informe de balance es exploratorio y conserva los valores fundamentales de la economía anterior.
