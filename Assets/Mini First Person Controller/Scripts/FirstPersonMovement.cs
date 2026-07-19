using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class FirstPersonMovement : MonoBehaviour
{
    public float speed = 5;

    [Header("Running")]
    public bool canRun = true;
    public bool IsRunning { get; private set; }
    public float runSpeed = 9;
    public KeyCode runningKey = KeyCode.LeftShift;

    Rigidbody rigidbody;
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    private bool isControlEnabled = true;  // Por defecto activado

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

void FixedUpdate()
    {
        // Solo mover si el control está activado
        if (!isControlEnabled) return;

        IsRunning = canRun && InputCompat.IsKeyPressed(runningKey);

        float targetMovingSpeed = IsRunning ? runSpeed : speed;
        if (speedOverrides.Count > 0)
        {
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();
        }

        Vector2 axis = Vector2.zero;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) axis.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) axis.x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) axis.y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) axis.y += 1f;
        }
        axis = Vector2.ClampMagnitude(axis, 1f);

        Vector2 targetVelocity = new Vector2(axis.x * targetMovingSpeed, axis.y * targetMovingSpeed);
        rigidbody.linearVelocity = transform.rotation * new Vector3(targetVelocity.x, rigidbody.linearVelocity.y, targetVelocity.y);
    }

    public void SetControlEnabled(bool enabled)
    {
        isControlEnabled = enabled;
        Debug.Log("Movimiento: " + (enabled ? "ACTIVADO" : "DESACTIVADO"));
    }
}