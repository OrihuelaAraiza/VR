# Labo1 AR - instalación y prueba en iPad

## Requisitos

- Una Mac con Xcode instalado.
- Un iPad compatible con ARKit y con iPadOS 15 o posterior.
- Cable USB, o emparejamiento inalámbrico ya configurado en Xcode.
- Un Apple ID agregado en Xcode. Para una prueba local basta una cuenta personal gratuita.
- Uno de los blancos `ar1.jpg` o `ar2.jpg` impreso a **18.0 cm por lado**.

ARKit requiere la cámara y hardware real. El simulador de iPad no sirve para validar el seguimiento de imágenes de este laboratorio.

## Lo que ya está configurado

- AR Foundation y Apple ARKit XR Plugin 6.6.1.
- ARKit habilitado y marcado como requisito del dispositivo.
- Aplicación restringida a iPad.
- iPadOS mínimo: 15.0.
- IL2CPP, ARM64 y Metal.
- Identificador: `com.up.vr.labo1`.
- Permiso de cámara con descripción en español.
- Proyecto de Xcode: `Builds/iOS/Labo1AR-iPad/Unity-iPhone.xcodeproj`.

## Firmar e instalar desde Xcode

1. Abre `Builds/iOS/Labo1AR-iPad/Unity-iPhone.xcodeproj`.
2. Conecta y desbloquea el iPad. Pulsa **Confiar** si el iPad pregunta si confías en la Mac.
3. En Xcode selecciona el proyecto azul **Unity-iPhone** y después el target **Unity-iPhone**.
4. Abre **Signing & Capabilities**.
5. Activa **Automatically manage signing** y elige tu cuenta en **Team**.
6. Si Xcode indica que `com.up.vr.labo1` ya está ocupado, cambia **Bundle Identifier** por uno único, por ejemplo `com.tunombre.labo1ar`.
7. En la barra superior deja el scheme **Unity-iPhone** y elige el iPad conectado como destino.
8. Pulsa **Run** o `⌘R`.
9. Si iPadOS solicita **Modo de desarrollador**, actívalo en **Ajustes > Privacidad y seguridad > Modo de desarrollador**, reinicia el iPad y vuelve a ejecutar desde Xcode.
10. En el primer arranque acepta el permiso de cámara.

## Prueba del laboratorio

Haz la prueba con buena iluminación, evitando reflejos sobre la impresión:

1. Arranque limpio: se ve la cámara y no aparece ningún modelo 3D.
2. Detección: al apuntar a `ar1` o `ar2` aparece el modelo de Sabrina Carpenter, con una altura aproximada de 14 cm.
3. Pérdida: al sacar por completo el blanco del encuadre, el modelo desaparece.
4. Recuperación: al volver a mostrar el blanco, el modelo reaparece.
5. Seguimiento: al mover lentamente la hoja, el modelo mantiene su posición sobre ella.

## Volver a generar el proyecto

Después de cambiar la escena o los scripts, usa en Unity:

`Labo1 > Compilar proyecto Xcode para iPad`

Unity actualizará la misma carpeta de Xcode y conservará la configuración reproducible de iPad, ARKit y cámara. Si Xcode pregunta cómo tratar archivos existentes, conserva los cambios de firma y permite que Unity actualice los archivos generados.
