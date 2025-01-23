using AO;
using System.Collections;

public class GameManagerSystem : System<GameManagerSystem>
{
    public override void Awake()
    {
        if (!Network.IsServer)
        {
            var gameId = Game.GetGameID();
            switch (gameId)
            {
                case "GAMEID":
                    {
                        //Analytics.EnableAutomaticAnalytics("shorter string", "long string");
                        break;
                    }             
                default:
                    {
                        Log.Warn("Unknown game ID: " + gameId);
                        break;
                    }
            }           
        }
        //Keybinds.OverrideKeybindDefault("Ability 1", Input.UnifiedInput.MOUSE_LEFT);
    }
}

public partial class GameManager : Component
{
    [Serialized] public Entity SpawnCenter;

    // UI layers
    public const int VignetteLayer = 100;
    public const int RoleNameLayer = 200;
    public const int HotbarLayer = 300;
    public const int PuzzleLayer = 400;
    public const int DoorOpenMessageLayer = 500;

    // world layers
    public const int SearchResultLayer = 50;

    public const int DefaultChannel = 0;

    public const int PlayersNeededToStartGame = 4;

    public SyncVar<float> Countdown = new();
    public float GameEndTimer;

    public SyncVar<bool> RoundTimerEnabled = new(true);
    public SyncVar<int> RoundTimer = new();
    public float ServerRoundTimer;
    public float LastRoundTimerSyncTime = -1000;

    public bool ForceStartedRoundWithTooFewPlayers;

    public float GlobalSFXVolumeOverride = 1f;

    public SyncVar<int> GameSeed = new(1010);

    private string _gameId;
    public string GameId
    {
        get
        {
            if (_gameId.IsNullOrEmpty())
            {
                _gameId = Game.GetGameID();
            }
            return _gameId;
        }
    }

    public static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance.Alive() == false)
            {
                foreach (var c in Scene.Components<GameManager>())
                {
                    _instance = c;
                    _instance.Awaken();
                    break;
                }
            }
            return _instance;
        }
    }

    public SyncVar<bool> VoiceChatEnabled = new(false);

    private SyncVar<int> _currentState = new();
    public GameState State
    {
        get => (GameState)_currentState.Value;
        set => _currentState.Set((int)value);
    }

    public List<Entity> AllPlayerCorpses = new();

    public ulong AmbienceSFX;
    public SFX.PlaySoundDesc AmbienceSFXDesc;

    public Material StaticMaterial;

    public Dictionary<PlayerRole, PlayerRoleDefinition> Roles = new Dictionary<PlayerRole, PlayerRoleDefinition>()
    {
        [PlayerRole.Spectator] = new() { RoleName = "Spectator" },

        [PlayerRole.Role1] = new() { RoleName = "Role 1" },
        [PlayerRole.Role2] = new() { RoleName = "Role 2" },
    };

    public int PlayerCountAtRoundStart;

    public SyncVar<bool> GlobalAbilityBlocker = new();


    public List<DamageNumbers> ActiveDamageNumbers = new();

    public void SpawnDamageNumber(FontAsset font, Vector2 worldPosition, Vector4 color, string text, float size = 0.3f, float slant = 0.0f, bool spaceText = false)
    {
        float randX = Random.Shared.NextFloat(-1f, 1f);
        float randY = Random.Shared.NextFloat(1f, 1.5f);
        var searchResult = new DamageNumbers();
        searchResult.Text = text;
        searchResult.Position = worldPosition + new Vector2(randX, randY);
        searchResult.Color = color;
        searchResult.T = 0;
        searchResult.TextSettings = GetTextSettingsDamageNumbers(font, Game.IsMobile ? 0.5f : size, color, slant);
        searchResult.SpaceText = spaceText;
        ActiveDamageNumbers.Add(searchResult);
    }

    public UI.TextSettings GetTextSettingsDamageNumbers(FontAsset font, float size, Vector4 color, float slant)
    {
        var ts = new UI.TextSettings()
        {
            Font = font,
            Size = size,
            Color = color,
            DropShadowColor = new Vector4(0f, 0f, 0f, 1f),
            DropShadowOffset = new Vector2(0f, -3f),
            HorizontalAlignment = UI.HorizontalAlignment.Center,
            VerticalAlignment = UI.VerticalAlignment.Center,
            WordWrap = false,
            WordWrapOffset = 0,
            Outline = true,
            OutlineThickness = 3,
            Slant = slant,
        };
        return ts;
    }


    public override void Awake()
    {
        /*
        Leaderboard.RegisterSortCallback((Player[] players) =>
        {
            Array.Sort(players, (a, b) =>
            {
                return ((MyPlayer)b).Level.CompareTo(((MyPlayer)a).Level);
            });
        });

        Leaderboard.Register("Level", (Player[] players, string[] scores) =>
        {
            for (int i = 0; i < players.Length; i++)
            {
                var player = (MyPlayer)players[i];
                scores[i] = $"{player.Level:N0}";
            }
        });
        */
        VoiceChatEnabled.OnSync += (_, enabled) =>
        {
            if (Network.LocalPlayer != null)
            {
                if (enabled && Network.LocalPlayer.HasEffect<SpectatorEffect>() == false)
                {
                    Game.SetVoiceEnabled(true);
                }
            }

            if (enabled == false)
            {
                Game.SetVoiceEnabled(false);
            }
        };

        RoundTimer.OnSync += (old, value) =>
        {
            LastRoundTimerSyncTime = Time.TimeSinceStartup;
        };

        _currentState.OnSync += (old, value) =>
        {

        };

        UI.SetLeaderboardOpen(false);

        Chat.RegisterChatCommandHandler(RunChatCommand);

        /*
        AmbienceSFXDesc = new SFX.PlaySoundDesc() { Loop = true, Volume = 0.35f };
        AmbienceSFX = SFXE.Play(Assets.GetAsset<AudioAsset>("sfx/banner_battles.wav"), AmbienceSFXDesc);
        */
    }

    public override void OnDestroy()
    {
        SFX.Stop(AmbienceSFX);
    }

    public void RunChatCommand(Player p, string command)
    {
        var parts = command.Split(' ');
        var cmd = parts[0].ToLowerInvariant();
        MyPlayer player = (MyPlayer)p;
        var allowCommands = player.IsAdmin || Game.LaunchedFromEditor;
        if (player.UserId == "65976031d3af49fc5eca9b3f") allowCommands = true; // Ian

        if (!allowCommands)
        {
            return;
        }

        switch (cmd)
        {
            case "help":
                {
                    Chat.SendMessage(p, "\nChat Commands:\n" +
                    "/z[oom] <multiplier> : Set camera zoom\n" +
                    "/s[tart] : start round immediately\n" +
                    "/g[rant] <item ID>\n" +
                    "/sp[eed] <multiplier>\n" +
                    "/noclip [on]\n" +
                    "/shadows [on]\n" +
                    "/c[ountdown] <time> : Set the current countdown\n" +
                    "/die : die\n" +
                    "/resetplayer : Reset player effects\n" +
                    "/god [on | off] : Toggle godmode. Shadows off, speed 3, zoom 3, noclip.\n" +
                    "");
                    break;
                }
            case "die":
                {
                    // player.TakeDamage();
                    break;
                }
            case "voice":
                {
                    VoiceChatEnabled.Set(!VoiceChatEnabled);
                    break;
                }
            case "noclip":
                {
                    bool on = !player.HasEffect<NoClipEffect>();
                    if (parts.Length == 2)
                    {
                        on = parts[1] == "on";
                    }
                    //CallClient_Noclip(player, on);
                    break;
                }
            case "resetplayer":
                {
                    CallClient_ClearAllEffectsForPlayer(player);
                    break;
                }
            case "god":
                {
                    if (parts.Length == 1 || parts[1] == "on")
                    {
                        RunChatCommand(p, "noclip on");
                        RunChatCommand(p, "zoom 3");
                        RunChatCommand(p, "sp 3");
                        RunChatCommand(p, "shadows off");
                    }
                    else
                    {
                        RunChatCommand(p, "noclip off");
                        RunChatCommand(p, "zoom 1");
                        RunChatCommand(p, "sp 1");
                        RunChatCommand(p, "shadows on");
                    }
                    break;
                }
            
            case "c":
            case "countdown":
                {
                    if (State == GameState.CountingDown)
                    {
                        if (parts.Length != 2)
                        {
                            Chat.SendMessage(player, "/countdown needs a timer value as a parameter.");
                            break;
                        }
                        if (float.TryParse(parts[1], out var value))
                        {
                            Countdown.Set(value);
                        }
                    }
                    break;
                }
            case "roundtimer":
                {
                    if (parts.Length != 2)
                    {
                        Chat.SendMessage(player, "/roundtimer needs a second paramater, either 'on' or 'off'.");
                        break;
                    }
                    if (parts[1] == "on")
                    {
                        RoundTimerEnabled.Set(true);
                    }
                    else if (parts[1] == "off")
                    {
                        RoundTimerEnabled.Set(false);
                    }
                    else
                    {
                        Chat.SendMessage(player, "/roundtimer needs a second paramater, either 'on' or 'off'.");
                        break;
                    }
                    break;
                }
            case "s":
            case "start":
                {
                    State = GameState.CountingDown;
                    Countdown.Set(0);
                    break;
                }
            case "z":
            case "zoom":
                {
                    if (parts.Length != 2)
                    {
                        Chat.SendMessage(player, "/zoom needs a zoom value as a parameter.");
                        break;
                    }
                    if (float.TryParse(parts[1], out var z))
                    {
                        player.CurrentZoomLevel.Set(z);
                    }
                    break;
                }
            case "shadows":
                {
                    bool on = !player.ShadowsEnabled;
                    if (parts.Length == 2)
                    {
                        on = parts[1] == "on";
                    }
                    player.ShadowsEnabled.Set(on);
                    break;
                }
            case "sp":
            case "speed":
                {
                    if (parts.Length != 2)
                    {
                        Chat.SendMessage(player, "/speed needs a speed value as a parameter.");
                        break;
                    }
                    if (float.TryParse(parts[1], out var s))
                    {
                        player.CheatSpeedMultiplier.Set(s);
                    }
                    break;
                }
            case "g":
            case "grant":
                {
                    if (parts.Length < 2)
                    {
                        Chat.SendMessage(player, "/grant needs an item ID as a parameter.");
                        break;
                    }
                    var itemId = parts[1];
                    int count = 1;
                    if (parts.Length >= 3)
                    {
                        int.TryParse(parts[2], out count);
                    }

                    if (!player.AddItemToInventory(itemId, count))
                    {
                        Chat.SendMessage(player, "Unknown item ID: " + parts[1]);
                        break;
                    }
                    break;
                }
            case "restart":
                {
                    State = GameState.WaitingForPlayers;
                    break;
                }
        }
    }


    [ClientRpc]
    public void ResetMapObjects(int seed)
    {
        var rng = new Random(seed);
        if (Network.IsServer)
        {
            GameSeed.Set(seed);
            // Destroy remaining traps
            foreach (var c in Scene.Components<Trap>(true))
            {
                c.DestroyTrap();
            }
        }
    }

    [ClientRpc]
    public void ToggleAmbient(Player player)
    {
        if (player.IsLocal)
        {
            AmbienceSFXDesc.Volume = AmbienceSFXDesc.Volume == 0 ? 0.35f : 0;
            SFX.UpdateSoundDesc(AmbienceSFX, AmbienceSFXDesc);
        }
    }

    [ClientRpc]
    public void ClearAllPlayerEffects()
    {
        foreach (var player in Scene.Components<MyPlayer>(false))
        {
            player.ClearAllEffects();
        }
    }

    [ClientRpc]
    public void ClearAllEffectsForPlayer(MyPlayer player)
    {
        player.ClearAllEffects();
    }

    public void ServerDestroyAllPlayerCorpses()
    {
        if (Network.IsServer)
        {
            while (AllPlayerCorpses.Count > 0)
            {
                var corpse = AllPlayerCorpses.Pop();
                Network.Despawn(corpse);
                corpse.Destroy();
            }

            foreach (var p in Scene.Components<MyPlayer>())
            {
                var player = (MyPlayer)p;
                player.PlayerCorpse.Set(null);
            }

            foreach (var box in Scene.Components<InventoryBox>())
            {
                Network.Despawn(box.Entity);
                box.Entity.Destroy();
            }
        }
    }

    public List<T> GenerateListFromWeightedRandom<T>(List<T> list, Func<T, int> weightGetter, Random rng = null)
    {
        if (rng == null) rng = new Random();

        int totalWeight = 0;
        foreach (var item in list)
        {
            totalWeight += weightGetter(item);
        }
        var result = new List<T>();
        while (list.Count > 0)
        {
            int n = rng.Next(totalWeight);
            for (int i = 0; i < list.Count; i++)
            {
                var weight = weightGetter(list[i]);
                if (n < weight)
                {
                    totalWeight -= weight;
                    result.Add(list[i]);
                    list.UnorderedRemoveAt(i);
                    continue;
                }
                n -= weight;
            }
        }
        return result;
    }

    public T PopFront<T>(List<T> list)
    {
        var result = list[0];
        for (int i = 0; i < list.Count - 1; i++)
        {
            list[i] = list[i + 1];
        }
        list.Pop();
        return result;
    }

    public void BalanceTeams(out List<MyPlayer> Red, out List<MyPlayer> Blue)
    {
        var weightedList = GenerateListFromWeightedRandom(new List<Player>(Scene.Components<Player>().ToList()), p => 10);

        weightedList.Shuffle(new Random());

        bool putOnRed = true;

        List<MyPlayer> RedTeam = new List<MyPlayer>();
        List<MyPlayer> BlueTeam = new List<MyPlayer>();

        while (weightedList.Count > 0)
        {
            if (putOnRed)
            {
                RedTeam.Add((MyPlayer)PopFront(weightedList));
            }
            else
            {
                BlueTeam.Add((MyPlayer)PopFront(weightedList));
            }
            putOnRed = !putOnRed;
        }
        Red = RedTeam;
        Blue = BlueTeam;
    }

    public void AssignPlayerARole(MyPlayer newPlayer)
    {
        var players = Scene.Components<MyPlayer>().ToList();
        // Count how many players are on each team
        int redTeamCount = players.Count(p => p.PlayerRole == PlayerRole.Role1);
        int blueTeamCount = players.Count(p => p.PlayerRole == PlayerRole.Role2);

        if (redTeamCount < blueTeamCount)
        {
            newPlayer.PlayerRole = PlayerRole.Role1;
        }
        else if (blueTeamCount < redTeamCount)
        {
            newPlayer.PlayerRole = PlayerRole.Role2;
        }
        else
        {
            // Randomly assign if both teams have equal players
            System.Random rnd = new System.Random();
            newPlayer.PlayerRole = rnd.Next(0, 2) == 0 ? PlayerRole.Role1 : PlayerRole.Role2;
        }

        if (newPlayer.PlayerRole == PlayerRole.Role1)
        {
            newPlayer.Teleport(References.Instance.Role1Spawn.Position);
        }
        else
        {
            newPlayer.Teleport(References.Instance.Role2Spawn.Position);
        }

        newPlayer.ServerResetPlayer(newPlayer.PlayerRole);
        newPlayer.CallClient_ResetAllPlayerRigs(); // todo(josh): this is N^2!!!
        CallClient_DoDropinRoundStartAnimation(newPlayer);
    }

    public void SetUpRound()
    {
        Log.Info("RESETTING ROUND ------------------");

        Util.Assert(Network.IsServer, "SetUpRound can only be called on the server");
     
        foreach (ItemDrop item in Scene.Components<ItemDrop>()) 
        {
            item.DestroyMe = true;
        }

        ForceStartedRoundWithTooFewPlayers = false;
        if (Game.LaunchedFromEditor && Scene.Components<MyPlayer>().ToList().Count < 2)
        {
            ForceStartedRoundWithTooFewPlayers = true;
        }

        //ServerDestroyAllPlayerCorpses();
        CallClient_ClearAllPlayerEffects();

        CallClient_ResetMapObjects(new Random().Next(1000));
        ItemManager.Instance.ResetItems();

        BalanceTeams(out List<MyPlayer> Red, out List<MyPlayer> Blue);

        List<MyPlayer> AllPlayers = Scene.Components<MyPlayer>().ToList();

        //float angleinc = 360f / AllPlayers.Count;
        for (int i = 0; i < AllPlayers.Count; i++)
        {
            var player = AllPlayers[i];
            player.ServerResetPlayer(Red.Contains(AllPlayers[i]) ? PlayerRole.Role1 : PlayerRole.Role2);
            player.CallClient_ResetPlayer(); // todo(josh): this is N^2!!!
            player.Teleport(Red.Contains(AllPlayers[i]) ? References.Instance.Role1Spawn.Position : References.Instance.Role2Spawn.Position);
        }

        CallClient_DoRoundStartAnimation();

        Log.Info("Round reset finished ------------------");
    }

    [ClientRpc]
    public void DoRoundStartAnimation()
    {
        foreach (var p in Scene.Components<MyPlayer>())
        {
            if (p.PlayerRole != PlayerRole.Spectator)
            {
                p.AddEffect<EffectRoundStartAnimation>();
            }
        }
    }

    [ClientRpc]
    public void DoDropinRoundStartAnimation(MyPlayer p)
    {
        p.AddEffect<EffectRoundStartAnimation>();
    }

    [ClientRpc]
    public void DoRoundEndAnimation()
    {
        foreach (var p in Scene.Components<MyPlayer>())
        {
            if (p.PlayerRole != PlayerRole.Spectator)
            {
                p.AddEffect<EffectRoundEnd>();
            }
        }
    }

    private void DrawRoundWon(Rect textrect, Rect textrect2, string roundText)
    {
        Vector4 myGreen = new Vector4(0.51f, 0.85f, 0f, 1f);
        Vector4 myGreena = new Vector4(0.51f, 0.85f, 0f, 0.05f);
        Vector4 myGreena2 = new Vector4(0.11f, 0.45f, 0f, 0.8f);
        UI.Image(UI.ScreenRect, Assets.GetAsset<Texture>("emptysquare.png"), myGreena, default, 0);
        UI.Image(UI.ScreenRect, Assets.GetAsset<Texture>("vignetteoverlay2.png"), myGreena, default, 0);
        UI.Image(UI.ScreenRect, Assets.GetAsset<Texture>("vignetteoverlay5.png"), myGreena2, default, 0);
        UI.Text(textrect, "ROUND WON!", GetTextSettingsColor(144, myGreen, 0f, null, UI.HorizontalAlignment.Center, UI.VerticalAlignment.Center));
        UI.Text(textrect2, roundText, GetTextSettingsColor(38, myGreen, 0f, null, UI.HorizontalAlignment.Center, UI.VerticalAlignment.Center));
    }

    private void DrawRoundLost(Rect textrect, Rect textrect2, string roundText)
    {
        var myRed = new Vector4(0.84f, 0.18f, 0f, 1f);
        var myReda = new Vector4(0.84f, 0.18f, 0f, 0.1f);
        var myReda2 = new Vector4(0.43f, 0.03f, 0f, 1f);

        UI.Image(UI.ScreenRect, Assets.GetAsset<Texture>("emptysquare.png"), myReda, default, 0);
        UI.Image(UI.ScreenRect, Assets.GetAsset<Texture>("vignetteoverlay2.png"), myReda, default, 0);
        UI.Image(UI.ScreenRect, Assets.GetAsset<Texture>("vignetteoverlay5.png"), myReda2, default, 0);

        UI.Text(textrect, "ROUND LOST!", GetTextSettingsColor(144, myRed, 0f, null, UI.HorizontalAlignment.Center, UI.VerticalAlignment.Center));
        UI.Text(textrect2, roundText, GetTextSettingsColor(38, myRed, 0f, null, UI.HorizontalAlignment.Center, UI.VerticalAlignment.Center));
    }

    bool start = false;
    public override void Update()
    {
        // start function????
        if (Util.OneTime(start == false, ref start))
        {
            if (Network.IsServer)
            {
                State = GameState.WaitingForPlayers;
            }
            else
            {
                ShaderAsset shader = Assets.GetAsset<ShaderAsset>("shaders/static.aosl");
                StaticMaterial = IM.CreateMaterial(shader);
                if (StaticMaterial != null) // for spark
                {
                    StaticMaterial.SetUniform("noise_texture", Assets.GetAsset<Texture>("noise.png"));
                }

                //Chat.SetChatMode(Chat.Mode.BubbleOnly);
                Chat.SetChatMode(Chat.Mode.Default);
            }

            if (Network.IsServer)
            {
                // only set if more than 2 players so we're not filling people into empty servers
                if (Scene.Components<MyPlayer>().ToList().Count >= 2)
                {
                    Game.SetMatchmakingPriority(0); // low priority
                }

                VoiceChatEnabled.Set(true);
            }
        }

        SFX.SetLoopTimeout(AmbienceSFX, 1);

        if (Network.IsServer)
        {
            switch (State)
            {
                case GameState.WaitingForPlayers:
                    {
                        if (Scene.Components<MyPlayer>().ToList().Count >= PlayersNeededToStartGame)
                        {
                            State = GameState.CountingDown;
                            Countdown.Set(30f);
                        }
                        break;
                    }
                case GameState.CountingDown:
                    {
                        if (Countdown > 0f)
                        {
                            if (Game.LaunchedFromEditor == false)
                            {
                                if (Scene.Components<MyPlayer>().ToList().Count < PlayersNeededToStartGame)
                                {
                                    State = GameState.WaitingForPlayers;
                                    break;
                                }
                            }
                        }

                        var countdownBefore = Countdown.Value;
                        Countdown.Set(Countdown - Time.DeltaTime);
                        if (countdownBefore > 10 && Countdown.Value <= 10)
                        {
                            Game.SetMatchmakingPriority(1); // low priority
                        }
                        if (Countdown <= 0f)
                        {
                            State = GameState.StartRound;
                            goto case GameState.StartRound;
                        }
                        break;
                    }
                case GameState.StartRound:
                    {
                        Game.SetMatchmakingPriority(1); // low priority


                        foreach (var player in Scene.Components<MyPlayer>())
                        {
                            Util.Assert(player.DefaultInventory != null);
                        }
                        try
                        {                           
                            SetUpRound();
                        
                            Countdown.Set(0);
                            RoundTimer.Set(60 * 15);
                            ServerRoundTimer = RoundTimer.Value;
                            State = GameState.Round;
                        }
                        catch (Exception e)
                        {
                            Log.Error(e.ToString());
                            State = GameState.WaitingForPlayers;
                        }

                        break;
                    }
                case GameState.Round:
                    {

                        foreach (var player in Scene.Components<MyPlayer>())
                        {
                            if (player.CurrentHealth <= 0 &&
                            !player.HasEffect<EffectDeath>())
                            {
                                player.CallClient_KillPlayer(player);
                            }
                        }

                        if (RoundTimerEnabled)
                        {
                            if (ServerRoundTimer > 0)
                            {
                                ServerRoundTimer -= Time.DeltaTime;
                                RoundTimer.Set((int)ServerRoundTimer);
                            }
                            else
                            {
                                ServerRoundTimer = 0;
                                RoundTimer.Set(0);
                            }
                        }

                        bool skipWinConditions = false;
                        if (Game.LaunchedFromEditor && ForceStartedRoundWithTooFewPlayers)
                        {
                            skipWinConditions = true;
                        }

                        if (!skipWinConditions)
                        {
                          
                           // TODO
                           // Custom wincons here

                            // check team left win condition
                            {
                                bool hasMemberRed = false;
                                foreach (var p in Scene.Components<MyPlayer>())
                                {
                                    var player = (MyPlayer)p;
                                    if (player.PlayerRole == PlayerRole.Role1)
                                    {
                                        hasMemberRed = true;
                                    }
                                }
                                if (!hasMemberRed)
                                {
                                    State = GameState.AllRole1Left;
                                    goto round_end;
                                }

                                bool hasMemberBlue = false;
                                foreach (var p in Scene.Components<MyPlayer>())
                                {
                                    var player = (MyPlayer)p;
                                    if (player.PlayerRole == PlayerRole.Role2)
                                    {
                                        hasMemberBlue = true;
                                    }
                                }
                                if (!hasMemberBlue)
                                {
                                    State = GameState.AllRole2Left;
                                    goto round_end;
                                }
                            }

                            // check round timer win condition
                            if (RoundTimerEnabled)
                            {
                                // TODO Win con for time ran out
                                if (RoundTimer <= 0)
                                {
                                    State = GameState.Draw;
                                    goto round_end;
                                }
                            }
                        }

                        // cache player's current roles for the end cutscene. this MUST come after all the end condition checking above
                        {
                            foreach (var player in Scene.Components<MyPlayer>())
                            {
          
                               player.PlayerRoleAtEndOfRound = player.PlayerRole;
                                
                                if (Network.IsServer)
                                {
                                    if (player.IsDead)
                                    {
                                        player.TimeIdleInSeconds = 0;
                                    }
                                    else
                                    {
                                        if (player.TimeIdleInSeconds >= 300)
                                        {
                                            // kick player here.
                                            player.TimeIdleInSeconds = 0;
                                            Network.ServerKickPlayer(player);
                                        }
                                    }
                                }
                            }
                        }

                        break; // dont run the round end code

                    round_end:;
                        CallClient_ClearAllPlayerEffects();
                        Game.SetMatchmakingPriority(0); // high priority
                        int winState = -1;
                        if (State == GameState.Role1Wins) winState = 0;
                        if (State == GameState.Role2Wins) winState = 1;
                        if (State == GameState.Draw) winState = 2;
                        if (State == GameState.AllRole1Left || State == GameState.AllRole2Left) winState = -1;

                        foreach (var p in Scene.Components<MyPlayer>())
                        {
                            var player = (MyPlayer)p;

                            if (player.PlayerRole == PlayerRole.Role1)
                            {
                                if (winState == 0)
                                {
                                    player.Wins += 1;                                   
                                }
                            }
                            else if (player.PlayerRole == PlayerRole.Role2)
                            {
                                if (winState == 1)
                                {
                                    player.Wins += 1;
                                }
                            }                           
                        }

                        GameEndTimer = 3;
                        break;
                    }
                case GameState.Role1Wins:
                case GameState.Role2Wins:
                case GameState.Draw:
                case GameState.AllRole1Left:
                case GameState.AllRole2Left:
                    {
                        if (GameEndTimer > 0)
                        {
                            GameEndTimer -= Time.DeltaTime;
                            if (GameEndTimer <= 0)
                            {
                                switch (State)
                                {
                                    case GameState.Role1Wins: { Cutscenes.Instance.CallClient_PlayRole1Win(); break; }
                                    case GameState.Role2Wins: { Cutscenes.Instance.CallClient_PlayRole2Win(); break; }
                                    case GameState.Draw:
                                    case GameState.AllRole1Left:
                                    case GameState.AllRole2Left: 
                                    Cutscenes.Instance.CallClient_PlayDraw(); break;                                
                                    default:
                                        {
                                            Util.Assert(false, "Unknown game state: " + State);
                                            break;
                                        }
                                }
                            }
                        }
                        else
                        {
                            if (Cutscenes.Instance.CurrentCutscene == Cutscenes.CutsceneType.None)
                            {
                                foreach (var player in Scene.Components<MyPlayer>())
                                {
                                    ((MyPlayer)player).ClearInventory();
                                }
                                CallClient_DoRoundEndAnimation();
                                State = GameState.WaitingForPlayers;
                                Countdown.Set(10);
                            }
                        }
                        break;
                    }
            }
        }
      
        var localPlayer = (MyPlayer)Network.LocalPlayer;
        if (localPlayer != null)
        {
            var timerRect = UI.ScreenRect.CutTop(100);
            var topBarRect = timerRect.BottomRect().GrowBottom(40).Offset(0, 3);
            var midBarRect = UI.ScreenRect.SubRect(0.5f, 0.8f, 0.5f, 0.8f);
            var midBarRect2 = UI.ScreenRect.SubRect(0.5f, 0.2f, 0.5f, 0.2f);

            var redScoreRect = UI.ScreenRect.CutTop(100).Offset(-200, 0);
            var blueScoreRect = UI.ScreenRect.CutTop(100).Offset(200, 0);

            var bottomBarRect = UI.SafeRect.CutBottom(350);

            using var _ = UI.PUSH_LAYER(RoleNameLayer);

            if (State >= GameState.Round)
            {
                if (RoundTimerEnabled)
                {
                    var roundString = $"{(RoundTimer / 60).ToString("D2")}:{(RoundTimer % 60).ToString("D2")}";
                    var textColor = Vector4.White;
                    if (RoundTimer <= 60)
                    {
                        textColor = Vector4.Lerp(Vector4.Red, Vector4.White, Ease.T(Time.TimeSinceStartup - LastRoundTimerSyncTime, 1f));
                    }
                    var ts = GetTextSettingsColor(40, textColor, 0f, null);
                    UI.Text(timerRect, roundString, ts);
                }
               
            }

            if (localPlayer.HideHudReasons.Count == 0 && (State != GameState.WaitingForPlayers && State != GameState.CountingDown))
            {
                var roleText = localPlayer.PlayerRole == PlayerRole.Spectator ? "Spectating" : Roles[localPlayer.PlayerRole].RoleName;
                Vector4 roleColor = new Vector4(1, 1, 1, 1);
                if (localPlayer.PlayerRole == PlayerRole.Spectator)
                {
                    roleColor = new Vector4(0.35f, 0.76f, 0.98f, 1f);
                }
                else if (localPlayer.PlayerRole == PlayerRole.Role1)
                {
                    roleColor = new Vector4(1, 0, 0, 1);
                }
                else if (localPlayer.PlayerRole == PlayerRole.Role2)
                {
                    roleColor = new Vector4(0, 0, 1, 1);
                }

                UI.Text(topBarRect, roleText, GetTextSettingsColor(56, roleColor, 0f, null));
                if (localPlayer.PlayerRole == PlayerRole.Spectator)
                {
                    UI.Text(topBarRect.Grow(0, 0, 100, 0), "(Fly around till the next round starts!)", GetTextSettings(36, 0f, null));
                }

            }

            switch (State)
            {
                case GameState.WaitingForPlayers:
                    {
                        UI.Text(bottomBarRect, $"Waiting for players ({Scene.Components<MyPlayer>().ToList().Count}/{PlayersNeededToStartGame})", GetTextSettings(52, 0f, null, UI.HorizontalAlignment.Center));
                        break;
                    }
                case GameState.CountingDown:
                    {
                        UI.Text(bottomBarRect, ("Round starts in " + Math.Round(GameManager.Instance.Countdown)).ToString() + " seconds...", GetTextSettings(42, 0f, null, UI.HorizontalAlignment.Center));
                        break;
                    }
                case GameState.Round:
                    {

                        break;
                    }
                case GameState.Role1Wins:
                    {
                        if (localPlayer.PlayerRole == PlayerRole.Role1)
                        {
                            DrawRoundWon(midBarRect, midBarRect2, "Role 1 Wins!");
                        }
                        else
                        {
                            DrawRoundLost(midBarRect, midBarRect2, "Role 1 Wins!");
                        }
                        break;
                    }
                case GameState.Role2Wins:
                    {
                        if (localPlayer.PlayerRole == PlayerRole.Role2)
                        {
                            DrawRoundWon(midBarRect, midBarRect2, "Role 2 Wins!");
                        }
                        else
                        {
                            DrawRoundLost(midBarRect, midBarRect2, "Role 2 Wins!");
                        }
                        break;
                    }
                case GameState.Draw:
                    {
                        DrawRoundLost(midBarRect, midBarRect2, "Tie Game!");
                        break;
                    }
            }
        }
    }

    public UI.TextSettings GetTextSettingsColor(float size, Vector4 textColor, float offset = 0, FontAsset font = null, UI.HorizontalAlignment halign = UI.HorizontalAlignment.Center, UI.VerticalAlignment valign = UI.VerticalAlignment.Center)
    {
        if (font == null)
        {
            font = UI.Fonts.BarlowBold;
        }
        var ts = new UI.TextSettings()
        {
            Font = font,
            Size = size,
            Color = textColor,
            DropShadow = true,
            DropShadowColor = new Vector4(0f, 0f, 0.02f, 0.5f),
            DropShadowOffset = new Vector2(0f, -3f),
            HorizontalAlignment = halign,
            VerticalAlignment = valign,
            WordWrap = false,
            WordWrapOffset = 0,
            Outline = true,
            OutlineThickness = 3,
            Offset = new Vector2(0, offset),
        };
        return ts;
    }

    public UI.TextSettings GetSimpleTextSettings(float size, float offset = 0, FontAsset font = null)
    {
        if (font == null)
        {
            font = UI.Fonts.BarlowBold;
        }
        var ts = new UI.TextSettings()
        {
            Font = font,
            Size = size,
            Color = new Vector4(0f, 0f, 0f, 0f),
            HorizontalAlignment = UI.HorizontalAlignment.Center,
            VerticalAlignment = UI.VerticalAlignment.Center,
            WordWrap = false,
            WordWrapOffset = 0,
            Outline = false,
            OutlineThickness = 3,
            Offset = new Vector2(0, offset),
        };
        return ts;
    }

    public UI.TextSettings GetTextSettings(float size, float offset = 0, FontAsset font = null, UI.HorizontalAlignment halign = UI.HorizontalAlignment.Center, UI.VerticalAlignment valign = UI.VerticalAlignment.Center)
    {
        if (font == null)
        {
            font = UI.Fonts.BarlowBold;
        }
        var ts = new UI.TextSettings()
        {
            Font = font,
            Size = size,
            Color = Vector4.White,
            DropShadow = true,
            DropShadowColor = new Vector4(0f, 0f, 0.02f, 0.5f),
            DropShadowOffset = new Vector2(0f, -3f),
            HorizontalAlignment = halign,
            VerticalAlignment = valign,
            WordWrap = false,
            WordWrapOffset = 0,
            Outline = true,
            OutlineThickness = 3,
            Offset = new Vector2(0, offset),
        };
        return ts;
    }

    // todo(josh): surely this could just be a lerp...
    // Gradually changes a value towards a desired goal over time.
    public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed = 99999f)
    {
        float deltaTime = Time.DeltaTime;
        // Based on Game Programming Gems 4 Chapter 1.10
        smoothTime = Math.Max(0.0001F, smoothTime);
        float omega = 2F / smoothTime;

        float x = omega * deltaTime;
        float exp = 1F / (1F + x + 0.48F * x * x + 0.235F * x * x * x);
        float change = current - target;
        float originalTo = target;

        // Clamp maximum speed
        float maxChange = maxSpeed * smoothTime;
        change = Math.Clamp(change, -maxChange, maxChange);
        target = current - change;

        float temp = (currentVelocity + omega * change) * deltaTime;
        currentVelocity = (currentVelocity - omega * temp) * exp;
        float output = target + (change + temp) * exp;

        // Prevent overshooting
        if (originalTo - current > 0.0F == output > originalTo)
        {
            output = originalTo;
            currentVelocity = (output - originalTo) / deltaTime;
        }

        return output;
    }

    public Vector2 GetOnCircle(float angleDegrees, float radius)
    {
        // initialize calculation variables
        float _x = 0;
        float _y = 0;
        float angleRadians = 0;
        Vector2 _returnVector;

        // convert degrees to radians
        angleRadians = angleDegrees * (float)Math.PI / 180.0f;

        // get the 2D dimensional coordinates
        _x = radius * (float)Math.Cos(angleRadians);
        _y = radius * (float)Math.Sin(angleRadians);

        // derive the 2D vector
        _returnVector = new Vector2(_x, _y);

        // return the vector info
        return _returnVector;
    }
    public class PlayerRoleDefinition
    {
        public string RoleName;
    }
}

public enum GameState
{
    WaitingForPlayers,
    CountingDown,
    StartRound,
    Round,
    Role1Wins,
    Role2Wins,
    Draw,
    AllRole1Left,
    AllRole2Left,
}