using UnityEngine;

// Gestor de audio (Fase 6). Genera los efectos POR CÓDIGO (ondas sintetizadas), así
// que no hace falta importar archivos de sonido. Se engancha a los eventos del juego:
// alarma al llegar un paciente crítico, "ding" al salvar, "buzz" al perder, click al usar
// herramientas, y un latido que se acelera cuando el paciente enfocado está por morir.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Volúmenes")]
    [Range(0f, 1f)] public float volumenSFX = 0.6f;
    [Range(0f, 1f)] public float volumenMusica = 0.35f;

    [Tooltip("Música de fondo opcional. Si la asignas, suena en bucle. (No obligatorio.)")]
    public AudioClip musicaFondo;

    AudioSource sfxSource;
    AudioSource musicSource;
    AudioSource latidoSource;

    AudioClip clipClick, clipDing, clipBuzz, clipAlarma, clipLatido;
    float proximoLatido;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();
        latidoSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        musicSource.playOnAwake = false; musicSource.loop = true;
        latidoSource.playOnAwake = false;

        // Sintetizar los efectos
        clipClick  = GenerarTono("click", 900f, 0.06f, 0.5f, true, 45f);
        clipDing   = GenerarAcorde("ding", new float[]{ 660f, 990f }, 0.28f, 0.5f, 6f);   // agudo agradable
        clipBuzz   = GenerarTono("buzz", 130f, 0.45f, 0.5f, true, 5f);                     // grave desagradable
        clipAlarma = GenerarTono("alarma", 780f, 0.18f, 0.5f, true, 8f);                   // beep de urgencia
        clipLatido = GenerarTono("latido", 90f, 0.12f, 0.7f, false, 22f);                  // pum del corazón
    }

    void Start()
    {
        // Cargar volúmenes guardados (persisten entre sesiones)
        volumenSFX = PlayerPrefs.GetFloat("vol_sfx", volumenSFX);
        volumenMusica = PlayerPrefs.GetFloat("vol_musica", volumenMusica);

        if (musicaFondo != null)
        {
            musicSource.clip = musicaFondo;
            musicSource.volume = volumenMusica;
            musicSource.Play();
        }

        if (PatientManager.Instance != null)
        {
            PatientManager.Instance.OnPacienteLlega += p =>
            {
                if (p.severidad == Severidad.Critico) PlayAlarma();
            };
            PatientManager.Instance.OnPacienteResuelto += (p, salvado) =>
            {
                if (salvado) PlaySuccess(); else PlayFail();
            };
        }
        if (MedicalToolsManager.Instance != null)
            MedicalToolsManager.Instance.OnHerramientaUsada += (h, exito, msg) => PlayClick();
    }

    void Update()
    {
        // Latido del paciente enfocado: se acelera cuando le queda poca vida/tiempo
        var pm = PatientManager.Instance;
        if (pm == null || pm.slotEnfocado == null || pm.slotEnfocado.paciente == null) return;

        var slot = pm.slotEnfocado;
        float pct = slot.paciente.health / slot.paciente.maxHealth;
        if (pct > 0.6f) return; // solo cuando está en riesgo

        float intervalo = Mathf.Lerp(0.35f, 1.1f, pct); // menos vida = latido más rápido
        if (Time.unscaledTime >= proximoLatido)
        {
            proximoLatido = Time.unscaledTime + intervalo;
            latidoSource.PlayOneShot(clipLatido, volumenSFX * (pct < 0.3f ? 1f : 0.7f));
        }
    }

    // ------------------------- API de volumen (la usan los sliders de Ajustes) -------------------------
    public void SetVolumenSFX(float valor)
    {
        volumenSFX = Mathf.Clamp01(valor);
        PlayerPrefs.SetFloat("vol_sfx", volumenSFX);
        // feedback inmediato: un click de muestra al mover el slider
        if (sfxSource != null && clipClick != null) sfxSource.PlayOneShot(clipClick, volumenSFX);
    }

    public void SetVolumenMusica(float valor)
    {
        volumenMusica = Mathf.Clamp01(valor);
        PlayerPrefs.SetFloat("vol_musica", volumenMusica);
        if (musicSource != null) musicSource.volume = volumenMusica; // aplicar en vivo a la música que suena
        // feedback audible al mover el slider (por si no hay música sonando)
        if (sfxSource != null && clipClick != null) sfxSource.PlayOneShot(clipClick, Mathf.Max(0.2f, volumenSFX));
    }

    // ------------------------- API -------------------------
    public void PlayClick() { sfxSource.PlayOneShot(clipClick, volumenSFX); }
    public void PlaySuccess() { sfxSource.PlayOneShot(clipDing, volumenSFX); }
    public void PlayFail() { sfxSource.PlayOneShot(clipBuzz, volumenSFX); }
    public void PlayAlarma()
    {
        // Doble beep de alarma
        sfxSource.PlayOneShot(clipAlarma, volumenSFX);
        Invoke(nameof(SegundoBeep), 0.22f);
    }
    void SegundoBeep() { sfxSource.PlayOneShot(clipAlarma, volumenSFX); }

    // ------------------------- Síntesis -------------------------
    AudioClip GenerarTono(string nombre, float freq, float dur, float volumen, bool cuadrada, float decay)
    {
        int sampleRate = 44100;
        int samples = Mathf.Max(1, (int)(sampleRate * dur));
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float env = Mathf.Exp(-t * decay);
            float s = Mathf.Sin(2f * Mathf.PI * freq * t);
            float wave = cuadrada ? Mathf.Sign(s) : s;
            data[i] = wave * volumen * env;
        }
        var clip = AudioClip.Create(nombre, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    AudioClip GenerarAcorde(string nombre, float[] freqs, float dur, float volumen, float decay)
    {
        int sampleRate = 44100;
        int samples = Mathf.Max(1, (int)(sampleRate * dur));
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float env = Mathf.Exp(-t * decay);
            float sum = 0f;
            for (int f = 0; f < freqs.Length; f++)
                sum += Mathf.Sin(2f * Mathf.PI * freqs[f] * t);
            data[i] = (sum / freqs.Length) * volumen * env;
        }
        var clip = AudioClip.Create(nombre, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
