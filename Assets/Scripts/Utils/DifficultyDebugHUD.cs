using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class DifficultyDebugHUD : MonoBehaviour
{
    public TextMeshProUGUI hudText;
    public bool showHUD = true;
    Transform canvasRaiz;

    void Start()
    {
        var canvasGO = GameObject.Find("MenuCanvas");
        if (canvasGO != null) canvasRaiz = canvasGO.transform;
        if (hudText == null && canvasRaiz != null)
        {
            var go = new GameObject("DifficultyHUD", typeof(RectTransform));
            go.transform.SetParent(canvasRaiz, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.02f, 0.15f);
            rt.anchorMax = new Vector2(0.45f, 0.85f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            hudText = go.AddComponent<TextMeshProUGUI>();
            hudText.fontSize = 22;
            hudText.alignment = TextAlignmentOptions.TopLeft;
            hudText.color = Color.white;
        }
    }

    void Update()
    {
        var kb = Keyboard.current;
        var d = DifficultyDirector.Instance;
        var gm = GameManager.Instance;
        if (kb == null || d == null) return;

        if (kb.gKey.wasPressedThisFrame)
        {
            d.RegistrarResultadoPaciente(salvado: true, tiempoSegundos: 8f, tiempoLimite: 30f, erroresCometidos: 0);
            if (gm != null) gm.RegistrarPacienteResuelto(true);
        }

        if (kb.hKey.wasPressedThisFrame)
        {
            d.RegistrarResultadoPaciente(salvado: true, tiempoSegundos: 26f, tiempoLimite: 30f, erroresCometidos: 2);
            if (gm != null) gm.RegistrarPacienteResuelto(true);
        }

        if (kb.pKey.wasPressedThisFrame)
        {
            d.RegistrarResultadoPaciente(salvado: false, tiempoSegundos: 30f, tiempoLimite: 30f, erroresCometidos: 3);
            if (gm != null) gm.RegistrarPacienteResuelto(false);
        }

        if (kb.nKey.wasPressedThisFrame)
        {
            if (gm != null) gm.EndDay();
        }

        if (hudText != null)
        {
            if (!showHUD || Time.timeScale > 0f)
            {
                hudText.text = "";
                return;
            }

            if (canvasRaiz != null && hudText.transform.parent != canvasRaiz)
                hudText.transform.SetParent(canvasRaiz, false);
            hudText.transform.SetAsLastSibling();
            var pesos = d.GetPesosHeridas();
            hudText.text =
                "<b>IA — DIRECTOR DE DIFICULTAD</b>\n" +
                $"Score: {d.performanceScore:F2}   Nivel: <color=yellow>{d.currentTier}</color>   Día: {d.diaActual}\n" +
                $"Salvados: {d.pacientesSalvados}   Perdidos: {d.pacientesPerdidos}\n" +
                $"Heridas → Bala {pesos.bala:P0} | Cuchillo {pesos.cuchillo:P0} | Accidente {pesos.accidente:P0}\n" +
                $"Próximo paciente en: {d.GetIntervaloAparicion():F0}s   Tiempo por paciente: {d.GetTiempoLimitePacienteSegundos():F0}s (x{d.GetMultiplicadorTiempoLimite():F2})\n" +
                "<size=70%>[G] salvar rápido  [H] salvar lento  [P] perder  [N] día siguiente</size>";
        }
    }
}
