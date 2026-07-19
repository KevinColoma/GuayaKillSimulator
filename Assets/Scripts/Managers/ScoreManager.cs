using System.Collections.Generic;
using UnityEngine;

// Tabla de puntuaciones de los jugadores. Persiste con PlayerPrefs (sobrevive entre
// sesiones) y guarda el Top 10 ordenado por días sobrevividos y pacientes salvados.
// Clase estática: no necesita GameObject ni wiring en la escena.
public static class ScoreManager
{
    const string Key = "scores_v1";
    const int MaxScores = 10;
    const char SepCampos = '';  // unit separator: no aparece en nombres escritos
    const char SepFilas = '\n';

    public class ScoreEntry
    {
        public string nombre;
        public int dias;
        public int salvados;
        public string fecha;

        // Puntaje numérico para ordenar: los días pesan mucho más que los salvados sueltos.
        public int Puntaje => dias * 100 + salvados;
    }

    // Registra una partida terminada. Ignora partidas vacías (nadie atendido, 0 días).
    public static void GuardarScore(string nombre, int dias, int salvados)
    {
        if (dias <= 0 && salvados <= 0) return;

        var lista = GetScores();
        lista.Add(new ScoreEntry
        {
            nombre = Sanitizar(string.IsNullOrWhiteSpace(nombre) ? "Doc anónimo" : nombre),
            dias = dias,
            salvados = salvados,
            fecha = System.DateTime.Now.ToString("dd/MM/yy")
        });

        lista.Sort((a, b) => b.Puntaje.CompareTo(a.Puntaje));
        if (lista.Count > MaxScores) lista.RemoveRange(MaxScores, lista.Count - MaxScores);

        Guardar(lista);
    }

    public static List<ScoreEntry> GetScores()
    {
        var lista = new List<ScoreEntry>();
        string raw = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(raw)) return lista;

        foreach (var fila in raw.Split(SepFilas))
        {
            if (string.IsNullOrEmpty(fila)) continue;
            var campos = fila.Split(SepCampos);
            if (campos.Length < 4) continue;
            int dias, salvados;
            int.TryParse(campos[1], out dias);
            int.TryParse(campos[2], out salvados);
            lista.Add(new ScoreEntry { nombre = campos[0], dias = dias, salvados = salvados, fecha = campos[3] });
        }
        return lista;
    }

    public static void BorrarTodo()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }

    static void Guardar(List<ScoreEntry> lista)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < lista.Count; i++)
        {
            var e = lista[i];
            sb.Append(e.nombre).Append(SepCampos).Append(e.dias).Append(SepCampos)
              .Append(e.salvados).Append(SepCampos).Append(e.fecha);
            if (i < lista.Count - 1) sb.Append(SepFilas);
        }
        PlayerPrefs.SetString(Key, sb.ToString());
        PlayerPrefs.Save();
    }

    static string Sanitizar(string s)
    {
        return s.Replace(SepCampos, ' ').Replace(SepFilas, ' ').Trim();
    }

    // Texto formateado para el panel de Puntuaciones de la UI.
    public static string TextoPanel()
    {
        var lista = GetScores();
        if (lista.Count == 0)
            return "<i>Aún no hay puntuaciones.\nSobrevive un turno para aparecer aquí.</i>";

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < lista.Count; i++)
        {
            var e = lista[i];
            string pos = "<b>" + (i + 1) + ".</b>";
            sb.AppendLine($"{pos} {e.nombre}  —  <b>{e.dias}</b> días · <b>{e.salvados}</b> salvados  <size=55%>({e.fecha})</size>");
        }
        return sb.ToString();
    }
}
