using UnityEngine;
using System;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────────────────
//  SA2 Upgrade System – mirrors the original Sonic Adventure 2 upgrade set
//  for all six playable characters.
//
//  Features
//  ────────
//  • Bool flags per upgrade (serialized via Unity's JsonUtility)
//  • Unlock() / Lock() / HasUpgrade() API
//  • Stat/ability side-effects applied on unlock (override ApplyUpgradeEffect)
//  • UI integration hooks  (OnUpgradeUnlocked / OnUpgradeLocked events)
//  • Save / Load via PlayerPrefs (JSON snapshot)
// ─────────────────────────────────────────────────────────────────────────────

#region ── Upgrade Enums ──────────────────────────────────────────────────────

public enum SonicUpgrade
{
    LightShoes,         // Bounce Bracelet prerequisite; air dash
    AncientLight,       // Light Speed Attack charge
    BounceBracelet,     // Bounce Attack
    FlameRing,          // Flame Ring (fire grind rails)
    MagicGloves         // Magic Hands
}

public enum ShadowUpgrade
{
    AirShoes,           // Hover / air dash
    AncientLight,       // Light Speed Attack charge
    FlameRing           // Flame Ring
}

public enum TailsUpgrade
{
    Bazooka,            // Lock-on missile
    LaserBlaster,       // Rapid-fire laser
    Booster,            // Hover / flight booster
    MysticMelody        // Activates ancient ruins
}

public enum EggmanUpgrade
{
    JetEngine,          // Speed upgrade
    LargeCannon,        // Cannon power upgrade
    Laser,              // Laser weapon
    MysticMelody        // Activates ancient ruins
}

public enum KnucklesUpgrade
{
    ShovelClaw,         // Dig into soft ground
    AirNecklace,        // Glide further / air boost
    HammerGloves,       // Break hard rocks
    Sunglasses,         // Detects Master Emerald shards
    MysticMelody        // Activates ancient ruins
}

public enum RougeUpgrade
{
    PickNails,          // Dig into soft ground
    TreasureScope,      // Detect Emerald Shards through walls
    IronBoots,          // Break hard rocks / kick power
    MysticMelody        // Activates ancient ruins
}

#endregion

#region ── SaveData ───────────────────────────────────────────────────────────

[Serializable]
public class SA2UpgradeSaveData
{
    // Sonic
    public bool sonic_LightShoes;
    public bool sonic_AncientLight;
    public bool sonic_BounceBracelet;
    public bool sonic_FlameRing;
    public bool sonic_MagicGloves;

    // Shadow
    public bool shadow_AirShoes;
    public bool shadow_AncientLight;
    public bool shadow_FlameRing;

    // Tails
    public bool tails_Bazooka;
    public bool tails_LaserBlaster;
    public bool tails_Booster;
    public bool tails_MysticMelody;

    // Eggman
    public bool eggman_JetEngine;
    public bool eggman_LargeCannon;
    public bool eggman_Laser;
    public bool eggman_MysticMelody;

    // Knuckles
    public bool knuckles_ShovelClaw;
    public bool knuckles_AirNecklace;
    public bool knuckles_HammerGloves;
    public bool knuckles_Sunglasses;
    public bool knuckles_MysticMelody;

    // Rouge
    public bool rouge_PickNails;
    public bool rouge_TreasureScope;
    public bool rouge_IronBoots;
    public bool rouge_MysticMelody;
}

#endregion

#region ── Main MonoBehaviour ─────────────────────────────────────────────────

public class SA2Upgrades : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────
    public static SA2Upgrades Instance { get; private set; }

    private const string SAVE_KEY = "SA2_UpgradeData";

    // ── UI / External Events ───────────────────────────────────────────────
    /// <summary>Fired whenever any upgrade is unlocked.  Args: (characterName, upgradeName)</summary>
    public static event Action<string, string> OnUpgradeUnlocked;

    /// <summary>Fired whenever any upgrade is locked (removed).  Args: (characterName, upgradeName)</summary>
    public static event Action<string, string> OnUpgradeLocked;

    // ── Internal flag storage ──────────────────────────────────────────────
    // Sonic
    private bool _sonic_LightShoes;
    private bool _sonic_AncientLight;
    private bool _sonic_BounceBracelet;
    private bool _sonic_FlameRing;
    private bool _sonic_MagicGloves;

    // Shadow
    private bool _shadow_AirShoes;
    private bool _shadow_AncientLight;
    private bool _shadow_FlameRing;

    // Tails
    private bool _tails_Bazooka;
    private bool _tails_LaserBlaster;
    private bool _tails_Booster;
    private bool _tails_MysticMelody;

    // Eggman
    private bool _eggman_JetEngine;
    private bool _eggman_LargeCannon;
    private bool _eggman_Laser;
    private bool _eggman_MysticMelody;

    // Knuckles
    private bool _knuckles_ShovelClaw;
    private bool _knuckles_AirNecklace;
    private bool _knuckles_HammerGloves;
    private bool _knuckles_Sunglasses;
    private bool _knuckles_MysticMelody;

    // Rouge
    private bool _rouge_PickNails;
    private bool _rouge_TreasureScope;
    private bool _rouge_IronBoots;
    private bool _rouge_MysticMelody;

    // ── Optional character stat components ────────────────────────────────
    // Assign these in the Inspector if you want ApplyUpgradeEffect to drive
    // real stat changes. Leave null and only the event will fire.
    [Header("Character Stat Components (optional)")]
    public SonicStats sonicStats;
    public ShadowStats shadowStats;
    public TailsStats tailsStats;
    public EggmanStats eggmanStats;
    public KnucklesStats knucklesStats;
    public RougeStats rougeStats;

    // ──────────────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ──────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadUpgrades();
    }

    // ──────────────────────────────────────────────────────────────────────
    //  PUBLIC API – Sonic
    // ──────────────────────────────────────────────────────────────────────

    public bool HasUpgrade(SonicUpgrade upgrade) => upgrade switch
    {
        SonicUpgrade.LightShoes => _sonic_LightShoes,
        SonicUpgrade.AncientLight => _sonic_AncientLight,
        SonicUpgrade.BounceBracelet => _sonic_BounceBracelet,
        SonicUpgrade.FlameRing => _sonic_FlameRing,
        SonicUpgrade.MagicGloves => _sonic_MagicGloves,
        _ => false
    };

    public void Unlock(SonicUpgrade upgrade)
    {
        if (HasUpgrade(upgrade)) return;
        SetFlag(upgrade, true);
        ApplyUpgradeEffect(upgrade);
        OnUpgradeUnlocked?.Invoke("Sonic", upgrade.ToString());
        SaveUpgrades();
    }

    public void Lock(SonicUpgrade upgrade)
    {
        if (!HasUpgrade(upgrade)) return;
        SetFlag(upgrade, false);
        RemoveUpgradeEffect(upgrade);
        OnUpgradeLocked?.Invoke("Sonic", upgrade.ToString());
        SaveUpgrades();
    }

    private void SetFlag(SonicUpgrade u, bool value)
    {
        switch (u)
        {
            case SonicUpgrade.LightShoes: _sonic_LightShoes = value; break;
            case SonicUpgrade.AncientLight: _sonic_AncientLight = value; break;
            case SonicUpgrade.BounceBracelet: _sonic_BounceBracelet = value; break;
            case SonicUpgrade.FlameRing: _sonic_FlameRing = value; break;
            case SonicUpgrade.MagicGloves: _sonic_MagicGloves = value; break;
        }
    }

    // ── Stat / ability side-effects ───────────────────────────────────────

    private void ApplyUpgradeEffect(SonicUpgrade upgrade)
    {
        if (sonicStats == null) return;
        switch (upgrade)
        {
            case SonicUpgrade.LightShoes:
                sonicStats.canAirDash = true;
                sonicStats.canLightSpeedDash = true;
                break;
            case SonicUpgrade.AncientLight:
                sonicStats.canLightSpeedAttack = true;
                break;
            case SonicUpgrade.BounceBracelet:
                sonicStats.canBounceAttack = true;
                break;
            case SonicUpgrade.FlameRing:
                sonicStats.canUseFlameRing = true;
                break;
            case SonicUpgrade.MagicGloves:
                sonicStats.canUseMagicHands = true;
                break;
        }
    }

    private void RemoveUpgradeEffect(SonicUpgrade upgrade)
    {
        if (sonicStats == null) return;
        switch (upgrade)
        {
            case SonicUpgrade.LightShoes:
                sonicStats.canAirDash = false;
                sonicStats.canLightSpeedDash = false;
                break;
            case SonicUpgrade.AncientLight:
                sonicStats.canLightSpeedAttack = false;
                break;
            case SonicUpgrade.BounceBracelet:
                sonicStats.canBounceAttack = false;
                break;
            case SonicUpgrade.FlameRing:
                sonicStats.canUseFlameRing = false;
                break;
            case SonicUpgrade.MagicGloves:
                sonicStats.canUseMagicHands = false;
                break;
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  PUBLIC API – Shadow
    // ──────────────────────────────────────────────────────────────────────

    public bool HasUpgrade(ShadowUpgrade upgrade) => upgrade switch
    {
        ShadowUpgrade.AirShoes => _shadow_AirShoes,
        ShadowUpgrade.AncientLight => _shadow_AncientLight,
        ShadowUpgrade.FlameRing => _shadow_FlameRing,
        _ => false
    };

    public void Unlock(ShadowUpgrade upgrade)
    {
        if (HasUpgrade(upgrade)) return;
        SetFlag(upgrade, true);
        ApplyUpgradeEffect(upgrade);
        OnUpgradeUnlocked?.Invoke("Shadow", upgrade.ToString());
        SaveUpgrades();
    }

    public void Lock(ShadowUpgrade upgrade)
    {
        if (!HasUpgrade(upgrade)) return;
        SetFlag(upgrade, false);
        RemoveUpgradeEffect(upgrade);
        OnUpgradeLocked?.Invoke("Shadow", upgrade.ToString());
        SaveUpgrades();
    }

    private void SetFlag(ShadowUpgrade u, bool value)
    {
        switch (u)
        {
            case ShadowUpgrade.AirShoes: _shadow_AirShoes = value; break;
            case ShadowUpgrade.AncientLight: _shadow_AncientLight = value; break;
            case ShadowUpgrade.FlameRing: _shadow_FlameRing = value; break;
        }
    }

    private void ApplyUpgradeEffect(ShadowUpgrade upgrade)
    {
        if (shadowStats == null) return;
        switch (upgrade)
        {
            case ShadowUpgrade.AirShoes:
                shadowStats.canAirDash = true;
                shadowStats.canLightSpeedDash = true;
                break;
            case ShadowUpgrade.AncientLight:
                shadowStats.canLightSpeedAttack = true;
                break;
            case ShadowUpgrade.FlameRing:
                shadowStats.canUseFlameRing = true;
                break;
        }
    }

    private void RemoveUpgradeEffect(ShadowUpgrade upgrade)
    {
        if (shadowStats == null) return;
        switch (upgrade)
        {
            case ShadowUpgrade.AirShoes:
                shadowStats.canAirDash = false;
                shadowStats.canLightSpeedDash = false;
                break;
            case ShadowUpgrade.AncientLight:
                shadowStats.canLightSpeedAttack = false;
                break;
            case ShadowUpgrade.FlameRing:
                shadowStats.canUseFlameRing = false;
                break;
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  PUBLIC API – Tails
    // ──────────────────────────────────────────────────────────────────────

    public bool HasUpgrade(TailsUpgrade upgrade) => upgrade switch
    {
        TailsUpgrade.Bazooka => _tails_Bazooka,
        TailsUpgrade.LaserBlaster => _tails_LaserBlaster,
        TailsUpgrade.Booster => _tails_Booster,
        TailsUpgrade.MysticMelody => _tails_MysticMelody,
        _ => false
    };

    public void Unlock(TailsUpgrade upgrade)
    {
        if (HasUpgrade(upgrade)) return;
        SetFlag(upgrade, true);
        ApplyUpgradeEffect(upgrade);
        OnUpgradeUnlocked?.Invoke("Tails", upgrade.ToString());
        SaveUpgrades();
    }

    public void Lock(TailsUpgrade upgrade)
    {
        if (!HasUpgrade(upgrade)) return;
        SetFlag(upgrade, false);
        RemoveUpgradeEffect(upgrade);
        OnUpgradeLocked?.Invoke("Tails", upgrade.ToString());
        SaveUpgrades();
    }

    private void SetFlag(TailsUpgrade u, bool value)
    {
        switch (u)
        {
            case TailsUpgrade.Bazooka: _tails_Bazooka = value; break;
            case TailsUpgrade.LaserBlaster: _tails_LaserBlaster = value; break;
            case TailsUpgrade.Booster: _tails_Booster = value; break;
            case TailsUpgrade.MysticMelody: _tails_MysticMelody = value; break;
        }
    }

    private void ApplyUpgradeEffect(TailsUpgrade upgrade)
    {
        if (tailsStats == null) return;
        switch (upgrade)
        {
            case TailsUpgrade.Bazooka:
                tailsStats.hasLockOnMissile = true;
                break;
            case TailsUpgrade.LaserBlaster:
                tailsStats.laserFireRate += 0.5f;
                break;
            case TailsUpgrade.Booster:
                tailsStats.canHover = true;
                tailsStats.hoverDuration += 2f;
                break;
            case TailsUpgrade.MysticMelody:
                tailsStats.canActivateRuins = true;
                break;
        }
    }

    private void RemoveUpgradeEffect(TailsUpgrade upgrade)
    {
        if (tailsStats == null) return;
        switch (upgrade)
        {
            case TailsUpgrade.Bazooka:
                tailsStats.hasLockOnMissile = false;
                break;
            case TailsUpgrade.LaserBlaster:
                tailsStats.laserFireRate -= 0.5f;
                break;
            case TailsUpgrade.Booster:
                tailsStats.canHover = false;
                tailsStats.hoverDuration -= 2f;
                break;
            case TailsUpgrade.MysticMelody:
                tailsStats.canActivateRuins = false;
                break;
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  PUBLIC API – Eggman
    // ──────────────────────────────────────────────────────────────────────

    public bool HasUpgrade(EggmanUpgrade upgrade) => upgrade switch
    {
        EggmanUpgrade.JetEngine => _eggman_JetEngine,
        EggmanUpgrade.LargeCannon => _eggman_LargeCannon,
        EggmanUpgrade.Laser => _eggman_Laser,
        EggmanUpgrade.MysticMelody => _eggman_MysticMelody,
        _ => false
    };

    public void Unlock(EggmanUpgrade upgrade)
    {
        if (HasUpgrade(upgrade)) return;
        SetFlag(upgrade, true);
        ApplyUpgradeEffect(upgrade);
        OnUpgradeUnlocked?.Invoke("Eggman", upgrade.ToString());
        SaveUpgrades();
    }

    public void Lock(EggmanUpgrade upgrade)
    {
        if (!HasUpgrade(upgrade)) return;
        SetFlag(upgrade, false);
        RemoveUpgradeEffect(upgrade);
        OnUpgradeLocked?.Invoke("Eggman", upgrade.ToString());
        SaveUpgrades();
    }

    private void SetFlag(EggmanUpgrade u, bool value)
    {
        switch (u)
        {
            case EggmanUpgrade.JetEngine: _eggman_JetEngine = value; break;
            case EggmanUpgrade.LargeCannon: _eggman_LargeCannon = value; break;
            case EggmanUpgrade.Laser: _eggman_Laser = value; break;
            case EggmanUpgrade.MysticMelody: _eggman_MysticMelody = value; break;
        }
    }

    private void ApplyUpgradeEffect(EggmanUpgrade upgrade)
    {
        if (eggmanStats == null) return;
        switch (upgrade)
        {
            case EggmanUpgrade.JetEngine:
                eggmanStats.topSpeedMultiplier += 0.2f;
                break;
            case EggmanUpgrade.LargeCannon:
                eggmanStats.cannonDamageMultiplier += 0.5f;
                break;
            case EggmanUpgrade.Laser:
                eggmanStats.hasLaser = true;
                break;
            case EggmanUpgrade.MysticMelody:
                eggmanStats.canActivateRuins = true;
                break;
        }
    }

    private void RemoveUpgradeEffect(EggmanUpgrade upgrade)
    {
        if (eggmanStats == null) return;
        switch (upgrade)
        {
            case EggmanUpgrade.JetEngine:
                eggmanStats.topSpeedMultiplier -= 0.2f;
                break;
            case EggmanUpgrade.LargeCannon:
                eggmanStats.cannonDamageMultiplier -= 0.5f;
                break;
            case EggmanUpgrade.Laser:
                eggmanStats.hasLaser = false;
                break;
            case EggmanUpgrade.MysticMelody:
                eggmanStats.canActivateRuins = false;
                break;
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  PUBLIC API – Knuckles
    // ──────────────────────────────────────────────────────────────────────

    public bool HasUpgrade(KnucklesUpgrade upgrade) => upgrade switch
    {
        KnucklesUpgrade.ShovelClaw => _knuckles_ShovelClaw,
        KnucklesUpgrade.AirNecklace => _knuckles_AirNecklace,
        KnucklesUpgrade.HammerGloves => _knuckles_HammerGloves,
        KnucklesUpgrade.Sunglasses => _knuckles_Sunglasses,
        KnucklesUpgrade.MysticMelody => _knuckles_MysticMelody,
        _ => false
    };

    public void Unlock(KnucklesUpgrade upgrade)
    {
        if (HasUpgrade(upgrade)) return;
        SetFlag(upgrade, true);
        ApplyUpgradeEffect(upgrade);
        OnUpgradeUnlocked?.Invoke("Knuckles", upgrade.ToString());
        SaveUpgrades();
    }

    public void Lock(KnucklesUpgrade upgrade)
    {
        if (!HasUpgrade(upgrade)) return;
        SetFlag(upgrade, false);
        RemoveUpgradeEffect(upgrade);
        OnUpgradeLocked?.Invoke("Knuckles", upgrade.ToString());
        SaveUpgrades();
    }

    private void SetFlag(KnucklesUpgrade u, bool value)
    {
        switch (u)
        {
            case KnucklesUpgrade.ShovelClaw: _knuckles_ShovelClaw = value; break;
            case KnucklesUpgrade.AirNecklace: _knuckles_AirNecklace = value; break;
            case KnucklesUpgrade.HammerGloves: _knuckles_HammerGloves = value; break;
            case KnucklesUpgrade.Sunglasses: _knuckles_Sunglasses = value; break;
            case KnucklesUpgrade.MysticMelody: _knuckles_MysticMelody = value; break;
        }
    }

    private void ApplyUpgradeEffect(KnucklesUpgrade upgrade)
    {
        if (knucklesStats == null) return;
        switch (upgrade)
        {
            case KnucklesUpgrade.ShovelClaw:
                knucklesStats.canDig = true;
                break;
            case KnucklesUpgrade.AirNecklace:
                knucklesStats.glideDuration += 3f;
                break;
            case KnucklesUpgrade.HammerGloves:
                knucklesStats.canBreakHardRocks = true;
                break;
            case KnucklesUpgrade.Sunglasses:
                knucklesStats.canDetectShards = true;
                break;
            case KnucklesUpgrade.MysticMelody:
                knucklesStats.canActivateRuins = true;
                break;
        }
    }

    private void RemoveUpgradeEffect(KnucklesUpgrade upgrade)
    {
        if (knucklesStats == null) return;
        switch (upgrade)
        {
            case KnucklesUpgrade.ShovelClaw:
                knucklesStats.canDig = false;
                break;
            case KnucklesUpgrade.AirNecklace:
                knucklesStats.glideDuration -= 3f;
                break;
            case KnucklesUpgrade.HammerGloves:
                knucklesStats.canBreakHardRocks = false;
                break;
            case KnucklesUpgrade.Sunglasses:
                knucklesStats.canDetectShards = false;
                break;
            case KnucklesUpgrade.MysticMelody:
                knucklesStats.canActivateRuins = false;
                break;
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  PUBLIC API – Rouge
    // ──────────────────────────────────────────────────────────────────────

    public bool HasUpgrade(RougeUpgrade upgrade) => upgrade switch
    {
        RougeUpgrade.PickNails => _rouge_PickNails,
        RougeUpgrade.TreasureScope => _rouge_TreasureScope,
        RougeUpgrade.IronBoots => _rouge_IronBoots,
        RougeUpgrade.MysticMelody => _rouge_MysticMelody,
        _ => false
    };

    public void Unlock(RougeUpgrade upgrade)
    {
        if (HasUpgrade(upgrade)) return;
        SetFlag(upgrade, true);
        ApplyUpgradeEffect(upgrade);
        OnUpgradeUnlocked?.Invoke("Rouge", upgrade.ToString());
        SaveUpgrades();
    }

    public void Lock(RougeUpgrade upgrade)
    {
        if (!HasUpgrade(upgrade)) return;
        SetFlag(upgrade, false);
        RemoveUpgradeEffect(upgrade);
        OnUpgradeLocked?.Invoke("Rouge", upgrade.ToString());
        SaveUpgrades();
    }

    private void SetFlag(RougeUpgrade u, bool value)
    {
        switch (u)
        {
            case RougeUpgrade.PickNails: _rouge_PickNails = value; break;
            case RougeUpgrade.TreasureScope: _rouge_TreasureScope = value; break;
            case RougeUpgrade.IronBoots: _rouge_IronBoots = value; break;
            case RougeUpgrade.MysticMelody: _rouge_MysticMelody = value; break;
        }
    }

    private void ApplyUpgradeEffect(RougeUpgrade upgrade)
    {
        if (rougeStats == null) return;
        switch (upgrade)
        {
            case RougeUpgrade.PickNails:
                rougeStats.canDig = true;
                break;
            case RougeUpgrade.TreasureScope:
                rougeStats.canDetectShardsThroughWalls = true;
                break;
            case RougeUpgrade.IronBoots:
                rougeStats.canBreakHardRocks = true;
                rougeStats.kickDamageMultiplier += 0.3f;
                break;
            case RougeUpgrade.MysticMelody:
                rougeStats.canActivateRuins = true;
                break;
        }
    }

    private void RemoveUpgradeEffect(RougeUpgrade upgrade)
    {
        if (rougeStats == null) return;
        switch (upgrade)
        {
            case RougeUpgrade.PickNails:
                rougeStats.canDig = false;
                break;
            case RougeUpgrade.TreasureScope:
                rougeStats.canDetectShardsThroughWalls = false;
                break;
            case RougeUpgrade.IronBoots:
                rougeStats.canBreakHardRocks = false;
                rougeStats.kickDamageMultiplier -= 0.3f;
                break;
            case RougeUpgrade.MysticMelody:
                rougeStats.canActivateRuins = false;
                break;
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Convenience helpers
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>Unlock every upgrade for every character (debug / new game+ helper).</summary>
    public void UnlockAll()
    {
        foreach (SonicUpgrade u in Enum.GetValues(typeof(SonicUpgrade))) Unlock(u);
        foreach (ShadowUpgrade u in Enum.GetValues(typeof(ShadowUpgrade))) Unlock(u);
        foreach (TailsUpgrade u in Enum.GetValues(typeof(TailsUpgrade))) Unlock(u);
        foreach (EggmanUpgrade u in Enum.GetValues(typeof(EggmanUpgrade))) Unlock(u);
        foreach (KnucklesUpgrade u in Enum.GetValues(typeof(KnucklesUpgrade))) Unlock(u);
        foreach (RougeUpgrade u in Enum.GetValues(typeof(RougeUpgrade))) Unlock(u);
    }

    /// <summary>Lock every upgrade for every character (reset).</summary>
    public void LockAll()
    {
        foreach (SonicUpgrade u in Enum.GetValues(typeof(SonicUpgrade))) Lock(u);
        foreach (ShadowUpgrade u in Enum.GetValues(typeof(ShadowUpgrade))) Lock(u);
        foreach (TailsUpgrade u in Enum.GetValues(typeof(TailsUpgrade))) Lock(u);
        foreach (EggmanUpgrade u in Enum.GetValues(typeof(EggmanUpgrade))) Lock(u);
        foreach (KnucklesUpgrade u in Enum.GetValues(typeof(KnucklesUpgrade))) Lock(u);
        foreach (RougeUpgrade u in Enum.GetValues(typeof(RougeUpgrade))) Lock(u);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Save / Load  (PlayerPrefs + JsonUtility)
    // ──────────────────────────────────────────────────────────────────────

    public void SaveUpgrades()
    {
        var data = new SA2UpgradeSaveData
        {
            // Sonic
            sonic_LightShoes = _sonic_LightShoes,
            sonic_AncientLight = _sonic_AncientLight,
            sonic_BounceBracelet = _sonic_BounceBracelet,
            sonic_FlameRing = _sonic_FlameRing,
            sonic_MagicGloves = _sonic_MagicGloves,
            // Shadow
            shadow_AirShoes = _shadow_AirShoes,
            shadow_AncientLight = _shadow_AncientLight,
            shadow_FlameRing = _shadow_FlameRing,
            // Tails
            tails_Bazooka = _tails_Bazooka,
            tails_LaserBlaster = _tails_LaserBlaster,
            tails_Booster = _tails_Booster,
            tails_MysticMelody = _tails_MysticMelody,
            // Eggman
            eggman_JetEngine = _eggman_JetEngine,
            eggman_LargeCannon = _eggman_LargeCannon,
            eggman_Laser = _eggman_Laser,
            eggman_MysticMelody = _eggman_MysticMelody,
            // Knuckles
            knuckles_ShovelClaw = _knuckles_ShovelClaw,
            knuckles_AirNecklace = _knuckles_AirNecklace,
            knuckles_HammerGloves = _knuckles_HammerGloves,
            knuckles_Sunglasses = _knuckles_Sunglasses,
            knuckles_MysticMelody = _knuckles_MysticMelody,
            // Rouge
            rouge_PickNails = _rouge_PickNails,
            rouge_TreasureScope = _rouge_TreasureScope,
            rouge_IronBoots = _rouge_IronBoots,
            rouge_MysticMelody = _rouge_MysticMelody
        };

        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public void LoadUpgrades()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return;

        var data = JsonUtility.FromJson<SA2UpgradeSaveData>(PlayerPrefs.GetString(SAVE_KEY));
        if (data == null) return;

        // Sonic
        _sonic_LightShoes = data.sonic_LightShoes;
        _sonic_AncientLight = data.sonic_AncientLight;
        _sonic_BounceBracelet = data.sonic_BounceBracelet;
        _sonic_FlameRing = data.sonic_FlameRing;
        _sonic_MagicGloves = data.sonic_MagicGloves;
        // Shadow
        _shadow_AirShoes = data.shadow_AirShoes;
        _shadow_AncientLight = data.shadow_AncientLight;
        _shadow_FlameRing = data.shadow_FlameRing;
        // Tails
        _tails_Bazooka = data.tails_Bazooka;
        _tails_LaserBlaster = data.tails_LaserBlaster;
        _tails_Booster = data.tails_Booster;
        _tails_MysticMelody = data.tails_MysticMelody;
        // Eggman
        _eggman_JetEngine = data.eggman_JetEngine;
        _eggman_LargeCannon = data.eggman_LargeCannon;
        _eggman_Laser = data.eggman_Laser;
        _eggman_MysticMelody = data.eggman_MysticMelody;
        // Knuckles
        _knuckles_ShovelClaw = data.knuckles_ShovelClaw;
        _knuckles_AirNecklace = data.knuckles_AirNecklace;
        _knuckles_HammerGloves = data.knuckles_HammerGloves;
        _knuckles_Sunglasses = data.knuckles_Sunglasses;
        _knuckles_MysticMelody = data.knuckles_MysticMelody;
        // Rouge
        _rouge_PickNails = data.rouge_PickNails;
        _rouge_TreasureScope = data.rouge_TreasureScope;
        _rouge_IronBoots = data.rouge_IronBoots;
        _rouge_MysticMelody = data.rouge_MysticMelody;

        // Re-apply all active effects after loading
        ReapplyAllEffects();
    }

    /// <summary>Re-drives stat components after a load so they match the saved flags.</summary>
    private void ReapplyAllEffects()
    {
        foreach (SonicUpgrade u in Enum.GetValues(typeof(SonicUpgrade))) { if (HasUpgrade(u)) ApplyUpgradeEffect(u); }
        foreach (ShadowUpgrade u in Enum.GetValues(typeof(ShadowUpgrade))) { if (HasUpgrade(u)) ApplyUpgradeEffect(u); }
        foreach (TailsUpgrade u in Enum.GetValues(typeof(TailsUpgrade))) { if (HasUpgrade(u)) ApplyUpgradeEffect(u); }
        foreach (EggmanUpgrade u in Enum.GetValues(typeof(EggmanUpgrade))) { if (HasUpgrade(u)) ApplyUpgradeEffect(u); }
        foreach (KnucklesUpgrade u in Enum.GetValues(typeof(KnucklesUpgrade))) { if (HasUpgrade(u)) ApplyUpgradeEffect(u); }
        foreach (RougeUpgrade u in Enum.GetValues(typeof(RougeUpgrade))) { if (HasUpgrade(u)) ApplyUpgradeEffect(u); }
    }
}

#endregion

// ─────────────────────────────────────────────────────────────────────────────
//  Character Stat Components
//  Attach these to each character's GameObject. SA2Upgrades will find them
//  via the Inspector references and mutate the fields on unlock / lock.
// ─────────────────────────────────────────────────────────────────────────────

public class SonicStats : MonoBehaviour
{
    [Header("Movement")]
    public bool canAirDash;
    public bool canLightSpeedDash;

    [Header("Attacks")]
    public bool canLightSpeedAttack;
    public bool canBounceAttack;
    public bool canUseFlameRing;
    public bool canUseMagicHands;
}

public class ShadowStats : MonoBehaviour
{
    [Header("Movement")]
    public bool canAirDash;
    public bool canLightSpeedDash;

    [Header("Attacks")]
    public bool canLightSpeedAttack;
    public bool canUseFlameRing;
}

public class TailsStats : MonoBehaviour
{
    [Header("Weapons")]
    public bool hasLockOnMissile;
    public float laserFireRate = 1f;

    [Header("Movement")]
    public bool canHover;
    public float hoverDuration = 2f;

    [Header("World")]
    public bool canActivateRuins;
}

public class EggmanStats : MonoBehaviour
{
    [Header("Movement")]
    public float topSpeedMultiplier = 1f;

    [Header("Weapons")]
    public float cannonDamageMultiplier = 1f;
    public bool hasLaser;

    [Header("World")]
    public bool canActivateRuins;
}

public class KnucklesStats : MonoBehaviour
{
    [Header("Abilities")]
    public bool canDig;
    public bool canBreakHardRocks;
    public bool canDetectShards;
    public float glideDuration = 3f;

    [Header("World")]
    public bool canActivateRuins;
}

public class RougeStats : MonoBehaviour
{
    [Header("Abilities")]
    public bool canDig;
    public bool canBreakHardRocks;
    public bool canDetectShardsThroughWalls;
    public float kickDamageMultiplier = 1f;

    [Header("World")]
    public bool canActivateRuins;
}
