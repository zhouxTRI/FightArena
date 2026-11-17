using System;

namespace FightArena.Core.Input
{
    [Flags]                             //用这个特性是标记这个枚举可以作为位标志使用
    public enum InputLockFlags          //用于标记输入锁定的状态
    {
        None = 0,
        Inventory = 1 << 0,     // 1，目录状态
        Pause = 1 << 1,         // 2，暂停状态
        Victory = 1 << 2,       // 4，胜利状态
        Defeat = 1 << 3,        // 8，失败状态
        Modal = 1 << 4          // 16，模态状态（比如弹窗）
    }
}
