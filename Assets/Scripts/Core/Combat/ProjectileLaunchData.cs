using UnityEngine;

namespace FightArena.Core.Combat
{
    public enum ProjectileTeam                      //投射物的团队枚举类型，用于区分投射物属于玩家还是敌人
    {
        Player,
        Enemy
    }

    public readonly struct ProjectileLaunchData     //投射物发射数据的结构体，用于封装投射物发射时所需的各种参数
    {
        public ProjectileTeam Team { get; }
        public int SourceInstanceId { get; }        //投射物来源的实例ID，用于标识投射物的来源对象
        public Vector2 Origin { get; }              //投射物的起始位置，用于确定投射物的发射点
        public Vector2 Direction { get; }           //投射物的方向向量，用于确定投射物的飞行方向
        public int Damage { get; }                  //投射物的伤害值，用于确定投射物对目标造成的伤害量
        public float Speed { get; }                 //投射物的速度，用于确定投射物的飞行速度
        public float MaxDistance { get; }           //投射物的最大飞行距离，用于限制投射物的飞行范围
        public float Lifetime { get; }              //投射物的生命周期，用于确定投射物在场景中存在的时间

        public ProjectileLaunchData(
            ProjectileTeam team,
            int sourceInstanceId,
            Vector2 origin, 
            Vector2 direction,
            int damage,
            float speed,
            float maxDistance,
            float lifetime)
        {
            Team = team;
            SourceInstanceId = sourceInstanceId;
            Origin = origin;
            Direction = direction.sqrMagnitude > 0.000001f
                ? direction.normalized
                : Vector2.right;                            //无效的方向向量时，默认使用 Vector2.right 作为方向      
            Damage = Mathf.Max(0,damage);
            Speed = Mathf.Max(0f,speed);
            MaxDistance = Mathf.Max(0f,maxDistance);
            Lifetime = Mathf.Max(0f,lifetime);
        }
    }
}