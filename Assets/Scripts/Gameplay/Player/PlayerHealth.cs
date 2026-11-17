using System;
using FightArena.Core.Combat;
using UnityEngine;

namespace FightArena.Gameplay.Player
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 100;

        private int currentHealth;
        private bool isDead;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => isDead;
        public bool IsInvulnerable { get; private set; }                    //玩家是否处于无敌状态，外部只能读取，内部可以设置

        public event Action<DamageInfo> Damaged;                            //当玩家受到伤害时触发的事件，传递DamageInfo参数
        public event Action Died;                                           //当玩家死亡时触发的事件

        private void Awake()
        {
            ResetHealth();
        }

        public void ResetHealth()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            currentHealth = maxHealth;
            isDead = false;
            IsInvulnerable = false;
        }

        // 尝试应用伤害信息，如果玩家处于无敌状态或已经死亡，或者伤害数值小于等于0，则返回false，否则应用伤害并返回true
        public bool TryApplyDamage(in DamageInfo damage)
        {
            if (isDead || IsInvulnerable || damage.Amount <= 0)
            {
                return false;
            }

            currentHealth = Mathf.Max(0, currentHealth - damage.Amount);
            Damaged?.Invoke(damage);

            if (currentHealth == 0)
            {
                Die();
            }

            return true;
        }

        public void SetInvulnerable(bool value)
        {
            if (isDead)
            {
                IsInvulnerable = false;
                return;
            }

            IsInvulnerable = value;
        }

        private void Die()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            IsInvulnerable = false;
            Died?.Invoke();
        }

        // 在编辑器中验证最大生命值，确保其不小于1
        private void OnValidate()
        {
            maxHealth = Mathf.Max(1, maxHealth);
        }
    }
}
