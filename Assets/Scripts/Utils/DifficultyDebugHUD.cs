using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// HUD de prueba para el DifficultyDirector (IA de dificultad adaptativa).
// Durante el gameplay:
//   G = simular paciente SALVADO rápido y sin errores (buen desempeño)
//   H = simular paciente SALVADO lento y con errores (desempeño mediocre)
//   P = simular paciente PERDIDO (mal desempeño)
//   N = avanzar al siguiente día
// El panel muestra en vivo el score, nivel, pesos de heridas e intervalos.
// Quitar este componente (o desactivar showHUD) para builds finales.
public class DifficultyDebugHUD : MonoBehaviour
{
    public TextMeshProUGUI hudText;
    public bool showHUD = true;

    void Update()
    {
        var kb = Keyboard.current;
        var d = DifficultyDirector.Instance;
        if (kb == null || d == null) return;

        if (kb.gKey.wasPressedThisFrame)
            d.RegistrarResultadoPaciente(salvado: true, tiempoSegundos: 8f, tiempoLimite: 30f, erroresCometidos: 0);

        if (kb.hKey.wasPressedThisFrame)
            d.RegistrarResultadoPaciente(salvado: true, tiempoSegundos: 26f, tiempoLimite: 30f, erroresCometidos: 2);

        if (kb.pKey.wasPressedThisFrame)
            d.RegistrarResultadoPaciente(salvado: false, tiempoSegundos: 30f, tiempoLimite: 30f, erroresCometidos: 3);

        if (kb.nKey.wasPressedThisFrame)
            d.AvanzarDia();

        if (hudText != null)
        {
            if (!showHUD)
            {
                hudText.text = "";
                return;
            }

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
