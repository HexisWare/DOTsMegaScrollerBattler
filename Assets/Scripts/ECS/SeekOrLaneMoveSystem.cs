using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ResolveCollisionsSystem))] // move after collisions are resolved
public partial struct SeekOrLaneMoveSystem : ISystem
{
    private ComponentLookup<LocalTransform> _xfLookup; // random access to target positions

    public void OnCreate(ref SystemState s)
    {
        _xfLookup = s.GetComponentLookup<LocalTransform>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        _xfLookup.Update(ref s);
        float dt = SystemAPI.Time.DeltaTime;

        // Don't move entities that are flagged as in-collision to avoid tunneling.
        foreach (var (xf, agent, e) in SystemAPI
                     .Query<RefRW<LocalTransform>, RefRO<Agent>>()
                     .WithNone<InCollisionRange>()
                     .WithEntityAccess())
        {
            float3 pos = xf.ValueRO.Position;
            float3 vel;

            // If we have a target in range, steer toward it. Otherwise, lane move.
            if (SystemAPI.HasComponent<Target>(e))
            {
                var t = SystemAPI.GetComponentRO<Target>(e).ValueRO.Value;
                if (_xfLookup.HasComponent(t))
                {
                    float3 tpos = _xfLookup[t].Position;
                    float3 to   = tpos - pos;
                    float len   = math.length(to);
                    if (len > 1e-5f)
                    {
                        float3 dir = to / len; // normalized direction to target
                        vel = dir * agent.ValueRO.MoveSpeed;
                    }
                    else
                    {
                        // Fallback to lane direction if we're exactly on top (rare)
                        float dirX = (agent.ValueRO.Faction == Faction.Player) ? 1f : -1f;
                        vel = new float3(dirX * agent.ValueRO.MoveSpeed, 0, 0);
                    }
                }
                else
                {
                    // Target entity vanished — go lane
                    float dirX = (agent.ValueRO.Faction == Faction.Player) ? 1f : -1f;
                    vel = new float3(dirX * agent.ValueRO.MoveSpeed, 0, 0);
                }
            }
            else
            {
                // No target in DetectRange → straight lane travel
                float dirX = (agent.ValueRO.Faction == Faction.Player) ? 1f : -1f;
                vel = new float3(dirX * agent.ValueRO.MoveSpeed, 0, 0);
            }

            xf.ValueRW.Position = pos + vel * dt;
        }
    }
}
