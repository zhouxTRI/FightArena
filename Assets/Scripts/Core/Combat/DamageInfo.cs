using UnityEngine;

namespace FightArena.Core.Combat
{
    public readonly struct DamageInfo                       //只读结构体，用于表示伤害信息
    {
        public int SourceInstanceId { get; }                //伤害来源的实例ID
        public int Amount { get; }                          //伤害数值
        public Vector2 HitPoint { get; }                    //伤害发生的点
        public Vector2 HitDirection { get; }                //伤害的方向

        public DamageInfo(
            int sourceInstanceId,
            int amount,
            Vector2 hitPoint,
            Vector2 hitDirection)
        {
            SourceInstanceId = sourceInstanceId;
            Amount = Mathf.Max(0, amount);
            HitPoint = hitPoint;
            HitDirection = hitDirection;
        }
    }
}
