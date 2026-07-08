using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class CameraMotionBlur : MonoBehaviour
{
    public Shader motionBlurShader;

    [Header("Blur")]
    [Tooltip("Overall strength of the camera motion blur.")]
    [Range(0f, 8f)] public float blurScale = 2.5f;

    [Tooltip("Maximum per-pixel smear length, in fractions of the screen.")]
    [Range(0.001f, 0.2f)] public float maxBlurRadius = 0.05f;

    [Tooltip("Number of texture samples along the velocity vector. Higher = smoother, costlier.")]
    [Range(2, 24)] public int sampleCount = 10;

    [Tooltip("Depth below which a pixel is treated as sky and left un-blurred.")]
    [Range(0.00001f, 0.01f)] public float depthThreshold = 0.0001f;

    [Header("Vignette Shape")]
    [Tooltip("Distance from screen center (0 = center, 1 = corner) where edge blur begins.")]
    [Range(0f, 1f)] public float vignetteStart = 0.35f;

    [Tooltip("Distance from center where edge blur reaches full strength.")]
    [Range(0f, 1.5f)] public float vignetteEnd = 1.0f;

    [Tooltip("Falloff curve. >1 keeps the center clear longer and ramps hard at the edges.")]
    [Range(0.25f, 6f)] public float vignettePower = 2.5f;

    [Tooltip("Residual blur kept at the screen center. 0 = perfectly sharp center.")]
    [Range(0f, 1f)] public float centerStrength = 0f;

    [Header("Motion Gain")]
    [Tooltip("Camera turn speed (deg/sec) where rotation blur starts.")]
    public float rotationStart = 60f;
    [Tooltip("Camera turn speed (deg/sec) where rotation blur reaches full gain.")]
    public float rotationFull = 360f;

    [Tooltip("World speed (units/sec) where movement blur starts.")]
    public float velocityStart = 6f;
    [Tooltip("World speed (units/sec) where movement blur reaches full gain.")]
    public float velocityFull = 28f;

    [Tooltip("Gain multiplier applied at full rotation/velocity. 0 = motion gate disabled (always full blur).")]
    [Range(0f, 1f)] public float motionGateAmount = 1f;

    [Tooltip("How fast the gain follows changes in motion. Higher = snappier.")]
    public float gainResponse = 12f;

    private Material _mat;
    private Camera _cam;
    private Matrix4x4 _prevViewProj;
    private bool _hasPrev;

    private Quaternion _prevRot;
    private Vector3 _prevPos;
    private float _gain = 1f;

    void OnEnable()
    {
        _cam = GetComponent<Camera>();
        if (!motionBlurShader) motionBlurShader = Shader.Find("Hidden/CameraMotionBlur");
        _mat = new Material(motionBlurShader);
        _mat.hideFlags = HideFlags.HideAndDontSave;
        _cam.depthTextureMode |= DepthTextureMode.Depth;
        _hasPrev = false;
        _prevRot = transform.rotation;
        _prevPos = transform.position;
        _gain = motionGateAmount > 0f ? 0f : 1f;
    }

    private static float Ramp(float value, float start, float full)
    {
        if (full <= start) return value >= full ? 1f : 0f;
        return Mathf.Clamp01((value - start) / (full - start));
    }

    private float ComputeMotionGain()
    {
        if (motionGateAmount <= 0f) return 1f;

        float dt = Mathf.Max(Time.deltaTime, 1e-5f);

        float rotSpeed = Quaternion.Angle(transform.rotation, _prevRot) / dt;

        float velSpeed = (transform.position - _prevPos).magnitude / dt;
        if (PlayerMovement.started)
            velSpeed = Mathf.Max(velSpeed, PlayerMovement.getVelocity().magnitude);

        float rotGate = Ramp(rotSpeed, rotationStart, rotationFull);
        float velGate = Ramp(velSpeed, velocityStart, velocityFull);

        float target = Mathf.Lerp(1f - motionGateAmount, 1f, Mathf.Max(rotGate, velGate));
        _gain = Mathf.Lerp(_gain, target, 1f - Mathf.Exp(-gainResponse * dt));

        _prevRot = transform.rotation;
        _prevPos = transform.position;
        return _gain;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (!_mat || !_cam || !_mat.shader || !_mat.shader.isSupported)
        {
            Graphics.Blit(src, dst);
            return;
        }

        Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(_cam.projectionMatrix, false);
        Matrix4x4 viewProj = gpuProj * _cam.worldToCameraMatrix;
        Matrix4x4 invViewProj = viewProj.inverse;

        if (!_hasPrev) _prevViewProj = viewProj;

        float gain = ComputeMotionGain();

        _mat.SetMatrix("_InvViewProj", invViewProj);
        _mat.SetMatrix("_PrevViewProj", _prevViewProj);
        _mat.SetFloat("_BlurScale", blurScale * gain);
        _mat.SetFloat("_MaxBlurRadius", maxBlurRadius);
        _mat.SetInt("_SampleCount", Mathf.Max(2, sampleCount));
        _mat.SetFloat("_DepthThreshold", depthThreshold);
        _mat.SetFloat("_VignetteStart", vignetteStart);
        _mat.SetFloat("_VignetteEnd", vignetteEnd);
        _mat.SetFloat("_VignettePower", vignettePower);
        _mat.SetFloat("_CenterStrength", centerStrength);

        Graphics.Blit(src, dst, _mat);

        _prevViewProj = viewProj;
        _hasPrev = true;
    }

    void OnDisable()
    {
        if (_mat) DestroyImmediate(_mat);
        _mat = null;
        _hasPrev = false;
    }
}
