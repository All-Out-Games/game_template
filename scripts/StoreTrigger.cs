using System.Drawing;
using System.Threading;
using AO;

public class StoreTrigger : Component
{
    [Serialized] public Interactable Interactable;
    [Serialized] public PlayerRole Team;
    [Serialized] public Spine_Animator SpineAnimator;
    public override void Awake()
    {
        Interactable.Awaken();

        Interactable.CanUseCallback = (Player p) =>
        {
            //if (GameManager.Instance.State != GameState.Round) return false;
            if (GameManager.Instance.State == GameState.Round && ((MyPlayer)p).PlayerRole != Team) return false;
            return true;
        };

        Interactable.OnInteract = (Player p) =>
        {
            OpenUI((MyPlayer)p);
        };
        StartRig();
    }

    public void StartRig()
    {
        SpineAnimator.Awaken();
        var sm = StateMachine.Make();
        SpineAnimator.SpineInstance.SetStateMachine(sm, SpineAnimator.Entity);
        var mainLayer = sm.CreateLayer("main");
        var idleState = mainLayer.CreateState("Idle", 0, true);
        mainLayer.SetInitialState(idleState);

        SpineAnimator.SetCrewchsia(Team == PlayerRole.Role1 ? 0 : 15);
        //SpineAnimator.SpineInstance.RefreshSkins();
    }

    public void OpenUI(MyPlayer p)
    {
       if (p.IsLocal)
        {
            p.StoreEntity = Entity;
            Store.Instance.ItemShopOpen = true;
        }
    }

}