using UnityEngine;

namespace FightArena.Core.Combat
{
    public readonly struct ProjectileHitResult              //投射物命中结果的结构体，用于表示投射物与目标之间的碰撞信息
    {
        public int ProjectileId { get; }                       //投射物的实例ID，用于标识命中的投射物
        public ProjectileTeam Team { get; }                    //投射物的团队枚举类型，用于区分命中的投射物属于玩家还是敌人
        public Vector2 Point { get; }                          //命中点的坐标，用于表示投射物与目标碰撞的具体位置
        public Collider2D Collider { get; }                    //命中的碰撞体，用于表示投射物与目标碰撞的具体对象
        public bool HitWall { get; }                           //是否命中墙壁，用于表示投射物是否与墙壁发生碰撞
        public bool DamageApplied { get; }                     //是否成功应用伤害，用于表示投射物是否对目标造成了伤害

        public ProjectileHitResult(
            int projectileId,
            ProjectileTeam team,
            Vector2 point,
            Collider2D collider,
            bool hitWall,
            bool damageApplied)
        {
            ProjectileId = projectileId;
            Team = team;
            Point = point;
            Collider = collider;
            HitWall = hitWall;
            DamageApplied = damageApplied;
        }
    }
}