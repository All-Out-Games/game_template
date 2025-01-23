using AO;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class BillboardSign : Component
{
    [Serialized] public string Message;
    [Serialized] public float HalfWidth = 0.5f;

    public float NearSinceTime;

    public override void Update()
    {
        if (Network.IsServer) return;
        if (Network.LocalPlayer == null) return;

        var localPlayer = (MyPlayer)Network.LocalPlayer;
        if ((localPlayer.Entity.Position - Entity.Position).Length > 2)
        {
            NearSinceTime = Time.TimeSinceStartup;
            if (localPlayer.NearSign == this)
            {
                localPlayer.NearSign = null;
            }
        }
        else
        {
            localPlayer.NearSign = this;
            float jiggle = Util.Jitter(Ease.T(Time.TimeSinceStartup - NearSinceTime, 0.5f), 4);

            UI.PushContext(UI.Context.WORLD); using var _1 = AllOut.Defer(UI.PopContext);
            UI.PushScaleFactor(1.5f); using var _2 = AllOut.Defer(UI.PopScaleFactor);
            UI.PushLayerRelative(2); using var _3 = AllOut.Defer(UI.PopLayer);

            var pos = Entity.Position + new Vector2(jiggle * 0.1f, 1.0f);
            var adjustedHalfWidth = Game.IsMobile ? HalfWidth * 3 : HalfWidth * 2;
            var rect = new Rect(pos, pos).CenterRect().OffsetUnscaled(0, 0.3f).Grow(0, adjustedHalfWidth, 0, adjustedHalfWidth);

            int bgSerial = IM.GetNextSerial();
            var actualTextRect = UI.Text(rect, Message, new UI.TextSettings()
            {
                Font = UI.Fonts.Barlow,
                Size = Game.IsMobile ? 0.3f : 0.2f,
                Color = Vector4.White,
                HorizontalAlignment = UI.HorizontalAlignment.Center,
                VerticalAlignment = UI.VerticalAlignment.Center,
                Outline = true,
                OutlineThickness = 3,
                WordWrap = true,
            });

            IM.SetNextSerial(bgSerial);
            var slice = new UI.NineSlice() { slice = new Vector4(34, 34, 34, 34), sliceScale = 0.5f };
            slice.sliceScale *= 4;
            UI.Image(actualTextRect.Grow(0.15f), Assets.GetAsset<Texture>("modal-9slice.png"), Vector4.White, slice);
        }
    }
}