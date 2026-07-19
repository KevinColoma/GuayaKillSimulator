using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// Marca una camilla improvisada (MorgueTable, operation_table...) como estación de
// tratamiento. El paciente NPC camina hasta el punto de acercamiento, se sube y
// adopta la pose de "acostado" sobre la superficie. Se auto-registra en una lista
// estática para que PatientManager pueda pedir la camilla libre más cercana.
public class TreatmentStation : MonoBehaviour
{
    public static readonly List<TreatmentStation> Stations = new List<TreatmentStation>();

    [Header("Puntos de la estación")]
    [Tooltip("Altura (en metros) sobre el pivote de la camilla donde se acuesta el paciente.")]
    public float alturaSuperficie = 0.9f;
    [Tooltip("Distancia (m) frente a la camilla donde el paciente se detiene antes de subir.")]
    public float distanciaAcercamiento = 1.2f;

    [Header("Ajuste fino de la pose acostada (por camilla)")]
    [Tooltip("Grados extra sobre la pose base de acostado (base = 90 en X). Nudge en vivo desde el Inspector.")]
    public Vector3 ajusteRotacion = Vector3.zero;
    [Tooltip("Desplazamiento extra (metros, mundo) sobre la posicion acostada, ademas de alturaSuperficie.")]
    public Vector3 ajustePosicion = Vector3.zero;

    public bool Ocupada { get; private set; }

    void OnEnable()
    {
        if (!Stations.Contains(this)) Stations.Add(this);
    }

    void OnDisable()
    {
        Stations.Remove(this);
    }

    // Punto en el NavMesh, al lado de la camilla, donde el paciente camina primero
// Punto en el NavMesh, al lado de la camilla, donde el paciente camina primero.
    // Se valida contra el NavMesh para NO devolver un punto dentro de una pared o
    // sobre la mesa (que causaría rutas parciales y que el paciente se acueste antes de llegar).
    public Vector3 PuntoAcercamiento()
    {
        Vector3 crudo = transform.position - transform.forward * distanciaAcercamiento;
        crudo.y = transform.position.y;

        NavMeshHit hit;
        // 1) intentar el punto ideal frente a la camilla
        if (NavMesh.SamplePosition(crudo, out hit, 2f, NavMesh.AllAreas))
            return hit.position;
        // 2) probar el lado opuesto por si la camilla está pegada a una pared
        Vector3 opuesto = transform.position + transform.forward * distanciaAcercamiento;
        opuesto.y = transform.position.y;
        if (NavMesh.SamplePosition(opuesto, out hit, 2f, NavMesh.AllAreas))
            return hit.position;
        // 3) cualquier punto caminable cerca de la camilla
        if (NavMesh.SamplePosition(transform.position, out hit, 4f, NavMesh.AllAreas))
            return hit.position;
        return crudo;
    }

    // Punto y orientación finales donde el cuerpo del paciente queda "acostado"
public Vector3 PuntoAcostado()
    {
        return transform.position + Vector3.up * alturaSuperficie + ajustePosicion;
    }

public Quaternion RotacionAcostado()
    {
        // Tumbado boca arriba: base 90 en X sobre la orientacion de la camilla, mas el ajuste fino del Inspector.
        return transform.rotation * Quaternion.Euler(90f + ajusteRotacion.x, ajusteRotacion.y, ajusteRotacion.z);
    }

    public void Ocupar() => Ocupada = true;
    public void Liberar() => Ocupada = false;

    // Devuelve la estación libre más cercana a una posición dada (null si todas ocupadas)
    public static TreatmentStation MasCercanaLibre(Vector3 desde)
    {
        TreatmentStation mejor = null;
        float mejorDist = float.MaxValue;
        foreach (var s in Stations)
        {
            if (s == null || s.Ocupada) continue;
            float d = (s.transform.position - desde).sqrMagnitude;
            if (d < mejorDist) { mejorDist = d; mejor = s; }
        }
        return mejor;
    }

    public static bool HayEstacionLibre()
    {
        foreach (var s in Stations)
            if (s != null && !s.Ocupada) return true;
        return false;
    }
}
