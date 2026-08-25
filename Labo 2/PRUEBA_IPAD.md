# Prueba física del Labo 2 en iPad

El proyecto Unity, la exportación iOS y la compilación nativa sin firma ya fueron validados. La cámara ARKit y los blancos impresos solo pueden comprobarse en un iPad físico.

## Antes de ejecutar

1. Imprime `Assets/Blancos/ar1.jpg` y `Assets/Blancos/ar2.jpg` como dos hojas separadas y distintas.
2. La biblioteca está configurada para **18 cm de ancho físico en ambas imágenes**. Mide solo el área impresa de cada blanco. Si alguna mide distinto, actualiza su ancho en `BibliotecaBlancos` antes de volver a exportar.
3. Conecta y desbloquea `iPad de Jp`, acepta **Confiar** si iPadOS lo solicita y deja Developer Mode activo.

## Instalar desde Xcode

1. Abre `Builds/iOS/Labo2AR-iPad/Unity-iPhone.xcodeproj`.
2. Selecciona el proyecto `Unity-iPhone`, target `Unity-iPhone`, pestaña **Signing & Capabilities**.
3. Conserva **Automatically manage signing** activo y el Team `8MBP94XP38`.
4. Selecciona `iPad de Jp` como destino y presiona **Run**.
5. En el primer inicio acepta el permiso de cámara.

Si Xcode no puede crear el perfil para `com.up.vr.labo2`, confirma que la cuenta correcta aparezca en **Xcode > Settings > Accounts** y vuelve a seleccionar el Team. No cambies el proyecto a iPhone ni a Android.

## Cinco pruebas para el video

Graba la pantalla del iPad y muestra, en este orden:

1. La app abre mostrando la cámara, sin figura alguna.
2. Al apuntar a `ar1` aparece únicamente el cubo rojo sobre su hoja.
3. Al apuntar a `ar2` aparece únicamente la esfera azul sobre su hoja.
4. Al encuadrar las dos hojas aparecen simultáneamente cubo y esfera, cada uno sobre la suya.
5. Al retirar una hoja del encuadre desaparece solo su figura; la otra permanece visible.

La cuarta toma es la evidencia principal de que el reparto por nombre funciona.
