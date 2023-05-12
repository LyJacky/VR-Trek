﻿using UnityEngine;

namespace TrekVRApplication {

    public static class MathUtils {

        /// <summary>
        ///     Look up table for 10^n where n less than 8 for 
        ///     faster computation.
        /// </summary>
        private static int[] PowerOf10Table = new int[] {
            1, 10, 100, 1000, 10000, 100000, 1000000, 10000000
        };

        public static int Clamp(int value, int min, int max = int.MaxValue) {
            return value < min ? min : value > max ? max : value;
        }

        public static float Clamp(float value, float min, float max = float.MaxValue) {
            return value < min ? min : value > max ? max : value;
        }

        // Source: https://stackoverflow.com/questions/600293/how-to-check-if-a-number-is-a-power-of-2
        public static bool IsPowerOfTwo(int x) {
            return (x != 0) && ((x & (x - 1)) == 0);
        }

        /// <summary>
        ///     Wraps an angle to a value between 0 and 360 degrees.
        /// </summary>
        /// <param name="angle">Angle in degress.</param>
        /// <returns>The equivalent angle between 0 and 360 degrees.</returns>
        public static float WrapAngle0To360(float angle) {
            return angle % 360f;
        }

        /// <summary>
        ///     Wraps an angle to a value between -180 and 180 degrees.
        /// </summary>
        /// <param name="angle">Angle in degress.</param>
        /// <returns>The equivalent angle between -180 and 180 degrees.</returns>
        public static float WrapAngle180(float angle) {
            if (angle < -180) {
                return (angle - 180) % 360f + 180f;
            } else if (angle > 180) {
                return (angle + 180) % 360f - 180f;
            }
            return angle;
        }

        /// <summary>
        ///     Wraps an angle to a value between 0 and 2PI radians.
        /// </summary>
        /// <param name="angle">Angle in radians.</param>
        /// <returns>The equivalent angle between 0 and 2PI radians.</returns>
        public static float WrapAngle0To2Pi(float angle) {
            return angle % (2 * Mathf.PI);
        }

        /// <summary>
        ///     Wraps an angle to a value between -PI and PI radians.
        /// </summary>
        /// <param name="angle">Angle in radians.</param>
        /// <returns>The equivalent angle between -PI and PI radians.</returns>
        public static float WrapAnglePi(float angle) {
            return (angle + Mathf.PI) % (2 * Mathf.PI) - Mathf.PI;
        }

        /// <summary>
        ///     Compares two floating point precision numbers up to the given
        ///     decimal place.
        /// </summary>
        /// <param name="a">The first floating point number</param>
        /// <param name="b">The second floating point number</param>
        public static bool CompareFloats(float a, float b, int precision = 3) {
            float multiplier;
            if (precision < 8) {
                multiplier = PowerOf10Table[precision];
            } else {
                multiplier = Mathf.Pow(10, precision);
            }
            a *= multiplier;
            b *= multiplier;
            return Mathf.RoundToInt(a) == Mathf.RoundToInt(b);
        }

        /// <summary>
        ///     Calculate distance between a point and a line. 
        ///     Copied over from Unity's HandleUtility.cs.
        /// </summary>
        public static float DistancePointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd) {
            Vector3 relativePoint = point - lineStart;
            Vector3 lineDirection = lineEnd - lineStart;
            float length = lineDirection.magnitude;
            Vector3 normalizedLineDirection = lineDirection;
            if (length > .000001f)
                normalizedLineDirection /= length;

            float dot = Vector3.Dot(normalizedLineDirection, relativePoint);
            dot = Mathf.Clamp(dot, 0.0F, length);

            Vector3 projected = lineStart + normalizedLineDirection * dot;
            return Vector3.Magnitude(projected - point);
        }

    }

}