using UnityEngine;

public enum TipoHerida { Bala, ArmaBlanca, Accidente }
public enum Severidad { Leve, Moderado, Critico }

// Datos y lógica de un paciente. Clase serializable pura (no MonoBehaviour):
// PatientManager la instancia y la UI la lee. Compatible con object pooling futuro.
[System.Serializable]
public class Patient
{
    public string nombre;
    public TipoHerida tipoHerida;
    public Severidad severidad;
    public float health;
    public float maxHealth;
    [Tooltip("Vida perdida por segundo por sangrado")]
    public float bloodLossPorSegundo;
    public string diagnostico;
    public string dialogoAbsurdo;

    public bool EstaVivo() => health > 0f;
    // Umbral del 99% en vez de 100 exacto: el sangrado por frame haría imposible
    // alcanzar el máximo exacto (se descuenta antes del chequeo en Update).
    public bool EstaEstable() => health >= maxHealth * 0.99f;

    public void AplicarDanio(float cantidad)
    {
        health = Mathf.Max(0f, health - cantidad);
    }

    public void Curar(float cantidad)
    {
        health = Mathf.Min(maxHealth, health + cantidad);
    }

    // Herramienta correcta según el tipo de herida (documento de diseño):
    //   Bala -> Pinzas | Arma blanca -> Suturas | Accidente -> Torniquete
    public string HerramientaCorrecta()
    {
        switch (tipoHerida)
        {
            case TipoHerida.Bala: return "Pinzas";
            case TipoHerida.ArmaBlanca: return "Suturas";
            default: return "Torniquete";
        }
    }

    public string NombreHerida()
    {
        switch (tipoHerida)
        {
            case TipoHerida.Bala: return "Herida de bala";
            case TipoHerida.ArmaBlanca: return "Apuñalamiento";
            default: return "Accidente de tránsito";
        }
    }
}
