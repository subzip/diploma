using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TracerVFX : MonoBehaviour
{
    [SerializeField] private float speed = 260f;
    [SerializeField] private float trailLength = 0.35f;
    [SerializeField] private float fadeOutTime = 0.04f;
    [SerializeField] private float width = 0.02f;
    [SerializeField] private Color color = new Color(1f, 0.95f, 0.75f, 0.95f);
    [SerializeField] private Material tracerMaterial;

    private LineRenderer line;
    private Vector3 startPoint;
    private Vector3 endPoint;
    private Vector3 direction;
    private float distanceTotal;
    private float distanceTraveled;
    private bool started;
    private bool reachedEnd;
    private float fadeTimer;
    private Color baseColor;

    public void Initialize(
        Vector3 start,
        Vector3 end,
        float tracerSpeed,
        float tracerWidth,
        float tracerLength,
        Color tracerColor,
        Material material,
        float fadeOut = 0.04f)
    {
        if (line == null) line = GetComponent<LineRenderer>();

        startPoint = start;
        endPoint = end;
        direction = (endPoint - startPoint).normalized;
        distanceTotal = Vector3.Distance(startPoint, endPoint);
        distanceTraveled = 0f;
        reachedEnd = false;
        fadeTimer = 0f;

        speed = Mathf.Max(1f, tracerSpeed);
        width = Mathf.Max(0.001f, tracerWidth);
        trailLength = Mathf.Max(0.01f, tracerLength);
        fadeOutTime = Mathf.Max(0.01f, fadeOut);
        color = tracerColor;
        tracerMaterial = material;
        baseColor = color;

        SetupLine();
        started = true;

        line.SetPosition(0, startPoint);
        line.SetPosition(1, startPoint);
    }

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        if (!started || line == null) return;

        if (!reachedEnd)
        {
            distanceTraveled += speed * Time.deltaTime;
            float headT = distanceTotal > 0.001f ? Mathf.Clamp01(distanceTraveled / distanceTotal) : 1f;
            Vector3 head = Vector3.Lerp(startPoint, endPoint, headT);
            Vector3 tail = head - direction * trailLength;

            float dotToStart = Vector3.Dot(tail - startPoint, direction);
            if (dotToStart < 0f) tail = startPoint;

            line.SetPosition(0, tail);
            line.SetPosition(1, head);

            if (headT >= 1f)
            {
                reachedEnd = true;
            }
            return;
        }

        fadeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(fadeTimer / fadeOutTime);
        Color c = baseColor;
        c.a = Mathf.Lerp(baseColor.a, 0f, t);
        line.startColor = c;
        line.endColor = c;

        if (t >= 1f)
            Destroy(gameObject);
    }

    private void SetupLine()
    {
        if (line == null) return;

        line.enabled = true;
        line.positionCount = 2;
        line.textureMode = LineTextureMode.Stretch;
        line.alignment = LineAlignment.View;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.useWorldSpace = true;
        line.startWidth = width;
        line.endWidth = width * 0.55f;
        line.startColor = color;
        line.endColor = color;
        line.material = tracerMaterial != null ? tracerMaterial : CreateFallbackMaterial();
    }

    private static Material CreateFallbackMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        Material mat = new Material(shader);
        return mat;
    }
}
