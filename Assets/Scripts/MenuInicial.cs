using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuInicial : MonoBehaviour
{
    // ====================== SEGUNDO SCRIPT (BASE) ======================
    [Header("Menú Principal")]
    public GameObject welcomeText;
    public GameObject readyButton;
    public GameObject menuButton;
    public GameObject canvasMenu;

    [Header("Menú de Pausa")]
    public GameObject pauseMenuPanel;
    public GameObject continueButton;
    public GameObject exitToMenuButton;
    public GameObject quitGameButton;

    [Header("Referencias del Jugador")]
    public GameObject playerGameObject;

    [Header("Configuración")]
    public float delayBeforeButtons = 2f;

    // ====================== PRIMER SCRIPT (NUEVO) ======================
    [Header("=== PANTALLAS DEL MENÚ (Nuevo) ===")]
    public GameObject pantallaBienvenida;
    public GameObject pantallaPersonalizacion;

    [Header("=== PERSONALIZACIÓN ===")]
    public TMP_InputField inputNombrePasante;

    [Header("Power-ups / Rasgos")]
    public GameObject botonManosFirmes;
    public GameObject botonCreyente;
    public GameObject botonResistenteEstres;

    public GameObject highlightManosFirmes;
    public GameObject highlightCreyente;
    public GameObject highlightResistente;

    // ====================== PRIVADOS ======================
    private FirstPersonLook playerController;
    private FirstPersonMovement playerMovement;
    private bool isGameActive = false;
    private bool isPaused = false;

    private string nombrePasante = "Dr. Guayaco";
    private string rasgoSeleccionado = "Ninguno";

    // ====================== INICIO ======================

    void Start()
    {
        if (playerGameObject != null)
        {
            playerController = playerGameObject.GetComponent<FirstPersonLook>();
            playerMovement = playerGameObject.GetComponent<FirstPersonMovement>();
        }

        SetCursorState(false);
        Time.timeScale = 1f;

        if (welcomeText != null) welcomeText.SetActive(true);
        if (readyButton != null) readyButton.SetActive(false);
        if (menuButton != null) menuButton.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);

        DesactivarTodasLasPantallas();
        if (pantallaBienvenida != null) pantallaBienvenida.SetActive(true);

        ResetearHighlights();
        SetPlayerControl(false);
        isGameActive = false;
        isPaused = false;

        StartCoroutine(ShowButtonsAfterDelay());
    }

    // ====================== UPDATE ======================

    void Update()
    {
        if (isGameActive && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    // ====================== CURSOR Y CONTROL ======================

    void SetCursorState(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        Debug.Log("Cursor: " + (locked ? "BLOQUEADO" : "LIBRE"));
    }

    void SetPlayerControl(bool enabled)
    {
        if (playerController != null) playerController.SetControlEnabled(enabled);
        if (playerMovement != null) playerMovement.SetControlEnabled(enabled);
    }

    // ====================== PAUSA ======================

    void TogglePause()
    {
        if (!isGameActive) return;
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            SetCursorState(false);
            SetPlayerControl(false);
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
            Debug.Log("Juego pausado");
        }
        else
        {
            Time.timeScale = 1f;
            SetCursorState(true);
            SetPlayerControl(true);
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            Debug.Log("Juego reanudado");
        }
    }

    // ====================== CORRUTINA ======================

    IEnumerator ShowButtonsAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeButtons);
        SetCursorState(false);
        if (readyButton != null) readyButton.SetActive(true);
        if (menuButton != null) menuButton.SetActive(true);
    }

    // ====================== PANTALLAS (Primer script) ======================

    public void MostrarPantallaBienvenida()
    {
        DesactivarTodasLasPantallas();
        if (pantallaBienvenida != null) pantallaBienvenida.SetActive(true);
    }

    public void MostrarPersonalizacion()
    {
        DesactivarTodasLasPantallas();
        if (pantallaPersonalizacion != null) pantallaPersonalizacion.SetActive(true);

        if (inputNombrePasante != null)
            inputNombrePasante.text = PlayerPrefs.GetString("NombrePasante", "Dr. Guayaco");

        ResetearHighlights();
    }

    private void DesactivarTodasLasPantallas()
    {
        if (pantallaBienvenida != null) pantallaBienvenida.SetActive(false);
        if (pantallaPersonalizacion != null) pantallaPersonalizacion.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    // ====================== POWER-UPS / RASGOS ======================

    public void SeleccionarManosFirmes()
    {
        rasgoSeleccionado = "ManosFirmes";
        ResetearHighlights();
        if (highlightManosFirmes != null) highlightManosFirmes.SetActive(true);
    }

    public void SeleccionarCreyente()
    {
        rasgoSeleccionado = "Creyente";
        ResetearHighlights();
        if (highlightCreyente != null) highlightCreyente.SetActive(true);
    }

    public void SeleccionarResistenteEstres()
    {
        rasgoSeleccionado = "ResistenteEstres";
        ResetearHighlights();
        if (highlightResistente != null) highlightResistente.SetActive(true);
    }

    private void ResetearHighlights()
    {
        if (highlightManosFirmes != null) highlightManosFirmes.SetActive(false);
        if (highlightCreyente != null) highlightCreyente.SetActive(false);
        if (highlightResistente != null) highlightResistente.SetActive(false);
    }

    // ====================== BOTONES DEL SEGUNDO SCRIPT ======================

    public void OnReadyPressed()
    {
        Debug.Log("READY - Iniciando juego...");
        if (canvasMenu != null) canvasMenu.SetActive(false);
        SetPlayerControl(true);
        SetCursorState(true);
        isGameActive = true;
        isPaused = false;
        Time.timeScale = 1f;
    }

    public void OnMenuPressed()
    {
        Debug.Log("MENU - Saliendo del juego...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnContinuePressed()
    {
        Debug.Log("CONTINUAR - Reanudando partida...");
        isPaused = false;
        Time.timeScale = 1f;
        SetPlayerControl(true);
        SetCursorState(true);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    public void OnExitToMenuPressed()
    {
        Debug.Log("VOLVER AL MENÚ...");
        Time.timeScale = 1f;
        SetPlayerControl(false);
        SetCursorState(false);

        if (canvasMenu != null) canvasMenu.SetActive(true);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);

        isGameActive = false;
        isPaused = false;

        if (readyButton != null) readyButton.SetActive(false);
        if (menuButton != null) menuButton.SetActive(false);
        if (welcomeText != null) welcomeText.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(ShowButtonsAfterDelay());

        Debug.Log("Estado reseteado - isGameActive: " + isGameActive);
    }

    public void OnQuitGamePressed()
    {
        Debug.Log("SALIR - Cerrando juego...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ====================== BOTONES DEL PRIMER SCRIPT ======================

    public void OnBotonEmpezarPressed()
    {
        if (inputNombrePasante != null && !string.IsNullOrEmpty(inputNombrePasante.text))
            nombrePasante = inputNombrePasante.text;

        PlayerPrefs.SetString("NombrePasante", nombrePasante);
        PlayerPrefs.SetString("RasgoInicial", rasgoSeleccionado);

        Debug.Log($"Pasante creado → Nombre: {nombrePasante} | Rasgo: {rasgoSeleccionado}");

        SceneManager.LoadScene("GameScene");
    }

    public void OnBotonSalirPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}