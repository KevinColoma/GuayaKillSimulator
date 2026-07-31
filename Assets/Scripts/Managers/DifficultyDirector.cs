using UnityEngine;

// Niveles de dificultad que expone el Director a otros sistemas (UI, narrador, generador de pacientes)
public enum DifficultyTier
{
    Facil,
    Normal,
    Dificil,
    Infernal
}

// Pesos de probabilidad para el tipo de herida del siguiente paciente
[System.Serializable]
public struct WoundWeights
{
    public float bala;
    public float cuchillo;
    public float accidente;
}

// Director de Dificultad Adaptativa (DDA): mide el desempeño reciente del jugador
// con una Media Móvil Exponencial (EMA) y ajusta en vivo qué tan exigente es el juego.
// Mismo enfoque que el "AI Director" de Left 4 Dead: no sube la dificultad solo por
// el número de día, sino que reacciona a si el jugador está dominando o sufriendo.
public class DifficultyDirector : MonoBehaviour
{
    public static DifficultyDirector Instance { get; private set; }

    [Header("Estado del Director (solo lectura en runtime)")]
    [Range(0f, 1f)]
    [Tooltip("0 = jugador en apuros, 1 = jugador dominando. Se actualiza con cada resultado de paciente.")]
    public float performanceScore = 0.5f;
    public DifficultyTier currentTier = DifficultyTier.Normal;
    public int diaActual = 1;
    public int pacientesSalvados = 0;
    public int pacientesPerdidos = 0;

    [Header("Ajuste del algoritmo (EMA)")]
    [Range(0.05f, 0.9f)]
    [Tooltip("Cuánto pesa cada resultado nuevo sobre el score acumulado. Alto = reacciona rápido, Bajo = más estable.")]
    public float smoothingFactor = 0.25f;

    [Header("Umbrales de nivel")]
    public float umbralFacil = 0.3f;
    public float umbralDificil = 0.6f;
    public float umbralInfernal = 0.75f;

    public event System.Action<DifficultyTier> OnDifficultyChanged;
    public event System.Action<float> OnPerformanceUpdated;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Lo llama PatientManager/PatientGenerator cuando el jugador termina de atender a un paciente
    public void RegistrarResultadoPaciente(bool salvado, float tiempoSegundos, float tiempoLimite, int erroresCometidos)
    {
        if (salvado) pacientesSalvados++;
        else pacientesPerdidos++;

        float scoreInstantaneo = CalcularScoreInstantaneo(salvado, tiempoSegundos, tiempoLimite, erroresCometidos);

        // Media móvil exponencial: suaviza el ruido de un solo resultado para que
        // un golpe de suerte (o de mala suerte) no dispare la dificultad de golpe
        performanceScore = Mathf.Clamp01(Mathf.Lerp(performanceScore, scoreInstantaneo, smoothingFactor));

        ActualizarTier();
        OnPerformanceUpdated?.Invoke(performanceScore);

        Debug.Log($"[DifficultyDirector] {(salvado ? "SALVADO" : "PERDIDO")} | score instantáneo={scoreInstantaneo:F2} | score suavizado={performanceScore:F2} | nivel={currentTier}");
    }

    float CalcularScoreInstantaneo(bool salvado, float tiempo, float tiempoLimite, int errores)
    {
        float baseScore = salvado ? 1f : 0f;

        // Eficiencia de tiempo: usar menos tiempo del límite suma puntos
        float eficienciaTiempo = tiempoLimite > 0f ? Mathf.Clamp01(1f - (tiempo / tiempoLimite)) : 0.5f;

        // Cada error de procedimiento resta puntos (hasta un tope)
        float penalizacionErrores = Mathf.Clamp01(errores * 0.15f);

        float score = baseScore * 0.7f + eficienciaTiempo * 0.3f - penalizacionErrores;
        return Mathf.Clamp01(score);
    }

    void ActualizarTier()
    {
        DifficultyTier nuevoTier;
        if (performanceScore < umbralFacil) nuevoTier = DifficultyTier.Facil;
        else if (performanceScore < umbralDificil) nuevoTier = DifficultyTier.Normal;
        else if (performanceScore < umbralInfernal) nuevoTier = DifficultyTier.Dificil;
        else nuevoTier = DifficultyTier.Infernal;

        if (nuevoTier != currentTier)
        {
            currentTier = nuevoTier;
            OnDifficultyChanged?.Invoke(currentTier);
            Debug.Log("[DifficultyDirector] Cambio de nivel de dificultad -> " + currentTier);
        }
    }

    public void AvanzarDia()
    {
        diaActual++;
    }

    // ---------------------------------------------------------------
    // API que consumirá PatientGenerator (Fase 2, pendiente de crear)
    // ---------------------------------------------------------------

    // Combina la tabla base por día del documento de diseño con el ajuste
    // en vivo del desempeño: si el jugador domina, empuja hacia heridas más
    // exigentes (bala); si sufre, favorece las más simples (accidente).
    public WoundWeights GetPesosHeridas()
    {
        WoundWeights baseWeights;
        if (diaActual <= 3)
            baseWeights = new WoundWeights { bala = 0.4f, cuchillo = 0.3f, accidente = 0.3f };
        else if (diaActual <= 6)
            baseWeights = new WoundWeights { bala = 0.5f, cuchillo = 0.25f, accidente = 0.25f };
        else
            baseWeights = new WoundWeights { bala = 0.6f, cuchillo = 0.3f, accidente = 0.1f };

        float ajuste = (performanceScore - 0.5f) * 0.3f; // rango aprox. -0.15 a +0.15
        baseWeights.bala = Mathf.Clamp01(baseWeights.bala + ajuste);
        baseWeights.accidente = Mathf.Clamp01(baseWeights.accidente - ajuste);
        return baseWeights;
    }

    // Segundos entre la llegada de pacientes: menos tiempo de respiro cuanto mejor le va al jugador
    public float GetIntervaloAparicion()
    {
        return Mathf.Lerp(55f, 25f, performanceScore);
    }

    // Multiplicador sobre el tiempo límite base de cada paciente
    public float GetMultiplicadorTiempoLimite()
    {
        return Mathf.Lerp(1.3f, 0.7f, performanceScore);
    }

// Tiempo límite concreto (en segundos) para atender al próximo paciente.
    // Parte de una base de 45s, se reduce ~2s por día transcurrido (mínimo 25s de base)
    // y luego se multiplica por el ajuste en vivo del desempeño (0.7x a 1.3x).
    public float GetTiempoLimitePacienteSegundos()
    {
        float baseSegundos = Mathf.Max(45f - (diaActual - 1) * 2f, 25f);
        return baseSegundos * GetMultiplicadorTiempoLimite();
    }

}
