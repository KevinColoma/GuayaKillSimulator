using UnityEngine;
using UnityEngine.AI;

// IA de deambulación para NPCs usando NavMesh (pathfinding A* de Unity).
// El NPC elige puntos aleatorios del NavMesh y camina hacia ellos, subiendo
// gradas y esquivando paredes automáticamente. Su velocidad escala con:
//   1. El nivel del DifficultyDirector (la otra IA del juego): más presión, NPCs más frenéticos.
//   2. El tiempo transcurrido de la sesión (aceleración gradual).
[RequireComponent(typeof(NavMeshAgent))]
public class NPCWanderAI : MonoBehaviour
{
    [Header("Deambulación")]
    [Tooltip("Radio máximo (m) alrededor del punto actual para elegir el siguiente destino.")]
    public float radioDeambulacion = 12f;
    [Tooltip("Segundos de espera al llegar a un destino antes de elegir otro.")]
    public float esperaMin = 0.5f;
    public float esperaMax = 3f;

    [Header("Velocidad dinámica")]
    public float velocidadBase = 1.6f;
    [Tooltip("Velocidad extra a máxima dificultad (performanceScore = 1).")]
    public float velocidadExtraPorDificultad = 2.2f;
    [Tooltip("Velocidad extra por minuto de sesión transcurrido.")]
    public float aceleracionPorMinuto = 0.15f;
    public float velocidadMaxima = 6f;

    NavMeshAgent agente;
    float proximoDestinoEn;
    float tiempoInicio;

    void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
    }

void Start()
    {
        tiempoInicio = Time.time;

        // Si el NPC no cayó exactamente sobre el NavMesh, moverlo al punto caminable más cercano
        if (!agente.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 15f, NavMesh.AllAreas))
                agente.Warp(hit.position);
        }

        ElegirNuevoDestino();
    }

    void Update()
    {
        ActualizarVelocidad();

        // ¿Llegó al destino (o el camino quedó inválido)? Esperar un poco y elegir otro.
        if (!agente.pathPending && agente.remainingDistance <= agente.stoppingDistance + 0.2f)
        {
            if (Time.time >= proximoDestinoEn)
                ElegirNuevoDestino();
        }
    }

    void ActualizarVelocidad()
    {
        float porDificultad = 0f;
        if (DifficultyDirector.Instance != null)
            porDificultad = DifficultyDirector.Instance.performanceScore * velocidadExtraPorDificultad;

        float minutos = (Time.time - tiempoInicio) / 60f;
        float porTiempo = minutos * aceleracionPorMinuto;

        agente.speed = Mathf.Min(velocidadBase + porDificultad + porTiempo, velocidadMaxima);
        agente.angularSpeed = 240f;
        agente.acceleration = 12f;
    }

    void ElegirNuevoDestino()
    {
        // Muestrear un punto aleatorio válido dentro del NavMesh cerca del NPC
        for (int intento = 0; intento < 10; intento++)
        {
            Vector3 candidato = transform.position + Random.insideUnitSphere * radioDeambulacion;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidato, out hit, 3f, NavMesh.AllAreas))
            {
                agente.SetDestination(hit.position);
                proximoDestinoEn = Time.time + Random.Range(esperaMin, esperaMax);
                return;
            }
        }
        // Si no encontró punto válido, reintenta pronto
        proximoDestinoEn = Time.time + 1f;
    }
}
