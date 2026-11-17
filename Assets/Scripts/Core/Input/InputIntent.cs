using UnityEngine;

namespace FightArena.Core.Input
{
    public readonly struct InputIntent              //只读结构体，用于表示输入意图
    {
        public Vector2 Move { get; }
        public Vector2 AimWorldPosition { get; }
        public bool FireHeld { get; }
        public bool FirePressed { get; }
        public bool DodgePressed { get; }
        public bool InteractPressed { get; }
        public bool PausePressed { get; }
        public bool InventoryPressed { get; }

        // 构造
        public InputIntent(
            Vector2 move, 
            Vector2 aimWorldPosition, 
            bool fireHeld, 
            bool firePressed, 
            bool dodgePressed, 
            bool interactPressed, 
            bool pausePressed, 
            bool inventoryPressed)
        {
            Move = move;
            AimWorldPosition = aimWorldPosition;
            FireHeld = fireHeld;
            FirePressed = firePressed;
            DodgePressed = dodgePressed;
            InteractPressed = interactPressed;
            PausePressed = pausePressed;
            InventoryPressed = inventoryPressed;
        }
    }
}
