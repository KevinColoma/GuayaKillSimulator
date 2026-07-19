using UnityEngine;
using UnityEngine.InputSystem;

// Puente de compatibilidad: traduce KeyCode (legado) a teclas del New Input System.
// Los assets de terceros (ej. Mini First Person Controller) exponen campos KeyCode
// en el Inspector; esto evita depender de la clase legacy UnityEngine.Input, que
// requiere reiniciar el Editor tras cambiar "Active Input Handling" a "Both".
public static class InputCompat
{
    public static bool IsKeyPressed(KeyCode code)
    {
        var kb = Keyboard.current;
        if (kb == null) return false;

        switch (code)
        {
            case KeyCode.LeftShift: return kb.leftShiftKey.isPressed;
            case KeyCode.RightShift: return kb.rightShiftKey.isPressed;
            case KeyCode.LeftControl: return kb.leftCtrlKey.isPressed;
            case KeyCode.RightControl: return kb.rightCtrlKey.isPressed;
            case KeyCode.LeftAlt: return kb.leftAltKey.isPressed;
            case KeyCode.RightAlt: return kb.rightAltKey.isPressed;
            case KeyCode.Space: return kb.spaceKey.isPressed;
            case KeyCode.Escape: return kb.escapeKey.isPressed;
            case KeyCode.Return: return kb.enterKey.isPressed;
            case KeyCode.Tab: return kb.tabKey.isPressed;
            default: return false;
        }
    }

    public static bool IsKeyDown(KeyCode code)
    {
        var kb = Keyboard.current;
        if (kb == null) return false;

        switch (code)
        {
            case KeyCode.Space: return kb.spaceKey.wasPressedThisFrame;
            case KeyCode.Escape: return kb.escapeKey.wasPressedThisFrame;
            case KeyCode.Return: return kb.enterKey.wasPressedThisFrame;
            case KeyCode.LeftShift: return kb.leftShiftKey.wasPressedThisFrame;
            case KeyCode.LeftControl: return kb.leftCtrlKey.wasPressedThisFrame;
            default: return false;
        }
    }
}
