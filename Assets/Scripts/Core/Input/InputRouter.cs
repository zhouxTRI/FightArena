using UnityEngine;

namespace FightArena.Core.Input
{   
    //将原始输入意图转换为游戏逻辑所需的输入意图
    [DefaultExecutionOrder(-100)]                   //确保InputRouter在其他脚本之前执行
    public sealed class InputRouter : MonoBehaviour //sealed类，防止继承
    {
        [Header("References")]
        [SerializeField] private Camera aimCamera; //用于将屏幕坐标转换为世界坐标的摄像机引用

        [Header("Options")]
        [SerializeField] private bool enableGameplayInput = true; //Gameplay输入是否启用

        private readonly Plane aimPlane = new Plane(Vector3.forward, Vector3.zero); //定义一个Z=0平面，用于计算瞄准位置
        private Vector2 lastValidAimWorldPosition;                                  //存储上一次有效的瞄准世界位置
        private InputLockFlags activeInputLocks;             //当前活动的输入锁定标志
        private InputIntent currentInputIntent;              //当前的输入意图
        private bool hasValidAimPosition;                    //是否有有效的瞄准位置
        private bool warnedMissingAimCamera;                 //是否已经警告过缺少瞄准摄像机

        public InputIntent CurrentInputIntent => currentInputIntent;    //公开当前的输入意图
        public InputLockFlags ActiveInputLocks => activeInputLocks;     //公开当前的输入锁定标志
        
        public bool CombatInputAllowed
        {
            get
            {
                //如果启用了Gameplay输入，并且没有活动的输入锁定，则允许战斗输入
                return enableGameplayInput && 
                       activeInputLocks == InputLockFlags.None;
            }
        }

        private void Awake()
        {
            if(aimCamera == null)
            {
                aimCamera = Camera.main;                                    //如果没有指定瞄准摄像机，则使用主摄像机
            }

            if (aimCamera == null)
            {
                WarnMissingAimCameraOnce();
            }
        }

        private void Update()
        {
            //获取移动输入
            Vector2 rawMove = new Vector2(
                UnityEngine.Input.GetAxisRaw("Horizontal"), 
                UnityEngine.Input.GetAxisRaw("Vertical")
            );

            if(rawMove.sqrMagnitude > 1f)
            {
                rawMove.Normalize();                                        //移动输入归一化
            }

            Vector2 aimWorldPosition = ResolveAimWorldPosition();       
            bool combatAllowed = CombatInputAllowed;   
            Vector2 move = combatAllowed ? rawMove : Vector2.zero;                              //如果允许战斗输入，则使用移动输入，否则为零
            bool fireHeld = combatAllowed && UnityEngine.Input.GetButton("Fire1");              //如果允许战斗输入，则获取开火按钮状态
            bool firePressed = combatAllowed && UnityEngine.Input.GetButtonDown("Fire1");       //如果允许战斗输入，则获取开火按钮按下状态
            bool dodgePressed = combatAllowed && UnityEngine.Input.GetButtonDown("Dodge");      //如果允许战斗输入，则获取闪避按钮按下状态
            bool interactPressed = combatAllowed && UnityEngine.Input.GetButtonDown("Interact"); //战斗输入允许时才获取交互按钮
            bool pausePressed = UnityEngine.Input.GetButtonDown("Pause");                       //获取暂停按钮
            bool inventoryPressed = UnityEngine.Input.GetButtonDown("ToggleInventory");         //切换背包

            currentInputIntent = new InputIntent(
                move, 
                aimWorldPosition, 
                fireHeld, 
                firePressed, 
                dodgePressed, 
                interactPressed, 
                pausePressed, 
                inventoryPressed
            );
        }

        public void AddLock(InputLockFlags lockFlag)                                           //添加输入锁
        {
            if(lockFlag == InputLockFlags.None)
            {
                return;
            }
            activeInputLocks |= lockFlag;                                                      //使用按位或运算符添加锁定标志
            ClearCombatIntentImmediately();
        }
        public void RemoveLock(InputLockFlags lockFlag)                                        //移除输入锁
        {
            if(lockFlag == InputLockFlags.None)
            {
                return;
            }
            activeInputLocks &= ~lockFlag;                                                     //使用按位与运算符移除锁定标志
        }
        public void ClearLocks()
        {
            activeInputLocks = InputLockFlags.None;
        }
        public void SetGameplayInputEnabled(bool enabled)
        {
            enableGameplayInput = enabled;
            if(!enabled)
            {
                ClearCombatIntentImmediately();
            }
        }

        // 警告缺少瞄准摄像机
        private void WarnMissingAimCameraOnce()
        {
            if (warnedMissingAimCamera)
            {
                return;
            }

            warnedMissingAimCamera = true;
            Debug.LogError(
                "InputRouter requires an aim camera. " +
                "Assign CameraRig/Main Camera in the Inspector.",
                this);
        }
        
        // 解析瞄准世界位置
        private Vector2 ResolveAimWorldPosition()                                        
        {
            if (aimCamera == null)
            {
                WarnMissingAimCameraOnce();                              
                return lastValidAimWorldPosition;
            }

            Ray ray = aimCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);      //从鼠标位置创建一条射线
            if (!aimPlane.Raycast(ray, out float distance))                
            {
                return lastValidAimWorldPosition;                                       //如果射线没有与平面相交，则返回上一次有效的瞄准位置
            }

            Vector3 worldPoint = ray.GetPoint(distance);
            lastValidAimWorldPosition = new Vector2(worldPoint.x, worldPoint.y);        //更新二维瞄准世界位置
            hasValidAimPosition = true;
            return lastValidAimWorldPosition;                                           //返回计算出的瞄准世界位置
        }

        // 清除战斗意图,但保留暂停和背包状态
        private void ClearCombatIntentImmediately()
        {
            InputIntent previousIntent = currentInputIntent;

            currentInputIntent = new InputIntent(
                Vector2.zero, 
                hasValidAimPosition ? lastValidAimWorldPosition : previousIntent.AimWorldPosition, 
                false, 
                false, 
                false, 
                false, 
                previousIntent.PausePressed, 
                previousIntent.InventoryPressed
            );
        }
        // 在重置时，将瞄准摄像机设置为主摄像机
        private void Reset()
        {
            aimCamera = Camera.main; 
        }
    }
}
