using AO;
using System.Buffers;

public partial class ProjectileExample : Component
{
    public float Lifetime;
    public const float MaxLife = 1.5f;
    public bool AlreadyHitSomething;

    public override void Awake()
    {
        GetComponent<Projectile>().OnHit += OnHit;
    }

    public override void Update()
    {
        Lifetime += Time.DeltaTime;
        if (Lifetime > MaxLife)
        {
            this.Entity.Destroy();
        }
    }

    private void OnHit(Entity other, bool predicted)
    {
        if (AlreadyHitSomething) return;
        if (other.GetComponent<ProjectileIgnore>() != null) return;

        MyPlayer player = null;
        var projectile = Entity.GetComponent<Projectile>();

        var collisionChild = other.GetComponent<PlayerCollisionChild>();
        if (collisionChild != null)
        {
            player = collisionChild.Player;
        }

        var potentialPlayer = other.GetComponent<MyPlayer>();
        if (potentialPlayer != null && player == null && potentialPlayer != projectile.Owner)
        {
            player = potentialPlayer;
        }

        if (player != null)
        {
            if (player.Alive() == false) return;
            if (player.IsDead) return;
            if (player.HasEffect<SpectatorEffect>()) return;
            if (!player.IsValidTarget) return;
        }

        if (player != null && player == projectile.Owner) return;
        if (other.Name == projectile.Owner.Name) return;
        if (other.Name == Entity.Name) return;
        if (player != null && player.PlayerRole == ((MyPlayer)projectile.Owner).PlayerRole) return;

        // HIT CONFIRMED
        AlreadyHitSomething = true;

        // Spawn in a hit effect if you'd like...
        //var fx = References.Instance.BulletHitPrefab.Instantiate();
        //fx.Position = Position;
       
        SFXE.Play(Assets.GetAsset<AudioAsset>("sfx/RifleHit.wav"), new() { Positional = true, Position = Entity.Position });

        if (predicted == false && player != null)
        {
            if (Network.IsServer)
            {
                //player.CallClient_CAddHitEffect((Player)player, 35, projectile.Owner);
            }
        }

        Entity.Destroy();
    }
}
