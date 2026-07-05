namespace DietPhysics
{
    public interface ICollider
    {
        public Vec3 GetPosition();
        public DietObjectType Type { get; }
        public int TypeData { get; }
    }
}
