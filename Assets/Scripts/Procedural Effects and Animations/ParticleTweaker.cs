using System.Numerics;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleTweaker : MonoBehaviour
{
    public float simulationSpeed = 2f;
    public float emissionRate = 0f;
    private ParticleSystem ps;
    private ParticleSystem.MainModule main;
    private ParticleSystem.EmissionModule emission;
    private float velocity;
    private float targetIntensity;
    private float currentIntensity;
    private float lerpSpeed = 1;
    public static UnityEngine.Vector3 velocityVec;
    private UnityEngine.Vector3 vec1;
    private UnityEngine.Vector3 vec2;
    [SerializeField] private Rigidbody rb;
    public static float yVelo;
    private Transform _camTransform;

    [SerializeField] private float emitterDistance = 10f;
    private Camera _cam;
    private int _lastWidth;
    private int _lastHeight;


    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        main = ps.main;
        emission = ps.emission;
        _camTransform = Camera.main.transform;

        main.simulationSpeed = simulationSpeed;

        var rateOverTime = emission.rateOverTime;
        rateOverTime.constant = emissionRate;
        emission.rateOverTime = rateOverTime;
    }

    void Update()
    {
        if (_lastWidth != Screen.width || _lastHeight != Screen.height)
        {
            FitRadiusToView();
        }

        // FishNet spawns the player after this component starts, so Local is null
        // for the first frames of a session and again after the player despawns.
        PlayerMovement player = PlayerMovement.Local;
        if (player == null || rb == null)
        {
            velocityVec = UnityEngine.Vector3.zero;
            yVelo = 0f;
            emission.rateOverTime = 0f;
            return;
        }

        if (_camTransform == null)
        {
            if (Camera.main == null) return;
            _camTransform = Camera.main.transform;
        }

        float deltaY = rb.linearVelocity.y;

        if (player.isGrounded && !player.onSlope)
        {
            velocityVec = new UnityEngine.Vector3(player.newVelocity.x + player.dashVector.x, deltaY, player.newVelocity.z + player.dashVector.z);
        }
        else
        {
            velocityVec = new UnityEngine.Vector3(8.5f/7.5f*player.newVelocity.x + player.dashVector.x, 0, 8.5f/7.5f*player.newVelocity.z + player.dashVector.z);
        }

        velocity = velocityVec.magnitude;
        vec1 = new UnityEngine.Vector3(velocityVec.x, 0, velocityVec.z).normalized;
        vec2 = new UnityEngine.Vector3(_camTransform.forward.x, 0, _camTransform.forward.z).normalized;
        yVelo = player.newVelocity.y + player.dashVector.y;

        float theta = Mathf.Clamp(Mathf.Cos(UnityEngine.Vector3.SignedAngle(vec1, vec2, new UnityEngine.Vector3(0, 1, 0))), 0, 1);
        float val = Mathf.Clamp(15 * (velocity - 9f), 0, 50);
        targetIntensity = val;

        emission.rateOverTime = targetIntensity;
    }
    private void FitRadiusToView()
    {
        float aspect = (float)Screen.width / Screen.height;
        var shape = ps.shape;
        shape.radius = emitterDistance * Mathf.Sqrt(1f + aspect * aspect);

        _lastWidth = Screen.width;
        _lastHeight = Screen.height;
    }
}