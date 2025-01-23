using AO;

public static partial class BBUtil
{
    public static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDistanceDelta)
    {
        Vector2 a = target - current;
        float magnitude = MathF.Sqrt((a.X * a.X) + (a.Y * a.Y));
        if (magnitude <= maxDistanceDelta || magnitude == 0f)
        {
            return target;
        }
        return current + a / magnitude * maxDistanceDelta;
    }
}
