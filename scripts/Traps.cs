using AO;

public partial class Trap : Component
{
    [Serialized] public Sprite_Renderer Sprite;
    [Serialized] public Spine_Animator Rig;
    public Collider Collider;

    public bool Triggered = false;

    public override void Awake()
    {
        Collider = GetComponent<Collider>();
        if (Collider != null && Network.IsServer)
        {
            Collider.OnCollisionEnter += other =>
            {
                var player = other.GetComponent<MyPlayer>();
                if (player.Alive() && !Triggered &&
                !player.IsDead)
                {
                    TriggerTrap(player);
                    Triggered = true;
                }
            };
        }
    }

    public virtual void TriggerTrap(MyPlayer player)
    {
        // For overrides. Maybe something here later for default traps idk.
    }

    public void DestroyTrap()
    {
        if (Network.IsServer)
        {
            Network.Despawn(Entity);
            Entity.Destroy();
        }
    }
}

public partial class BearTrap : Trap
{
    private SyncVar<int> _playerRole = new((int)PlayerRole.Spectator);
    public PlayerRole PlayerRole
    {
        get => (PlayerRole)_playerRole.Value;
        set => _playerRole.Set((int)value);
    }
    float destroyTimer;

    public MyPlayer Owner;

    public override void Awake()
    {
        // setup spine stuff etc
        Rig.Awaken();
        var sm = StateMachine.Make();
        var layer = sm.CreateLayer("main");
        var appear = layer.CreateState("RAMB132/beartrap_spawn", 0, false);
        var idle = layer.CreateState("RAMB132/beartrap_idle", 0, true);
        var triggered = layer.CreateState("RAMB132/beartrap_cm_activate", 0, false);
        var trigger = sm.CreateVariable("myTrigger", StateMachineVariableKind.TRIGGER);
        layer.CreateTransition(appear, idle, true);
        layer.CreateGlobalTransition(triggered).CreateTriggerCondition(trigger);
        layer.SetInitialState(appear);
        Rig.SpineInstance.SetStateMachine(sm, Rig.Entity);

        SFXE.Play(Assets.GetAsset<AudioAsset>("sfx/beartrap_spawn.wav"), new() { Positional = true, Position = Entity.Position });
    }

    bool initLazy = false;
    public override void Update()
    {
        base.Update();

        if (Util.OneTime(initLazy == false, ref initLazy))
        {
            Collider = GetComponent<Collider>();
            if (Collider != null && Network.IsServer)
            {
                Collider.OnCollisionEnter += other =>
                {
                    var player = other.GetComponent<MyPlayer>();
                    if (player.Alive() && !Triggered &&
                    !player.IsDead &&
                    player.PlayerRole != PlayerRole)
                    {
                        TriggerTrap(player);
                        Triggered = true;
                    }
                };
            }

            _playerRole.OnSync += (old, value) =>
            {
                if (Network.LocalPlayer != null)
                {
                    if ((int)((MyPlayer)Network.LocalPlayer).PlayerRole != _playerRole)
                    {
                        Rig.LocalEnabled = false;
                    }
                }
            };
            
        }

        if (Network.IsServer)
        {
            if (destroyTimer > 0)
            {
                destroyTimer -= Time.DeltaTime;
                if (destroyTimer <= 0)
                {
                    Network.Despawn(Entity);
                    Entity.Destroy();
                }
            }
        }
    }

    public override void TriggerTrap(MyPlayer player)
    {
        // add effect
       // CallClient_AddBearTrap(player);
        destroyTimer = 8f;
    }

    /*
    [ClientRpc]
    public void AddBearTrap(MyPlayer player)
    {
        if (player.Alive())
        {
            Rig.LocalEnabled = true;
            Rig.SpineInstance.StateMachine.SetTrigger("myTrigger");
            player.AddEffect<EffectBearTrapped>();
            Entity.Position = player.Position;

            if (Network.IsServer)
            {
                player.CallClient_CAddHitEffect(player, 25, Owner);
            }

            SFXE.Play(Assets.GetAsset<AudioAsset>("sfx/beartrap_cm_activate.wav"), new() { Positional = true, Position = Entity.Position });
        }
    }
    */
}
