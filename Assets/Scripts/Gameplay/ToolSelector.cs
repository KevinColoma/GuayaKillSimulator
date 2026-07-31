using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

public class ToolSelector : MonoBehaviour
{
    [Tooltip("Distancia maxima del raycast para detectar herramientas")]
    public float maxDistance = 35f;

    [Tooltip("Capas en las que buscar modelos de herramienta")]
    public LayerMask toolLayer = 1 << 8;

    [Tooltip("Tecla para usar la herramienta seleccionada")]
    public Key useKey = Key.E;

    [Tooltip("Offset del tooltip respecto a la posicion del modelo en pantalla")]
    public Vector2 tooltipOffset = new Vector2(0, 60);

    Camera cam;
    ToolModel targetActual;
    ToolModel targetAnterior;
    GameObject tooltipInstance;
    TextMeshProUGUI tooltipText;

    FirstPersonLook playerLook;
    FirstPersonMovement playerMovement;

    bool cursorDebeEstarLibre;
    bool esperandoFinMinijuego;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;

        var player = GameObject.Find("Player");
        if (player != null)
        {
            playerLook = player.GetComponentInChildren<FirstPersonLook>();
            playerMovement = player.GetComponent<FirstPersonMovement>();
        }
    }

    void Start()
    {
        MiniGameManager.OnMiniGameStarted += OnMiniGameStarted;
        MiniGameManager.OnMiniGameEnded += OnMiniGameEnded;

        if (tooltipInstance == null)
            CrearTooltipPorDefecto();

        CrearCrosshair();
    }

    void OnDestroy()
    {
        MiniGameManager.OnMiniGameStarted -= OnMiniGameStarted;
        MiniGameManager.OnMiniGameEnded -= OnMiniGameEnded;
    }

    void Update()
    {
        if (cam == null) return;

        if (esperandoFinMinijuego)
        {
            OcultarTooltip();
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        targetActual = null;
        if (Physics.Raycast(ray, out hit, maxDistance, toolLayer))
        {
            targetActual = hit.collider.GetComponentInParent<ToolModel>();
        }

        if (targetActual != targetAnterior)
        {
            if (targetAnterior != null)
                targetAnterior.DesactivarResaltado();
            if (targetActual != null)
                targetActual.ActivarResaltado();
            targetAnterior = targetActual;
        }

        if (targetActual != null)
        {
            Vector3 screenPos = cam.WorldToScreenPoint(targetActual.transform.position);
            MostrarTooltip(targetActual, screenPos);

            if (Keyboard.current != null && Keyboard.current[useKey].wasPressedThisFrame)
            {
                UsarHerramienta(targetActual);
            }
        }
        else
        {
            OcultarTooltip();
        }
    }

    void UsarHerramienta(ToolModel tool)
    {
        if (string.IsNullOrEmpty(tool.toolName)) return;
        if (MedicalToolsManager.Instance == null) return;

        LiberarCursor();
        MedicalToolsManager.Instance.UseTool(tool.toolName);
        StartCoroutine(VerificarMinijuego());
    }

    IEnumerator VerificarMinijuego()
    {
        yield return null;
        if (MiniGameManager.Instance != null && !MiniGameManager.Instance.EnCurso)
            CapturarCursor();
    }

    void LiberarCursor()
    {
        cursorDebeEstarLibre = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (playerLook != null)
        {
            playerLook.enabled = false;
            playerLook.SetControlEnabled(false);
        }
        if (playerMovement != null)
            playerMovement.SetControlEnabled(false);

        if (targetActual != null)
        {
            targetActual.DesactivarResaltado();
            targetActual = null;
        }
        targetAnterior = null;
        OcultarTooltip();

        esperandoFinMinijuego = true;
    }

    void CapturarCursor()
    {
        cursorDebeEstarLibre = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (playerLook != null)
        {
            playerLook.enabled = true;
            playerLook.SetControlEnabled(true);
        }
        if (playerMovement != null)
            playerMovement.SetControlEnabled(true);

        esperandoFinMinijuego = false;
    }

    void OnMiniGameStarted()
    {
        if (!cursorDebeEstarLibre)
            LiberarCursor();
    }

    void OnMiniGameEnded()
    {
        CapturarCursor();
    }

    void MostrarTooltip(ToolModel tool, Vector3 screenPos)
    {
        if (tooltipInstance == null) return;

        screenPos.x += tooltipOffset.x;
        screenPos.y += tooltipOffset.y;

        tooltipInstance.SetActive(true);
        var rt = tooltipInstance.GetComponent<RectTransform>();
        rt.position = screenPos;

        if (tooltipText != null)
        {
            string nombre = string.IsNullOrEmpty(tool.displayName) ? tool.toolName : tool.displayName;
            tooltipText.text = "Presiona E para usar " + nombre;
        }
    }

    void OcultarTooltip()
    {
        if (tooltipInstance != null)
            tooltipInstance.SetActive(false);
    }

    void CrearCrosshair()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        var cross = new GameObject("Crosshair", typeof(RectTransform));
        cross.transform.SetParent(canvas.transform, false);
        var crt = cross.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(6, 6);

        var img = cross.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.red;
        img.raycastTarget = false;
    }

    void CrearTooltipPorDefecto()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        tooltipInstance = new GameObject("Tooltip_UI", typeof(RectTransform));
        tooltipInstance.transform.SetParent(canvas.transform, false);

        var rt = tooltipInstance.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(350, 50);

        var bg = tooltipInstance.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);

        var textGO = new GameObject("TooltipText", typeof(RectTransform));
        textGO.transform.SetParent(tooltipInstance.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        tooltipText = textGO.AddComponent<TextMeshProUGUI>();
        tooltipText.fontSize = 22;
        tooltipText.alignment = TextAlignmentOptions.Center;
        tooltipText.color = Color.white;
        tooltipText.raycastTarget = false;

        tooltipInstance.SetActive(false);
    }

    public void ForzarCapturarCursor()
    {
        CapturarCursor();
    }
}
