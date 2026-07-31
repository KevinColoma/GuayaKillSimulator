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

    [Header("Créditos")]
    public GameObject creditosPanel;

    [Header("Game Over")]
    public GameObject gameOverPanel;

    [Header("Pausa")]
    public Button reanudarButton;
    public Button reiniciarButton;
    public Button salirAlMenuButton;
    public Button ajustesPausaButton;

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

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver += IrAGameOver;

        CrearGameOverPanel();
        CrearCreditosPanel();
        WireButtons();
        SetMenuBackground();
        SetPlayerControl(false);
        SetCursorState(false);
        SetActivo(pauseMenuPanel, false);
        MostrarSolo(PantallaMenu.Splash);
    }

    void CrearGameOverPanel()
    {
        if (gameOverPanel != null) return;
        var canvas = GameObject.Find("MenuCanvas");
        if (canvas == null) return;
        gameOverPanel = new GameObject("GameOverPanel", typeof(RectTransform));
        gameOverPanel.transform.SetParent(canvas.transform, false);
        var bg = gameOverPanel.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.08f, 0.02f, 0.02f, 0.92f);
        var rt = gameOverPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var txo = new GameObject("GameOverText", typeof(RectTransform));
        txo.transform.SetParent(gameOverPanel.transform, false);
        var trt = txo.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.08f, 0.2f);
        trt.anchorMax = new Vector2(0.92f, 0.8f);
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        var txt = txo.AddComponent<TMPro.TextMeshProUGUI>();
        txt.fontSize = 38;
        txt.alignment = TMPro.TextAlignmentOptions.Center;
        txt.color = new Color(1f, 0.3f, 0.2f, 1f);
        txt.text = "";
        gameOverPanel.SetActive(false);
        Debug.Log("[MenuFlow] gameOverPanel creado desde código");
    }

    void CrearCreditosPanel()
    {
        if (creditosPanel != null) return;
        var canvas = GameObject.Find("MenuCanvas");
        if (canvas == null) return;
        creditosPanel = new GameObject("CreditosPanel", typeof(RectTransform));
        creditosPanel.transform.SetParent(canvas.transform, false);
        var bg = creditosPanel.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.94f);
        var rt = creditosPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var txo = new GameObject("CreditosText", typeof(RectTransform));
        txo.transform.SetParent(creditosPanel.transform, false);
        var trt = txo.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.08f, 0.3f);
        trt.anchorMax = new Vector2(0.92f, 0.7f);
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        var txt = txo.AddComponent<TMPro.TextMeshProUGUI>();
        txt.fontSize = 36;
        txt.alignment = TMPro.TextAlignmentOptions.Center;
        txt.color = new Color(1f, 0.85f, 0.2f, 1f);
        txt.text = "Este juego fue hecho con amor compresion y ternura! \n\nGRUPO 8\n\nKevin Coloma\nJeffrey Manobanda\nJhordy Marcillo\nMoreira Erick\n\nPresiona ESC o haz clic para cerrar";

        // Clic en cualquier parte cierra
        creditosPanel.AddComponent<Button>().onClick.AddListener(OcultarCreditos);
        creditosPanel.SetActive(false);
    }

    public void MostrarCreditos()
    {
        CrearCreditosPanel();
        SetActivo(creditosPanel, true);
        if (creditosPanel != null) creditosPanel.transform.SetAsLastSibling();
    }

    public void OcultarCreditos()
    {
        SetActivo(creditosPanel, false);
    }

    void Update()
    {
        if (creditosPanel != null && creditosPanel.activeSelf && InputCompat.IsKeyDown(KeyCode.Escape))
        {
            OcultarCreditos();
            return;
        }
        if (gameOverPanel != null && gameOverPanel.activeSelf)
        {
            if (InputCompat.IsKeyDown(KeyCode.Space) || InputCompat.IsKeyDown(KeyCode.Escape))
            {
                SalirAlMenuPrincipal();
            }
            return;
        }
        if (juegoActivo && InputCompat.IsKeyDown(KeyCode.Escape))
        {
            TogglePausa();
        }
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
        if (ajustesPausaButton == null && pauseMenuPanel != null) {
            // transform.Find sí encuentra hijos inactivos (el panel de pausa arranca oculto)
            var t = pauseMenuPanel.transform.Find("PauseAjustesButton");
            if (t != null) ajustesPausaButton = t.GetComponent<Button>();
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
        WireButton(ajustesVolverButton, VolverDesdeAjustes);
        WireButton(ajustesPausaButton, IrAAjustesDesdePausa);
        WireButton(logrosVolverButton, IrAMenuPrincipal);
        WireButton(avatarVolverButton, IrAMenuPrincipal);
        WireButton(avatarConfirmarButton, ConfirmarPersonalizacionEIniciarGameplay);
        WireButton(reanudarButton, TogglePausa);
        WireButton(reiniciarButton, ReiniciarTurno);
        WireButton(salirAlMenuButton, SalirAlMenuPrincipal);

        // Botón Créditos en esquina inferior derecha del MainMenuPanel
        if (mainMenuPanel != null)
        {
            var btnGO = new GameObject("CreditosButton", typeof(RectTransform));
            btnGO.transform.SetParent(mainMenuPanel.transform, false);
            var btnRt = btnGO.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.78f, 0.02f);
            btnRt.anchorMax = new Vector2(0.98f, 0.09f);
            btnRt.offsetMin = Vector2.zero; btnRt.offsetMax = Vector2.zero;
            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnGO.AddComponent<UnityEngine.UI.Image>();
            btn.targetGraphic.color = new Color(0.8f, 0.7f, 0.1f, 0.85f);
            var btnTxtGO = new GameObject("Text", typeof(RectTransform));
            btnTxtGO.transform.SetParent(btnGO.transform, false);
            var btnTxtRt = btnTxtGO.GetComponent<RectTransform>();
            btnTxtRt.anchorMin = Vector2.zero; btnTxtRt.anchorMax = Vector2.one;
            btnTxtRt.offsetMin = Vector2.zero; btnTxtRt.offsetMax = Vector2.zero;
            var btnTxt = btnTxtGO.AddComponent<TMPro.TextMeshProUGUI>();
            btnTxt.text = "CRÉDITOS";
            btnTxt.fontSize = 28;
            btnTxt.alignment = TMPro.TextAlignmentOptions.Center;
            btnTxt.color = Color.white;
            btn.onClick.AddListener(MostrarCreditos);
        }
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
        if (gameOverPanel != null) SetActivo(gameOverPanel, false);
        if (creditosPanel != null) SetActivo(creditosPanel, false);
    }

    void SetActivo(GameObject go, bool activo)
    {
        if (go != null) go.SetActive(activo);
    }

    void SetMenuBackground()
    {
        var tex = Resources.Load<Texture2D>("MenuBackground");
        if (tex == null) return;
        AddBgToPanel(splashPanel, tex);
        AddBgToPanel(mainMenuPanel, tex);
    }

    void AddBgToPanel(GameObject panel, Texture2D tex)
    {
        if (panel == null) return;
        var bgGO = new GameObject("BgImage", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
        bgGO.transform.SetParent(panel.transform, false);
        var rt = bgGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var ri = bgGO.GetComponent<UnityEngine.UI.RawImage>();
        ri.texture = tex;
        bgGO.transform.SetAsFirstSibling();
    }

    public void IrAMenuPrincipal() => MostrarSolo(PantallaMenu.MenuPrincipal);

    public void IrAAjustes()
    {
        ajustesDesdePausa = false;
        MostrarSolo(PantallaMenu.Ajustes);
        WireAudioSliders();
    }

    // Ajustes abiertos DURANTE la partida (desde el menú de pausa): permite afinar la
    // sensibilidad y el audio en caliente y volver al juego para probarlo al instante.
    bool ajustesDesdePausa = false;

    public void IrAAjustesDesdePausa()
    {
        ajustesDesdePausa = true;
        SetActivo(pauseMenuPanel, false);
        SetActivo(settingsPanel, true);
        if (settingsPanel != null) settingsPanel.transform.SetAsLastSibling();
        WireAudioSliders();
    }

    // El botón "Volver" de Ajustes regresa a donde estabas: pausa o menú principal.
    public void VolverDesdeAjustes()
    {
        if (ajustesDesdePausa)
        {
            ajustesDesdePausa = false;
            SetActivo(settingsPanel, false);
            SetActivo(pauseMenuPanel, true);
            if (pauseMenuPanel != null) pauseMenuPanel.transform.SetAsLastSibling();
            return;
        }
        IrAMenuPrincipal();
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
        AgrandarTextosAvatar();
    }

    void AgrandarTextosAvatar()
    {
        if (avatarPanel == null) return;
        var textos = avatarPanel.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
        foreach (var t in textos)
        {
            if (t.fontSize < 28f)
                t.fontSize = Mathf.Max(28f, t.fontSize * 1.4f);
        }
    }

    // ------------------------- Ajustes: volúmenes y sensibilidad -------------------------
    Slider volumeSlider, sfxSlider, musicSlider, sensibilidadSlider;
    bool audioWired = false;

    // Rango de sensibilidad del mouse ofrecido en Ajustes
    const float SensMin = 0.05f;
    const float SensMax = 4f;

    void WireAudioSliders()
    {
        if (audioWired) return;

        // Sensibilidad del mouse -> FirstPersonLook (se aplica en vivo y se guarda)
        if (sensibilidadSlider == null)
        {
            var go = GameObject.Find("MenuCanvas/SettingsPanel/SensibilidadSlider");
            if (go != null) sensibilidadSlider = go.GetComponent<Slider>();
        }
        if (sensibilidadSlider != null)
        {
            sensibilidadSlider.minValue = SensMin;
            sensibilidadSlider.maxValue = SensMax;
            float actual = playerLook != null
                ? playerLook.sensitivity
                : PlayerPrefs.GetFloat(FirstPersonLook.PrefSensibilidad, 1.2f);
            sensibilidadSlider.value = Mathf.Clamp(actual, SensMin, SensMax);
            sensibilidadSlider.onValueChanged.AddListener(SetSensibilidad);
        }

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

        // Lecturas numéricas a la derecha de cada slider (feedback inmediato del valor)
        AdjuntarValorLabel(volumeSlider, v => Mathf.RoundToInt(v * 100f) + "%");
        AdjuntarValorLabel(sfxSlider, v => Mathf.RoundToInt(v * 100f) + "%");
        AdjuntarValorLabel(musicSlider, v => Mathf.RoundToInt(v * 100f) + "%");
        AdjuntarValorLabel(sensibilidadSlider, v => v.ToString("0.0") + "x");

        if (volumeSlider != null && sfxSlider != null && musicSlider != null && sensibilidadSlider != null)
            audioWired = true;
    }

    // Crea (una sola vez) una etiqueta con el valor actual del slider y la mantiene al día.
    void AdjuntarValorLabel(Slider slider, System.Func<float, string> formato)
    {
        if (slider == null) return;
        string nombre = slider.name + "_Valor";
        if (slider.transform.parent.Find(nombre) != null) return; // ya existe

        var sliderRT = slider.GetComponent<RectTransform>();
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(slider.transform.parent, false);
        var rt = go.GetComponent<RectTransform>();
        // Misma franja vertical que el slider, pegado a su derecha
        rt.anchorMin = new Vector2(0.905f, sliderRT.anchorMin.y);
        rt.anchorMax = new Vector2(0.99f, sliderRT.anchorMax.y);
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.raycastTarget = false;
        txt.fontSize = 26;
        txt.alignment = TextAlignmentOptions.MidlineLeft;
        txt.color = Color.white;
        txt.text = formato(slider.value);

        slider.onValueChanged.AddListener(v => txt.text = formato(v));
    }

    public void SetVolumenGeneral(float valor)
    {
        AudioListener.volume = valor;
    }

    // Aplica la sensibilidad del mouse en vivo (y la persiste vía FirstPersonLook).
    public void SetSensibilidad(float valor)
    {
        if (playerLook == null && playerGameObject != null)
            playerLook = playerGameObject.GetComponentInChildren<FirstPersonLook>();

        if (playerLook != null) playerLook.SetSensibilidad(valor);
        else PlayerPrefs.SetFloat(FirstPersonLook.PrefSensibilidad, valor);
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
        SetActivo(gameOverPanel, false);

        SetPlayerControl(true);
        SetCursorState(true);
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
            UIManager.Instance.MostrarNarrador("Controles: WASD moverte · teclas 1-7 herramientas · Mira modelos 3D en quirófanos y presiona E para usarlos · Esc pausa.", 8f);
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
        SetActivo(gameOverPanel, false);

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

    // Si el jugador cierra el juego a mitad de un turno (sin pasar por "Salir al menú"),
    // su partida igual queda registrada en la tabla de puntuaciones.
    void OnApplicationQuit()
    {
        if (juegoActivo)
            GuardarPuntuacionDeLaPartida();
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

    public void IrAGameOver()
    {
        Debug.Log("[MenuFlow] IrAGameOver INVOCADO. gameOverPanel=" + (gameOverPanel != null ? gameOverPanel.name : "null"));
        juegoActivo = false;
        pausado = false;
        Time.timeScale = 0f;

        SetPlayerControl(false);
        SetCursorState(false);
        SetActivo(pauseMenuPanel, false);

        CrearGameOverPanel();

        Debug.Log("[MenuFlow] Activando gameOverPanel...");
        SetActivo(gameOverPanel, true);
        if (gameOverPanel != null)
        {
            gameOverPanel.transform.SetAsLastSibling();
            var txt = gameOverPanel.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (txt != null)
            {
                int dias = GameManager.Instance != null ? GameManager.Instance.daysSurvived : 0;
                int salvados = DifficultyDirector.Instance != null ? DifficultyDirector.Instance.pacientesSalvados : 0;
                txt.text = "☠ GAME OVER ☠\n\nPerdiste 4 pacientes seguidos.\n\nDías sobrevividos: " + dias + "\nPacientes salvados: " + salvados + "\n\nPresiona ESPACIO o ESC para volver al menú.";
            }
        }

        Debug.Log("[MenuFlow] IrAGameOver completado. gameOverPanel=" + (gameOverPanel != null ? gameOverPanel.gameObject.name : "null") + " active=" + (gameOverPanel != null ? gameOverPanel.activeSelf.ToString() : "N/A"));
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
