using AO;

public class PlayerFollower : Component
{
    public Entity Following;

    public override void LateUpdate()
    {
        if (Following.Alive())
        {
            Entity.Position = Following.Position;
        }
    }
}
