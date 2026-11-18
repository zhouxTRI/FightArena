
namespace FightArena.Core.Combat
{   
    public interface IDamageable                        //这个接口的用途是定义一个可受伤害的对象的行为规范，实现该接口的类才能被视为可受伤害的对象
    {
        int InstanceId { get; }                         //一个只读属性 InstanceId，用于获取对象的实例ID
        bool TryApplyDamage(in DamageInfo damage);      //尝试对对象应用伤害，并返回是否成功应用
    }
}
