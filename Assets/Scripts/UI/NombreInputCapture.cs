using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Captura de texto para el campo de nombre usando el New Input System
// (Keyboard.current.onTextInput), en vez de depender del Input legacy interno de
// TMP_InputField (que puede no capturar teclas según la config de Active Input Handling).
// Mientras el panel de personalización esté visible, lo que escribas edita el nombre.
// El campo se pone en readOnly para que ESTE componente sea la única fuente del texto
// (así no hay caracteres duplicados).
[RequireComponent(typeof(TMP_InputField))]
public class NombreInputCapture : MonoBehaviour
{
    public int maxLen = 18;

    TMP_InputField field;
    bool suscrito = false;

    void Awake()
    {
        field = GetComponent<TMP_InputField>();
        field.readOnly = true; // nosotros manejamos el texto
    }

    void OnEnable()
    {
        Suscribir();
    }

    void OnDisable()
    {
        Desuscribir();
    }

    void Suscribir()
    {
        if (suscrito) return;
        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput += OnText;
            suscrito = true;
        }
    }

    void Desuscribir()
    {
        if (!suscrito) return;
        if (Keyboard.current != null) Keyboard.current.onTextInput -= OnText;
        suscrito = false;
    }

    void OnText(char c)
    {
        if (field == null) return;
        if (char.IsControl(c)) return;              // ignora enter/tab/backspace (backspace va en Update)
        if (field.text.Length >= maxLen) return;
        field.text += c;
    }

    void Update()
    {
        // Si el teclado no estaba listo en OnEnable, suscribirse en cuanto aparezca
        if (!suscrito) Suscribir();

        var kb = Keyboard.current;
        if (kb == null || field == null) return;

        // Borrar con retroceso (mantener presionado repite cada ~0.08s)
        if (kb.backspaceKey.wasPressedThisFrame)
            Borrar();
    }

    void Borrar()
    {
        if (field.text.Length > 0)
            field.text = field.text.Substring(0, field.text.Length - 1);
    }
}
