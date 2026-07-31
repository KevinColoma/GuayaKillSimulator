using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

// Controla el ciclo de vida de los pacientes durante el turno, con MÚLTIPLES camillas
// (una por TreatmentStation). Flujo constante: mientras haya una camilla libre, llega
// un paciente nuevo. El jugador atiende al paciente ENFOCADO (el más cercano dentro de
// rango). Cada camilla lleva su propio cronómetro y sangrado.
public class PatientManager : MonoBehaviour
{
    public static PatientManager Instance { get; private set; }

    // Un puesto de tratamiento = una camilla con su paciente y su reloj.
    public class Slot
    {
        public TreatmentStation estacion;
        public Patient paciente;            // acostado, en tratamiento
        public Patient pacientePendiente;   // generado, viene en camino
        public PatientBody cuerpo;
        public float tiempoRestante, tiempoLimite, tiempoAtendiendo;
        public int errores;
        public bool tratamientoIniciado;
        public bool minijuegoActivo;
        public float reservadoDesde;   // momento en que se despachó el cuerpo (para detectar atascos)

        public bool Libre => paciente == null && pacientePendiente == null && (estacion == null || !estacion.Ocupada);
        public bool EnTratamiento => paciente != null && tratamientoIniciado;
    }

    [Header("Configuración")]
    [Tooltip("Segundos antes de que llegue el PRIMER paciente del turno")]
    public float esperaPrimerPaciente = 5f;
    [Tooltip("Distancia máxima (m) para 'enfocar' y atender a un paciente en la camilla")]
    public float rangoEnfoque = 5f;

    [Header("Spawn de pacientes en la entrada")]
    public Transform entradaSpawn;
    public GameObject prefabPacienteSpawn;
    [Range(0f, 1f)] public float probabilidadSpawn = 0.5f;

    // Eventos (sabor/narrador). El HUD lee los slots directamente cada frame.
    public event System.Action<Patient> OnPacienteEnCamino;
    public event System.Action<Patient> OnPacienteLlega;
    public event System.Action<Patient, bool> OnPacienteResuelto;

    public readonly List<Slot> slots = new List<Slot>();
    public Slot slotEnfocado { get; private set; }

    bool turnoActivo = false;
    Coroutine cicloActual;
    Camera camara;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += OnGameStateChanged;
            if (GameManager.Instance.currentState == GameState.EnTurno)
                ComenzarTurno();
        }
    }

    void OnGameStateChanged(GameState estado)
    {
        if (estado == GameState.EnTurno) ComenzarTurno();
        else TerminarTurno();
    }

    public void ComenzarTurno()
    {
        turnoActivo = true;
        ConstruirSlots();
        if (cicloActual != null) StopCoroutine(cicloActual);
        cicloActual = StartCoroutine(CicloDeLlegadas(esperaPrimerPaciente));
    }

    public void TerminarTurno()
    {
        turnoActivo = false;
        foreach (var s in slots)
            if (s.cuerpo != null) { s.cuerpo.AltaMedica(false); s.cuerpo = null; }
        slots.Clear();
        slotEnfocado = null;
        if (cicloActual != null) { StopCoroutine(cicloActual); cicloActual = null; }
    }

    void ConstruirSlots()
    {
        slots.Clear();
        foreach (var est in TreatmentStation.Stations)
        {
            est.Liberar();
            slots.Add(new Slot { estacion = est });
        }
    }

    // Flujo constante: cada iteración llena una camilla libre; el ritmo lo marca la IA.
    IEnumerator CicloDeLlegadas(float esperaInicial)
    {
        yield return new WaitForSeconds(esperaInicial);
        while (turnoActivo)
        {
            // Llenar TODAS las camillas libres (flujo constante a ambas, no se detiene por muertes)
            foreach (var s in slots)
                if (s.Libre) TryDespachar(s);

            float baseInt = DifficultyDirector.Instance != null ? DifficultyDirector.Instance.GetIntervaloAparicion() : 40f;
            float gap = Mathf.Max(4f, baseInt * 0.5f);
            yield return new WaitForSeconds(gap);
        }
    }

    void TryDespachar(Slot slot)
    {
        try { DespacharPacienteA(slot); }
        catch (System.Exception e) { Debug.LogWarning("[PatientManager] fallo al despachar paciente: " + e.Message); }
    }

    void DespacharPacienteA(Slot slot)
    {
        var cuerpo = ElegirCuerpo();
        if (cuerpo == null) return; // no hay NPC disponible ahora; se reintenta

        slot.pacientePendiente = PatientGenerator.GenerarPaciente();
        slot.reservadoDesde = Time.time;
        slot.tiempoLimite = DifficultyDirector.Instance != null ? DifficultyDirector.Instance.GetTiempoLimitePacienteSegundos() : 45f;
        slot.tiempoRestante = slot.tiempoLimite;
        slot.tiempoAtendiendo = 0f;
        slot.errores = 0;
        slot.tratamientoIniciado = false;
        slot.minijuegoActivo = false;
        slot.cuerpo = cuerpo;

        Debug.Log($"[PatientManager] {slot.pacientePendiente.nombre} ({slot.pacientePendiente.diagnostico}) viene a una camilla.");
        cuerpo.AsignarComoPaciente(slot.estacion, () => OnCuerpoAcostado(slot));
        OnPacienteEnCamino?.Invoke(slot.pacientePendiente);
    }

    void OnCuerpoAcostado(Slot slot)
    {
        if (slot.pacientePendiente == null) return;
        slot.paciente = slot.pacientePendiente;
        slot.pacientePendiente = null;

        // Recalcular el límite de tiempo AQUÍ (no usar el que se calculó al generar el paciente):
        // caminar hasta la camilla puede tardar varios segundos y la dificultad pudo cambiar
        // mientras tanto (ej. se resolvió al otro paciente). El cronómetro clínico debe reflejar
        // el nivel de dificultad vigente en el momento en que arranca el tratamiento.
        slot.tiempoLimite = DifficultyDirector.Instance != null
            ? DifficultyDirector.Instance.GetTiempoLimitePacienteSegundos()
            : 45f;
        slot.tiempoRestante = slot.tiempoLimite;
        slot.tratamientoIniciado = true;

        // Atar el sangrado al cronómetro real: antes bloodLossPorSegundo salía de una tabla
        // fija en PatientGenerator, desconectada del tiempoLimite (que varía 25-59s según
        // dificultad/día). Resultado: pacientes Críticos con sangrado alto se desangraban en
        // 3-8s mientras el reloj visible marcaba 30+ — morían "antes de llegar a 0" el timer.
        // Ahora el sangrado se calcula para que, SIN tratar al paciente, se desangre justo a
        // una fracción del tiempoLimite (el margen de cada severidad), así el cronómetro que
        // ve el jugador siempre representa fielmente cuánto le queda de verdad.
        float margenSangrado = slot.paciente.severidad == Severidad.Leve ? 1.3f
            : slot.paciente.severidad == Severidad.Moderado ? 0.85f
            : 0.5f; // Crítico: se desangra a mitad del cronómetro si no se le trata
        slot.paciente.bloodLossPorSegundo = slot.paciente.health / (slot.tiempoLimite * margenSangrado);
        Debug.Log($"[PatientManager] {slot.paciente.nombre} en la camilla. \"{slot.paciente.dialogoAbsurdo}\" (límite {slot.tiempoLimite:F0}s, nivel {(DifficultyDirector.Instance != null ? DifficultyDirector.Instance.currentTier.ToString() : "?")})");
        OnPacienteLlega?.Invoke(slot.paciente);
    }

    void Update()
    {
        if (!turnoActivo) return;

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];

            // Recuperar camillas atascadas: cuerpo despachado que nunca llegó a acostarse
            // (se quedó trabado en el camino) -> liberar la camilla para que llegue otro.
            if (slot.pacientePendiente != null && !slot.tratamientoIniciado
                && Time.time - slot.reservadoDesde > 25f)
            {
                AbortarReserva(slot);
                continue;
            }

            if (!slot.EnTratamiento || slot.minijuegoActivo) continue;

            // Cronómetro + sangrado
            slot.tiempoRestante -= Time.deltaTime;
            slot.tiempoAtendiendo += Time.deltaTime;
            slot.paciente.AplicarDanio(slot.paciente.bloodLossPorSegundo * Time.deltaTime);

            if (!slot.paciente.EstaVivo() || slot.tiempoRestante <= 0f)
                ResolverSlot(slot, false);
            else if (slot.paciente.EstaEstable())
                ResolverSlot(slot, true);
        }

        ActualizarEnfoque();
    }

    // Un cuerpo se atascó camino a la camilla: destruirlo y liberar la camilla para reintentar.
    void AbortarReserva(Slot slot)
    {
        Debug.Log("[PatientManager] Cuerpo atascado camino a la camilla; se libera y reintenta.");
        if (slot.cuerpo != null) { slot.cuerpo.AltaMedica(false); slot.cuerpo = null; }
        if (slot.estacion != null) slot.estacion.Liberar();
        slot.pacientePendiente = null;
        slot.tratamientoIniciado = false;
    }

    // El paciente enfocado = el más cercano al jugador dentro de rango (para atender y HUD)
    void ActualizarEnfoque()
    {
        if (camara == null) camara = Camera.main;
        if (camara == null) { slotEnfocado = null; return; }

        Vector3 pj = camara.transform.position;
        Slot mejor = null;
        float mejorDist = rangoEnfoque * rangoEnfoque;
        foreach (var s in slots)
        {
            if (!s.EnTratamiento) continue;
            float d = (s.estacion.PuntoAcostado() - pj).sqrMagnitude;
            if (d < mejorDist) { mejorDist = d; mejor = s; }
        }
        slotEnfocado = mejor;
    }

    public void ResolverSlot(Slot slot, bool salvado)
    {
        if (slot == null || slot.paciente == null) return;

        var resuelto = slot.paciente;
        slot.paciente = null;
        slot.tratamientoIniciado = false;
        slot.minijuegoActivo = false;

        if (DifficultyDirector.Instance != null)
            DifficultyDirector.Instance.RegistrarResultadoPaciente(salvado, slot.tiempoAtendiendo, slot.tiempoLimite, slot.errores);
        if (GameManager.Instance != null)
            GameManager.Instance.RegistrarPacienteResuelto(salvado);

        // Alta del cuerpo: muerto desaparece, salvado se levanta y vuelve a deambular
        if (slot.cuerpo != null) { slot.cuerpo.AltaMedica(salvado); slot.cuerpo = null; }

        Debug.Log($"[PatientManager] {resuelto.nombre} {(salvado ? "SALVADO" : "PERDIDO")} tras {slot.tiempoAtendiendo:F0}s con {slot.errores} error(es).");
        OnPacienteResuelto?.Invoke(resuelto, salvado);
    }

    // ---- API para MedicalToolsManager (opera sobre el paciente enfocado) ----
    public Slot SlotDePaciente(Patient p)
    {
        return slots.Find(s => s.paciente == p);
    }

    public void RegistrarErrorEnfocado()
    {
        if (slotEnfocado != null) slotEnfocado.errores++;
    }

    // ---- Selección/spawn del cuerpo ----
    PatientBody ElegirCuerpo()
    {
        bool puedeSpawnear = entradaSpawn != null && prefabPacienteSpawn != null;
        PatientBody wanderer = BuscarWandererLibre();

        bool spawnear;
        if (puedeSpawnear && wanderer == null) spawnear = true;
        else if (!puedeSpawnear) spawnear = false;
        else spawnear = Random.value < probabilidadSpawn;

        if (spawnear) return SpawnearPaciente();
        return wanderer;
    }

    PatientBody BuscarWandererLibre()
    {
        var candidatos = new List<PatientBody>();
        foreach (var pb in FindObjectsByType<PatientBody>(FindObjectsSortMode.None))
            if (pb.DisponibleComoPaciente && !pb.esSpawneado) candidatos.Add(pb);
        if (candidatos.Count == 0) return null;
        return candidatos[Random.Range(0, candidatos.Count)];
    }

    PatientBody SpawnearPaciente()
    {
        var go = Instantiate(prefabPacienteSpawn, entradaSpawn.position, entradaSpawn.rotation);
        var agente = go.GetComponent<NavMeshAgent>();
        if (agente != null && !agente.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(entradaSpawn.position, out hit, 6f, NavMesh.AllAreas))
                agente.Warp(hit.position);
        }
        var cuerpo = go.GetComponent<PatientBody>();
        if (cuerpo != null) cuerpo.esSpawneado = true;
        return cuerpo;
    }
}
