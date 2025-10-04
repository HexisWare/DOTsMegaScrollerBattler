using Unity.Entities;
using Unity.Mathematics;

public struct ProjectileDefaults : IComponentData
{
    public float  Speed;
    public float  Radius;
    public float  Life;
    public float4 PlayerColor; // linear RGBA
    public float4 EnemyColor;  // linear RGBA
}
