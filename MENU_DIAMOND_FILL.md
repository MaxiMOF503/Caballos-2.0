# Rombos vacíos y rellenos

Esta revisión reemplaza el anillo luminoso de la etapa anterior por un relleno
dorado sólido, limitado al interior del rombo. No tiñe texto ni rectángulos.

La marca permanente de Nueva partida no provenía solo del estado del script:
el PNG original contiene una marca dorada interior en esa opción. Se conserva
el PNG sin cambios. Un Graphic nuevo, EmptyDiamondRepair, cubre exclusivamente
ese interior con colores interpolados del fondo adyacente (muestras a izquierda
y derecha por fila). Su geometría es un rombo de 26×26 píxeles fuente, sin fondo
rectangular y sin raycasts. Es una reconstrucción aproximada del fondo oculto,
no una recuperación exacta de los píxeles que estaban detrás de la marca.

Encima de esa reparación y de los otros rombos se colocan los cuatro DiamondFill,
de 23×23 píxeles fuente. Son opacos y tienen centro sólido: cinco vértices y cuatro
triángulos. Se mantienen los centros medidos y las anclas relativas a la imagen.
Los cuatro rellenos están desactivados en la escena guardada y al abrir el menú.
El nombre de clase MenuDiamondGlow se conserva para no romper referencias, pero
ya no dibuja un glow ni un anillo.

MenuDiamondSelection concentra el estado: hover y foco auténtico de teclado se
registran por separado. Hover tiene prioridad. Al salir, vuelve al foco de teclado
solo si hubo navegación y el EventSystem aún enfoca esa misma opción. Sin ese
foco, todos quedan vacíos. Un clic limpia el foco de teclado viejo. Los Select
programáticos iniciales no crean un relleno; los paneles limpian el estado visual.
Se apagan todos los rellenos antes de activar uno.

Se corrigieron MenuDiamondSelection, MenuDiamondOption y MenuDiamondGlow; se agregó
MenuDiamondEmptyMask. MainMenuHoverSetup configura la presentación y mantiene el
resto de bloques YAML intactos. MainMenuHoverValidation cubre la nueva lógica.
Los callbacks y scripts de Nueva/Cargar/Configuración/Salir no se modificaron.

## Pruebas

Unity 6000.3.13f1, batchmode/nographics, copia aislada. Compilación y ejecución
final con código 0. Resultados en FillValidation/results.txt.

- Estado inicial sin rellenos activos, incluida Nueva partida.
- Las cuatro opciones por separado y 400 transiciones rápidas, con salidas tardías.
- Hover reemplaza teclado; salir recupera solo el foco real de teclado vigente.
- Sin foco de teclado, salir apaga todos; el clic elimina selecciones viejas.
- Abrir/cerrar paneles no deja residuos; se respetan opciones deshabilitadas.
- Raycasts UGUI sobre botones transparentes y callbacks de clic de las cuatro opciones.
- Centro sólido opaco comprobado en la malla; reparación no dorada y no bloqueante.
- Centros y tamaño proporcional comprobados a cuatro resoluciones.
- Archivos protegidos y bloques ajenos de la escena conservados.

No se controló el escritorio. Queda pendiente revisar el resultado renderizado:
intensidad del relleno, coincidencia de bordes y mezcla de la reparación del fondo
en Nueva partida. Las pruebas geométricas no se presentan como validación visual.
El checkpoint que agrupa estas etapas se documenta en CheckpointValidation/README.md.
