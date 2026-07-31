using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

// Inventario de herramientas médicas con usos limitados (documento de diseño).
// Usar la herramienta correcta para la herida cura fuerte; la incorrecta cura poco
// y cuenta como error (alimenta a la IA de dificultad vía PatientManager).
public class MedicalToolsManager : MonoBehaviour
{
    public static MedicalToolsManager Instance { get; private set; }

    [System.Serializable]
    public class Tool
    {
        public string nombre;
        public int cantidad;
        public int cantidadMaxima;
        public bool reutilizable;
        public float curacion;
    }

    public List<Tool> herramientas = new List<Tool>();

    [Tooltip("Distancia máxima (m) a la camilla para poder ATENDER/lanzar minijuego. Ver constantes = rango de enfoque del PatientManager.")]
    public float rangoAtencion = 3f;

    public event System.Action OnInventarioCambiado;
    public event System.Action<string, bool, string> OnHerramientaUsada; // nombre, éxito, mensaje

    public void NotificarCambioInventario() => OnInventarioCambiado?.Invoke();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ConfigurarInventarioInicial();
    }

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnDayEnded += _ => RestockTools();
        StartCoroutine(CicloPickups());
    }

    IEnumerator CicloPickups()
    {
        float spawnY = 0.8f;
        while (true)
        {
            var pickups = GameObject.FindObjectsByType<ToolPickup>(FindObjectsSortMode.None);
            int actuales = pickups.Length;
            int objetivo = 50;
            int enPrimerPiso = 0;
            foreach (var p in pickups)
                if (Mathf.Abs(p.transform.position.y - spawnY) < 1.5f) enPrimerPiso++;
            if (actuales < objetivo)
            {
                int restantes = objetivo - actuales;
                int necesariosP1 = 30 - enPrimerPiso;
                for (int i = 0; i < restantes; i++)
                {
                    float rango = (i < necesariosP1) ? 40f : 50f;
                    Vector3 origen = new Vector3(
                        Random.Range(-rango, rango), 5f, Random.Range(-rango, rango));
                    if (NavMesh.SamplePosition(origen, out var hit, 15f, NavMesh.AllAreas))
                    {
                        bool enP1 = Mathf.Abs(hit.position.y) < 1f;
                        if (i < necesariosP1 && !enP1) continue;
                        ToolPickup.Crear(hit.position + Vector3.up * spawnY);
                    }
                }
            }
            yield return new WaitForSeconds(4f);
        }
    }

    // Público y re-llamable: al iniciar el turno se vuelve a invocar con la personalización
    // ya elegida, para que el bono de inventario por experiencia se aplique de verdad
    // (en Awake, RunConfig aún tiene los valores por defecto).
    public void ConfigurarInventarioInicial()
    {
        // Bono de suministros de arranque según la experiencia del pasante
        int bono = RunConfig.BonusInventarioExperiencia();

        herramientas.Clear();
        herramientas.Add(new Tool { nombre = "Gasas",      cantidad = 5 + bono, cantidadMaxima = 5 + bono, reutilizable = false, curacion = 12f });
        herramientas.Add(new Tool { nombre = "Alcohol",    cantidad = 3 + bono, cantidadMaxima = 3 + bono, reutilizable = false, curacion = 8f });
        herramientas.Add(new Tool { nombre = "Pinzas",     cantidad = 1, cantidadMaxima = 1, reutilizable = true,  curacion = 30f });
        herramientas.Add(new Tool { nombre = "Suturas",    cantidad = 4 + bono, cantidadMaxima = 4 + bono, reutilizable = false, curacion = 30f });
        herramientas.Add(new Tool { nombre = "Torniquete", cantidad = 2 + bono, cantidadMaxima = 2 + bono, reutilizable = false, curacion = 30f });
        herramientas.Add(new Tool { nombre = "Kit",        cantidad = 1, cantidadMaxima = 1, reutilizable = false, curacion = 20f });
        herramientas.Add(new Tool { nombre = "Oración",    cantidad = 999, cantidadMaxima = 999, reutilizable = true, curacion = 0f });

        OnInventarioCambiado?.Invoke();
    }

    public Tool GetTool(string nombre) => herramientas.Find(t => t.nombre == nombre);

    public int GetToolQuantity(string nombre)
    {
        var t = GetTool(nombre);
        return t != null ? t.cantidad : 0;
    }

    public void RestockTools()
    {
        foreach (var t in herramientas)
            t.cantidad = t.cantidadMaxima;
        OnInventarioCambiado?.Invoke();
        Debug.Log("[MedicalTools] Inventario reabastecido para el nuevo día.");
        if (UIManager.Instance != null)
            UIManager.Instance.MostrarNarrador("Suministros reabastecidos. Aprovéchalos, que aquí nada dura.", 4f);
    }

    // Núcleo del sistema: aplicar una herramienta al paciente actual
bool EsHerramientaDePrecision(string nombre)
    {
        return nombre == "Pinzas" || nombre == "Suturas" || nombre == "Torniquete";
    }

    // Paciente de alto riesgo: crítico o casi muerto. Candidato a complicación inmediata.
    bool EsPacienteRiesgo(Patient p)
    {
        return p.severidad == Severidad.Critico || p.health < p.maxHealth * 0.35f;
    }

    // Todo paciente en Dificil/Infernal se complica tras un procedimiento exitoso.
    // En Normal hay 50% de probabilidad si es de alto riesgo.
    // El minijuego sorpresa se elige al azar entre Gasas, Alcohol y Kit.
    bool RequiereComplicacion(Patient p)
    {
        var director = DifficultyDirector.Instance;
        if (director == null) return false;
        if (director.currentTier == DifficultyTier.Dificil || director.currentTier == DifficultyTier.Infernal)
            return true;
        if (director.currentTier == DifficultyTier.Normal)
            return EsPacienteRiesgo(p) && Random.value < 0.5f;
        return false;
    }

    // Colores del banner de aviso del minijuego
    static readonly Color ColorAvisoRiesgo = new Color(0.85f, 0.5f, 0.05f, 0.95f);   // naranja
    static readonly Color ColorAvisoComplicacion = new Color(0.8f, 0.1f, 0.1f, 0.95f); // rojo

    // Si el paciente puede complicarse, avisar desde el PRIMER procedimiento
    // con un banner arriba del minijuego (para que el jugador sepa que puede venir otro).
    void AvisarSiRiesgoAlto(Patient p)
    {
        if (EsPacienteRiesgo(p) && MiniGameManager.Instance != null && DifficultyDirector.Instance != null
            && DifficultyDirector.Instance.currentTier >= DifficultyTier.Dificil)
            MiniGameManager.Instance.AnunciarProximoProcedimiento(
                "⚠ PACIENTE DE ALTO RIESGO — puede requerir hasta " + MaxProcedimientos + " procedimientos", ColorAvisoRiesgo);
    }

    static readonly string[] MinijuegosComplicacion = { "Gasas", "Alcohol", "Kit" };

    const int MaxProcedimientos = 3;

    // Cierre común de un procedimiento exitoso: o estabiliza al paciente, o (en Infernal
    // con riesgo alto) encadena una complicación con otro minijuego sorpresa.
    // Puede complicarse VARIAS veces (50% cada vez), hasta un máximo de 3 procedimientos.
    void ResolverProcedimientoExitoso(string nombre, PatientManager.Slot slotRef, Patient pacienteRef, float curacionFinal)
    {
        ResolverProcedimientoExitoso(nombre, slotRef, pacienteRef, curacionFinal, 1);
    }

    void ResolverProcedimientoExitoso(string nombre, PatientManager.Slot slotRef, Patient pacienteRef, float curacionFinal, int procedimientoActual)
    {
        var pm = PatientManager.Instance;

        if (procedimientoActual < MaxProcedimientos && RequiereComplicacion(pacienteRef) && MiniGameManager.Instance != null)
        {
            int siguiente = procedimientoActual + 1;
            string comp = MinijuegosComplicacion[Random.Range(0, MinijuegosComplicacion.Length)];
            OnHerramientaUsada?.Invoke(nombre, true, "¡COMPLICACIÓN! " + pacienteRef.nombre + " se desestabiliza. ¡Otro procedimiento, ya!");
            slotRef.minijuegoActivo = true;
            OnInventarioCambiado?.Invoke();

            string aviso = siguiente < MaxProcedimientos
                ? "🚨 ¡COMPLICACIÓN! — PROCEDIMIENTO " + siguiente + " (puede complicarse otra vez)"
                : "🚨 ¡SE COMPLICA OTRA VEZ! — PROCEDIMIENTO " + siguiente + " (ÚLTIMO)";
            MiniGameManager.Instance.AnunciarProximoProcedimiento(aviso, ColorAvisoComplicacion);

            MiniGameManager.Instance.JugarHerramienta(comp, pacienteRef, res =>
            {
                slotRef.minijuegoActivo = false;
                if (slotRef.paciente != pacienteRef) return;
                if (res.success)
                {
                    // Recursivo: tras superar esta complicación puede venir OTRA (hasta el tope)
                    ResolverProcedimientoExitoso(nombre, slotRef, pacienteRef, curacionFinal, siguiente);
                }
                else
                {
                    pacienteRef.AplicarDanio(res.damageIfFailed);
                    slotRef.errores++;
                    OnHerramientaUsada?.Invoke(nombre, false, res.failureMessage);
                    OnInventarioCambiado?.Invoke();
                }
            });
            return;
        }

        pacienteRef.Curar(curacionFinal);
        string msg = procedimientoActual > 1
            ? "Complicación controlada tras " + procedimientoActual + " procedimientos. Sobrevivió de milagro."
            : nombre + ": procedimiento exitoso.";
        OnHerramientaUsada?.Invoke(nombre, true, msg);
        OnInventarioCambiado?.Invoke();
        if (pacienteRef.EstaEstable() && pm != null) pm.ResolverSlot(slotRef, true);
    }


    public bool UseTool(string nombre)
    {
        var pm = PatientManager.Instance;
        var slot = pm != null ? pm.slotEnfocado : null;
        if (slot == null || slot.paciente == null)
        {
            OnHerramientaUsada?.Invoke(nombre, false, "Acércate a un paciente en una camilla.");
            return false;
        }

        var tool = GetTool(nombre);
        if (tool == null) return false;

        if (tool.cantidad <= 0)
        {
            OnHerramientaUsada?.Invoke(nombre, false, "¡Se acabaron las " + nombre + "!");
            return false;
        }

        var paciente = slot.paciente;

        // Solo se puede ATENDER (y lanzar minijuego) si el jugador está CERCA de esta camilla.
        // Ver constantes de lejos sí (triaje), pero tratar requiere estar pegado al paciente.
        var cam = Camera.main;
        if (cam != null && slot.estacion != null)
        {
            float dist = Vector3.Distance(cam.transform.position, slot.estacion.PuntoAcostado());
            if (dist > rangoAtencion)
            {
                OnHerramientaUsada?.Invoke(nombre, false, "Acércate más a " + paciente.nombre + " para atenderlo.");
                return false;
            }
        }

        // CASO ESPECIAL: Oración — funciona incluso durante un minijuego en curso
        if (nombre == "Oración")
        {
            float probabilidad = RunConfig.rasgoElegido == RasgoInicial.Creyente ? 0.30f : 0.05f;
            if (Random.value < probabilidad)
            {
                paciente.Curar(paciente.maxHealth);
                OnHerramientaUsada?.Invoke(nombre, true, "¡MILAGRO! El paciente se levanta como nuevo.");
                OnInventarioCambiado?.Invoke();
                pm.ResolverSlot(slot, true);
                return true;
            }
            else
            {
                slot.errores++;
                OnHerramientaUsada?.Invoke(nombre, false, "Dios está ocupado con otro barrio. Sigue sangrando.");
            }
            OnInventarioCambiado?.Invoke();
            return true;
        }

        // No permitir usar herramientas mientras un minijuego está activo
        if (MiniGameManager.Instance != null && MiniGameManager.Instance.EnCurso)
        {
            OnHerramientaUsada?.Invoke(nombre, false, "Termina el procedimiento primero.");
            return false;
        }

        if (!tool.reutilizable) tool.cantidad--;

        bool esCorrecta = paciente.HerramientaCorrecta() == nombre;
        float curacion = tool.curacion;

        // Rasgo "Manos firmes": las pinzas curan 25% más (menos temblor)
        if (nombre == "Pinzas" && RunConfig.rasgoElegido == RasgoInicial.ManosFirmes)
            curacion *= 1.25f;

        // Experiencia del pasante: cura un poco más en cada aplicación
        curacion *= RunConfig.MultiplicadorCuracionExperiencia();

        // Herramienta de precisión + herida correcta -> lanzar minijuego (no cura directo)
        if (esCorrecta && EsHerramientaDePrecision(nombre) && MiniGameManager.Instance != null)
        {
            var slotRef = slot;
            var pacienteRef = paciente;
            slotRef.minijuegoActivo = true;
            OnInventarioCambiado?.Invoke();
            AvisarSiRiesgoAlto(pacienteRef);
            MiniGameManager.Instance.Jugar(pacienteRef.tipoHerida, pacienteRef, resultado =>
            {
                slotRef.minijuegoActivo = false;
                if (slotRef.paciente != pacienteRef) return; // ya se resolvió/cambió el paciente
                if (resultado.success)
                {
                    ResolverProcedimientoExitoso(nombre, slotRef, pacienteRef, pacienteRef.maxHealth);
                }
                else
                {
                    pacienteRef.AplicarDanio(resultado.damageIfFailed);
                    slotRef.errores++;
                    OnHerramientaUsada?.Invoke(nombre, false, resultado.failureMessage);
                    OnInventarioCambiado?.Invoke();
                }
            });
            return true;
        }

        if (esCorrecta)
        {
            paciente.Curar(curacion);
            OnHerramientaUsada?.Invoke(nombre, true, nombre + " aplicada: justo lo que necesitaba.");
        }
        else
        {
            // Herramienta equivocada: cura poco y cuenta como error de procedimiento
            paciente.Curar(curacion * 0.3f);
            slot.errores++;
            OnHerramientaUsada?.Invoke(nombre, false, nombre + "... no era eso, pero algo ayuda.");
        }

        OnInventarioCambiado?.Invoke();

        // Resolver de inmediato si quedó estable (no esperar al siguiente frame,
        // donde el sangrado volvería a bajarlo del umbral)
        if (paciente.EstaEstable())
            pm.ResolverSlot(slot, true);

        return true;
    }
}
