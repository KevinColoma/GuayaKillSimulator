using UnityEngine;

// Generador procedural de pacientes. Consulta al DifficultyDirector (IA adaptativa)
// para decidir tipo de herida y severidad, y arma el paciente con nombre y
// diálogo absurdo aleatorios en jerga guayaca.
public static class PatientGenerator
{
    static readonly string[] Nombres =
    {
        "Don Washington", "La Ñaña Maritza", "El Cabezón Vera", "Doña Blanquita",
        "El Pite Zambrano", "Jendry Alexander", "La Mona Cecilia", "El Chino Farfán",
        "Mayerli del Rocío", "Don Colón", "El Flaco Quiñónez", "La Suca Torres",
        "Byron Stalin", "Doña Petita", "El Gato Mendoza", "Wellington Paúl",
        "El Loco Alcívar", "La Tere Bajaña", "Franklin Roosevelt Pin", "Doña Yolita",
        "El Pelado Macías", "Katiuska Del Cisne", "El Mocho Rendón", "La Gorda Salazar",
        "Kerly Anabel", "Don Chancho Reyes", "El Zurdo Bermúdez", "La Nena Intriago",
        "Kleber Washington", "El Ronco Palma", "Sandra Milena Loor", "El Pana Cedeño",
        "Doña Fanny", "El Tigre Suárez", "Yandry Emperatriz", "El Peluca Andrade",
        "La Vieja Consuelo", "Bryan Elvis Chalá", "El Cholo Rezabala", "Marjorie del Jesús"
    };

    static readonly string[] DialogosAbsurdos =
    {
        "Doc, el cuchillo era para pelar mangos, le juro.",
        "Me caí porque la acera está pa' llorar.",
        "Bajé de la moto esquivando un asalto.",
        "Solo fui a comprar pan, doc, el pan tuvo la culpa.",
        "La bala no era para mí, yo solo pasaba saludando.",
        "Mi suegra me dijo 'ven un ratito' y mire cómo terminé.",
        "Estaba bailando salsa choke y se me cruzó el machete.",
        "El semáforo estaba en verde... pa'l otro carro también.",
        "Yo no estaba en la bronca, la bronca vino a mí.",
        "Me mordió el perro del vecino y de la ira me caí solo.",
        "Aposté que aguantaba dos peleas, doc. Gané una.",
        "El taxi frenó y yo iba de parrillero sin casco, usted sabe.",
        "Estaba haciendo un TikTok en la terraza, no pregunte más.",
        "El billar se puso intenso, salieron las tacadas de verdad.",
        "Fui a cobrar una deuda y me pagaron con intereses.",
        "Doc, arregle rapidito que dejé la olla en la candela."
    };

    // Crea un paciente nuevo consultando los pesos en vivo de la IA de dificultad
    public static Patient GenerarPaciente()
    {
        var director = DifficultyDirector.Instance;
        int dia = director != null ? director.diaActual : 1;

        var paciente = new Patient();
        paciente.nombre = Nombres[Random.Range(0, Nombres.Length)];
        paciente.dialogoAbsurdo = DialogosAbsurdos[Random.Range(0, DialogosAbsurdos.Length)];
        paciente.tipoHerida = ElegirHerida(director);
        paciente.severidad = ElegirSeveridad(dia);

        // Vida inicial según severidad (mobile-friendly: números simples).
        // bloodLossPorSegundo NO se fija aquí: PatientManager.OnCuerpoAcostado lo recalcula
        // atado al tiempoLimite real (que solo se conoce cuando el paciente se acuesta), para
        // que el sangrado y el cronómetro visible siempre cuenten la misma historia.
        switch (paciente.severidad)
        {
            case Severidad.Leve: paciente.maxHealth = 100f; paciente.health = 65f; break;
            case Severidad.Moderado: paciente.maxHealth = 100f; paciente.health = 45f; break;
            default: paciente.maxHealth = 100f; paciente.health = 30f; break;
        }

        // A MAYOR NIVEL de la IA de dificultad, el paciente llega más grave: menos vida inicial
        // (necesita más curación = más herramientas/minijuegos).
        if (director != null)
            paciente.health *= Mathf.Lerp(1.0f, 0.6f, director.performanceScore); // hasta 40% menos vida inicial

        paciente.diagnostico = paciente.NombreHerida() + " (" + paciente.severidad + ")";
        return paciente;
    }

    static TipoHerida ElegirHerida(DifficultyDirector director)
    {
        // Pesos dinámicos de la IA; si no hay director, tabla del día 1
        WoundWeights pesos = director != null
            ? director.GetPesosHeridas()
            : new WoundWeights { bala = 0.4f, cuchillo = 0.3f, accidente = 0.3f };

        float total = pesos.bala + pesos.cuchillo + pesos.accidente;
        float r = Random.value * total;
        if (r < pesos.bala) return TipoHerida.Bala;
        if (r < pesos.bala + pesos.cuchillo) return TipoHerida.ArmaBlanca;
        return TipoHerida.Accidente;
    }

    static Severidad ElegirSeveridad(int dia)
    {
        // Días tempranos favorecen casos leves; desde el día 5 ya casi todo es serio
        float pCritico = Mathf.Clamp01(0.1f + dia * 0.06f);
        float pModerado = Mathf.Clamp01(0.3f + dia * 0.04f);
        float r = Random.value;
        if (r < pCritico) return Severidad.Critico;
        if (r < pCritico + pModerado) return Severidad.Moderado;
        return Severidad.Leve;
    }
}
