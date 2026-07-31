# GUAYAKILL SIMULATOR

Sátira / simulador de emergencias médicas en un hospital distópico de Guayaquil ("Guayakill"). Eres un pasante FPS en un hospital al borde del caos: llegan pacientes heridos (bala / arma blanca / accidente) a dos camillas y debes estabilizarlos con herramientas limitadas y minijuegos de precisión bajo presión de tiempo.

Desarrollado en **Unity** (Universal Render Pipeline) con un enfoque **mobile-first** (Android/iOS), pensado para futuro VR (Meta Quest / OpenXR).

## Mecánicas principales

- **Triaje de 2 pacientes simultáneos** en dos camillas, con flujo constante de llegadas.
- **7 herramientas** (gasas, alcohol, pinzas, suturas, torniquete, kit, oración) con inventario limitado y restock diario.
- **3 minijuegos de precisión** con puntero (mouse/touch/VR): extracción de bala, sutura de arma blanca y torniquete de accidente.
- **IA de dificultad adaptativa** estilo "AI Director" (Left 4 Dead): ajusta en vivo gravedad, intervalo de llegadas, tiempo límite y pesos de heridas según tu desempeño.
- **Sistema físico de pacientes**: los ciudadanos deambulan por el hospital (NavMesh) y, al ser reclutados, caminan solos hasta la camilla.
- **Personalización del avatar**: nombre, color de uniforme, mascarilla/guantes, nivel de experiencia y rasgo inicial (Manos Firmes, Creyente, Resistente al Estrés).
- **5 logros** con progreso persistente.
- **Audio sintetizado por código** (SFX) + música ambiente.
- Narrador sarcástico con jerga guayaca.

## Controles (PC / editor)

| Acción | Tecla |
|---|---|
| Moverse | WASD |
| Mirar | Mouse |
| Correr / Saltar / Agacharse | Shift / Espacio / Ctrl |
| Herramientas (1-7) | 1 Gasas · 2 Alcohol · 3 Pinzas · 4 Suturas · 5 Torniquete · 6 Kit · 7 Oración |
| Alternar explorar ↔ atender (cursor libre) | Tab |
| Pausa | Esc |

En móvil se toca directamente; los minijuegos usan el puntero (mouse/touch).

## Estructura de la escena

- **Escena principal:** `Assets/Scenes/SampleScene.unity` (los managers, el jugador, los NPCs y la UI de menús viven aquí).
- **Prefab del mapa:** `Assets/Abandoned_Asylum/Prefabs/Asylum.prefab` (contiene los quirófanos y las camillas con `TreatmentStation`).
- **Managers (GameObject `GameSystems`):** `GameManager`, `PatientManager`, `MedicalToolsManager`, `UIManager`, `AchievementManager`, `MiniGameManager`, `AudioManager`.
- **IA de dificultad:** `DifficultyDirector` + `DifficultyDebugHUD` (HUD de test con teclas G/H/P/N).

## Scripts propios

`Assets/Scripts/` se organiza en:

- **Managers/** — flujo global, slots de camillas, inventario de herramientas, logros, audio y flujo de menús.
- **Gameplay/** — `Patient`, `PatientGenerator`, `TreatmentStation`, `PatientBody`, `NPCWanderAI`, `PatientHighlight`, `MiniGameManager`.
- **UI/** — HUD de gameplay construido por código, botones de personalización y efectos de glitch.
- **Utils/** — config del avatar (`RunConfig`) y utilidades de input.

## Requisitos

- Unity 6 (URP), New Input System (compatible con "Both").
- NavMesh horneado en `Assets/NavMesh/AsylumNavMesh.asset`.

## Notas técnicas importantes

- ⚠️ **No rebakear el NavMesh fino con `NavMeshSurface.BuildNavMesh()`** (crashea por OOM). Usar `NavMeshBuilder.UpdateNavMeshData()` con voxel ≥ 0.2.
- La puerta del Quirófano 1 se conecta al NavMesh mediante `NavMeshLink` (`NavLink_Quirofano1`). Si mueves el quirófano, hay que reubicar el link.
- Más contexto en `HANDOFF_TECNICO.md`.
