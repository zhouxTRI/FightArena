using UnityEngine;

namespace FightArena.Gameplay.Player
{
    public sealed class PlayerAnimatorBridge : MonoBehaviour                    //这里如果不用sealed修饰符，其他类可以继承这个类，可能会导致一些问题，比如其他类重写了这个类的某些方法，导致动画播放不正常，所以这里使用sealed修饰符，表示这个类不能被继承
    {
        private static readonly int SpeedHash =                                 //这个参数是用来控制动画的播放速度的，SpeedHash是一个静态只读字段，它的值是通过Animator.StringToHash方法将字符串"Speed"转换为一个整数哈希值，这样可以提高性能，因为在运行时使用字符串查找动画参数会比较慢，而使用哈希值查找会更快
            Animator.StringToHash("Speed");                           
        private static readonly int IsDodgingHash =                             //作为闪避动画的播放状态参数
            Animator.StringToHash("IsDodging");
        private static readonly int IsDeadHash =
            Animator.StringToHash("IsDead");

        [SerializeField] private Animator playerAnimator;

        private void Awake()
        {
            if (playerAnimator == null)
            {
                Debug.LogError(
                    "PlayerAnimatorBridge requires the Neo Animator.",
                    this);
                enabled = false;
            }
        }

        public void Render(PlayerState state, float speed01)                    //speed01表示玩家当前的移动速度相对于最大移动速度的比例
        {
            if (playerAnimator == null)
            {
                return;
            }

            float effectiveSpeed = state == PlayerState.Normal                  //控制动画播放速度参数，如果玩家的状态是Normal，那么就使用Mathf.Clamp01(speed01)来限制speed01的值在0到1之间
                ? Mathf.Clamp01(speed01)                            
                : 0f;

            playerAnimator.SetFloat(                                            //这里的目的是为了让动画的播放速度随着玩家的移动速度而变化，如果玩家的状态不是Normal，那么就将动画的播放速度设置为0，这样就不会播放移动动画
                SpeedHash,                                                      //动画参数的哈希值
                effectiveSpeed,                                                 //动画参数的值
                0.05f,                                                          //动画参数的变化速度
                Time.deltaTime);                                                //时间增量

            playerAnimator.SetBool(                                             //这里是设置闪避动画的播放状态，如果玩家的状态是Dodging，那么就设置IsDodgingHash为true，否则为false
                IsDodgingHash,
                state == PlayerState.Dodging);

            playerAnimator.SetBool(
                IsDeadHash,
                state == PlayerState.Dead);
        }

        //重置动画状态机
        public void ResetForNewSession()
        {
            if (playerAnimator == null)
            {
                return;
            }

            playerAnimator.SetFloat(SpeedHash, 0f);
            playerAnimator.SetBool(IsDodgingHash, false);
            playerAnimator.SetBool(IsDeadHash, false);
            playerAnimator.Play("Idle", 0, 0f);
        }
    }
}
