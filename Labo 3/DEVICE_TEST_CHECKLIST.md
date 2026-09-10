# Checklist físico · Apple Vision Pro

Marca esta lista dentro del visor y con otra persona observando la consola de Xcode.

## Antes de ejecutar

- [ ] Vision Pro y Mac emparejados en Device Hub.
- [ ] Developer Mode activo en Vision Pro.
- [ ] `Automatically manage signing` activo y Team seleccionado.
- [ ] Vision Pro seleccionado como destino real, no el simulador.
- [ ] Área libre y otra persona cerca durante la prueba.

## Seis pruebas equivalentes al paso 30

- [ ] La app abre un espacio inmersivo mixto; el banco no aparece como contenido 2D dentro de la ventana.
- [ ] La consola imprime `os`, `gfx` y `sim`; `sim` tiene que ser `false` y `gfx` debe nombrar un dispositivo Metal.
- [ ] Al mover la cabeza, el banco permanece estable en el espacio y la vista responde sin arrastre perceptible.
- [ ] Se ven ambas manos reales y el sistema responde a pellizcos directos e indirectos.
- [ ] El torquímetro se toma con un pellizco, acompaña la mano y la consola identifica izquierda, derecha o ambas.
- [ ] Al soltar sobre la mesa se queda; al soltar fuera de la mesa cae y colisiona con el piso.

## Medición dentro del visor

- [ ] ¿La cubierta quedó a la altura esperada con las manos? Anota la respuesta literal.
- [ ] ¿El torquímetro se percibe de 30 cm? Anota la respuesta literal.

## Entregable sugerido

Graba un video corto de la vista del Vision Pro con las seis pruebas en ese orden. Agrega como texto las tres líneas `os`, `gfx`, `sim` y las dos respuestas de la medición. El último plano debe mostrar el torquímetro cayendo fuera de la mesa.

## Límite de la validación local

Un `BUILD SUCCEEDED` o pruebas unitarias exitosas confirman compilación y medidas declaradas; no confirman seguimiento real de manos, escala percibida, comodidad, estabilidad espacial ni física observada en el Vision Pro.
