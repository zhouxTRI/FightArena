using FightArena.Core.Input;
using UnityEngine;

namespace FightArena.Gameplay.Player
{
    public sealed class PlayerInput : MonoBehaviour
    {
        [SerializeField] private InputRouter inputRouter;                           //输入路由器引用(通常在Player Prefab实例上填写)

        public InputIntent CurrentInputIntent
        {
            get
            {
                if(inputRouter == null)
                {
                    return default;                                                //如果输入路由器为空，则返回默认的输入意图
                }
                return inputRouter.CurrentInputIntent;                             //返回当前的输入意图
            }
        }
        private void Awake()
        {
            if(inputRouter == null)
            {
                Debug.LogError(
                    "PlayerInput requires a reference to Systems/InputRouter.",
                    this);
                enabled = false;                                                      //如果输入路由器为空，则禁用此组件
            }
        }
        private void Reset()
        {
            inputRouter = FindObjectOfType<InputRouter>();                          //在重置时，尝试找到场景中的InputRouter
        }
    }
}