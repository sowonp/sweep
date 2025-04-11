
#if COLLECTIONS_INSTALLED
using Unity.Collections;
#endif

namespace WaveMaker
{
    [System.Serializable]
    public struct IntegerPair
    {
        public int x;
        public int z;

        public IntegerPair(int inX, int inZ) { x = inX; z = inZ; }

        public static bool operator !=(IntegerPair first, IntegerPair second) => first.x != second.x || first.z != second.z;
        public static bool operator ==(IntegerPair first, IntegerPair second) => first.x == second.x && first.z == second.z;
        public override int GetHashCode() => 0; //TODO
        public override bool Equals(object obj) => obj is IntegerPair pair && obj != null && x == pair.x && z == pair.z;
    }

    /// <summary>
    /// Defines a hit of the associated interactor into the surface.
    /// There is one hit per cell, and the occupied depth and distance from the bottom of the volume are stored too.
    /// Cell index will be -1 to mark when there are no more hits in the list of hits
    /// </summary>
    public struct InteractionData
    {
        public int cellIndex;

        /// <summary> Distance occupied in this cell. Expressed in local space </summary>
        public float occupancy;

        /// <summary> Distance from the bottom of the volume in local space</summary>
        public float distance; 

        public static InteractionData Null => new InteractionData(-1, 0, 0);

        public bool IsNull => cellIndex == -1;

        public InteractionData(int cellIndex, float occupation, float distanceFromBottom)
        {
            this.cellIndex = cellIndex;
            this.occupancy = occupation;
            this.distance = distanceFromBottom;
        }

        public void CopyData(ref InteractionData outData)
        {
            outData.cellIndex = cellIndex;
            outData.occupancy = occupancy;
            outData.distance = distance;
        }
    }

#if MATHEMATICS_INSTALLED && BURST_INSTALLED && COLLECTIONS_INSTALLED

    public static class InteractionDataArray
    {
        public static void CreateAndInitialize(int nMaxInteractorsDetected, int nMaxCellsPerInteractor, out NativeArray<InteractionData> data)
        {
            int nItems = nMaxInteractorsDetected * nMaxCellsPerInteractor;
            data = new NativeArray<InteractionData>(nItems, Allocator.Persistent);
            for (int i = 0; i < data.Length; i += nMaxCellsPerInteractor)
                data[i] = InteractionData.Null;
        }

        /// <summary>
        /// Reset first cell of each object in the interaction data. This is detected as 
        /// there are no hits for this interactor anymore
        /// </summary>
        public static void Reset(ref NativeArray<InteractionData> data, int nMaxCellsPerInteractor)
        {
            // Set first on each group to null
            for (int i = 0; i < data.Length; i += nMaxCellsPerInteractor)
                data[i] = InteractionData.Null;

            // Set last on each group to null to detect if limit reached
            for (int i = nMaxCellsPerInteractor - 1; i < data.Length; i += nMaxCellsPerInteractor)
                data[i] = InteractionData.Null;
        }

        public static void SetNull(ref NativeArray<InteractionData> array, int nMaxCellsPerInteractor, int interactorIndex, int hitIndex)
        {
            if (hitIndex >= nMaxCellsPerInteractor)
                throw new System.ArgumentException("Trying to modify a hit outside of the limit of hits available for this interactor");
            
            array[interactorIndex * nMaxCellsPerInteractor + hitIndex] = InteractionData.Null;
        }

        public static void GetData(in NativeArray<InteractionData> array, int nMaxCellsPerInteractor, int interactorIndex, int hitIndex, ref InteractionData inOutData)
        {
            if (hitIndex >= nMaxCellsPerInteractor)
                throw new System.ArgumentException("Trying to get data for a hit outside of the limit of hits available for this interactor");

            array[interactorIndex * nMaxCellsPerInteractor + hitIndex].CopyData(ref inOutData);
        }

        public static void AddHit(ref NativeArray<InteractionData> array, int nMaxCellsPerInteractor, 
                                  int interactorIndex, int hitIndex, int cellIndex, float occupancy, float distance)
        {
            if (hitIndex >= nMaxCellsPerInteractor)
                throw new System.ArgumentException("Trying to add a hit outside of the limit of hits available for this interactor");

            int index = interactorIndex * nMaxCellsPerInteractor + hitIndex;
            var d = array[index];
            d.cellIndex = cellIndex;
            d.occupancy = occupancy;
            d.distance = distance;
            array[index] = d;
        }

        public static bool HasReachedCellLimit(in NativeArray<InteractionData> array, int nMaxCellsPerInteractor, int interactorIndex)
        {
            int start = interactorIndex * nMaxCellsPerInteractor;
            return !array[start + nMaxCellsPerInteractor - 1].IsNull;
        }
        
        /// <summary>
        /// Has the given interactor hit the given cell?
        /// </summary>
        public static bool HasHit(in NativeArray<InteractionData> array, int nMaxCellsPerInteractor, int interactorIndex, int cellIndex, out int hitIndex)
        {
            // TODO: Find a faster way to do this
            int start = interactorIndex * nMaxCellsPerInteractor;
            hitIndex = -1;

            for (int i = start; i < start + nMaxCellsPerInteractor; i++)
            {
                if (array[i].IsNull)
                    return false;
                else if (array[i].cellIndex == cellIndex)
                {
                    hitIndex = i - start;
                    return true;
                }
            }
            return false;
        }
    }

#endif
}