#if MATHEMATICS_INSTALLED && BURST_INSTALLED && COLLECTIONS_INSTALLED

using System.Collections.Generic;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace WaveMaker
{
    public struct HitDistance
    {
        public int colliderIndex;
        public float distanceFromBottom;

        public HitDistance(int colIndex, float dist)
        {
            colliderIndex = colIndex;
            distanceFromBottom = dist;
        }

        public struct SortByDistance : IComparer<HitDistance>
        {
            int IComparer<HitDistance>.Compare(HitDistance a, HitDistance b)
            {
                if (a.distanceFromBottom > b.distanceFromBottom)
                    return 1;
                if (a.distanceFromBottom < b.distanceFromBottom)
                    return -1;
                else
                    return 0;
            }
        }
    }

    /// <summary>
    /// Buoyant forces are calculated using world space. Since all the data of occuption and distances is
    /// expressed in world space, we have to convert all of the data accordingly.
    /// </summary>
    [BurstCompile]
    public struct ApplyBuoyantForcesJob : IJobParallelFor
    {
        public NativeArray<WaveMakerSurface.RigidBodyData> rigidBodyData;
        [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<float4> gradients;
        [NativeDisableParallelForRestriction] [ReadOnly] public NativeArray<InteractionData> interactionData;

        [NativeDisableParallelForRestriction] [WriteOnly] public NativeArray<WaveMakerSurface.BuoyantForceData> buoyantForces;

        [ReadOnly] public float4 upDirection_ws;
        [ReadOnly] public int nMaxCellsPerInteractor;
        [ReadOnly] public float2 sampleSize_ls;
        [ReadOnly] public float detectionDepth_ls;
        [ReadOnly] public IntegerPair resolution;
        [ReadOnly] public bool horizontalBuoyancy;
        [ReadOnly] public float4x4 l2wTransformMatrix;
        [ReadOnly] public float density;
        [ReadOnly] public float buoyancyDamping;
        [ReadOnly] public float3 scale_l2w;
        [ReadOnly] public float fixedDeltaTime;
        [ReadOnly] public NativeParallelHashMap<int, int> colliderToRbIndices;
        [ReadOnly] public bool exportForces;

        public void Execute(int interactorIndex)
        {
            if (!colliderToRbIndices.TryGetValue(interactorIndex, out int rbIndex))
                return;

            //TODO: EFFICIENCY. Simply don't store rbs that are kinematic? But we need to detect the change
            // Can't apply forces to kinematic rbs
            if (rigidBodyData[rbIndex].isKinematic)
                return;

            var rbData = rigidBodyData[rbIndex];

            // Backup both values before modifying them for the next step
            var oldLinearVel = rbData.linearVelocity;
            var oldAngularVel = rbData.angularVelocity;

            for (int hitIndex = 0; hitIndex < nMaxCellsPerInteractor; hitIndex++)
            {
                var data = new InteractionData();
                InteractionDataArray.GetData(in interactionData, nMaxCellsPerInteractor, interactorIndex, hitIndex, ref data);

                // Anymore hits from now on
                if (data.IsNull)
                    break;

                // Local to world space for interacting with the Unity Physics world
                float fluidVolume =
                    data.occupancy * scale_l2w.y * 
                    sampleSize_ls.x * scale_l2w.x * 
                    sampleSize_ls.y * scale_l2w.z;

                if (fluidVolume < 0.0001f)
                    continue;

                var buoyancyDirection = upDirection_ws;

                if (horizontalBuoyancy)
                {
                    var grad = gradients[data.cellIndex];
                    buoyancyDirection.x += grad.x;
                    buoyancyDirection.z += grad.z;
                    buoyancyDirection = math.normalize(buoyancyDirection);
                }

                Utils.FromIndexToSampleIndices(data.cellIndex, resolution, out int sampleX, out int sampleZ);
                var hitPos_ls = new float4(sampleX * sampleSize_ls.x, data.distance - detectionDepth_ls, sampleZ * sampleSize_ls.y, 0);
                var hitPos_ws = new float4(math.mul(l2wTransformMatrix, new float4(hitPos_ls.xyz, 1)).xyz, 0);

                // Apply buoyant force using the surface normal instead of the up vector
                var force_ws = Utils.ComputeBuoyantForce(in rbData.centerOfMass_ws, in oldLinearVel, in oldAngularVel,
                                                         fluidVolume, density, in upDirection_ws, in buoyancyDirection, in hitPos_ws, buoyancyDamping);
                if (exportForces)
                {
                    var forceData = new WaveMakerSurface.BuoyantForceData();
                    forceData.hitPos_ws = hitPos_ws;
                    forceData.force_ws = force_ws;
                    buoyantForces[data.cellIndex] = forceData;
                }

                // Update Linear Velocity
                float4 velDelta = (force_ws / rbData.mass);
                rbData.linearVelocity += velDelta.xyz * fixedDeltaTime;

                // Update Angular Velocity
                float4 torque_ws = new float4(math.cross(hitPos_ws.xyz - rbData.centerOfMass_ws.xyz, force_ws.xyz), 0);
                float3 angVelDelta = math.mul(rbData.inverseInertiaTensor, torque_ws).xyz;
                rbData.angularVelocity += angVelDelta * fixedDeltaTime;
            }

            rigidBodyData[rbIndex] = rbData;
        }
    }

    /// <summary>
    /// Sets to null the last hit of each collider in the interaction data array. We can only do this
    /// after all the paralell hits have been added to the array
    /// </summary>
    [BurstCompile]
    public struct FinishOccupancyJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<InteractionData> interactionData;

        [ReadOnly] public NativeArray<int> hitsPerObject;
        [ReadOnly] public int nMaxCellsPerInteractor;

        public void Execute(int index)
        {
            var nHits = hitsPerObject[index];

            if (nHits > 0 && nHits < nMaxCellsPerInteractor)
                InteractionDataArray.SetNull(ref interactionData, nMaxCellsPerInteractor, index, nHits);
        }
    }

    [BurstCompile]
    public struct OccupancyEffectJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<float> heights;
        [ReadOnly] [NativeDisableParallelForRestriction] public NativeArray<float> occupancy;
        [ReadOnly] [NativeDisableParallelForRestriction] public NativeArray<float> occupancyPrevious;
        [ReadOnly] public NativeArray<int> fixedSamples;
        [ReadOnly] public IntegerPair ghostResolution;
        [ReadOnly] public IntegerPair resolution;
        [ReadOnly] public float effectScale;
        [ReadOnly] public float sleepThreshold;
        [WriteOnly] public NativeArray<long> isAwake;

        public void Execute(int index)
        {
            if (fixedSamples[index] == 1)
                return;

            float heightOffset = 0;

            Utils.FromIndexToSampleIndices(in index, in resolution, out int x, out int z);

            // Left
            if (x > 0)
            {
                int indexAux = Utils.FromSampleIndicesToIndex(resolution, x - 1, z);
                heightOffset += (occupancy[indexAux] - occupancyPrevious[indexAux]) * 0.25f;
            }

            // right
            if (x < resolution.x - 1)
            {
                int indexAux = Utils.FromSampleIndicesToIndex(resolution, x + 1, z);
                heightOffset += (occupancy[indexAux] - occupancyPrevious[indexAux]) * 0.25f;
            }

            // bottom
            if (z > 0)
            {
                int indexAux = Utils.FromSampleIndicesToIndex(resolution, x, z - 1);
                heightOffset += (occupancy[indexAux] - occupancyPrevious[indexAux]) * 0.25f;
            }

            // top
            if (z < resolution.z - 1)
            {
                int indexAux = Utils.FromSampleIndicesToIndex(resolution, x, z + 1);
                heightOffset += (occupancy[indexAux] - occupancyPrevious[indexAux]) * 0.25f;
            }

            if (heightOffset > sleepThreshold || heightOffset < -sleepThreshold)
            {
                int heightsIndex = Utils.FromNoGhostIndexToGhostIndex(index, in resolution, in ghostResolution);
                heights[heightsIndex] += heightOffset * effectScale;

                ActivateAwakeStatus();
            }
        }

        private unsafe void ActivateAwakeStatus()
        {
            Interlocked.Exchange(ref ((long*)isAwake.GetUnsafePtr())[0], 1);
        }
    }

    [BurstCompile]
    public struct RaymarchOccupancy : IJobParallelFor
    {
        // Write
        [NativeDisableParallelForRestriction] public NativeArray<InteractionData> interactionData; // Data in local space
        [NativeDisableParallelForRestriction] public NativeArray<float> occupancy; // Data in local space
        [NativeDisableParallelForRestriction] unsafe public NativeArray<int> hitsPerRigidbody;

        // Read
        [ReadOnly] public NativeArray<NativeCollider> colliders;
        [ReadOnly] public float4x4 _l2wTransformMatrix;
        [ReadOnly] public IntegerPair resolution;
        [ReadOnly] public float2 sampleSize_ls;
        [ReadOnly] public float detectionDepth_ls;
        [ReadOnly] public float depthScale_l2w;
        [ReadOnly] public float depthScale_w2l;
        [ReadOnly] public float4 upDirection_ws;
        [ReadOnly] public IntegerPair areaResolution;
        [ReadOnly] public IntegerPair offset;
        [ReadOnly] public bool affectSurface;
        [ReadOnly] public bool buoyancyEnabled;
        [ReadOnly] public NativeParallelHashMap<int, int> colliderToRbIndices;
        [ReadOnly] public int nMaxCellsPerInteractor;
        [ReadOnly] public float smoothedBorder_ws;

        public void Execute(int cellIndex)
        {
            // Calculate the index in the whole surface
            int x = cellIndex % areaResolution.x + offset.x;
            int z = cellIndex / areaResolution.x + offset.z;
            cellIndex = resolution.x * z + x;

            occupancy[cellIndex] = 0;

            //NOTE: Dispose doesn't have to be called inside jobs even though documentation says so. This is said by Joaquim Ante (CTO) in the forums
            var dataPerRigidbody = new NativeArray<InteractionData>(colliders.Length, Allocator.Temp);
            var botTopHitOrder = new NativeList<HitDistance>(colliders.Length, Allocator.Temp);

            // Pass bottom value on cell 0,0. Then pass ws vectors.
            float4 origin_ws = new float4(math.mul(_l2wTransformMatrix, new float4(x * sampleSize_ls.x, -detectionDepth_ls, z * sampleSize_ls.y, 1)).xyz, 0);
            bool anyHit = false;

            //NOTE: We store occupancy data for collliders without rigidbodies too. But we only output the colliders with rbs
            for (int i = 0; i < colliders.Length; i++)
            {
                float occ_ws = RaymarchUtils.CalculateOccupancy(colliders[i], origin_ws, upDirection_ws, detectionDepth_ls * depthScale_l2w, smoothedBorder_ws, out float hitDistance_ws);
                anyHit = anyHit || occ_ws > 0;

                if (occ_ws > 0.0001f)
                {
                    float hitDistance_ls = hitDistance_ws * depthScale_w2l;
                    float occ_ls = occ_ws * depthScale_w2l;
                    dataPerRigidbody[i] = new InteractionData(cellIndex, occ_ls, hitDistance_ls);
                    botTopHitOrder.AddNoResize(new HitDistance(i, hitDistance_ls));
                }
                else
                    dataPerRigidbody[i] = InteractionData.Null;
            }

            if (anyHit)
            {
                //TODO: Pass to the job instead of creating one
                botTopHitOrder.Sort(new HitDistance.SortByDistance());

                if (affectSurface)
                    CalculateGlobalOccupancy(in cellIndex, in botTopHitOrder, in dataPerRigidbody, ref occupancy);

                if (buoyancyEnabled)
                {
                    CollapseOccupanciesByRigidBody(ref dataPerRigidbody, in colliderToRbIndices, in botTopHitOrder);
                    CopyHitsToFinalArray(in dataPerRigidbody);
                }
            }
        }

        internal static void CalculateGlobalOccupancy(in int cellIndex, in NativeList<HitDistance> botTopHitOrder,
                                            in NativeArray<InteractionData> localInteractionData, ref NativeArray<float> occupancy)
        {
            float finalOcc = 0;
            float minNextDistance = 0;

            for (int i = 0; i < botTopHitOrder.Length; i++)
            {
                var interactorNativeId = botTopHitOrder[i].colliderIndex;
                var occ = localInteractionData[interactorNativeId].occupancy;
                var dist = localInteractionData[interactorNativeId].distance;

                // collider ends before the previous ended (overlapped), ignore
                if (dist + occ < minNextDistance)
                    continue;

                // if overlapped at the beginning
                if (dist < minNextDistance)
                    finalOcc += dist + occ - minNextDistance; // Add the rest of the space
                else
                    finalOcc += occ;

                minNextDistance = dist + occ;
            }

            occupancy[cellIndex] = finalOcc;
        }

        internal void CopyHitsToFinalArray(in NativeArray<InteractionData> dataPerRigidbody)
        {
            // Only the data for the collapsed rigidbodies is left. The rest is null
            for (int i = 0; i < dataPerRigidbody.Length; i++)
            {
                if (!dataPerRigidbody[i].IsNull)
                    AddHit(i, dataPerRigidbody[i]);
            }
        }

        internal unsafe void AddHit(int rbIndex, in InteractionData data)
        {
            //NOTE: We always increment for new hits. It is an unsafe operation because it is shared by all samples in the grid
            int currentNHits = Interlocked.Increment(ref ((int*)hitsPerRigidbody.GetUnsafePtr())[rbIndex]);
            if (currentNHits > nMaxCellsPerInteractor)
                return;

            InteractionDataArray.AddHit(ref interactionData, nMaxCellsPerInteractor, rbIndex,
                                        currentNHits - 1, data.cellIndex, data.occupancy, data.distance);
        }

        internal static void CollapseOccupanciesByRigidBody(ref NativeArray<InteractionData> dataPerInteractor,
                                                in NativeParallelHashMap<int, int> colliderToRbIndices, in NativeList<HitDistance> botTopHitOrder)
        {
            int nInteractors = dataPerInteractor.Length;

            // Store which is the first collider per rigid body detected to store the sum of the collapsed colliders
            var firstColliderPerRb = new NativeArray<int>(nInteractors, Allocator.Temp);
            for (int i = 0; i < nInteractors; i++)
                firstColliderPerRb[i] = -1;

            for (int i = 0; i < botTopHitOrder.Length; i++)
            {
                var colliderIndex = botTopHitOrder[i].colliderIndex;

                // Ignore colliders without rididbodies, delete data
                if (!colliderToRbIndices.TryGetValue(colliderIndex, out int rbIndex))
                {
                    dataPerInteractor[colliderIndex] = InteractionData.Null;
                    continue;
                }

                // If it is the first collider from this rb we find
                int firstIndex = firstColliderPerRb[rbIndex];
                if (firstIndex == -1)
                {
                    firstColliderPerRb[rbIndex] = colliderIndex;
                    continue;
                }

                InteractionData firstData = dataPerInteractor[firstIndex];
                InteractionData currentData = dataPerInteractor[colliderIndex];

                // No overlap, we start counting from this new collider
                if (firstData.distance + firstData.occupancy < currentData.distance)
                    firstColliderPerRb[rbIndex] = colliderIndex;

                // Overlapped fully, delete data
                else if (firstData.distance + firstData.occupancy >= currentData.distance + currentData.occupancy)
                    dataPerInteractor[colliderIndex] = InteractionData.Null;

                // Overlapped partially, add occupancy
                else if (firstData.distance + firstData.occupancy < currentData.distance + currentData.occupancy)
                {
                    float newOcc = currentData.distance + currentData.occupancy - firstData.distance;
                    dataPerInteractor[firstIndex] = new InteractionData(firstData.cellIndex, newOcc, firstData.distance);
                    dataPerInteractor[colliderIndex] = InteractionData.Null;
                }
            }

            firstColliderPerRb.Dispose();
        }

    }

    public class RaymarchUtils
    {
        //TODO WARNING Doesn't support thickness == 0 . All operations are performed in WS
        public static float CalculateOccupancy(NativeCollider collider, float4 bottomOrigin_ws, float4 upDir_ws,
                                            float volumeDepth_ws, float smoothedBorder_ws, out float hitDistance_ws)
        {
            // Adjust depth adding thickness
            bottomOrigin_ws -= upDir_ws * smoothedBorder_ws;
            volumeDepth_ws += smoothedBorder_ws * 2;

            // Hit with thickened collider from bottom
            var horizontalDistFromBottom = Raymarch(bottomOrigin_ws, upDir_ws, collider, out float4 bottomHit_ws, out float hitDistFromBottom_ws, smoothedBorder_ws);
            hitDistance_ws = hitDistFromBottom_ws;

            // No hit (Out of the water area or too far horizontally)
            if (horizontalDistFromBottom > 0 || horizontalDistFromBottom < -1 || hitDistance_ws > volumeDepth_ws)
                return 0;

            // Already inside
            if (horizontalDistFromBottom == -1)
                bottomHit_ws = bottomOrigin_ws;

            // Continue from thickened object hit to internal collider hit
            horizontalDistFromBottom = Raymarch(bottomHit_ws, upDir_ws, collider, out float4 _, out _);

            float4 topOrigin_ws = bottomOrigin_ws + upDir_ws * volumeDepth_ws;
            Raymarch(topOrigin_ws, -upDir_ws, collider, out float4 _, out float hitDistFromTop_ws, smoothedBorder_ws);
            // If we hit or are inside the collider, weight is 1. Otherwise, weight is smaller
            float weight = math.clamp(1 - horizontalDistFromBottom / smoothedBorder_ws, 0, 1);

            // Calculate occupancy with bottom and top hits
            hitDistFromTop_ws = math.clamp(hitDistFromTop_ws - smoothedBorder_ws, 0, float.MaxValue);
            hitDistFromBottom_ws = math.clamp(hitDistFromBottom_ws - smoothedBorder_ws, 0, float.MaxValue);
            float fullOcc = volumeDepth_ws - smoothedBorder_ws * 2 - hitDistFromBottom_ws - hitDistFromTop_ws;
            float occ = weight * fullOcc;

            // Fix hit distance with the weighted occupancy removed and the thickness added previously
            hitDistance_ws += (fullOcc - occ) * 0.5f - smoothedBorder_ws;

            return occ;
        }

        /// <returns> 
        /// If not hit, minimum distance to collider + thickness + contact offset. 
        /// If hit, returns 0, and the new hit pos.
        /// If already inside object or , returns -1 and the same origin as hitPos
        /// If object in the back of the ray, returns a big negative value, and same origin as hitPos.
        /// HitDist is always 0 unless there is a hit
        /// </returns>
        public static float Raymarch(in float4 origin_ws, in float4 dir_ws, in NativeCollider collider, out float4 hitPos_ws, out float hitDist_ws, float thickness = 0, float thresholdDistance = 0.01f)
        {
            float4 currentPos_ws = origin_ws;
            float minStepDistance = float.MaxValue;
            hitPos_ws = currentPos_ws;
            hitDist_ws = 0;

            float nextStepDistance = collider.NearestPointFrom(origin_ws, thickness, out float4 firstHit_ws);

            // Already inside the object (or thickened object)
            if (nextStepDistance <= thresholdDistance)
                return -1;

            // the object is in the other side and not inside. No hit.
            if (math.dot(firstHit_ws - origin_ws, dir_ws) < 0)
                return float.MinValue;

            // While next step doesn't grow and it's not too far
            while (nextStepDistance < minStepDistance)
            {
                minStepDistance = nextStepDistance;

                // Advance
                float stepSize = nextStepDistance;
                currentPos_ws.x += dir_ws.x * stepSize;
                currentPos_ws.y += dir_ws.y * stepSize;
                currentPos_ws.z += dir_ws.z * stepSize;
                hitDist_ws += nextStepDistance;

                nextStepDistance = collider.NearestPointFrom(currentPos_ws, thickness, out _);

                // object hit (or thickened object)
                if (nextStepDistance < thresholdDistance)
                {
                    hitPos_ws = currentPos_ws;
                    return 0;
                }
            }

            // Exactly in the border or no hit
            hitPos_ws = currentPos_ws;
            return minStepDistance;
        }
    }
}

#endif