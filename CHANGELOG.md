# Registro de Cambios (Changelog) - Vamos Aprendiendo

Todos los cambios notables en este proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/) y este proyecto se adhiere a [Semantic Versioning](https://semver.org/lang/es/).

---

## [Unreleased] - Por Desarrollar

### Pendiente / Conocido
- **Texturas referenciadas que no vinieron en los paquetes** (no se pueden reparar desde el proyecto; hay que volver a descargar el paquete o elegir otra textura): `Idyllic Fantasy Nature` (rama `Branch` y agua `Lake`), `Polytope Studio` (`PT_Generic_Leaf_mat`, `PT_Rocks_mat`), `TriForge Assets` (tronco y pasto), `IL3DN` (rampas de `IL3DN_FX_Glow`). Prefabs con huecos: `IL3DN_GodRays` y `Polytope Studio/Fence`.
- **Scripts perdidos en `BaseScene`** (4): cámara principal y cámaras Cinemachine; el paquete Cinemachine no está instalado.
- **`SineVFX`**: sus 21 materiales usan shaders propios del pipeline antiguo y no se convirtieron.
- **`Stair_Step_4 (1)`** (en `plataforma_inicial`) no tiene material asignado.
- **Cuenta de pruebas `test` / `test1`** (`AuthService.cs`): entra sin backend; quitarla o protegerla con `#if UNITY_EDITOR || DEVELOPMENT_BUILD` antes de generar un build para terceros.
- **Rutas del Editor y del CLI de Unity:** los comandos del CLI cambiaron de nombre (`menu` → `editor invoke_menu`, `editor_play` → `editor.playmode start`, etc.).
- Sin probar en dispositivo real: controles táctiles, rendimiento del agua, fauna y portales en tablet.

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

## [0.7.0] - 2026-10-26

### Agregado
- **Ciclo de día y noche** (`SkyboxAnimator.cs`, `SkyboxSetupTool.cs`): 12 minutos de día (amanecer 45 s + día + atardecer 45 s) y 12 minutos de noche, con los cielos `FS003_Sunrise/Day/Sunset/Night` de Fantasy Skybox FREE. Cada momento cambia color y rotación de la luz direccional, luz ambiente y color de la bruma. Menú `Tools > Scenes > Setup Animated Skybox (BaseScene)`.
- **Personaje `Encapuchado`** reemplaza la cápsula del jugador (contenedor `Visual`, animaciones Idle/Caminar/Saltar/Festejar).
- **`SpiralLift`**: el jugador se centra en la plataforma al subir y se respeta la posición donde se colocó en la escena.

### Cambiado
- **UV de `Island_Grass_Rim`, `Grass_Cap_Mini_Island_*` y `Plataforma_uno`:** cada objeto tiene su propia malla con UV en metros reales (3 m por repetición); los bordes y tapas no traían UV y la textura se veía plana o estirada. Menú `Tools > Scenes > Fix Grass Rims, Caps and Platforms UV (3 m)`.
- **Materiales de bordes, tapas y plataformas:** todos los `Island_Grass_Rim` y `Grass_Cap_Mini_Island_*` usan `M_YFSM_Sand07` (con textura de color) y las `Plataforma_uno*` el nuevo `M_Plataforma_Arena` (base `T_YFSM_03_d`). `M_YFSM_Sand01/02/05` no traen textura de color en el paquete y se veían planos.
- **Mar de 5 km** (antes 650 m) y bruma con el color del horizonte de cada momento del día, para que no se vea la esquina del agua.
- **Bruma del mar:** niebla lineal 60–190 m (antes 90–260 m), totalmente opaca antes del borde del mar (≥ 225 m), con el color del horizonte de cada momento del día; ya no se ve el corte del agua.

### Agregado
- **Preparación para móvil y tablet:**
  - Plataforma activa **Android** (IL2CPP, ARM64), identificador `com.darkmeridional.vamosaprendiendo`, orientación **solo horizontal**, y las 7 escenas de retos en *Build Settings* (`MobileSetupTool.cs`, menú `Tools > Mobile`).
  - **Joystick flotante** (`UI/VirtualJoystick.cs`): aparece donde se apoya el pulgar, zona muerta del 20 %, adherencia a los ejes a ±14° y reescalado continuo del valor. `UI/MobileControlsUI.cs` lo conecta con `PlayerController` y bloquea la alineación automática de la cámara mientras se usa. `UI/TouchLookArea.cs` gira la cámara arrastrando en la mitad derecha. Botón **SALTAR** en pantalla.
  - **Botón de pausa / play** (`UI/PauseButtonUI.cs`, imágenes `btn_pause.png` y `btn_play.png` generadas con `PauseButtonGenerator.cs`): abre y cierra el menú de pausa existente.
- **Escenas 2D de trazado** (`Scripts/Challenges/Tracing/`, `Editor/TraceSceneBuilder.cs`):
  - `LetterTrace_2D` (**Vocales** y **Consonantes**, mayúscula y minúscula, incluida la Ñ) y `NumberTrace_2D` (**0 al 9**), 64 formas definidas en `GlyphLibrary.cs`.
  - **Desbloqueo por progreso** (`TraceCatalog.cs`, `TraceProgress.cs`): no se puede hacer la B sin la A, ni el 3 sin el 2, 1 y 0. Progreso guardado por usuario (`PlayerPrefs`).
  - **Validación estricta** (`TraceValidator.cs`): el trazo se descarta en cuanto se sale de la forma; cobertura ≥ 85 % y precisión ≥ 97 %.
  - Menús `Tools > Progress > Reset / Complete All Trace Items` para pruebas.
- **Portales a los retos** (`Gameplay/Environment/PortalTrigger.cs`, `Gameplay/Player/PlayerSpawnRestorer.cs`): portal **amarillo → letras**, portal **rojo → números**; al volver, el jugador aparece junto al portal.
- **Caída al vacío** (`Gameplay/Player/FallRespawn.cs`): la pantalla se oscurece durante 3 s con la cámara siguiendo al personaje y este regresa a una posición anterior a la caída.
- **Escaleras en espiral → una sola plataforma elevadora** (`Gameplay/Environment/SpiralLift.cs`, `Editor/SpiralLiftSetupTool.cs`): sube y baja al jugador por la curva de los escalones, une las plataformas de abajo y de arriba, y vuelve sola a la base. Los demás escalones se ocultan (`Hide Extra Steps` / `Show All Steps`).
- **Ambientación:**
  - **Cielo animado** (`SkyboxAnimator.cs`, Fantasy Skybox `FS003_Day`) con giro lento y ciclo de día opcional (apagado).
  - **Mar** (`Editor/WaterSetupTool.cs`): una sola malla de 650 m bajo las islas con Simple Water Shader (copia de doble cara en `_Project/Art/Shaders/Water`), bruma lineal 90–260 m y niebla bajo el agua (`SeaAtmosphere.cs`).
  - **Fauna** (`Gameplay/Fauna/`, `Editor/FaunaSetupTool.cs`): 11 grupos de aves en vuelo circular sobre las plataformas inferiores y 16 mariposas revoloteando; se ocultan a distancia (`ProximityActivator.cs`).
  - **Resplandor cálido en los portales** (`PortalGlow.cs`, `Editor/PortalGlowSetupTool.cs`): halo y luz puntual que pulsan, en la cara exterior de cada `Island_Portal_Core`.
- **Materiales de arena y texturas en islas** (`Editor/RockBaseMaterialTool.cs`, `PortalCoreMaterialTool.cs`, `TextureDensityTool.cs`, `FixUvStretchTool.cs`): materiales `M_YFSM_Sand*` en `Island_Rock_Base`, `Mini_Island_Right_Front/Back` y `Plataforma_uno (4)`; materiales aleatorios (A_piece_of_nature, Texture_Me, Blink) en los `Island_Portal_Core`. Las mallas sin UV se copian a `_Project/Art/Meshes/Islands/` con UV a 3 m por repetición.
- **Conexión de sesión de la app con el backend** (`Scripts/Networking/`): `ApiClient` con refresco automático de token y errores con `code`; `AuthService` con `RefreshTokens()` (un solo refresh a la vez) y `Logout()`; `PresenceService` con sesión + latidos cada 15 s (`/presence/sessions`), cierre por segundo plano/salida/cierre de sesión y reapertura con 409/404; `JsonBody.cs` (omite campos vacíos); `Bootstrapper.cs` crea los servicios si se juega desde otra escena y reabre la sesión con el token guardado.
- **Inicio de sesión de pruebas:** usuario `test` / contraseña `test1` entra sin validar contra el backend (`AuthService.cs`, **temporal**).
- **Convertidor de materiales de paquetes** (`Editor/PackageMaterialConverter.cs`, menú `Tools > Materials > Convert Package Materials To URP`): pasa a URP Lit/Unlit los materiales Standard, Legacy, Mobile/Unlit, HDRP Lit y shaders propios de Built-in.
- **Paquetes externos nuevos:** Fantasy Forest Environment Free Sample, Idyllic Fantasy Nature, IL3DN, Mushroom house, Polytope Studio, Selcuk Gerceker, TriForge Assets, Whaleride, YughuesFreeBarkMaterial, YughuesFreeSandMaterials, Zacxophone (aves), Butterfly (Animated), Fantasy Skybox FREE, Simple Water Shader (IgniteCoders), Wooden GUI, Texture_Me, Blink, A_piece_of_nature, Cartoon Animal Pack 01, Kevin Iglesias, SineVFX.

### Cambiado
- **Reorganización de paquetes externos en `Assets/_ThirdParty/`** (con `AssetDatabase.MoveAsset`, conserva GUID): `Vegetation`, `Buildings`, `Textures`, `Animals`, `Characters`, `Props`, `Environment`, `VFX`, `UI`. En la raíz de `Assets` quedan `_Project`, `Editor`, `Resources`, `Settings`, `TextMesh Pro` y `_Recovery`. Rutas actualizadas en las herramientas del Editor y en `PT_Welcome_Screen.cs` (Polytope) e `IL3DN_Wind_Editor.cs`.
- **Materiales de paquetes convertidos a URP** (78 + 51 materiales; los de `YughuesFreeBarkMaterial` y los shaders de `Polytope Studio` se sustituyeron con los de sus paquetes URP incluidos).
- **Pantalla de login:** eliminados los botones de Google y Apple y el separador "o continúa con"; textos más grandes (título 22→28, campos y etiquetas 13→17, botón principal 16→20); tarjeta reducida a 400 de alto.
- **Cámara** (`SmoothThirdPersonCamera.cs`): nuevas `SuppressAutoAlign` y `AddOrbitDelta()` para el control táctil.
- **`PlayerController.cs`:** nuevo `ResetVerticalVelocity()`.
- **Dashboard del tutor:** `PlayerSession` guarda `profileType`, nombre, hijo seleccionado y tokens; `LoginResponseDto` incorpora `profileType`, `profile` y `hasCompletedSetup`.

### Arreglado
- **Presencia en el dashboard del tutor** (`PresenceService.cs`, 2026-10-08): la app aparecía desconectada porque, al perder el foco del Editor, `OnApplicationPause(true)` cerraba la sesión. Ahora solo cierra en un móvil real (no en el Editor ni en el *Device Simulator*) y `Application.runInBackground = true` mantiene los latidos. Referencia: `vamos-aprendiendo-web/backend/docs/unity-app-sessions.md` (sección 4).
- **Joystick errático:** medía desde la esquina del círculo y no desde su centro, y la alineación automática de la cámara hacía girar al personaje. Corregido con joystick flotante, zona muerta, adherencia a ejes y cámara fija mientras se usa.
- **Botón "Salir" del menú de pausa:** las zonas táctiles no coincidían con los botones dibujados en `menu_pause.png` (1536×1024) y se solapaban; recalculadas en `PauseMenuUI.cs` y `PauseMenuSetupTool.cs`. Ahora "Salir" también cierra la sesión (`logout`) antes de volver al login.
- **Validación del trazo:** aceptaba líneas fuera de la forma; ahora se rechazan.
- **Agua:** líneas blancas y cuadrícula entre bloques → malla única; el mar no se veía desde abajo al caer → shader de doble cara.
- **Rayas y estiramiento de texturas** en `Stair_Step_*`, `Dais_Inner`, `Rock_Dais_1`, `Rock_Back_Standing`, `Landmark_Boulder_Stair`, islas, rocas y portales: UV rehechas por plano de cara (`FixUvStretchTool.cs`); estiramiento máximo de 1,3 a 1,0–1,08.
- **Modelos rosados:** materiales Standard/Legacy/HDRP de los paquetes convertidos a URP; shader `Vegetation` de Idyllic Fantasy Nature con palabras clave de sombras duplicadas.
- **Errores de consola:** `Readme.asset` de Simple Water Shader (comillas simples con apóstrofes en el YAML) y `AmbiguousMatchException` de `ReadmeEditor.cs` (se desactiva la carga automática de layout; el Readme sigue en el menú `Documentation`). El error `ScriptableSingleton ... DragAndDropCache` es de Unity AI y no del proyecto.
- **Materiales de `YughuesFreeSandMaterials`** (magenta): sustituidos por las versiones URP de su paquete.

---

## [0.5.0] - 2026-09-10

### Agregado
- **Paquetes de Efectos Visuales y Partículas (VFX):**
  - **Hovl Studio - Magic Effects Pack (`Assets/Hovl Studio/Magic effects pack/`):** Colección de efectos de partículas mágicas y visuales para ambientación y retroalimentación interactiva en el mundo.
  - **Reversed Interactive - The Portal Collection (`Assets/Reversed Interactive/The Portal Collection/`):** Modelos, prefabs y shaders de portales mágicos para zonas de transición y desafíos.
- **Librería de Audio y Efectos Sonoros (SFX):**
  - **25 RPG Game Tracks (`Assets/_Project/Audio/SFX/25 Rpg Game Tracks/`):** Repertorio de pistas musicales de temática RPG para ambientación y exploración.
  - **Casual Game Sounds U6 (`Assets/_Project/Audio/SFX/Casual Game Sounds U6/`):** Banco de efectos de audio casuales optimizados para Unity 6 (sonidos de interfaz, interacción, saltos y recompensas).
- **Colección de Modelos 3D Estilizados:**
  - **PixitGames - PixitUnderseaCollection (`Assets/_Project/PixitGames/Pixit3D/PixitUnderseaCollection/`):** Modelos 3D temáticos low-poly para expandir la escenografía y complementar la identidad visual del juego.

### Cambiado
- **Ajustes en Escena Principal ([BaseScene.unity](file:///E:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scenes/BaseScene.unity)):**
  - Actualización del montaje del entorno con los nuevos componentes y assets integrados.
- **Dependencias del Proyecto (`Packages/manifest.json`):**
  - Actualizado el paquete `com.unity.visualscripting` a la versión `1.9.12`.
  - Configuración y ajuste del asset de fuente fallback de TextMesh Pro (`LiberationSans SDF - Fallback.asset`).

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
