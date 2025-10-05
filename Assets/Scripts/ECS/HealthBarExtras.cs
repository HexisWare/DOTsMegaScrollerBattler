using Unity.Entities;

// Add these to each bar entity (Bg/Fill) when you spawn them:
public struct HealthBarOwner : IComponentData
{
    public Entity Owner;   // who this bar belongs to
}

public struct HealthBarElement : IComponentData
{
    // tag on all health-bar quads (Bg/Fill)
}
