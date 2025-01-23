using AO;

public partial class Store
{
    public static List<ShopCategory.ProductDescription> WeaponProducts = new() {

        new () { Id = "Balloon_Sword", Rarity = ItemRarity.Common, Price = 15, Icon = "AbilityIcons/BalloonSword.png", Currency = MoneyCurrency, Name = "Balloon Sword", Description = "A sword fashioned from a couple of balloons... You won't do much damage with this, but at least it's something...", },
        new () { Id = "Revolver", Rarity = ItemRarity.Common, Price = 65, Icon = "AbilityIcons/Revolver.png", Currency = MoneyCurrency, Name = "Revolver", Description = "A revolver with 6 shots. Aim them well!", },
        new () { Id = "Beam_Saber", Rarity = ItemRarity.Uncommon, Price = 150, Icon = "AbilityIcons/BeamSaber.png", Currency = MoneyCurrency, Name = "Beam Saber", Description = "A powerful beam saber. Does good damage if you can get in melee range! Deals bonus damage to flag carriers!", },
        new () { Id = "Sniper_Rifle", Rarity = ItemRarity.Uncommon, Price = 175, Icon = "AbilityIcons/Sniper.png", Currency = MoneyCurrency, Name = "Sniper Rifle", Description = "A long-range sniper rifle. It only has a few shots, but if you can land a hit, they do massive damage!", },
        new () { Id = "Rocket_Launcher", Rarity = ItemRarity.Rare, Price = 250, Icon = "AbilityIcons/RocketLauncher.png", Currency = MoneyCurrency, Name = "Rocket Launcher", Description = "A rocket launcher that can shoot 2 rockets, each exploding on hit and dealing massive damage in a large area!", },
        new () { Id = "Assault_Rifle", Rarity = ItemRarity.Rare, Price = 325, Icon = "AbilityIcons/AssaultRifle.png", Currency = MoneyCurrency, Name = "Assault Rifle", Description = "A powerful rifle with lots of ammo. It can shoot very fast and deals a moderate amount of damage!", },
        new () { Id = "Katana", Rarity = ItemRarity.Rare, Price = 325, Icon = "AbilityIcons/Katana.png", Currency = MoneyCurrency, Name = "Katana", Description = "A katana with extra range that attacks twice per swing!", },
        new () { Id = "Chicken_Sword", Rarity = ItemRarity.Epic, Price = 50, Icon = "AbilityIcons/ChickenSword.png", Currency = PremiumCurrency, Name = "Chicken Sword", Description = "A weird chicken sword! Turns enemies into chickens when you smack'em, then deals damage when they poof back into schleems!", },
        new () { Id = "Raygun", Rarity = ItemRarity.Epic, Price = 100, Icon = "AbilityIcons/Raygun.png", Currency = PremiumCurrency, Name = "Raygun", Description = "A powerful raygun. Getting hit causes lightning to strike everyone around!", },
        new () { Id = "Bee_Cannon", Rarity = ItemRarity.Epic, Price = 150, Icon = "AbilityIcons/BeeCanon.png", Currency = PremiumCurrency, Name = "Bee Cannon", Description = "A mighty cannon that shoots beehives! Clouds of bees will track down players!", },
        new () { Id = "Baseball_Bat", Rarity = ItemRarity.Epic, Price = 50, Icon = "AbilityIcons/BaseballBat.png", Currency = PremiumCurrency, Name = "Baseball Bat", Description = "A homerun-hitting baseball bat! Knocks players back to their spawn when you him 'em!", },
        new () { Id = "Shotgun", Rarity = ItemRarity.Epic, Price = 500, Icon = "AbilityIcons/Shotgun.png", Currency = MoneyCurrency, Name = "Shotgun", Description = "A shotgun that shoots spreads of bullets!", },
    };

    public static List<ShopCategory.ProductDescription> ConsumeProducts = new() {
        new () { Id = "Health_Potion", Rarity = ItemRarity.Common, Price = 10, Icon = "ItemIcons/HealthPotion.png", Currency = MoneyCurrency, Name = "Health Potion", Description = "A health potion that restores health over time when used.", },
        new () { Id = "Speed_Potion", Rarity = ItemRarity.Common, Price = 50, Icon = "ItemIcons/SpeedPotion.png", Currency = MoneyCurrency, Name = "Speed Potion", Description = "A potion that increases your speed for 10 seconds when used.", },
        new () { Id = "Stealth_Potion", Rarity = ItemRarity.Uncommon, Price = 25, Icon = "ItemIcons/StealthPotion.png", Currency = PremiumCurrency, Name = "Stealth Potion", Description = "A potion that turns you invisible for 8 seconds when used.", },
        new () { Id = "Medpack", Rarity = ItemRarity.Uncommon, Price = 100, Icon = "ItemIcons/Medpack.png", Currency = MoneyCurrency, Name = "Medpack", Description = "A health potion that restores health over time when used.", },
        new () { Id = "Bear_Trap", Rarity = ItemRarity.Uncommon, Price = 150, Icon = "AbilityIcons/BearTrap.png", Currency = MoneyCurrency, Name = "Bear Trap", Description = "Use to place a bear trap on the map that's invisible to the other team! If they step on it, they take damage and get stuck for 4 seconds!", },
        //new () { Id = "Air_Strike", Rarity = ItemRarity.Rare, Price = 300, Icon = "AbilityIcons/Remote.png", Currency = MoneyCurrency, Name = "Air Strike", Description = "A remote that you can use to call in an air strike on the target location!", },
        new () { Id = "Mushroom", Rarity = ItemRarity.Epic, Price = 1000, Icon = "ItemIcons/Mushroom.png", Currency = MoneyCurrency, Name = "Mushroom", Description = "A suspiciously familiar mushroom... Using the Mushroom will make your character grow gigantic!", },
        new () { Id = "EXP_Potion", Rarity = ItemRarity.Epic, Price = 35, Icon = "ItemIcons/InvincibilityPotion.png", Currency = PremiumCurrency, Name = "EXP Potion", Description = "A potion that gives you double XP at the end of a round!", },
        //new () { Id = "MrCloud", Rarity = ItemRarity.Epic, Price = 75, Icon = "AbilityIcons/mrcloud.png", Currency = PremiumCurrency, Name = "Mr Cloud", Description = "Summon Mr Cloud to roam around and shock your enemies!", },
        new () { Id = "RCX", Rarity = ItemRarity.Epic, Price = 15, Icon = "AbilityIcons/rc_car.png", Currency = PremiumCurrency, Name = "RCX", Description = "A remote control car that explodes on reactivation!", },
        new () { Id = "Foam_Finger", Rarity = ItemRarity.Epic, Price = 50, Icon = "AbilityIcons/FoamFinger.png", Currency = PremiumCurrency, Name = "Foam Finger", Description = "A foam finger to buff your team with! Grants health regen and a speed buff to nearby teammates!", },
        //new () { Id = "Meteor_Storm", Rarity = ItemRarity.Mythic, Price = 125, Icon = "AbilityIcons/Meteor.png", Currency = PremiumCurrency, Name = "Meteor Storm", Description = "A meteor storm that rains down lethal comets for a full 30 seconds!", },
        new () { Id = "Nuke", Rarity = ItemRarity.Mythic, Price = 250, Icon = "AbilityIcons/Nuke.png", Currency = PremiumCurrency, Name = "Nuke", Description = "A nuclear missle! Call this in to destroy all your enemies! (Nukes have a 3 minute cooldown for everyone)", },
    };
  
    public static List<ShopCategory.ProductDescription> ChestProducts = new()
    {
        new () { Id = "Small_Chest",   Price = 0, SparksProductId = "671d30c5f35333d5c92af45c", Icon = "ItemIcons/Chest1.png", Name = "Small Chest o'Gold", Description = "A small Chest of Gold! This grants you 300 Gold when purchased! It also gives everyone else in your game 50 Gold!", },
        new () { Id = "Medium_Chest",   Price = 0, SparksProductId = "671d3126b6cbf4879658f44a", Icon = "ItemIcons/Chest2New.png", Name = "Medium Chest o'Gold", Description = "A medium sized Chest of Gold! Grants 800 Gold when purchased! It also gives everyone else in your game 150 Gold!", },
        new () { Id = "Large_Chest",  Price = 0, SparksProductId = "671d3153fae4c56ad34e7f0c", Icon = "ItemIcons/Chest3.png", Name = "Large Chest o'Gold", Description = "A large sized Chest of Gold! Grants 1500 Gold when purchased! It also gives everyone else in your game 300 Gold!", },
        new () { Id = "Epic_Chest",  Price = 0, SparksProductId = "671d3188bff84482f82c8a41", Icon = "ItemIcons/Chest4.png", Name = "Epic Chest o'Gold", Description = "A HUGE Chest of Gold! Grants 2500 Gold when purchased! It also gives everyone else in your game 500 Gold!", },
    }; 
}
