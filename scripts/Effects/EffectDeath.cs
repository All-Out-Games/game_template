using AO;

public class EffectDeath : MyEffect
{
    public override bool IsActiveEffect => true;
    public override bool FreezePlayer => false;
    public override bool IsValidTarget => false;
    public override float DefaultDuration => 10f;
    public override bool BlockAbilityActivation => true;

    public override void OnEffectStart(bool isDropIn)
    {
        if (!isDropIn)
        {
            Player.PlaceCorpseAndDropItems();
            if (!Player.IsLocal) Player.AddInvisibilityReason(nameof(EffectDeath));
            else
            {
                Player.SpineAnimator.SpineInstance.ColorMultiplier = new Vector4(1, 1, 1, 0.5f);
            }
            Player.AddNameInvisibilityReason(nameof(EffectDeath));
        }    

        Player.AddEffect<SpectatorEffect>();

        if (Network.IsServer)
        {
            Player.CurrentHealth = 100;
        }
    }

    public override void OnEffectEnd(bool interrupt)
    {
        Player.RemoveInvisibilityReason(nameof(EffectDeath));
        Player.RemoveNameInvisibilityReason(nameof(EffectDeath));
        Player.RemoveEffect<SpectatorEffect>(true);
        // TP them to spawn
        Player.Teleport(Player.PlayerRole == PlayerRole.Role1 ? References.Instance.Role1Spawn.Position : References.Instance.Role2Spawn.Position);
        // remove corpse
        if (Network.IsServer)
        {
            Network.Despawn(Player.PlayerCorpse.Value);
            Player.PlayerCorpse.Value.Destroy();
        }

        Player.SpineAnimator.SpineInstance.ColorMultiplier = new Vector4(1, 1, 1, 1);
    }

    public override void OnEffectUpdate()
    {
        if (Player.IsLocal)
        {
            using var _ = UI.PUSH_LAYER(200);

            var rtRect = UI.ScreenRect.CutBottom(350).Offset(0, 0);

            var rts = GameManager.Instance.GetTextSettingsColor(40, new Vector4(1, 1, 1, 1), 0f, null);
            UI.Text(rtRect, "Respawn in: " + ((int)DurationRemaining).ToString(), rts);

        }
    }
}