using AO;

public partial class PlayerCorpse : Component, INetworkedComponent
{
    [Serialized] public Spine_Animator PlayerAnimator;
    [Serialized] public string PlayerName;
    [Serialized] public int ColorIndex;

    [Serialized] public string DeathAnim;
    public string[] Skins;

    public SyncVar<Entity> ForPlayer = new();

    float timer = 0;

    public void NetworkSerialize(AO.StreamWriter writer)
    {
        writer.WriteStringArray(Skins);
    }

    public void NetworkDeserialize(AO.StreamReader reader)
    {
        Skins = reader.ReadStringArray();
        SetSkins(Skins);
    }

    public override void Awake()
    {
        {
            var sm = StateMachine.Make();
            var baseLayer = sm.CreateLayer("base");
            var idleState = baseLayer.CreateState("Idle", 0, true);
            var death = baseLayer.CreateState("Death_No_HP", 0, false);
            var dieTrigger = sm.CreateVariable("die", StateMachineVariableKind.TRIGGER);

            baseLayer.SetInitialState(idleState);
            baseLayer.CreateGlobalTransition(death).CreateTriggerCondition(dieTrigger);

            PlayerAnimator.Awaken();
            PlayerAnimator.SpineInstance.SetStateMachine(sm, Entity);

            PlayerAnimator.SetCrewchsia(ColorIndex);
            PlayerAnimator.SpineInstance.SetSkeleton(Assets.GetAsset<SpineSkeletonAsset>("animations/ctf_player/ctf_player.merged_spine_rig#output"));
        }

        ForPlayer.OnSync += (old, value) =>
        {
        };

        if (!string.IsNullOrEmpty(DeathAnim))
        {
            PlayerAnimator.SpineInstance.StateMachine.SetTrigger(DeathAnim);
            PlayerAnimator.SpineInstance.Update(0);
            PlayerAnimator.SpineInstance.Update(10);
        }

        if (Skins != null)
        {
            SetSkins(Skins);
        }
    }

    [ClientRpc]
    public void SetSkins(string[] skins)
    {
        Skins = skins;
        if (Skins != null)
        {
            foreach (var skin in Skins)
            {
                PlayerAnimator.SpineInstance.EnableSkin(skin);
            }
        }
        PlayerAnimator.SpineInstance.RefreshSkins();
    }

    public override void Update()
    {
        if (!string.IsNullOrEmpty(DeathAnim)) timer += Time.DeltaTime;
    }
}