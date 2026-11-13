# Subastas de caballos

La sección se abre desde el botón Subasta de la tienda. Sus cifras, caballo, datos y botones se construyen con Unity UI. El escenario decorativo está en `Assets/Resources/Subasta/Fondo.png`.

## Oferta y calendario

Cada oferta ocupa los días 1–4, 5–8, 9–12 y así sucesivamente. El cierre ocurre al salir del cuarto día mediante el avance normal del GameManager. El quinto día muestra la siguiente oferta. Los horarios de Windows y el tiempo transcurrido dentro de la pantalla carecen de efecto sobre la oferta.

Se generan un identificador para la subasta y otro para el caballo. La semilla combina la campaña, su primer caballo y el ciclo. Permite reproducir la misma oferta si se repite una transición después de un fallo de escritura. El JSON conserva la oferta completa y las decisiones ya tomadas.

El nombre procede del catálogo del GameManager. Se selecciona uno de los 12 perfiles válidos, excluyendo el perfil y nombre de la oferta inmediatamente anterior. Los caballos nuevos tienen nivel 1, edad entre 3 y 7 años y atributos dentro de los límites de Caballo. Las estadísticas iniciales aumentan moderadamente con la semana, con máximo 10 para velocidad y stamina y máximo 14 para potencial. Suerte e inteligencia utilizan los mismos rangos de progresión. Resistencia utiliza el atributo existente stamina.

La raza es un dato descriptivo nuevo. Las ofertas pueden ser Pura sangre, Criollo, Cuarto de milla o Árabe. La raza carece de bonificaciones de carrera. Los caballos anteriores conservan el valor No registrada cuando ese dato falta en su guardado. El modelo actual carece de sexo y genealogía.

El valor estimado combina las cuatro estadísticas, potencial y edad. La salida se sitúa aproximadamente en la mitad de ese valor, redondeada a múltiplos de 25. Los valores de la referencia artística se sustituyen por los cálculos del modelo.

## Identidad y presentación

El perfil visual se conserva en `visualProfileId`. El mismo ID acompaña al caballo en la oferta, entrega al establo, selección activa, JSON y parrilla de carrera.

HorseVisualProfile admite un splash dedicado opcional. Actualmente faltan esos 12 splashes y se muestra el sprite base exacto de cada perfil, con una advertencia por identidad. La ilustración del caballo marrón de la referencia sirve únicamente para indicar la zona de presentación.

`presentationBounds` guarda los límites de alfa del sprite base, calculados durante la importación de validación. HorseVisualCatalog crea una presentación recortada sobre la misma textura. Esta presentación elimina márgenes transparentes en las fichas. Las referencias base y los ocho frames de carrera mantienen su asociación original.

## Datos ocultos

Nombre, raza e imagen son visibles desde el inicio. Edad, nivel, potencial, velocidad, stamina, suerte e inteligencia permanecen ocultos inicialmente. Se eligen dos características distintas al crear la subasta. La primera aparece el día 2 y la segunda el día 4. El día 3 mantiene una sola característica revelada. La ficha básica también respeta estas reglas para edad y nivel.

Las características elegidas y los datos reales se guardan desde el inicio. Reabrir la sección y cargar el JSON conservan esa información. El panel Ver pedigrí explica la ausencia de datos genealógicos y evita mostrar estadísticas.

## Pujas, postores y reservas

Participan tres postores con presupuestos definidos al crear la oferta. Su valoración utiliza la calidad real y edad del caballo, con variación moderada. El presupuesto representa entre el 65% y el 125% del valor estimado. La probabilidad de ofertar considera el día, el precio relativo a su presupuesto y si se trata de una respuesta al jugador. Los postores se retiran cuando la mínima supera su presupuesto.

La máquina puede ofrecer una vez por día procesado y responder a cada nueva puja del jugador. La decisión depende de la semilla persistida y del historial de ofertas. Abrir la pantalla carece de efecto sobre esas decisiones.

La mínima inicial es el precio de salida. Cada oferta posterior exige un incremento cercano al 10%, redondeado a múltiplos de 25 y con mínimo 25. Los controles + y − ajustan el importe y el botón Pujar ejecuta la transacción.

El campo existente `GameManager.dinero` representa el saldo disponible. Al liderar una oferta se aparta el importe y se registra en `SubastaData.reserva`. Aumentar una puja propia aparta únicamente la diferencia. La tienda, reroll, equipamiento y apuestas continúan usando ese saldo disponible. Si la IA supera al jugador, se devuelve exactamente la reserva y se pone a cero. El HUD distingue saldo disponible y reserva.

Una puja fallida por escritura revierte saldo, historial, respuesta de IA y avisos. Las ofertas llevan identificadores y el resultado de la subasta protege su cierre de repeticiones. El saldo máximo de los ingresos contempla los fondos apartados para permitir su devolución sin desbordamiento.

## Cierre y avisos

Una victoria convierte la reserva en pago definitivo y añade una copia del caballo con el mismo identificador y datos. El caballo activo conserva su selección hasta que el jugador decida cambiarla. Una derrota devuelve cualquier reserva residual y conserva el resultado en el historial. Una oferta sin pujas termina sin transferencia.

Seguir activa avisos de puja superada y último día. Los resultados finales se notifican también. Los avisos aparecen de manera no modal en el Hub, con un máximo de diez segundos visibles por aviso. Su lectura se guarda para evitar repetirlos al cargar. Los avisos de seguimiento vencen al terminar su oferta. La pantalla de subasta muestra siempre un aviso discreto durante su cuarto día.

La campaña existente termina en el día 42. La oferta incompleta del ciclo 41–44 se cancela al terminar la campaña y libera su reserva una sola vez. Este cierre conserva la duración de la campaña y evita entregar un caballo antes de completar la oferta.

## Caballo activo

El establo conserva sus cuatro pestañas. El selector inferior permite recorrer los caballos y el botón Hacer activo guarda la elección para carreras. Los retratos, la ficha y el equipamiento se actualizan con el caballo consultado. Cada objeto conserva su `caballoEquipadoId` y su ranura individual.

La selección activa se bloquea desde que existe una parrilla preparada, incluidos los estados con apuesta y carrera iniciada. Para elegir otro caballo hay que hacerlo antes de seleccionar la carrera siguiente. Los participantes ya fijados conservan sus datos.

## Persistencia

GameData incorpora `subastas` y `caballoActivoId`. CaballoData incorpora `raza`. La versión JSON permanece en 1 para mantener la compatibilidad con los guardados existentes. La migración conserva caballos, inventario y asociaciones de equipo, elige el primer caballo como activo y crea una oferta para el ciclo del día guardado. Reconoce tanto campos ausentes como objetos vacíos reconstruidos por Unity.

Se guardan las ofertas, presupuestos, revelaciones, reservas, seguimiento, avisos, resultados y caballos adquiridos. El historial y el caballo entregado forman parte de la misma escritura de GameData que registra el cambio de día.

## Archivos principales

| Archivo | Función |
| --- | --- |
| `Assets/Scripts/SistemaSubastas.cs` | Datos, generación, revelaciones, incrementos y ofertas IA |
| `Assets/Scripts/GameManager.Subastas.cs` | Transacciones, reserva, seguimiento, cierre y caballo activo |
| `Assets/Scripts/SubastaView.cs` | Sección de subastas, controles y panel informativo |
| `Assets/Scripts/AvisosSubasta.cs` | Avisos no modales del Hub |
| `Assets/Scripts/GameManager.cs` | Persistencia y conexión con los días |
| `Assets/Scripts/GameManager.Carrera.cs` | Uso del caballo activo y límite monetario con reserva |
| `Assets/Scripts/GameData.cs`, `Caballo.cs` | Nuevos datos persistentes |
| `Assets/Scripts/TiendaView.cs`, `TiendaLayout.cs`, `UIManager.cs` | Navegación entre secciones y Escape del panel |
| `Assets/Scripts/EstabloView.cs`, `EstabloView.Interfaz.cs` | Selección activa y presentación individual |
| `Assets/Scripts/HorseVisualProfile.cs`, `HorseVisualCatalog.cs` | Splash opcional y presentación de la identidad |
| `Assets/Resources/HorseProfiles/horse_01.asset` a `horse_12.asset` | Límites de presentación de los 12 perfiles |
| `Assets/Resources/Subasta/Fondo.png` | Escenario decorativo |

## Recurso artístico

El escenario se preparó con la herramienta integrada imagegen a partir de la referencia aprobada. Se reutilizan los recursos de madera, marcos, pergamino e iconos existentes para la UI. El escenario contiene solamente decoración y tarima, dejando el caballo, los textos y los controles a Unity.

El prompt final utilizado fue el siguiente.

> Edit target Image 1. Create a production environment-only background for this Unity horse auction screen, landscape 16:9. Preserve the original warm brown aged timber, bronze edges, two lanterns, red horse banners, books and barrel, light and overall frontal composition exactly. Remove ALL text, numerals, icons, labels, buttons, UI frames and panels including the top sign and ALL three bottom panels and the left name card and day card. Remove the horse completely. Reconstruct plain horizontal wooden planks in the bottom half where panels were. The central empty low circular wood-and-bronze display plinth stays, its top at about 45% image height. Upper 14% is a simple plain horizontal wooden header beam available for separate dynamic Unity UI. Leave the middle of the stage empty for a dynamic horse sprite. No flowers, leaves, laurels or golden horseshoe adornments, no characters, no text, no UI buttons, no baked shadows of missing horse. Keep clear spaces for native UI to be layered later. This is only the decorative wooden environment, never a complete screenshot UI.
