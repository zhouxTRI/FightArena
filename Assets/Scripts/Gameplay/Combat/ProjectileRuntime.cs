using FightArena.Core.Combat;
using UnityEngine;

namespace FightArena.Gameplay.Combat
{
    public sealed class ProjectileRuntime : MonoBehaviour           //投射物运行时组件，用于处理投射物的行为和逻辑
    {
                                                                    //这里不用Rigidbody2D和Collider2D组件，而是使用射线检测来处理投射物的碰撞逻辑;
        [Header("Collision")]                                       //好处是可以避免物理引擎的复杂性和性能开销，同时可以更精确地控制投射物的碰撞检测和命中逻辑；
        [SerializeField] private LayerMask targetMask;              //投射物的目标层掩码，用于指定投射物可以命中的目标对象
        [SerializeField] private LayerMask obstacleMask;            //投射物的障碍物层掩码，用于指定投射物会被阻挡的障碍物对象

        [Header("Presentation")]
        [SerializeField] private TrailRenderer[] trails;            //投射物的尾迹渲染器数组，用于在投射物飞行过程中显示尾迹效果
        [SerializeField] private ParticleSystem[] particles;        //投射物的粒子系统数组，用于在投射物飞行过程中显示粒子效果

        private readonly RaycastHit2D[] hits = new RaycastHit2D[16];  //用于存储射线检测的命中结果的数组，最大长度为16(考虑到性能和内存开销，限制了最大命中数量)

        private ProjectileService owner;                            //投射物服务的引用，用于管理投射物的生命周期和行为
        private ProjectileTeam team;
        private Vector2 direction;
        private int projectileId;
        private int sourceInstanceId;
        private int damage;
        private float speed;
        private float maxDistance;
        private float lifetime;
        private float traveledDistance;                             //投射物已经飞行的距离，用于判断投射物是否超过最大飞行距离
        private float remainingLifetime;                            //投射物剩余的生命周期，用于判断投射物是否超过最大存在时间
        private bool active;

        public bool IsActive => active;                             //投射物是否处于激活状态的只读属性，用于外部判断投射物的状态

        private void Awake()
        {
            if(trails == null || trails.Length == 0)
            {
                trails = GetComponentsInChildren<TrailRenderer>(true);        //如果尾迹渲染器数组为空，则尝试从子对象中获取尾迹渲染器组件
            }
            if(particles == null || particles.Length == 0)
            {
                particles = GetComponentsInChildren<ParticleSystem>(true);     //如果粒子系统数组为空，则尝试从子对象中获取粒子系统组件
            }
        }

        internal void Launch(                                           //用于初始化投射物的发射数据和状态，internal方法确保不能被外部类直接调用
            in ProjectileLaunchData launchData,
            ProjectileService service,
            ProjectileTeam runtimeTeam,
            int runtimeProjectileId
        )
        {
            owner = service;
            team = runtimeTeam;
            projectileId = runtimeProjectileId;
            sourceInstanceId = launchData.SourceInstanceId;
            direction = launchData.Direction.sqrMagnitude > 0.000001f
                ? launchData.Direction.normalized
                : Vector2.right;                                        
            damage = launchData.Damage;
            speed = launchData.Speed;
            maxDistance = launchData.MaxDistance;
            lifetime = launchData.Lifetime;
            traveledDistance = 0f;                                      //投射物已经飞行的距离初始化为0
            remainingLifetime = lifetime;
            active = true;

            transform.position = launchData.Origin;
            transform.rotation = Quaternion.Euler(                      //设置投射物的旋转角度，使其朝向发射方向
                0f,                                                         //绕X轴的旋转角度，设置为0f表示不进行绕X轴的旋转
                0f,                                                         //绕Y轴的旋转角度
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg       //绕Z轴的旋转角度，使用Mathf.Atan2计算方向向量的角度，并将其转换为度数
            );
            
            //这里注意先清除的是对象池复用带来的脏历史，不是你要展示的当前效果；清除后立即重新启用，所以内容从发射点重新开始
            for(int i = 0; i < trails.Length; i++)                      
            {
                if(trails[i] == null)
                {
                    continue;
                }
                trails[i].Clear();                                       //清除尾迹渲染器的历史数据，确保尾迹效果从发射点开始
                trails[i].emitting = true;                               //启用尾迹渲染器的发射状态，使其开始渲染尾迹效果
            }
            for(int i = 0; i < particles.Length; i++)
            {
                if(particles[i] == null)
                {
                    continue;
                }                       
                particles[i].Clear(true);                               //清除粒子系统的历史数据，确保粒子效果从发射点开始
                particles[i].Play(true);                                //播放粒子系统的效果，使其在投射物飞行过程中显示粒子效果
            }
        }

        internal void TickFixed(float deltaTime)                        //用于在FixedUpdate中更新投射物的状态和行为，internal方法确保不能被外部类直接调用
        {
            if(!active || deltaTime <= 0f || speed <= 0f)
            {
                return;
            }
            
            //计算投射物的移动步长和碰撞检测掩码，并使用Physics2D.RaycastNonAlloc进行射线检测，获取命中结果
            Vector2 start = transform.position;
            float step = speed * deltaTime;
            int collisionMask = targetMask.value | obstacleMask.value;     //将目标层掩码和障碍物层掩码进行按位或运算，得到投射物的碰撞检测掩码
            int hitCount = Physics2D.RaycastNonAlloc(
                start,
                direction,
                hits,
                step,
                collisionMask
            );

            float nearestWallDistance = float.PositiveInfinity;             //用于存储最近的墙壁碰撞距离，初始值为正无穷大
            float nearestTargetDistance = float.PositiveInfinity;
            RaycastHit2D nearestWallHit = default;                          //用于存储最近的墙壁碰撞信息，默认值是一个空的RaycastHit2D结构体
            RaycastHit2D nearestTargetHit = default;

            //遍历射线检测的命中结果，找到最近的墙壁碰撞和目标碰撞
            for(int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = hits[i];
                if(hit.collider == null || hit.collider.isTrigger)          //如果碰撞体为空或者是触发器，则跳过该碰撞结果，继续处理下一个碰撞结果
                {
                    continue;
                }

                int hitLayerBit = 1 << hit.collider.gameObject.layer;                  //获取碰撞体所在的层的位掩码，用于判断碰撞体是否属于目标层或障碍物层
                
                if((obstacleMask.value & hitLayerBit) != 0)                            //如果碰撞体属于障碍物层，则更新最近的墙壁碰撞信息
                {
                    if(hit.distance < nearestWallDistance)
                    {
                        nearestWallDistance = hit.distance;
                        nearestWallHit = hit;
                    }
                }
                else if((targetMask.value & hitLayerBit) != 0)
                {
                    if(hit.distance < nearestTargetDistance)
                    {
                        nearestTargetDistance = hit.distance;
                        nearestTargetHit = hit;
                    }
                }
            }

            //只有确实检测到墙壁，才允许进入墙壁命中分支
            if(nearestWallDistance < float.PositiveInfinity &&
               nearestWallDistance <= nearestTargetDistance)
            {
                Vector2 hitPoint = nearestWallHit.point;
                transform.position = hitPoint;
                ReportHit(hitPoint, nearestWallHit.collider, true, false);              //报告投射物命中墙壁的结果，传递命中点、碰撞体、是否命中墙壁和是否成功应用伤害的标志
                ReturnToPool();                                                         //将投射物返回对象池，结束投射物的生命周期
                return;
            }
            //如果最近的目标碰撞距离小于正无穷大，则说明投射物命中了目标，处理目标碰撞逻辑
            if(nearestTargetDistance < float.PositiveInfinity)
            {
                Vector2 hitPoint = nearestTargetHit.point;
                transform.position = hitPoint;

                IDamageable damageable =
                    nearestTargetHit.collider.GetComponentInParent<IDamageable>();      //尝试获取命中目标的IDamageable组件，用于应用伤害
                bool damageApplied = false;

                if(damageable != null)
                {
                    DamageInfo damageInfo = new DamageInfo(
                        sourceInstanceId,
                        damage,
                        hitPoint,
                        direction
                    );
                    damageApplied = damageable.TryApplyDamage(damageInfo);          //尝试对命中目标应用伤害，返回是否成功应用伤害的标志
                }

                ReportHit(
                    hitPoint,
                    nearestTargetHit.collider,
                    false,
                    damageApplied
                );
                ReturnToPool();
                return;
            }

            transform.position = start + direction * step;                 //如果没有命中墙壁或目标，则更新投射物的位置，沿着方向移动步长
            traveledDistance += step;                                      //更新投射物已经飞行的距离
            remainingLifetime -= deltaTime;                                //更新投射物剩余的生命周期

            if(traveledDistance >= maxDistance || remainingLifetime <= 0f) //如果投射物已经超过最大飞行距离或生命周期结束，则将投射物返回对象池
            {
                ReturnToPool();
            }
        }

        //关闭投射物的生命周期的方法
        internal void Deactivate()                                        
        {
            active = false;
            owner = null;
            for(int i = 0; i < trails.Length; i++)
            {
                if(trails[i] != null)
                {
                    trails[i].emitting = false;                          //停用尾迹渲染器的发射状态，使其停止渲染尾迹效果
                    trails[i].Clear();                                   //清除尾迹渲染器的历史数据，确保尾迹效果不再显示
                }
            }
            for(int i = 0; i < particles.Length; i++)
            {
                if(particles[i] != null)
                {
                    particles[i].Stop(true);                             //停止粒子系统的播放，使其不再显示粒子效果
                    particles[i].Clear(true);                            //清除粒子系统的历史数据，确保粒子效果不再显示
                }
            }
            gameObject.SetActive(false);                                 //将投射物的游戏对象设置为不激活状态，使其从场景中隐藏
        }

        //将投射物返回对象池的方法
        private void ReturnToPool()
        {
            if(owner != null)
            {
                owner.Release(this);                                    //调用投射物服务的Release方法，将投射物返回对象池
            }
        }

        //报告投射物命中结果的方法
        private void ReportHit(
            Vector2 point,
            Collider2D collider,
            bool hitWall,
            bool damageApplied
        )
        {
            if(owner == null)
            {
                return;
            }

            owner.ReportHit(
                projectileId,
                team,
                point,
                collider,
                hitWall,
                damageApplied);
        }



    }
}
