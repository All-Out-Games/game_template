using AO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public partial class InventoryBox : Component
{
    [Serialized] public Spine_Animator Spine;

    public Inventory Inventory;

    public override void Awake()
    {
        Spine.Awaken();
        var sm = StateMachine.Make();
        var baseLayer = sm.CreateLayer("main");
        var appearState = baseLayer.CreateState("appear", 0, false);
        var idleState = baseLayer.CreateState("idle", 0, true);
        var appearTrigger = sm.CreateVariable("appear", StateMachineVariableKind.TRIGGER);
        baseLayer.SetInitialState(idleState);
        baseLayer.CreateTransition(idleState, appearState, false).CreateTriggerCondition(appearTrigger);
        baseLayer.CreateTransition(appearState, idleState, true);
        Spine.SpineInstance.SetStateMachine(sm, Entity);
        Spine.SpineInstance.StateMachine.SetTrigger("appear");

        SFX.Play(Assets.GetAsset<AudioAsset>("sfx/more/lose_items.wav"), new() { Positional = true, Position = Entity.Position });
        Spine.SpineInstance.ColorMultiplier = new Vector4(1, 1, 1, 1);
    }


    public override void OnDestroy()
    {
        if (Inventory != null)
        {
            Inventory.DestroyInventory(Inventory);
        }
    }

    [ClientRpc]
    public void Setup(Player player, int id)
    {
        Inventory = Inventory.CreateInventory(player.UserId + "_box" + id, 32);

        var interactable = GetComponent<Interactable>();
        interactable.CanUseCallback = p =>
        {
            var player = (MyPlayer)p;
            if (player.PlayerRole == PlayerRole.Spectator) return false;
            return true;
        };
        interactable.OnInteract += p =>
        {
            var player = (MyPlayer)p;
            var anyFailed = false;
            foreach (var item in Inventory.Items)
            {
                if (item == null) continue;

                var pickedUp = false;
               
                if (!pickedUp)
                {
                    anyFailed = true;
                }
            }

            if (anyFailed == false)
            {
                // the player picked them all up, so despawn!
                if (Network.IsServer)
                {
                    Network.Despawn(Entity);
                    Entity.Destroy();
                }
            }
           
        };
    }
}