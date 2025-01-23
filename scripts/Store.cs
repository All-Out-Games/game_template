using AO;
using GameAnalyticsSDK.Net;

public partial class Store : System<Store>
{
    public const string MoneyCurrency = "Silver";
    public const string PremiumCurrency = "Gold";

    public Shop ItemShop;

    public override void Start()
    {
        Economy.RegisterCurrency(MoneyCurrency, "ItemIcons/Silver.png");
        Economy.RegisterCurrency(PremiumCurrency, "ItemIcons/Gold.png");

        if (Network.IsServer) Purchasing.SetPurchaseHandler(SparksPurchaseHandler);


        ItemShop = Economy.CreateShop("Item Shop");
        ItemShop.SetPurchaseModifier(OnBeforeItemPurchase);
        if (Network.IsClient)
        {
            ItemShop.SetCustomDisplay(CustomItemShopDisplay);
        }
        if (Network.IsServer)
        {
            ItemShop.SetPurchaseHandler(OnItemPurchase);
        }

        var weaponCat = ItemShop.AddCategory("Weapons");
        weaponCat.Icon = "AbilityIcons/Revolver.png";
        foreach (var p in WeaponProducts)
        {
            weaponCat.AddProduct(p);
        }

        var consumesCat = ItemShop.AddCategory("Consumes");
        consumesCat.Icon = "ItemIcons/HealthPotion.png";
        foreach (var p in ConsumeProducts)
        {
            consumesCat.AddProduct(p);
        }

        var sparksCat = ItemShop.AddCategory("Chests");
        sparksCat.Icon = "ItemIcons/Chest4.png";
        foreach (var p in ChestProducts)
        {
            sparksCat.AddProduct(p);
        }



    }

    private bool SparksPurchaseHandler(Player _player, string productId)
    {
        var player = (MyPlayer)_player;

        var chestProduct = ChestProducts.FirstOrDefault(prod => prod.SparksProductId == productId);
        if (chestProduct.Id.IsNullOrEmpty() == false)
        {

            if (Network.IsServer)
            {

            }
            return true;
        }
       
        return false;
    }
    

    #region ItemShop
    public PurchaseModification OnBeforeItemPurchase(Player _player, GameProduct product)
    {
        var player = (MyPlayer)_player;

        var modification = new PurchaseModification(product);

        modification.ModifyProduct = false;

        /*
        if (((MyPlayer)_player).Level < LevelRequired(product.Id))
        {
            modification.ModifyProduct = true;
            modification.Color = PurchaseButtonColor.Red;
            modification.OnBuyButtonClicked = () => PurchaseFail(_player);
        }
        else
        {
            //modification.OnBuyButtonClicked = () => try purchase here somehow?
        }
        */

        return modification;
    }


    void PurchaseFail(Player player)
    {
        MyPlayer p = (MyPlayer)player;
        if (p.IsLocal)
        {
            SFXE.Play(Assets.GetAsset<AudioAsset>("sfx/retro_fail_sound_05.wav"), new() { });
            p.ShakeScreen(0.35f, 0.1f);
        }
    }


    public void CustomItemShopDisplay(GameProduct product, Rect rect)
    {
        rect.CutTop(10);
        var descriptionRect = rect.CutTop(200).Inset(0, 15, 0, 15);
        UI.Text(descriptionRect, product.Description, new UI.TextSettings()
        {
            Font = UI.Fonts.Barlow,
            Size = 32,
            VerticalAlignment = UI.VerticalAlignment.Top,
            HorizontalAlignment = UI.HorizontalAlignment.Center,
            Color = Vector4.White,
            WordWrap = true,
            Outline = true,
            OutlineThickness = 3.0f,
            DoAutofit = true,
            AutofitMinSize = 16,
            AutofitMaxSize = 32,
        });
        descriptionRect = rect.CutTop(25);

    }

    public bool OnItemPurchase(Player _player, GameProduct product)
    {
        var player = (MyPlayer)_player;
        
        var weaponProduct = WeaponProducts.FirstOrDefault(prod => prod.Id == product.Id);
        if (weaponProduct.Id.IsNullOrEmpty() == false)
        {
            if (Network.IsServer)
            {
                var itemDefinition = ItemManager.Instance.TryFindItem(product.Id);
                if (itemDefinition == null) return false;
                var room = Inventory.CalculateRoomInInventoryForItem(itemDefinition, player.DefaultInventory);
                if (room < 1)
                {
                    return false;
                }

                if (GameManager.Instance.State != GameState.Round) return false;

                player.AddItemToInventory(product.Id, 1);
                player.CallClient_PlayShopSFX(player);
                return true;
            }

            //GameAnalytics.AddResourceEvent(EGAResourceFlowType.Sink, product.Currency, product.Price, "weapon", $"{drillConfig.Id}-{currentLevel + 1}");
        }

        var consumeProduct = ConsumeProducts.FirstOrDefault(prod => prod.Id == product.Id);
        if (consumeProduct.Id.IsNullOrEmpty() == false)
        {
            if (Network.IsServer)
            {
                var itemDefinition = ItemManager.Instance.TryFindItem(product.Id);
                if (itemDefinition == null) return false;
                var room = Inventory.CalculateRoomInInventoryForItem(itemDefinition, player.DefaultInventory);
                if (room < 1)
                {
                    return false;
                }

                if (GameManager.Instance.State != GameState.Round) return false;

                player.AddItemToInventory(product.Id, 1);
                player.CallClient_PlayShopSFX(player);
                return true;
            }

            //GameAnalytics.AddResourceEvent(EGAResourceFlowType.Sink, product.Currency, product.Price, "weapon", $"{drillConfig.Id}-{currentLevel + 1}");
        }

        return false;
    }
    #endregion

    public bool ItemShopOpen;

    public override void Update()
    {
        if (ItemShopOpen)
        {
            Rect rect = UI.ScreenRect.CenterRect().Grow(300, 500, 300, 500);
            using var _ = UI.PUSH_LAYER(100);
            ItemShopOpen = ItemShop.Draw(rect);
        }
    }
}