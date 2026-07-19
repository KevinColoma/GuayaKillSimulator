using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

// Grupo de botones de selección única (ej. color de uniforme, nivel de experiencia, rasgo inicial).
// Evita depender de Toggle/ToggleGroup de uGUI (requieren más wiring); resalta el botón
// elegido cambiando su color y expone el índice seleccionado.
public class SingleSelectButtonGroup : MonoBehaviour
{
    public Button[] botones;
    public Color colorSeleccionado = new Color(1f, 0.85f, 0f, 1f);
    public Color colorNormal = new Color(0.25f, 0.25f, 0.3f, 1f);
    public int indiceSeleccionado = 0;

    // Si es true, NO recolorea los botones (para no tapar su color propio, ej. swatches de
    // uniforme); en su lugar resalta el elegido con un borde (Outline) y lo agranda un poco.
    public bool preservarColorBoton = false;

    public event UnityAction<int> OnSeleccionCambiada;

    bool wireado = false;

    void Start()
    {
        WireBotones();
    }

    // Permite asignar los botones desde código (por ejemplo desde MenuFlowManager)
    // cuando el arreglo no viene enlazado desde el Inspector.
    public void SetBotones(Button[] nuevosBotones)
    {
        botones = nuevosBotones;
        wireado = false;
        WireBotones();
    }

    void WireBotones()
    {
        if (wireado || botones == null) return;
        for (int i = 0; i < botones.Length; i++)
        {
            int indice = i; // capturar por valor para el closure
            if (botones[i] != null)
                botones[i].onClick.AddListener(() => Seleccionar(indice));
        }
        wireado = true;
        ActualizarVisual();
    }

    public void Seleccionar(int indice)
    {
        if (botones == null || indice < 0 || indice >= botones.Length) return;
        indiceSeleccionado = indice;
        ActualizarVisual();
        OnSeleccionCambiada?.Invoke(indiceSeleccionado);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
    }

    void ActualizarVisual()
    {
        if (botones == null) return;
        for (int i = 0; i < botones.Length; i++)
        {
            if (botones[i] == null) continue;
            bool sel = (i == indiceSeleccionado);

            if (preservarColorBoton)
            {
                // Resalta con borde blanco + un pelín más grande, sin tapar el color del botón
                var outline = botones[i].GetComponent<Outline>();
                if (outline == null) outline = botones[i].gameObject.AddComponent<Outline>();
                outline.effectColor = Color.white;
                outline.effectDistance = new Vector2(5f, 5f);
                outline.enabled = sel;
                botones[i].transform.localScale = sel ? Vector3.one * 1.12f : Vector3.one;
            }
            else
            {
                var img = botones[i].GetComponent<Image>();
                if (img != null)
                    img.color = sel ? colorSeleccionado : colorNormal;
            }
        }
    }
}
