# Beta jugable

Abrir el proyecto con Unity 6000.3.13f1. El repositorio usa Git LFS para
imágenes, modelos y otros recursos binarios. En una copia nueva ejecutar
`git lfs pull` antes de abrir Unity. La primera escena de la compilación es
`Assets/Scenes/MainMenu.unity`.

## Flujo de juego

- El menú principal ofrece Nueva partida, Cargar, Configuración y Salir.
- El tablón del Hub abre Elegir carrera cuando falta una selección. La oferta habitual contiene entre
  dos y cuatro carreras por día. La Copa Nocturna está configurada como cierre
  semanal. Se pueden editar estas opciones en
  `Assets/Resources/Carreras/Catalogo.asset`.
- Seleccionar prepara y guarda una parrilla según la distancia y dificultad
  elegidas y abre el tablón. Apostar es opcional. El televisor usa exclusivamente
  esa carrera y mantiene el botón para iniciar la simulación. Si falta una
  elección, muestra un aviso en el Hub.
- La selección se puede cambiar hasta que se confirme una apuesta o se inicie
  la carrera. La parrilla guarda estadísticas efectivas, identidades visuales,
  semillas y cuotas. Preparar el equipo antes de pulsar Seleccionar. El control
  Cambiar carrera del tablón permite volver al selector antes de apostar o iniciar.
- El tablón usa dinero ficticio. Solo confirmar descuenta el importe. El pago
  incluye la apuesta original y se redondea hacia abajo. El premio por ganar la
  carrera es independiente. El guardado JSON conserva la parrilla, la apuesta
  y el resultado. Una carrera interrumpida se reinicia con sus mismas semillas.

## Establo

Stats muestra los datos del caballo seleccionado. Up permite preparar mejoras
y confirmarlas juntas. Procrear lista los caballos propios. La compatibilidad
y la crianza siguen deshabilitadas. Items muestra el inventario real, cantidades
y descripciones del catálogo.

| Ítem | Ranuras compatibles | Bonificación |
|---|---|---|
| Zapatillas | Patas delanteras o traseras | +1 velocidad |
| Herradura de Plata | Patas delanteras o traseras | +1 resistencia |
| Herradura de Oro | Patas delanteras o traseras | +2 velocidad |
| Sombrero de Copa | Cabeza | +1 suerte |
| Sombrero Noble | Cabeza | +2 suerte |

Cada ranura admite una unidad. Equipar cuesta $50, desequipar $50 y reemplazar
$100. Una operación incompatible no cambia el saldo ni el equipo. Los guardados
anteriores conservan el equipo de Patas en la ranura delantera. Espalda está
disponible como ranura, aunque el catálogo actual carece de objetos compatibles.

## Validación

`BetaJugableValidation.Run` comprueba equipamiento, selección diaria, parrilla,
tablón, cuotas, guardado y una carrera completa. Ejecutarla con Unity en
`-batchmode -nographics` desde una copia aislada y con
`CABALLOS_VALIDATION_SAVE_PATH` en una carpeta nueva debajo de
`../ValidationSave`. Las capturas y guardados de las pruebas se mantienen
fuera del repositorio.
