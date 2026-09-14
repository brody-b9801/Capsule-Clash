using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class ChangeMat : NetworkBehaviour
{
    public Material damaged;
    public Material normal;
    public Material self;
    public Material wood;
    public Material wire;
    public Material nails;
    public Material gun;

    [Tooltip("How long a player shows the damaged material after being hit.")]
    [SerializeField] private float damageFlashDuration = 0.05f;

    [Header("Desert Colors")]
    public Color desertLitColor = Color.white;
    public Color desertUnlitColor = Color.white;
    public Color desertHardEdgeLightColor = Color.white;
    public Color desertPlayerUnlitColor = Color.white;

    [Header("Void Colors")]
    public Color voidLitColor = Color.white;
    public Color voidUnlitColor = Color.white;
    public Color voidHardEdgeLightColor = Color.white;
    public Color voidPlayerUnlitColor = Color.white;

    [Header("Maze Colors")]
    public Color mazeLitColor = Color.white;
    public Color mazeUnlitColor = Color.white;
    public Color mazeHardEdgeLightColor = Color.white;
    public Color mazePlayerUnlitColor = Color.white;

    [Header("Ice Colors")]
    public Color iceLitColor = Color.white;
    public Color iceUnlitColor = Color.white;
    public Color iceHardEdgeLightColor = Color.white;
    public Color icePlayerUnlitColor = Color.white;

    private static readonly int LitColorID = Shader.PropertyToID("_HighColor");
    private static readonly int UnlitColorID = Shader.PropertyToID("_LowColor");
    private static readonly int HardEdgeLightColorID = Shader.PropertyToID("_RimColor");

    private Renderer player;
    private PlayerMovement movement;
    private Coroutine damageFlashRoutine;

    public static ChangeMat Local { get; private set; }

    private void Awake()
    {
        player = GetComponent<Renderer>();
        movement = GetComponent<PlayerMovement>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner)
        {
            Local = this;
            dimensionMaterialChange("Desert");
        }
        ApplyBaseMaterial();
    }

    public override void OnStopClient()
    {
        if (Local == this) Local = null;
        if (damageFlashRoutine != null)
        {
            StopCoroutine(damageFlashRoutine);
            damageFlashRoutine = null;
        }
        base.OnStopClient();
    }

    public void dimensionMaterialChange(string materialDimension)
    {
        if (materialDimension == "Desert") {
            ApplyDimensionColors(desertLitColor, desertUnlitColor, desertHardEdgeLightColor, desertPlayerUnlitColor);
        } else if (materialDimension == "Void") {
            ApplyDimensionColors(voidLitColor, voidUnlitColor, voidHardEdgeLightColor, voidPlayerUnlitColor);
        } else if (materialDimension == "Ice") {
            ApplyDimensionColors(iceLitColor, iceUnlitColor, iceHardEdgeLightColor, icePlayerUnlitColor);
        } else if (materialDimension == "Maze") {
            ApplyDimensionColors(mazeLitColor, mazeUnlitColor, mazeHardEdgeLightColor, mazePlayerUnlitColor);
        } else {
            Debug.LogError("dimension typed wrong in dimensionMaterialChange: " + materialDimension);
        }
    }

    private void ApplyDimensionColors(Color lit, Color unlit, Color hardEdgeLight, Color playerUnlit)
    {
        SetMaterialColors(normal, lit, playerUnlit, hardEdgeLight);
        SetMaterialColors(wood, lit, unlit, hardEdgeLight);
        SetMaterialColors(wire, lit, unlit, hardEdgeLight);
        SetMaterialColors(nails, lit, unlit, hardEdgeLight);
        SetMaterialColors(gun, lit, unlit, hardEdgeLight);
    }

    private void SetMaterialColors(Material mat, Color lit, Color unlit, Color hardEdgeLight)
    {
        if (mat == null) return;
        if (mat.HasProperty(LitColorID)) mat.SetColor(LitColorID, lit);
        if (mat.HasProperty(UnlitColorID)) mat.SetColor(UnlitColorID, unlit);
        if (mat.HasProperty(HardEdgeLightColorID)) mat.SetColor(HardEdgeLightColorID, hardEdgeLight);
    }

    public void FlashDamaged()
    {
        if (player == null || damaged == null) return;
        if (damageFlashRoutine != null) StopCoroutine(damageFlashRoutine);
        damageFlashRoutine = StartCoroutine(DamageFlash());
    }

    private void ApplyBaseMaterial()
    {
        if (player == null) return;
        Material baseMaterial = IsOwner ? self : normal;
        if (baseMaterial != null) player.sharedMaterial = baseMaterial;
    }

    IEnumerator DamageFlash() {
        player.sharedMaterial = damaged;
        yield return new WaitForSeconds(damageFlashDuration);
        ApplyBaseMaterial();
        damageFlashRoutine = null;
    }
}
