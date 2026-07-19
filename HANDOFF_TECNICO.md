# GUAYAKILL SIMULATOR — Handoff Técnico Completo

> Documento de traspaso para otro agente de IA. Contiene TODO el contexto técnico del proyecto
> a la fecha (2026-07-15). Léelo completo antes de tocar código.

---

## 0. Cómo conectarte al proyecto (MCP for Unity)

- **Ruta del proyecto:** `C:\Users\kevoe\GUAYAKILL U_3D` (Unity, Universal Render Pipeline / URP).
- **Se controla vía MCP for Unity** (servidor `unityMCP`). Herramientas clave: `manage_gameobject`, `manage_components`, `manage_scene`, `manage_asset`, `create_script`/`manage_script`/`script_apply_edits`/`apply_text_edits`, `batch_execute`, `execute_code`, `manage_camera`, `read_console`, `manage_editor`, `refresh_unity`, `find_gameobjects`.
- Si hay varias instancias Unity conectadas usa `set_active_instance` con `Name@hash` (mirar recurso `mcpforunity://instances`). La instancia es `GUAYAKILL U_3D@<hash>`.
- **Escena principal de trabajo:** `Assets/Scenes/SampleScene.unity` (aquí viven managers, jugador, NPCs, UI). ⚠️ El nombre "SampleScene" es histórico; ES la escena del juego.
- **Prefab del mapa:** `Assets/Abandoned_Asylum/Prefabs/Asylum.prefab` (instanciado en la escena como `Asylum_Map`). Los quirófanos están DENTRO de este prefab.

### Gotchas de MCP aprendidos (IMPORTANTE)
- `execute_code` compila con **CodeDom = C# 6**: NO local functions, NO expression-bodied locals, NO `using` en el cuerpo. `Object` es ambiguo → usa `UnityEngine.Object.DestroyImmediate/FindObjectsByType`.
- `execute_code` bloquea patrones peligrosos (`AssetDatabase.DeleteAsset`) por safety_checks; pásalo con `safety_checks:false` si es intencional.
- `batch_execute`: los `tool` van SIN prefijo `mcp__unityMCP__` (usa `"manage_gameobject"`, no el nombre completo). Máx 25 comandos por batch (config).
- `manage_components set_property` con **Color**: usa formato objeto `{"r":..,"g":..,"b":..,"a":..}`, NO array `[r,g,b,a]` (el array falla con error de JSON). Para referencias a GameObject de escena usa `{"name":"X"}` dentro de `properties:{...}`.
- Crear un GameObject vacío hijo de un Canvas NO le agrega `RectTransform` automáticamente vía MCP → añádelo explícito antes de fijar anchors. Al reparentar UI, los hijos conservan `anchoredPosition` viejo (puede quedar en -1280 → texto invisible); fíjalo a `[0,0]`.
- Prefab stage (editar un prefab abierto) NO se guarda con `EditorSceneManager.SaveScene` ("preview scene") → usa `PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath, out ok)`. Cierra prefab mode con `StageUtility.GoToMainStage()`.
- `script_apply_edits` con `anchor_replace` trata el anchor como **regex** → escapa `()[]<>` o falla; si falla el batch es atómico (no aplica nada). Para edits multilínea grandes es más fiable el editor de archivo directo (Write/Edit) + `refresh_unity`.
- Recrear un `.cs` (delete+create) NO rompe el componente en la escena ni su wiring — Unity re-matchea por nombre de clase.
- ⚠️ **El editor de Unity SIN FOCO casi no avanza frames** (`Time.frameCount` se queda ~1). Las corrutinas/`Update`/`Destroy` (diferido a fin de frame) NO corren durante los tests por MCP. Para probar lógica hay que **forzar estados por reflexión** o confiar en el código. Cuando el usuario juega con la ventana enfocada, todo corre a 60fps normal.
- Un bake fino de NavMesh de TODO el mapa vía `NavMeshSurface.BuildNavMesh()` a voxel <0.2 **crashea Unity por OOM** (llega a 3GB). Ver sección NavMesh.

---

## 1. El juego

- **Nombre:** Guayakill Simulator. **Género:** sátira / simulador de emergencias médicas en un hospital distópico de Guayaquil ("Guayakill").
- **Plataforma:** Mobile-first (Android/iOS) → futuro VR (Meta Quest / OpenXR). Por eso: **usar New Input System** (nada de `UnityEngine.Input` legacy), UI escalable, nada de resolución fija.
- **Estética:** "color de 64 bits" (saturado, chillón), humor barrial, narrador sarcástico, jerga guayaca.
- **Mecánica central:** el jugador (pasante FPS) camina por el hospital, llegan pacientes heridos (bala / arma blanca / accidente) a dos camillas, y debe estabilizarlos con herramientas limitadas y **minijuegos de precisión** bajo presión de tiempo. Triaje entre 2 pacientes simultáneos. IA de dificultad adaptativa.

---

## 2. Estructura de la escena (SampleScene) — 19 raíces

| GameObject | Contenido / rol |
|---|---|
| `Directional Light`, `Global Volume` | Iluminación URP por defecto |
| `Asylum_Map` | Instancia del prefab del asilo (2 pisos + gradas). Tiene `NavMeshSurface`. Contiene los quirófanos (ver §7) |
| `Player` | Prefab `Mini First Person Controller/First Person Controller.prefab`. Hijo `First Person Camera` (tag MainCamera, `FirstPersonLook`, `Zoom`). Componentes: `FirstPersonMovement`, `Jump`, `Crouch`, Rigidbody, CapsuleCollider |
| `NPC_Paciente_1..8` | Ciudadanos low-poly (DavidJalbert). Cada uno: `NavMeshAgent` (radius 0.25) + `NPCWanderAI` + `PatientBody` + `PatientHighlight`. Deambulan y pueden ser reclutados como pacientes |
| `MenuCanvas` | Canvas ScreenSpaceOverlay (ref 1080×1920). Contiene TODOS los paneles de menú (Splash, MainMenu, Settings, Achievements, Avatar, Pause) construidos por MCP + los HUD de gameplay construidos POR CÓDIGO en runtime |
| `EventSystem` | `InputSystemUIInputModule` (New Input System) |
| `GameFlow` | Componente `MenuFlowManager` (flujo de pantallas + control de jugador/cursor) |
| `GameSystems` | Los 7 managers: `GameManager`, `PatientManager`, `MedicalToolsManager`, `UIManager`, `AchievementManager`, `MiniGameManager`, `AudioManager` |
| `DifficultyDirector` | Componente `DifficultyDirector` (IA de dificultad) + `DifficultyDebugHUD` (HUD de test con teclas G/H/P/N) |
| `EntradaPacientes` | Empty en (2,0.2,8) — punto de spawn de pacientes nuevos |
| `NavLink_Quirofano1` | `NavMeshLink` que conecta la puerta del Quirófano 1 (ver §7) |

**Active Input Handling = Both** en Project Settings (necesario porque algunos scripts de terceros usan `Input` legacy). Todo el input propio usa New Input System.

---

## 3. Scripts propios (Assets/Scripts) — 21 archivos

### Managers/ (en GameObject `GameSystems`, salvo DifficultyDirector y MenuFlowManager)

**`DifficultyDirector.cs`** (en GO `DifficultyDirector`, singleton, `DontDestroyOnLoad`) — **IA #1: dificultad adaptativa** estilo "AI Director" de Left 4 Dead.
- `performanceScore` (0-1, media móvil exponencial, `smoothingFactor` 0.25), `currentTier` (Facil/Normal/Dificil/Infernal), `diaActual`, contadores salvados/perdidos.
- `RegistrarResultadoPaciente(bool salvado, float tiempo, float tiempoLimite, int errores)` → recalcula el score.
- API que consume el resto: `GetPesosHeridas()` (WoundWeights bala/cuchillo/accidente), `GetIntervaloAparicion()` (25-55s), `GetMultiplicadorTiempoLimite()`, `GetTiempoLimitePacienteSegundos()` (base 45s −2/día, mín 25, × multiplicador).
- Eventos `OnPerformanceUpdated`, `OnDifficultyChanged`.

**`GameManager.cs`** (singleton) — flujo global. `GameState` {Menu, EnTurno, FinDeTurno}, `daysSurvived`, `pacientesPorDia=3`. `IniciarTurno/StartNewDay/RegistrarPacienteResuelto/EndDay/ReiniciarTurno/TerminarTurno`. Eventos `OnStateChanged`, `OnDayStarted`, `OnDayEnded`.

**`PatientManager.cs`** (singleton) — **núcleo del gameplay, sistema de SLOTS (2 camillas, 2 pacientes a la vez).**
- `public class Slot { estacion, paciente, pacientePendiente, cuerpo(PatientBody), tiempoRestante/Limite/Atendiendo, errores, tratamientoIniciado, minijuegoActivo, reservadoDesde; bool Libre; bool EnTratamiento; }`
- `List<Slot> slots` (uno por `TreatmentStation`), `Slot slotEnfocado` (el más cercano a `Camera.main` dentro de `rangoEnfoque`=5m, recalculado cada frame).
- `CicloDeLlegadas` (corrutina): **flujo constante** — cada iteración despacha a TODAS las camillas libres (`TryDespachar`, con try-catch), espera `GetIntervaloAparicion()*0.5`. NO se detiene por muertes.
- `DespacharPacienteA(slot)`: genera Patient, elige cuerpo (`ElegirCuerpo`: reutiliza un NPC que deambula o spawnea en `entradaSpawn` con `prefabPacienteSpawn`, prob `probabilidadSpawn`=0.5), lo manda a la camilla (`cuerpo.AsignarComoPaciente(estacion, callback)`).
- `OnCuerpoAcostado(slot)`: cuando el cuerpo se acuesta → `slot.paciente` set, `tratamientoIniciado=true`, arranca cronómetro.
- `Update`: por slot en tratamiento (y `!minijuegoActivo`) → resta tiempo + aplica sangrado → resuelve si muere/estabiliza. **`AbortarReserva`**: si un cuerpo despachado no llega en 25s → lo destruye y libera la camilla (evita camillas bloqueadas).
- `ResolverSlot(slot, bool salvado)` → registra en DifficultyDirector + GameManager, `cuerpo.AltaMedica(salvado)`, dispara `OnPacienteResuelto`.
- Wireado en Inspector: `entradaSpawn`=EntradaPacientes, `prefabPacienteSpawn`=`Assets/Prefabs/PacienteSpawneable.prefab`.

**`MedicalToolsManager.cs`** (singleton) — inventario + aplicación de herramientas. Herramientas: Gasas(5), Alcohol(3), Pinzas(1,reutilizable), Suturas(4), Torniquete(2), Kit(1), Oración(∞, 5%→30% con rasgo Creyente). `rangoAtencion`=3m.
- `UseTool(nombre)`: opera sobre `pm.slotEnfocado`. **Requiere proximidad**: si `Distance(Camera.main, slot.estacion.PuntoAcostado()) > rangoAtencion` → rechaza "Acércate más". Si la herramienta es de precisión (Pinzas/Suturas/Torniquete) Y es la correcta para la herida → lanza minijuego (`MiniGameManager.Jugar`), setea `slot.minijuegoActivo`; callback: éxito=Curar(maxHealth)+ResolverSlot, fallo=AplicarDanio+error. Herramienta incorrecta cura 30% + error. Rasgo Manos Firmes = pinzas +25% y menos temblor.
- `RestockTools` al terminar el día. Eventos `OnInventarioCambiado`, `OnHerramientaUsada(nombre,exito,msg)`.

**`AchievementManager.cs`** (singleton) — Fase 5. 5 logros del doc, métricas persistentes con PlayerPrefs (`ach_metric_N`, `ach_unlocked_id`). Se suscribe a `OnPacienteResuelto` (salvados, arma blanca=sutura) y `OnDayEnded` (días). `TextoPanel()` con ★/🔒. Wireado: `MenuFlowManager.achievementsListText` refresca en `IrALogros()`.

**`AudioManager.cs`** (singleton) — Fase 6. **SFX SINTETIZADOS POR CÓDIGO** (no requiere importar): `AudioClip.Create`+`SetData` con ondas seno/cuadrada+envolvente. click(herramienta), ding(salvado), buzz(perdido), alarma(llega crítico), latido(acelera si paciente enfocado <60% vida). `musicaFondo` = mp3 existente de Stethoscope (loop). 3 AudioSources creados en Awake. Enganchado a los eventos de PatientManager/MedicalToolsManager.

**`MenuFlowManager.cs`** (en GO `GameFlow`) — flujo de pantallas + control del jugador.
- `PantallaMenu` {Splash, MenuPrincipal, Ajustes, Logros, Personalizacion, Gameplay}. `MostrarSolo(pantalla)` activa/desactiva paneles.
- Splash→click→MenuPrincipal; MenuPrincipal→{Ajustes, Logros, IniciarTurno→Personalizacion}; Personalizacion→Confirmar→Gameplay (guarda en `RunConfig`, `GameManager.IniciarTurno`).
- Pausa (Esc): `TogglePausa` (Time.timeScale=0, muestra pauseMenuPanel al frente con `SetAsLastSibling`). Reanudar/Reiniciar/SalirAlMenu.
- **Cursor/cámara:** `cursorLibre` toggled con **Tab** → `ToggleCursorLibre`: libera cursor (CursorLockMode.None) y **apaga `FirstPersonLook.enabled=false`** (cámara 100% quieta) para poder hacer clic en el HUD; Tab de nuevo vuelve a explorar. `SetPlayerControl` reactiva `.enabled`. Referencia `playerGameObject`=Player wireada; `playerLook`/`playerMovement` resueltos en Start.

### Gameplay/

**`Patient.cs`** — clase serializable pura (NO MonoBehaviour). Campos: nombre, `TipoHerida`{Bala,ArmaBlanca,Accidente}, `Severidad`{Leve,Moderado,Critico}, health/maxHealth, bloodLossPorSegundo, diagnostico, dialogoAbsurdo. Métodos: `AplicarDanio/Curar/EstaVivo/EstaEstable`(≥99% maxHealth)/`HerramientaCorrecta`(Bala→Pinzas, ArmaBlanca→Suturas, Accidente→Torniquete)/`NombreHerida`.

**`PatientGenerator.cs`** — estático. `GenerarPaciente()`: 16 nombres guayacos + 16 diálogos absurdos, herida por `GetPesosHeridas()` de la IA, severidad escala por día. **Vida/sangrado escalan con `performanceScore`**: vida ×Lerp(1,0.6), sangrado ×Lerp(1,1.6) → más nivel = pacientes más graves.

**`TreatmentStation.cs`** — marca una camilla. Registro estático `List<TreatmentStation> Stations` (OnEnable/OnDisable). `PuntoAcercamiento()` (validado contra NavMesh: prueba frente/opuesto/cercano), `PuntoAcostado()` (pos + up*alturaSuperficie), `RotacionAcostado()` (90° X = tumbado), `Ocupar/Liberar/Ocupada`, `MasCercanaLibre`. Config: MorgueTable alturaSuperficie 0.95, opertation_table 1.0, distanciaAcercamiento 1.3.

**`PatientBody.cs`** (en NPCs + prefab spawn) — comportamiento físico del paciente. Estados {Libre, Caminando, Acomodandose, Acostado, Retirandose}. `AsignarComoPaciente(estacion, callback)`: desactiva NPCWanderAI, activa highlight rojo, NavMeshAgent camina al PuntoAcercamiento (radius 0.25, `stoppingDistance` 0.3). Gate de llegada: solo se acuesta cuando `remainingDistance` OK + `velocity`≈0 + `Distance<1.4` del acercamiento → lerp de posición+rotación a la camilla → callback (arranca cronómetro). `AltaMedica(bool salvado)`: **muerto→`Destroy` (desaparece); salvado→Retirandose (se levanta y vuelve a deambular)**. `esSpawneado` (los spawn se destruyen igual al morir).

**`NPCWanderAI.cs`** (en NPCs) — **IA #2 (parcial): pathfinding NavMesh**. Deambula eligiendo puntos aleatorios (`NavMesh.SamplePosition`), sube gradas solo. Velocidad dinámica: base 1.6 + hasta 2.2 por `performanceScore` + 0.15/min de sesión, tope 6. En Start hace Warp al NavMesh si cae fuera.

**`PatientHighlight.cs`** (en NPCs + prefab) — silueta roja del herido. `Activar()` agrega el material `Assets/Materials/RedOutline.mat` como slot EXTRA en cada Renderer (re-dibuja la malla extruida = contorno); `Desactivar()` restaura. Shader `Assets/Materials/RedOutline.shader` (URP inverted-hull, `Cull Front`). Activado en AsignarComoPaciente, desactivado en AltaMedica.

**`MiniGameManager.cs`** (singleton) — **Fase 4, minijuegos de precisión.** UI construida por código, input `UnityEngine.InputSystem.Pointer.current` (mouse/touch/VR). `Jugar(TipoHerida, Patient, Action<MiniGameResult>)`:
- Bala → **Extracción**: mantener el cursor (sigue al Pointer) sobre la bala pese al temblor sinusoidal (Manos Firmes lo reduce 55%); barra de progreso llena=éxito; timeout 9s=fallo (daño 15+sangrado).
- ArmaBlanca → **Sutura**: tocar 5 puntos en orden que oscilan; 12s límite; fallo daño 18.
- Accidente → **Torniquete**: marcador rebota en barra, `Pointer.press` en zona verde; 2/3 aciertos=éxito; fallo daño 16.
- `EnCurso` bloquea usar otras herramientas. `MiniGameResult{success, damageIfFailed, failureMessage}`.

### UI/

**`UIManager.cs`** (singleton) — HUD del gameplay **construido por código** en Start (cero wiring). Dos paneles de paciente: principal (enfocado) + `panelPaciente2` (el otro, para triaje) — **sondea `pm.slots` cada frame** (`PintarPaneles`), muestra nombre/herida/"Cura con"/vida(barra verde→amarillo→rojo)/cronómetro. Contador de días, línea de narrador, toolbar de 7 botones con `[N]` (atajos). **Atajos teclado 1-7** (`Keyboard.current`) llaman `UseTool`. `MostrarNarrador(msg, seg)`.

**`SingleSelectButtonGroup.cs`** — grupo de botones de selección única (color de uniforme, nivel, rasgo). **`ToggleTextButton.cs`** — botón binario SÍ/NO (mascarilla, guantes). **`TextGlitchFX.cs`** — vibra los vértices TMP del logo "ESPE GAMES" (splash).

### Utils/

**`RunConfig.cs`** — estático. Config del avatar elegida en Personalización: nombreJugador, colorUniformeIndex, usaMascarilla/Guantes, `NivelExperiencia`, `RasgoInicial`{ManosFirmes, Creyente, ResistenteAlEstres}. Lo consume MedicalToolsManager/MiniGameManager.

**`InputCompat.cs`** — puente `IsKeyPressed(KeyCode)`/`IsKeyDown(KeyCode)` que traduce KeyCode legacy a `Keyboard.current` (para campos KeyCode del Inspector de assets de terceros sin usar `Input` legacy).

**`DifficultyDebugHUD.cs`** (en GO DifficultyDirector) — HUD de test: teclas G(salvar rápido)/H(salvar lento)/P(perder)/N(día siguiente) simulan resultados y muestran el estado de la IA. Apagar `showHUD` para builds.

---

## 4. Flujo de datos / arquitectura de eventos

```
Menús (MenuFlowManager) → Confirmar → GameManager.IniciarTurno() → GameState.EnTurno
   → PatientManager.ComenzarTurno() → CicloDeLlegadas (corrutina, flujo constante)
        → DespacharPacienteA(slot libre) → PatientGenerator.GenerarPaciente()
             (herida/severidad/vida ← DifficultyDirector)
        → ElegirCuerpo() → PatientBody.AsignarComoPaciente(estacion)
             → NavMeshAgent camina → se acuesta → OnCuerpoAcostado → cronómetro ON
   → Jugador se acerca (<3m) → UseTool(tecla 1-7 o clic) sobre slotEnfocado
        → si precisión+correcta → MiniGameManager.Jugar → éxito/fallo
        → ResolverSlot(salvado?) → DifficultyDirector.RegistrarResultado()  (IA reacciona)
                                 → GameManager.RegistrarPacienteResuelto()  (días)
                                 → AchievementManager (logros)  → AudioManager (ding/buzz)
                                 → PatientBody.AltaMedica(salvado)  (desaparece / se levanta)
        → slot libre → CicloDeLlegadas rellena  (flujo nunca para)
GameManager: cada 3 pacientes → EndDay → DifficultyDirector.AvanzarDia + RestockTools
```

Patrones: **Singleton** (todos los managers), **Observer** (eventos C# `event Action`). El HUD **sondea** (no solo eventos) por los 2 pacientes concurrentes.

---

## 5. Controles (PC / editor)

- **WASD** moverse, **mouse** mirar (FirstPersonLook), **Shift** correr, **Space** saltar, **Ctrl** agacharse.
- **Teclas 1-7** = herramientas (1 Gasas … 3 Pinzas … 5 Torniquete … 7 Oración). Funcionan con el cursor bloqueado.
- **Tab** = alternar EXPLORAR (cursor bloqueado, mouse-look) ↔ ATENDER (cursor libre para clic en HUD; cámara congelada).
- **Esc** = pausa.
- En **móvil** se toca directo (sin problema de cursor). Minijuegos usan Pointer (mouse/touch).

---

## 6. Las dos IAs del juego

1. **DifficultyDirector** (dificultad adaptativa): media móvil exponencial del desempeño → ajusta en vivo pesos de heridas, intervalo de llegada, tiempo por paciente, y (nuevo) gravedad de los pacientes (vida/sangrado). También alimenta la velocidad de los NPCs.
2. **NPCWanderAI + NavMesh** (pathfinding A*): los ciudadanos deambulan, suben gradas, y los reclutados como pacientes navegan a la camilla. Velocidad escala con la dificultad y el tiempo de sesión.

---

## 7. NavMesh — CRÍTICO, leer antes de tocar navegación

- `NavMeshSurface` está en `Asylum_Map`. NavMesh horneado a **voxel 0.2, agentRadius 0.2** (asset `Assets/NavMesh/AsylumNavMesh.asset`). Los agentes (PatientBody) usan radius 0.25.
- ⚠️ **NUNCA rebakear fino todo el mapa con `NavMeshSurface.BuildNavMesh()`** (voxel <0.2) → **crashea Unity por OOM**. Para rehornear usa `NavMeshBuilder.UpdateNavMeshData(surface.navMeshData, settings, sources, bounds)` con `settings = NavMesh.GetSettingsByID(surface.agentTypeID)` (reescribe el asset existente, sin crashear). Voxel seguro ≥0.2.
- **Quirófanos (dentro del prefab Asylum):** `QuirofanoObjectsTools 1 ` (¡espacio al final!) con camilla `MorgueTable`; `QuirofanoObjectsTools 2` con camilla `opertation_table_mesh` (LODGroup). Ambos en el 2º piso (y≈3.9). Cada camilla tiene `TreatmentStation` + props decorativos (Tweezers, BandageRoll de CrowAssets URP, + Syringe/Medkit/Bandages...).
- **La puerta del Quirófano 1 estaba desconectada del NavMesh** (dos islas). Solución: GameObject `NavLink_Quirofano1` con `NavMeshLink` (startPoint (-18.8,3.8,-13.6) → endPoint (-18.8,3.8,-15.0), width 1.4, bidireccional). Si mueves el Quirófano 1 o su camilla, **hay que reubicar este link**. El Quirófano 2 conecta sin link.
- **Diagnóstico de islas:** `NavMesh.CalculatePath(origen, destino)`; si `PathPartial`, el último corner es donde se corta; sondear puntos alrededor con CalculatePath desde el origen → los que dan Partial son la isla aislada → puentear el hueco corto con NavMeshLink.

---

## 8. Assets de terceros relevantes

- `Mini First Person Controller` — controlador FPS del jugador. **Editado**: `FirstPersonMovement.cs` y `FirstPersonLook.cs` migrados a New Input System (`Keyboard.current`/`Mouse.current`); FirstPersonLook tiene `ignorarUnFrame` (descarta el salto de cámara al reactivar mouse-look). Jump/Crouch/Zoom usan `InputCompat`.
- `Abandoned_Asylum` (1.9GB) — mapa. `DavidJalbert/LowPolyPeople` — ciudadanos/pacientes. `Kabungus/HouseholdItems` — props (jeringas, medkit, cuchillos). `CrowAssets/Stylized Bathroom Set` — Tweezers/BandageRoll. `Stethoscope` — mp3 de música + sistema de splines (para futura sutura con trazo). `Dnk_Dev`, `VertexField`, `SimpleSky`, `TextMesh Pro`.
- ⚠️ Hay un `.meta` corrupto: `Assets/Abandoned_Asylum/Textures/Tiled/Walls/Wall_4_Albedo.png.meta` (GUID inválido). Advertencia benigna; borrar el .meta para regenerarlo si molesta.

---

## 9. Estado de las fases (roadmap del documento de diseño)

- ✅ **Fase 1** Exploración + estructura de carpetas (`Assets/Scripts/{Managers,Gameplay,UI,Audio,Utils}`, etc.).
- ✅ **Fase 2** GameManager, Patient(+Generator+Manager), MedicalToolsManager, UIManager.
- ✅ **Fase 3** Patient/PatientGenerator (cubierta en Fase 2, cumple el spec).
- ✅ **Fase 4** MiniGameManager (extracción bala / sutura / torniquete).
- ✅ **Fase 5** AchievementManager (5 logros, panel funcional).
- ✅ **Fase 6** AudioManager (SFX sintetizados + música existente).
- ✅ **IA extra** DifficultyDirector + NPCs NavMesh.
- ✅ **Sistema físico de pacientes** a 2 camillas, flujo constante, silueta roja, proximidad para atender.

### Pendiente / mejoras posibles
- **NarratorManager formal** (hoy los mensajes salen de eventos sueltos en UIManager; el doc pide 20+ frases categorizadas por evento con VO).
- **Audio rico**: voz del narrador, cumbia de emergencia real (que se corta al llegar crítico), pitidos de monitor → el usuario importaría archivos a `Assets/Audio/{Music,SFX,Voice}` o generarlos con IA (needs fal key en la MCP de Unity).
- **Sutura con splines** (el asset Stethoscope tiene Bezier/Spline) para un minijuego de trazo más rico.
- **Object pooling** de pacientes (mobile), compresión de texturas, profiling +60fps (optimización mobile del doc).
- **Highlight visual** del SingleSelectButtonGroup en Personalización (cosmético).
- **Preparación VR real** (WorldSpace UI, interacción con controladores) — hoy está "VR-ready" solo en el sentido de usar Pointer/InputSystem.
- Los pacientes SALVADOS spawneados vuelven a deambular (crecen lento el conteo de NPCs); si molesta, destruirlos tras irse.

---

## 10. Cómo probar (workflow recomendado)

1. `manage_editor play` → `execute_code`: `GameObject.Find("GameFlow").GetComponent<MenuFlowManager>()` y llamar `IrAMenuPrincipal(); IrAPersonalizacion(); avatarConfirmarButton.onClick.Invoke();` para saltar al gameplay.
2. Si `Time.timeScale==0` ponerlo a 1 (a veces queda pausado).
3. **Recordar el throttle de frames sin foco**: para probar lógica de pacientes, forzar estados por reflexión (llamar `DespacharPacienteA`, `OnCuerpoAcostado`, `ResolverSlot`, mover `Camera.main` + `ActualizarEnfoque`, etc.) en vez de esperar corrutinas.
4. `manage_camera screenshot include_image:true` para ver el HUD. `read_console types:["error"]` tras cada cambio.
5. `manage_editor stop` + `manage_scene save` al terminar.

---

## 11. Memoria persistente del agente anterior

Hay notas acumuladas en `C:\Users\kevoe\.claude\projects\C--Users-kevoe-Desktop-Proyecto-Gestion-de-acuerdos-PPP---copia\memory\proyecto-guayakill-simulator.md` (mismo contenido, con historial de fixes y gotchas). Este HANDOFF es el resumen consolidado y autoritativo a 2026-07-15.
