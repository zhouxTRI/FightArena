using UnityEngine;

namespace FightArena.Core.Combat
{
    public readonly struct EnemyKilledInfo
    {
        public int EnemyInstanceId { get; }
        public string MonsterId { get; }
        public int EnemyKindId { get; }
        public Vector2 Position { get; }
        public int KillerInstanceId { get; }

        public EnemyKilledInfo(
            int enemyInstanceId,
            string monsterId,
            int enemyKindId,
            Vector2 position,
            int killerInstanceId)
        {
            EnemyInstanceId = enemyInstanceId;
            MonsterId = monsterId;
            EnemyKindId = enemyKindId;
            Position = position;
            KillerInstanceId = killerInstanceId;
        }
    }
}
