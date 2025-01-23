using AO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public partial class ItemDrop : Component
{
    [Serialized] public Sprite_Renderer Sprite;
    public string ItemID;
    public int Amount = 1;
    public float Durability = 1f;

    [Serialized] public Circle_Collider Collider;
    public float timer = 0;

    public bool DestroyMe = false;

    public override void Awake()
    {
        //Collider = GetComponent<Collider>();
        Collider.Awaken();
        Sprite.Awaken();
        Sprite.Entity.LocalPosition = new Vector2(0, new Random().Next(0, 8) * 0.05f);
    }


    bool floatUp = true;
    float destroyTimer = 0;
    public override void Update()
    {
        base.Update();
        timer += Time.DeltaTime;

        if (DestroyMe) destroyTimer += Time.DeltaTime;
        if (destroyTimer >= 3)
        {
            if (Network.IsServer)
            {
                Network.Despawn(Entity);
                Entity.Destroy();
            }
        }

        if (Network.IsServer &&
            timer >= 120)
        {
            Network.Despawn(Entity);
            Entity.Destroy();
        }

        if (floatUp)
        {
            Sprite.Entity.LocalPosition = new Vector2(0, Sprite.Entity.LocalPosition.Y + 0.005f);
            if (Sprite.Entity.LocalPosition.Y >= 0.385f)
            {
                floatUp = false;
            }
        }
        else
        {
            Sprite.Entity.LocalPosition = new Vector2(0, Sprite.Entity.LocalPosition.Y - 0.005f);
            if (Sprite.Entity.LocalPosition.Y <= 0f)
            {
                floatUp = true;
            }
        }
    }

    [ClientRpc]
    public void Pickup()
    {
        if (Network.IsClient)
        {
            SFXE.Play(Assets.GetAsset<AudioAsset>("sfx/item_pickup.wav"), new() { Positional = true, Position = Entity.Position });
            PickupNotification();
            Sprite.LocalEnabled = false;
        }        
    }

    public bool CreateItemDrop(string id, int amount)
    {
        // server only
        if (ItemManager.Instance.TryFindItem(id) == null) return false;

        ItemID = id;
        Amount = amount;

        Collider.OnCollisionEnter += other =>
        {
            if (Network.IsServer)
            {
                if (DestroyMe) return;
                var player = other.GetComponent<MyPlayer>();
                if (player.Alive() &&
                GameManager.Instance.State == GameState.Round &&
                timer >= 0.5f)
                {
      
                    // Logic for Currencies, etc here
                    {
                        player.AddItemToInventory(ItemID, Amount, Durability);
                    }

                    CallClient_Pickup();
                    DestroyMe = true;
                }
            }
        };

        CallClient_Setup(ItemID, Amount);
        return true;
    }

    [ClientRpc]
    public void Setup(string id, int amount)
    {
        // assign the icon
        Item_Definition def = ItemManager.Instance.TryFindItem(id);
        if (def == null) return;

        if (Network.IsClient)
        {
            ItemID = id;
            Amount = amount;
        }
        Sprite.Sprite = Assets.GetAsset<Texture>(def.Icon);
        Sprite.Entity.Scale = GetSpriteSize(id);
    }

    Vector2 GetSpriteSize(string id)
    {
        switch (id)
        {
            case "Silver":
            case "Gold":
            case "Revolver":
            case "Health_Potion":
            case "Speed_Potion":
            case "Mushroom":
                return new Vector2(0.5f, 0.5f);

            default:
                return new Vector2(1, 1);
        }
    }

    public void PickupNotification()
    {
        if (ItemManager.Instance.TryFindItem(ItemID) == null) return;

        GameManager.Instance.SpawnDamageNumber(UI.Fonts.Asap, Entity.Position, Vector4.White, ItemManager.Instance.TryFindItem(ItemID).Name + " x" + Amount.ToString());
    }
}