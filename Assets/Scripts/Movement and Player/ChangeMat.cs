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

    public static ChangeMat Local { get; private set; }

    public bool healed = false;

    private void Awake()
    {
        player = GetComponent<Renderer>();
        movement = GetComponent<PlayerMovement>();
        dimensionMaterialChange("Desert");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner) Local = this;
    }

    public override void OnStopClient()
    {
        if (Local == this) Local = null;
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

    public void TakeDamage(NetworkObject shot, NetworkObject shooter, bool shotgun, float dist)
    {
        StartCoroutine(endDamaged(shot, shooter, shotgun, dist));
    }

    [ObserversRpc]
    private void TakeDamageSync(NetworkObject av, NetworkObject shoot, bool shotgun, float dist)
    {
        if (IsOwner)
        {
            player.sharedMaterial = self;
        } else{
            player.sharedMaterial = normal;
        }
    }

    [Server]
    public void ControlDamage(NetworkObject shotAvatar, NetworkObject shooter, bool shotgun, float dist)
    {
        DamageControl damageControl = GetComponent<DamageControl>();
        if (damageControl == null) return;

        PlayerMovement victimMovement = GetComponent<PlayerMovement>();
        if (victimMovement == null || !victimMovement.canTakeDamage) return;

        float damageMultiplier = upgradeManager.Local != null ? upgradeManager.Local.damageMultiplier : 1f;

        float damageDealt;
        if (!shotgun) {
            damageDealt = 18 * damageMultiplier;
        } else {
            if (dist < 3) {
                damageDealt = 17 * damageMultiplier;
            } else {
                damageDealt = Mathf.Clamp((17 * damageMultiplier - ((dist-3)*0.6f)), 1, 15 * damageMultiplier);
            }
        }

        damageControl.health.Value -= damageDealt;

        bool died = damageControl.health.Value <= 0 && !healed;
        if (died) {
            healed = true;
            victimMovement.killHealSync(shooter);
        }

        ApplyDamageFeedback(died);
    }

    [ObserversRpc]
    private void ApplyDamageFeedback(bool died)
    {
        HealthController.updateHealth();

        if (died)
        {
            PlayerMovement victimMovement = GetComponent<PlayerMovement>();
            if (victimMovement != null) victimMovement.Die();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerControlDamage(NetworkObject shot, NetworkObject shooter, bool shotgun, float dist)
        => ControlDamage(shot, shooter, shotgun, dist);

    [ServerRpc(RequireOwnership = false)]
    private void ServerTakeDamageSync(NetworkObject shot, NetworkObject shooter, bool shotgun, float dist)
        => TakeDamageSync(shot, shooter, shotgun, dist);

    IEnumerator endDamaged(NetworkObject shot, NetworkObject shooter, bool shotgun, float dist) {
        ServerControlDamage(shot, shooter, shotgun, dist);
        yield return new WaitForSeconds(0.05f);
        ServerTakeDamageSync(shot, shooter, shotgun, dist);
    }
}
