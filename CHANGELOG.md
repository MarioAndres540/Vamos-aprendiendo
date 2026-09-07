# Registro de Cambios (Changelog) - Vamos Aprendiendo

Todos los cambios notables en este proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/) y este proyecto se adhiere a [Semantic Versioning](https://semver.org/lang/es/).

---

## [Unreleased] - Por Desarrollar

### En Progreso / Flujo de Texturizado de Modelos 3D (IA VARCO 3D)
- **Modelos de Entorno Pendientes de Texturizado:**
  - `Low Poly Floating Temple.fbx`
  - `Floating Autumn Tree.fbx`
  - `Fantasy Tree Diorama.fbx`
  - `Fantasy Rune Portal.fbx`
  - `Stone Eagle Statue.fbx`
  - `Glowing Japanese Lantern.fbx`
- **Flujo Establecido para Continuar:**
  1. **Descarga desde VARCO 3D:** Descargar **Base Color (`B`)** y **Normal Map (`N`)** de cada modelo.
  2. **Nombrado y Guardado en Texturas:** Guardar en `Assets/_Project/Art/Textures/` con nombres descriptivos (ej: `Temple_BaseColor.png`, `Temple_Normal.png`) para evitar colisiones con el nombre genérico `material_0`.
  3. **Configuración de Normal Map:** En Unity, seleccionar la textura normal y cambiar *Texture Type* a `Normal map` -> *Apply*.
  4. **Creación de Material URP:** Crear en `Assets/_Project/Art/Materials/` (ej: `M_Temple.mat`) con shader `Universal Render Pipeline/Lit` (o `Unlit`).
  5. **Asignación:** Asignar *Base Color* a `Base Map`, *Normal Map* a `Normal Map`, ajustar *Smoothness* a `0.0 - 0.1` y aplicar al modelo en escena.

### Pendiente General
- **Mecánicas de Juego:** Interacciones básicas, límites de la plataforma y animaciones de personaje.

---

## [0.2.0] - 2026-09-07

### Arreglado
- **Física y Movimiento del Jugador ([PlayerController.cs](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Gameplay/Player/PlayerController.cs)):**
  - Unificadas las llamadas múltiples a `controller.Move()` en una sola ejecución combinada por frame, evitando que `controller.isGrounded` falle y que la gravedad se acumule indefinidamente hacia abajo hasta bloquear al personaje contra el suelo.
  - Implementado movimiento relativo a la cámara (*Camera-Relative Movement*): al presionar **W/A/S/D**, el personaje se desplaza y rota en dirección hacia donde apunta la cámara activa (`Camera.main`), resolviendo el bloqueo frontal contra las rocas del escenario al usar coordenadas globales.
  - Añadida adherencia al suelo (`verticalVelocity.y = -2f` cuando está en tierra) para garantizar estabilidad y evitar desconexiones en rampas y desniveles.
- **Servidor del Editor ([UnityMcpBridge.cs](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/Editor/UnityMcpBridge.cs)):**
  - Añadido desenganche limpio del listener mediante `AssemblyReloadEvents.beforeAssemblyReload` al recargar scripts o entrar/salir del modo Play.
  - Capturada la excepción `HttpListenerException` en el puerto `8089`, eliminando de la consola el error repetitivo `Solo se permite un uso de cada dirección de socket`.
- **Materiales y Texturas de Arte:**
  - Corrección de material de farol: creado y vinculado [M_Lamppost.mat](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Art/Materials/M_Lamppost.mat) con su textura [Stylized_Wooden_Lamppost_baseColor.png](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Art/Textures/Stylized_Wooden_Lamppost_baseColor.png) en el modelo [Stylized Wooden Lamppost.fbx](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Art/Models/Environment/Stylized%20Wooden%20Lamppost.fbx).
  - Eliminación de `material_0.mat` y texturas genéricas (`diffuse.png`, `normal.png`) que sobreescribían de forma errónea los modelos en escena.

### Cambiado
- **Diagnóstico y Calibración de Cinemachine ([SampleScene.unity](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/Scenes/SampleScene.unity)):**
  - Identificada la causa de la orientación lateral de la cámara (`Rotation Control: None` a 80°); documentada la configuración de `Rotation Composer` con compensación vertical de objetivo (`Target Offset: Y = 1.0 - 1.2`) y modo de seguimiento `Simple Follow With World Up`.
  - Identificada y corregida la causa del hundimiento visual del personaje: ajuste del centro en `CharacterController` (`Center: Y = 0` para coincidir con el pivote central de la cápsula de Unity).


---

## [0.1.0] - 2026-09-05

### Agregado
- **Control de versiones:** Archivo [.gitignore](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/.gitignore) configurado con los estándares oficiales de GitHub para proyectos de Unity 6.
- **Estructura base del proyecto:** Carpetas organizadas en `Assets/_Project/` (`Art/`, `Audio/`, `Prefabs/`, `Scenes/`, `ScriptableObjects/`, `Scripts/`).
- **Integración MCP:** Puente de comunicación `UnityMcpBridge.cs` en `Assets/Editor/` para conexión y diagnóstico del proyecto.
- **Sistema de Entrada:** Configuración del nuevo Input System de Unity con el mapa de acciones en [InputSystem_Actions.inputactions](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/InputSystem_Actions.inputactions).
- **Importación de Arte y Modelos 3D:** Integración de modelos `.blend` en la escena (`plataforma_inicial`, `plataformas`, `puente_Dos`, `arbol_rosado`).
- **Controlador del Jugador:** Script [PlayerController.cs](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Gameplay/Player/PlayerController.cs) con lógica de movimiento, gravedad y recepción de eventos `OnMove`.
- **Cámara Dinámica:** Integración de `CinemachineCamera` configurada para seguir al `Player` (`TrackingTarget`).
- **Colisiones de Entorno:** Sustitución de `BoxCollider` por `MeshCollider` en los modelos 3D de plataformas para ajuste preciso de la geometría.
- **Escena Principal (`SampleScene.unity`):** Iluminación, *Global Volume* para post-procesamiento y ensamblado del entorno.
- **Documentación:** Archivo [CHANGELOG.md](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/CHANGELOG.md) para control de versiones y trazabilidad de cambios.

---

### Tipos de cambios:
- **`Agregado`** (*Added*): Para nuevas características o assets.
- **`Cambiado`** (*Changed*): Para modificaciones a funcionalidades existentes.
- **`Deprecado`** (*Deprecated*): Para características que pronto serán eliminadas.
- **`Eliminado`** (*Removed*): Para características o archivos eliminados.
- **`Arreglado`** (*Fixed*): Para corrección de errores (bugs).
- **`Seguridad`** (*Security*): En caso de vulnerabilidades o ajustes de seguridad.
