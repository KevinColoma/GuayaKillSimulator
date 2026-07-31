using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

// Minijuegos de precisión (Fase 4). Se disparan al usar la herramienta correcta
// sobre el paciente acostado. UI construida por código (mobile-first) e input por
// Pointer (mouse/touch/VR-ready, sin la clase Input legacy).
//   - Herida de bala    -> Extracción con pinzas (mantener firme sobre la bala)
//   - Apuñalamiento      -> Sutura (trazar la línea de puntos en orden)
//   - Accidente          -> Torniquete (acción rápida por tiempo)
public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }

    [System.Serializable]
    public class MiniGameResult
    {
        public bool success;
        public float damageIfFailed;
        public string failureMessage;
    }

    [Tooltip("Canvas donde se dibuja el minijuego. Si es null busca 'MenuCanvas'.")]
    public Canvas canvasRaiz;

    public static event System.Action OnMiniGameStarted;
    public static event System.Action OnMiniGameEnded;

    bool _enCurso;
    public bool EnCurso
    {
        get => _enCurso;
        private set
        {
            if (_enCurso == value) return;
            _enCurso = value;
            if (_enCurso) OnMiniGameStarted?.Invoke();
            else OnMiniGameEnded?.Invoke();
        }
    }

    GameObject overlay;

    // Aviso extra (banner superior) para el PRÓXIMO overlay: lo setea MedicalToolsManager
    // antes de lanzar el minijuego (ej. "PACIENTE DE ALTO RIESGO" o "COMPLICACIÓN 2/2").
    string bannerProximo;
    Color bannerProximoColor;

    public void AnunciarProximoProcedimiento(string texto, Color color)
    {
        bannerProximo = texto;
        bannerProximoColor = color;
    }

    // En INFERNAL todos los minijuegos son más frenéticos: los elementos se mueven
    // más rápido y hay menos tiempo para completarlos.
    bool EsInfernal => DifficultyDirector.Instance != null
        && DifficultyDirector.Instance.currentTier == DifficultyTier.Infernal;
    bool EsDificilOInfernal => DifficultyDirector.Instance != null
        && (DifficultyDirector.Instance.currentTier == DifficultyTier.Dificil
            || DifficultyDirector.Instance.currentTier == DifficultyTier.Infernal);
    float FactorVelocidad => EsInfernal ? 2.0f : 1f;  // movimiento/oscilación/temblor
    float FactorTiempo => EsInfernal ? 0.7f : 1f;     // límites de tiempo más cortos
    float FactorRango => EsInfernal ? 1.5f : 1f;      // rango de oscilación/drift de elementos

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
    }

    // Punto de entrada: MedicalToolsManager llama esto con el tipo de herida del paciente
    public void Jugar(TipoHerida tipo, Patient paciente, System.Action<MiniGameResult> onComplete)
    {
        if (EnCurso)
        {
            onComplete?.Invoke(new MiniGameResult { success = false, damageIfFailed = 0f, failureMessage = "Ya hay un procedimiento en curso." });
            return;
        }
        // En Difícil/Infernal, 40% de probabilidad de jugar Punzada en vez del minijuego específico
        if (EsDificilOInfernal && Random.value < 0.4f)
        {
            StartCoroutine(Punzada(paciente, onComplete));
            return;
        }
        switch (tipo)
        {
            case TipoHerida.Bala: StartCoroutine(ExtraccionBala(paciente, onComplete)); break;
            case TipoHerida.ArmaBlanca: StartCoroutine(Sutura(paciente, onComplete)); break;
            default: StartCoroutine(Torniquete(paciente, onComplete)); break;
        }
    }

    // Punto de entrada para las herramientas de apoyo (Gasas/Alcohol/Kit), que en
    // dificultad Difícil/Infernal también exigen un minijuego, sin importar el tipo de herida.
    public void JugarHerramienta(string nombreHerramienta, Patient paciente, System.Action<MiniGameResult> onComplete)
    {
        Debug.Log("[MINIGAME] JugarHerramienta llamado para " + nombreHerramienta + " EnCurso=" + EnCurso + " canvasRaiz=" + (canvasRaiz != null));
        if (EnCurso)
        {
            onComplete?.Invoke(new MiniGameResult { success = false, damageIfFailed = 0f, failureMessage = "Ya hay un procedimiento en curso." });
            return;
        }
        switch (nombreHerramienta)
        {
            case "Gasas": Debug.Log("[MINIGAME] Lanzando PresionSostenida"); StartCoroutine(PresionSostenida(paciente, onComplete)); break;
            case "Alcohol": Debug.Log("[MINIGAME] Lanzando LimpiezaHerida"); StartCoroutine(LimpiezaHerida(paciente, onComplete)); break;
            case "Kit": Debug.Log("[MINIGAME] Lanzando SecuenciaAuxilio"); StartCoroutine(SecuenciaAuxilio(paciente, onComplete)); break;
            default: onComplete?.Invoke(new MiniGameResult { success = true }); break;
        }
    }

    // ============================ MINIJUEGO 1: EXTRACCIÓN DE BALA ============================
    IEnumerator ExtraccionBala(Patient paciente, System.Action<MiniGameResult> onComplete)
    {
        EnCurso = true;
        var root = CrearOverlay("EXTRACCIÓN DE BALA", "Lleva las pinzas (verde) sobre la bala (amarillo) y mantenlas ahí hasta llenar la barra. Si te alejas, tiembla y sangra.");

        // Zona de juego
        var zona = CrearPanel("Zona", root.transform, new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.72f), new Color(0.1f, 0.1f, 0.13f, 0.9f));
        var zonaRT = zona.GetComponent<RectTransform>();

        // Bala (objetivo) en posición aleatoria dentro de la zona
        var bala = CrearPanel("Bala", zona.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.9f, 0.8f, 0.2f, 1f));
        var balaRT = bala.GetComponent<RectTransform>();
        balaRT.sizeDelta = new Vector2(70, 70);
        balaRT.anchoredPosition = new Vector2(Random.Range(-250f, 250f), Random.Range(-150f, 150f));

        // Pinzas (cursor)
        var pinzas = CrearPanel("Pinzas", zona.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.2f, 0.9f, 0.4f, 0.85f));
        var pinzasRT = pinzas.GetComponent<RectTransform>();
        pinzasRT.sizeDelta = new Vector2(48, 48);

        // Barra de progreso de extracción
        var progresoFondo = CrearPanel("ProgFondo", root.transform, new Vector2(0.3f, 0.16f), new Vector2(0.7f, 0.2f), new Color(0.2f, 0.2f, 0.25f, 1f));
        var progresoFill = CrearPanel("ProgFill", progresoFondo.transform, new Vector2(0, 0), new Vector2(0, 1), new Color(0.2f, 0.9f, 0.4f, 1f));
        var progFillRT = progresoFill.GetComponent<RectTransform>();

        // "Manos firmes" reduce el temblor; los guantes dan mejor agarre (menos temblor);
        // en Infernal tiembla más y hay menos tiempo
        float temblorBase = 26f * FactorVelocidad;
        if (RunConfig.rasgoElegido == RasgoInicial.ManosFirmes) temblorBase *= 0.45f;
        temblorBase *= RunConfig.FactorTemblorGuantes();

        float progreso = 0f;
        float tiempo = 0f;
        float limite = 9f * FactorTiempo;
        float sangradoExtra = 0f;
        MiniGameResult result = null;

        while (result == null)
        {
            // Congelar el minijuego mientras el juego está en pausa (Esc): los minijuegos
            // usan tiempo unscaled, así que sin esta guarda seguirían corriendo (y podrías
            // perder al paciente) con el menú de pausa encima.
            if (Time.timeScale == 0f) { yield return null; continue; }

            tiempo += Time.unscaledDeltaTime;

            // Posición del puntero -> local a la zona + temblor
            Vector2 local;
            Vector2 screen = Pointer.current != null ? Pointer.current.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(zonaRT, screen, null, out local);
            Vector2 temblor = new Vector2(
                Mathf.Sin(Time.unscaledTime * 13f) * temblorBase,
                Mathf.Cos(Time.unscaledTime * 17f) * temblorBase);
            pinzasRT.anchoredPosition = local + temblor;

            float dist = Vector2.Distance(pinzasRT.anchoredPosition, balaRT.anchoredPosition);
            bool sobreBala = dist < 55f;

            if (sobreBala)
            {
                progreso += Time.unscaledDeltaTime * 0.45f;
                pinzas.GetComponent<Image>().color = new Color(0.2f, 0.9f, 0.4f, 0.95f);
            }
            else
            {
                progreso -= Time.unscaledDeltaTime * 0.2f;
                sangradoExtra += Time.unscaledDeltaTime * 3f;   // penalización por temblar lejos
                pinzas.GetComponent<Image>().color = new Color(0.9f, 0.3f, 0.2f, 0.9f);
            }
            progreso = Mathf.Clamp01(progreso);
            progFillRT.anchorMax = new Vector2(progreso, 1f);

            if (progreso >= 1f)
                result = new MiniGameResult { success = true };
            else if (tiempo >= limite)
                result = new MiniGameResult { success = false, damageIfFailed = 15f + sangradoExtra, failureMessage = "Se te fue la mano, doc. Sangrado crítico." };

            yield return null;
        }

        CerrarOverlay(result.success ? "¡Bala extraída!" : result.failureMessage, result.success);
        yield return new WaitForSecondsRealtime(0.8f);
        LimpiarOverlay();
        EnCurso = false;
        onComplete?.Invoke(result);
    }

    // ============================ MINIJUEGO 2: SUTURA ============================
    IEnumerator Sutura(Patient paciente, System.Action<MiniGameResult> onComplete)
    {
        EnCurso = true;
        var root = CrearOverlay("SUTURA", "Toca los puntos EN ORDEN, de izquierda a derecha, mientras oscilan. Complétalos todos antes de que acabe el tiempo.");

        var zona = CrearPanel("Zona", root.transform, new Vector2(0.2f, 0.28f), new Vector2(0.8f, 0.72f), new Color(0.1f, 0.1f, 0.13f, 0.9f));
        var zonaRT = zona.GetComponent<RectTransform>();

        // Puntos de sutura en línea (con leve desvío para exigir precisión)
        int totalPuntos = 5;
        var puntos = new RectTransform[totalPuntos];
        var basePos = new Vector2[totalPuntos];
        for (int i = 0; i < totalPuntos; i++)
        {
            float t = i / (float)(totalPuntos - 1);
            var p = CrearPanel("Punto" + i, zona.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.7f, 0.7f, 0.8f, 1f));
            var prt = p.GetComponent<RectTransform>();
            prt.sizeDelta = new Vector2(46, 46);
            basePos[i] = new Vector2(Mathf.Lerp(-320f, 320f, t), Random.Range(-30f, 30f));
            prt.anchoredPosition = basePos[i];
            puntos[i] = prt;
        }

        var pinzas = CrearPanel("Aguja", zona.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.2f, 0.9f, 0.4f, 0.85f));
        var pinzasRT = pinzas.GetComponent<RectTransform>();
        pinzasRT.sizeDelta = new Vector2(40, 40);

        int siguiente = 0;
        float tiempo = 0f;
        float limite = 12f * FactorTiempo;
        float oscilacionVel = 3f * FactorVelocidad;
        float oscilacionAmp = 18f * FactorRango;
        MiniGameResult result = null;

        while (result == null)
        {
            // Congelar el minijuego mientras el juego está en pausa (Esc): los minijuegos
            // usan tiempo unscaled, así que sin esta guarda seguirían corriendo (y podrías
            // perder al paciente) con el menú de pausa encima.
            if (Time.timeScale == 0f) { yield return null; continue; }

            tiempo += Time.unscaledDeltaTime;

            // Distracción: el paciente se mueve (los puntos oscilan; en Infernal, más rápido y amplio)
            for (int i = 0; i < totalPuntos; i++)
                puntos[i].anchoredPosition = basePos[i] + new Vector2(0f, Mathf.Sin(Time.unscaledTime * oscilacionVel + i) * oscilacionAmp);

            Vector2 local;
            Vector2 screen = Pointer.current != null ? Pointer.current.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(zonaRT, screen, null, out local);
            pinzasRT.anchoredPosition = local;

            // ¿Tocó el siguiente punto en orden?
            if (siguiente < totalPuntos && Vector2.Distance(local, puntos[siguiente].anchoredPosition) < 40f)
            {
                puntos[siguiente].GetComponent<Image>().color = new Color(0.2f, 0.9f, 0.4f, 1f);
                siguiente++;
            }

            if (siguiente >= totalPuntos)
                result = new MiniGameResult { success = true };
            else if (tiempo >= limite)
                result = new MiniGameResult { success = false, damageIfFailed = 18f, failureMessage = "Sutura chueca. La herida se reabrió." };

            yield return null;
        }

        CerrarOverlay(result.success ? "¡Herida cerrada!" : result.failureMessage, result.success);
        yield return new WaitForSecondsRealtime(0.8f);
        LimpiarOverlay();
        EnCurso = false;
        onComplete?.Invoke(result);
    }

    // ============================ MINIJUEGO 3: TORNIQUETE ============================
    IEnumerator Torniquete(Patient paciente, System.Action<MiniGameResult> onComplete)
    {
        EnCurso = true;
        var root = CrearOverlay("TORNIQUETE", "Haz CLIC cuando el marcador pase por la zona verde. Acierta al menos 2 de 3 intentos.");

        // Barra horizontal con zona verde y marcador que rebota
        var barra = CrearPanel("Barra", root.transform, new Vector2(0.15f, 0.45f), new Vector2(0.85f, 0.55f), new Color(0.2f, 0.2f, 0.25f, 1f));
        var verde = CrearPanel("Verde", barra.transform, new Vector2(0.42f, 0f), new Vector2(0.58f, 1f), new Color(0.2f, 0.9f, 0.4f, 0.8f));
        var marcador = CrearPanel("Marcador", barra.transform, new Vector2(0f, 0f), new Vector2(0.02f, 1f), new Color(1f, 1f, 1f, 1f));
        var marcadorRT = marcador.GetComponent<RectTransform>();

        int aciertos = 0;
        int rondas = 3;
        int rondaActual = 0;
        float tiempo = 0f;
        float limite = 10f * FactorTiempo;
        float velocidad = 1.3f * FactorVelocidad;
        float pos = 0f;
        int dir = 1;
        bool presionAnterior = false;
        MiniGameResult result = null;

        while (result == null)
        {
            // Congelar el minijuego mientras el juego está en pausa (Esc): los minijuegos
            // usan tiempo unscaled, así que sin esta guarda seguirían corriendo (y podrías
            // perder al paciente) con el menú de pausa encima.
            if (Time.timeScale == 0f) { yield return null; continue; }

            tiempo += Time.unscaledDeltaTime;
            pos += dir * velocidad * Time.unscaledDeltaTime;
            if (pos >= 1f) { pos = 1f; dir = -1; }
            if (pos <= 0f) { pos = 0f; dir = 1; }
            marcadorRT.anchorMin = new Vector2(pos * 0.98f, 0f);
            marcadorRT.anchorMax = new Vector2(pos * 0.98f + 0.02f, 1f);

            bool presion = Pointer.current != null && Pointer.current.press.isPressed;
            if (presion && !presionAnterior)
            {
                bool enVerde = pos >= 0.42f && pos <= 0.58f;
                if (enVerde) { aciertos++; velocidad += 0.35f; }
                rondaActual++;
                if (rondaActual >= rondas)
                    result = new MiniGameResult
                    {
                        success = aciertos >= 2,
                        damageIfFailed = 16f,
                        failureMessage = "Torniquete flojo. Siguió sangrando."
                    };
            }
            presionAnterior = presion;

            if (tiempo >= limite && result == null)
                result = new MiniGameResult { success = aciertos >= 2, damageIfFailed = 16f, failureMessage = "Muy lento con el torniquete." };

            yield return null;
        }

        CerrarOverlay(result.success ? "¡Hemorragia controlada!" : result.failureMessage, result.success);
        yield return new WaitForSecondsRealtime(0.8f);
        LimpiarOverlay();
        EnCurso = false;
        onComplete?.Invoke(result);
    }

    // ============================ MINIJUEGO 4: PRESIÓN SOSTENIDA (Gasas) ============================
    // Solo se dispara en dificultad Difícil/Infernal. La zona de la herida se mueve
    // lentamente; hay que perseguirla con la gasa para mantener la presión.
    IEnumerator PresionSostenida(Patient paciente, System.Action<MiniGameResult> onComplete)
    {
        Debug.Log("[MINIGAME] PresionSostenida INICIADO");
        EnCurso = true;
        Debug.Log("[MINIGAME] EnCurso=" + EnCurso + " canvasRaiz=" + (canvasRaiz != null) + " canvasRaiz.name=" + (canvasRaiz != null ? canvasRaiz.name : "null"));
        var root = CrearOverlay("PRESIÓN SOSTENIDA", "Persigue la herida (rojo) con la gasa y mantén el contacto para llenar la barra antes de que acabe el tiempo.");

        var zona = CrearPanel("Zona", root.transform, new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.72f), new Color(0.1f, 0.1f, 0.13f, 0.9f));
        var zonaRT = zona.GetComponent<RectTransform>();

        var objetivo = CrearPanel("Objetivo", zona.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.85f, 0.3f, 0.3f, 1f));
        var objetivoRT = objetivo.GetComponent<RectTransform>();
        objetivoRT.sizeDelta = new Vector2(80, 80);

        var gasa = CrearPanel("Gasa", zona.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.9f, 0.9f, 0.85f, 0.9f));
        var gasaRT = gasa.GetComponent<RectTransform>();
        gasaRT.sizeDelta = new Vector2(46, 46);

        var progresoFondo = CrearPanel("ProgFondo", root.transform, new Vector2(0.3f, 0.16f), new Vector2(0.7f, 0.2f), new Color(0.2f, 0.2f, 0.25f, 1f));
        var progresoFill = CrearPanel("ProgFill", progresoFondo.transform, new Vector2(0, 0), new Vector2(0, 1), new Color(0.85f, 0.3f, 0.3f, 1f));
        var progFillRT = progresoFill.GetComponent<RectTransform>();

        float progreso = 0f;
        float tiempo = 0f;
        float limite = 7f * FactorTiempo;
        Vector2 velocidadDrift = new Vector2(Random.Range(-60f, 60f), Random.Range(-40f, 40f)) * FactorVelocidad;
        float limiteDriftX = 260f * FactorRango;
        float limiteDriftY = 150f * FactorRango;
        MiniGameResult result = null;

        while (result == null)
        {
            // Congelar el minijuego mientras el juego está en pausa (Esc): los minijuegos
            // usan tiempo unscaled, así que sin esta guarda seguirían corriendo (y podrías
            // perder al paciente) con el menú de pausa encima.
            if (Time.timeScale == 0f) { yield return null; continue; }

            tiempo += Time.unscaledDeltaTime;

            Vector2 posObjetivo = objetivoRT.anchoredPosition + velocidadDrift * Time.unscaledDeltaTime;
            if (posObjetivo.x > limiteDriftX || posObjetivo.x < -limiteDriftX) velocidadDrift.x *= -1f;
            if (posObjetivo.y > limiteDriftY || posObjetivo.y < -limiteDriftY) velocidadDrift.y *= -1f;
            posObjetivo.x = Mathf.Clamp(posObjetivo.x, -limiteDriftX, limiteDriftX);
            posObjetivo.y = Mathf.Clamp(posObjetivo.y, -limiteDriftY, limiteDriftY);
            objetivoRT.anchoredPosition = posObjetivo;

            Vector2 local;
            Vector2 screen = Pointer.current != null ? Pointer.current.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(zonaRT, screen, null, out local);
            gasaRT.anchoredPosition = local;

            bool sobreObjetivo = Vector2.Distance(local, objetivoRT.anchoredPosition) < 60f;

            if (sobreObjetivo)
            {
                progreso += Time.unscaledDeltaTime * 0.4f;
                gasa.GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.85f, 1f);
            }
            else
            {
                progreso -= Time.unscaledDeltaTime * 0.25f;
                gasa.GetComponent<Image>().color = new Color(0.6f, 0.5f, 0.5f, 0.7f);
            }
            progreso = Mathf.Clamp01(progreso);
            progFillRT.anchorMax = new Vector2(progreso, 1f);

            if (progreso >= 1f)
                result = new MiniGameResult { success = true };
            else if (tiempo >= limite)
                result = new MiniGameResult { success = false, damageIfFailed = 8f, failureMessage = "La gasa se resbaló. Sigue sangrando un poco." };

            yield return null;
        }

        CerrarOverlay(result.success ? "¡Presión aplicada!" : result.failureMessage, result.success);
        yield return new WaitForSecondsRealtime(0.8f);
        LimpiarOverlay();
        EnCurso = false;
        onComplete?.Invoke(result);
    }

    // ============================ MINIJUEGO 5: LIMPIEZA DE HERIDA (Alcohol) ============================
    // Solo Difícil/Infernal. Hay que frotar (mover el algodón de un lado a otro) sobre la
    // mancha para ir limpiándola dentro del tiempo límite.
    IEnumerator LimpiezaHerida(Patient paciente, System.Action<MiniGameResult> onComplete)
    {
        EnCurso = true;
        var root = CrearOverlay("LIMPIEZA DE HERIDA", "Frota: mueve el algodón de lado a lado SOBRE la mancha para irla borrando. Límpiala del todo antes de que acabe el tiempo.");

        var zona = CrearPanel("Zona", root.transform, new Vector2(0.22f, 0.3f), new Vector2(0.78f, 0.7f), new Color(0.1f, 0.1f, 0.13f, 0.9f));
        var zonaRT = zona.GetComponent<RectTransform>();

        var mancha = CrearPanel("Mancha", zona.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.55f, 0.15f, 0.1f, 0.85f));
        var manchaRT = mancha.GetComponent<RectTransform>();
        manchaRT.sizeDelta = new Vector2(180, 110);

        var algodon = CrearPanel("Algodon", zona.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.95f, 0.95f, 0.9f, 0.9f));
        var algodonRT = algodon.GetComponent<RectTransform>();
        algodonRT.sizeDelta = new Vector2(50, 50);

        var progresoFondo = CrearPanel("ProgFondo", root.transform, new Vector2(0.3f, 0.16f), new Vector2(0.7f, 0.2f), new Color(0.2f, 0.2f, 0.25f, 1f));
        var progresoFill = CrearPanel("ProgFill", progresoFondo.transform, new Vector2(0, 0), new Vector2(0, 1), new Color(0.7f, 0.85f, 0.95f, 1f));
        var progFillRT = progresoFill.GetComponent<RectTransform>();

        float progreso = 0f;
        float tiempo = 0f;
        float limite = 6f * FactorTiempo;
        Vector2 posAnterior = Vector2.zero;
        bool primera = true;
        MiniGameResult result = null;

        while (result == null)
        {
            // Congelar el minijuego mientras el juego está en pausa (Esc): los minijuegos
            // usan tiempo unscaled, así que sin esta guarda seguirían corriendo (y podrías
            // perder al paciente) con el menú de pausa encima.
            if (Time.timeScale == 0f) { yield return null; continue; }

            tiempo += Time.unscaledDeltaTime;

            Vector2 local;
            Vector2 screen = Pointer.current != null ? Pointer.current.position.ReadValue() : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(zonaRT, screen, null, out local);
            algodonRT.anchoredPosition = local;

            bool dentroDeMancha = Mathf.Abs(local.x) < 95f && Mathf.Abs(local.y) < 60f;

            if (!primera && dentroDeMancha)
                progreso += Vector2.Distance(local, posAnterior) * 0.0022f;

            posAnterior = local;
            primera = false;

            progreso = Mathf.Clamp01(progreso);
            progFillRT.anchorMax = new Vector2(progreso, 1f);
            mancha.GetComponent<Image>().color = Color.Lerp(new Color(0.55f, 0.15f, 0.1f, 0.85f), new Color(0.55f, 0.15f, 0.1f, 0f), progreso);

            if (progreso >= 1f)
                result = new MiniGameResult { success = true };
            else if (tiempo >= limite)
                result = new MiniGameResult { success = false, damageIfFailed = 6f, failureMessage = "Quedó mal desinfectada. Riesgo de infección." };

            yield return null;
        }

        CerrarOverlay(result.success ? "¡Herida limpia!" : result.failureMessage, result.success);
        yield return new WaitForSecondsRealtime(0.8f);
        LimpiarOverlay();
        EnCurso = false;
        onComplete?.Invoke(result);
    }

    // ============================ MINIJUEGO 6: SECUENCIA DE AUXILIO (Kit) ============================
    // Solo Difícil/Infernal. Simon-says: memoriza el orden en que se iluminan los pasos
    // del kit y repítelo tocándolos en el mismo orden.
    IEnumerator SecuenciaAuxilio(Patient paciente, System.Action<MiniGameResult> onComplete)
    {
        EnCurso = true;
        var root = CrearOverlay("SECUENCIA DE AUXILIO", "Observa el orden en que se iluminan los pasos y repítelo tocándolos en la MISMA secuencia. Un error y falla.");

        var zona = CrearPanel("Zona", root.transform, new Vector2(0.15f, 0.35f), new Vector2(0.85f, 0.62f), new Color(0f, 0f, 0f, 0f));
        int totalBotones = 4;
        var botones = new GameObject[totalBotones];
        var colorNormal = new Color(0.3f, 0.3f, 0.38f, 1f);
        var colorFlash = new Color(1f, 0.85f, 0f, 1f);
        float ancho = 1f / totalBotones;
        for (int i = 0; i < totalBotones; i++)
            botones[i] = CrearPanel("Paso" + i, zona.transform, new Vector2(i * ancho + 0.02f, 0.1f), new Vector2((i + 1) * ancho - 0.02f, 0.9f), colorNormal);

        // En Infernal: secuencia más larga y parpadeos más veloces (menos tiempo para memorizar)
        int largoSecuencia = EsInfernal ? 5 : 4;
        var secuencia = new System.Collections.Generic.List<int>();
        for (int i = 0; i < largoSecuencia; i++) secuencia.Add(Random.Range(0, totalBotones));

        // Mostrar la secuencia parpadeando cada paso en orden
        yield return new WaitForSecondsRealtime(0.5f);
        for (int i = 0; i < secuencia.Count; i++)
        {
            int idx = secuencia[i];
            botones[idx].GetComponent<Image>().color = colorFlash;
            yield return new WaitForSecondsRealtime(0.55f / FactorVelocidad);
            botones[idx].GetComponent<Image>().color = colorNormal;
            yield return new WaitForSecondsRealtime(0.2f / FactorVelocidad);
        }

        // Habilitar los botones para la fase de respuesta del jugador
        int idxClick = -1;
        var botonComponents = new Button[totalBotones];
        for (int i = 0; i < totalBotones; i++)
        {
            botonComponents[i] = botones[i].AddComponent<Button>();
            int capturado = i;
            botonComponents[i].onClick.AddListener(() => { idxClick = capturado; });
        }

        int siguienteEsperado = 0;
        float tiempo = 0f;
        float limite = 8f * FactorTiempo;
        MiniGameResult result = null;

        while (result == null)
        {
            // Congelar el minijuego mientras el juego está en pausa (Esc): los minijuegos
            // usan tiempo unscaled, así que sin esta guarda seguirían corriendo (y podrías
            // perder al paciente) con el menú de pausa encima.
            if (Time.timeScale == 0f) { yield return null; continue; }

            tiempo += Time.unscaledDeltaTime;

            if (idxClick >= 0)
            {
                if (idxClick == secuencia[siguienteEsperado])
                {
                    botones[idxClick].GetComponent<Image>().color = new Color(0.2f, 0.9f, 0.4f, 1f);
                    siguienteEsperado++;
                    if (siguienteEsperado >= secuencia.Count)
                        result = new MiniGameResult { success = true };
                }
                else
                {
                    result = new MiniGameResult { success = false, damageIfFailed = 10f, failureMessage = "Te equivocaste de paso. El kit no ayudó mucho." };
                }
                idxClick = -1;
            }

            if (tiempo >= limite && result == null)
                result = new MiniGameResult { success = false, damageIfFailed = 10f, failureMessage = "Muy lento armando el kit." };

            yield return null;
        }

        CerrarOverlay(result.success ? "¡Auxilio aplicado!" : result.failureMessage, result.success);
        yield return new WaitForSecondsRealtime(0.8f);
        LimpiarOverlay();
        EnCurso = false;
        onComplete?.Invoke(result);
    }

    // ============================ MINIJUEGO 7: PUNZADA (alternativa aleatoria) ============================
    IEnumerator Punzada(Patient paciente, System.Action<MiniGameResult> onComplete)
    {
        EnCurso = true;
        var root = CrearOverlay("PUNZADA", "Presiona clic cuando la aguja esté en la zona verde. 3 aciertos.");

        var zona = CrearPanel("Zona", root.transform, new Vector2(0.25f, 0.3f), new Vector2(0.75f, 0.7f), new Color(0.1f, 0.1f, 0.13f, 0.9f));

        float targetCenter = Random.Range(-180f, 180f);
        float targetWidth = 60f;
        var target = CrearPanel("ZonaObjetivo", zona.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.2f, 0.9f, 0.4f, 0.5f));
        var targetRT = target.GetComponent<RectTransform>();
        targetRT.sizeDelta = new Vector2(targetWidth, 60);
        targetRT.anchoredPosition = new Vector2(targetCenter, 0);

        var aguja = CrearPanel("Aguja", zona.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(1f, 0.3f, 0.2f, 1f));
        var agujaRT = aguja.GetComponent<RectTransform>();
        agujaRT.sizeDelta = new Vector2(10, 70);

        var timerFill = CrearPanel("Timer", root.transform, new Vector2(0.3f, 0.18f), new Vector2(0.7f, 0.22f), new Color(0.9f, 0.3f, 0.2f, 1f));
        var timerRT = timerFill.GetComponent<RectTransform>();

        var aciertosTxt = CrearTexto("Aciertos", root.transform, new Vector2(0.1f, 0.12f), new Vector2(0.9f, 0.16f), 28, TextAlignmentOptions.Center);
        aciertosTxt.text = "Aciertos: 0 / 3";
        aciertosTxt.color = Color.white;

        int aciertos = 0;
        float tiempo = 0f;
        float limite = 10f * FactorTiempo;
        float vel = 180f * FactorVelocidad;
        float rangoAguja = 230f * FactorRango;
        float esperaClick = 0f;
        bool puedeClick = true;
        MiniGameResult result = null;

        while (result == null)
        {
            if (Time.timeScale == 0f) { yield return null; continue; }
            tiempo += Time.unscaledDeltaTime;

            if (!puedeClick)
            {
                esperaClick -= Time.unscaledDeltaTime;
                if (esperaClick <= 0f) puedeClick = true;
            }

            float posX = Mathf.Sin(Time.unscaledTime * vel * 0.01f) * rangoAguja;
            agujaRT.anchoredPosition = new Vector2(posX, 0);

            bool enZona = Mathf.Abs(posX - targetCenter) < targetWidth * 0.5f;
            aguja.GetComponent<Image>().color = enZona ? new Color(0.2f, 1f, 0.4f, 1f) : new Color(1f, 0.3f, 0.2f, 1f);

            if (puedeClick && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                puedeClick = false;
                aguja.GetComponent<Image>().color = Color.white;
                if (enZona)
                {
                    aciertos++;
                    aciertosTxt.text = "Aciertos: " + aciertos + " / 3";
                    if (aciertos >= 3)
                        result = new MiniGameResult { success = true };
                    else
                    {
                        targetCenter = Random.Range(-180f, 180f);
                        targetRT.anchoredPosition = new Vector2(targetCenter, 0);
                        esperaClick = 0.3f;
                    }
                }
                else
                {
                    result = new MiniGameResult { success = false, damageIfFailed = 15f, failureMessage = "¡Punzada fallida! La aguja se desvió." };
                }
            }
            else if (!puedeClick && aguja.GetComponent<Image>().color == Color.white)
            {
                aguja.GetComponent<Image>().color = enZona ? new Color(0.2f, 1f, 0.4f, 1f) : new Color(1f, 0.3f, 0.2f, 1f);
            }

            if (tiempo >= limite && result == null)
                result = new MiniGameResult { success = false, damageIfFailed = 15f, failureMessage = "Te quedaste sin tiempo." };

            timerRT.localScale = new Vector3(1f - tiempo / limite, 1, 1);
            yield return null;
        }

        CerrarOverlay(result.success ? "¡Punzada exacta!" : result.failureMessage, result.success);
        yield return new WaitForSecondsRealtime(0.8f);
        LimpiarOverlay();
        EnCurso = false;
        onComplete?.Invoke(result);
    }

    // ============================ UI helpers ============================
    GameObject CrearOverlay(string titulo, string instruccion)
    {
        overlay = CrearPanel("MiniGameOverlay", canvasRaiz.transform, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.78f));
        overlay.transform.SetAsLastSibling();

        // Banner de aviso en la parte superior (alto riesgo / complicación 2 de 2)
        if (!string.IsNullOrEmpty(bannerProximo))
        {
            var banner = CrearPanel("BannerAviso", overlay.transform, new Vector2(0.12f, 0.93f), new Vector2(0.88f, 0.99f), bannerProximoColor);
            var btxt = CrearTexto("BannerTxt", banner.transform, Vector2.zero, Vector2.one, 30, TextAlignmentOptions.Center);
            btxt.text = bannerProximo;
            btxt.color = Color.white;
            btxt.fontStyle = FontStyles.Bold;
            bannerProximo = null; // solo aplica a este overlay
        }

        var t = CrearTexto("Titulo", overlay.transform, new Vector2(0.1f, 0.82f), new Vector2(0.9f, 0.92f), 60, TextAlignmentOptions.Center);
        t.text = titulo; t.color = new Color(1f, 0.85f, 0f, 1f); t.fontStyle = FontStyles.Bold;

        var ins = CrearTexto("Instruccion", overlay.transform, new Vector2(0.1f, 0.74f), new Vector2(0.9f, 0.8f), 28, TextAlignmentOptions.Center);
        ins.text = instruccion; ins.color = Color.white; ins.fontStyle = FontStyles.Italic;
        return overlay;
    }

    void CerrarOverlay(string mensaje, bool exito)
    {
        if (overlay == null) return;
        var res = CrearTexto("Resultado", overlay.transform, new Vector2(0.1f, 0.36f), new Vector2(0.9f, 0.46f), 44, TextAlignmentOptions.Center);
        res.text = mensaje;
        res.color = exito ? new Color(0.2f, 0.95f, 0.4f, 1f) : new Color(1f, 0.3f, 0.2f, 1f);
        res.fontStyle = FontStyles.Bold;
    }

    void LimpiarOverlay()
    {
        if (overlay != null) Destroy(overlay);
        overlay = null;
    }

    GameObject CrearPanel(string nombre, Transform padre, Vector2 anclaMin, Vector2 anclaMax, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(padre, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anclaMin; rt.anchorMax = anclaMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
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
        txt.raycastTarget = false;
        txt.fontSize = tamano;
        txt.alignment = alineacion;
        txt.text = "";
        return txt;
    }
}
