using Unity.Entities;
using Unity.Mathematics;

public enum Faction : byte { Player = 0, Enemy = 1 }

public struct Agent : IComponentData
{
    public Faction Faction;
    public float MoveSpeed;
    public float DetectRange; // how far we "sense" enemies
    public float Radius;      // collision proximity
}

public struct Target : IComponentData
{
    public Entity Value; // opposing agent
}

public struct InCollisionRange : IComponentData {} // marker

public struct Lifetime : IComponentData { public float Seconds; } // optional, unused here
