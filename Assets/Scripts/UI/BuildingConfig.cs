using System;

[Serializable] public class BuildingSetConfig { public BuildingConfigBox[] boxes; }

[Serializable]
public class BuildingConfigBox
{
    public string id;        // "Top" | "Middle" | "Bottom"
    public string group;     // "Ground" | "Air" | "Orbital"
    public int    hp;        // 200
    public float  cooldown;  // attack speed for buildings

    // Optional (safe defaults if omitted):
    public float  shootRange; // meters (default 7.5)
    public int    damage;     // default 1
    public string[] canAttack; // default all groups

    public float  moveSpeed;
}
