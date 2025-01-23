using AO;
using System.Diagnostics;

public class ItemManager : Component
{
    [AOIgnore] public static ItemManager Instance;

    public List<Item_Definition> AllItemDefinitions = new List<Item_Definition>();

    public static string GetArticle(Item_Definition defn)
    {
        switch (defn.Id)
        {
            case "Gold":
            case "Silver":          
                {
                    return "some";
                }
            case "Axe":
                {
                    return "an";
                }
        }
        return "a";
    }

    public static Item_Instance CreateItem(string itemId)
    {
        var defn = ItemManager.Instance.TryFindItem(itemId);
        if (defn == null)
        {
            Util.Assert(false, "Unknown item " + itemId);
            return null;
        }
        return CreateItem(defn);
    }

    public static Item_Instance CreateItem(Item_Definition defn, long count = 1)
    {
        var instance = Inventory.CreateItem(defn, count);
        if (defn.Id == "Breakable_Item") instance.Durability = 1f;

        return instance;
    }

    public override void Awake()
    {
        Instance = this;

        var currencyItem = Item_Definition.Create(new ItemDescription()
        {
            Id = "Currency_Item",
            Name = "Currency",
            Icon = "ItemIcons/Silver.png",
            StackSize = 9999,
        });
        AllItemDefinitions.Add(currencyItem);

        var breakableItem = Item_Definition.Create(new ItemDescription()
        {
            Id = "Breakable_Item",
            Name = "Breakable Item",
            Icon = "AbilityIcons/BalloonSword.png",
            StackSize = 1,
        });
        AllItemDefinitions.Add(breakableItem);

        var consumableItem = Item_Definition.Create(new ItemDescription()
        {
            Id = "Consumable_Item",
            Name = "Consumable",
            Icon = "ItemIcons/Mushroom.png",
            StackSize = 1,
        });
        AllItemDefinitions.Add(consumableItem);
    }

    void TryUseItem(Player player, Item_Instance item)
    {
        if (player.IsLocal)
        {
            MyPlayer myPlayer = ((MyPlayer)player);

            switch (item.Definition.Id)
            {
                
                case null:
                case "":
                    Log.Warn("Tried to use an empty item");
                    break;
            }
        }
    }

    public void ResetItems()
    {
        
    }

    public Vector2 GetPickupScale(string itemId)
    {
        Vector2 thescale = new Vector2(1f, 1f);
        switch (itemId)
        {
            case "Bullet":
                {
                    thescale = new Vector2(5f, 5f);
                    break;
                }
            case "Revolver":
                {
                    thescale = new Vector2(0.5f, 0.5f);
                    break;
                }
        }

        return thescale;
    }

    public Item_Definition FindItem(string itemId)
    {
        Item_Definition defn = AllItemDefinitions.First(item => item.Id == itemId);
        return defn;
    }

    public Item_Definition TryFindItem(string itemId)
    {
        Item_Definition defn = AllItemDefinitions.FirstOrDefault(item => item.Id == itemId);
        return defn;
    }
}
