using UnityEngine;

namespace FightArena.Gameplay.Player
{
    public sealed class PlayerAim : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Transform aimPivot;                        //用于旋转瞄准方向的枢轴点引用
        [SerializeField] private Transform visualRoot;                      //用于翻转视觉根节点的引用，以实现角色朝向的变化

        [Header("Presentation")]       
        [SerializeField] private bool allowVisualFlip = true;               //是否允许在瞄准时翻转视觉根节点的X轴缩放，以实现角色朝向的变化

        private Vector2 aimDirection = Vector2.right;                       //玩家的瞄准方向，默认为向右
        private int currentFacingSign = 1;                                  //当前角色的朝向符号，1表示向右，-1表示向左
        private bool aimingEnabled = true;

        public Vector2 AimDirection => aimDirection;
        public int CurrentFacingSign => currentFacingSign;

        private void Awake()
        {
            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }

            if (aimPivot == null || visualRoot == null || aimCamera == null)
            {
                Debug.LogError(
                    "PlayerAim requires AimCamera, AimPivot and VisualRoot.",
                    this);
                enabled = false;
            }
        }

        public void SetEnabled(bool value)
        {
            aimingEnabled = value;
        }

//在每一帧Update调用此方法，更新瞄准方向和视觉根节点的朝向
        public void Tick(Vector2 aimWorldPosition)
        {
            if (!aimingEnabled || aimPivot == null)
            {
                return;
            }

            Vector2 origin = transform.position;                                    //获取玩家的世界位置作为瞄准的起点
            Vector2 delta = aimWorldPosition - origin;                              //计算瞄准位置与玩家位置的偏移量

            if (delta.sqrMagnitude > 0.000001f)
            {
                aimDirection = delta.normalized;
            }

            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) *             //计算瞄准方向的角度，并将其转换为度数
                          Mathf.Rad2Deg;

            aimPivot.rotation = Quaternion.Euler(0f, 0f, angle);

            if (!allowVisualFlip || visualRoot == null ||
                Mathf.Abs(aimDirection.x) <= 0.01f)
            {
                return;
            }

            int desiredSign = aimDirection.x >= 0f ? 1 : -1;                        //根据瞄准方向的X分量确定期望的朝向符号，1表示向右，-1表示向左

            if (desiredSign == currentFacingSign)
            {
                return;
            }

            currentFacingSign = desiredSign;
            Vector3 scale = visualRoot.localScale;                                  //获取视觉根节点的当前缩放
            scale.x = Mathf.Abs(scale.x) * currentFacingSign;                       //根据当前朝向符号调整视觉根节点的X轴缩放，实现角色朝向的翻转
            visualRoot.localScale = scale;
        }

        private void Reset()
        {
            aimCamera = Camera.main;
        }
    }
}
