namespace DietPhysics
{
    public struct Vec3
    {
        public float x;
        public float y;
        public float z;

        private static readonly Vec3 zeroVector = new Vec3(0f, 0f, 0f);
        private static readonly Vec3 oneVector = new Vec3(1f, 1f, 1f);
        private static readonly Vec3 upVector = new Vec3(0f, 1f, 0f);
        private static readonly Vec3 downVector = new Vec3(0f, -1f, 0f);
        private static readonly Vec3 leftVector = new Vec3(-1f, 0f, 0f);
        private static readonly Vec3 rightVector = new Vec3(1f, 0f, 0f);
        private static readonly Vec3 forwardVector = new Vec3(0f, 0f, 1f);
        private static readonly Vec3 backVector = new Vec3(0f, 0f, -1f);

        public Vec3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vec3 zero
        {
            
            get
            {
                return zeroVector;
            }
        }
        public static Vec3 one
        {
            
            get
            {
                return oneVector;
            }
        }
        public static Vec3 forward
        {
            
            get
            {
                return forwardVector;
            }
        }
        public static Vec3 back
        {
            
            get
            {
                return backVector;
            }
        }
        public static Vec3 up
        {
            
            get
            {
                return upVector;
            }
        }
        public static Vec3 down
        {
            
            get
            {
                return downVector;
            }
        }
        public static Vec3 left
        {
            
            get
            {
                return leftVector;
            }
        }
        public static Vec3 right
        {
            
            get
            {
                return rightVector;
            }
        }
        public Vec3 normalized
        {
            
            get
            {
                return Normalize(this);
            }
        }
        public float magnitude
        {
            
            get
            {
                return (float)Math.Sqrt(x * x + y * y + z * z);
            }
        }
        public float sqrMagnitude
        {
            
            get
            {
                return x * x + y * y + z * z;
            }
        }
        
        public static Vec3 Normalize(Vec3 value)
        {
            float num = Magnitude(value);
            if (num > 1E-05f)
            {
                return value / num;
            }

            return zero;
        }
        
        public void Normalize()
        {
            float num = Magnitude(this);
            if (num > 1E-05f)
            {
                this /= num;
            }
            else
            {
                this = zero;
            }
        }
        
        public static float Magnitude(Vec3 vector)
        {
            return (float)Math.Sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z);
        }
        
        public static float Dot(Vec3 lhs, Vec3 rhs)
        {
            return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
        }
        
        public static Vec3 Reflect(Vec3 inDirection, Vec3 inNormal)
        {
            float num = -2f * Dot(inNormal, inDirection);
            return new Vec3(num * inNormal.x + inDirection.x, num * inNormal.y + inDirection.y, num * inNormal.z + inDirection.z);
        }
        
        public static Vec3 Cross(Vec3 lhs, Vec3 rhs)
        {
            return new Vec3(lhs.y * rhs.z - lhs.z * rhs.y, lhs.z * rhs.x - lhs.x * rhs.z, lhs.x * rhs.y - lhs.y * rhs.x);
        }
        
        public static float Distance(Vec3 a, Vec3 b)
        {
            float num = a.x - b.x;
            float num2 = a.y - b.y;
            float num3 = a.z - b.z;
            return (float)Math.Sqrt(num * num + num2 * num2 + num3 * num3);
        }
        #region Operators
        
        public static Vec3 operator +(Vec3 a, Vec3 b)
        {
            return new Vec3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        
        public static Vec3 operator -(Vec3 a, Vec3 b)
        {
            return new Vec3(a.x - b.x, a.y - b.y, a.z - b.z);
        }
        
        public static Vec3 operator -(Vec3 a)
        {
            return new Vec3(0f - a.x, 0f - a.y, 0f - a.z);
        }
        
        public static Vec3 operator *(Vec3 a, float d)
        {
            return new Vec3(a.x * d, a.y * d, a.z * d);
        }

        
        public static Vec3 operator *(float d, Vec3 a)
        {
            return new Vec3(a.x * d, a.y * d, a.z * d);
        }
        
        public static Vec3 operator /(Vec3 a, float d)
        {
            return new Vec3(a.x / d, a.y / d, a.z / d);
        }
        
        public static bool operator ==(Vec3 lhs, Vec3 rhs)
        {
            float num = lhs.x - rhs.x;
            float num2 = lhs.y - rhs.y;
            float num3 = lhs.z - rhs.z;
            float num4 = num * num + num2 * num2 + num3 * num3;
            return num4 < 9.99999944E-11f;
        }
        
        public static bool operator !=(Vec3 lhs, Vec3 rhs)
        {
            return !(lhs == rhs);
        }
        #endregion
    }
}
