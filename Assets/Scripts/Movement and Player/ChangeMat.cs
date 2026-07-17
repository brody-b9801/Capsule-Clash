using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Alteruna;

public class ChangeMat : AttributesSync
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
    [SerializeField] private Alteruna.Avatar avatar;
    public static string avatarRef;
    public static string shooterRef;
    public static bool healed = false;

    private void Awake()
    {
        player = GetComponent<Renderer>();
        movement = GetComponent<PlayerMovement>();
        avatarRef = avatar.ToString();
        dimensionMaterialChange("Desert");
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

    public void TakeDamage(string shot, string shoot, bool shotgun, float dist)
    {
        StartCoroutine(endDamaged(shot, shoot, shotgun, dist));
    }

    [SynchronizableMethod]
    private void TakeDamageSync(string av, string shoot, bool shotgun, float dist)
    {
        if (player.gameObject.GetComponent<Alteruna.Avatar>().IsOwner)
        {
            player.sharedMaterial = self;
        } else{
            player.sharedMaterial = normal;
        }
    }

    [SynchronizableMethod]
    public void ControlDamage(string shotAvatar, string shooter, bool shotgun, float dist)
    {
        bool avatarSame = PlayerMovement.getAvatarBool(shotAvatar);

        if (avatarSame) 
        {
            if (!shotgun) {
                PlayerMovement.healthWidth -= 18 * upgradeManager.damageMultiplier;
            } else {
                if (dist < 3) {
                    PlayerMovement.healthWidth -= 17 * upgradeManager.damageMultiplier;
                } else {
                    PlayerMovement.healthWidth -= Mathf.Clamp((17 * upgradeManager.damageMultiplier - ((dist-3)*0.6f)), 1, 15 * upgradeManager.damageMultiplier);
                }

            }

            HealthController.updateHealth();
        } 
        if (PlayerMovement.healthWidth <= 0 && !healed) {
            healed = true;
            GetComponent<PlayerMovement>().Die();
            PlayerMovement playerMovementInstance = GetComponent<PlayerMovement>();
            playerMovementInstance.killHeal(shooter); // Use instance reference here
        }
    }

    IEnumerator endDamaged(string shot, string shoot, bool shotgun, float dist) {
        BroadcastRemoteMethod(2, shot, shoot, shotgun, dist);
        yield return new WaitForSeconds(0.05f);
        BroadcastRemoteMethod(0, shot, shoot, shotgun, dist);
    }

    [SynchronizableMethod]
    private void idk(string av, string shoot, bool shotgun, float dist)
    {
        if (avatar.IsOwner)
            BroadcastRemoteMethod(1, av, shoot, shotgun, dist);
    }
}
