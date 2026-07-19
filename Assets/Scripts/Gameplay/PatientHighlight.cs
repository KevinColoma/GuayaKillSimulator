using UnityEngine;
using System.Collections.Generic;

// Resalta la silueta del NPC en rojo mientras es el paciente herido en tratamiento.
// Técnica: agrega un material de contorno (inverted-hull, Cull Front) como slot extra
// en cada Renderer, de modo que la malla se dibuja una segunda vez extruida = borde rojo.
// Funciona sobre SkinnedMeshRenderer y MeshRenderer sin duplicar objetos.
public class PatientHighlight : MonoBehaviour
{
    [Tooltip("Material del contorno. Si es null se carga Assets/Materials/RedOutline.mat.")]
    public Material outlineMaterial;

    bool activo;
    readonly List<Renderer> renderers = new List<Renderer>();
    readonly List<Material[]> materialesOriginales = new List<Material[]>();

    void Awake()
    {
        if (outlineMaterial == null)
            outlineMaterial = Resources.Load<Material>("RedOutline"); // fallback opcional
        GetComponentsInChildren(true, renderers);
    }

    public void Activar()
    {
        if (activo || outlineMaterial == null) return;
        activo = true;

        materialesOriginales.Clear();
        foreach (var r in renderers)
        {
            var orig = r.sharedMaterials;
            materialesOriginales.Add(orig);

            // Añadir el material de contorno como último slot (re-dibuja la malla extruida)
            var conOutline = new Material[orig.Length + 1];
            for (int i = 0; i < orig.Length; i++) conOutline[i] = orig[i];
            conOutline[orig.Length] = outlineMaterial;
            r.materials = conOutline;
        }
    }

    public void Desactivar()
    {
        if (!activo) return;
        activo = false;

        for (int i = 0; i < renderers.Count && i < materialesOriginales.Count; i++)
        {
            if (renderers[i] != null)
                renderers[i].materials = materialesOriginales[i];
        }
        materialesOriginales.Clear();
    }
}
