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

        puntoAcercamiento = estacion.PuntoAcercamiento();
        if (agente.isOnNavMesh)
        {
            agente.isStopped = false;
            agente.stoppingDistance = 0.3f;
            agente.SetDestination(puntoAcercamiento);
        }
        estado = Estado.Caminando;
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
                    bool casiQuieto = agente.velocity.sqrMagnitude < 0.06f;
                    bool cercaFisico = Vector3.Distance(transform.position, puntoAcercamiento) < 1.4f;
                    if (rutaLista && casiQuieto && cercaFisico)
                        IniciarAcomodo();
                }
                break;

            case Estado.Acomodandose:
                // Interpolar suavemente hacia la pose de acostado sobre la camilla
                transform.position = Vector3.MoveTowards(transform.position, posAcostado, velocidadAcomodo * Time.deltaTime);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotAcostado, velocidadAcomodo * 90f * Time.deltaTime);
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
