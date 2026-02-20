using UnityEngine;

namespace CherryFramework.Utils
{
    public static class MathUtils
    {
        public static Vector2 RotateByRads(this Vector2 v, float delta) =>
            new Vector2(
                v.x * Mathf.Cos(delta) - v.y * Mathf.Sin(delta),
                v.x * Mathf.Sin(delta) + v.y * Mathf.Cos(delta)
            );
        
        public static Vector2 RotateByDegrees(this Vector2 v, float delta) => 
            v.RotateByRads(delta * Mathf.Deg2Rad);

        public static Vector3 ArrayToVector3(float[] array)
        {
            return new Vector3(array[0], array[1], array[2]);
        }

        public static float[] Vector3ToArray(Vector3 v)
        {
            return new float[] { v.x, v.y, v.z };
        }

        public static float[] QuaternionToArray(Quaternion q)
        {
            return new float[] { q.x, q.y, q.z, q.w };
        }

        public static Quaternion ArrayToQuaternion(float[] array)
        {
            return new Quaternion(array[0], array[1], array[2], array[3]);
        }
        
        /// <summary>
        /// Checks if the value is within the range [a, b] or (a, b) depending on the 'exclusive' flag.
        /// The order of the range boundaries does not matter.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="a">First range boundary.</param>
        /// <param name="b">Second range boundary.</param>
        /// <param name="exclusive">If true, checks exclusive range (a, b). If false, checks inclusive range [a, b].</param>
        public static bool InRange(this float value, float a, float b, bool exclusive = false)
        {
            if (exclusive)
                return value > Mathf.Min(a, b) && value < Mathf.Max(a, b);

            return value >= Mathf.Min(a, b) && value <= Mathf.Max(a, b);
        }
    }
}