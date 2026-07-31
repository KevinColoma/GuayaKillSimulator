using UnityEngine;

public class ToolPickup : MonoBehaviour
{
    public string toolName;
    float tiempoMaximo = 30f;
    Transform player;

    void Start()
    {
        player = Camera.main?.transform;
        Destroy(gameObject, tiempoMaximo);
    }

    void Update()
    {
        if (player == null) return;
        transform.Rotate(0, 60f * Time.deltaTime, 0);
        float dy = Mathf.Sin(Time.time * 2.5f) * 0.25f;
        transform.position = new Vector3(transform.position.x,
            posicionOriginal.y + dy, transform.position.z);
        if (Vector3.Distance(player.position, transform.position) < 2.5f)
            PickUp();
    }

    Vector3 posicionOriginal;
    bool pickedUp;

    void PickUp()
    {
        if (pickedUp) return;
        pickedUp = true;
        var tools = MedicalToolsManager.Instance;
        if (tools != null)
        {
            var tool = tools.GetTool(toolName);
            if (tool != null) tool.cantidad++;
            tools.NotificarCambioInventario();
        }
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySuccess();
        Destroy(gameObject);
    }

    public static GameObject Crear(Vector3 pos)
    {
        string[] tools = { "Gasas", "Alcohol", "Suturas", "Torniquete", "Kit" };
        string h = tools[Random.Range(0, tools.Length)];

        GameObject go;
        Color col;

        switch (h)
        {
            case "Gasas":
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.localScale = new Vector3(0.35f, 0.08f, 0.3f);
                col = Color.white;
                break;
            case "Alcohol":
                go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.transform.localScale = new Vector3(0.15f, 0.3f, 0.15f);
                col = new Color(0.5f, 0.8f, 1f);
                break;
            case "Suturas":
                go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.transform.localScale = new Vector3(0.05f, 0.25f, 0.05f);
                col = new Color(0.7f, 0.7f, 0.85f);
                break;
            case "Torniquete":
                go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.transform.localScale = new Vector3(0.25f, 0.06f, 0.25f);
                col = new Color(0.9f, 0.25f, 0.15f);
                break;
            default:
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.localScale = new Vector3(0.25f, 0.18f, 0.2f);
                col = new Color(0.95f, 0.75f, 0.15f);
                break;
        }

        go.name = "Pickup_" + h;
        go.transform.position = pos;

        float emision = 0.4f;
        Color finalCol = col * (1f + emision);
        var rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = finalCol;
            rend.material.EnableKeyword("_EMISSION");
            rend.material.SetColor("_EmissionColor", col * 0.6f);
        }

        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var pickup = go.AddComponent<ToolPickup>();
        pickup.toolName = h;
        pickup.posicionOriginal = pos;

        var colComponent = go.GetComponent<Collider>();
        if (colComponent != null) colComponent.isTrigger = true;

        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 4f;
        light.intensity = 0.8f;
        light.color = col;
        light.shadows = LightShadows.None;

        var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beam.name = "Beam";
        beam.transform.SetParent(go.transform, false);
        beam.transform.localPosition = Vector3.zero;
        beam.transform.localScale = new Vector3(0.03f, 2.5f, 0.03f);
        var beamRend = beam.GetComponent<Renderer>();
        if (beamRend != null)
        {
            beamRend.material.color = new Color(col.r, col.g, col.b, 0.15f);
            beamRend.material.EnableKeyword("_EMISSION");
            beamRend.material.SetColor("_EmissionColor", col * 0.3f);
        }
        Destroy(beam.GetComponent<Collider>());

        return go;
    }
}
