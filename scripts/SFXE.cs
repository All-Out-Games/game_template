using static AO.SFX;

namespace AO;

public static class SFXE
{
    public static ulong Play(AudioAsset asset, PlaySoundDesc desc)
    {
        var descNew = desc;
        desc.Volume = GameManager.Instance.GlobalSFXVolumeOverride * desc.Volume;
        return SFX.Play(asset, desc);
    }
}