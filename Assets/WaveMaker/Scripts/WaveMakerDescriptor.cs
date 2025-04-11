using System;
using UnityEngine;
using Unity.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WaveMaker
{
    [HelpURL("http://wavemaker.lidia-martinez.com/")]
    [CreateAssetMenu(fileName = "WaveMakerDescriptor", menuName = "WaveMaker Descriptor", order = 10)]
    public class WaveMakerDescriptor : ScriptableObject
    {
        public event EventHandler OnResolutionChanged;
        public event EventHandler OnDestroyed;

        [SerializeField] bool[] fixedGrid;

        [SerializeField] IntegerPair _resolution = new IntegerPair(50, 50);

#if MATHEMATICS_INSTALLED && BURST_INSTALLED && COLLECTIONS_INSTALLED

        public IntegerPair Resolution { get; private set; }

        /// <summary>Number of vertices/samples along the X axis </summary>
        public int ResolutionX {
            get => _resolution.x;
            set 
            {
                if (_resolution.x != value)
                    SetResolution(value, ResolutionZ);
            }
        }

        /// <summary>Number of vertices/samples along the Z axis</summary>
        public int ResolutionZ {
            get => _resolution.z;
            set
            {
                if (_resolution.z != value)
                    SetResolution(ResolutionX, value);
            }
        }

        [HideInInspector]
        public Color defaultColor = Color.white;

        [HideInInspector]
        public Color fixedColor = Color.black;
        
        public bool IsInitialized => _isInitialized;
        public static int MaxVertices => _maxVertices;
        public static int MinResolution => _minResolution;

        // Non serialized data that is used to be modified during playmode and other processes. A Request and Release methods are
        // needed to be called to modify the shared grid.
        public ref readonly NativeArray<int> SharedFixedGrid => ref sharedFixedGrid;
        int sharedFixedGridCounter;
        NativeArray<int> sharedFixedGrid;

        bool _isInitialized = false;

        IntegerPair _oldResolution;
        const int _maxVertices = 65536;
        const int _minResolution = 3;

        private void Awake()
        {
            _isInitialized = false;
        }

        private void OnEnable()
        {
            _oldResolution = _resolution;
            
            if (fixedGrid == null)
                fixedGrid = new bool[ResolutionX * ResolutionZ];

            sharedFixedGridCounter = 0;

            _isInitialized = true;
        }

        /// <summary>
        /// Call this method before calling any other method that modifies the data of the descriptor. Call the release method after you finish.
        /// </summary>
        /// <returns></returns>
        public ref readonly NativeArray<int> RequestData()
        {
            if (!sharedFixedGrid.IsCreated)
            {
                sharedFixedGrid = new NativeArray<int>(ResolutionX * ResolutionZ, Allocator.Persistent);
                CopySerializedDataToNativeData();
            }
            sharedFixedGridCounter++;
            return ref sharedFixedGrid;
        }

        /// <summary>
        /// Call this method after modifying the descriptor data via any method. Before modifying, you have to call RequestData.
        /// This will store the changed data into the serialized information and set the project to dirty.
        /// </summary>
        public void ReleaseData()
        {
            if (sharedFixedGridCounter > 0)
                sharedFixedGridCounter--;

            if (sharedFixedGridCounter == 0 && sharedFixedGrid.IsCreated)
            {
                CopyNativeDataToSerializedData();
                sharedFixedGrid.Dispose();
            }
        }

        /// <summary>
        /// If you have requested data, and you dont want to release it but the data changed in memory 
        /// has to be serialized and the scene set to dirty, call this
        /// </summary>
        public void ApplyData()
        {
            if (sharedFixedGridCounter <= 0)
            {
                Utils.LogError("Cannot apply data to Descriptor, the shared data has not been requested before.");
                return;
            }

            CopyNativeDataToSerializedData();
        }

        /// <summary>
        /// Use this function to fix and unfix samples on the grid. Dont forget to call RequestData and ReleaseData when modifying the fixed grid.
        /// </summary>
        /// <param name="x">0 to ResolutionX - 1</param>
        /// <param name="z">0 to ResolutionZ - 1</param>
        /// <param name="isFixed">New fixed status</param>
        public void SetFixed(int x, int z, bool isFixed)
        {
            if (x < 0 || x >= ResolutionX || z < 0 || z >= ResolutionZ)
            {
                Utils.LogError("Cannot set the fixed status to the given sample. It is out of bounds. " + x + " - " + z);
                return;
            }

            SetFixed(Utils.FromSampleIndicesToIndex(Resolution, in x, in z), isFixed);
        }

        /// <summary>
        /// Dont forget to call RequestData and ReleaseData when modifying the fixed grid.
        /// </summary>
        public void SetFixed(int index, bool isFixed)
        {
            if (index >= fixedGrid.Length || index < 0)
            {
                Utils.LogError(string.Format("Cannot set the fixed status to the given sample index {0}. It is out of bounds.", index));
                return;
            }

            if (sharedFixedGridCounter <= 0)
            {
                Utils.LogError("Before modifying the fixed grid, make sure to call the RequestData method on the Descriptor and release when finished.");
                return;
            }

            sharedFixedGrid[index] = isFixed ? 1 : 0;
        }

        ///<summary>This reads the status of the serialized data. If you changed the fixed grid you should call request and release methods to store the changes first.
        ///If you are using this very often, then it is recommended to grab the reference of the fixeGrid array using the property</summary>
        ///<returns>True if the status of the given sample is fixed or not.</returns>
        public bool IsFixed(int index)
        {
            if (index < 0 || index >= fixedGrid.Length)
            {
                Utils.LogError("Cannot get the fixed status to the given sample. It is out of bounds.");
                return true;
            }

            return fixedGrid[index];
        }

        /// <summary>
        /// Change resolution of the descriptor. This will make the whole grid regenerate
        /// </summary>
        public void SetResolution(int newResolutionX, int newResolutionZ)
        {
            //TODO: can this happen? During simulation the resource is reserved.
            if (sharedFixedGridCounter > 0)
            {
                Utils.LogError("Cannot change the size of the resolution if there is still somebody using the shared fixed grid or in play mode. Please, make sure you call Request and Release around the use of the resource.");
                return;
            }

            if (newResolutionX * newResolutionZ > _maxVertices)
            {
                if (newResolutionX > newResolutionZ)
                    newResolutionX = newResolutionZ / _maxVertices;
                
                if (newResolutionZ > newResolutionX)
                    newResolutionZ = newResolutionX / _maxVertices;

                Utils.LogError("Descriptor resolution cannot generate a mesh with more than (" + _maxVertices + "). Clamping biggest resolution.");
            }

            if (newResolutionX < _minResolution || newResolutionZ < _minResolution)
            {
                newResolutionX = newResolutionX < _minResolution? _minResolution: newResolutionX;
                newResolutionZ = newResolutionZ < _minResolution? _minResolution: newResolutionZ;
                Utils.LogError("Descriptor resolution cannot be less than " + _minResolution + ". Clamping.");
            }

            if (_resolution.x == newResolutionX && _resolution.z == newResolutionZ)
                return; 

            _oldResolution = _resolution;
            _resolution = new IntegerPair(newResolutionX, newResolutionZ);

            UpdateFixedGridSizes();
            OnResolutionChanged?.Invoke(this, null);
        }

        /// <summary>
        /// It will set to fixed all border samples
        /// </summary>
        public void FixBorders()
        {
            if (sharedFixedGridCounter <= 0)
            {
                Utils.LogError("Before modifying the fixed grid, make sure to call the RequestData method on the Descriptor and release when finished.");
                return;
            }

            for (int x = 0; x < ResolutionX; x++)
                for (int z = 0; z < ResolutionZ; z++)
                    if (x == 0 || z == 0 || x == ResolutionX - 1 || z == ResolutionZ - 1)
                        sharedFixedGrid[ResolutionX * z + x] = 1;
        }

        /// <summary>
        /// Set all samples to fixed or unfixed status
        /// </summary>
        public void SetAllFixStatus(bool newValue = false)
        {
            if (sharedFixedGridCounter <= 0)
            {
                Utils.LogError("Before modifying the fixed grid, make sure to call the RequestData method on the Descriptor and release when finished.");
                return;
            }

            for (int i = 0; i < sharedFixedGrid.Length; i++)
                sharedFixedGrid[i] = newValue? 1: 0;
        }

        ///<summary>It makes the fixed areas to grow by one cell around them.
        /// Warning: This reads the status of the serialized data. If you changed the fixed grid you should call request and release methods to store the changes first.
        ///If you are using this very often, then it is recommended to grab the reference of the fixeGrid array using the property</summary>
        public void DilateFixedCells()
        {
            for(int z = 0; z < ResolutionZ; z++)
            {
                for(int x = 0; x < ResolutionX; x++)
                {
                    int centerIndex = Utils.FromSampleIndicesToIndex(_resolution, x, z);

                    // already fixed
                    if(sharedFixedGrid[centerIndex] == 1)
                        continue;

                    // Check cells around
                    for(int zoff = -1; zoff <= 1; zoff++)
                    {
                        for(int xoff = -1; xoff <= 1; xoff++)
                        {
                            int otherX = x + xoff;
                            int otherZ = z + zoff;

                            // out of bounds
                            if(otherX < 0 || otherZ < 0 || otherX >= _resolution.x || otherZ >= _resolution.z)
                                continue;

                            // if the other is fixed, mark this one as future fixed
                            if(sharedFixedGrid[Utils.FromSampleIndicesToIndex(_resolution, otherX, otherZ)] == 1)
                            {
                                sharedFixedGrid[centerIndex] = 2;
                                continue;
                            }

                        }
                    }
                }
            }

            // Set marked cells as fixed
            for(int z = 0; z < ResolutionZ; z++)
                for(int x = 0; x < ResolutionX; x++)
                {
                    int index = Utils.FromSampleIndicesToIndex(_resolution, x, z);
                    if(sharedFixedGrid[index] == 2)
                        sharedFixedGrid[index] = 1;
                }
        }

        ///<summary>It makes the fixed areas to be reduced by one cell.
        ///Warning: This reads the status of the serialized data. If you changed the fixed grid you should call request and release methods to store the changes first.
        ///If you are using this very often, then it is recommended to grab the reference of the fixeGrid array using the property</summary>
        public void ErodeFixedCells()
        {
            for(int z = 0; z < ResolutionZ; z++)
            {
                for(int x = 0; x < ResolutionX; x++)
                {
                    int centerIndex = Utils.FromSampleIndicesToIndex(_resolution, x, z);

                    // already fixed
                    if(sharedFixedGrid[centerIndex] == 0)
                        continue;

                    bool fixedToBeUnfixed = false;

                    // Check if any cells around are unfixed
                    for(int zoff = -1; zoff <= 1; zoff++)
                    {
                        for(int xoff = -1; xoff <= 1; xoff++)
                        {
                            int otherX = x + xoff;
                            int otherZ = z + zoff;

                            // out of bounds
                            if(otherX < 0 || otherZ < 0 || otherX >= _resolution.x || otherZ >= _resolution.z)
                                continue;

                            if(sharedFixedGrid[Utils.FromSampleIndicesToIndex(_resolution, otherX, otherZ)] == 0)
                            {
                                fixedToBeUnfixed = true;
                                break;
                            }
                        }

                        if(fixedToBeUnfixed)
                            break;
                    }

                    if(fixedToBeUnfixed)
                        sharedFixedGrid[centerIndex] = 2;
                }
            }

            // Set marked cells as fixed
            for(int z = 0; z < ResolutionZ; z++)
                for(int x = 0; x < ResolutionX; x++)
                {
                    int index = Utils.FromSampleIndicesToIndex(_resolution, x, z);
                    if(sharedFixedGrid[index] == 2)
                        sharedFixedGrid[index] = 0;
                }
        }

        /// <summary>
        /// will update the fixed grid changing the resolution. 
        /// </summary>
        /// <param name="copyPreviousStatus">Current values will be kept if it grows or is reduced, adding unfixed values if growing</param>
        private void UpdateFixedGridSizes()
        {
            if (_oldResolution.x == ResolutionX && _oldResolution.z == ResolutionZ)
                return;

            //TODO: We could use the original array using other numbers than 0 and 1. Take adavantage of it not being a bool array
            bool[] fixedGridAux = fixedGrid;
            fixedGrid = new bool[ResolutionX * ResolutionZ];

            // Copy all values from the old one to the new one
            for (int z = 0; z < ResolutionZ; z++)
                for (int x = 0; x < ResolutionX; x++)
                {
                    int newIndex = Utils.FromSampleIndicesToIndex(_resolution, x, z);
                    int oldIndex = Utils.FromIndexToScaledIndex(newIndex, _resolution, _oldResolution);
                    fixedGrid[newIndex] = fixedGridAux[oldIndex];
                }

            SetModifiedAndDirty();
        }

        private void SetModifiedAndDirty()
        {
            #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            #endif
        }

        private void CopySerializedDataToNativeData()
        {
            for (int i = 0; i < fixedGrid.Length; i++)
                sharedFixedGrid[i] = fixedGrid[i] ? 1 : 0;
        }

        private void CopyNativeDataToSerializedData()
        {
            for (int i = 0; i < fixedGrid.Length; i++)
                fixedGrid[i] = sharedFixedGrid[i] == 1;

            SetModifiedAndDirty();
        }

        /// <summary>
        /// TODO: Improve design. This is only used for the paint mode, it makes the drawing process more efficient. 
        /// </summary>
        internal void CopyNativeDataToSerializedData(IntegerPair areaSize, IntegerPair areaOffset)
        {
            for (int z = areaOffset.z; z <= areaOffset.z + areaSize.z; ++z)
                for (int x = areaOffset.x; x <= areaOffset.x + areaSize.x; ++x)
                {
                    if (x >= ResolutionX || z >= ResolutionZ)
                        continue;

                    var i = Utils.FromSampleIndicesToIndex(_resolution, x, z);
                    fixedGrid[i] = sharedFixedGrid[i] == 1;
                }
            SetModifiedAndDirty();
        }

        public Texture2D ToTexture()
        {
            if (sharedFixedGridCounter > 0)
                CopyNativeDataToSerializedData();
            
            var tex = new Texture2D(ResolutionX, ResolutionZ, TextureFormat.RGBA32, 0, false);
            
            for (int z = 0; z < ResolutionZ; z++)
                for (int x = 0; x < ResolutionX; x++)
                {
                    int index = Utils.FromSampleIndicesToIndex(_resolution, x, z);
                    tex.SetPixel(x, z, new Color(fixedGrid[index]? 1 : 0, 0, 0, 1));
                }

            tex.Apply();
            return tex;
        }

        public void FromTexture(Texture2D tex)
        {
            SetResolution(tex.width, tex.height);

            for (int z = 0; z < ResolutionZ; z++)
                for (int x = 0; x < ResolutionX; x++)
                {
                    int index = Utils.FromSampleIndicesToIndex(_resolution, x, z);
                    Color col = tex.GetPixel(x, z);
                    fixedGrid[index] = col.r > 0;
                }

            if (sharedFixedGridCounter > 0)
                CopySerializedDataToNativeData();

            SetModifiedAndDirty();
        }

        private void OnDestroy()
        {
            //TODO: NOT CALLED! Using AssetModificationProcessor instead
            OnDestroyed?.Invoke(this, null);
        }
#endif
    }

#if UNITY_EDITOR && MATHEMATICS_INSTALLED && BURST_INSTALLED && COLLECTIONS_INSTALLED
    public class WaveMakerDescriptorDeleteDetector : UnityEditor.AssetModificationProcessor
        {
            static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions opt)
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(WaveMakerDescriptor))
                {
                    foreach (var surf in GameObject.FindObjectsByType<WaveMakerSurface>(FindObjectsSortMode.None))
                    {
                        if (surf.Descriptor != null && path == AssetDatabase.GetAssetPath(surf.Descriptor.GetInstanceID()))
                            surf.Descriptor = null;
                    }
                }
                return AssetDeleteResult.DidNotDelete;
            }
        }
#endif
}

