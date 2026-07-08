using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnowParticles : MonoBehaviour
{
    [SerializeField] private GameObject ps;

    [SerializeField] private float emitterDistance = 10f;
    private ParticleSystem particle;
    private int _lastWidth;
    private int _lastHeight;

    void Awake()
    {
        ps.SetActive(false);
        particle = ps.GetComponent<ParticleSystem>();
        FitRadiusToView();
    }
    void Update()
    {
        if (_lastWidth != Screen.width || _lastHeight != Screen.height)
        {
            FitRadiusToView();
        }
        if (ps.activeSelf)
        {
            ps.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    public void toggleParticles(bool isActive)
    {
        ps.SetActive(isActive);
    }

    private void FitRadiusToView()
    {
        float aspect = (float)Screen.width / Screen.height;
        var shape = particle.shape;
        shape.radius = emitterDistance * Mathf.Sqrt(1f + aspect * aspect);

        _lastWidth = Screen.width;
        _lastHeight = Screen.height;
    }
}
