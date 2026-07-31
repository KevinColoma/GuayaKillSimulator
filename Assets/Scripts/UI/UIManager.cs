using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

// HUD del gameplay (Sala de Urgencias). Se CONSTRUYE por código al iniciar
// (cero wiring manual en el Inspector) colgado del canvas principal.
// Dos paneles según el documento de diseño:
//  - Panel de Paciente: cronómetro, vida, herida; verde -> amarillo -> rojo.
//  - Panel de Decisiones Rápidas: botones de herramientas con cantidades.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Tooltip("Canvas raíz donde se construye el HUD. Si es null, busca 'MenuCanvas'.")]
    public Canvas canvasRaiz;

    GameObject hudRoot;
    TextMeshProUGUI pacienteInfo;
    TextMeshProUGUI cronometro;
    TextMeshProUGUI diasContador;
    TextMeshProUGUI narrador;
    Image barraVidaFill;
    TextMeshProUGUI hpLabel;
    // Segundo panel: el OTRO paciente (para triaje con 2 camillas)
    GameObject panelPaciente2;
    TextMeshProUGUI pacienteInfo2;
    TextMeshProUGUI cronometro2;
    Image barraVidaFill2;
    TextMeshProUGUI hpLabel2;
    readonly Dictionary<string, TextMeshProUGUI> etiquetasHerramientas = new Dictionary<string, TextMeshProUGUI>();
    string[] toolbarOrden;
    float narradorHasta;
    float proximoComentarioIdle;

    static readonly string[] ComentariosInicioDia = new string[]
    {
        "Día {0}. Otro turno, otro milagro con guantes de látex.",
        "Comienza el día {0}. Que la suerte y el alcohol en gel te acompañen.",
        "Día {0}: menos pacientes que ayer no significa menos caos.",
        "Arranca el día {0}. Guayakill nunca duerme, tú tampoco deberías.",
        "Día {0}. Respira hondo. El olor a alcohol ya no se te va a quitar.",
    };

    static readonly string[] ComentariosIdle = new string[]
    {
        "Recuerda: el alcohol es para el paciente. A veces.",
        "Si dudas entre dos herramientas, prueba la Oración. Es gratis.",
        "El machete no perdona, pero tú sí puedes intentarlo.",
        "Un paciente feliz es un paciente que sigue respirando.",
        "En Guayakill, la ambulancia también llega tarde.",
        "Lávate las manos. O no. Total, ya es tarde.",
        "La ética médica es opcional, el tiempo no.",
        "Si algo sale mal, siempre puedes culpar al Director de Dificultad.",
        "Cada paciente es una nueva oportunidad de fallar con estilo.",
        "Tip: correr en círculos no cura a nadie, pero desestresa.",
        "El torniquete no es un accesorio de moda, doc.",
        "Nadie dijo que ser médico de barrio fuera glamoroso.",
    };

    static readonly Color VERDE = new Color(0.15f, 0.95f, 0.35f);
    static readonly Color AMARILLO = new Color(1f, 0.8f, 0f);
    static readonly Color ROJO = new Color(1f, 0.2f, 0.15f);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (canvasRaiz == null)
        {
            var go = GameObject.Find("MenuCanvas");
            if (go != null) canvasRaiz = go.GetComponent<Canvas>();
        }

        ConstruirHUD();
        hudRoot.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += estado => hudRoot.SetActive(estado == GameState.EnTurno);
            GameManager.Instance.OnDayEnded += dias => MostrarNarrador("Día completado. Sobreviviste " + dias + " día(s). Nadie te va a dar una medalla, pero igual.");
            GameManager.Instance.OnDayStarted += OnDayStarted;
        }
        if (PatientManager.Instance != null)
        {
            PatientManager.Instance.OnPacienteLlega += OnPacienteLlega;
            PatientManager.Instance.OnPacienteResuelto += OnPacienteResuelto;
        }
        if (MedicalToolsManager.Instance != null)
        {
            MedicalToolsManager.Instance.OnInventarioCambiado += ActualizarEtiquetasHerramientas;
            MedicalToolsManager.Instance.OnHerramientaUsada += (h, exito, msg) => MostrarNarrador(msg);
        }
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.OnLogroDesbloqueado += OnLogroDesbloqueado;
        }

        ProgramarProximoComentarioIdle();
    }

    void Update()
    {
        // Contador de días siempre fresco y limpieza del narrador
        if (hudRoot != null && hudRoot.activeSelf)
        {
            if (GameManager.Instance != null && diasContador != null)
            {
                int dia = DifficultyDirector.Instance != null ? DifficultyDirector.Instance.diaActual : 1;
                diasContador.text = "Día " + dia + "  |  Sobrevividos: " + GameManager.Instance.daysSurvived;
            }
            if (narrador != null && Time.unscaledTime > narradorHasta && !string.IsNullOrEmpty(narrador.text))
                narrador.text = "";

            // Comentarios ambientales aleatorios cuando el narrador está libre (no pisa otros
            // mensajes, ni aparece con el juego pausado o un minijuego en curso)
            if (narrador != null && string.IsNullOrEmpty(narrador.text) && Time.unscaledTime > proximoComentarioIdle
                && Time.timeScale > 0f
                && (MiniGameManager.Instance == null || !MiniGameManager.Instance.EnCurso))
            {
                MostrarNarrador(ComentariosIdle[Random.Range(0, ComentariosIdle.Length)], 5f);
                ProgramarProximoComentarioIdle();
            }

            PintarPaneles();

            // Atajos de teclado 1-7 para usar herramientas (el cursor está bloqueado por el mouse-look)
            var kb = Keyboard.current;
            if (kb != null && MedicalToolsManager.Instance != null && toolbarOrden != null)
            {
                bool enCurso = MiniGameManager.Instance != null && MiniGameManager.Instance.EnCurso;
                for (int i = 0; i < toolbarOrden.Length && i < 7; i++)
                {
                    if (!TeclaNumero(kb, i + 1)) continue;
                    // Oración (índice 6/tecla 7) funciona incluso durante minijuegos
                    if (enCurso && toolbarOrden[i] != "Oración") continue;
                    MedicalToolsManager.Instance.UseTool(toolbarOrden[i]);
                }
            }
        }
    }

    bool TeclaNumero(Keyboard kb, int n)
    {
        switch (n)
        {
            case 1: return kb.digit1Key.wasPressedThisFrame;
            case 2: return kb.digit2Key.wasPressedThisFrame;
            case 3: return kb.digit3Key.wasPressedThisFrame;
            case 4: return kb.digit4Key.wasPressedThisFrame;
            case 5: return kb.digit5Key.wasPressedThisFrame;
            case 6: return kb.digit6Key.wasPressedThisFrame;
            case 7: return kb.digit7Key.wasPressedThisFrame;
            default: return false;
        }
    }

    // ------------------------- Eventos -------------------------

    void OnPacienteLlega(Patient p)
    {
        MostrarNarrador("\"" + p.dialogoAbsurdo + "\"", 6f);
    }

    void OnPacienteResuelto(Patient p, bool salvado)
    {
        if (salvado) MostrarNarrador("Milagro, doc. " + p.nombre + " vive. ¿Se ganó el bingo?", 5f);
        else MostrarNarrador(p.nombre + " no lo logró. En Guayakill hasta el machete tiene más experiencia.", 5f);
    }

    void OnDayStarted(int dia)
    {
        string plantilla = ComentariosInicioDia[Random.Range(0, ComentariosInicioDia.Length)];
        MostrarNarrador(string.Format(plantilla, dia), 5f);
    }

    void OnLogroDesbloqueado(AchievementManager.Achievement a)
    {
        MostrarNarrador("Logro desbloqueado: \"" + a.title + "\". Avísale a tu mamá.", 6f);
    }

    void ProgramarProximoComentarioIdle()
    {
        proximoComentarioIdle = Time.unscaledTime + Random.Range(25f, 40f);
    }

    // Sondea los slots cada frame: panel principal = paciente ENFOCADO, panel 2 = el otro.
    void PintarPaneles()
    {
        var pm = PatientManager.Instance;
        if (pm == null || pacienteInfo == null) return;

        var enfocado = pm.slotEnfocado;

        if (enfocado != null && enfocado.paciente != null)
        {
            PintarSlot(enfocado, pacienteInfo, cronometro, barraVidaFill, true, hpLabel);
        }
        else
        {
            int enTratamiento = pm.slots.FindAll(s => s.EnTratamiento).Count;
            pacienteInfo.text = enTratamiento > 0
                ? "Acércate a una camilla para atender.\n(" + enTratamiento + " paciente(s) en tratamiento)"
                : "Esperando pacientes...";
            if (cronometro != null) cronometro.text = "";
            if (barraVidaFill != null) barraVidaFill.fillAmount = 0f;
        }

        // Panel secundario: el otro slot en tratamiento distinto al enfocado
        PatientManager.Slot otro = null;
        foreach (var s in pm.slots)
            if (s.EnTratamiento && s != enfocado) { otro = s; break; }

        if (panelPaciente2 != null) panelPaciente2.SetActive(otro != null);
        if (otro != null)
            PintarSlot(otro, pacienteInfo2, cronometro2, barraVidaFill2, false, hpLabel2);
    }

    void PintarSlot(PatientManager.Slot slot, TextMeshProUGUI info, TextMeshProUGUI crono, Image barra, bool completo, TextMeshProUGUI hp = null)
    {
        var p = slot.paciente;
        float pct = p.health / p.maxHealth;

        if (completo)
            info.text = p.nombre + "\n" + p.diagnostico + "\nCura con: " + p.HerramientaCorrecta();
        else
            info.text = p.nombre + " (" + p.HerramientaCorrecta() + ")";

        if (crono != null)
        {
            int restante = Mathf.CeilToInt(Mathf.Max(0f, slot.tiempoRestante));
            // Proporción restante sobre el límite REAL de este paciente (varía 25-59s según
            // la dificultad vigente): el umbral de alerta se adapta al límite, no es un
            // número fijo de segundos, para que "poco tiempo" signifique lo mismo siempre.
            float fraccion = slot.tiempoLimite > 0f ? slot.tiempoRestante / slot.tiempoLimite : 0f;

            crono.text = completo ? (restante + "s / " + Mathf.CeilToInt(slot.tiempoLimite) + "s") : (restante + "s");
            crono.color = fraccion < 0.2f ? ROJO : (fraccion < 0.45f ? AMARILLO : Color.white);
        }
        if (barra != null)
        {
            barra.fillAmount = pct;
            Color baseColor = pct < 0.5f
                ? Color.Lerp(ROJO, AMARILLO, pct * 2f)
                : Color.Lerp(AMARILLO, VERDE, (pct - 0.5f) * 2f);
            if (pct < 0.25f)
            {
                float pulso = Mathf.Sin(Time.unscaledTime * 10f) * 0.35f + 0.65f;
                baseColor.a = pulso;
            }
            barra.color = baseColor;
        }
        if (hp != null)
            hp.text = Mathf.CeilToInt(p.health) + "/" + Mathf.CeilToInt(p.maxHealth);
    }

    void ActualizarEtiquetasHerramientas()
    {
        var tools = MedicalToolsManager.Instance;
        if (tools == null) return;
        foreach (var t in tools.herramientas)
        {
            if (!etiquetasHerramientas.ContainsKey(t.nombre)) continue;
            bool agotado = !t.reutilizable && t.cantidad <= 0 && t.nombre != "Oración";
            string cant = t.nombre == "Oración" ? "5%" : (t.reutilizable ? "∞" : "rest: " + t.cantidad.ToString());
            int idx = toolbarOrden != null ? System.Array.IndexOf(toolbarOrden, t.nombre) : -1;
            string tecla = idx >= 0 ? "<size=110%>[" + (idx + 1) + "]</size> " : "";
            var label = etiquetasHerramientas[t.nombre];
            label.text = tecla + t.nombre + "\n<size=85%>" + cant + "</size>";
            label.color = agotado ? Color.red : new Color(1f, 1f, 1f, 0.9f);
            var bg = label.transform.parent.GetComponent<UnityEngine.UI.Image>();
            if (bg != null)
                bg.color = agotado ? new Color(0.6f, 0.1f, 0.1f, 0.7f)
                    : t.nombre == "Oración" ? new Color(0.8f, 0.7f, 0.1f, 0.7f)
                    : new Color(0.16f, 0.45f, 0.7f, 0.6f);
        }
    }

    public void MostrarNarrador(string mensaje, float segundos = 4f)
    {
        if (narrador == null) return;
        narrador.text = mensaje;
        narradorHasta = Time.unscaledTime + segundos;
    }

    // ------------------------- Construcción del HUD -------------------------

    void ConstruirHUD()
    {
        hudRoot = CrearPanel("GameplayHUD", canvasRaiz.transform, Vector2.zero, Vector2.one, new Color(0, 0, 0, 0));
        // El fondo transparente del HUD NO debe bloquear clics de otros paneles (ej. menú de pausa)
        hudRoot.GetComponent<Image>().raycastTarget = false;

        // Panel de Paciente (esquina superior derecha)
        var panelPaciente = CrearPanel("PatientPanel", hudRoot.transform, new Vector2(0.62f, 0.72f), new Vector2(0.98f, 0.97f), new Color(0.05f, 0.05f, 0.08f, 0.85f));
        pacienteInfo = CrearTexto("PacienteInfo", panelPaciente.transform, new Vector2(0.04f, 0.35f), new Vector2(0.7f, 0.96f), 26, TextAlignmentOptions.TopLeft);
        pacienteInfo.text = "Esperando al próximo paciente...";
        cronometro = CrearTexto("Cronometro", panelPaciente.transform, new Vector2(0.72f, 0.5f), new Vector2(0.98f, 0.95f), 48, TextAlignmentOptions.Center);

        // Barra de vida del paciente
        var barraFondo = CrearPanel("VidaFondo", panelPaciente.transform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.26f), new Color(0.25f, 0.25f, 0.3f, 1f));
        var fillGO = CrearPanel("VidaFill", barraFondo.transform, Vector2.zero, Vector2.one, VERDE);
        barraVidaFill = fillGO.GetComponent<Image>();
        barraVidaFill.type = Image.Type.Filled;
        barraVidaFill.fillMethod = Image.FillMethod.Horizontal;
        barraVidaFill.fillAmount = 0f;
        hpLabel = CrearTexto("HpLabel", barraFondo.transform, Vector2.zero, Vector2.one, 22, TextAlignmentOptions.Center);
        hpLabel.color = Color.white;

        // Segundo panel de paciente (la OTRA camilla), más pequeño, debajo del primero
        panelPaciente2 = CrearPanel("PatientPanel2", hudRoot.transform, new Vector2(0.62f, 0.58f), new Vector2(0.98f, 0.71f), new Color(0.05f, 0.05f, 0.08f, 0.7f));
        pacienteInfo2 = CrearTexto("PacienteInfo2", panelPaciente2.transform, new Vector2(0.04f, 0.45f), new Vector2(0.72f, 0.95f), 20, TextAlignmentOptions.MidlineLeft);
        cronometro2 = CrearTexto("Cronometro2", panelPaciente2.transform, new Vector2(0.74f, 0.45f), new Vector2(0.98f, 0.95f), 30, TextAlignmentOptions.Center);
        var barraFondo2 = CrearPanel("VidaFondo2", panelPaciente2.transform, new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.38f), new Color(0.25f, 0.25f, 0.3f, 1f));
        var fillGO2 = CrearPanel("VidaFill2", barraFondo2.transform, Vector2.zero, Vector2.one, VERDE);
        barraVidaFill2 = fillGO2.GetComponent<Image>();
        barraVidaFill2.type = Image.Type.Filled;
        barraVidaFill2.fillMethod = Image.FillMethod.Horizontal;
        barraVidaFill2.fillAmount = 0f;
        hpLabel2 = CrearTexto("HpLabel2", barraFondo2.transform, Vector2.zero, Vector2.one, 16, TextAlignmentOptions.Center);
        hpLabel2.color = Color.white;
        panelPaciente2.SetActive(false);

        // Contador de días (arriba al centro)
        diasContador = CrearTexto("DiasContador", hudRoot.transform, new Vector2(0.3f, 0.93f), new Vector2(0.7f, 0.99f), 30, TextAlignmentOptions.Center);
        diasContador.color = AMARILLO;

        // Narrador (abajo al centro, sobre la barra de herramientas)
        narrador = CrearTexto("NarradorTexto", hudRoot.transform, new Vector2(0.1f, 0.16f), new Vector2(0.9f, 0.24f), 28, TextAlignmentOptions.Center);
        narrador.fontStyle = FontStyles.Italic;

        // Panel de Decisiones Rápidas: barra de herramientas (abajo, más pequeña, solo informativa)
        var toolbar = CrearPanel("Toolbar", hudRoot.transform, new Vector2(0.02f, 0.01f), new Vector2(0.98f, 0.13f), new Color(0.05f, 0.05f, 0.08f, 0.7f));
        toolbarOrden = new string[] { "Gasas", "Alcohol", "Pinzas", "Suturas", "Torniquete", "Kit", "Oración" };
        string[] orden = toolbarOrden;
        float ancho = 1f / orden.Length;
        for (int i = 0; i < orden.Length; i++)
        {
            string nombre = orden[i];
            var btnGO = CrearPanel("Tool_" + nombre, toolbar.transform,
                new Vector2(i * ancho + 0.004f, 0.05f), new Vector2((i + 1) * ancho - 0.004f, 0.95f),
                nombre == "Oración" ? new Color(0.8f, 0.7f, 0.1f, 0.7f) : new Color(0.16f, 0.45f, 0.7f, 0.6f));

            var label = CrearTexto("Label", btnGO.transform, new Vector2(0f, 0.0f), new Vector2(1f, 0.85f), 24, TextAlignmentOptions.Center);
            label.color = new Color(1f, 1f, 1f, 0.9f);
            etiquetasHerramientas[nombre] = label;
        }
        ActualizarEtiquetasHerramientas();
    }

    GameObject CrearPanel(string nombre, Transform padre, Vector2 anclaMin, Vector2 anclaMax, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(padre, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anclaMin; rt.anchorMax = anclaMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return go;
    }

    TextMeshProUGUI CrearTexto(string nombre, Transform padre, Vector2 anclaMin, Vector2 anclaMax, float tamano, TextAlignmentOptions alineacion)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anclaMin; rt.anchorMax = anclaMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.raycastTarget = false; // los textos del HUD nunca deben interceptar clics
        txt.fontSize = tamano;
        txt.alignment = alineacion;
        txt.text = "";
        return txt;
    }
}
