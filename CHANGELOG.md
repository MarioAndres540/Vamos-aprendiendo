# Registro de Cambios (Changelog) - Vamos Aprendiendo

Todos los cambios notables en este proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/) y este proyecto se adhiere a [Semantic Versioning](https://semver.org/lang/es/).

---

## [Unreleased] - Por Desarrollar

### Agregado — Preparación de la entrega del PMV (2026-11-03)
- **Tiempo en el HUD:** `PlayerHudUI.timeLabel` muestra el tiempo de juego de la sesión bajo la barra de experiencia (el reloj sobrevive a los cambios de escena y se reinicia al entrar otra cuenta o hijo). El panel del HUD pasa a 196 px de alto.
- **Texturas a 1024:** `Tools/Optimization/Textures - Cap to 1024` (`TextureBudgetTool`) bajó el tope de importación de 280 texturas 3D/entorno a 1024; se exceptúan sprites de interfaz, cielos e iconos. Es reversible con `Textures - Restore Texture Limits (2048)` y los archivos originales no cambian.
- **Compilación:** las escenas vacías (`MathOperation_2D`, `Maze_3D`, `Memory_3D`, `PlatformJump_25D`) salieron de la compilación; quedan Login, Menú principal, Mundo, Tu ruta y los dos retos de trazo.
- **Backend público en las compilaciones:** `ApiClient` usa la dirección de `Resources/api_base_url.txt` si existe. `Tools/Build/Set Backend URL from Clipboard`, `Build Windows` y `Build Android APK` (`BuildTool`). En Android usar una dirección https.
- **GDD final:** `Docs/GDD_Vamos_Aprendiendo.html` y `.pdf` (con anexos de requisitos, manual de usuario, uso de IA y lista de chequeo). Los campos marcados en amarillo quedan por completar.

### Cambiado — "Regístrate" abre la página de registro (2026-11-03)
- El botón "Regístrate" del login abre en el navegador del dispositivo la página web donde se hace el registro (el tutor llena sus datos y los de su hijo; después inicia sesión en la app). La dirección está en `LoginUI.registerUrl` (escena `LoginScene`); mientras esté vacía, el botón avisa que el registro estará disponible pronto en vez de fallar.
- Para la grabación del video: este flujo (registro en la web → inicio de sesión en la app) cuenta como parte de lo que se muestra.

### Agregado — Menú principal con instructivo y créditos (2026-11-03)
- Nueva escena `MainMenuScene` (`Tools/Scenes/Build Main Menu Scene`, `MainMenuUI`), entre el login y el mundo: **Jugar** (carga el mundo con la pantalla de carga), **Instructivo** (objetivo y controles; "Ver el tutorial" lo vuelve a lanzar), **Créditos** (estudio, autoría y recursos de terceros) y **Cerrar sesión**. Saluda con el nombre de quien juega.
- `LoginUI.targetSceneName` ahora es `MainMenuScene`; la escena quedó en la compilación justo después de `LoginScene`.
- Revisar el nombre de los autores en los créditos (`MainMenuSceneBuilder.CreditsText`) antes de entregar.

### Cambiado — Animaciones de Mixamo en los dos golems (2026-11-03)
- Se aplican las animaciones de Mixamo de `Art/Animations` (`Mutant Idle`, `Orc Idle`, `Clapping`, `Thoughtful Head Shake`) con `Tools/Animation/Apply Mixamo Animations to Golems` (`MixamoGolemRetarget`). Reemplaza las animaciones por curvas de la versión anterior (`GolemAnimationBuilder` y los clips `Giant01_*` se eliminaron).
- **Giant01 - Golem** (humanoide): los FBX se importan como Humanoid y sus clips se usan directo. Idle = Mutant Idle (en bucle), `Festejar` = Clapping, `Pensar` = Thoughtful Head Shake.
- **Crowned Stone Golem** (esqueleto propio de 14 huesos, no puede ser humanoide): los clips se hornean a 30 fps copiando el giro de cada hueso de Mixamo sobre el equivalente del golem (`CrownedGolem_Idle` desde Orc Idle, `_Festejar`, `_Pensar`). El torso y las piernas copian solo el 25-30 % del giro (los idles de Mixamo van muy encorvados y el golem es corto y redondo); brazos y cabeza, casi todo.
- Controladores `Giant01` y `CrownedGolem`: Idle por defecto; los triggers `Festejar` y `Pensar` los disparan y vuelven solos al Idle. Ningún script los dispara todavía.

### Cambiado — El servidor también cobra la experiencia en el cofre (2026-11-03)
- Al abrir el cofre la app llama a `POST /telemetry/xp/claim` (`XpService.Claim`) con los ejercicios cobrados; el HUD toma `totalXp` del servidor (solo lo cobrado) y el panel del cofre muestra el XP de `pending`. `XpService.FetchSynced` declara al servidor lo que el jugador ya había cobrado antes de este cambio o sin conexión.
- Compatible con un backend anterior: si la respuesta no trae `pending`, se calcula con lo cobrado guardado en el dispositivo.
- Requiere aplicar la migración `20261103_xp_claims.sql` y desplegar el backend.

### Cambiado — La experiencia se entrega solo al tocar el cofre (2026-11-03)
- **El "?" del cofre aparece apenas hay ejercicios completados sin cobrar**, de cualquier ruta (antes solo con la ruta al 100 %: con los números 4 y 5 hechos no salía). Un toque cobra todos los ejercicios pendientes juntos; el panel muestra su XP y, si son de varias rutas, "Tus rutas".
- **El HUD solo cuenta lo cobrado:** el número de XP y el llenado de la barra salen de los ejercicios cobrados en el cofre, no de todo lo aprobado. Al completar una actividad el HUD ya no sube solo; sube al abrir el cofre.
- Lo cobrado se guarda por cuenta e hijo (`mission.claimedIds.<cuenta>.<hijo>`); lo cobrado con versiones anteriores se respeta (claves `mission.claimed.*`).
- `MissionRewards` pasa de "ruta cobrada sí/no" a la lista de ejercicios cobrados (`AllUnclaimed`, `MarkClaimed(ids)`, `ClaimedGlyphIds`).
- Nota: el backend sigue registrando el XP al aprobar cada ejercicio, así que el dashboard web lo muestra desde ese momento; el "cobro" en el cofre es del juego.

### Agregado — Tutorial de bienvenida y controles (2026-11-03)
- **`TutorialManager`** (`Tools/UI/Add Tutorial to BaseScene`, ya agregado a `BaseScene`): tarjeta de bienvenida con "Aceptar" y, después, una viñeta por control: **joystick, cámara, salto, sonido y pausa**. Cada paso ilumina el control, atenúa el resto, deja el juego detenido (`timeScale = 0`) y **no avanza hasta que el jugador usa ese control**; los demás botones no reciben toques. Cierra con una tarjeta "¡Listo!".
- Pausa: al abrir el menú el tutorial se esconde y continúa al reanudar. Sonido: si el jugador lo silencia en el paso, se vuelve a activar solo. "Saltar tutorial" (arriba al centro) lo cierra.
- Se muestra una sola vez por cuenta e hijo (`tutorial.done.<cuenta>.<hijo>` en PlayerPrefs). Para volver a verlo: `Tools/UI/Reset Tutorial (this device, current account)` o `TutorialManager.Begin()`.
- **Botón de ayuda "?"** (`HelpButtonUI`, `Tools/UI/Add Help Button to BaseScene`): arriba a la derecha, debajo del de pausa, vuelve a lanzar el tutorial. No hace nada si el tutorial ya está en marcha o el menú de pausa está abierto.
- `TouchLookArea` expone el evento estático `Dragged` (lo usa el paso de la cámara).

### Agregado — Animaciones de los golems (2026-11-03)
- **Diagnóstico:** ni `Giant01 - Golem` (Kevin Iglesias, rig humanoide de 53 huesos, el pack no trae clips) ni `Crowned Stone Golem` (rig genérico de 14 huesos, en pose de T) traían animaciones ni controlador.
- **Creadas** con `Tools/Animation/Build Golem Animations` (`GolemAnimationBuilder`) en `Art/Animations/Golems`: `Idle` en bucle (respiración y balanceo lento) y `Festejar` (brazos arriba y salto) para cada uno, más un controlador por golem (`Idle` por defecto; el trigger `Festejar` lo dispara y vuelve solo al Idle). Giant01 usa músculos humanoides; el golem coronado, curvas sobre sus huesos (sus brazos pasan de la T a caídos).
- Asignadas a sus instancias de `BaseScene`. Quedan en `CullCompletely`: solo se animan cuando se ven.

### Cambiado — Pantalla de trazo con el diseño nuevo (2026-11-03)
- **Letras y números** (`LetterTrace_2D`, `NumberTrace_2D`; se regenera con `Tools/UI/Redesign Trace Screens (letters + numbers)`): barra superior con volver, título y los pasos **MIRA > TRAZA > PRACTICA**, panel de **Instrucciones** (un paso por trazo, con el color de su número), zona de trazo con **números de orden y flechas de dirección**, **Comenzar a trazar** / **Repetir modelo**, "Paso x de n", mascota y panel **Sonido de apoyo**.
- **Flujo:** al abrir una forma, una bolita recorre los trazos en orden (modelo); el dedo no dibuja hasta pulsar *Comenzar a trazar*. *Repetir modelo* lo vuelve a mostrar. La lógica de validación (`TraceValidator`) y el envío de intentos no cambiaron.
- **Imágenes del diseño** (`Art/images`): `btn_atras`, `info` (MIRA > TRAZA > PRACTICA, con un resplandor sobre el paso actual), `btn_inicio_actividad`, `btn_repet`, `btn_silencio`, `low_music`, `voz_guia`, `footer_info` y `purple_icon_sonido`. Si falta alguna, el constructor usa el dibujo propio de ese elemento.
- **Sonido de apoyo** (`TraceSoundPanel`): Silencio / Música suave (`Ambient 1`, en bucle) / Voz guía, con volumen (el mismo de la pausa). El modo se recuerda. *Escuchar* y la voz guía leen `Resources/Voice/<id>` (ej. `lo_a`, `up_B`, `d_7`); mientras no existan avisa "llegará pronto".

### Agregado — HUD del jugador, controles reubicados y ruta Consonantes (2026-11-03)
- **HUD** arriba a la izquierda (`PlayerHudUI`, `Tools/UI/Add Player HUD to BaseScene`): nombre de quien juega (el hijo si la cuenta es de tutor) y barra de experiencia con los XP dentro.
- **Controles:** el joystick queda a la altura del botón de salto (`VirtualJoystick.restMarginBottom`); la pausa pasa a la derecha, bajo el audio.
- **Consonantes:** la tarjeta de "Tu ruta" ya no es "Próximamente": 44 ejercicios (22 letras x mayúscula/minúscula), abre el reto en la pestaña de consonantes y se sincroniza con el backend (requiere la migración `20261103_consonants_route.sql`).
- **Cofre/"?" de consonantes:** se puede cobrar con lo hecho hasta el momento (sin esperar al 100 %); cobra solo las consonantes nuevas desde el último cofre.
### Agregado — Signo "!" de misión nueva y reglas de aparición de los dos signos (2026-11-02)
- **"?" (fin de misión):** visible solo mientras haya una ruta al 100 % sin cobrar; **al cobrar todas se oculta** (sin dibujo ni collider).
- **"!" (misión nueva, `send mission`, `NewMissionSign.cs`):** aparece **al empezar** (ninguna ruta hecha) y **al terminar la ruta en la que se estaba**, cuando queda otra sin empezar; se oculta mientras hay una ruta empezada sin terminar y cuando ya están todas completas. Regla en `MissionRewards.HasNewMissionAvailable()` (solo cuentan Números, Letras minúsculas y Letras mayúsculas). Comprobada con 8 casos, incluido el progreso real del jugador (números 20 %, vocales 100 % → oculta).
- **Animación y efectos de la "!"**: rebota y se balancea de lado a lado (no gira, para verse siempre de frente), con pulso, un destello dorado (`Holy hit`) al aparecer y un aura amarilla (`Buff`, reducida con `auraScale` porque su humo de 2 m se veía como una cúpula de cerca). La "?" conserva su giro y su aura morada de estrellas, así se distinguen.
- **Al tocar la "!"** abre la pantalla de rutas, igual que el pulpo (`TapToLoadScene.Open()`).
- **Reorganización:** `MissionMarker.cs` es la base común (mostrar/ocultar, aparición con rebote, animación, aura y destello, toque); `MissionSign` y `NewMissionSign` solo definen cuándo se ven y qué hacen al tocarlos. Los ajustes ya guardados de la "?" se conservaron. `MissionSignSetupTool` ahora configura también la "!" (collider, pulpo, efectos, animación).
- Pruebas en Play con el progreso real y escenarios temporales; el progreso de la cuenta se guardó y se restauró idéntico. Ojo: durante los primeros segundos de Play el Editor aún no ha iniciado sesión y las pruebas escriben en el perfil `guest`; se limpió lo agregado. **No probado:** el toque real sobre la "!" (se verificó el enlace con el pulpo) ni con pantalla táctil.

### Agregado — Signo de fin de misión "?" y cofre con recompensa de XP (2026-11-02)
- **`MissionSign.cs`** (en `finish_mision`, sobre el cofre): está **oculto** hasta que una ruta llega al 100 % (letras minúsculas, mayúsculas o números, en ese orden) y aún no se cobró. Al cumplirse **aparece** con un rebote y un destello, **flota, gira y pulsa**, con un aura de estrellas. Al tocarlo **vuela al cofre**, el cofre se **abre** con un estallido de estrellas, y sale el panel de recompensa.
- **Panel de recompensa** (`MissionRewardPanel.cs`, prefab `Prefabs/UI/MissionRewardPanel`): "¡Cofre del tesoro!", la ruta, **"+N XP"** (cuenta hacia arriba) con la experiencia ganada en esa ruta, y la **experiencia total**. Las cifras salen del backend (`XpService.cs`, `GET /telemetry/xp`): suma el XP de los ejercicios de esa ruta. **Sin conexión no inventa un número**: felicita ("¡Lo lograste!"). Al pulsar "¡Genial!" el cofre se cierra y la ruta queda cobrada.
- **El XP ya se entrega al aprobar cada ejercicio** (backend); el cofre lo muestra y marca la ruta como cobrada, no lo retiene. Si se quiere que el XP quede pendiente hasta abrir el cofre, hay que cambiarlo en el backend.
- **"Cobrada" se guarda en el dispositivo** por cuenta e hijo (`MissionRewards.cs`, PlayerPrefs `mission.claimed.*`); no viaja al servidor. Si hay otra ruta completa sin cobrar, el signo vuelve a aparecer.
- Efectos de **Magic Effects (Hovl Studio)**: aura `Star aura`, destello `Holy hit` y estallido `Star hit` (se ven bien en URP). Asignados en el componente.
- **Wooden GUI no se pudo usar:** el paquete `Wooden GUI and UIAnimation` del proyecto solo trae datos de animación y prefabs vacíos, **sin imágenes** (los prefabs anidados y los sprites faltan). El panel usa arte de madera generado por código en `Art/images/ui_reward/` (panel, cinta, botón y estrella); si se vuelve a importar el paquete completo se pueden cambiar esos sprites en el prefab.
- Herramienta `MissionSignSetupTool.cs` (*Tools > Scenes > Setup Mission Sign (BaseScene)*): genera el arte y el prefab, agrega el collider y el componente al signo, lo enlaza con el cofre y los efectos, y guarda la escena. Se puede repetir.
- **Cambio en `BaseScene`:** el toque directo sobre el cofre (`TapToOpen`) quedó **desactivado**: el cofre ahora se abre desde el signo para entregar su recompensa. Para volver a abrirlo con un toque, activar ese componente.
- Verificado en Play (simulando el toque sobre el signo): aparece solo con la ruta completa, vuela, abre el cofre, muestra "+50 XP" y 140 XP en total con datos reales, cierra y marca la ruta. **No probado:** con un toque real en pantalla táctil, ni sin conexión.
- Pendiente: la "?" tiene ~300 000 triángulos (ver el apartado de materiales); con el signo siempre animado conviene reducirla antes de un build para tableta.

### Agregado — Materiales de los signos de misión "!" y "?" (2026-11-02)
- Los modelos `send mission.fbx` (la **"!"**, 4.370 triángulos) y `finish_mision.fbx` (la **"?"**, 298.706 triángulos), en `Prefabs/UI`, se importaron con un solo material gris sin textura. Los colores originales (amarillo de bloque con piezas rojas) venían **dentro de los propios modelos**:
  - La "!" no trae texturas en el `.fbx`, pero su `.glb` (misma malla) sí: color, normales y rugosidad. Se extrajeron a `Art/Textures/MisionSigns/Exclamacion_*`.
  - La "?" trae color y normales embebidos en el `.fbx`: se extrajeron a `Art/Textures/MisionSigns/Interrogacion_*`.
- Materiales URP Lit `M_Signo_Exclamacion` y `M_Signo_Interrogacion` (`Art/Materials/MisionSigns`): plástico brillante (sin metal, brillo 0,72, mapa de normales al 80 %). Se asignan **al modelo** (remapeo de material del importador), así que las instancias de `BaseScene` y las futuras los usan sin editar ninguna escena.
- Herramienta `MissionSignsMaterialTool.cs` (menú *Tools > Art > Create Mission Signs Materials (! and ?)*): configura las texturas (sRGB, normales, máximo 2048 px, compresión), crea o actualiza los materiales y los asigna. Se puede repetir sin duplicar.
- **Pendiente (rendimiento):** la "?" pesa 26 MB y tiene ~300 000 triángulos para un icono; en tableta conviene reducirla a unos pocos miles (el sentido de la "!" es 4.370). Su textura original mide 2560 px y se importa a 2048.

### Cambiado — Los ejercicios ahora se envían al backend (2026-10-31)
- **`ProgressSyncService.cs`** dejó de usar `/progress/mine` y `/progress/sync` (no existían: el progreso nunca salía del dispositivo, por eso el dashboard web no mostraba nada). Ahora:
  - `ReportAttempt(glyphId, coverage, accuracy)` encola cada intento (aprobado o no) y lo envía a `POST /telemetry/exercises` con un `clientAttemptId` propio, el `studentId` del hijo que juega y la cobertura/exactitud de `TraceValidator`. **El backend decide si aprobó y entrega 10 XP una sola vez por ejercicio**; Unity ya no sube "el conjunto de completados".
  - `Fetch()` baja `GET /telemetry/route-progress` (con `studentId` si hay hijo) y suma los ejercicios aprobados al progreso local (`escritura.lo_a` → `lo_a`).
  - `Push()` envía la cola pendiente (sin red, por ejemplo). La cola se guarda por usuario en `PlayerPrefs` (`attempts.<usuario>.queue`, máx. 200), y un reintento **reutiliza el mismo `clientAttemptId`**, así que no duplica intentos ni XP. Los rechazos 400/403/404 se descartan (reintentar no cambia nada); los errores de red, 401 y 5xx se reintentan después.
  - Evento `ProgressSyncService.XpAwarded(puntos, total)` cuando el backend entrega XP (todavía no se muestra en pantalla).
- **`TraceChallenge.CheckTrace`** llama a `ReportAttempt` en cada evaluación; ya no usa `PushInBackground`.
- **Progreso anterior importado (2026-11-01):** lo que el dispositivo ya tenía completado no tiene cobertura ni exactitud, así que no pasa por `POST /telemetry/exercises`. Tras `Fetch` y `Push`, `ProgressSyncService` lo declara una vez a `POST /telemetry/exercises/import` (en lotes de 100, sin las consonantes y solo si no quedan intentos por enviar). El backend lo registra como "importado" (sin cobertura ni exactitud inventadas), lo omite si ya estaba aprobado y da 10 XP (regla `exercise_imported`, configurable). Así lo que se ve en "Tu ruta" aparece también en el dashboard web. Requiere la migración `20261101_import_completed_exercises.sql` del backend; sin ella la llamada falla con un aviso y se reintenta en el siguiente `Fetch`. Reemplaza al punto anterior de "para que cuente hay que repetir el ejercicio". Compila; **no se probó contra el backend real**.
- Las consonantes se rechazan con 404 (están inactivas en el catálogo del backend) y se descartan.
- Verificado: compila; el cuerpo JSON sale con punto decimal aunque el dispositivo esté en es-CO; la respuesta de `route-progress` se interpreta bien; la cola guarda intentos aprobados y no aprobados. **No se probó contra el backend real con una cuenta** (hay que probarlo con la cuenta de prueba, repitiendo un ejercicio).

### Agregado — Contenido de niños o de adultos y selección del hijo (2026-10-29)
- **`PlayerSession.Audience`**: el backend ahora envía `audience` en el login (`ninos` para el tutor, `adultos` para el adulto, vacío para el tester). Atajos: `IsKidsAudience`, `IsAdultsAudience`, `IsTutor`, y `MustChooseAudience` + `ChooseAudience(...)` para que el tester (o la cuenta `test` sin backend) elija la versión.
- **Al reabrir la app con el token guardado** el perfil ya no se pierde: `AuthService.RefreshTokens` toma `profileType`, `audience` y `user` de la respuesta de `/auth/refresh` (`PlayerSession.UpdateProfile`). Antes `ProfileType` quedaba vacío desde el segundo uso.
- **`StudentsService`** (nuevo, `Scripts/Networking/StudentsService.cs`): consulta `GET /students/mine` y elige el hijo que juega antes de abrir la sesión de app (`PrepareCurrentStudent`):
  - Un hijo: se elige solo.
  - Varios: se usa el último elegido en el dispositivo; si no hay, se dispara el evento `StudentSelectionRequired` para que una pantalla de selección llame a `PlayerSession.SelectStudent(id)`.
  - Sin red: se usa el último elegido, sin borrarlo.
  - Lo llaman `LoginUI` (después del login) y `PresenceService` (al reabrir con el token guardado). Así `studentId` llega en `POST /presence/sessions` y la sesión queda a nombre del niño.
- **`PlayerSession.SelectStudent` / `LoadSavedStudentId`**: el hijo elegido se recuerda por cuenta en `PlayerPrefs`. `CurrentStudentId` ahora solo se cambia con `SelectStudent`.
- **`LoginUI`**: campos opcionales `kidsSceneName` y `adultsSceneName`. Si se configuran, después del login se carga la escena según `audience`; vacíos (por defecto) se mantiene `targetSceneName` (`BaseScene`).
- DTOs: `audience` en `LoginResponseDto` y `UserDataDto`; `TokenResponseDto` con `profileType`, `audience` y `user`; nuevo `StudentDto.cs` (`StudentSummaryDto`, `MyStudentsResponseDto`).

### Corregido
- **`ApiClient` enviaba cuerpo en las peticiones GET**: en Android e iOS una GET con cuerpo puede fallar. Ahora solo se adjunta cuerpo (y `Content-Type`) en POST/PUT/PATCH/DELETE.
- **Cuenta de pruebas `test` / `test1`**: tenía `role = "student"`, un rol que no existe en el backend. Ahora es `test` con perfil `tester`.

### Pendiente
- **Pantalla para elegir hijo** cuando el tutor tiene varios: escuchar `StudentsService.StudentSelectionRequired` y llamar a `PlayerSession.SelectStudent(id)`.
- **Usar `Audience` en el juego**: hoy existe una sola escena general (`BaseScene`); definir qué retos o escenas son para niños y cuáles para adultos, o configurar `kidsSceneName` / `adultsSceneName`.
- **Pantalla para que el tester elija versión** (`MustChooseAudience` → `ChooseAudience`).
- Verificado compilando los 130 scripts del juego con el compilador de Unity (sin errores); falta probarlo en el Editor (la ventana de Unity no se había enfocado, así que aún no recompiló).

### Pendiente — Instrumentación de Unity para el dashboard web (acordado el 2026-10-31; no se modificó código de Unity)
El backend ya recibe ejercicios, entrega XP y calcula el progreso por ruta (`POST /telemetry/exercises`, `GET /telemetry/route-progress`); contrato completo en `vamos-aprendiendo-web/backend/docs/unity-exercises.md`. Hasta que Unity migre, el dashboard muestra "Aún no recibimos ejercicios" y "Precisión: No disponible".
- [x] (2026-10-31) **Migrar `ProgressSyncService.cs`** (hecho: ver el apartado de abajo): `GET /progress/mine` y `POST /progress/sync` **no existen** en el backend (las rutas eran supuestas; hoy responden 404 y el progreso queda solo en `PlayerPrefs`). Reemplazar por `GET /telemetry/route-progress` (ids `escritura.lo_a`, `matematicas.d_7`; quitar el prefijo de ruta) y por un `POST /telemetry/exercises` por intento, con un `clientAttemptId` nuevo por intento y reutilizado al reintentar el mismo envío.
- [x] (2026-10-31) Enviar **todos los intentos** (también los no aprobados) con cobertura y exactitud (`TraceValidator.Evaluate`); el backend decide si aprobó (≥ 0,85 y ≥ 0,97) y entrega 10 XP una sola vez por ejercicio. Mostrar `totalXp` y `xpAwarded` de la respuesta.
- [x] (2026-10-31) Enviar `studentId` del hijo que juega (obligatorio en cuentas de tutor con hijos).
- [ ] **Métricas geométricas** para la precisión de letras y números: trayectoria esperada, puntos recorridos (máx. 12 trazos y 400 puntos por trazo), desviaciones, cumplimiento del punto de inicio (círculo 1), orden de los trazos, dirección y giro.
- [ ] **Cambiar la medición de las salidas de la línea**: hoy `TraceChallenge.RejectStroke` descarta el trazo y lo repite sin registrarlo; hay que contar y medir cada salida (`offPathCount`) antes de descartar para tener estadística real.
- [ ] Definir la fórmula de `precisionPct` (forma + orden + dirección).
- [ ] Decidir qué pasa con el progreso ya guardado solo en el dispositivo (sin cobertura ni exactitud): descartarlo o importarlo con una regla explícita.
- [ ] Implementar la captura y envío de **lectura**; definir la estructura de **lógica**; definir cómo medir **atención**, **memoria** y **creatividad** (hoy solo existen Matemáticas y Escritura).
- [ ] Validar el flujo completo de recompensas XP desde Unity hasta el dashboard; diseñar después el sistema de niveles.

### Pendiente / Conocido
- **Diálogos con personajes** (diseño propuesto, sin implementar): cuadro con retrato, nombre, texto por partes con efecto de escritura, puntos de progreso y botón Siguiente; el primero al tocar a `Octopus_Rigged_Unity`. Falta definir las frases y en qué otras partes se usará.
- **Texturas referenciadas que no vinieron en los paquetes** (no se pueden reparar desde el proyecto; hay que volver a descargar el paquete o elegir otra textura): `Idyllic Fantasy Nature` (rama `Branch` y agua `Lake`), `Polytope Studio` (`PT_Generic_Leaf_mat`, `PT_Rocks_mat`), `TriForge Assets` (tronco y pasto), `IL3DN` (rampas de `IL3DN_FX_Glow`). Prefabs con huecos: `IL3DN_GodRays` y `Polytope Studio/Fence`.
- **Scripts perdidos en `BaseScene`** (4): cámara principal y cámaras Cinemachine; el paquete Cinemachine no está instalado.
- **`SineVFX`**: sus 21 materiales usan shaders propios del pipeline antiguo y no se convirtieron.
- **`Stair_Step_4 (1)`** (en `plataforma_inicial`) no tiene material asignado.
- **Cuenta de pruebas `test` / `test1`** (`AuthService.cs`): entra sin backend; quitarla o protegerla con `#if UNITY_EDITOR || DEVELOPMENT_BUILD` antes de generar un build para terceros.
- **Rutas del Editor y del CLI de Unity:** los comandos del CLI cambiaron de nombre (`menu` → `editor invoke_menu`, `editor_play` → `editor.playmode start`, etc.).
- Sin probar en dispositivo real: controles táctiles, rendimiento del agua, fauna y portales en tablet.

### Sugerencias de mejora / Rendimiento (análisis de Occlusion Culling, 2026-10-31, sin cambios en el proyecto)
- **Situación medida en `BaseScene`:** 2.552 renderers activos (2.499 mallas, 33 con esqueleto, 20 de partículas), 506 `LODGroup`, 2.158 con sombra (~1.176 de pasto y ~254 de arbustos y flores). Solo 79 objetos tienen marcas de oclusión estática, no hay datos horneados, áreas ni portales; la cámara ya tiene Occlusion Culling activado. Android usa Vulkan con OpenGL ES 3 de respaldo.
- **Occlusion culling horneado (Umbra): viable, pero beneficio esperado bajo.** Habría que marcar como estáticas islas, plataformas y templo y hornear. El mundo es abierto (islas flotantes con aire y mar entre ellas), así que casi nada queda tapado desde la cámara. Mirando hacia las islas entran ~2.250 renderers en el frustum, mirando al mar ~20; no se pudo estimar cuántos quedarían ocultos. Dejarlo para el final o descartarlo, salvo que se agreguen zonas cerradas (cuevas, interiores).
- **Occlusion culling por GPU (Unity 6): probar en una rama.** Requiere Render Graph (activo), *Forward+* en el renderer, *GPU Resident Drawer* en *Instanced Drawing* y *GPU Occlusion* en el Universal Renderer. Hoy `Mobile_RPAsset` usa Forward y el Resident Drawer apagado; `PC_RPAsset` ya tiene Forward+ y Resident Drawer, sin GPU Occlusion. No funciona en OpenGL ES (el respaldo no se beneficiaría), solo aplica a mallas normales (no a personajes con esqueleto ni partículas) y en tabletas de gama baja el costo extra de GPU podría anular la ganancia. El Resident Drawer por sí solo puede reducir la carga de CPU del pasto y los arbustos.
- **Mallas muy pesadas (probable mayor ganancia):** faroles 7 × ~500.000 triángulos, estatuas de águila 5 × ~500.000, los 2 `puente` ~1,46 M cada uno (43 renderers), templo flotante ~500.000, golem ~280.000 (las cifras suman todos los LOD, así que están algo infladas). Reducir a unos pocos miles de triángulos o añadir LOD.
- **Distancia de vista:** la bruma oculta todo desde ~190 m pero la cámara llega a 3.500 m. Reducir el *Far Clip* y poner distancias de culling por capa (pasto, mariposas, arbustos) quitaría objetos sin cambiar lo que se ve; comprobar que el borde del mar no se note.
- **Orden recomendado:** 1) medir con el Profiler en una tableta real (CPU vs GPU); 2) simplificar mallas pesadas; 3) ajustar distancia de cámara y culling por capa; 4) probar Forward+ + Resident Drawer + GPU Occlusion en una rama (dispositivo con Vulkan); 5) occlusion horneado al final, si acaso.

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

### Cambiado
- **Las tarjetas de "Tu ruta" ya no abren el ejercicio directo** (`ActivitySelectUI.cs`, `PortalAutoWalker.cs` nuevo, `ActivityRoute.PendingPortalScene`, `PortalTrigger.TargetSceneName`): al elegir una tarjeta se vuelve al mundo con la pantalla de carga "Llevándote al portal..." y el personaje aparece a 6 m del portal de esa actividad (Números → portal de números; letras minúsculas y mayúsculas → portal de letras, con la forma elegida) y camina hasta cruzarlo; el reto se abre al entrar. Los portales están en islas propias sin camino a pie desde el pulpo, por eso aparece frente al portal en lugar de recorrer el mundo. El componente se crea solo, no hay que añadirlo a la escena. Si no llega a entrar en 8 s se cancela y se limpia la selección. La tarjeta Consonantes sigue bloqueada.
- **"Volver" en "Tu ruta" regresa al punto donde estaba el personaje al tocar el pulpo** (`TapToLoadScene.cs`, `PortalReturnState`): antes volvía al inicio de la escena. Se guarda la posición al tocar al pulpo y `PlayerSpawnRestorer` la restaura.
- Desde un reto abierto por tarjeta, *Volver* devuelve al mundo junto al portal (comportamiento normal de los portales), no a "Tu ruta".

### Corregido
- **Caminar con teclado y pulsar el botón Saltar detenía al personaje** (componente `PlayerInput` del Player en `BaseScene`): al tocar un botón en pantalla, el Input System cambiaba solo del esquema "Keyboard&Mouse" a "Touch" y desactivaba las teclas WASD. Ahora `Never Auto Switch Control Schemes` está activado y el esquema por defecto es "Keyboard&Mouse"; el joystick táctil y el botón siguen funcionando porque escriben directo en `PlayerController`.
- **Salto fluido al caminar** (`Encapuchado.controller`, `PlayerAnimatorDriver.cs`, `EncapuchadoSetup.cs`): el clip Saltar dura 1,63 s pero el salto en el aire ~0,8 s, y siempre terminaba en Idle. Al caminar y saltar, el personaje aterrizaba, seguía deslizándose en la pose de salto ~0,7 s, pasaba por Idle y recién después volvía a Caminar. Ahora el Animator tiene el parámetro `Grounded`: Saltar termina al tocar el suelo y va directo a Caminar (si hay movimiento) o a Idle (si está quieto), y el clip se acelera ×1,8 para que el aterrizaje coincida. Menú *Tools > Encapuchado > 3. Ajustar fin del salto (Animator)* aplica el cambio al controlador existente.
- **Joystick en reposo mal ubicado**: la zona táctil `Joystick_Area` había quedado con desplazamientos (se veía como un cuadro pequeño) y el mando en reposo no llegaba a la esquina. `ControlsArtGenerator.ApplyToScene` ahora la restablece (mitad izquierda, 70 % de alto, sin desplazamientos) y el salto volvió a (-140, 140).

### Cambiado
- **`AudioToggleButton` y `AmbientMusicPlayer` usan `GameAudio`**: el estado del audio (activado, volumen de música y de efectos) vive en un solo lugar y se guarda en PlayerPrefs (`audio_enabled`, `audio_music_volume`, `audio_sfx_volume`). El volumen base de la música pasó de 0,6 a 0,85 para que al 70 % suene igual que antes.
- **Aviso:** *Tools > UI > Rebuild Pause Menu (Only Art Image)* reconstruye el menú con la imagen original y quita el panel de audio; después hay que ejecutar *Add Audio Panel to Pause Menu*.

### Agregado
- **Pantalla de actividades "Tu ruta"** (escena `ActivitySelect`, `ActivitySelectUI.cs`, `ActivityCardView.cs`, `ResponsiveCardGrid.cs`, `SafeAreaFitter.cs`, `ActivityProgress.cs`, `ActivitySelectSceneBuilder.cs`, menú *Tools > Scenes > Build Activity Select Scene (Tu ruta)*): al tocar al pulpo (`TapToLoadScene`, collider nuevo) se muestra la pantalla de carga "Cargando tu progreso..." mientras se baja el progreso del backend (antes solo se oscurecía la pantalla). Cuatro tarjetas: Números (10 números, 10 % cada uno), Letras minúsculas "Aprende a hacer las vocales" y Letras mayúsculas "Ahora aprende las vocales en mayúsculas" (5 vocales, 20 % cada una) y Consonantes (próximamente, bloqueada). Estrella al llegar al 100 %. Responsive: Canvas Scaler 1920x1080 (match 0,5), área segura, anclas proporcionales, textos con autoajuste y rejilla que pasa de una fila (horizontal) a 2x2 (vertical o casi cuadrada).
- **Pantalla de carga reutilizable** (`LoadingOverlay.cs`): mismo estilo que la del Login (tarjeta, aro, barra y porcentaje) o solo fundido. Al pulsar *Volver* (en la pantalla de actividades y en las actividades) se guarda el progreso en el backend con esta pantalla antes de cambiar de escena.
- **Sincronización de progreso** (`ProgressSyncService.cs`, `TraceProgress.cs`): tras el login se baja el progreso (`GET /progress/mine`) y se suma al local; al terminar un trazo y al volver se sube (`POST /progress/sync`, cuerpo `{"completed":["lo_a","up_E","d_7"...]}`). Primero local: sin red el juego sigue y el progreso queda pendiente hasta el siguiente envío. **Las rutas son supuestas: falta confirmarlas con el backend** (constantes en `ProgressSyncService`).
- **Letras por tarjeta** (`ActivityRoute`, `TraceChallenge.cs`): la tarjeta de minúsculas o mayúsculas abre `LetterTrace_2D` con solo esa forma, el desbloqueo A→E→I→O→U cuenta solo esa forma y *Volver* regresa a la pantalla de actividades. Desde el portal sigue igual que antes (ambas formas).
- **Corrección responsive de las escenas de trazo** (`DesignFrame.cs`, `TraceSceneBuilder.cs`): se diseñaban para 1920 px de ancho y en 4:3 (tabletas) se solapaban. Ahora el contenido va en un marco que se escala para caber en cualquier proporción. Escenas `LetterTrace_2D` y `NumberTrace_2D` regeneradas.
- **Toque sobre objetos** (`TapInteractable.cs`): base común de `TapToOpen` y `TapToLoadScene`; un toque rápido en un solo fotograma ahora también cuenta.
- **Cofre y portón animados** (`HingeOpener.cs`, `TapToOpen.cs`, `ProximityOpener.cs`, `OpenablesSetupTool.cs`, menú *Tools > Scenes > Setup Chest and Gate Animations (BaseScene)*): los modelos no traían animaciones, así que las piezas giran por su bisagra. El cofre (`Chest`) abre su tapa (100°) al tocarlo o hacerle clic desde menos de 12 m (un segundo toque lo cierra; arrastrar la cámara o tocar un botón de pantalla no cuenta). El portón de madera `PT_Modular_Gate_Wood_01` abre sus dos hojas (90°) cuando el jugador se acerca a 4 m, hacia el lado contrario al jugador, y las cierra al alejarse a 6 m o al estar en otra plataforma. `HingeOpener` es reutilizable (eventos `Opened`/`Closed`) para los siguientes objetos.
- **Panel de audio en el menú de pausa** (`PauseAudioPanel.cs`, `GameAudio.cs`, `PauseAudioPanelBuilder.cs`, menú *Tools > UI > Add Audio Panel to Pause Menu*): casilla de audio activado y barras de volumen de música y de efectos (pasos de 5 %), dentro de la tarjeta entre "Pausa" y los botones Reanudar y Salir. La tarjeta se alargó con `menu_pause_audio.png` (generada a partir de `menu_pause.png`, que no se modificó) y las zonas de Reanudar y Salir bajaron con ella. Los ajustes se guardan en PlayerPrefs y quedan sincronizados con el botón de audio de la esquina. `AmbientMusicPlayer` escala su volumen con la barra de música.
- **Jerarquía de BaseScene ordenada** (`HierarchyOrganizerTool.cs`, menú *Tools > Scenes > Organize Hierarchy (BaseScene)*): ~420 objetos sueltos en la raíz se agruparon conservando su posición en el mundo. Raíz: `CAMERAS & SYSTEMS`, `LIGHTING & VOLUME`, `GAMEPLAY`, `ENVIRONMENT` (Level_Geometry, Nature/{Trees, Stumps_Logs, Shrubs_Flowers, Rocks, Grass}, Props_Decoration/Fences, Water_Sea), `FAUNA` (Birds, Butterflies, Farm_Animals), `FX` y `UI` (los 4 canvas). Se puede volver a ejecutar sin duplicar.
- **Ciclo de día y noche** (`SkyboxAnimator.cs`, `SkyboxSetupTool.cs`): 12 minutos de día (amanecer 45 s + día + atardecer 45 s) y 12 minutos de noche, con los cielos `FS003_Sunrise/Day/Sunset/Night` de Fantasy Skybox FREE. Cada momento cambia color y rotación de la luz direccional, luz ambiente y color de la bruma. Menú `Tools > Scenes > Setup Animated Skybox (BaseScene)`.
- **Imágenes de los controles táctiles** (`ControlsArtGenerator.cs`): `joy_base.png`, `joy_knob.png`, `btn_jump.png` y `btn_camera.png` en `Art/images`, con la paleta del botón de pausa. Se aplican a `BaseScene` con `Tools > Mobile > Apply Control Art to BaseScene`; se regeneran con `Tools > Art > Generate Control Buttons`. Se añadió `Cam_Hint` (señal de mover la cámara, sin toques).
- **Botón de audio** (`AudioToggleButton.cs`, `btn_audio_on/off.png`): arriba a la derecha, espejo del botón de pausa; silencia o activa todo el sonido y recuerda la elección. Menú `Tools > Mobile > Add Audio Button to BaseScene`.
- **Alineación de controles:** el joystick en reposo y el botón de salto quedan a 240 px de su esquina inferior (antes el joystick quedaba más arriba y al centro porque calculaba su posición antes de que el Canvas escalara); se recoloca si cambia el tamaño de pantalla. `Cam_Hint` centrado sobre el salto.
- **Música ambiente** (`AmbientMusicPlayer.cs`, `AmbientMusicSetupTool.cs`): en `BaseScene` suena de día `Ambient 9 > 2 > 1` y de noche `Night Ambient 5 > 3 > 2` (de `Audio/SFX/25 Rpg Game Tracks`), en bucle, con fundido entre pistas y al cambiar de día a noche. Las 6 pistas se importan en streaming y comprimidas. Menú `Tools > Scenes > Setup Ambient Music (BaseScene)`.
- **Personaje `Encapuchado`** reemplaza la cápsula del jugador (contenedor `Visual`, animaciones Idle/Caminar/Saltar/Festejar).
- **`SpiralLift`**: el jugador se centra en la plataforma al subir y se respeta la posición donde se colocó en la escena.

### Cambiado
- **UV de `Island_Grass_Rim`, `Grass_Cap_Mini_Island_*` y `Plataforma_uno`:** cada objeto tiene su propia malla con UV en metros reales (3 m por repetición); los bordes y tapas no traían UV y la textura se veía plana o estirada. Menú `Tools > Scenes > Fix Grass Rims, Caps and Platforms UV (3 m)`.
- **Materiales de bordes, tapas y plataformas:** todos los `Island_Grass_Rim` y `Grass_Cap_Mini_Island_*` usan `M_YFSM_Sand07` (con textura de color) y las `Plataforma_uno*` el nuevo `M_Plataforma_Arena` (base `T_YFSM_03_d`). `M_YFSM_Sand01/02/05` no traen textura de color en el paquete y se veían planos.
- **`Stylized_Tree`:** el tronco y la copa no tenían UV (se veían de un solo color) y la copa usaba el material de piso del bosque; ahora tienen UV propias y la copa usa `M_Hojas_Arbol` (verde). Se incluyó en `Fix Grass Rims, Caps and Platforms UV`.
- **Pasto en `Plataforma_uno (1)`:** `GrassScatterTool.cs` reparte ~130 matas de `PT_Grass_02` en manchones por la arena libre (esquiva linternas, rocas, camino, cerca y el pasto existente). Menú `Tools > Scenes > Scatter Grass On Plataforma_uno (1)` (se puede repetir; reemplaza el grupo `Grass_Plataforma_uno_1`).
- **Animales de granja (Cartoon Animal Pack):** al jugar desaparecían porque sus clips mueven la raíz del modelo y los mandaban al origen del mundo. Cada animal está ahora dentro de un contenedor con `FarmAnimal.cs` (Idle y saltitos ocasionales). Menú `Tools > Scenes > Setup Farm Animals (BaseScene)`.
- **Puentes (`puente`, `puente (2)`):** se desactivaron 84 colisiones invisibles (piezas `.001` —postes, tablones, barandas— con el render apagado) que quedaban en el centro del puente y trababan al jugador a mitad de camino. La colisión visible (`Bridge`) se conserva.
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
