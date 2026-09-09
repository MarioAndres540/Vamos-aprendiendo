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
- **Flujo Establecido para Continuar:**
  1. **Descarga desde VARCO 3D:** Descargar **Base Color (`B`)** y **Normal Map (`N`)** de cada modelo.
  2. **Nombrado y Guardado en Texturas:** Guardar en `Assets/_Project/Art/Textures/` con nombres descriptivos (ej: `Temple_BaseColor.png`, `Temple_Normal.png`) para evitar colisiones con el nombre genérico `material_0`.
  3. **Configuración de Normal Map:** En Unity, seleccionar la textura normal y cambiar *Texture Type* a `Normal map` -> *Apply*.
  4. **Creación de Material URP:** Crear en `Assets/_Project/Art/Materials/` (ej: `M_Temple.mat`) con shader `Universal Render Pipeline/Lit` (o `Unlit`).
  5. **Asignación:** Asignar *Base Color* a `Base Map`, *Normal Map* a `Normal Map`, ajustar *Smoothness* a `0.0 - 0.1` y aplicar al modelo en escena.

---

## [0.4.0] - 2026-09-09

### Agregado
- **Sistema de Menú de Pausa Ilustrado con Tecla ESC ([PauseMenuUI.cs](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/UI/PauseMenuUI.cs)):**
  - **Uso Exclusivo de la Ilustración:** Ventana de pausa centrada en tamaño grande (750px de ancho) basada directamente en la ilustración de arte [menu_pause.png](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Art/images/menu_pause.png) (pulpito con birrete y botones integrados).
  - **Hitboxes Interactivas Calibradas:** Botones translúcidos con feedback visual sobre los botones internos **Reanudar (Verde)** y **Salir (Rojo)**.
  - **Oscurecimiento de Pantalla (*Dim Overlay*):** Panel frontal oscuro (`#050A14` al 70%) que atenúa el fondo del juego.
  - **Detección de Entrada (New Input System):** Pausa inmediata al presionar la tecla `Escape` o el botón *Start* del Gamepad.
  - **Congelamiento y Transición Fluida:** Control estricto de `Time.timeScale = 0`, animación suave de entrada/salida (*Unscaled Time*) y desbloqueo automático del cursor.
  - **Auto-Inicialización Global:** Carga automática en `BaseScene` y escenas de desafíos minijuegos sin necesidad de configuración manual.
- **Material y Texturas PBR para Golem Coronado ([M_Crowned_Stone.mat](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Art/Materials/M_Crowned_Stone.mat)):**
  - **Empaquetado PBR Metallic-Smoothness:** Generada la textura combinada [Crowned_Stone_Golem_metallicSmoothness.png](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Art/Textures/Crowned_Stone_Golem_metallicSmoothness.png) (R: Metálico, A: Suavidad = `255 - Roughness`) en espacio lineal.
  - **Configuración de Normal Map:** Importada [Crowned_Stone_Golem_normal.png](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Art/Textures/Crowned_Stone_Golem_normal.png) como `Normal map` (`sRGB: false`).
  - **Material URP Lit:** Asignadas las texturas `_BaseMap`, `_BumpMap` y `_MetallicGlossMap` con el keyword `_METALLICSPECGLOSSMAP` activo para reflejos metálicos dorados en la corona y relieve rocoso en el cuerpo.
  - **Mapeo Automático:** Vinculado `material_0` dentro de [Crowned Stone Golem.fbx.meta](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Art/Models/Environment/Crowned%20Stone%20Golem.fbx.meta).
- **Sistema de Iluminación y Emisión para Lámparas Japonesas ([LanternGlow.cs](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Gameplay/Environment/LanternGlow.cs)):**
  - **Mapa de Emisión de Alta Precisión:** Generado [Glowing_Japanese_Lantern_emission.png](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Art/Textures/Glowing_Japanese_Lantern_emission.png) para aislar la cámara interior de la vela y las runas de la base.
  - **Material URP con Emisión HDR:** Configurado [M_Glowing_japanis.mat](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Art/Materials/M_Glowing_japanis.mat) con `_EmissionColor` HDR Cyan (`#4DEEEA` a `2.5x`) y `Normal Map`.
  - **Luces Puntuales (Point Lights):** Fuentes de luz integradas (`Range: 5.5m`, `Intensity: 2.8`, `Color: Cyan Luminoso`, `Soft Shadows`) dentro de las lámparas en [BaseScene.unity](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scenes/BaseScene.unity).
  - **Animación Orgánica y Respiración Mágica:** Efecto de pulsación senoidal suave y micro-flicker Perlin.
  - **Herramienta de Automatización:** Script de editor [LanternGlowSetupTool.cs](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/Editor/LanternGlowSetupTool.cs) y endpoint `/lantern/setup` en [UnityMcpBridge.cs](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/Editor/UnityMcpBridge.cs).

### Arreglado
- **Limpieza de UI Obsoleta en Escena:** Eliminadas todas las cajas y botones residuales anteriores de la escena [BaseScene.unity](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scenes/BaseScene.unity) para dejar únicamente la ilustración limpia.

---

## [0.3.0] - 2026-09-08

### Agregado
- **Pantalla de Carga Interactiva y Asíncrona ([LoginUI.cs](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/UI/LoginUI.cs)):**
  - Sistema de transición con `SceneManager.LoadSceneAsync` hacia `BaseScene`.
  - Tarjeta central con estética *Glassmorphism*, fondo translúcido y bordes iluminados.
  - Spinner geométrico animado con rotación constante a 360°/s.
  - Barra de progreso interactiva con llenado horizontal suave simulado de 0% a 100%.
  - Contador numérico de porcentaje en tiempo real y textos informativos dinámicos de aprendizaje.
- **Salto para el Personaje ([PlayerController.cs](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Gameplay/Player/PlayerController.cs)):**
  - Mecánica de salto ágil (`jumpHeight = 1.25m`) mediante la Barra Espaciadora (`Space`), botón sur de Gamepad y eventos del nuevo Input System.
  - Detección estricta de suelo (`CharacterController.isGrounded`) y cálculo de velocidad vertical con aceleración por gravedad.
- **Sistema de Cámara en Tercera Persona Avanzado ([SmoothThirdPersonCamera.cs](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Gameplay/Camera/SmoothThirdPersonCamera.cs)):**
  - **Órbita con Clic Derecho:** Rotación libre de 360° en horizontal y vertical con límites de inclinación (*Pitch* entre `-12°` y `70°`).
  - **Zoom con Rueda del Ratón:** Ajuste de distancia suave entre `2.5m` (primer plano) y `20m` (vista panorámica).
  - **Auto-Alineación Detrás del Personaje:** Reubicación automática y suave de la cámara detrás de la espalda del jugador al moverse o girar en 180° (teclas **A, D, S, W**).
  - **Auto-Corrección de Perspectiva:** Detección y ajuste a modo perspectiva (`orthographic = false`, FOV `60°`) al iniciar la escena.
- **Carrusel de Personajes Multi-Generacional ([CharacterCarouselUI.cs](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/UI/CharacterCarouselUI.cs)):**
  - Transición suave cada 4 segundos entre pares de personajes adaptados (Niños: `boy`/`gril` ➔ Adultos: `men`/`woman` ➔ Adultos Mayores: `old-men`/`old-woman`).
  - Micro-animación de respiración / flotación (*idle breathing*) en reposo.
- **Escenas de Desafíos Educativos ([ChallengeScenesCreator.cs](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/Editor/ChallengeScenesCreator.cs)):**
  - Creador automatizado de las 5 escenas de minijuegos en `Assets/_Project/Scenes/Challenges/`:
    - `MathOperation_2D.unity`
    - `NumberTrace_2D.unity`
    - `PlatformJump_25D.unity`
    - `Maze_3D.unity`
    - `Memory_3D.unity`
- **Herramientas de Editor (Tools):**
  - `Tools > Scenes > Create or Rebuild Login Scene` ([LoginSceneGenerator.cs](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/Editor/LoginSceneGenerator.cs)).
  - `Tools > Environment > Scale Environment x1.5` ([SceneEnvironmentScaler.cs](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/Editor/SceneEnvironmentScaler.cs)) para escalar el entorno sin afectar al jugador.
  - `Tools > Camera > Auto-Fix and Bind Camera to Player` ([CameraSetupTool.cs](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/Editor/CameraSetupTool.cs)).

### Arreglado
- **Compatibilidad Exclusiva con New Input System:**
  - Eliminadas todas las llamadas residuales a `UnityEngine.Input` (Legacy) en scripts de gameplay, resolviendo el `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class, but you have switched active Input handling to Input System package`.
  - Migrada toda la lectura de ratón a `UnityEngine.InputSystem.Mouse.current`.
- **Cámara Fija / Bloqueada en BaseScene:**
  - Corregido el seguimiento de cámara en `BaseScene.unity` mediante vinculación automática del target `Player` en tiempo de ejecución.
- **Conflictos de Compilación C#:**
  - Corregida la ambigüedad `Object` vs `System.Object` utilizando `UnityEngine.Object.FindAnyObjectByType`.
  - Resueltos conflictos de variables de ámbito local en [LoginSceneGenerator.cs](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/Editor/LoginSceneGenerator.cs).

### Cambiado
- **Estructura y Nomenclatura de Escenas:**
  - Renombrada la escena central `SampleScene.unity` a [BaseScene.unity](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scenes/BaseScene.unity) y reubicada en `Assets/_Project/Scenes/`.
  - Actualizados los índices de compilación en `EditorBuildSettings` (Índice 0: `LoginScene`, Índice 1: `BaseScene`, Índices 2..6: Desafíos).

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
