# Carrera y progresión semanal — 13/11/2026

Se conserva la simulación, las tarifas, las bandas de rivales, el guion de bienvenida, la meta y los resultados completos. La cámara del Hub no se modificó.

## Representación de carrera

Los puntos procedían de los Image de `PuntoCompetitivo`, blancos para rivales y amarillo para el jugador. Se retiró su componente visible; se conserva el RectTransform de referencia. El actor pasó de 142×96 a 190×156 y su Image mantiene aspecto: el límite cuadrado de los frames de carrera aumenta de 96 a 156 unidades, aproximadamente 62,5%. El desplazamiento vertical de la imagen es constante (−18); no se modifica progreso, velocidad, meta ni poses de los archivos de animación. La flecha sigue como hija de la instancia propia, con anclaje proporcional a su altura y desplazamiento (49, -20) sobre la cabeza: la posición anterior quedaba oculta por el sprite del carril superior tras ampliar los caballos. Se conserva su aparición inicial de 2 segundos y el control lógico de A.

Se renderizó y revisó una carrera de seis caballos a 1600×900. La captura mencionada en el pedido no estaba adjunta en este mensaje: se usaron los renders previos como referencia. Los 100 perfiles y sus ocho fases se probaron automáticamente, sin cambiar sus imágenes. La valoración del apoyo de patas durante todo el loop y el uso físico de A quedan pendientes de revisión manual; no se afirma una inspección humana de todos los loops.

## Saldo, metas y derrota

Partidas nuevas: $2.500, día 1. Partidas existentes: saldo intacto. `Assets/Resources/MetasSemanales.json` centraliza las metas: 3000, 6000, 15000 y un incremento de 9000 desde la cuarta semana (24000, 33000, 42000 para las seis semanas existentes). `GameManager.ObtenerCuotaSemana` conserva su nombre por compatibilidad de código, pero devuelve una meta: ya no es un cobro.

El saldo total es disponible más reserva de subasta, sin valorar caballos ni objetos. La meta se evalúa al cerrar cada séptimo día, después de los premios/apuestas y del cierre de subasta correspondiente. No se descuenta ni reinicia dinero. No hay evaluación anticipada por estar debajo de la meta. En derrota no se avanza al día siguiente y no se inicia otra partida automáticamente.

La derrota por saldo negativo se determina al guardar una operación confirmada; los estados negativos heredados de partidas anteriores no se convierten en derrota al cargar ni por una mera migración. Un nuevo descenso confirmado o un cierre de día sí aplica las reglas nuevas. $0 no es derrota inmediata. Si un guardado transaccional falla, el nuevo estado de derrota de esa transacción se revierte junto con el flujo económico existente.

`GameData.derrota` guarda el motivo. La carga de un archivo derrotado vuelve a bloquear la campaña. El modal tiene dos botones, Nueva partida y Menú principal, reutiliza madera y pergamino existentes y tiene una capa de bloqueo, navegación explícita entre ambos botones y prioridad sobre Escape. Nueva partida usa el flujo existente; no elimina otros archivos de guardado. Menú principal guarda la derrota y carga el menú sin iniciar otra partida.

Si la derrota resulta del cierre de carrera, se conserva primero el podio. Su botón Volver entrega el control al Game Over. Si se carga una partida derrotada se muestra la derrota directamente, sin reactivar la competición.

El HUD compartido de Hub/carrera muestra semana, saldo total real y meta, con barra limitada a 0–100% pero sin truncar el monto. El indicador antiguo de dinero se oculta para no duplicarlo. El nombre y las llegadas de carrera se colocan debajo; el HUD se oculta sobre el podio y en los otros menús. La moneda visible se normalizó a formato es-AR, como $2.500; los cambios en establo/tienda son formato y acceso al pergamino, sin rediseño ni cambios de compra.

## Validación ejecutada

Unity 6000.3.13f1 batch, exclusivamente sobre C:\Caballos\Validation\MainMenu y guardados separados. Sin Computer Use, entradas al teclado físico, cierre de aplicaciones del usuario ni build de distribución.

- SemanaProbe: 78 comprobaciones aprobadas: umbrales 2999/3000/superior, días 7→8, 14→15 y 21→22, sin descuentos, saldo reservado, cero, negativo real, guardado/carga, migración sin derrota retroactiva, premio del día 7, liquidación única, espera del podio, bloqueo de Escape, ambos botones del modal, seis actores sin puntos, A lógica y carrera completa con escala nueva.
- AnimacionesCatalogoProbe: 2604 comprobaciones aprobadas, 100 perfiles, ocho fases por perfil, carrera real y restauración de identidad.
- Compilación de scripts Windows: 27 ensamblados, sin ejecutable.
- Renders: C:\Caballos\Validation\Semana\carrera.png, gameover.png y semanal.png. Se revisó composición y se ajustó el margen del texto del pergamino para apartarlo de sus adornos.
- git diff --check sin errores (avisos de normalización LF/CRLF).

Las pruebas propias y resultados están en Validacion/Semana, fuera de Assets. Logs aprobados: C:\Caballos\Validation\HubRefresh\Semana-EscalaFlecha.log, Semana-Animaciones.log y Semana-CompilacionVerificada.log. Los renders no sustituyen probar mouse/teclado físicos, DPI y legibilidad del galope. No se repitió toda la batería antigua, cuyas expectativas de saldo inicial/cuotas ya no describen estas reglas.

## Archivos modificados

Nuevos: Assets/Scripts/ProgresionSemanal.cs y Assets/Resources/MetasSemanales.json con sus .meta.

Integración: GameManager.cs, GameManager.Carrera.cs, GameData.cs, CarreraUI.cs, PistaLateralCarrera.cs, UIManager.cs y MenuPausa.cs. Formato de importes: CarreraProgramada.cs, EstabloView.cs, EstabloView.Interfaz.cs, TablonView.cs y TiendaView.cs; esta última expone también el pergamino ya utilizado. No se modificó CameraController.cs ni las escenas o assets de caballos.

Respaldos: C:\Caballos\Validation\Semana\Antes. Cambios anteriores conservados; sin staging, commit ni push.
