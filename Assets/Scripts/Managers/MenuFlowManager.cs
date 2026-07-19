using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.Events;

// Pantallas del flujo de menús, en el orden del documento de diseño:
// Splash -> Menu Principal -> (Ajustes | Logros | Personalizacion) -> Gameplay -> Pausa
public enum PantallaMenu
{
    Splash,
    MenuPrincipal,
    Ajustes,
    Logros,
    Puntuaciones,
    Personalizacion,
    Gameplay
}

// Controlador central del flujo de pantallas de "Guayakill Simulator".
// Reemplaza al antiguo MenuInicial.cs (solo tenía Bienvenida + Pausa) con el flujo
// completo del documento de diseño.
public class MenuFlowManager : MonoBehaviour
{
    [Header("Paneles (pantallas)")]
    public GameObject splashPanel;
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject achievementsPanel;
    public GameObject avatarPanel;
    public GameObject pauseMenuPanel;

    [Header("Splash")]
    public GameObject splashClickCatcher;

    [Header("Menú Principal")]
    public Button ajustesButton;
    public Button logrosButton;
    public Button puntuacionesButton;
    public Button iniciarTurnoButton;

    [Header("Logros")]
    public TMPro.TextMeshProUGUI achievementsListText;

    [Header("Puntuaciones")]
    public GameObject scoresPanel;
    public TMPro.TextMeshProUGUI scoresListText;
    public Button scoresVolverButton;

    [Header("Botones Volver")]
    public Button ajustesVolverButton;
    public Button logrosVolverButton;
    public Button avatarVolverButton;

    [Header("Personalización de Avatar - campos")]
    public TMP_InputField nombreInput;
    public SingleSelectButtonGroup colorGroup;
    public ToggleTextButton mascarillaToggle;
    public ToggleTextButton guantesToggle;
    public SingleSelectButtonGroup experienciaGroup;
    public SingleSelectButtonGroup rasgosGroup;
    public Button avatarConfirmarButton;

    [Header("Pausa")]
    public Button reanudarButton;
    public Button reiniciarButton;
    public Button salirAlMenuButton;

    [Header("Referencias del jugador")]
    public GameObject playerGameObject;

    FirstPersonLook playerLook;
    FirstPersonMovement playerMovement;
    PantallaMenu pantallaActual;
    bool juegoActivo = false;
    bool pausado = false;
    Vector3 posicionInicialJugador;
    Quaternion rotacionInicialJugador;

    void Start()
    {
        if (playerGameObject != null)
        {
            playerLook = playerGameObject.GetComponentInChildren<FirstPersonLook>();
            playerMovement = playerGameObject.GetComponent<FirstPersonMovement>();
            posicionInicialJugador = playerGameObject.transform.position;
            rotacionInicialJugador = playerGameObject.transform.rotation;
        }

        WireButtons();
        SetPlayerControl(false);
        SetCursorState(false);
        SetActivo(pauseMenuPanel, false);
        MostrarSolo(PantallaMenu.Splash);
    }

    bool cursorLibre = false;

    void Update()
    {
        if (juegoActivo && InputCompat.IsKeyDown(KeyCode.Escape))
        {
            TogglePausa();
        }

        // Tab: alterna entre EXPLORAR (cursor bloqueado, mouse-look) y ATENDER
        // (cursor libre para hacer clic en las herramientas del HUD)
        if (juegoActivo && !pausado)
        {
            var kb = Keyboard.current;
            if (kb != null && kb.tabKey.wasPressedThisFrame)
                ToggleCursorLibre();
        }
    }

    void ToggleCursorLibre()
    {
        cursorLibre = !cursorLibre;
        SetCursorState(!cursorLibre);          // libre = cursor desbloqueado
        if (playerLook != null)
        {
            playerLook.SetControlEnabled(!cursorLibre);
            playerLook.enabled = !cursorLibre;  // apagar el componente entero: su Update no corre = cámara 100% quieta
        }
        if (UIManager.Instance != null)
            UIManager.Instance.MostrarNarrador(cursorLibre
                ? "Modo ATENDER: haz clic en las herramientas (Tab para volver a explorar)."
                : "Modo EXPLORAR: mueve la cámara y camina (Tab para atender).", 3f);
    }

    void WireButtons()
    {
        // Auto-buscar botones si no están asignados
        if (ajustesButton == null) {
            var go = GameObject.Find("MenuCanvas/MainMenuPanel/AjustesButton");
            if (go != null) ajustesButton = go.GetComponent<Button>();
        }
        if (logrosButton == null) {
            var go = GameObject.Find("MenuCanvas/MainMenuPanel/LogrosButton");
            if (go != null) logrosButton = go.GetComponent<Button>();
        }
        if (puntuacionesButton == null) {
            var go = GameObject.Find("MenuCanvas/MainMenuPanel/PuntuacionesButton");
            if (go != null) puntuacionesButton = go.GetComponent<Button>();
        }
        if (scoresVolverButton == null) {
            var go = GameObject.Find("MenuCanvas/ScoresPanel/ScoresVolverButton");
            if (go != null) scoresVolverButton = go.GetComponent<Button>();
        }
        if (iniciarTurnoButton == null) {
            var go = GameObject.Find("MenuCanvas/MainMenuPanel/IniciarTurnoButton");
            if (go != null) iniciarTurnoButton = go.GetComponent<Button>();
        }
        if (ajustesVolverButton == null) {
            var go = GameObject.Find("MenuCanvas/SettingsPanel/SettingsVolverButton");
            if (go != null) ajustesVolverButton = go.GetComponent<Button>();
        }
        if (logrosVolverButton == null) {
            var go = GameObject.Find("MenuCanvas/AchievementsPanel/AchievementsVolverButton");
            if (go != null) logrosVolverButton = go.GetComponent<Button>();
        }
        if (avatarVolverButton == null) {
            var go = GameObject.Find("MenuCanvas/AvatarPanel/AvatarVolverButton");
            if (go != null) avatarVolverButton = go.GetComponent<Button>();
        }
        if (avatarConfirmarButton == null) {
            var go = GameObject.Find("MenuCanvas/AvatarPanel/AvatarConfirmarButton");
            if (go != null) avatarConfirmarButton = go.GetComponent<Button>();
        }
        if (reanudarButton == null) {
            var go = GameObject.Find("MenuCanvas/PauseMenuPanel/PauseReanudarButton");
            if (go != null) reanudarButton = go.GetComponent<Button>();
        }
        if (reiniciarButton == null) {
            var go = GameObject.Find("MenuCanvas/PauseMenuPanel/PauseReiniciarButton");
            if (go != null) reiniciarButton = go.GetComponent<Button>();
        }
        if (salirAlMenuButton == null) {
            var go = GameObject.Find("MenuCanvas/PauseMenuPanel/PauseSalirButton");
            if (go != null) salirAlMenuButton = go.GetComponent<Button>();
        }

        if (splashClickCatcher != null)
        {
            var btn = splashClickCatcher.GetComponent<Button>();
            WireButton(btn, IrAMenuPrincipal);
        }
        WireButton(ajustesButton, IrAAjustes);
        WireButton(logrosButton, IrALogros);
        WireButton(puntuacionesButton, IrAPuntuaciones);
        WireButton(scoresVolverButton, IrAMenuPrincipal);
        WireButton(iniciarTurnoButton, IrAPersonalizacion);
        WireButton(ajustesVolverButton, IrAMenuPrincipal);
        WireButton(logrosVolverButton, IrAMenuPrincipal);
        WireButton(avatarVolverButton, IrAMenuPrincipal);
        WireButton(avatarConfirmarButton, ConfirmarPersonalizacionEIniciarGameplay);
        WireButton(reanudarButton, TogglePausa);
        WireButton(reiniciarButton, ReiniciarTurno);
        WireButton(salirAlMenuButton, SalirAlMenuPrincipal);
    }

    void WireButton(Button boton, UnityAction accion)
    {
        if (boton == null) return;
        boton.onClick.AddListener(accion);
    }

    void MostrarSolo(PantallaMenu pantalla)
    {
        pantallaActual = pantalla;
        SetActivo(splashPanel, pantalla == PantallaMenu.Splash);
        SetActivo(mainMenuPanel, pantalla == PantallaMenu.MenuPrincipal);
        SetActivo(settingsPanel, pantalla == PantallaMenu.Ajustes);
        SetActivo(achievementsPanel, pantalla == PantallaMenu.Logros);
        SetActivo(scoresPanel, pantalla == PantallaMenu.Puntuaciones);
        SetActivo(avatarPanel, pantalla == PantallaMenu.Personalizacion);
    }

    void SetActivo(GameObject go, bool activo)
    {
        if (go != null) go.SetActive(activo);
    }

    public void IrAMenuPrincipal() => MostrarSolo(PantallaMenu.MenuPrincipal);

    public void IrAAjustes()
    {
        MostrarSolo(PantallaMenu.Ajustes);
        WireAudioSliders();
    }

    public void IrALogros()
    {
        if (achievementsListText != null && AchievementManager.Instance != null)
            achievementsListText.text = AchievementManager.Instance.TextoPanel();
        MostrarSolo(PantallaMenu.Logros);
    }

    bool scoresVolverWired = false;

    public void IrAPuntuaciones()
    {
        // El botón Volver del panel de scores no se pudo cablear en Start (el panel estaba
        // inactivo y GameObject.Find no encuentra inactivos): lo cableamos aquí, la 1ª vez.
        if (!scoresVolverWired)
        {
            if (scoresVolverButton == null && scoresPanel != null)
            {
                var b = scoresPanel.transform.Find("ScoresVolverButton");
                if (b != null) scoresVolverButton = b.GetComponent<Button>();
            }
            if (scoresVolverButton != null)
            {
                scoresVolverButton.onClick.AddListener(IrAMenuPrincipal);
                scoresVolverWired = true;
            }
        }

        if (scoresListText != null)
            scoresListText.text = ScoreManager.TextoPanel();
        MostrarSolo(PantallaMenu.Puntuaciones);
    }

    public void IrAPersonalizacion()
    {
        MostrarSolo(PantallaMenu.Personalizacion);
        WireAvatarGroups();
    }

    // ------------------------- Ajustes: volúmenes -------------------------
    Slider volumeSlider, sfxSlider, musicSlider;
    bool audioWired = false;

    void WireAudioSliders()
    {
        if (audioWired) return;

        // Volumen general (maestro) -> AudioListener.volume
        if (volumeSlider == null)
        {
            var go = GameObject.Find("MenuCanvas/SettingsPanel/VolumeSlider");
            if (go != null) volumeSlider = go.GetComponent<Slider>();
        }
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(SetVolumenGeneral);
        }

        // Sonido (SFX) -> AudioManager.volumenSFX
        if (sfxSlider == null)
        {
            var go = GameObject.Find("MenuCanvas/SettingsPanel/SFXSlider");
            if (go != null) sfxSlider = go.GetComponent<Slider>();
        }
        if (sfxSlider != null && AudioManager.Instance != null)
        {
            sfxSlider.value = AudioManager.Instance.volumenSFX;
            sfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetVolumenSFX);
        }

        // Música -> AudioManager.volumenMusica
        if (musicSlider == null)
        {
            var go = GameObject.Find("MenuCanvas/SettingsPanel/MusicSlider");
            if (go != null) musicSlider = go.GetComponent<Slider>();
        }
        if (musicSlider != null && AudioManager.Instance != null)
        {
            musicSlider.value = AudioManager.Instance.volumenMusica;
            musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetVolumenMusica);
        }

        if (volumeSlider != null && sfxSlider != null && musicSlider != null)
            audioWired = true;
    }

    public void SetVolumenGeneral(float valor)
    {
        AudioListener.volume = valor;
    }

    // ------------------------- Personalización: grupos de selección -------------------------
    bool avatarGroupsWired = false;

    void WireAvatarGroups()
    {
        if (avatarGroupsWired) return;

        if (colorGroup != null)
        {
            colorGroup.preservarColorBoton = true; // no tapar el color propio de cada swatch
            var swatches = new Button[]
            {
                BuscarBoton("MenuCanvas/AvatarPanel/ColorSwatch1"),
                BuscarBoton("MenuCanvas/AvatarPanel/ColorSwatch2"),
                BuscarBoton("MenuCanvas/AvatarPanel/ColorSwatch3"),
                BuscarBoton("MenuCanvas/AvatarPanel/ColorSwatch4"),
            };
            colorGroup.SetBotones(swatches);
            // Pintar cada swatch con su color real de la paleta de uniformes
            for (int i = 0; i < swatches.Length; i++)
            {
                if (swatches[i] == null) continue;
                var img = swatches[i].GetComponent<Image>();
                if (img != null) img.color = RunConfig.ColorUniforme(i);
            }
        }

        if (experienciaGroup != null)
        {
            experienciaGroup.SetBotones(new Button[]
            {
                BuscarBoton("MenuCanvas/AvatarPanel/ExpNovatoButton"),
                BuscarBoton("MenuCanvas/AvatarPanel/ExpIntermedioButton"),
                BuscarBoton("MenuCanvas/AvatarPanel/ExpExperimentadoButton"),
            });
        }

        if (rasgosGroup != null)
        {
            rasgosGroup.SetBotones(new Button[]
            {
                BuscarBoton("MenuCanvas/AvatarPanel/RasgoManosFirmesButton"),
                BuscarBoton("MenuCanvas/AvatarPanel/RasgoCreyenteButton"),
                BuscarBoton("MenuCanvas/AvatarPanel/RasgoResistenteButton"),
            });
        }

        avatarGroupsWired = true;
    }

    Button BuscarBoton(string path)
    {
        var go = GameObject.Find(path);
        if (go == null) return null;
        return go.GetComponent<Button>();
    }

    public void ConfirmarPersonalizacionEIniciarGameplay()
    {
        GuardarPersonalizacionEnRunConfig();

        MostrarSolo(PantallaMenu.Gameplay); // oculta todos los paneles de menú (Gameplay = ninguno visible)
        SetActivo(pauseMenuPanel, false);

        SetPlayerControl(true);
        SetCursorState(true);
        cursorLibre = false;
        juegoActivo = true;
        pausado = false;
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.IniciarTurno();

        // Re-configurar el inventario AHORA que RunConfig ya tiene la experiencia elegida
        // (en Awake del manager todavía tenía los valores por defecto).
        if (MedicalToolsManager.Instance != null)
            MedicalToolsManager.Instance.ConfigurarInventarioInicial();

        // El color del uniforme tiñe el cuerpo del pasante (visible en su sombra / al mirar abajo).
        AplicarColorUniforme();

        if (UIManager.Instance != null)
        {
            // Feedback: resumen del pasante y sus ventajas activas
            UIManager.Instance.MostrarNarrador(ResumenPersonalizacion(), 9f);
            // Los controles, unos segundos después
            CancelInvoke(nameof(MostrarControlesEnNarrador));
            Invoke(nameof(MostrarControlesEnNarrador), 9.5f);
        }

        
Debug.Log($"Turno iniciado. Doc: {RunConfig.nombreJugador} | Rasgo: {RunConfig.rasgoElegido} | Nivel: {RunConfig.nivelExperiencia} | Mascarilla: {RunConfig.usaMascarilla} | Guantes: {RunConfig.usaGuantes}");
    }

    void MostrarControlesEnNarrador()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.MostrarNarrador("Controles: WASD moverte · teclas 1-7 herramientas · Tab liberar el cursor para clic · Esc pausa.", 7f);
    }

    // Resumen legible de la personalización activa y el efecto real de cada opción.
    string ResumenPersonalizacion()
    {
        string masc = RunConfig.usaMascarilla ? "con mascarilla (menos complicaciones)" : "sin mascarilla (más riesgo)";
        string guantes = RunConfig.usaGuantes ? "con guantes (mejor pulso)" : "sin guantes";
        int bono = RunConfig.BonusInventarioExperiencia();
        string exp = "Nivel " + RunConfig.NombreNivel() + (bono > 0 ? " (+" + bono + " suministros, cura +" + Mathf.RoundToInt((RunConfig.MultiplicadorCuracionExperiencia() - 1f) * 100f) + "%)" : "");
        return "Pasante " + RunConfig.nombreJugador + " · uniforme " + RunConfig.NombreColorUniforme() + " · " + exp + " · " + masc + " · " + guantes + " · Rasgo: " + RunConfig.NombreRasgo() + ".";
    }

    // Tiñe la malla del cuerpo del jugador con el color de uniforme elegido.
    void AplicarColorUniforme()
    {
        if (playerGameObject == null) return;
        var cuerpo = playerGameObject.transform.Find("Capsule Mesh");
        if (cuerpo == null) return;
        var rend = cuerpo.GetComponent<Renderer>();
        if (rend == null) return;
        // material (instancia) para no teñir el material compartido de otros objetos
        rend.material.color = RunConfig.ColorUniforme();
    }

    void GuardarPersonalizacionEnRunConfig()
    {
        if (nombreInput != null && !string.IsNullOrWhiteSpace(nombreInput.text))
            RunConfig.nombreJugador = nombreInput.text;

        if (colorGroup != null) RunConfig.colorUniformeIndex = colorGroup.indiceSeleccionado;
        if (mascarillaToggle != null) RunConfig.usaMascarilla = mascarillaToggle.valorActual;
        if (guantesToggle != null) RunConfig.usaGuantes = guantesToggle.valorActual;
        if (experienciaGroup != null) RunConfig.nivelExperiencia = (NivelExperiencia)experienciaGroup.indiceSeleccionado;
        if (rasgosGroup != null) RunConfig.rasgoElegido = (RasgoInicial)rasgosGroup.indiceSeleccionado;
    }

    public void TogglePausa()
    {
        if (!juegoActivo) return;

        pausado = !pausado;
        if (pausado)
        {
            // Pausa el "temporizador de la barra de vida del paciente": Time.timeScale = 0
            // congela cualquier Update/temporizador basado en Time.deltaTime del futuro sistema de pacientes.
            Time.timeScale = 0f;
            SetCursorState(false);
            SetPlayerControl(false);
            SetActivo(pauseMenuPanel, true);
            // Traer la pausa al frente: el HUD de gameplay se construye después
            // en la jerarquía del canvas y quedaría dibujado encima
            if (pauseMenuPanel != null)
                pauseMenuPanel.transform.SetAsLastSibling();
            Debug.Log("Juego pausado");
        }
        else
        {
            Time.timeScale = 1f;
            cursorLibre = false;
            SetCursorState(true);
            SetPlayerControl(true);
            SetActivo(pauseMenuPanel, false);
            Debug.Log("Juego reanudado");
        }
    }

    // Resetea el estado del turno actual (posición del jugador, contadores de dificultad)
    // sin salir al menú principal.
    public void ReiniciarTurno()
    {
        Debug.Log("Reiniciando turno actual...");

        if (GameManager.Instance != null)
            GameManager.Instance.ReiniciarTurno();


        if (DifficultyDirector.Instance != null)
        {
            DifficultyDirector.Instance.performanceScore = 0.5f;
            DifficultyDirector.Instance.diaActual = 1;
            DifficultyDirector.Instance.pacientesSalvados = 0;
            DifficultyDirector.Instance.pacientesPerdidos = 0;
        }

        RestaurarJugadorAPosicionInicial();

        Time.timeScale = 1f;
        pausado = false;
        SetActivo(pauseMenuPanel, false);
        SetCursorState(true);
        SetPlayerControl(true);
    }

    // Destruye la sesión de juego activa (vuelve al Menú Principal, resetea RunConfig).
    public void SalirAlMenuPrincipal()
    {
        Debug.Log("Saliendo al menú principal, sesión de turno destruida.");

        // Guardar la puntuación de esta partida ANTES de resetear RunConfig
        GuardarPuntuacionDeLaPartida();

        if (GameManager.Instance != null)
            GameManager.Instance.TerminarTurno();


        Time.timeScale = 1f;
        juegoActivo = false;
        pausado = false;
        SetActivo(pauseMenuPanel, false);

        SetPlayerControl(false);
        SetCursorState(false);
        RestaurarJugadorAPosicionInicial();
        RunConfig.Resetear();

        MostrarSolo(PantallaMenu.MenuPrincipal);
    }

    // Registra la partida recién terminada en la tabla de puntuaciones.
    void GuardarPuntuacionDeLaPartida()
    {
        int dias = GameManager.Instance != null ? GameManager.Instance.daysSurvived : 0;
        int salvados = DifficultyDirector.Instance != null ? DifficultyDirector.Instance.pacientesSalvados : 0;
        ScoreManager.GuardarScore(RunConfig.nombreJugador, dias, salvados);
    }

    void RestaurarJugadorAPosicionInicial()
    {
        if (playerGameObject == null) return;
        var rb = playerGameObject.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;
        playerGameObject.transform.SetPositionAndRotation(posicionInicialJugador, rotacionInicialJugador);
    }

    void SetCursorState(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void SetPlayerControl(bool activo)
    {
        if (playerLook != null)
        {
            playerLook.enabled = activo;   // reactivar el componente (por si un Tab-atender lo dejó apagado)
            playerLook.SetControlEnabled(activo);
        }
        if (playerMovement != null) playerMovement.SetControlEnabled(activo);
    }
}
