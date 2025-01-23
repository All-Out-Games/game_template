using AO;

public partial class SmokePoof : Component
{
    float timer = 0;
    public Spine_Animator Animator;

    public override void Awake()
    {
        base.Awake();

        Animator = GetComponent<Spine_Animator>();

        Animator.Awaken();
        var sm = StateMachine.Make();
        var layer = sm.CreateLayer("main");
        var idle = layer.CreateState("poof", 0, false);      
        layer.SetInitialState(idle);       
        Animator.SpineInstance.SetStateMachine(sm, Animator.Entity);
    }

    public override void Update()
    {
        base.Update();
        timer += Time.DeltaTime;
        if (timer > 2f)
        {
            Entity.Destroy();
        }
    }
}

public partial class MissleExplosion : Component
{
    float timer = 0;
    public Spine_Animator Animator;

    public override void Awake()
    {
        base.Awake();

        Animator = GetComponent<Spine_Animator>();

        Animator.Awaken();
        var sm = StateMachine.Make();
        var layer = sm.CreateLayer("main");
        var idle = layer.CreateState("TOY98/hit_effect", 0, false);
        layer.SetInitialState(idle);
        Animator.SpineInstance.SetStateMachine(sm, Animator.Entity);        
    }

    public override void Update()
    {
        base.Update();
        timer += Time.DeltaTime;
        if (timer > 0.23f)
        {
            Entity.Destroy();
        }
    }
}