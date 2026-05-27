namespace DietPhysics
{
    public struct Mtf
    {
        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                value = min;
            }
            else if (value > max)
            {
                value = max;
            }

            return value;
        }
        public static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }
        public static float Sqrt(float f)
        {
            return (float)Math.Sqrt(f);
        }
        public static float Abs(float f)
        {
            return Math.Abs(f);
        }
        public static int Abs(int value)
        {
            return Math.Abs(value);
        }
        public static float Max(float a, float b)
        {
            return (a > b) ? a : b;
        }
        public static float Min(float a, float b)
        {
            return (a < b) ? a : b;
        }
    }
}
