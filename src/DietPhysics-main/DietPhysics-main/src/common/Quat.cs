using System.Numerics;

namespace DietPhysics
{
    public struct Quat
    {
        public float x;
        public float y;
        public float z;
        public float w;
        public Quat(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
        #region Operators
        public static Quat operator +(Quat q1, Quat q2)
        {
            return new Quat(q1.x + q2.x, q1.y + q2.y, q1.z + q2.z, q1.w + q2.w);
        }
        public static Quat operator -(Quat q1, Quat q2)
        {
            return new Quat(q1.x - q2.x, q1.y - q2.y, q1.z - q2.z, q1.w - q2.w);
        }
        public static Quat operator *(Quat q1, Quat q2)
        {
            return new Quat(
                q1.w * q2.x + q1.x * q2.w + q1.y * q2.z - q1.z * q2.y,
                q1.w * q2.y - q1.x * q2.z + q1.y * q2.w + q1.z * q2.x,
                q1.w * q2.z + q1.x * q2.y - q1.y * q2.x + q1.z * q2.w,
                q1.w * q2.w - q1.x * q2.x - q1.y * q2.y - q1.z * q2.z
            );
        }
        public static Quat operator *(Quat q, float f)
        {
            return new Quat(q.x * f, q.y * f, q.z * f, q.w * f);
        }
        public static Vec3 operator *(Quat rotation, Vec3 point)
        {
            float num = rotation.x * 2f;
            float num2 = rotation.y * 2f;
            float num3 = rotation.z * 2f;
            float num4 = rotation.x * num;
            float num5 = rotation.y * num2;
            float num6 = rotation.z * num3;
            float num7 = rotation.x * num2;
            float num8 = rotation.x * num3;
            float num9 = rotation.y * num3;
            float num10 = rotation.w * num;
            float num11 = rotation.w * num2;
            float num12 = rotation.w * num3;
            Vec3 result = default(Vec3);
            result.x = (1f - (num5 + num6)) * point.x + (num7 - num12) * point.y + (num8 + num11) * point.z;
            result.y = (num7 + num12) * point.x + (1f - (num4 + num6)) * point.y + (num9 - num10) * point.z;
            result.z = (num8 - num11) * point.x + (num9 + num10) * point.y + (1f - (num4 + num5)) * point.z;
            return result;
        }
        public static Quat operator /(Quat q, float f)
        {
            return new Quat(q.x / f, q.y / f, q.z / f, q.w / f);
        }
        public static Quat operator /(Quat value1, Quat value2)
        {
            Quat ans;

            float q1x = value1.x;
            float q1y = value1.y;
            float q1z = value1.z;
            float q1w = value1.w;

            //-------------------------------------
            // Inverse part.
            float ls = value2.x * value2.x + value2.y * value2.y +
                       value2.z * value2.z + value2.w * value2.w;
            float invNorm = 1.0f / ls;

            float q2x = -value2.x * invNorm;
            float q2y = -value2.y * invNorm;
            float q2z = -value2.z * invNorm;
            float q2w = value2.w * invNorm;

            //-------------------------------------
            // Multiply part.

            // cross(av, bv)
            float cx = q1y * q2z - q1z * q2y;
            float cy = q1z * q2x - q1x * q2z;
            float cz = q1x * q2y - q1y * q2x;

            float dot = q1x * q2x + q1y * q2y + q1z * q2z;

            ans.x = q1x * q2w + q2x * q1w + cx;
            ans.y = q1y * q2w + q2y * q1w + cy;
            ans.z = q1z * q2w + q2z * q1w + cz;
            ans.w = q1w * q2w - dot;

            return ans;
        }
        #endregion
        public static Quat Euler(Vec3 euler)
        {
            float xRad = euler.x * (float)Math.PI / 180f;
            float yRad = euler.y * (float)Math.PI / 180f;
            float zRad = euler.z * (float)Math.PI / 180f;

            float cx = (float)Math.Cos(xRad * 0.5f);
            float sx = (float)Math.Sin(xRad * 0.5f);
            float cy = (float)Math.Cos(yRad * 0.5f);
            float sy = (float)Math.Sin(yRad * 0.5f);
            float cz = (float)Math.Cos(zRad * 0.5f);
            float sz = (float)Math.Sin(zRad * 0.5f);

            float w = cx * cy * cz + sx * sy * sz;
            float x = sx * cy * cz - cx * sy * sz;
            float y = cx * sy * cz + sx * cy * sz;
            float z = cx * cy * sz - sx * sy * cz;

            return new Quat(x, y, z, w);
        }
        public static Quat Divide(Quat value1, Quat value2)
        {
            return value1 / value2;
        }
        public static Quat Divide(Quat left, float divisor)
        {
            return new Quat(
                left.x / divisor,
                left.y / divisor,
                left.z / divisor,
                left.w / divisor
            );
        }

        public static Quat Conjugate(Quat value)
        {
            return Multiply(value, new Vector4(-1.0f, -1.0f, -1.0f, 1.0f));
        }
        public static Quat Multiply(Quat value1, Quat value2)
        {
            return value1 * value2;
        }
        public static Quat Multiply(Quat value1, Vector4 value2)
        {
            return new Quat(
                value1.x * value2.X,
                value1.y * value2.Y,
                value1.z * value2.Z,
                value1.w * value2.W
            );
        }
        public static Quat Inverse(Quat value)
        {
            return Divide(Conjugate(value), value.LengthSquared());
        }
        public static float Dot(Quat Quat1, Quat Quat2)
        {
            return (Quat1.x * Quat2.x)
                 + (Quat1.y * Quat2.y)
                 + (Quat1.z * Quat2.z)
                 + (Quat1.w * Quat2.w);
        }
        public readonly float LengthSquared()
        {
            return Dot(this, this);
        }
    }
}
