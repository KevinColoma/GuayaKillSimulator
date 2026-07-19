using UnityEngine;

public enum NivelExperiencia { Novato, Intermedio, Experimentado }
public enum RasgoInicial { ManosFirmes, Creyente, ResistenteAlEstres }

// Guarda la configuración del avatar/turno elegida en la pantalla de Personalización,
// para que la consuman los sistemas de gameplay (PatientGenerator, MedicalToolsManager, etc.)
// cuando se construyan en la Fase 2. Datos vivos durante la sesión, no se serializan a disco.
public static class RunConfig
{
    public static string nombreJugador = "Doc";
    public static int colorUniformeIndex = 0;
    public static bool usaMascarilla = true;
    public static bool usaGuantes = true;
    public static NivelExperiencia nivelExperiencia = NivelExperiencia.Novato;
    public static RasgoInicial rasgoElegido = RasgoInicial.ManosFirmes;

    public static void Resetear()
    {
        nombreJugador = "Doc";
        colorUniformeIndex = 0;
        usaMascarilla = true;
        usaGuantes = true;
        nivelExperiencia = NivelExperiencia.Novato;
        rasgoElegido = RasgoInicial.ManosFirmes;
    }

    // ------------------------- Efectos de la personalización en el gameplay -------------------------
    // Fuente única de verdad: los managers (MedicalTools, MiniGame, DifficultyDirector)
    // leen estos valores derivados en vez de duplicar la lógica de cada opción.

    // Nivel de experiencia -> cura mejor (más años, mejor pulso general).
    //   Novato 1.0 · Intermedio 1.1 · Experimentado 1.2
    public static float MultiplicadorCuracionExperiencia()
    {
        switch (nivelExperiencia)
        {
            case NivelExperiencia.Intermedio: return 1.1f;
            case NivelExperiencia.Experimentado: return 1.2f;
            default: return 1f;
        }
    }

    // Nivel de experiencia -> más suministros de arranque (sabe qué stockear).
    //   Novato +0 · Intermedio +1 · Experimentado +2  (a las herramientas consumibles)
    public static int BonusInventarioExperiencia()
    {
        switch (nivelExperiencia)
        {
            case NivelExperiencia.Intermedio: return 1;
            case NivelExperiencia.Experimentado: return 2;
            default: return 0;
        }
    }

    // Guantes -> mejor agarre, menos temblor en los minijuegos de precisión (0.8x).
    // Se combina con el rasgo "Manos firmes" (que aplica su propio factor aparte).
    public static float FactorTemblorGuantes()
    {
        return usaGuantes ? 0.8f : 1f;
    }

    // Mascarilla -> menos riesgo de que el paciente se "infecte"/complique en Infernal.
    //   Con mascarilla 35% · sin mascarilla 50%
    public static float ProbabilidadComplicacion()
    {
        return usaMascarilla ? 0.35f : 0.5f;
    }

    // Paleta de colores del uniforme (índices 0-3 = los 4 swatches del menú).
    public static Color ColorUniforme() => ColorUniforme(colorUniformeIndex);

    public static Color ColorUniforme(int indice)
    {
        switch (indice)
        {
            case 1: return new Color(0.85f, 0.85f, 0.9f);   // blanco clínico
            case 2: return new Color(0.15f, 0.55f, 0.35f);  // verde quirófano
            case 3: return new Color(0.7f, 0.2f, 0.2f);     // rojo
            default: return new Color(0.2f, 0.4f, 0.75f);   // azul (por defecto)
        }
    }

    public static string NombreColorUniforme()
    {
        switch (colorUniformeIndex)
        {
            case 1: return "blanco clínico";
            case 2: return "verde quirófano";
            case 3: return "rojo";
            default: return "azul";
        }
    }

    public static string NombreNivel()
    {
        switch (nivelExperiencia)
        {
            case NivelExperiencia.Intermedio: return "Intermedio";
            case NivelExperiencia.Experimentado: return "Experimentado";
            default: return "Novato";
        }
    }

    public static string NombreRasgo()
    {
        switch (rasgoElegido)
        {
            case RasgoInicial.Creyente: return "Creyente";
            case RasgoInicial.ResistenteAlEstres: return "Resistente al estrés";
            default: return "Manos firmes";
        }
    }
}
