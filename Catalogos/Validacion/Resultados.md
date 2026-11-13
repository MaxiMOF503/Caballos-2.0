# Resultado de la ampliación de catálogos

Validado el 12 de noviembre de 2026 con Unity 6000.3.13f1, en una copia aislada. No se utilizaron controles del escritorio ni guardados personales. No se hizo commit, push ni una build de distribución.

## Contenido entregado

| Contenido | Cantidad y estado |
| --- | --- |
| Ítems | 80: 20 de cabeza, 15 de espalda, 30 de patas y 15 consumibles |
| Iconos | 74 nuevos y 6 originales conservados; los 80 tienen imagen propia |
| Identidades de catálogo | 100, separadas del ID persistente de cada instancia |
| Splashes | 100 PNG nuevos y distintos, revisados contra pelaje, crin y mantilla de su perfil |
| Animación reutilizada | 12 perfiles existentes, con 8 frames cada uno: 96 frames conservados |
| Arte pendiente | 0 |

Los CSV completos están en [Items.csv](../Items.csv) y [Caballos.csv](../Caballos.csv). [ProduccionVisual.json](../ProduccionVisual.json), [ManifiestoVisual.json](../ManifiestoVisual.json) y [PromptsVisuales.json](../PromptsVisuales.json) registran recursos, referencias, estados y prompts. La herramienta utilizada fue `image_gen` integrada, una solicitud por recurso; dos recursos requirieron además retirar fondos residuales. Esas correcciones no cuentan como iconos o splashes adicionales. El marcador de interrogación de respaldo tampoco cuenta entre los 180 recursos.

Se conservan las seis definiciones anteriores, sus IDs, precios y PNG. Los 28 sprites legacy y las animaciones recuperadas no se eliminaron. El catálogo no añade cien caballos al establo del jugador.

## Integración

- Tienda: mantiene seis espacios, tres ofertas normales, stock, renovación, confirmación y compras sobre el catálogo real de 80 productos.
- Establo: usa el inventario y las cuatro ranuras existentes. Admite equipo nuevo con niveles mínimos y bonificaciones combinadas. Los consumibles se usan desde el modal existente, sin equiparlos ni arrastrarlos a ranuras.
- Consumibles: una unidad por caballo y día, antes de preparar la carrera; preparación máxima +2 por atributo y expiración al avanzar el día. La XP es permanente. Si falla el guardado, se restauran unidad, nivel, puntos, XP, preparación, límite diario y estado aleatorio.
- Subastas: eligen identidades del catálogo y conservan ciclo de cuatro días, pujas, revelaciones, reservas y entrega. El caballo ganado conserva la instancia, estadísticas, perfil y splash de la oferta.
- Carreras y JSON: los rivales utilizan identidades del catálogo; las estadísticas se generan una vez. Equipo y preparación se incorporan una vez a la instantánea. Los guardados anteriores conservan su identidad y aspecto sin reasignación por coincidencia de nombre.

## Pruebas ejecutadas

| Prueba | Resultado |
| --- | --- |
| Catálogo completo en Play Mode batch | **1240 comprobaciones aprobadas**: conteos, IDs, precios, compra de los 80 productos, los 65 equipables, los 15 consumibles, niveles mínimos, identidad, guardado/carga, preparación en carrera y reversión ante fallo de escritura, incluso al cruzar un nivel |
| Interfaz en Play Mode batch | **1296 comprobaciones aprobadas**: 80 detalles de producto en cuatro resoluciones, referencias de iconos, cancelación/doble confirmación, consumo, límite diario, equipo y splash de establo/subasta |
| Subastas | **163 comprobaciones aprobadas** del sistema existente integrado con el catálogo; ciclo, reservas, IA, entrega, persistencia y carrera |
| Regresión de la beta | Escenarios **A–G aprobados**, con y sin apuesta, persistencia, cambio de carrera y liquidación única |
| Scripts de Player Windows | **27 ensamblados compilados**, incluido `Assembly-CSharp.dll`, para `StandaloneWindows64`; no se generó un ejecutable |
| Escenas, prefabs y perfiles | **12 comprobaciones aprobadas**: tres escenas sin scripts perdidos, referencias resueltas y 12 perfiles con 96 frames válidos |
| Auditoría gráfica | **180 PNG distintos**, RGBA, transparencia real, silueta dentro de márgenes; ningún archivo pendiente, vacío o duplicado por hash |
| Metadatos | Ningún `.meta` ausente o huérfano en los recursos nuevos; ningún GUID duplicado en `Assets` |
| Balance exploratorio | 2400 carreras sintéticas con el simulador real y muestra de 100 subastas; detalle en [balance.txt](balance.txt) |

La revisión de interfaz y los renders a archivo cubren **1920×1080, 1600×900, 1366×768 y 1280×720**. Se inspeccionaron capturas de tienda, establo y subasta. Los auxiliares finales y los listados de comprobaciones acompañan este informe, fuera de `Assets`, para que no se incluyan en el juego.

## Memoria e importación

Con las 180 texturas del catálogo cargadas, Unity reportó **53.245.440 bytes (50,8 MiB)** mediante `Profiler.GetRuntimeMemorySizeLong`. La cifra corresponde a esas texturas en el Editor, no a toda la RAM del juego ni a un perfil de rendimiento del ejecutable.

Los 174 PNG nuevos conservan sus archivos fuente de 1536×1024. Se importan a 384×256 para Windows, con BC7, sin mipmaps ni copia CPU legible. Los splashes usan Point y los iconos Bilinear. Los seis recursos anteriores mantienen su configuración. El ensayo inicial a 512×341 caía a RGBA32; se corrigió la importación a dimensiones compatibles con bloques, sin estirar ni editar los PNG.

## Incidencias y límites

1. Se corrigieron asociaciones de sal/galletas y dos pares de splashes tras revisar el contenido de las salidas gráficas. Cada ID final apunta a su icono o a un caballo del pelaje, crin y mantilla esperados.
2. Se detectaron fondos residuales en las orejeras de fieltro y Lince. Se retiraron con la herramienta de imágenes y se volvieron a revisar sobre gris. Los PNG del proyecto se copiaron directamente de los resultados, sin transformarlos con scripts.
3. Al iniciar las pruebas apareció un error **4551 al cargar una DLL de la caché Burst JIT** de la copia aislada. No fue un error C# del catálogo. Las pruebas funcionales también pasaron en la ejecución inicial, pero el log contenía esa incidencia. La ejecución final usa `--burst-disable-compilation` únicamente en el proceso de validación. No se modificaron ajustes de Burst, seguridad ni del proyecto principal. Queda pendiente comprobar Burst en la sesión normal y en el próximo Player.
4. Dos repeticiones del fixture fallaron al reemplazar el primer archivo de guardado temporal inmediatamente después del inicio de escena. No se determinó la causa del fallo de `File.Replace`. Se dejó concluir el inicio y el primer guardado durante dos segundos antes de comenzar las acciones del fixture; la validación completa posterior pasó. No se modificó `SaveSystem` ni se añadió un método de guardado alternativo. El error de acceso al archivo `.tmp` que se provoca deliberadamente más adelante forma parte de la prueba de reversión y fue comprobado.
5. El equipo nuevo más potente requiere nivel 2 o 3 para limitar ventajas iniciales. Se mantienen saldo, premios, apuestas y los seis productos originales. Relámpago ya parte con estadísticas superiores y mostró tasas de victoria muy altas en carreras cortas con equipo. La muestra no demuestra equilibrio de una campaña completa: queda pendiente una partida de balance con ofertas aleatorias reales.
6. **No se ejecutó un Player Windows ni se comprobó el empaquetado final de Resources en una build nueva**, porque no está autorizada una build de distribución en esta tarea. Compilar los scripts y revisar las referencias no sustituye esa prueba.

## Conservación del proyecto

Los hashes de `CameraController.cs` y `Assets/Scenes/hub.unity` coinciden con el respaldo tomado al comenzar esta tarea. Se conservaron los cambios locales anteriores. El ZIP de la beta anterior conserva su SHA-256 `E5D2CE8B9729EB9E83EEE9001A6E549451C509322C48CE76F2589F814E8A779F`.

Los trece scripts nuevos o modificados por esta ampliación están enumerados en [scripts-cambiados.txt](scripts-cambiados.txt). También se añadieron datos JSON, imágenes, sus `.meta` y documentación. No se rediseñaron menús, no se alteró el Hub ni la cámara, y no se introdujo otro inventario.
