using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ShootingSystem))]
public partial struct SeekOrLaneMoveSystem : ISystem
{
    private ComponentLookup<LocalTransform> _xfLookup;

    public void OnCreate(ref SystemState s)
    {
        _xfLookup = s.GetComponentLookup<LocalTransform>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState s)
    {
        _xfLookup.Update(ref s);
        float dt = SystemAPI.Time.DeltaTime;

        foreach (var (xf, agent, e) in SystemAPI
                     .Query<RefRW<LocalTransform>, RefRO<Agent>>()
                     .WithNone<InCollisionRange>()   // your existing no-move-if-colliding tag
                     .WithNone<Attacking>()          // ← NEW: hold position while attacking
                     .WithEntityAccess())
        {
            float3 pos = xf.ValueRO.Position;
            float3 vel;

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
                        float3 dir = to / len;
                        vel = dir * agent.ValueRO.MoveSpeed;
                    }
                    else
                    {
                        float dirX = (agent.ValueRO.Faction == Faction.Player) ? 1f : -1f;
                        vel = new float3(dirX * agent.ValueRO.MoveSpeed, 0, 0);
                    }
                }
                else
                {
                    float dirX = (agent.ValueRO.Faction == Faction.Player) ? 1f : -1f;
                    vel = new float3(dirX * agent.ValueRO.MoveSpeed, 0, 0);
                }
            }
            else
            {
                float dirX = (agent.ValueRO.Faction == Faction.Player) ? 1f : -1f;
                vel = new float3(dirX * agent.ValueRO.MoveSpeed, 0, 0);
            }

            xf.ValueRW.Position = pos + vel * dt;
        }
    }
}
