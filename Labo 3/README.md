# Labo 3 · Banco Vision

Adaptación nativa para Apple Vision Pro de la práctica del PDF `lab01.es.pdf`. El proyecto no usa Android, APK, `adb`, controladores Oculus ni el XR Interaction Toolkit. Usa SwiftUI, RealityKit y el sistema de manipulación espacial de visionOS 26.

## Qué conserva de la práctica

- Banco inmersivo en escala métrica.
- Piso físico de 20 × 20 m.
- Cubierta de 1.60 × 0.80 m, a 0.90 m del piso virtual.
- Motor de 0.50 × 0.40 × 0.40 m.
- Torquímetro de 30 cm que se puede tomar, girar, transferir entre manos y soltar.
- Cuerpo dinámico, gravedad y colisiones reales: si se suelta fuera de la mesa, cae al piso.
- Registro en consola del sistema, GPU, simulador/dispositivo y mano que tomó el torquímetro.

## Requisitos

- Xcode 26.0 o posterior con el SDK de visionOS 26.
- Apple Vision Pro con visionOS 26 para la prueba física.
- Cuenta de Apple configurada en Xcode para instalar en el visor.

No se necesita Unity ni una licencia PolySpatial para esta versión.

### Por qué esta entrega es nativa

El PDF usa Unity, pero el cambio a Vision Pro ya no es solo sustituir la pestaña Android: una experiencia inmersiva de Unity necesita los paquetes visionOS/PolySpatial y una licencia Unity Pro, Enterprise o Industry. En esta Mac están instalados Unity 6 y el módulo visionOS, pero la licencia activa es Unity Personal. Por eso la adaptación se implementó con las APIs nativas de Apple, sin convertir el ejercicio en una ventana plana ni dejarlo bloqueado por licencia.

## Ejecutar

1. Abre `BancoVision.xcodeproj` en Xcode.
2. Selecciona el target `BancoVision` y entra a **Signing & Capabilities**.
3. Activa **Automatically manage signing**, elige tu Team y cambia el Bundle Identifier si Xcode indica que ya existe.
4. Empareja el Vision Pro desde **Window > Devices and Simulators** o **Xcode > Open Developer Tool > Device Hub**.
5. Elige el Vision Pro como destino y presiona **Run** (`⌘R`).
6. En la ventana inicial, pulsa **Abrir banco**.

Para una revisión rápida sin visor, selecciona **Apple Vision Pro Simulator**. El simulador prueba apertura, composición y navegación, pero no demuestra ergonomía, manos físicas ni percepción estereoscópica en hardware.

## Interacción

Mira el torquímetro y pellizca para tomarlo a distancia, o acércate y haz un pellizco directo. Muévelo y gíralo naturalmente. RealityKit identifica la lateralidad del dispositivo de entrada y la app imprime una de estas líneas:

```text
torquímetro en mano izquierda
torquímetro en mano derecha
torquímetro en ambas manos
```

Al soltar, la manipulación termina y el cuerpo vuelve a quedar bajo gravedad. El botón **Recolocar torquímetro** lo regresa a su posición inicial y cancela cualquier velocidad residual.

## Equivalencias Android → Vision Pro

| Guía original | Adaptación |
|---|---|
| Unity 6 + Universal 3D | App visionOS nativa |
| XR Interaction Toolkit | `ManipulationComponent` de RealityKit |
| OpenXR en Android | Runtime espacial de visionOS |
| Oculus Touch Controller Profile | Manos y pellizco naturales del sistema |
| Meta Quest Feature Group | Target `xros` / `xrsimulator` |
| Rigidbody + XR Grab Interactable | `PhysicsBodyComponent` + `ManipulationComponent` |
| `adb devices` / `adb logcat` | Device Hub + consola de Xcode |
| APK, Vulkan, ARM64 | App visionOS firmada, Metal, arm64 |

## Validación automatizada

Desde esta carpeta:

```bash
./Scripts/validate.sh
```

El script valida la estructura, compila la app y las pruebas de escala para Apple Vision Pro Simulator, y compila para un destino genérico de visionOS sin firma. En esta máquina, el intento de ejecutar XCTest quedó detenido antes del primer caso por el runner de CoreSimulator (`waiting for workers to materialize`), así que no se cuenta como una ejecución aprobada.

La compilación no reemplaza la prueba física solicitada por el laboratorio. Usa `DEVICE_TEST_CHECKLIST.md` con el visor puesto.
