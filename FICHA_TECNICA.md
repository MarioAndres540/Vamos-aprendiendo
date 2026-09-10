# FICHA TÉCNICA DEL PROYECTO

---

## 1. INFORMACIÓN GENERAL DEL PROYECTO

| Campo | Especificación |
| :--- | :--- |
| **Nombre del Software** | **Vamos Aprendiendo** (`Vamos_aprendiendo`) |
| **Organización / Estudio** | **Darkmeridional studio games** |
| **Contexto Académico/Productivo** | Proyecto Productivo SENA (Servicio Nacional de Aprendizaje) |
| **Versión Actual** | `0.5.0` |
| **Motor de Videojuegos (Game Engine)** | **Unity 6** (Versión exacta: `6000.3.11f1`) |
| **Pipeline de Renderizado** | **Universal Render Pipeline (URP 17.3.0)** |
| **Lenguaje de Programación** | C# (.NET Standard 2.1 / C# 9.0+) |
| **Sistema de Entrada** | **Unity New Input System** (`com.unity.inputsystem 1.19.0`) |
| **Sistema de Texto / UI** | TextMesh Pro (`com.unity.ugui 2.0.0`) |
| **Plataforma Objetivo Principal** | PC Standalone Windows (`x86_64`) |
| **Plataformas Secundarias / Proyectadas** | WebGL / Android Tablet |
| **Control de Versiones** | Git + Plastic SCM (`.gitattributes`, `.gitignore` oficial Unity) |

---

## 2. DESCRIPCIÓN Y OBJETIVO PEDAGÓGICO

**Vamos Aprendiendo** es una aplicación interactiva lúdico-educativa multi-generacional en 3D/2.5D diseñada para fortalecer habilidades fundamentales en tres grupos etarios:
1. **Infantil:** Iniciación en lectoescritura, grafomotricidad, reconocimiento de trazos y conteo básico.
2. **Adulto:** Fortalecimiento de operaciones aritméticas, agilidad mental y comprensión léxica.
3. **Adulto Mayor:** Estimulación cognitiva, preservación de memoria espacial, resolución de laberintos y retención visual.

El entorno sitúa al usuario en un archipiélago de islas flotantes estilizadas en 3D donde la progresión espacial (desbloqueo de puentes y plataformas) está directamente vinculada al éxito en los desafíos educativos.

---

## 3. ARQUITECTURA DE SOFTWARE

El proyecto implementa una arquitectura modular desacoplada por capas con separación estricta de responsabilidades:

```
                                  +------------------------------------+
                                  |            BOOTSTRAPPER            |
                                  |     (Ciclo de vida y arranque)     |
                                  +-----------------+------------------+
                                                    |
         +--------------------+---------------------+---------------------+--------------------+
         |                    |                     |                     |                    |
         v                    v                     v                     v                    v
+-----------------+  +-----------------+  +------------------+  +------------------+  +-----------------+
|   CORE & FLOW   |  |     GAMEPLAY    |  |    CHALLENGES    |  |  UI / INTERFAZ   |  |   NETWORKING    |
| - GameFlowMgr   |  | - PlayerCtrl    |  | - ChallengeBase  |  | - LoginUI        |  | - ApiClient     |
| - GameState     |  | - CameraCtrl    |  | - Math / Tracing |  | - PauseMenuUI    |  | - AuthService   |
| - SceneLoader   |  | - LanternGlow   |  | - Maze / Memory  |  | - CharacterUI    |  | - Telemetry     |
+-----------------+  +-----------------+  +------------------+  +------------------+  +-----------------+
         |                    |                     |                     |                    |
         +--------------------+---------------------+---------------------+--------------------+
                                                    |
                                  +-----------------+------------------+
                                  |      PROFILES & DATA (SESSION)     |
                                  | - PlayerSession   - ProfileManager |
                                  +------------------------------------+
```

### Patrones de Diseño Implementados:
- **State Pattern / Finite State Machine:** Gestionado por `GameFlowManager` y `GameState` para transicionar entre estados (Autenticación, Exploración en Base, Desafío Activo, Pausa, Pantalla de Resultados).
- **Template Method & Strategy Pattern:** Estructurado en `ChallengeBase` para estandarizar el ciclo de vida de los minijuegos (inicio, evaluación de entrada, cálculo de puntajes y fin).
- **Data Transfer Objects (DTO):** Objetos de transferencia fuertemente tipados (`LoginResponseDto`, `PlayerProfileDto`, `ChallengeResultDto`, `ProgressDto`) para serialización JSON e intercambio con backend.
- **Service Layer Pattern:** Desacoplamiento de llamadas de red en servicios dedicados (`AuthService`, `LicenseService`, `TelemetryService`).
- **Observer / Event-Driven Input:** Desacoplamiento de periféricos usando las acciones del *New Input System* (`PlayerInput.cs`).

---

## 4. ESPECIFICACIÓN DE MÓDULOS Y SUBSISTEMAS

### 4.1. Módulo Core y Flujo de Juego (`Assets/_Project/Scripts/Core/`)
- [`Bootstrapper.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Core/Bootstrapper.cs): Punto de entrada y persistencia global (`DontDestroyOnLoad`). Inicializa dependencias críticas y valida la integridad de la sesión.
- [`GameFlowManager.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Core/GameFlowManager.cs): Coordinador central de estados de ejecución del juego.
- [`GameState.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Core/GameState.cs): Enumerador formal de estados del sistema (`Initializing`, `Login`, `ExploringHub`, `InChallenge`, `Paused`, `Completed`).
- [`SceneLoader.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Core/SceneLoader.cs): Encapsulador de carga asíncrona de escenas con control de progreso (`AsyncOperation`).

### 4.2. Módulo de Jugabilidad y Entorno (`Assets/_Project/Scripts/Gameplay/`)
- [`PlayerController.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Gameplay/Player/PlayerController.cs):
  - Basado en `CharacterController`.
  - **Movimiento Relativo a la Cámara (*Camera-Relative Movement*):** Proyecta los vectores de entrada sobre el plano horizontal de la cámara activa (`Camera.main`).
  - **Física Gravitacional y Salto:** Unificación de desplazamiento en una sola llamada por frame a `Move()`, salto vertical de `1.25m` y fuerza de adherencia al suelo (`verticalVelocity.y = -2f` cuando está en tierra).
- [`SmoothThirdPersonCamera.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Gameplay/Camera/SmoothThirdPersonCamera.cs):
  - Órbita completa 360° horizontal con clic secundario y pitch vertical restringido (`-12°` a `70°`).
  - Zoom dinámico con rueda del ratón (`2.5m` a `20.0m`).
  - Auto-alineación suave detrás de la espalda del avatar al desplazarse.
- [`LanternGlow.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Gameplay/Environment/LanternGlow.cs):
  - Control de emisión dinámica URP (`_EmissionColor`) y oscilación senoidal/ruido Perlin para iluminar faroles y lámparas flotantes.
- [`PlatformUnlockController.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Hud/PlatformUnlockController.cs):
  - Controla la visibilidad, activación de colisiones y animaciones de surgimiento de los puentes inter-islas al completar retos.

### 4.3. Módulo de Desafíos Educativos (`Assets/_Project/Scripts/Challenges/`)
- **Clase Base y Métricas:**
  - [`ChallengeBase.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Challenges/ChallengeBase.cs): Estructura base para todos los desafíos (temporizador, puntuación, intentos).
  - [`ChallengeManager.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Challenges/ChallengeManager.cs): Orquestador de inicio y cierre de retos.
  - [`ChallengeMetrics.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Challenges/ChallengeMetrics.cs): Registro analítico de tiempo de respuesta, precisión, errores e índice de retención.
- **Categorías de Minijuegos:**
  - **Trazado y Grafomotricidad (`Tracing/`):** [`LetterTraceChallenge.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Challenges/Tracing/LetterTraceChallenge.cs), [`NumberTraceChallenge.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Challenges/Tracing/NumberTraceChallenge.cs) y [`TraceValidator.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Challenges/Tracing/TraceValidator.cs) para cálculo de tolerancia angular y cercanía de trazos mediante splines/puntos interpolados.
  - **Lógica y Matemáticas (`Math/`):** [`MathOperationChallenge.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Challenges/Math/MathOperationChallenge.cs) (aritmética básica y combinada) y [`NumberSequenceChallenge.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Challenges/Math/NumberSequenceChallenge.cs) (patrones y series numéricas).
  - **Lectoescritura (`Literacy/`):** [`WordChallenge.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Challenges/Literacy/WordChallenge.cs) y [`PhraseChallenge.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Challenges/Literacy/PhraseChallenge.cs) (construcción de palabras y oraciones).
  - **Estimulación Cognitiva Adulto Mayor (`AdultGames/`):** [`MazeChallenge.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Challenges/AdultGames/MazeChallenge.cs) (orientación espacial en laberinto 3D) y [`MemoryChallenge.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Challenges/AdultGames/MemoryChallenge.cs) (asociación y retención visual de pares).

### 4.4. Módulo de Red, Telemetría y Sesiones (`Assets/_Project/Scripts/Networking/` y `Profiles/`)
- [`ApiClient.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Networking/ApiClient.cs): Cliente HTTP basado en `UnityWebRequest` con serialización JSON, encabezados de autorización Bearer y manejo de errores.
- [`AuthService.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Networking/AuthService.cs): Manejo de credenciales, autenticación y almacenamiento del token JWT.
- [`LicenseService.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Networking/LicenseService.cs): Verificación del estado de licencia de la institución / usuario.
- [`TelemetryService.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Networking/TelemetryService.cs): Envío asíncrono de eventos de interacción, tiempo de sesión y métricas de aprendizaje.
- [`ProfileManager.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Profiles/ProfileManager.cs) y [`PlayerSession.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/Profiles/PlayerSession.cs): Gestión de sesión local del estudiante y persistencia del perfil (`ProfileType`: Niño, Adulto, Adulto Mayor).

### 4.5. Módulo de Interfaz de Usuario y UX (`Assets/_Project/Scripts/UI/`)
- [`LoginUI.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/UI/LoginUI.cs): Tarjeta de acceso con estética *Glassmorphism*, barra de progreso animada, spinner de carga y textos contextuales.
- [`PauseMenuUI.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/UI/PauseMenuUI.cs): Menú de pausa modal con ilustración personalizada (`menu_pause.png`), oscurecimiento de fondo (*dimming overlay*), soporte para teclado (`Escape`) y Gamepad, operando con `Time.unscaledDeltaTime`.
- [`CharacterCarouselUI.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/UI/CharacterCarouselUI.cs): Carrusel adaptativo multi-generacional con micro-animación de respiración (*idle breathing*).
- [`ProgressSummaryUI.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/UI/ProgressSummaryUI.cs) y [`ChallengeResultPopupUI.cs`](file:///e:/SENA/Proyecto%20productivo/Vamos%20aprendiendo/Assets/_Project/Scripts/UI/ChallengeResultPopupUI.cs): Cuadros de diálogo para visualización de logros, estrellas y retroalimentación pedagógica.

---

## 5. INFRAESTRUCTURA DE ASSETS Y PIPELINE GRÁFICO

### 5.1. Pipeline de Renderizado y Shaders
- **Configuración URP:** Forward Renderer optimizado con iluminación en tiempo real y soporte para mapas de sombras suaves (*Soft Shadows*).
- **Estándar de Materiales PBR:** Shaders `Universal Render Pipeline/Lit` con textura combinada lineal (*Metallic en canal Rojo*, *Smoothness en canal Alfa*), mapa de normales en espacio tangente y máscaras de emisión HDR.

### 5.2. Paquetes y Recursos Audiovisuales Integrados
- **VFX y Partículas:**
  - `Hovl Studio - Magic effects pack`: Efectos mágicos estilizados y de retroalimentación.
  - `Reversed Interactive - The Portal Collection`: Shaders y prefabs de teletransporte para ingreso a minijuegos.
- **Audio y Efectos Sonoros (SFX):**
  - `25 RPG Game Tracks`: Ambientación musical instrumental orquestal/lúdica.
  - `Casual Game Sounds U6`: Banco de efectos UI, clics, aciertos, errores y recompensas optimizado para Unity 6.
- **Modelos 3D y Escenografía:**
  - `PixitUnderseaCollection` y modelos Low-Poly importados en formato `.fbx` y `.blend` (Plataformas flotantes, templos, faroles estilizados, portales rúnicos, árboles otoñales y golems protectores).

---

## 6. CONFIGURACIÓN DE ESCENAS Y COMPILACIÓN (BUILD)

### Escenas del Proyecto en `EditorBuildSettings`:
1. `Assets/_Project/Scenes/LoginScene.unity` (Índice `0` - Escena de arranque, verificación de perfil y carga).
2. `Assets/_Project/Scenes/BaseScene.unity` (Índice `1` - Escenario principal de exploración en tercera persona y selección de desafíos).
3. **Escenas de Desafíos (en `Assets/_Project/Scenes/Challenges/`):**
   - `MathOperation_2D.unity`
   - `NumberTrace_2D.unity`
   - `PlatformJump_25D.unity`
   - `Maze_3D.unity`
   - `Memory_3D.unity`

---

## 7. REQUERIMIENTOS DEL SISTEMA

### 7.1. Requerimientos para Desarrollo (Equipo Desarrollador / Editor)
- **Sistema Operativo:** Windows 10 / Windows 11 (64-bit).
- **Procesador:** Intel Core i5 de 8va generación / AMD Ryzen 5 o superior.
- **Memoria RAM:** Mínimo 8 GB (Recomendado 16 GB).
- **Tarjeta Gráfica:** NVIDIA GeForce GTX 1050 / AMD Radeon RX 560 o superior (compatible con DirectX 11/12).
- **Almacenamiento:** Mínimo 20 GB de espacio libre en disco SSD.
- **Software:** Unity Hub + Unity Editor 6000.3.11f1, Visual Studio Community 2022 o JetBrains Rider.

### 7.2. Requerimientos Mínimos para Usuario Final (Ejecutable `.exe`)
- **Sistema Operativo:** Windows 10 (64-bit).
- **Procesador:** Dual-Core x86_64 a 2.0 GHz o superior.
- **Memoria RAM:** 4 GB.
- **Gráficos:** Gráfica integrada Intel UHD Graphics 620 o equivalente con soporte DirectX 11.
- **Almacenamiento:** 1.5 GB de espacio disponible.
- **Periféricos:** Teclado y ratón (soporte opcional para Gamepad estándar XInput).

---

## 8. ESTRUCTURA DE DIRECTORIOS DEL REPOSITORIO

```
e:\SENA\Proyecto productivo\Vamos aprendiendo\
├── Assets\
│   ├── _Project\
│   │   ├── Art\                  # Modelos 3D (.fbx, .blend), Texturas PBR, Materiales URP e Ilustraciones
│   │   ├── Audio\                # Música ambiental (RPG Tracks) y efectos de sonido (SFX)
│   │   ├── PixitGames\           # Colecciones adicionales de arte 3D
│   │   ├── Prefabs\              # Prefabs de jugador, cámara, portales, faroles e interfaz
│   │   ├── Scenes\               # LoginScene, BaseScene y subcarpeta Challenges/
│   │   ├── ScriptableObjects\    # Contenedores de datos serializados y configuraciones
│   │   └── Scripts\              # Código C# modular (Core, Gameplay, Challenges, Networking, Profiles, UI)
│   ├── Editor\                   # Herramientas de automatización y puente UnityMcpBridge
│   ├── Hovl Studio\              # Assets del paquete de efectos de partículas
│   ├── Reversed Interactive\     # Assets del paquete de portales
│   └── TextMesh Pro\             # Configuración de tipografías SDF y fallbacks
├── Packages\                     # Manifiesto de paquetes de Unity (manifest.json)
├── ProjectSettings\              # Configuraciones del motor (Player, Input, Quality, Graphics, TagManager)
├── CHANGELOG.md                  # Historial cronológico de cambios y versiones (Keep a Changelog)
└── FICHA_TECNICA.md              # Documentación técnica formal del software (este documento)
```

---

## 9. CONVENCIONES DE CÓDIGO Y ESTÁNDARES TÉCNICOS

1. **Nomenclatura C#:**
   - Clases, Métodos y Propiedades: `PascalCase`.
   - Variables miembro privadas: `_camelCase` o `camelCase`.
   - Constantes: `UPPER_SNAKE_CASE` o `PascalCase`.
2. **Ciclo de Vida de Objetos:**
   - Separación estricta entre `Awake()` (inyección/resolución de referencias internas), `Start()` (conexión con sistemas externos) y `Update()`/`FixedUpdate()` (lógica continua).
3. **Manejo de Entrada:**
   - Prohibido el uso de la API obsoleta `UnityEngine.Input`. Todo evento debe ser despachado mediante el paquete `UnityEngine.InputSystem`.
4. **Gestión de Memoria y Garbage Collector:**
   - Reutilización de estructuras y listas en challenges para evitar asignaciones repetitivas en bucles de renderizado.
