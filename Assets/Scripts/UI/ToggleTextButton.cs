using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Botón binario auto-contenido (ej. "Mascarilla: SI/NO"). Cambia su propio texto y color
// al hacer clic; no necesita coordinarse con otros botones (a diferencia de SingleSelectButtonGroup).
public class ToggleTextButton : MonoBehaviour
{
    public string etiquetaBase = "Opción";
    public bool valorInicial = true;
    public bool valorActual { get; private set; }

    public Color colorActivo = new Color(0.15f, 0.95f, 0.35f, 1f);
    public Color colorInactivo = new Color(0.4f, 0.15f, 0.15f, 1f);

    TextMeshProUGUI label;
    Image fondo;
    Button boton;

    void Awake()
    {
        boton = GetComponent<Button>();
        fondo = GetComponent<Image>();
        label = GetComponentInChildren<TextMeshProUGUI>();
        valorActual = valorInicial;
    }

    void Start()
    {
        boton.onClick.AddListener(Alternar);
        ActualizarVisual();
    }

    public void Alternar()
    {
        valorActual = !valorActual;
        ActualizarVisual();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
    }

    void ActualizarVisual()
    {
        if (label != null)
            label.text = etiquetaBase + ": " + (valorActual ? "SÍ" : "NO");
        if (fondo != null)
        {
            fondo.color = valorActual ? colorActivo : colorInactivo;
            // Texto siempre legible: el verde activo es muy claro (pide letra oscura)
            // y el rojo inactivo es oscuro (pide letra blanca).
            UIContrast.AplicarATextos(this, fondo.color);
        }
    }
}
