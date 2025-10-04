using Unity.Entities;

public struct BuildingTarget : IComponentData {}
public struct BuildingHitbox : IComponentData { public float Radius; }
public struct Team : IComponentData { public Faction Value; }
