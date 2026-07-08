using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class healParticles : MonoBehaviour
{
    public static bool healing = false;
    private ParticleSystem ps;
    private ParticleSystem.EmissionModule emission;

    [SerializeField] private float emitterDistance = 10f;
    private int _lastWidth;
    private int _lastHeight;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        emission = ps.emission;
        FitRadiusToView();
    }

    void Update()
    {
        if (_lastWidth != Screen.width || _lastHeight != Screen.height)
        {
            FitRadiusToView();
        }
        emission.rateOverTime = healing ? 20f * upgradeManager.regenSpeedMultiplier : 0f;
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
