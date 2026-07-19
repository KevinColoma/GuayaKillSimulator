using UnityEngine;
using UnityEngine.InputSystem;


public class FirstPersonLook : MonoBehaviour
{
    [SerializeField]
    Transform character;
    public float sensitivity = 2;
    public float smoothing = 1.5f;

    Vector2 velocity;
    Vector2 frameVelocity;

    private bool isControlEnabled = true;  // Por defecto activado
    private bool ignorarUnFrame = false;   // descarta el salto de la cámara al reactivar el mouse-look

    void Reset()
    {
        character = GetComponentInParent<FirstPersonMovement>().transform;
    }

    void Start()
    {
        // No bloqueamos el cursor aquí, el menú lo controla
        if (character == null)
        {
            character = GetComponentInParent<FirstPersonMovement>().transform;
        }
    }

void Update()
    {
        if (!isControlEnabled) return;

        if (character == null) return;

        Vector2 mouseDelta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;

        // Al reactivar el mouse-look, descartar el primer frame: el delta acumulado
        // mientras el cursor estuvo libre haría girar la cámara de golpe.
        if (ignorarUnFrame)
        {
            ignorarUnFrame = false;
            frameVelocity = Vector2.zero;
            return;
        }

        Vector2 rawFrameVelocity = Vector2.Scale(mouseDelta, Vector2.one * sensitivity);
        frameVelocity = Vector2.Lerp(frameVelocity, rawFrameVelocity, 1 / smoothing);
        velocity += frameVelocity;
        velocity.y = Mathf.Clamp(velocity.y, -90, 90);

        transform.localRotation = Quaternion.AngleAxis(-velocity.y, Vector3.right);
        character.localRotation = Quaternion.AngleAxis(velocity.x, Vector3.up);
    }

    public void SetControlEnabled(bool enabled)
    {
        isControlEnabled = enabled;
        if (enabled)
        {
            ignorarUnFrame = true;      // evita el salto de cámara al volver a explorar
            frameVelocity = Vector2.zero;
        }
        Debug.Log("Mirada: " + (enabled ? "ACTIVADA" : "DESACTIVADA"));
    }
}