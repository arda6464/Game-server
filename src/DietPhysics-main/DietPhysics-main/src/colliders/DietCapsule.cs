namespace DietPhysics
{
    public class DietCapsule : ICollider
    {
        private Vec3 position;
        private Vec3 center;
        private float radius;
        private float height;
        private Vec3 segmentStart;
        private Vec3 segmentEnd;
        public Vec3 Axis => segmentEnd - segmentStart;
        public DietCapsule(Vec3 pos, Vec3 center, float radius, float height)
        {
            this.position = pos;
            this.center = center;
            this.radius = radius;
            this.height = height;

            //Vec3 halfHeight = new Vec3(0, (height - 2 * radius) / 2, 0);
            //segmentStart = center - halfHeight;
            //segmentEnd = center + halfHeight;

            Vec3 capsuleWorldCenter = pos + center; // (0,1,3) + (0,0,0) = (0,1,3)
            Vec3 halfHeight = new Vec3(0, (height - 2 * radius) / 2, 0); // (0,0.5,0)
            segmentStart = capsuleWorldCenter - halfHeight; // (0,0.5,3)
            segmentEnd = capsuleWorldCenter + halfHeight; // (0,1.5,3)

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
        public float Radius
        {
            get { return radius; }
            set { radius = value; }
        }
        public float Height
        {
            get { return height; }
            set { height = value; }
        }
        public Vec3 SegmentStart
        {
            get
            {
                Vec3 capsuleWorldCenter = this.position + center; // (0,1,3) + (0,0,0) = (0,1,3)
                Vec3 halfHeight = new Vec3(0, (height - 2 * radius) / 2, 0); // (0,0.5,0)
                segmentStart = capsuleWorldCenter - halfHeight;
                return segmentStart;
            }
            set { segmentStart = value; }
        }
        public Vec3 SegmentEnd
        {
            get
            {
                Vec3 capsuleWorldCenter = this.position + center; // (0,1,3) + (0,0,0) = (0,1,3)
                Vec3 halfHeight = new Vec3(0, (height - 2 * radius) / 2, 0); // (0,0.5,0)
                segmentEnd = capsuleWorldCenter + halfHeight; // (0,1.5,3)
                return segmentEnd;
            }
            set { segmentEnd = value; }
        }
    }
}
