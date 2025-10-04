using Unity.Entities;
using Unity.Mathematics;

public struct HealthBarChild : IComponentData
{
    public Entity Bg;          // background bar entity
    public Entity Fill;        // foreground (scaled) bar entity
    public float  Width;       // world units
    public float  Height;      // world units
    public float3 Offset;      // local offset above the unit
}
