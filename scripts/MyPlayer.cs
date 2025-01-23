using AO;
using System;

public partial class MyPlayer : Player
{
    public static Texture HealthBarBack_BlueTeam = Assets.KeepLoaded<Texture>("UI/Health_Bar/health_bar_blue/healthbar_back.png");
    public static Texture HealthBarBack_RedTeam = Assets.KeepLoaded<Texture>("UI/Health_Bar/health_bar_red/healthbar_back.png");
    public static Texture HealthBarFill_BlueTeam = Assets.KeepLoaded<Texture>("UI/Health_Bar/health_bar_blue/healthbar_fill.png");
    public static Texture HealthBarFill_RedTeam = Assets.KeepLoaded<Texture>("UI/Health_Bar/health_bar_red/healthbar_fill.png");
    public static Texture Pip_RedTeam = Assets.KeepLoaded<Texture>("UI/Health_Bar/health_bar_red/healthbar_pip.png");
    public static Texture Pip_BlueTeam = Assets.KeepLoaded<Texture>("UI/Health_Bar/health_bar_blue/healthbar_pip.png");

    public const float LightOuterRadius = 30;
    public const float LightInnerRadius = 21;

    public bool Music = true;

    public bool IsDead => HasEffect<SpectatorEffect>();

    public SyncVar<bool> Mobile = new();

    private string _currentItemID = new("");
    public string CurrentItemInHand
    {
        get => _currentItemID;
        set
        {
            if (Network.IsClient)
            {
                _currentItemID = value;
            }
        }
    }
    string oldItemInHand = "";

    public static readonly int DefaultMaxHealth = 100;
    private SyncVar<int> _currentHealth = new(DefaultMaxHealth);
    public int CurrentHealth
    {
        get => _currentHealth.Value;
        set
        {
            if (Network.IsServer)
            {
                _currentHealth.Set(value);
            }
        }
    }

    public int Wins
    {
        get
        {
            if (Network.IsClient) return 0;
            return Save.GetInt(this, "wins", 0);
        }

        set
        {
            if (Network.IsServer)
            {
                Save.SetInt(this, "wins", value);
                Save.OrderedSet("wins", $"{this.UserId}", value);
            }
        }
    }
  

    private SyncVar<int> _playerRole = new((int)PlayerRole.Spectator);
    public PlayerRole PlayerRole
    {
        get => (PlayerRole)_playerRole.Value;
        set => _playerRole.Set((int)value);
    }

    private SyncVar<int> _playerRoleAtEnd = new((int)PlayerRole.Spectator);
    public PlayerRole PlayerRoleAtEndOfRound
    {
        get => (PlayerRole)_playerRoleAtEnd.Value;
        set => _playerRoleAtEnd.Set((int)value);
    }


    public SyncVar<float> CurrentZoomLevel = new(1.0f);

    public SyncVar<bool> ShadowsEnabled = new(true);

    public CameraControl CameraControl;

    public bool FocusingUI = false;
    public bool FocusingUIVignette = false;
    public float CurrentFocusUI = 1f;
    public float CurrentFocusUIVelocity = 0f;
    public float CurrentFocusUIVignette = 0f;
    public float CurrentFocusUIVignetteVelocity = 0f;
    public float FlashAlpha = 0f;
    public float FlashAlphavel = 0f;
    public Vector4 FlashColor = new Vector4(1f, 0f, 0f, 1f);

    public float CurrentCamSlider;
    public float CurrentCamVelocity = 0f;

    public SyncVar<float> CheatSpeedMultiplier = new(1);

    public Entity SecondaryCamTarget;

    public List<string> HideHudReasons = new List<string>();
    public SyncVar<Entity> PlayerCorpse = new();

    public Vector3 AmbientColour = new Vector3(35f / 255f, 20f / 255f, 20f / 255f);

    public Vector2 Dash = Vector2.Zero;
    protected float DashRemainingDuration;
    protected const float DashDecayThreshold = 0.1f;

    public Entity LightFollow;
    public Light PlayerLight;

    public float TimeIdleInSeconds = 0;

    public BillboardSign NearSign;

    public Spine_Animator WorldOverrideSkeleton;
    protected Vector2 SkeletonScaleOriginal;

    private SyncVar<float> _scaleMod = new(1f);
    public float ScaleMod
    {
        get => _scaleMod.Value;
        set
        {
            if (Network.IsServer)
            {
                _scaleMod.Set(value);
            }
        }
    }
    public float CScaleMod = 1f;
    public Entity StoreEntity;

    public override void Awake()
    {
        if (IsLocal)
        {
            CameraControl = CameraControl.Create(1);
            CameraControl.Zoom = CurrentZoomLevel.Value;
            CameraControl.AmbientColour = AmbientColour;

            var lightEntity = Entity.FindByName("LightPlayer");
            PlayerLight = lightEntity.GetComponent<Light>();            
        }

        if (Network.IsClient)
        {
            var overrideSkeletonEntity = Entity.Create();
            overrideSkeletonEntity.SetParent(Entity, false);
            SkeletonScaleOriginal = new Vector2(0.1f, 0.1f);
            WorldOverrideSkeleton = overrideSkeletonEntity.AddComponent<Spine_Animator>();
            WorldOverrideSkeleton.Entity.LocalScale = SkeletonScaleOriginal;
            WorldOverrideSkeleton.Entity.LocalEnabled = false;
        }

        CurrentZoomLevel.OnSync += (old, value) =>
        {
        };

        _playerRole.OnSync += (old, value) =>
        {
            RemoveEffect<SpectatorEffect>(false);
            if (PlayerRole == PlayerRole.Spectator)
            {
                AddEffect<SpectatorEffect>();
            }
        };

        ShadowsEnabled.OnSync += (old, value) =>
        {
            if (IsLocal == false) return;

            var lightEntity = Entity.FindByName("LightPlayer");
            lightEntity.LocalEnabled = value;
        };
        
        // Base player spine
        {
            StateMachineVariable mIKBool = null;
            StateMachineVariable movingBool = null;

            var gameLayer = SpineAnimator.SpineInstance.StateMachine.CreateLayer("game_layer", 10);
            var empty = gameLayer.CreateState("__CLEAR_TRACK__", 0, true);

            var gameLayer2 = SpineAnimator.SpineInstance.StateMachine.CreateLayer("game_layer2", 11);
            var empty2 = gameLayer2.CreateState("__CLEAR_TRACK__", 0, true);

            var aoLayer = SpineAnimator.SpineInstance.StateMachine.TryGetLayerByName("main");
            var aoIdleState = aoLayer.TryGetStateByName("Idle");
            var aoRunState = aoLayer.TryGetStateByName("Run_Fast");

            mIKBool = SpineAnimator.SpineInstance.StateMachine.CreateVariable("mIK", StateMachineVariableKind.BOOLEAN);
            movingBool = SpineAnimator.SpineInstance.StateMachine.TryGetVariableByName("moving");
            var gameIdleMIKState = gameLayer.CreateState("008BED/Idle_mIK", 0, true);
            var gameRunMIKState = gameLayer.CreateState("008BED/Run_Fast_mIK", 0, true);

            // mIK
            gameLayer.CreateTransition(empty, gameIdleMIKState, false).CreateBoolCondition(mIKBool, true);
            gameLayer.CreateTransition(empty, gameRunMIKState, false).CreateBoolCondition(mIKBool, true);
            gameLayer.CreateTransition(gameIdleMIKState, gameRunMIKState, false).CreateBoolCondition(movingBool, true);
            gameLayer.CreateTransition(gameRunMIKState, gameIdleMIKState, false).CreateBoolCondition(movingBool, false);
            gameLayer.CreateTransition(gameIdleMIKState, empty, false).CreateBoolCondition(mIKBool, false);
            gameLayer.CreateTransition(gameRunMIKState, empty, false).CreateBoolCondition(mIKBool, false);


            var attackMik = gameLayer.CreateState("008BED/attack_mIK_AL", 0, false);
            var meleeTrigger = SpineAnimator.SpineInstance.StateMachine.CreateVariable("melee", StateMachineVariableKind.TRIGGER);
            gameLayer.CreateGlobalTransition(attackMik).CreateTriggerCondition(meleeTrigger);
            gameLayer.CreateTransition(attackMik, empty, true);

            var doubleAttackMik = gameLayer.CreateState("008BED/double_attack_mIK_AL", 0, false);
            var doubleMeleeTrigger = SpineAnimator.SpineInstance.StateMachine.CreateVariable("double_melee", StateMachineVariableKind.TRIGGER);
            gameLayer.CreateGlobalTransition(doubleAttackMik).CreateTriggerCondition(doubleMeleeTrigger);
            gameLayer.CreateTransition(doubleAttackMik, empty, true);

            var getHit = gameLayer.CreateState("008BED/get_hit", 0, false);
            var hitTrigger = SpineAnimator.SpineInstance.StateMachine.CreateVariable("hit", StateMachineVariableKind.TRIGGER);
            gameLayer.CreateGlobalTransition(getHit).CreateTriggerCondition(hitTrigger);
            gameLayer.CreateTransition(getHit, empty, true);

            var teleportLand = gameLayer.CreateState("Teleport_Appear", 0, false);
            var teleportLandTrigger = SpineAnimator.SpineInstance.StateMachine.CreateVariable("teleport_land", StateMachineVariableKind.TRIGGER);
            gameLayer.CreateGlobalTransition(teleportLand).CreateTriggerCondition(teleportLandTrigger);
            gameLayer.CreateTransition(teleportLand, empty, true);

            var hitFlying = gameLayer.CreateState("CTF/hit_fly", 0, false);
            var hitFlyingTrigger = SpineAnimator.SpineInstance.StateMachine.CreateVariable("hit_fly", StateMachineVariableKind.TRIGGER);
            gameLayer.CreateGlobalTransition(hitFlying).CreateTriggerCondition(hitFlyingTrigger);
            gameLayer.CreateTransition(hitFlying, teleportLand, true);

            var grow = gameLayer.CreateState("Grow_Big", 0, false);
            var growTrigger = SpineAnimator.SpineInstance.StateMachine.CreateVariable("grow", StateMachineVariableKind.TRIGGER);
            gameLayer.CreateGlobalTransition(grow).CreateTriggerCondition(growTrigger);
            gameLayer.CreateTransition(grow, empty, true);

            var shrink = gameLayer.CreateState("CTF/Shrink", 0, false);
            var shrinkTrigger = SpineAnimator.SpineInstance.StateMachine.CreateVariable("shrink", StateMachineVariableKind.TRIGGER);
            gameLayer.CreateGlobalTransition(shrink).CreateTriggerCondition(shrinkTrigger);
            gameLayer.CreateTransition(shrink, empty, true);

            var smash = gameLayer.CreateState("CTF/attack_mIK_AL2", 0, false);
            var smashTrigger = SpineAnimator.SpineInstance.StateMachine.CreateVariable("smash", StateMachineVariableKind.TRIGGER);
            gameLayer.CreateGlobalTransition(smash).CreateTriggerCondition(smashTrigger);
            gameLayer.CreateTransition(smash, empty, true);

            var remote = gameLayer.CreateState("CTF/UseRemote", 0, false);
            var remoteTrigger = SpineAnimator.SpineInstance.StateMachine.CreateVariable("remote", StateMachineVariableKind.TRIGGER);
            gameLayer.CreateGlobalTransition(remote).CreateTriggerCondition(remoteTrigger);
            gameLayer.CreateTransition(remote, empty, true);

            var punchMik = gameLayer.CreateState("CTF/punch_mIK", 0, false);
            var punchTrigger = SpineAnimator.SpineInstance.StateMachine.CreateVariable("punch_mIK", StateMachineVariableKind.TRIGGER);
            gameLayer.CreateGlobalTransition(punchMik).CreateTriggerCondition(punchTrigger);
            gameLayer.CreateTransition(punchMik, empty, true);

            var shootGun = gameLayer2.CreateState("CTF/shoot_gun_AL", 0, false);
            var shootGunTrigger = SpineAnimator.SpineInstance.StateMachine.CreateVariable("shoot_gun", StateMachineVariableKind.TRIGGER);
            gameLayer2.CreateGlobalTransition(shootGun).CreateTriggerCondition(shootGunTrigger);
            gameLayer2.CreateTransition(shootGun, empty2, true);

            gameLayer.SetInitialState(empty);
            gameLayer2.SetInitialState(empty);
        }      

        var collisionEntity = Assets.GetAsset<Prefab>("PlayerCollision.prefab").Instantiate();
        collisionEntity.GetComponent<PlayerCollisionChild>().Player = this;
        collisionEntity.LocalScale = new Vector2(1.1f, 1.1f);
        collisionEntity.SetParent(Entity, false);

        if (IsLocal)
        {
            LightFollow = Entity.Create();
            LightFollow.SetParent(Entity, false);
            LightFollow.LocalPosition = new Vector2(0, 0.75f);
            var light = Entity.FindByName("LightPlayer");
            light.GetComponent<PlayerFollower>().Following = LightFollow;
        }

        if (Network.IsServer)
        {
            LoadPlayerData();
        }
    }

    public void LoadPlayerData()
    {
        // Load saved data for players here
    }

    public void SetWorldOverrideSkeleton(string id)
    {
        if (Network.IsServer) { return; }

        if (WorldOverrideSkeleton.Alive())
        {
            WorldOverrideSkeleton.Entity.Destroy();
        }
        WorldOverrideSkeleton = null;

        var woStateMachine = StateMachine.Make(); ;
        var woLayer = woStateMachine.CreateLayer("woLayer", 0);

        var woEntity = Entity.Create();
        woEntity.SetParent(Entity, false);
        WorldOverrideSkeleton = woEntity.AddComponent<Spine_Animator>();
        WorldOverrideSkeleton.Awaken();
        StateMachineVariable movingBool = null;

        switch (id)
        {
            case "Chicken":
                WorldOverrideSkeleton.SpineInstance.SetSkeleton(Assets.GetAsset<SpineSkeletonAsset>("animations/chicken/FUSE105_chicken.spine"));
                WorldOverrideSkeleton.MaskInShadow = true;

                movingBool = woStateMachine.CreateVariable("moving", StateMachineVariableKind.BOOLEAN);
                var idleState = woLayer.CreateState("FUSE105/idle", 0, true);
                var runState = woLayer.CreateState("FUSE105/run", 0, true);
                woLayer.CreateTransition(idleState, runState, false).CreateBoolCondition(movingBool, true);
                woLayer.CreateTransition(runState, idleState, false).CreateBoolCondition(movingBool, false);
                woLayer.SetInitialState(idleState);

                WorldOverrideSkeleton.SpineInstance.SetStateMachine(woStateMachine, woEntity);
                WorldOverrideSkeleton.SpineInstance.Scale = new Vector2(2, 2);
                WorldOverrideSkeleton.LocalEnabled = true;
                break;

            default:
                Log.Warn("Invalid ID given to Override Skeleton");
                return;

        }
        AddInvisibilityReason("OverrideSkeleton");
    }

    public void RemoveWorldOverrideSkeleton()
    {
        if (Network.IsServer) { return; }

        RemoveInvisibilityReason("OverrideSkeleton");
        WorldOverrideSkeleton.Entity.LocalEnabled = false;
        SpineAnimator.SpineInstance.SetStateMachine(SpineAnimator.SpineInstance.StateMachine, null);
    }


    bool start = false;
    public override void Update()
    {
        if (Util.OneTime(start == false, ref start))
        {
            if (Network.IsServer)
            {
                if (GameManager.Instance.State == GameState.WaitingForPlayers)
                {
                    PlayerRole = PlayerRole.Role1;
                }

                if ((GameManager.Instance.State == GameState.Round || GameManager.Instance.State == GameState.CountingDown))
                {
                    GameManager.Instance.AssignPlayerARole(this);
                }              
            }           

            if (Network.IsClient && IsLocal)
            {
                CallServer_SetIsMobile(Game.IsMobile);
            }
          
        }

        DrawAbilities();

        // Player light
        if (IsLocal)
        {
            var currentCamera = CameraControl.GetCurrent();
            if (currentCamera != CameraControl)
            {
                CameraControl.Zoom = AOMath.Lerp(CameraControl.Zoom, currentCamera.Zoom, 10 * Time.DeltaTime);
            }
            else
            {
                CameraControl.Zoom = AOMath.Lerp(CameraControl.Zoom, CurrentZoomLevel, 10 * Time.DeltaTime);
            }
           
            // Player Light follow stuff
            {
                PlayerLight.GetComponent<PlayerFollower>().Following = LightFollow;

                PlayerLight.Color = new Vector4(1, 1, 1, 1);

                PlayerLight.Radi = Vector2.Lerp(PlayerLight.Radi, new Vector2(LightInnerRadius, LightOuterRadius), 10 * Time.DeltaTime);


                if (HasEffect<WatchingCutsceneEffect>())
                {
                    PlayerLight.LocalEnabled = false;
                }
                else
                {
                    PlayerLight.LocalEnabled = true;
                }

            }
           

            if (HideHudReasons.Count == 0 && GameManager.Instance.State == GameState.Round)
            {
               
            }
        }

        bool moving = Velocity.Length > 0.03f;

        if (WorldOverrideSkeleton.Alive() && WorldOverrideSkeleton.SpineInstance != null && WorldOverrideSkeleton.SpineInstance.StateMachine != null)
        {
            WorldOverrideSkeleton.SpineInstance.StateMachine.SetBool("moving", moving);
        }

        if (IsLocal)
        {
            if (SecondaryCamTarget.Alive())
            {
                CurrentCamSlider = GameManager.SmoothDamp(CurrentCamSlider, 1f, ref CurrentCamVelocity, 0.4f);
                Vector2 currentCamPos = Vector2.Lerp(CameraControl.Position, SecondaryCamTarget.Position, CurrentCamSlider);
                CameraControl.Position = currentCamPos;
            }
            else
            {
                Vector2 playerPos = this.Entity.Position + new Vector2(0, 0.5f);
                //CameraControl.Position = GameManager.SmoothDamp(CurrentFocusUI,1f,ref CurrentFocusUIVelocity,0.1f);
                CurrentCamSlider = GameManager.SmoothDamp(CurrentCamSlider, 0f, ref CurrentCamVelocity, 0.4f);

                Vector2 currentCamPos = Vector2.Lerp(playerPos, CameraControl.Position, CurrentCamSlider);
                CameraControl.Position = currentCamPos;
            }

            // don't draw this

            if (!IsDead)
            {
                // hotbar
                {
                    using var _1 = UI.PUSH_LAYER(GameManager.HotbarLayer);
                    var shouldDrawHotbar = HideHudReasons.Count == 0;
                    if (shouldDrawHotbar)
                    {
                        var hotbarResult = Inventory.DrawHotbar(DefaultInventory.Id, new Inventory.DrawOptions()
                        {
                            HotbarItemCount = 6,
                            AllowDragDrop = true,
                            ScrollItemSelection = true,
                            KeyboardItemSelection = true,
                            EnableUseFromHotbar = true,
                            EnableSelection = true
                        });

                        if (hotbarResult.DroppedItem != null)
                        {
                            CallServer_PleaseDropItem(hotbarResult.DroppedItem.InventorySlot);
                        }

                        if (hotbarResult.SelectedItem != null)
                        {
                            CurrentItemInHand = hotbarResult.SelectedItem.Definition.Name;
                        }
                        else
                        {
                            CurrentItemInHand = "";
                        }
                    }
                }
            }

            // vignette
            {
                using var _2 = UI.PUSH_LAYER(GameManager.VignetteLayer);

                var fullscreenrect = UI.ScreenRect;
                UI.Image(fullscreenrect, Assets.GetAsset<Texture>("vignetteoverlay5.png"), new Vector4(0f, 0f, 0.3f, 0.8f), default, 0);
                CurrentFocusUI = AOMath.MoveToward(CurrentFocusUI, FocusingUI ? 1f : 0f, Time.DeltaTime * 5);
                CurrentFocusUIVignette = AOMath.MoveToward(CurrentFocusUIVignette, FocusingUIVignette ? 1f : 0f, Time.DeltaTime * 5);

                UI.Image(fullscreenrect, Assets.GetAsset<Texture>("emptysquare.png"), new Vector4(0f, 0f, 0.2f, CurrentFocusUI * 0.95f), default, 0);
                UI.Image(fullscreenrect, Assets.GetAsset<Texture>("vignetteoverlay3.png"), new Vector4(0f, 0f, 0.2f, CurrentFocusUIVignette * 1f), default, 0);
                UI.Image(fullscreenrect, Assets.GetAsset<Texture>("vignetteoverlay3.png"), new Vector4(0f, 0f, 0.2f, CurrentFocusUIVignette * 1f), default, 0);
                UI.Image(fullscreenrect, Assets.GetAsset<Texture>("vignetteoverlay3.png"), new Vector4(0f, 0f, 0.2f, CurrentFocusUIVignette * 1f), default, 0);

                Vector4 _flashCol = FlashColor;
                _flashCol.W = FlashAlpha * 0.8f;
                UI.Image(fullscreenrect, Assets.GetAsset<Texture>("vignetteoverlay5.png"), _flashCol, default, 0);

                FlashAlpha = AOMath.MoveToward(FlashAlpha, 0f, Time.DeltaTime * 1);
            }
        }

        if (CurrentItemInHand != oldItemInHand)
        {
            AdjustSkins();
            if (IsLocal)
            {
                CallServer_RemoveAllAimEffects(this);
            }
        }

        DashDecay();
    }

    [ServerRpc]
    public void RemoveAllAimEffects(MyPlayer player)
    {
        CallClient_CRemoveAllAimEffects(player);
    }

    [ClientRpc]
    public void CRemoveAllAimEffects(MyPlayer player)
    {
        foreach (AEffect effect in player.Effects)
        {
            if (effect is AimEffect)
            {
                player.RemoveEffect(effect, true);
                break;
            }
        }
    }

    string currentSkin = "";
    void AdjustSkins()
    {
        if (currentSkin != "")
        {
            SpineAnimator.SpineInstance.DisableSkin(currentSkin);
        }

        switch (CurrentItemInHand)
        {
            case "Assault Rifle":
                currentSkin = "weapons/assault_rifle";
                SpineAnimator.SpineInstance.EnableSkin("weapons/assault_rifle");
                break;

            case "Balloon Sword":
                currentSkin = "weapons/balloon_sword";
                SpineAnimator.SpineInstance.EnableSkin("weapons/balloon_sword");
                break;

            case "Baseball Bat":
                currentSkin = "weapons/baseball_bat";
                SpineAnimator.SpineInstance.EnableSkin("weapons/baseball_bat");
                break;

            case "Beam Saber":
                currentSkin = "weapons/beam_saber";
                SpineAnimator.SpineInstance.EnableSkin("weapons/beam_saber");
                break;

            case "Bee Cannon":
                currentSkin = "weapons/beehive_launcher";
                SpineAnimator.SpineInstance.EnableSkin("weapons/beehive_launcher");
                break;

            case "Chicken Sword":
                currentSkin = "weapons/chicken";
                SpineAnimator.SpineInstance.EnableSkin("weapons/chicken");
                break;

            case "Katana":
                currentSkin = "weapons/katana";
                SpineAnimator.SpineInstance.EnableSkin("weapons/katana");
                break;

            case "Raygun":
                currentSkin = "weapons/ray_gun";
                SpineAnimator.SpineInstance.EnableSkin("weapons/ray_gun");
                break;

            case "Rocket Launcher":
                currentSkin = "weapons/rocket_launcher";
                SpineAnimator.SpineInstance.EnableSkin("weapons/rocket_launcher");
                break;

            case "Shotgun":
                currentSkin = "weapons/shotgun";
                SpineAnimator.SpineInstance.EnableSkin("weapons/shotgun");
                break;

            case "Sniper Rifle":
                currentSkin = "weapons/sniper_rifle";
                SpineAnimator.SpineInstance.EnableSkin("weapons/sniper_rifle");
                break;

            case "Revolver":
                currentSkin = "weapons/revolver";
                SpineAnimator.SpineInstance.EnableSkin("weapons/revolver");
                break;

            default:
                break;
        }

        SpineAnimator.SpineInstance.RefreshSkins();
        oldItemInHand = CurrentItemInHand;
    }

    public override void WriteFrameData(AO.StreamWriter writer)
    {
        Util.Assert(Network.IsClient);
        writer.WriteString(_currentItemID);
    }
    public override void ReadFrameData(AO.StreamReader reader)
    {
        _currentItemID = reader.ReadString();
    }

    public override void LateUpdate()
    {
        base.LateUpdate();

        if (Network.IsServer)
        {        
            if (!PlayerCorpse.Value.Alive() && GameManager.Instance.State == GameState.Round)
            {
                var playerCorpse = References.Instance.PlayerCorpsePrefab.Instantiate<PlayerCorpse>();
                playerCorpse.Entity.Name = $"{Name}_corpse";
                playerCorpse.Entity.Position = new Vector2(1000, 1000);
                playerCorpse.PlayerName = Name;
                playerCorpse.ColorIndex = ColorIndex;

                Network.Spawn(playerCorpse.Entity);
                playerCorpse.ForPlayer.Set(this.Entity);
                GameManager.Instance.AllPlayerCorpses.Add(playerCorpse.Entity);
                playerCorpse.CallClient_SetSkins(SpineAnimator.SpineInstance.GetSkins());

                PlayerCorpse.Set(playerCorpse.Entity);
            }

        }     

        if (!IsDead)
        {
            DrawHealthBar();
        }

        if (this.IsLocal)
        {
            if (Store.Instance.ItemShopOpen == true &&
                StoreEntity.Alive())
            {
                if (Vector2.Distance(Entity.Position, StoreEntity.Position) >= 4f)
                {
                    Store.Instance.ItemShopOpen = false;
                }
            }

            DrawDamageNumber();
        }

        // Camera control for size altering effects
        /*
        if (Size altering effects)
        {
            SpineAnimator.SpineInstance.Scale = new Vector2(CScaleMod, CScaleMod);
            SpineAnimator.SpineInstance.Speed = 1f / CScaleMod;
            if (IsLocal && CurrentZoomLevel.Value == 1f &&
                !DoesEffectControlZoom())
            {
                CameraControl.Zoom = 1f + ((CScaleMod - 1f) * 0.33f);
            }
        }
        else
        */
        {
            SpineAnimator.SpineInstance.Scale = new Vector2(ScaleMod, ScaleMod);
            SpineAnimator.SpineInstance.Speed = 1f / ScaleMod;
            if (IsLocal && CurrentZoomLevel.Value == 1f &&
                !DoesEffectControlZoom())
            {
                CameraControl.Zoom = 1f + ((ScaleMod - 1f) * 0.33f);
            }
        }
    }

    bool DoesEffectControlZoom()
    {
       
        return false;
    }

    public void DrawArrowToPosition(Vector2 position, bool red)
    {
        // Copied from Fatsim sell area stuff
        var aspect = UI.SafeRect.Width / UI.SafeRect.Height;
        var worldOffset = position - Entity.Position;
        var targetPlayerScreenPos = Camera.WorldToScreen(position + new Vector2(0, 1f));
        var killerPlayerScreenPos = Camera.WorldToScreen(Entity.Position + new Vector2(0, 0.5f));
        var dir = (targetPlayerScreenPos - killerPlayerScreenPos).Normalized;
        var pos = killerPlayerScreenPos;
        var distance = worldOffset.Length;
        float arrowSize = 40;
        var anim = (float)Math.Pow(Math.Abs(Math.Sin(Math.PI * Time.TimeSinceStartup)), 0.75);
        float distanceThreshold = 5;
        distanceThreshold = ((dir * distanceThreshold) / new Vector2(1, aspect)).Length;
        if (distance >= (distanceThreshold + 0.5f))
        {
            var t = 1 - Ease.T(distance - distanceThreshold, 1);
            var arrowScreenPos = new Rect(pos, pos).Offset(dir.X * 300, dir.Y * 300).Center; // note(josh): using rects to scale by screen size
            arrowScreenPos = Vector2.Lerp(arrowScreenPos, targetPlayerScreenPos, t);
            var rect = new Rect(arrowScreenPos, arrowScreenPos).Grow(arrowSize);
            var rotation = Math.Atan2(dir.Y, dir.X) * (180.0 / Math.PI);
            UI.Image(rect, Assets.GetAsset<Texture>(red ? "RedArrow.png" : "BlueArrow.png"), new Vector4(1, 1, 1, 0.75f), default, (float)rotation);
        }
        else
        {
            var rect = new Rect(targetPlayerScreenPos, targetPlayerScreenPos).Grow(arrowSize);
            rect = rect.Offset(0, anim * 50);
            UI.Image(rect, Assets.GetAsset<Texture>(red ? "RedArrow.png" : "BlueArrow.png"), new Vector4(1, 1, 1, 0.75f), default, 270);
        }
    }

    public UI.TextSettings GetTextSettings(float size, float offset = 0, FontAsset font = null, UI.HorizontalAlignment halign = UI.HorizontalAlignment.Center)
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
            DropShadowColor = new Vector4(0f, 0f, 0.02f, 0.5f),
            DropShadowOffset = new Vector2(0f, -3f),
            HorizontalAlignment = halign,
            VerticalAlignment = UI.VerticalAlignment.Center,
            WordWrap = false,
            WordWrapOffset = 0,
            Outline = true,
            OutlineThickness = 3,
            Offset = new Vector2(0, offset),
        };
        return ts;
    }

    protected void DashDecay()
    {
        if (DashRemainingDuration > 0) DashRemainingDuration -= Time.DeltaTime;
        Dash = DashRemainingDuration > 0 ? Dash : Vector2.Zero;
    }

    [ClientRpc]
    public void AddDash(Vector2 add, float duration)
    {
        SetFacingDirection(add.X > 0);
        Dash = add;
        DashRemainingDuration = duration;
    }

    public override Vector2 CalculatePlayerVelocity(Vector2 currentVelocity, Vector2 input, float deltaTime)
    {
        var multiplier = 0.7f * CheatSpeedMultiplier;

        /*
        if (Speed Altering effects )
        {
            multiplier *= 1.5f;
        }*/

        Vector2 velocity = DefaultPlayerVelocityCalculation(currentVelocity, input, deltaTime, multiplier);

        velocity += Dash * deltaTime;

        if (Network.IsServer)
        {
            if (IsDead == false && velocity == Vector2.Zero)
            {
                var timeBefore = TimeIdleInSeconds;
                TimeIdleInSeconds += Time.DeltaTime;
                if ((int)timeBefore == 239 && (int)TimeIdleInSeconds == 240)
                {
                    CallClient_ShowNotificationLocal("You will be kicked due to inactivity in 1 minute unless you move.");
                }
            }
            else
            {
                TimeIdleInSeconds = 0;
            }
        }

        return velocity;
    }

    [ClientRpc]
    public void ShakeScreen(float intensity, float duration)
    {
        if (CameraControl != null)
        {
            CameraControl.Shake(intensity, duration);
        }
    }

    public bool TryRemoveItems(Item_Definition defn, int countToRemove)
    {
        var items = DefaultInventory.Items;
        int haveCount = 0;
        for (int i = items.Length - 1; i >= 0; i--)
        {
            var item = items[i];
            if (item == null) continue;
            if (item.Definition == defn)
            {
                haveCount += (int)item.Quantity;
            }
        }

        if (haveCount < countToRemove)
        {
            return false;
        }

        for (int i = items.Length - 1; i >= 0; i--)
        {
            if (countToRemove == 0)
            {
                break;
            }

            var item = items[i];
            if (item == null) continue;
            if (item.Definition == defn)
            {
                if ((int)item.Quantity <= countToRemove)
                {
                    countToRemove -= (int)item.Quantity;
                    Inventory.DestroyItem(item);
                }
                else
                {
                    item.Quantity -= (long)countToRemove;
                    countToRemove = 0;
                    break;
                }
            }
        }

        return true;
    }

    public bool TryRemoveItems(string itemID, int countToRemove)
    {
        var defn = ItemManager.Instance.TryFindItem(itemID);
        if (defn == null) return false;
        return TryRemoveItems(defn, countToRemove);
    }

    [ClientRpc]
    public void SetFlash(Vector4 _flashColor)
    {
        FlashAlpha = 1f;
        FlashColor = _flashColor;
    }

    Ability AssignedMainAbility()
    {
        return null;
    }

    Ability AssignedSecondaryAbility()
    {
        return null;
    }

    public void DrawAbilities()
    {
        if (IsLocal && HideHudReasons.Count == 0 && GameManager.Instance.State == GameState.Round)
        {
            if (PlayerRole == PlayerRole.Role1)
            {
                DrawDefaultAbilityUI(new AbilityDrawOptions()
                {
                    AbilityElementSize = 200,
                    Abilities = new Ability[]{
                        AssignedMainAbility(),
                        AssignedSecondaryAbility(),
                        null,
                        null,
                        null,
                        null,
                    }
                });
            }

            if (PlayerRole == PlayerRole.Role2)
            {
                DrawDefaultAbilityUI(new AbilityDrawOptions()
                {
                    AbilityElementSize = 200,
                    Abilities = new Ability[]{
                        AssignedMainAbility(),
                        AssignedSecondaryAbility(),
                        null,
                        null,
                        null,
                        null,
                    }
                });
            }

            if (PlayerRole == PlayerRole.Spectator)
            {
                DrawDefaultAbilityUI(new AbilityDrawOptions()
                {
                    AbilityElementSize = 200,
                    Abilities = new Ability[]{
                        null,
                        null,
                        null,
                        null,
                        null,
                    }
                });
            }
        }
    }

    public static T TryGetClosestComponent<T>(Vector2 point, float range = float.MaxValue) where T : Component
    {
        T closest = null;
        foreach (var t in Scene.Components<T>())
        {
            var distance = (t.Position - point).Length;
            if (distance < range)
            {
                range = distance;
                closest = t;
            }
        }
        return closest;
    }

    // INVENTORY
    public Item_Instance TryGetItem(string itemId)
    {
        var items = DefaultInventory.Items;
        foreach (Item_Instance item in items)
        {
            if (item != null && item.Definition.Id == itemId)
            {
                return item;
            }
        }
        return null;
    }

    public void ClearInventory()
    {
        var items = DefaultInventory.Items;
        foreach (var item in items)
        {
            if (item != null)
            {
                Inventory.DestroyItem(item);
            }
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        ClearInventory();
        if (IsLocal)
        {
            CameraControl.Destroy();
        }
    }

    public bool AddItemToInventory(string itemId, long count = 1, float durability = 1f)
    {
        if (DefaultInventory == null)
        {
            return false;
        }
        var defn = ItemManager.Instance.TryFindItem(itemId);
        if (defn == null)
        {
            return false;
        }

        var itemsToDrop = new List<Item_Instance>();
        while (count > 0)
        {
            var stackSize = (long)defn.StackSize;
            if (stackSize > count) stackSize = count;
            count -= stackSize;

            var instance = ItemManager.CreateItem(defn, stackSize);

            if (instance.Durability == 1f)
            {
                instance.Durability = durability;
            }

            if (!Inventory.CanMoveItemToInventory(instance, DefaultInventory))
            {
                itemsToDrop.Add(instance);
            }
            else
            {
                Inventory.MoveItemToInventory(instance, DefaultInventory);
            }
        }

        foreach (var item in itemsToDrop)
        {
            ServerDropItems(item);
        }

        return true;
    }

    /// <summary>
    /// A little check for puzzles or anything that needs to check if you have X of a specific item
    /// </summary>
    /// <param name="itemId"></param>
    /// <param name="reqAmount"></param>
    /// <returns></returns>
    public long GetItemAmount(string itemId)
    {
        long total = 0;
        foreach (var item in DefaultInventory.Items)
        {
            if (item != null && item.Definition.Id == itemId)
            {
                total += item.Quantity;
            }
        }
        return total;
    }

    public int InventoryBoxesSpawned;

    public void ServerDropItems(Item_Instance itemToDrop)
    {
        Util.Assert(Network.IsServer);

        ulong rng = RNG.Seed((ulong)Game.FrameNumber);
        var item = Network.InstantiateAndSpawn(References.Instance.ItemDropPrefab, e => { 
            e.Position = Entity.Position + new Vector2(RNG.RangeFloat(ref rng, -0.55f, 0.55f), RNG.RangeFloat(ref rng, -0.55f, 0.55f));
            e.GetComponent<ItemDrop>().CreateItemDrop(itemToDrop.Definition.Id, (int)itemToDrop.Quantity);
            e.GetComponent<ItemDrop>().timer = -0.5f;
            e.GetComponent<ItemDrop>().Durability = itemToDrop.Durability;
        });
    }

    public void ServerResetPlayer(PlayerRole role)
    {
        PlayerRole = role;
        ClearInventory();
        CurrentHealth = 100;

        var playerCorpse = References.Instance.PlayerCorpsePrefab.Instantiate<PlayerCorpse>();
        playerCorpse.Entity.Name = $"{Name}_corpse";
        playerCorpse.Entity.Position = new Vector2(1000, 1000);
        playerCorpse.PlayerName = Name;
        playerCorpse.ColorIndex = ColorIndex;

        Network.Spawn(playerCorpse.Entity);
        playerCorpse.ForPlayer.Set(this.Entity);
        GameManager.Instance.AllPlayerCorpses.Add(playerCorpse.Entity);
        playerCorpse.CallClient_SetSkins(SpineAnimator.SpineInstance.GetSkins());

        PlayerCorpse.Set(playerCorpse.Entity);
        TimeIdleInSeconds = 0;
    }

    public Item_Instance HasItem(string itemID, int amount = 1)
    {
        Item_Instance item = null;
        foreach (var i in DefaultInventory.Items)
        {
            if (i == null) continue;
            if (i.Definition.Id == itemID)
            {
                if (i.Quantity >= amount)
                {
                    item = i;
                    break;
                }
            }
        }
        return item;
    }

    [ClientRpc]
    public void ResetPlayer()
    {
        if (!SpineAnimator.SpineInstance.TrySetToPlayerRig("animations/ctf_player/ctf_player.merged_spine_rig#output", this))
        {
            Util.Assert(false, "Failed to set to player rig.");
        }
    }

    [ClientRpc]
    public void ResetAllPlayerRigs()
    {
        foreach (MyPlayer player in Scene.Components<MyPlayer>())
        {
            if (!player.SpineAnimator.SpineInstance.TrySetToPlayerRig("animations/ctf_player/ctf_player.merged_spine_rig#output", player))
            {
                Log.Warn("Failed to set to player rig.");
            }
        }
    }

    [ServerRpc]
    public void ServerCloseIntro()
    {
        var player = Network.GetRemoteCallContextPlayer();
        if (player == null) return;
        if (player != this) return;

        CallClient_SkipIntro();
    }

    [ServerRpc]
    public void ServerCloseEnd()
    {
        var player = Network.GetRemoteCallContextPlayer();
        if (player == null) return;
        if (player != this) return;

        CallClient_SkipEnd();
    }

    [ClientRpc]
    public void SkipIntro()
    {
        RemoveEffect<EffectRoundStartAnimation>(true);
    }

    [ClientRpc]
    public void SkipEnd()
    {
        RemoveEffect<EffectRoundEnd>(true);
    }

    [ClientRpc]
    public void ShowNotificationLocal(string message)
    {
        if (IsLocal)
        {
            Notifications.Show(message);
        }
    }

    [ClientRpc]
    public void KillPlayer(Player player)
    {
        MyPlayer p = (MyPlayer)player;
        p.ClearAllEffects();
        //p.AddEffect<EffectDeath>();
    }

    [ClientRpc]
    public void PlayShopSFX(Player player)
    {
        if (player.IsLocal)
        {
            SFXE.Play(Assets.GetAsset<AudioAsset>("sfx/PurchaseSFX.wav"), new() { });
        }
    }

    [ServerRpc]
    public void SetIsMobile(bool mobile)
    {
        MyPlayer p = (MyPlayer)Network.GetRemoteCallContextPlayer();
        p.Mobile.Set(mobile);
    }

    public bool DrawCloseButton()
    {
        var exitButton = Assets.GetAsset<Texture>("fail_x.png");
        var exitButtonRect = UI.ScreenRect.TopRightRect().Grow(0, 0, 100, 100).FitAspect(exitButton.Aspect).Offset(-200, -100);
        if (UI.Button(exitButtonRect, "EXIT", new UI.ButtonSettings() { Sprite = exitButton }, new UI.TextSettings()).JustPressed || Input.GetKeyDown(Input.Keycode.KEYCODE_ESCAPE, true))
        {
            return true;
        }
        return false;
    }

    // @Credit: Lookumz
    protected Rect DrawHealthBar()
    {
        using var _1 = UI.PUSH_CONTEXT(UI.Context.WORLD);
        using var _2 = IM.PUSH_Z(GetZOffset() - 0.0001f); // minus an epsilon so the health bar draws over the player
        using var _3 = UI.PUSH_SCALE_FACTOR(5.0f / 540.0f);
        var healthRect = FinalNameRect.TopCenterRect().Offset(0, 5);
        healthRect = healthRect.Grow(13, 70, 0, 70).Offset(0, 0);
        var borderRect = healthRect.Grow(5.5f, 4, 5.5f, 4).Offset(0, -2);
        var levelRect = healthRect.CenterRect().Grow(15, 30, 15, 30).Offset(0, 24);

        var back = PlayerRole == PlayerRole.Role1 ? HealthBarBack_RedTeam : HealthBarBack_BlueTeam;
        var fill = PlayerRole == PlayerRole.Role1 ? HealthBarFill_RedTeam : HealthBarFill_BlueTeam;
        var pip = PlayerRole == PlayerRole.Role1 ? Pip_RedTeam : Pip_BlueTeam;

        // Draw bar background
        UI.Image(borderRect, back, Vector4.White, new UI.NineSlice());

        // Draw health percentage
        var healthPercent = CurrentHealth / (float)DefaultMaxHealth;
        var healthPercentRect = healthRect.SubRect(0, 0, healthPercent, 1, 0, 0, 0, 0);
        UI.Image(healthPercentRect, fill, Vector4.White, new UI.NineSlice());
        DrawPipOnHealthBar(healthRect, CurrentHealth, DefaultMaxHealth, 6, pip, 0.05f, 0.05f, Vector4.White);

        /*
        var ts = new UI.TextSettings()
        {
            Font = UI.Fonts.BarlowBold,
            Size = 28,
            VerticalAlignment = UI.VerticalAlignment.Top,
            HorizontalAlignment = UI.HorizontalAlignment.Center,
            Color = Vector4.White,
            Outline = true,
            OutlineColor = Vector4.Black
        };
        UI.Text(levelRect, "Lv: " + Level.ToString(), ts);
        */

        return healthRect;
    }

    public void DrawPipOnHealthBar(Rect barRect, float currentHealth, float maxHealth, int totalPips, Texture pipTexture, float leftOffset, float rightOffset, Vector4 color, float shrinkFactor = 0.3f)
    {
        float healthPerPip = maxHealth / (float)totalPips;

        float totalOffset = leftOffset + rightOffset;
        float availableWidth = 1.0f - totalOffset;  // Total width available for all pips
        float pipWidth = availableWidth / totalPips; // Width of each pip

        for (int i = 0; i < totalPips; i++)
        {
            // Calculate the threshold for this pip
            float pipThreshold = (i + 1) * healthPerPip;

            float xMin = leftOffset + i * pipWidth; // Start after the left offset for all pips
            float xMax = leftOffset + (i + 1) * pipWidth; // Extend by pipWidth

            // Only draw the pip if the current health is greater than the threshold for this pip
            if (currentHealth >= pipThreshold)
            {
                float newWidth = pipWidth * shrinkFactor;

                // Center the smaller pip rectangle
                float newXMin = xMin + (pipWidth - newWidth) / 2;
                float newXMax = xMax - (pipWidth - newWidth) / 2;

                var smallerPipRect = barRect.SubRect(newXMin, 0, newXMax, 1, 0, 0, 0, 0);
                UI.Image(smallerPipRect, pipTexture, color, new UI.NineSlice());
            }
        }
    }

    protected void DrawDamageNumber()
    {
        using var _1 = UI.PUSH_CONTEXT(UI.Context.WORLD);
        using var _2 = UI.PUSH_LAYER(5);

        List<DamageNumbers> numbers = GameManager.Instance.ActiveDamageNumbers;
        for (int i = numbers.Count - 1; i >= 0; i -= 1)
        {
            var result = numbers[i];
            float speed = 0.5f;
            var ts = result.TextSettings;

            result.T += Time.DeltaTime * speed;
            if (result.T >= 1 && result.DoingFading)
            {
                numbers.UnorderedRemoveAt(i);
                continue;
            }
            if (result.T >= 1 && !result.DoingFading)
            {
                result.T = 0.0f;
                result.DoingFading = true;
            }

            if (!result.DoingFading)
            {
                var pos = result.Position;
                pos.Y += AOMath.Lerp(0, 0.5f, Ease.OutQuart(result.T));               
                var color01 = Ease.FadeInAndOut(0.1f, 1f, result.T);
                ts.Color = Vector4.Lerp(ts.Color, result.Color, color01);
                result.LastPosition = pos;
            }
            else
            {
                ts.SpacingMultiplier = 1f;
                var colorAlpha = Vector4.Zero;
                ts.Color = Vector4.Lerp(ts.Color, colorAlpha, result.T);
            }

            var rect = new Rect(result.LastPosition, result.LastPosition);
            UI.Text(rect, result.Text, ts);
        }
    }

    [ClientRpc]
    public void NotifyPlayer(string notif)
    {
        if (!IsLocal) return;
        Notifications.Show(notif);
    }

    public PlayerCorpse PlaceCorpseAndDropItems()
    {
        var corpse = PlayerCorpse.Value.GetComponent<PlayerCorpse>();
        corpse.Entity.Position = Entity.Position;

        corpse.PlayerAnimator.SpineInstance.StateMachine.SetTrigger("die");
        corpse.DeathAnim = "die";
        SFXE.Play(Assets.GetAsset<AudioAsset>("sfx/0hp_death.wav"), new() { Positional = true, Position = Entity.Position });

        return corpse;
    }

    [ServerRpc]
    public void PleaseDropItem(long itemIndex)
    {
        var items = DefaultInventory.Items;
        if (itemIndex < 0 || itemIndex >= items.Length) return;
        var player = (MyPlayer)Network.GetRemoteCallContextPlayer();
        if (!player.Alive()) return;
        if (player.HasActiveEffect) return;
        var item = items[itemIndex];
        if (item == null) return;
        if (item.Inventory != player.DefaultInventory) return;
        
        ServerDropItems(item);
        TryRemoveItems(item.Definition, (int)item.Quantity);
    }

    public static void DrawTVEffect()
    {
        {
            using var _ = IM.PUSH_MATERIAL(GameManager.Instance.StaticMaterial);
            UI.Image(UI.ScreenRect, UI.WhiteSprite, new Vector4(1, 1, 1, 0.5f));
        }
        UI.Image(UI.ScreenRect, Assets.GetAsset<Texture>("crt_frame.png"));
    }

}

public enum PlayerRole
{
    Spectator = 0,
    Role1,
    Role2
}

#region Effects
public abstract class MyEffect : AEffect
{
    public new MyPlayer Player => (MyPlayer)base.Player;
}

public abstract class AimEffect : MyEffect {}

public class SpectatorEffect : MyEffect
{
    public override bool IsActiveEffect => false;
    public override bool IsValidTarget => false;

    public bool HasNameInvisReason = false;

    public void UpdateInvis()
    {
        if (Player.IsLocal || (Network.LocalPlayer.Alive() && Network.LocalPlayer.HasEffect<SpectatorEffect>()))
        {
            Player.SpineAnimator.SpineInstance.ColorMultiplier = new Vector4(1, 1, 1, 0.5f);
            if (HasNameInvisReason)
            {
                HasNameInvisReason = false;
                Player.RemoveNameInvisibilityReason(nameof(SpectatorEffect));
            }
        }
        else
        {
            Player.SpineAnimator.SpineInstance.ColorMultiplier = new Vector4(1, 1, 1, 0);
            if (!HasNameInvisReason)
            {
                HasNameInvisReason = true;
                Player.AddNameInvisibilityReason(nameof(SpectatorEffect));
            }
        }
    }

    public override void OnEffectStart(bool isDropIn)
    {
       // UpdateInvis();
        Player.SpineAnimator.DepthOffset = -10000;
        Player.SpineAnimator.SpineInstance.StateMachine.SetBool("ghost_form", true);
        Player.Entity.GetComponent<Circle_Collider>().LocalEnabled = false;
        if (!isDropIn)
        {
            Player.AddEmoteBlockReason(nameof(SpectatorEffect));
        }

        if (Player.IsLocal && GameManager.Instance.VoiceChatEnabled)
        {
            Game.SetVoiceEnabled(false);
        }
    }

    public override void OnEffectUpdate()
    {
        //UpdateInvis();
    }

    public override void OnEffectEnd(bool interrupt)
    {
        Player.RemoveEmoteBlockReason(nameof(SpectatorEffect));
        Player.SpineAnimator.DepthOffset = 0;
        Player.SpineAnimator.SpineInstance.StateMachine.SetBool("ghost_form", false);
        Player.SpineAnimator.SpineInstance.ColorMultiplier = new Vector4(1, 1, 1, 1);
        if (HasNameInvisReason)
        {
            Player.RemoveNameInvisibilityReason(nameof(SpectatorEffect));
        }
        Player.Entity.GetComponent<Circle_Collider>().LocalEnabled = true;

        if (Player.IsLocal && GameManager.Instance.VoiceChatEnabled)
        {
            Game.SetVoiceEnabled(true);
        }
        Player.SpineAnimator.SpineInstance.ColorMultiplier = new Vector4(1, 1, 1, 1);
    }
}

public class NoClipEffect : MyEffect
{
    public override bool IsActiveEffect => false;
    public override bool IsValidTarget => false;

    public override void OnEffectStart(bool isDropIn)
    {
        Player.Entity.GetComponent<Circle_Collider>().LocalEnabled = false;
    }

    public override void OnEffectUpdate()
    {
    }

    public override void OnEffectEnd(bool interrupt)
    {
        Player.Entity.GetComponent<Circle_Collider>().LocalEnabled = true;
    }
}

public class EffectRoundStartAnimation : MyEffect
{
    public override bool IsActiveEffect => true;
    public override bool FreezePlayer => true;

    public float Hold01;

    public override void OnEffectStart(bool isDropIn)
    {      

    }

    public override void OnEffectEnd(bool interrupt)
    {
    }

    public override void OnEffectUpdate()
    {
        const float TotalTime = 10f;

        Hold01 += Time.DeltaTime / TotalTime;
        Hold01 = (float)Math.Clamp(Hold01, 0, 1);

        if (Player.IsLocal)
        {
            var bgTint01 = Ease.FadeInAndOut(0.1f, 1.0f, Hold01);

            using var _1 = UI.PUSH_LAYER(GameManager.PuzzleLayer);
            using var _2 = UI.PUSH_COLOR_MULTIPLIER(new Vector4(1, 1, 1, bgTint01));

            UI.Image(UI.ScreenRect, null, new Vector4(0, 0, 0, 0.9f));

            var pos01 = Ease.SlideInAndOut(0.1f, 1.0f, Hold01);
            var ts = Player.GetTextSettings(52);
            ts.Color = Vector4.White;
            ts.WordWrap = true;
            var rect = UI.SafeRect.Offset(pos01 * 100, 0);
            switch (Player.PlayerRole)
            {
                case PlayerRole.Role1:
                    {
                        var actualRect = UI.Text(rect, "You're assigned role 1!", ts);
                        ts.Size = 64;
                        ts.Color = new Vector4(1, 0, 0, 1);
                        UI.Text(actualRect.TopRect().Grow(100, 500, 0, 500), "You are Role 1.\n\n", ts);
                        break;
                    }
                case PlayerRole.Role2:
                    {
                        var actualRect = UI.Text(rect, "You're assigned role 2!", ts);
                        ts.Size = 64;
                        ts.Color = new Vector4(0, 0, 1, 1);
                        UI.Text(actualRect.TopRect().Grow(100, 500, 0, 500), "You are Role 2.\n\n", ts);
                        break;
                    }                  
            }

            var emptyButtonSettings = new UI.ButtonSettings() { ColorMultiplier = Vector4.Zero };
            UI.Blocker(UI.ScreenRect, "intro blocker"); // to block drag and drop behind the intro
            if (UI.Button(UI.ScreenRect, "CLOSE", emptyButtonSettings, new UI.TextSettings()).Pressed)
            {
                Hold01 += Time.DeltaTime;
                Hold01 = (float)Math.Clamp(Hold01, 0, 1);
            }

            ts.Color = new Vector4(1, 1, 1, 1);
            var holdRectBg = UI.SafeRect.BottomCenterRect().Offset(0, 200).Grow(10, 150, 10, 150);
            var holdRect = holdRectBg.Inset(2, 2, 2, 2).SubRect(Hold01, 0, 1, 1);
            UI.Image(holdRectBg, null, new Vector4(0.8f, 0.8f, 0.8f, 1));
            UI.Image(holdRect, null, new Vector4(0.1f, 0.1f, 0.1f, 1));
            ts.Size = 28;
            var str = "Click and hold to close";
            if (Game.IsMobile)
            {
                str = "Tap and hold to close";
            }
            UI.Text(holdRectBg.Offset(0, 35), str, ts);
        }

        if (Hold01 >= 1)
        {
            if (Player.IsLocal)
            {
                Player.CallServer_ServerCloseIntro();
            }
            Player.RemoveEffect(this, false);
        }
    }
}

public class EffectRoundEnd : MyEffect
{
    public override bool IsActiveEffect => true;
    public override bool FreezePlayer => true;

    public float Hold01;

    public override void OnEffectStart(bool isDropIn)
    {
        if (Network.IsServer)
        {
           
        }
    }

    public override void OnEffectEnd(bool interrupt)
    {
        if (Network.IsServer)
        {
            
        }
    }

    public override void OnEffectUpdate()
    {
        const float TotalTime = 10f;

        Hold01 += Time.DeltaTime / TotalTime;
        Hold01 = (float)Math.Clamp(Hold01, 0, 1);

        if (Player.IsLocal)
        {
            var bgTint01 = Ease.FadeInAndOut(0.1f, 1.0f, Hold01);

            using var _1 = UI.PUSH_LAYER(GameManager.PuzzleLayer);
            using var _2 = UI.PUSH_COLOR_MULTIPLIER(new Vector4(1, 1, 1, bgTint01));

            UI.Image(UI.ScreenRect, null, new Vector4(0, 0, 0, 0.9f));

            var pos01 = Ease.SlideInAndOut(0.1f, 1.0f, Hold01);
            var ts = Player.GetTextSettings(52);
            ts.Color = Vector4.White;
            ts.WordWrap = true;

            var emptyButtonSettings = new UI.ButtonSettings() { ColorMultiplier = Vector4.Zero };
            UI.Blocker(UI.ScreenRect, "intro blocker"); // to block drag and drop behind the intro
            if (UI.Button(UI.ScreenRect, "CLOSE", emptyButtonSettings, new UI.TextSettings()).Pressed)
            {
                Hold01 += Time.DeltaTime;
                Hold01 = (float)Math.Clamp(Hold01, 0, 1);
            }

            ts.Color = new Vector4(1, 1, 1, 1);
            var holdRectBg = UI.SafeRect.BottomCenterRect().Offset(0, 250).Grow(10, 150, 10, 150);
            var holdRect = holdRectBg.Inset(2, 2, 2, 2).SubRect(Hold01, 0, 1, 1);
            UI.Image(holdRectBg, null, new Vector4(0.8f, 0.8f, 0.8f, 1));
            UI.Image(holdRect, null, new Vector4(0.1f, 0.1f, 0.1f, 1));
            ts.Size = 28;
            var str = "Click and hold to close";
            if (Game.IsMobile)
            {
                str = "Tap and hold to close";
            }
            UI.Text(holdRectBg.Offset(0, 35), str, ts);
        }

        if (Hold01 >= 1)
        {
            if (Player.IsLocal)
            {
                Player.CallServer_ServerCloseEnd();
            }
            Player.RemoveEffect(this, false);
        }
    }
}

public class WaitForAnimEffect : MyEffect
{
    public override bool IsActiveEffect => true;
    public override bool FreezePlayer => true;

    public override void OnEffectStart(bool isDropIn)
    {
        if (!isDropIn)
        {
            DurationRemaining = Player.SpineAnimator.SpineInstance.StateMachine.TryGetLayerByIndex(0).GetCurrentStateLength();
        }
    }

    public override void OnEffectEnd(bool interrupt)
    {
    }

    public override void OnEffectUpdate()
    {
    }
}

public abstract class MyAbility : Ability
{
    public new MyPlayer Player => (MyPlayer)base.Player;

    public override bool CanTarget(Player p)
    {
        var player = (MyPlayer)p;
        if (player.PlayerRole == PlayerRole.Spectator)
        {
            return false;
        }
        return true;
    }

    public override bool CanUse()
    {
        if (GameManager.Instance.GlobalAbilityBlocker)
        {
            return false;
        }
        if (GameManager.Instance.State != GameState.Round)
        {
            return false;
        }
        if (Player.PlayerRole == PlayerRole.Spectator)
        {
            return false;
        }
        return true;
    }
}


#endregion
