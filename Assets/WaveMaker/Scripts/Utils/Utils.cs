using System;
using System.Runtime.CompilerServices;
using UnityEngine;

#if MATHEMATICS_INSTALLED
using Unity.Mathematics;
#endif

namespace WaveMaker
{
    public static class Utils
    {
        
        public readonly static float Epsilon = (float)1e-6;

        public static void Log(string text, GameObject go = null)
        {
            if (go != null)
                Debug.Log(string.Format("WaveMaker gameObject '{0}' - {1}", go.name, text));
            else
                Debug.Log(string.Format("WaveMaker - {0}", text));
        }

        public static void LogWarning(string text, GameObject go = null)
        {
            if (go != null)
                Debug.LogWarning(string.Format("WaveMaker gameObject '{0}' - {1}", go.name, text));
            else
                Debug.LogWarning(string.Format("WaveMaker - {0}", text));
        }

        public static void LogError(string text, GameObject go = null)
        {
            if (go != null)
                Debug.LogError(string.Format("WaveMaker gameObject '{0}' - {1}", go.name, text));
            else
                Debug.LogError(string.Format("WaveMaker - {0}", text));
        }

#if MATHEMATICS_INSTALLED && BURST_INSTALLED && COLLECTIONS_INSTALLED

        /// <summary>
        /// Data arrays can be normal or contain one or several ghost columns and row around it.
        /// Use this to convert from the array index of a normal array to the corresponding index in the ghost array
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FromNoGhostIndexToGhostIndex(int indexNoGhost, in IntegerPair resolution, in IntegerPair ghostResolution)
        {
            int ghostResolutionXDiff = (ghostResolution.x - resolution.x) / 2;
            int ghostResolutionZDiff = (ghostResolution.z - resolution.z) / 2;
            FromIndexToSampleIndices(indexNoGhost, resolution, out int sampleX, out int sampleZ);
            int sampleXGhost = sampleX + ghostResolutionXDiff;
            int sampleZGhost = sampleZ + ghostResolutionZDiff;
            return FromSampleIndicesToIndex(in ghostResolution, sampleXGhost, sampleZGhost);
        }

        /// <summary>
        /// Data arrays can be normal or contain one or several ghost columns and row around it.
        /// Use this to convert from the array index of an expanded ghost array to the corresponding index in the normal array
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FromGhostIndexToNoGhostIndex(int indexGhost, in IntegerPair resolution, in IntegerPair ghostResolution)
        {
            int ghostResolutionXDiff = (ghostResolution.x - resolution.x) / 2;
            int ghostResolutionZDiff = (ghostResolution.z - resolution.z) / 2;
            FromIndexToSampleIndices(indexGhost, ghostResolution, out int sampleX, out int sampleZ);
            int sampleXNoGhost = sampleX - ghostResolutionXDiff;
            int sampleZNoGhost = sampleZ - ghostResolutionZDiff;
            return FromSampleIndicesToIndex(in resolution, sampleXNoGhost, sampleZNoGhost);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FromIndexToSampleIndices(in int index, in IntegerPair resolution, out int sampleX, out int sampleZ)
        {
            if (resolution.x <= 0 || resolution.z <= 0)
                throw new ArgumentException("Resolution is negative or 0");

            if (index < 0)
                throw new ArgumentException("Index is less than 0");

            sampleX = index % resolution.x;
            sampleZ = index / resolution.x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FromIndexToSampleIndices_Untested(in int index, in IntegerPair resolution, out int sampleX, out int sampleZ)
        {
            sampleX = index % resolution.x;
            sampleZ = index / resolution.x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FromSampleIndicesToIndex(in IntegerPair resolution, in int sampleX, in int sampleZ)
        {
            if (resolution.x <= 0 || resolution.z <= 0)
                throw new ArgumentException("Resolution is negative or 0");

            if (sampleX < 0 || sampleZ < 0)
                throw new ArgumentException("Sample indices are less than 0");
            
            return resolution.x * sampleZ + sampleX;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FromSampleIndicesToIndex_Untested(in IntegerPair resolution, in int sampleX, in int sampleZ)
        {
            return resolution.x * sampleZ + sampleX;
        }

        /// <summary> Nearest neighbour index </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FromIndexToScaledIndex(in int index, in IntegerPair resolution, in IntegerPair scaledResolution)
        {
            if (resolution == scaledResolution)
                return index;

            FromIndexToSampleIndices_Untested(index, in resolution, out int x, out int z);
            float xNorm = x == 0? 0f : (float)x / (resolution.x - 1);
            float zNorm = z == 0? 0f : (float)z / (resolution.z - 1);

            int scaledX = (int)math.round( (scaledResolution.x - 1) * xNorm);
            int scaledZ = (int)math.round( (scaledResolution.z - 1) * zNorm);
            return FromSampleIndicesToIndex_Untested(in scaledResolution, scaledX, scaledZ);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGhostSampleInGhostArea(in IntegerPair resolution, in IntegerPair ghostResolution, in int sampleX, in int sampleZ)
        {
            int ghostResolutionXDiff = (ghostResolution.x - resolution.x) / 2;
            int ghostResolutionZDiff = (ghostResolution.z - resolution.z) / 2;
            return (sampleX < ghostResolutionXDiff || sampleX >= ghostResolution.x - ghostResolutionXDiff ||
                    sampleZ < ghostResolutionZDiff || sampleZ >= ghostResolution.z - ghostResolutionZDiff);
        }

        /// <summary>
        /// Returns the index of the sample selected from the given sample plus the offset
        /// </summary>
        /// <param name="index">the origin sample</param>
        /// <param name="offsetX">number of samples in X to offset (-X or +X)</param>
        /// <param name="offsetZ">number of samples in Z to offset (-Z or +Z)</param>
        /// <returns>the index of the given sample or -1 if out of the area</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetOffsetSample_Untested(in IntegerPair resolution, in int index, in int offsetX, in int offsetZ)
        {
            FromIndexToSampleIndices_Untested(in index, in resolution, out int sampleX, out int sampleZ);
            sampleX += offsetX;
            sampleZ += offsetZ;
            if (sampleX < 0 || sampleZ < 0 || sampleX > resolution.x-1 || sampleZ > resolution.z-1)
                return -1;

            return FromSampleIndicesToIndex_Untested(in resolution, in sampleX, in sampleZ);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSampleInRange(in IntegerPair resolution, in int index)
        {
            return index >= 0 && index < resolution.x * resolution.z;
        }

        /// <returns><c>true</c> if sample is on the border of this resolution</returns>
        /// <param name="x">The x sample index on the resolution.</param>
        /// <param name="z">The z sample index on the resolution</param>
        /// <param name="xSide">-1 if on the left x extreme, 1 if on the right. 0 rest of cases</param>
        /// <param name="zSide">-1 if on the bottom z extreme, 1 if on the top. 0 rest of cases</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSampleExtreme(in int index, in IntegerPair resolution, ref int xSide, ref int zSide)
        {
            FromIndexToSampleIndices_Untested(in index, in resolution, out int x, out int z);
            xSide = x==0? -1 : x / (resolution.x - 1);
            zSide = z==0? -1 : z / (resolution.z - 1);
            return xSide != 0 || zSide != 0;
        }

        /// <summary>
        /// Calculates the min and max sample indices for the area the given collider occupies over the surface.
        /// </summary>
        public static void GetColliderProjectedAreaOnSurface(in WaveMakerSurface surface, in NativeCollider collider,
                                                            out int sampleMin_ls, out int sampleMax_ls)
        {
            float4 min = collider.boundsMin;
            float4 max = collider.boundsMax;

            TransformBounds(ref min, ref max, in surface._w2lTransformMatrix);
            
            sampleMin_ls = GetNearestSampleFromLocalPosition(min, surface._resolution, surface._sampleSize_ls);
            sampleMax_ls = GetNearestSampleFromLocalPosition(max, surface._resolution, surface._sampleSize_ls);
        }

        /// <summary> Transform center and extents of the given bounds transformed by the transformation matrix passed. </summary>
        public static void TransformBounds(ref float4 min, ref float4 max, in float4x4 matrix)
        {
            var xa = matrix.c0 * min.x;
            var xb = matrix.c0 * max.x;

            var ya = matrix.c1 * min.y;
            var yb = matrix.c1 * max.y;

            var za = matrix.c2 * min.z;
            var zb = matrix.c2 * max.z;

            var col4Pos = new float4(matrix.c3.xyz, 0);
            min = math.min(xa, xb) + math.min(ya, yb) + math.min(za, zb) + col4Pos;
            max = math.max(xa, xb) + math.max(ya, yb) + math.max(za, zb) + col4Pos;
        }

        /// <summary>
        /// Get position in surface local space of the given sample. Y coordinate will be 0.
        /// </summary>
        /// <exception cref="ArgumentException">If index out of range or incorrect values passed</exception>
        public static float4 GetLocalPositionFromSample(int sampleX, int sampleZ, in IntegerPair resolution, in float2 sampleSize)
        {
            if (sampleX >= resolution.x || sampleZ >= resolution.z || sampleX < 0 || sampleZ < 0)
            {
                sampleX = math.clamp(sampleX, 0, resolution.x - 1);
                sampleZ = math.clamp(sampleZ, 0, resolution.z - 1);

                //TODO: Error in burst. Not allowed.
                //throw new ArgumentException("Sample index is out of range : " + sampleX + " - " + sampleZ);
            }

            if (sampleSize.x <= 0 || sampleSize.y <= 0)
                throw new ArgumentException("Sample size values are not positive");

            if (resolution.x <= 0 || resolution.z <= 0)
                throw new ArgumentException("Resolution values are not positive");

            return new float4(sampleX * sampleSize.x, 0, sampleZ * sampleSize.y, 0);
        }

        /// <summary>
        /// Get position in surface local space of the given sample. Y coordinate will be 0.
        /// </summary>
        /// <exception cref="ArgumentException">If index out of range or incorrect values passed</exception>
        public static float4 GetLocalPositionFromSample(int sampleIndex, in IntegerPair resolution, in float2 sampleSize)
        {
            FromIndexToSampleIndices(sampleIndex, in resolution, out int sampleX, out int sampleZ);
            return GetLocalPositionFromSample(sampleX, sampleZ, in resolution, in sampleSize);
        }

        /// <summary>It returns the coordinates of the nearest sample for the given position. 
        /// If the position is away from the bounding box, it will return the nearest one anyway.</summary>
        public static int GetNearestSampleFromLocalPosition(float4 pos_ls, in IntegerPair resolution, in float2 sampleSize_ls)
        {
            if (sampleSize_ls.x <= 0 || sampleSize_ls.y <= 0)
                throw new ArgumentException("Sample size values are not positive");

            if (resolution.x < 3 || resolution.z < 3)
                throw new ArgumentException("Surface size values are not positive");

            int sampleX = (int) math.round(pos_ls.x / sampleSize_ls.x);
            int sampleZ = (int) math.round(pos_ls.z / sampleSize_ls.y);

            sampleX = math.clamp(sampleX, 0, resolution.x - 1);
            sampleZ = math.clamp(sampleZ, 0, resolution.z - 1);

            return sampleZ * resolution.x + sampleX;
        }

        /// <summary>
        /// Given a point in a given space and a center of rotation in the given space, returns the velocity at that point
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 VelocityAtPoint(float4 point, float4 centerOfRotation, float3 angularVelocity, float3 linearVelocity)
        {
            return math.cross(angularVelocity, (point - centerOfRotation).xyz) + linearVelocity;
        }

        /// <summary>
        /// Given an old and new rotation quaternions, return the angular velocity of the given object
        /// </summary>
        /// <param name="oldQuat">rotation before (normalized)</param>
        /// <param name="newQuat">rotation after a fixedDeltaTime (normalized)</param>
        public static float3 GetAngularVelocity(quaternion oldQuat, quaternion newQuat)
        {
            float scaledTime = 2 / Time.fixedDeltaTime;
            oldQuat.value.xyz *= -1;
            oldQuat = math.mul(newQuat, oldQuat);
            return oldQuat.value.xyz * scaledTime;
        }

        ///<summary></summary>Gradient has only X and Z coords.
        /// This returns a nomralized vector, the direction at which the gradient points at.</summary>
        public static void FromGradientToDirectionVector(ref Vector3 inoutVector)
        {
            // NOTE: Magnitude is already calculated in the Y component for efficienty reasons
            float magnitude = inoutVector.y;

            // Avoid division by zero
            if (magnitude < 0.00001f && magnitude > -0.00001f)
            {
                inoutVector.x = 0; inoutVector.y = 0; inoutVector.z = 0;
                return;
            }

            inoutVector.x = inoutVector.x / magnitude;
            inoutVector.y = -magnitude;
            inoutVector.z = inoutVector.z / magnitude;
        }

        // This operation is performed in Unity physics world, all expressed in World Space.
        public static float4 ComputeBuoyantForce(in float4 centerOfMass_ws, in float3 linearVel, in float3 angularVel, float inmersedVolume, float fluidDensity,
                                            in float4 upDirection_ws, in float4 forceDirection_ws, in float4 hitPos_ws, float damping = 0)
        {
            // d * vol * - gravity acc = mass * acc = Force
            float buoyantForceMag = fluidDensity * inmersedVolume * -Physics.gravity.y; 
            var buoyantForce = forceDirection_ws * buoyantForceMag; 

            // Damping force to avoid eternal bouncing. Only applied on the Y axis.
            if (damping > 0)
            {
                // Velocity at the point of hit in the object
                var velAtHitPos = VelocityAtPoint(hitPos_ws, centerOfMass_ws, angularVel, linearVel);
                float projectedVelocity = math.dot(velAtHitPos, upDirection_ws.xyz);

                /* TODO: Old method.
                // NOTE: There is a limit on velocity for the given damping and volume that will make buoyancy get to 0
                float velMax = buoyantForceMag / damping;

                // From vel == maxVel/2 to maxVel scale goes from 1 to 0.
                // Scale is 2 when vel is 0. 1 when vel is velMax/2 and 0 when vel is velMax. <0 if vel is bigger than velMax
                float scale = 2 - (2 * projectedVelocity / velMax);
                scale = math.max(scale, 0); // Clamp 0
                scale = math.min(scale, 1); // Clamp 1
                float dampingMagnitude = projectedVelocity * damping * (1 - scale);
                */

                float dampingMagnitude = projectedVelocity * damping;

                buoyantForce.x -= upDirection_ws.x * dampingMagnitude;
                buoyantForce.y -= upDirection_ws.y * dampingMagnitude;
                buoyantForce.z -= upDirection_ws.z * dampingMagnitude;
            }

            return buoyantForce;
        }

        /// <param name="a">start point of segment</param>
        /// <param name="b">end point of segment</param>
        /// <param name="point">point to compare</param>
        /// <param name="h">normalized coordinate from 0 in a to 1 in b. If clamp not enabled, can surpass those values</param>
        /// <param name="clampToSegment">mu will be less than 0 or more than 1 if the point is further away from a or b</param>
        /// <returns>the point inside the edge if clamped, or following the line defined by a and b</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 NearestPointOnEdge(float4 a, float4 b, float4 point, out float h, bool clampToSegment = true)
        {
            float4 ap = point - a;
            float4 ab = b - a;

            h = math.dot(ap, ab) / math.dot(ab, ab);

            if (clampToSegment)
                h = math.saturate(h);

            return a + ab * h;
        }

        public static void IncreaseAreaBy(in IntegerPair resolution, ref IntegerPair areaResolution, ref int xOffset, ref int zOffset, int by)
        {
            // Left
            areaResolution.x += math.min(xOffset, by);
            xOffset = math.max(0, xOffset - by);

            // Bottom
            areaResolution.z += math.min(zOffset, by);
            zOffset = math.max(0, zOffset - by);

            // Top - Right
            areaResolution.x = math.min(areaResolution.x + xOffset + by, resolution.x) - xOffset;
            areaResolution.z = math.min(areaResolution.z + zOffset + by, resolution.z) - zOffset;
        }

        public static void DrawDetectionDepth(WaveMakerSurface _surface)
        {
            float4 size = new float4(_surface.Size_ls.x, _surface.detectionDepth, _surface.Size_ls.y, 0);
            var mat = _surface._l2wTransformMatrix;

            Vector3 point0_ws = math.mul(mat, new float4(0, -size.y, 0, 1)).xyz;
            Vector3 point1_ws = math.mul(mat, new float4(0, -size.y, size.z, 1)).xyz;
            Vector3 point2_ws = math.mul(mat, new float4(size.x, -size.y, size.z, 1)).xyz;
            Vector3 point3_ws = math.mul(mat, new float4(size.x, -size.y, 0, 1)).xyz;

            Vector3 vectorUp_ws = math.mul(mat, new float4(0, size.y, 0, 0)).xyz;

            Debug.DrawRay(point0_ws, vectorUp_ws, Color.white);
            Debug.DrawRay(point1_ws, vectorUp_ws, Color.white);
            Debug.DrawRay(point2_ws, vectorUp_ws, Color.white);
            Debug.DrawRay(point3_ws, vectorUp_ws, Color.white);

            Debug.DrawLine(point0_ws, point1_ws, Color.white);
            Debug.DrawLine(point1_ws, point2_ws, Color.white);
            Debug.DrawLine(point2_ws, point3_ws, Color.white);
            Debug.DrawLine(point3_ws, point0_ws, Color.white);
        }

#endif
    }
}
