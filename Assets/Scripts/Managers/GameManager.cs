using UnityEngine;

// Estados globales del juego
public enum GameState
{
    Menu,       // Navegando menús (splash, principal, ajustes, avatar)
    EnTurno,    // Gameplay activo en la sala de urgencias
    FinDeTurno, // Resumen del día (pantalla futura)
    GameOver    // 4 pacientes perdidos consecutivos
}

// Controlador del flujo general del juego (Singleton).
// Maneja el estado global, el contador de "Días Sobrevividos" y el ciclo de días.
// La dificultad NO vive aquí: la maneja DifficultyDirector (la IA adaptativa);
// GameManager solo la consulta y le avisa cuando cambia el día.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Estado (solo lectura en runtime)")]
    public GameState currentState = GameState.Menu;
    public int daysSurvived = 0;
    public int pacientesAtendidosHoy = 0;
    public int rachaPerdidos = 0;

    public const int MaxRachaPerdidos = 4;

    [Header("Configuración")]
    [Tooltip("Pacientes que hay que resolver (vivos o muertos) para completar un día")]
    public int pacientesPorDia = 3;

    // Eventos para que otros sistemas se suscriban (patrón Observer, evita polling en Update)
    public event System.Action<GameState> OnStateChanged;
    public event System.Action<int> OnDayStarted;      // número de día que inicia
    public event System.Action<int> OnDayEnded;        // días sobrevividos acumulados
    public event System.Action OnGameOver;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void IniciarTurno()
    {
        daysSurvived = 0;
        pacientesAtendidosHoy = 0;
        rachaPerdidos = 0;

        // Resetear COMPLETO el Director de Dificultad: sin esto, una segunda partida
        // hereda el performanceScore y el conteo de salvados/perdidos de la anterior
        // (dificultad injusta y scores contaminados en la tabla de puntuaciones).
        if (DifficultyDirector.Instance != null)
        {
            DifficultyDirector.Instance.diaActual = 1;
            DifficultyDirector.Instance.performanceScore = 0.5f;
            DifficultyDirector.Instance.pacientesSalvados = 0;
            DifficultyDirector.Instance.pacientesPerdidos = 0;
        }

        CambiarEstado(GameState.EnTurno);
        StartNewDay();
        Debug.Log("[GameManager] Turno iniciado.");
    }

    public void StartNewDay()
    {
        pacientesAtendidosHoy = 0;
        int dia = DifficultyDirector.Instance != null ? DifficultyDirector.Instance.diaActual : 1;
        OnDayStarted?.Invoke(dia);
        Debug.Log("[GameManager] Comienza el día " + dia);
    }

    // Lo llama PatientManager cada vez que un paciente se resuelve (salvado o perdido)
    public void RegistrarPacienteResuelto(bool salvado)
    {
        try
        {
            Debug.Log("[GameManager] RegistrarPacienteResuelto(salvado=" + salvado + ") state=" + currentState + " racha=" + rachaPerdidos);

            // RACHA: SIEMPRE se actualiza, sin importar el estado del juego.
            // Así aunque un paciente se resuelva en un momento extraño, la racha no se pierde.
            if (salvado)
                rachaPerdidos = 0;
            else
                rachaPerdidos++;

            Debug.Log("[GameManager] -> racha ahora = " + rachaPerdidos + " (max=" + MaxRachaPerdidos + ")");
            if (rachaPerdidos >= MaxRachaPerdidos && currentState == GameState.EnTurno)
            {
                Debug.Log("[GameManager] GAME OVER: " + rachaPerdidos + " pacientes perdidos consecutivos.");
                CambiarEstado(GameState.GameOver);
                Debug.Log("[GameManager] Estado cambiado a GameOver, invocando OnGameOver...");
                if (OnGameOver != null)
                {
                    OnGameOver.Invoke();
                    Debug.Log("[GameManager] OnGameOver invocado.");
                }
                else
                {
                    Debug.LogWarning("[GameManager] OnGameOver no tiene suscriptores!");
                }
                return;
            }

            if (currentState != GameState.EnTurno) return;

            pacientesAtendidosHoy++;
            if (pacientesAtendidosHoy >= pacientesPorDia)
                EndDay();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[GameManager] Excepción en RegistrarPacienteResuelto: " + e.Message + "\n" + e.StackTrace);
        }
    }

    public void EndDay()
    {
        daysSurvived++;
        if (DifficultyDirector.Instance != null)
            DifficultyDirector.Instance.AvanzarDia();

        OnDayEnded?.Invoke(daysSurvived);
        Debug.Log("[GameManager] Día completado. Días sobrevividos: " + daysSurvived);

        StartNewDay();
    }

    public void ReiniciarTurno()
    {
        pacientesAtendidosHoy = 0;
        daysSurvived = 0;
        rachaPerdidos = 0;
        if (DifficultyDirector.Instance != null)
            DifficultyDirector.Instance.diaActual = 1;
        StartNewDay();
    }

    public void TerminarTurno()
    {
        CambiarEstado(GameState.Menu);
        Debug.Log("[GameManager] Turno terminado, de vuelta al menú.");
    }

    void CambiarEstado(GameState nuevo)
    {
        if (nuevo == currentState) return;
        currentState = nuevo;
        OnStateChanged?.Invoke(currentState);
    }
}
