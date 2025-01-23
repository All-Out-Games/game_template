using AO;

public class AbilityExample : MyAbility
{
    public override Type Effect => typeof(EffectExample);
    public override bool MonitorEffectDuration => true;
    public override TargettingMode TargettingMode => TargettingMode.Line;
    public override Type TargettingEffect => typeof(EffectAimExample);
    public override float Cooldown => 1f;
    public override float MaxDistance => 2f;
    public override Texture Icon => null; //Assets.GetAsset<Texture>("AbilityIcons/.png");

    public override bool CanUse()
    {
        if (!base.CanUse()) return false;
        if (Player.PlayerRole == PlayerRole.Spectator) return false;
        return true;
    }
}

public class EffectAimExample : AimEffect
{
    public override bool IsActiveEffect => false;
    public override bool GetInterruptedByNewActiveEffects => true;
    public override bool BlockAbilityActivation => false;

    public override void OnEffectStart(bool isDropIn)
    {
        Player.SetMouseIKEnabled(true);
        Player.SpineAnimator.SpineInstance.StateMachine.SetBool("mIK", true);
    }

    public override void OnEffectUpdate()
    {

    }

    public override void OnEffectEnd(bool interrupt)
    {
        if (interrupt)
        {
            Player.SetMouseIKEnabled(false);
            Player.SpineAnimator.SpineInstance.StateMachine.SetBool("mIK", false);
        }
    }
}

public class EffectExample : MyEffect
{
    public override bool IsActiveEffect => true;

    public override bool BlockAbilityActivation => true;

    public override float DefaultDuration => 1f;


    public override void OnEffectStart(bool isDropIn)
    {

    }

    public override void OnEffectUpdate()
    {

    }

    public override void OnEffectEnd(bool interrupt)
    {

    }
}
