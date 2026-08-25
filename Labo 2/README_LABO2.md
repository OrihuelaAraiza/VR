# Labo 2 AR - iPad

Proyecto Unity 6.5 preparado para la práctica **Dos imágenes, un cubo y una esfera**, adaptada exclusivamente a iPad/iOS.

## Comportamiento

- `ar1.jpg` está registrado en `BibliotecaBlancos` con el nombre exacto `cubo`.
- `ar2.jpg` está registrado con el nombre exacto `esfera`.
- `ContenidoCubo` crea un cubo rojo de 5 cm sobre `ar1`.
- `ContenidoEsfera` crea una esfera azul de 5 cm sobre `ar2`.
- Las dos imágenes pueden rastrearse al mismo tiempo.
- Cada figura solo permanece visible mientras ARKit reporta su imagen en estado `Tracking`.
- `Tracked Image Prefab` está vacío; `PrefabPerImage` decide qué contenido corresponde a cada imagen.

## Destino iOS

- iPad únicamente (`TARGETED_DEVICE_FAMILY = 2`).
- AR Foundation 6.6.1 y ARKit 6.6.1.
- iOS 15.0 o posterior.
- ARM64, IL2CPP y Metal.
- Bundle identifier: `com.up.vr.labo2`.
- Firma automática configurada para el Team `8MBP94XP38`.

## Archivos principales

- Escena: `Assets/Scenes/Labo2AR.unity`
- Script de reparto: `Assets/Scripts/PrefabPerImage.cs`
- Biblioteca: `Assets/Blancos/BibliotecaBlancos.asset`
- Prefabs: `Assets/Prefabs/ContenidoCubo.prefab` y `Assets/Prefabs/ContenidoEsfera.prefab`
- Exportación Xcode: `Builds/iOS/Labo2AR-iPad/Unity-iPhone.xcodeproj`

Para regenerar todo desde Unity usa **Labo 2 > Configurar proyecto AR para iPad**. Para volver a exportar Xcode usa **Labo 2 > Compilar proyecto Xcode para iPad**.

Consulta `PRUEBA_IPAD.md` para la prueba física y el orden del video entregable.
