using AO;

public class References : Component
{
    public Vector4 winGreen = new Vector4(0.51f, 0.85f, 0f, 1f);
    public Vector4 loseRed = new Vector4(0.84f, 0.18f, 0f, 1f);

    [Serialized] public BillboardSign TutorialSign;

    [Serialized] public Texture Logo;

    [Serialized] public Texture GenericButton;
    [Serialized] public Texture GenericButtonHover;
    [Serialized] public Texture GenericButtonPress;

    [Serialized] public Texture ProgressBarBig;
    [Serialized] public Texture ProgressBarSmall;

    [Serialized] public Prefab PlayerCorpsePrefab;

    [Serialized] public Prefab ItemDropPrefab;

    [Serialized] public Prefab BearTrapPrefab;

    [Serialized] public Entity Role1Spawn;
    [Serialized] public Entity Role2Spawn;


    private static References _instance;
    public static References Instance
    {
        get
        {
            if (_instance.Alive()) return _instance;
            foreach (var c in Scene.Components<References>(false))
            {
                _instance = c;
                break;
            }
            return _instance;
        }
    }

    public override void Awake()
    {

    }
}
