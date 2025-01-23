using AO;

public partial class Cutscenes : Component
{
    public static Cutscenes _instance;
    public static Cutscenes Instance
    {
        get
        {
            if (_instance.Alive() == false)
            {
                foreach (var c in Scene.Components<Cutscenes>())
                {
                    _instance = c;
                    _instance.Awaken();
                    break;
                }
            }
            return _instance;
        }
    }

    [Serialized] public Entity CutsceneLocations;

    public enum CutsceneType
    {
        None,
        Role1Wins,
        Role2Wins,
        Draw
    }
    public CutsceneType CurrentCutscene
    {
        get => (CutsceneType)CurrentCutsceneIndex;
        set => CurrentCutsceneIndex = (int)value;
    }

    public CameraControl CameraControl;

    public override void Awake()
    {
        CameraControl = CameraControl.Create(-1);
        CameraControl.AmbientColour = new Vector3(0, 0, 0);
    }

    public void Reset()
    {
        CurrentCutscene = CutsceneType.None;
        EndCutsceneCommon();
    }

    public override void Update()
    {
        if (CurrentCutscene != CutsceneType.None)
        {
            CutsceneData.Timer += Time.DeltaTime;
        }

        if (FadeOutTimer > 0f)
        {
            FadeOutTimer -= Time.DeltaTime;
            UI.Image(UI.ScreenRect, UI.WhiteSprite, Vector4.Black * FadeOutTimer);
        }

        switch (CurrentCutscene)
        {
            case CutsceneType.Role1Wins: UpdateRole1Wins(); break;
            case CutsceneType.Role2Wins: UpdateRole2Wins(); break;
            case CutsceneType.Draw: UpdateDraw(); break;
        }
    }

    public class CurrentCutsceneData
    {
        public bool Setup;
        public bool Ended;
        public float Timer;
    }

    public CurrentCutsceneData CutsceneData;

    public int CurrentCutsceneIndex;

    public float FadeOutTimer;

    public List<Entity> RedEntities = new List<Entity>();
    public List<Entity> BlueEntities = new List<Entity>();

    public Dictionary<string, bool> OneTimeBools = new Dictionary<string, bool>();
    public bool OneTime(bool condition, string id)
    {
        if (!condition) return false;
        if (OneTimeBools.ContainsKey(id)) return false;
        OneTimeBools[id] = true;
        return true;
    }


    public (Entity, Spine_Animator) SpawnPlayerEntity(Player player, Vector2 pos)
    {
        var playerEntity = Entity.Create();
        playerEntity.Name = $"{player.Name}_corpse";
        playerEntity.Position = pos;
        playerEntity.LocalScaleX = Math.Sign(player.Entity.LocalScaleX) * Math.Abs(playerEntity.LocalScaleX);
        var spineAnimator = playerEntity.AddComponent<Spine_Animator>();

        var playerSkins = player.SpineAnimator.SpineInstance.GetSkins();
        spineAnimator.SetCrewchsia(player.ColorIndex);
        spineAnimator.SpineInstance.TrySetToPlayerRig("animations/ctf_player/ctf_player.merged_spine_rig#output", player);
        if (Network.IsServer)
        {
            player.Teleport(pos);
        }
        return (playerEntity, spineAnimator);
    }

    public void UpdateFadeInAndOut(float fadeIn, float fadeOut)
    {
        var firstFade = Ease.FadeInAndOut(1, 3, CutsceneData.Timer - fadeIn);
        var secondFade = Ease.FadeInAndOut(1, 5, CutsceneData.Timer - fadeOut);
        var fadeAlpha = MathF.Max(firstFade, secondFade);
        using var _ = UI.PUSH_LAYER(GameManager.PuzzleLayer);
        UI.Image(UI.ScreenRect, UI.WhiteSprite, Vector4.Black * fadeAlpha);
    }

    public void UpdateRole1Wins()
    {
        UpdateFadeInAndOut(2f, 13f);

        if (Util.OneTime(CutsceneData.Timer >= 4, ref CutsceneData.Setup))
        {
            SetupCutsceneCommon();
            var cutsceneLocations = Entity.TryGetChildByName("CutsceneLocations");
            var redSpawns = cutsceneLocations.TryGetChildByName("RedSpawns");
            var blueSpawns = cutsceneLocations.TryGetChildByName("BlueSpawns");

            var availableRedSpawns = new List<Vector2>();
            var availableBlueSpawns = new List<Vector2>();

            foreach (var c in redSpawns.Children) availableRedSpawns.Add(c.Position);
            foreach (var c in blueSpawns.Children) availableBlueSpawns.Add(c.Position);

            List<MyPlayer> AllPlayers = Scene.Components<MyPlayer>().ToList();
            for (int i = 0; i < AllPlayers.Count; i++)
            {
                var player = AllPlayers[i];
                if (player.PlayerRoleAtEndOfRound == PlayerRole.Role1)
                {
                    var pos = availableRedSpawns[0];
                    availableRedSpawns.RemoveAt(0);

                    var (redEntity, redAnimator) = SpawnPlayerEntity(player, pos);
                    redAnimator.SpineInstance.SetAnimation("Grow_Big", true);
                    RedEntities.Add(redEntity);
                    if (player.IsLocal)
                    {
                        SFXE.Play(Assets.GetAsset<AudioAsset>("sfx/win_screen_jingle.wav"), new SFX.PlaySoundDesc() { Volume = 0.35f });
                    }
                }
                else if (player.PlayerRoleAtEndOfRound == PlayerRole.Role2)
                {
                    var pos = availableBlueSpawns[0];
                    availableBlueSpawns.RemoveAt(0);

                    var (blueEntity, blueAnimator) = SpawnPlayerEntity(player, pos);
                    blueAnimator.SpineInstance.SetAnimation("Idle_Drowsy", true);
                    BlueEntities.Add(blueEntity);
                    if (player.IsLocal)
                    {
                        // TODO defeat
                        SFXE.Play(Assets.GetAsset<AudioAsset>("sfx/win_screen_jingle.wav"), new SFX.PlaySoundDesc() { Volume = 0.35f });
                    }
                }
            }
        }

        if (Util.OneTime(CutsceneData.Timer > 15f, ref CutsceneData.Ended))
        {
            EndCutsceneCommon();
        }
    }

    public void UpdateRole2Wins()
    {
        UpdateFadeInAndOut(2f, 13f);

        if (Util.OneTime(CutsceneData.Timer >= 4, ref CutsceneData.Setup))
        {
            SetupCutsceneCommon();
            var cutsceneLocations = Entity.TryGetChildByName("CutsceneLocations");
            var redSpawns = cutsceneLocations.TryGetChildByName("RedSpawns");
            var blueSpawns = cutsceneLocations.TryGetChildByName("BlueSpawns");

            var availableRedSpawns = new List<Vector2>();
            var availableBlueSpawns = new List<Vector2>();

            foreach (var c in redSpawns.Children) availableRedSpawns.Add(c.Position);
            foreach (var c in blueSpawns.Children) availableBlueSpawns.Add(c.Position);

            List<MyPlayer> AllPlayers = Scene.Components<MyPlayer>().ToList();
            for (int i = 0; i < AllPlayers.Count; i++)
            {
                var player = AllPlayers[i];
                if (player.PlayerRoleAtEndOfRound == PlayerRole.Role1)
                {
                    var pos = availableRedSpawns[0];
                    availableRedSpawns.RemoveAt(0);

                    var (redEntity, redAnimator) = SpawnPlayerEntity(player, pos);
                    redAnimator.SpineInstance.SetAnimation("Idle_Drowsy", true);
                    RedEntities.Add(redEntity);
                    if (player.IsLocal)
                    {
                        // TODO defeat
                        SFXE.Play(Assets.GetAsset<AudioAsset>("sfx/win_screen_jingle.wav"), new SFX.PlaySoundDesc() { Volume = 0.35f });
                    }
                }
                else if (player.PlayerRoleAtEndOfRound == PlayerRole.Role2)
                {
                    var pos = availableBlueSpawns[0];
                    availableBlueSpawns.RemoveAt(0);

                    var (blueEntity, blueAnimator) = SpawnPlayerEntity(player, pos);
                    blueAnimator.SpineInstance.SetAnimation("Grow_Big", true);
                    BlueEntities.Add(blueEntity);
                    if (player.IsLocal)
                    {
                        SFXE.Play(Assets.GetAsset<AudioAsset>("sfx/win_screen_jingle.wav"), new SFX.PlaySoundDesc() { Volume = 0.35f });
                    }
                }
            }
        }

        if (Util.OneTime(CutsceneData.Timer > 15f, ref CutsceneData.Ended))
        {
            EndCutsceneCommon();
        }
    }

    public void UpdateDraw()
    {
        UpdateFadeInAndOut(2f, 13f);

        if (Util.OneTime(CutsceneData.Timer >= 4, ref CutsceneData.Setup))
        {
            SetupCutsceneCommon();
            var cutsceneLocations = Entity.TryGetChildByName("CutsceneLocations");
            var redSpawns = cutsceneLocations.TryGetChildByName("RedSpawns");
            var blueSpawns = cutsceneLocations.TryGetChildByName("BlueSpawns");

            var availableRedSpawns = new List<Vector2>();
            var availableBlueSpawns = new List<Vector2>();

            foreach (var c in redSpawns.Children) availableRedSpawns.Add(c.Position);
            foreach (var c in blueSpawns.Children) availableBlueSpawns.Add(c.Position);

            List<MyPlayer> AllPlayers = Scene.Components<MyPlayer>().ToList();
            for (int i = 0; i < AllPlayers.Count; i++)
            {
                var player = AllPlayers[i];
                if (player.PlayerRoleAtEndOfRound == PlayerRole.Role1)
                {
                    var pos = availableRedSpawns[0];
                    availableRedSpawns.RemoveAt(0);

                    var (redEntity, redAnimator) = SpawnPlayerEntity(player, pos);
                    redAnimator.SpineInstance.SetAnimation("MURD_002/Idle_Drowsy", true);
                    RedEntities.Add(redEntity);
                    if (player.IsLocal)
                    {
                        // TODO defeat
                        SFXE.Play(Assets.GetAsset<AudioAsset>("sfx/win_screen_jingle.wav"), new SFX.PlaySoundDesc() { Volume = 0.35f });
                    }
                }
                else if (player.PlayerRoleAtEndOfRound == PlayerRole.Role2)
                {
                    var pos = availableBlueSpawns[0];
                    availableBlueSpawns.RemoveAt(0);

                    var (blueEntity, blueAnimator) = SpawnPlayerEntity(player, pos);
                    blueAnimator.SpineInstance.SetAnimation("MURD_002/Idle_Drowsy", true);
                    BlueEntities.Add(blueEntity);
                    if (player.IsLocal)
                    {
                        SFXE.Play(Assets.GetAsset<AudioAsset>("sfx/win_screen_jingle.wav"), new SFX.PlaySoundDesc() { Volume = 0.35f });
                    }
                }
            }
        }

        if (Util.OneTime(CutsceneData.Timer > 15f, ref CutsceneData.Ended))
        {
            EndCutsceneCommon();
        }
    }

    public void SetupCutsceneCommon()
    {
        CameraControl.Position = CutsceneLocations.Position;
        CameraControl.Zoom = 1f;
        if (Network.IsClient) CameraControl.ControlLevel = 100;
        foreach (var c in Scene.Components<PlayerCorpse>(false)) c.PlayerAnimator.LocalEnabled = false;

        List<MyPlayer> AllPlayers = Scene.Components<MyPlayer>().ToList();
        foreach (var p in AllPlayers)
        {
            var player = (MyPlayer)p;
            player.ClearAllEffects();
            player.AddEffect<WatchingCutsceneEffect>(preInit: e =>
            {
                e.DoSFX = true;
                e.Invis = true;
            });
        }

        if (Network.IsServer)
        {
            //GameManager.Instance.ServerDestroyAllPlayerCorpses();
        }
    }

    public void StartCutsceneCommon()
    {
        CutsceneData = new CurrentCutsceneData();
    }

    public void EndCutsceneCommon()
    {
        if (Network.IsClient) CameraControl.ControlLevel = -1;

        FadeOutTimer = 1f;
        CurrentCutscene = CutsceneType.None;
        OneTimeBools.Clear();
        List<MyPlayer> AllPlayers = Scene.Components<MyPlayer>().ToList();
        foreach (var player in AllPlayers)
        {
            player.RemoveEffect<WatchingCutsceneEffect>(false);
        }

        foreach (var entity in RedEntities) entity.Destroy();
        foreach (var entity in BlueEntities) entity.Destroy();
        RedEntities.Clear();
        BlueEntities.Clear();
    }

    [ClientRpc]
    public void PlayRole1Win()
    {
        CurrentCutscene = CutsceneType.Role1Wins;
        StartCutsceneCommon();
    }

    [ClientRpc]
    public void PlayRole2Win()
    {
        CurrentCutscene = CutsceneType.Role2Wins;
        StartCutsceneCommon();
    }

    [ClientRpc]
    public void PlayDraw()
    {
        CurrentCutscene = CutsceneType.Draw;
        StartCutsceneCommon();
    }
}

public class WatchingCutsceneEffect : MyEffect
{
    public override bool IsActiveEffect => true;
    public override bool FreezePlayer => true;

    public bool StartedSFX;
    public ulong WinSFX;
    public ulong LoseSFX;

    public bool Invis;
    public bool DoSFX;

    public override void OnEffectStart(bool isDropIn)
    {
        if (!isDropIn)
        {
            if (Invis)
            {
                Player.AddInvisibilityReason(nameof(WatchingCutsceneEffect));
            }
        }
        Player.HideHudReasons.Add(nameof(WatchingCutsceneEffect)); // todo(josh): NetworkSerialize HideHudReasons for MyPlayer
    }

    public override void OnEffectEnd(bool interrupt)
    {
        if (Invis)
        {
            Player.RemoveInvisibilityReason(nameof(WatchingCutsceneEffect));
        }
        Player.HideHudReasons.Remove(nameof(WatchingCutsceneEffect));
        SFX.FadeOutAndStop(WinSFX, 0.5f);
        SFX.FadeOutAndStop(LoseSFX, 0.5f);
    }

    public override void OnEffectUpdate()
    {
        if (DoSFX && StartedSFX == false)
        {
            StartedSFX = true;
            if (GameManager.Instance.State == GameState.Draw)
            {
                // TODO draw sfx
                WinSFX = SFXE.Play(Assets.GetAsset<AudioAsset>("sfx/victory_loop.wav"), new SFX.PlaySoundDesc() { Volume = 0.1f });
            }
            else
            {
                var isWinner = (Player.PlayerRoleAtEndOfRound == PlayerRole.Role1 && GameManager.Instance.State == GameState.Role1Wins) ||
                    (Player.PlayerRoleAtEndOfRound == PlayerRole.Role2 && GameManager.Instance.State == GameState.Role2Wins);
                WinSFX = SFXE.Play(Assets.GetAsset<AudioAsset>("sfx/victory_loop.wav"), new SFX.PlaySoundDesc() { Volume = isWinner ? 0f : 0.1f });
                LoseSFX = SFXE.Play(Assets.GetAsset<AudioAsset>("sfx/defeat_loop2.wav"), new SFX.PlaySoundDesc() { Loop = true, Volume = isWinner ? 0.1f : 0f });
            }
        }
    }
}