using LiteEntitySystem;
using DietPhysics;

public class LootEntity : EntityLogic
{
    public SyncVar<int> ItemId;
    public SyncVar<byte> ItemType;
    public SyncVar<Vec3> Position;

    public LootEntity(EntityParams entityParams) : base(entityParams) { }

    public void Init(Vec3 position, byte itemType, int itemId)
    {
        Position.Value = position;
        ItemType.Value = itemType;
        ItemId.Value = itemId;
    }
}
