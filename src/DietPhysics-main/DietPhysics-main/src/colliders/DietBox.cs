namespace DietPhysics
{
    public class DietBox : ICollider
    {
        private Vec3 position;
        private Vec3 center;
        private Vec3 size;
        private Vec3 rotation;
        public DietBox(Vec3 pos, Vec3 center, Vec3 size, Vec3 rotation)
        {
            this.position = pos;
            this.center = center;
            this.size = size;
            this.rotation = rotation;
        }
        public Vec3 GetPosition()
        {
            return this.position;
        }
        public Vec3 Position
        {
            get { return position; }
            set { position = value; }
        }
        public Vec3 Center
        {
            get { return center; }
            set { center = value; }
        }
        public Vec3 Size
        {
            get { return size; }
            set { size = value; }
        }
        public Vec3 Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }
    }
}