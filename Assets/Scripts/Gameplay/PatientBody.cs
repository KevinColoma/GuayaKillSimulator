using UnityEngine;
using UnityEngine.AI;

// Comportamiento físico de un NPC que actúa como paciente:
// camina a la camilla asignada, se sube y adopta la pose de acostado, avisa
// cuando llega (ahí arranca el cronómetro clínico) y se levanta al recibir el alta.
[RequireComponent(typeof(NavMeshAgent))]
public class PatientBody : MonoBehaviour
{
    public enum Estado { Libre, Caminando, Acomodandose, Acostado, Retirandose }
    public Estado estado = Estado.Libre;

    [Tooltip("Si es true, el NPC fue creado solo para ser paciente y se destruye al recibir el alta.")]
    public bool esSpawneado = false;

    [Tooltip("Velocidad de la transición al subir/bajar de la camilla.")]
    public float velocidadAcomodo = 3f;

    [Header("Velocidad al dirigirse a la camilla")]
    [Tooltip("Velocidad base (m/s) al caminar hacia la camilla. Es independiente de la de deambular: " +
             "un herido que llega a urgencias va con prisa.")]
    public float velocidadCaminarBase = 3.5f;
    [Tooltip("Multiplicadores de velocidad según el nivel de la IA de dificultad.")]
    public float multFacil = 1f;
    public float multNormal = 1.2f;
    public float multDificil = 1.7f;
    public float multInfernal = 2.6f;

    // Multiplicador vigente para este traslado (se fija al ser asignado como paciente)
    float multiplicadorActual = 1f;

    NavMeshAgent agente;
    NPCWanderAI wander;
    PatientHighlight highlight;
    TreatmentStation estacion;
    System.Action alLlegar;

    Vector3 puntoAcercamiento;
    Vector3 posAcostado;
    Quaternion rotAcostado;
    Vector3 posLevantado;
    Quaternion rotLevantado;

    public bool DisponibleComoPaciente => estado == Estado.Libre;

    void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
        wander = GetComponent<NPCWanderAI>();
        highlight = GetComponent<PatientHighlight>();
    }

    // Lo llama PatientManager para reclutar a este NPC como paciente
// Lo llama PatientManager para reclutar a este NPC como paciente
    public void AsignarComoPaciente(TreatmentStation estacionDestino, System.Action callbackAlAcostarse)
    {
        estacion = estacionDestino;
        alLlegar = callbackAlAcostarse;
        estacion.Ocupar();

        if (wander != null) wander.enabled = false;
        if (highlight != null) highlight.Activar();  // resaltar la silueta roja del herido

        // Velocidad propia del traslado, escalada por el nivel de la IA de dificultad:
        // en Infernal los heridos entran casi corriendo a la camilla.
        multiplicadorActual = MultiplicadorPorDificultad();

        puntoAcercamiento = estacion.PuntoAcercamiento();
        if (agente.isOnNavMesh)
        {
            agente.isStopped = false;
            agente.stoppingDistance = 0.3f;
            agente.speed = velocidadCaminarBase * multiplicadorActual;
            // Aceleración y giro también escalan: sin esto, en tramos cortos el agente
            // nunca alcanzaría la velocidad alta y el aumento no se notaría.
            agente.acceleration = 20f * multiplicadorActual;
            agente.angularSpeed = 500f;
            agente.SetDestination(puntoAcercamiento);
        }
        estado = Estado.Caminando;
    }

    // Multiplicador de velocidad según el nivel vigente del Director de Dificultad.
    float MultiplicadorPorDificultad()
    {
        var dd = DifficultyDirector.Instance;
        if (dd == null) return multNormal;
        switch (dd.currentTier)
        {
            case DifficultyTier.Facil: return multFacil;
            case DifficultyTier.Dificil: return multDificil;
            case DifficultyTier.Infernal: return multInfernal;
            default: return multNormal;
        }
    }

    void Update()
    {
        switch (estado)
        {
            case Estado.Caminando:
                // Solo acostarse cuando REALMENTE llegó a la camilla: ruta completada,
                // agente casi detenido Y físicamente pegado al punto de acercamiento.
                if (!agente.pathPending)
                {
                    bool rutaLista = agente.remainingDistance <= agente.stoppingDistance + 0.25f;
                    // La tolerancia de "ya frenó" escala con la velocidad del traslado: a
                    // velocidad alta el umbral fijo era tan estricto que el paciente podía
                    // quedarse orbitando la camilla sin llegar a acostarse nunca.
                    bool casiQuieto = agente.velocity.sqrMagnitude < 0.06f * multiplicadorActual;
                    bool cercaFisico = Vector3.Distance(transform.position, puntoAcercamiento) < 1.4f;
                    if (rutaLista && casiQuieto && cercaFisico)
                        IniciarAcomodo();
                }
                break;

            case Estado.Acomodandose:
                // Interpolar hacia la pose de acostado (también más rápido en niveles altos)
                float vAcomodo = velocidadAcomodo * multiplicadorActual;
                transform.position = Vector3.MoveTowards(transform.position, posAcostado, vAcomodo * Time.deltaTime);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotAcostado, vAcomodo * 90f * Time.deltaTime);
                if (Vector3.Distance(transform.position, posAcostado) < 0.02f)
                {
                    transform.position = posAcostado;
                    transform.rotation = rotAcostado;
                    estado = Estado.Acostado;
                    alLlegar?.Invoke();   // ← aquí PatientManager arranca el cronómetro
                    alLlegar = null;
                }
                break;

            case Estado.Acostado:
                // Mantener la pose pegada a la camilla cada frame: así los ajustes de
                // ajusteRotacion/ajustePosicion en el Inspector de TreatmentStation se ven EN VIVO.
                if (estacion != null)
                    transform.SetPositionAndRotation(estacion.PuntoAcostado(), estacion.RotacionAcostado());
                break;

            case Estado.Retirandose:
                transform.position = Vector3.MoveTowards(transform.position, posLevantado, velocidadAcomodo * Time.deltaTime);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotLevantado, velocidadAcomodo * 90f * Time.deltaTime);
                if (Vector3.Distance(transform.position, posLevantado) < 0.05f)
                    FinRetiro();
                break;
        }
    }

    void IniciarAcomodo()
    {
        // Guardar de dónde vino para levantarse ahí y volver al NavMesh
        posLevantado = transform.position;
        rotLevantado = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        posAcostado = estacion.PuntoAcostado();
        rotAcostado = estacion.RotacionAcostado();

        if (agente.isOnNavMesh) agente.isStopped = true;
        agente.enabled = false; // soltar el control del agente para poder subir a la mesa
        estado = Estado.Acomodandose;
    }

    // Lo llama PatientManager cuando el paciente se resuelve (salvado o muerto)
// Lo llama PatientManager cuando el paciente se resuelve.
    //   muerto  -> desaparece enseguida
    //   salvado -> se levanta y vuelve a deambular
    public void AltaMedica(bool salvado)
    {
        if (estacion != null) estacion.Liberar();
        if (highlight != null) highlight.Desactivar();

        if (!salvado)
        {
            Destroy(gameObject);
            return;
        }
        estado = Estado.Retirandose;
    }

    void FinRetiro()
    {
        if (!agente.enabled) agente.enabled = true;
        if (agente.isOnNavMesh) agente.isStopped = false;
        else
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 4f, NavMesh.AllAreas))
                agente.Warp(hit.position);
        }
        if (wander != null) wander.enabled = true;
        estado = Estado.Libre;
        estacion = null;
    }
}
