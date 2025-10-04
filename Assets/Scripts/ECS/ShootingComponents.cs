using Unity.Entities;
using Unity.Mathematics;

public struct Shooter : IComponentData
{
    public float Range;
    public float FireCooldown;
    public float Damage;
    public int   TargetMask;   // bitmask: bit(GroupKind)
}

public struct ShooterCooldown : IComponentData { public float TimeLeft; }

public struct Projectile : IComponentData
{
    public Faction Faction;
    public float Radius;
    public float Damage;
}

public struct Velocity : IComponentData { public float3 Value; }
public struct NeedsRenderSetup : IComponentData {}
