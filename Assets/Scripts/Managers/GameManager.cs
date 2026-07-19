using UnityEngine;

// Estados globales del juego
public enum GameState
{
    Menu,       // Navegando menús (splash, principal, ajustes, avatar)
    EnTurno,    // Gameplay activo en la sala de urgencias
    FinDeTurno  // Resumen del día (pantalla futura)
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

    [Header("Configuración")]
    [Tooltip("Pacientes que hay que resolver (vivos o muertos) para completar un día")]
    public int pacientesPorDia = 3;

    // Eventos para que otros sistemas se suscriban (patrón Observer, evita polling en Update)
    public event System.Action<GameState> OnStateChanged;
    public event System.Action<int> OnDayStarted;      // número de día que inicia
    public event System.Action<int> OnDayEnded;        // días sobrevividos acumulados

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
        if (DifficultyDirector.Instance != null)
            DifficultyDirector.Instance.diaActual = 1;

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
    public void RegistrarPacienteResuelto()
    {
        if (currentState != GameState.EnTurno) return;

        pacientesAtendidosHoy++;
        if (pacientesAtendidosHoy >= pacientesPorDia)
            EndDay();
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
