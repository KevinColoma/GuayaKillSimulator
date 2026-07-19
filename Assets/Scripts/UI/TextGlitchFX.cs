using UnityEngine;
using TMPro;

// Efecto de tipografía "distorsionada y caótica" para el logo del Splash Screen:
// hace vibrar los vértices de cada carácter. Basado en el patrón clásico de
// manipulación de vértices de TextMeshPro.
[RequireComponent(typeof(TMP_Text))]
public class TextGlitchFX : MonoBehaviour
{
    public float amplitud = 3.5f;
    public float velocidad = 4f;

    TMP_Text texto;

    void Awake()
    {
        texto = GetComponent<TMP_Text>();
    }

    void Update()
    {
        texto.ForceMeshUpdate();
        TMP_TextInfo info = texto.textInfo;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = info.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int material = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Vector3[] verts = info.meshInfo[material].vertices;

            Vector3 offset = new Vector3(
                Mathf.Sin(Time.time * velocidad + i * 0.6f) * amplitud,
                Mathf.Cos(Time.time * velocidad * 1.3f + i * 0.9f) * amplitud,
                0f);

            verts[vertexIndex + 0] += offset;
            verts[vertexIndex + 1] += offset;
            verts[vertexIndex + 2] += offset;
            verts[vertexIndex + 3] += offset;
        }

        for (int m = 0; m < info.meshInfo.Length; m++)
        {
            info.meshInfo[m].mesh.vertices = info.meshInfo[m].vertices;
            texto.UpdateGeometry(info.meshInfo[m].mesh, m);
        }
    }
}
