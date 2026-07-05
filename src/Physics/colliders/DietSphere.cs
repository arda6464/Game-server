namespace DietPhysics
{
    public enum DietObjectType
    {
        None,
        Player,
        Wall
    }
    public class DietSphere : ICollider
    {
        private Vec3 position;
        private Vec3 center;
        private float radius;
        public DietObjectType Type { get; private set; }
        public int TypeData { get; private set; }

        public DietSphere(Vec3 pos, Vec3 center, float radius, DietObjectType type, int typedata)
        {
            this.Type = type;
            this.TypeData = typedata;
            this.position = pos;
            this.center = center;
            this.radius = radius;
        }
        public DietSphere(Vec3 pos, Vec3 center, float radius) : this(pos, center, radius, DietObjectType.None, 0)
        {
        }
        public Vec3 Position
        {
            get { return position; }
            set { position = value; }
        }
        public float Radius
        {
            get { return radius; }
            set { radius = value; }
        }
        public Vec3 Center
        {
            get { return center; }
            set { center = value; }
        }
        public Vec3 GetPosition()
        {
            return this.position;
        }
    }
}
