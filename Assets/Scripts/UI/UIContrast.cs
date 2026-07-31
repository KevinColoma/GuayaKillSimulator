using UnityEngine;

// Utilidad de legibilidad: elige el color de texto (blanco o casi negro) que MÁS
// contrasta con el fondo de un botón, usando la luminancia relativa de WCAG.
// Se usa en los botones que cambian de color en runtime (selección, toggles), donde
// un color de texto fijo acaba siendo ilegible sobre alguno de los estados.
public static class UIContrast
{
    public static readonly Color TextoClaro = Color.white;
    public static readonly Color TextoOscuro = new Color(0.06f, 0.06f, 0.07f, 1f);

    static float Canal(float v)
    {
        return v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
    }

    public static float Luminancia(Color c)
    {
        return 0.2126f * Canal(c.r) + 0.7152f * Canal(c.g) + 0.0722f * Canal(c.b);
    }

    public static float Contraste(Color a, Color b)
    {
        float la = Luminancia(a), lb = Luminancia(b);
        float hi = Mathf.Max(la, lb), lo = Mathf.Min(la, lb);
        return (hi + 0.05f) / (lo + 0.05f);
    }

    // Devuelve blanco o casi-negro, el que mejor se lea sobre 'fondo'.
    public static Color TextoLegible(Color fondo)
    {
        return Contraste(fondo, TextoClaro) >= Contraste(fondo, TextoOscuro)
            ? TextoClaro
            : TextoOscuro;
    }

    // Aplica el color legible a TODOS los textos hijos del botón (título y subtítulo).
    public static void AplicarATextos(Component raiz, Color fondo)
    {
        if (raiz == null) return;
        Color c = TextoLegible(fondo);
        foreach (var t in raiz.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
            t.color = c;
    }
}
