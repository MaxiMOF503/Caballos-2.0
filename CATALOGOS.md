# Catálogos de caballos e ítems

El catálogo contiene 80 definiciones de productos y 100 identidades de caballo. Los seis productos originales cuentan dentro del total y conservan sus IDs, precios e ilustraciones. Las identidades disponibles no se agregan automáticamente al establo.

Los catálogos completos para consultar están en [Items.csv](Catalogos/Items.csv) y [Caballos.csv](Catalogos/Caballos.csv). Los datos que usa el juego están en `Assets/Resources/Catalogos/Items.json` y `Caballos.json`. El estado de cada ilustración se registra individualmente en [ManifiestoVisual.json](Catalogos/ManifiestoVisual.json): `existente`, `revisado`, `generado_sin_revisar` o `pendiente`. El marcador de interrogación no cuenta como ilustración propia.

La entrega contiene 74 iconos nuevos, los seis iconos originales y 100 splashes nuevos, todos revisados y sin recursos pendientes. Los prompts y referencias están en [PromptsVisuales.json](Catalogos/PromptsVisuales.json). El [informe de validación](Catalogos/Validacion/Resultados.md) detalla los resultados ejecutados y las comprobaciones pendientes de Player Windows, Burst y balance de campaña.

## Ítems y funcionamiento

Hay 20 artículos de cabeza, 15 de espalda, 30 de patas y 15 consumibles. La categoría Patas utiliza las ranuras delanteras y traseras existentes; cada unidad solo ocupa una ranura. Los productos tienen ID estable, descripción, precio, nivel mínimo y efectos sobre las cuatro estadísticas que ya utiliza la simulación. Algunos combinan dos atributos y otros intercambian una ventaja por una penalización.

La tienda conserva seis espacios y su oferta normal de tres productos. Stock, compra, confirmación, inventario y renovación siguen usando los mismos sistemas. Los productos se eligen desde las 80 definiciones. La compra valida nuevamente el stock y el saldo al confirmar. El equipo nuevo potente requiere nivel 2 o 3; los seis productos anteriores mantienen sus condiciones.

Los consumibles se compran y seleccionan en el inventario del establo. El botón **Usar consumible** abre el modal existente. No se arrastran a las ranuras. Cada caballo admite una unidad por día, antes de seleccionar la carrera. La preparación temporal está limitada a +2 por atributo y vence al avanzar el día. La XP obtenida es permanente: 15 con Manzana Dorada o 35 con Manual de ejercicios. No se acumulan varios consumos en el mismo día ni se altera una parrilla ya preparada.

El uso guarda el inventario, la preparación y el límite diario juntos. Si falla la escritura, se restauran la unidad, la XP, la preparación y el estado aleatorio anterior. Los efectos del equipo y la preparación se incorporan una vez a la instantánea de la carrera, respetando el límite existente de 1–16.

## Identidades y compatibilidad

`catalogIdentityId` identifica una definición, mientras que `id` identifica la instancia persistente del caballo. La definición fija nombre, raza, `visualProfileId`, presentación y parámetros de generación. Las estadísticas se generan al crear la instancia y luego se conservan en JSON.

Se han producido y validado **100 animaciones propias (800 frames)**. La integración está activada mediante `HorseVisualCatalog.catalogAnimationsEnabled = true`. Los perfiles se cargan desde `Assets/Resources/HorseRaceProfiles/<catalogIdentityId>.asset`; el arreglo de doce perfiles anteriores se conserva para compatibilidad y migración. El estado individual, los prompts y la validación están en [Catalogos/Animaciones](Catalogos/Animaciones/Resultados.md).

La integración obtiene el perfil de carrera desde `catalogIdentityId`, con splash y ocho frames de esa misma identidad, y mantiene el perfil enlazado durante toda la carrera. Se validaron los cien perfiles, su persistencia, una carrera completa con una parrilla normal del catálogo y la restauración del splash propio al terminar. Las carreras del catálogo nuevo no utilizan los frames anteriores. Los recursos legacy permanecen disponibles para guardados sin correspondencia segura.

Las carreras generan rivales desde las identidades disponibles y conservan sus instantáneas deterministas. Las subastas eligen una identidad, generan una instancia y mantienen el ciclo de cuatro días, revelaciones, pujas, reservas y entrega originales. El caballo entregado conserva el mismo ID, perfil y estadísticas que tenía en la oferta.

Los guardados anteriores pueden omitir los campos nuevos. Con la integración nueva activada, una identidad ya registrada conserva su ID; un perfil antiguo reconocido puede migrarse por su familia de pelaje persistida, prefiriendo un nombre coincidente únicamente si también coincide esa familia. Se conservan el ID de instancia, nombre y estadísticas. Los perfiles `legacy:*` sin correspondencia segura no se adivinan por nombre. Los 28 sprites anteriores, las doce animaciones recuperadas, las unidades de inventario y sus IDs se conservan. El equipo se restaura por `idDefinicion`; el índice de sprite anterior sigue disponible como respaldo.

## Producción gráfica e importación

Las ilustraciones nuevas se producen con la herramienta integrada de generación de imágenes, una solicitud por recurso. Los iconos toman como referencia los originales de `Assets/Sprites/items`. Los splashes toman los colores del sprite base correspondiente y una referencia común de presentación pixel art. Cada lote se revisa antes de marcar sus recursos como definitivos. El manifiesto y los CSV indican el estado real de producción.

Los PNG de producción se conservan con transparencia y a resolución completa. Para Windows se importa una versión de hasta 384 píxeles, BC7, sin mipmaps ni lectura CPU. En imágenes de 1536×1024 esto produce 384×256, conserva la relación 3:2 y permite la compresión por bloques. A 512×341, Unity estaba usando RGBA32 y consumía más de siete veces la memoria. Los splashes usan filtro Point; los iconos, Bilinear. Los seis iconos anteriores y los frames de carrera conservan sus ajustes previos.

## Validación y alcance

Las pruebas se ejecutan en una copia aislada con Unity 6000.3.13f1 en modo batch. Cada ejecución usa un directorio de guardado y un prefijo de preferencias nuevos. Los renders se escriben a archivos; no se utiliza el escritorio.

Se comprueban conteos, IDs, categorías, precios, compras de los 80 artículos, equipamiento y bonificaciones de los 65 equipables, niveles mínimos, los 15 consumibles, límites diarios, reversión ante fallo de escritura, persistencia, identidades de las cien subastas y una carrera real con preparación. La prueba de interfaz recorre los 80 detalles de producto en 1920×1080, 1600×900, 1366×768 y 1280×720, además de confirmar compra, consumir y equipar desde los controles existentes.

La compilación de scripts para Windows se verifica con `PlayerBuildInterface.CompilePlayerScripts`. Esto no genera un ejecutable y no equivale a ejecutar el juego empaquetado. La comprobación de Player Windows queda pendiente hasta autorizar la próxima build. Los informes de resultados distinguen estas pruebas.

El balance exploratorio usa el simulador real con 2400 carreras sintéticas y 100 subastas, conservando el saldo inicial de $1000, los premios de $1000–5000, la penalización de $200 al perder y las cuotas existentes. Las ofertas reales de tienda son aleatorias; las combinaciones examinadas suponen acceso a todo el catálogo y constituyen un caso optimista. Relámpago ya parte con estadísticas superiores a los otros dos candidatos iniciales y obtuvo resultados muy altos en carreras cortas con equipo. Esta muestra no demuestra equilibrio de una campaña completa; se recomienda una partida de prueba antes de cerrar el balance académico.

La ampliación de catálogos no cambia la habitación ni la disposición de los menús. La corrección posterior del seguimiento de cámara y la revisión del saldo negativo se documentan por separado en [Animaciones/Resultados.md](Catalogos/Animaciones/Resultados.md). Los guardados personales no se modificaron. Las pruebas de legibilidad y consistencia artística no sustituyen la revisión final del jugador en su pantalla.
