using UnityEngine;
using System.Collections.Generic;

// Sistema de logros de supervivencia (Fase 5). Escucha eventos de otros sistemas
// (pacientes salvados, días sobrevividos, suturas, heridas de arma blanca) y
// desbloquea medallas irónicas. Persiste con PlayerPrefs entre sesiones.
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    public enum Metrica { PacientesSalvados, DiasSobrevividos, SuturasExitosas, PacientesArmaBlanca }

    [System.Serializable]
    public class Achievement
    {
        public string id;
        public string title;
        public string description;
        public Metrica metrica;
        public int requirement;
        public bool unlocked;
    }

    [Tooltip("Los 5 logros del documento de diseño.")]
    public List<Achievement> achievements = new List<Achievement>();

    // Contadores acumulados (persistentes)
    readonly Dictionary<Metrica, int> progreso = new Dictionary<Metrica, int>();

    public event System.Action<Achievement> OnLogroDesbloqueado;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (achievements.Count == 0) ConfigurarLogros();
        CargarProgreso();
    }

    void Start()
    {
        // Suscribirse a los eventos de los otros sistemas (patrón Observer)
        if (DifficultyDirector.Instance != null)
            DifficultyDirector.Instance.OnPerformanceUpdated += _ => { }; // placeholder por si se necesita
        if (PatientManager.Instance != null)
            PatientManager.Instance.OnPacienteResuelto += OnPacienteResuelto;
        if (GameManager.Instance != null)
            GameManager.Instance.OnDayEnded += OnDiaTerminado;
    }

    void ConfigurarLogros()
    {
        achievements.Add(new Achievement { id = "pulso_firme", title = "Ya no te tiembla el pulso", description = "Salva 5 pacientes", metrica = Metrica.PacientesSalvados, requirement = 5 });
        achievements.Add(new Achievement { id = "medico_combate", title = "Médico de combate", description = "Sobrevive 10 días", metrica = Metrica.DiasSobrevividos, requirement = 10 });
        achievements.Add(new Achievement { id = "suturador", title = "Suturador de emergencia", description = "20 suturas exitosas", metrica = Metrica.SuturasExitosas, requirement = 20 });
        achievements.Add(new Achievement { id = "psicologo_borrachos", title = "Psicólogo de borrachos", description = "15 pacientes con arma blanca", metrica = Metrica.PacientesArmaBlanca, requirement = 15 });
        achievements.Add(new Achievement { id = "leyenda_uci", title = "Leyenda Viva de la UCI Barrial", description = "Sobrevive 50 días", metrica = Metrica.DiasSobrevividos, requirement = 50 });
    }

    // ------------------------- Eventos entrantes -------------------------

    void OnPacienteResuelto(Patient p, bool salvado)
    {
        if (!salvado) return;
        Registrar(Metrica.PacientesSalvados, 1);
        if (p.tipoHerida == TipoHerida.ArmaBlanca)
        {
            Registrar(Metrica.PacientesArmaBlanca, 1);
            Registrar(Metrica.SuturasExitosas, 1); // salvar un apuñalamiento = sutura exitosa
        }
    }

    void OnDiaTerminado(int diasSobrevividos)
    {
        SetMetrica(Metrica.DiasSobrevividos, diasSobrevividos);
    }

    // ------------------------- Núcleo -------------------------

    public void Registrar(Metrica m, int cantidad)
    {
        SetMetrica(m, GetProgreso(m) + cantidad);
    }

    void SetMetrica(Metrica m, int valor)
    {
        progreso[m] = valor;
        PlayerPrefs.SetInt("ach_metric_" + (int)m, valor);
        CheckAchievements();
    }

    public int GetProgreso(Metrica m)
    {
        return progreso.ContainsKey(m) ? progreso[m] : 0;
    }

    public void CheckAchievements()
    {
        foreach (var a in achievements)
        {
            if (a.unlocked) continue;
            if (GetProgreso(a.metrica) >= a.requirement)
                UnlockAchievement(a.id);
        }
    }

    public void UnlockAchievement(string id)
    {
        var a = achievements.Find(x => x.id == id);
        if (a == null || a.unlocked) return;
        a.unlocked = true;
        PlayerPrefs.SetInt("ach_unlocked_" + a.id, 1);
        PlayerPrefs.Save();
        Debug.Log("[Logros] ¡Desbloqueado! " + a.title);
        OnLogroDesbloqueado?.Invoke(a);
    }

    void CargarProgreso()
    {
        foreach (Metrica m in System.Enum.GetValues(typeof(Metrica)))
            progreso[m] = PlayerPrefs.GetInt("ach_metric_" + (int)m, 0);
        foreach (var a in achievements)
            a.unlocked = PlayerPrefs.GetInt("ach_unlocked_" + a.id, 0) == 1;
    }

    // Texto formateado para el panel de Logros de la UI
    public string TextoPanel()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var a in achievements)
        {
            string icono = a.unlocked ? "★" : "☆";
            int prog = Mathf.Min(GetProgreso(a.metrica), a.requirement);
            sb.AppendLine(icono + "  <b>" + a.title + "</b>  <size=70%>(" + prog + "/" + a.requirement + ")</size>");
            sb.AppendLine("<size=60%>" + a.description + "</size>");
        }
        return sb.ToString();
    }
}
