using UnityEngine;
using System.Collections.Generic;

public class ToolModel : MonoBehaviour
{
    [Tooltip("Nombre interno de la herramienta: Gasas, Alcohol, Pinzas, Suturas, Torniquete, Kit")]
    public string toolName;

    [Tooltip("Nombre visible en el tooltip")]
    public string displayName;

    [Tooltip("Material del contorno. Si es null se carga RedOutline.mat.")]
    public Material outlineMaterial;

    bool resaltadoActivo;
    readonly List<Renderer> renderers = new List<Renderer>();
    readonly List<Material[]> materialesOriginales = new List<Material[]>();

    void Awake()
    {
        if (outlineMaterial == null)
        {
            outlineMaterial = Resources.Load<Material>("RedOutline");
            if (outlineMaterial == null)
            {
                var shader = Shader.Find("Custom/RedOutline");
                if (shader != null)
                    outlineMaterial = new Material(shader);
            }
        }
        GetComponentsInChildren(true, renderers);
    }

    public void ActivarResaltado()
    {
        if (resaltadoActivo || outlineMaterial == null) return;
        resaltadoActivo = true;

        materialesOriginales.Clear();
        foreach (var r in renderers)
        {
            var orig = r.sharedMaterials;
            materialesOriginales.Add(orig);

            var conOutline = new Material[orig.Length + 1];
            for (int i = 0; i < orig.Length; i++) conOutline[i] = orig[i];
            conOutline[orig.Length] = outlineMaterial;
            r.materials = conOutline;
        }
    }

    public void DesactivarResaltado()
    {
        if (!resaltadoActivo) return;
        resaltadoActivo = false;

        for (int i = 0; i < renderers.Count && i < materialesOriginales.Count; i++)
        {
            if (renderers[i] != null)
                renderers[i].materials = materialesOriginales[i];
        }
        materialesOriginales.Clear();
    }

    void OnDestroy()
    {
        DesactivarResaltado();
    }
}
