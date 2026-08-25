# Labo1 AR - prueba en dispositivo físico

## Lo que ya está configurado

- Unity 6.5 con URP.
- AR Foundation y Google ARCore XR Plugin 6.6.1.
- ARCore habilitado para Android y marcado como obligatorio.
- Depth API marcada como opcional; el seguimiento de imágenes no la necesita.
- Android con IL2CPP, ARM64 y OpenGLES3 sin Vulkan.
- API mínima 26, requerida por ARCore 6.6.1 en Unity 6000.5.
- Escena de compilación: `Assets/Scenes/Labo1AR.unity`.
- Biblioteca: `Assets/Blancos/BibliotecaBlancos.asset`.
- Blancos compatibles: `ar1.jpg` y `ar2.jpg`.
- Prefab rastreado con el modelo de Sabrina Carpenter: `Assets/Prefabs/ContenidoBlanco.prefab`.
- APK: `Builds/Labo1-AR.apk`.

## Preparar el blanco

1. Imprime `Assets/Blancos/ar1.jpg` o `Assets/Blancos/ar2.jpg` en papel mate.
2. En el diálogo de impresión usa **Tamaño real** o **100 %**. No uses “Ajustar a página”.
3. Ajusta la imagen para que el cuadrado mida exactamente **18.0 cm por lado**.
4. Verifica con una regla antes de probar. La biblioteca AR está calibrada a **0.18 m**.

Si decides imprimir a otro tamaño, cambia `PrintedTargetWidthMeters` en
`Assets/Editor/Labo1ProjectSetup.cs`, ejecuta `Labo1 > Configurar proyecto AR` y vuelve a compilar.

## Instalar y probar

1. Usa un Android compatible con ARCore.
2. Activa Opciones de desarrollador y Depuración USB.
3. Conecta el teléfono con un cable de datos, desbloquéalo y acepta la huella RSA si aparece.
4. Comprueba la conexión con:

   ```bash
   /Applications/Unity/Hub/Editor/6000.5.7f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb devices -l
   ```

5. Instala o actualiza el APK con:

   ```bash
   /Applications/Unity/Hub/Editor/6000.5.7f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb install -r Builds/Labo1-AR.apk
   ```

   También puedes abrir Unity y usar `File > Build Profiles > Build And Run`.

6. Acepta el permiso de cámara cuando Android lo solicite.

## Video entregable

Graba estas cuatro pruebas, en este orden:

1. Arranque limpio: se ve la cámara y no aparece ningún modelo 3D.
2. Detección: al apuntar al blanco aparece el modelo de Sabrina Carpenter, con una altura aproximada de 14 cm.
3. Pérdida y recuperación: al sacar el blanco del cuadro el modelo desaparece; al regresar, reaparece.
4. Seguimiento: al mover lentamente la hoja, el modelo sigue el blanco.

No inicies el video con el modelo ya visible.
