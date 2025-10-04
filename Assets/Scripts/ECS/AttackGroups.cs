using Unity.Entities;

public enum GroupKind : byte { Ground = 0, Air = 1, Orbital = 2 }

public struct AttackGroup : IComponentData
{
    public GroupKind Value;
}
