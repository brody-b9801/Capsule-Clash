using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UIElements;

public class MotionBlurToggle : MonoBehaviour
{
    [SerializeField] private PostProcessVolume volume;
    private ChromaticAberration chromaticAberration;
    private float currentIntensity = 0f;
    private float lerpSpeed = 5f;
    private float targetIntensity;
    private Vector3 position;
    private Vector3 prevPosition;
    private float velocity;
    private float i = 0;

    void Start()
    {
        volume.profile.TryGetSettings(out chromaticAberration);
    }

    void Update()
    {
        if (chromaticAberration == null) return;
        // The local player is spawned by FishNet after this component starts.
        if (PlayerMovement.Local == null) return;
        float val;
        if (RetroDither.isTeleporting) {
            val = 0;
        } else if (!PlayerMovement.Local.isGrounded) {
            val = Mathf.Clamp(0.125f * (Mathf.Abs(ParticleTweaker.yVelo) - 6), 0, 0.4f);
        }
        else {
            val = 0;
        }

        chromaticAberration.intensity.Override(val);
    }
}
