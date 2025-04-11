
using UnityEngine;
using System.Collections.Generic;

#if COLLECTIONS_INSTALLED
using Unity.Collections;
#endif

#if MATHEMATICS_INSTALLED
using Unity.Mathematics;
#endif

namespace WaveMaker
{
    /// <summary>
    /// This component has to be attached to a Wave Maker Surface to show more detailed debug 
    /// information on the internal simulation of the surface.
    /// </summary>
    [RequireComponent(typeof(WaveMakerSurface))]
    public class WaveMakerSurfaceDebugger : MonoBehaviour
    {

#if MATHEMATICS_INSTALLED && BURST_INSTALLED && COLLECTIONS_INSTALLED
        
        #region Members

        public enum DrawMode
        {
            none,
            grid,
            asleepStatus,
            activeArea,
            gradients,
            normals,
            meshNormals,
            normalMapNormals,
            tangents,
            offset,
            velocity,
            acceleration,
            relativeVelocity,
            globalOccupancy,
            interactorOccupancy,
            interactionData,
            buoyantForces,
            interactorBounds
        }

        public DrawMode drawMode = DrawMode.none;
        DrawMode currentDrawMode = DrawMode.none;

        public bool showDetectionDepth = false;
        public bool printDetectedInteractors = false;
        public float rayVisualScale = 1f;
        public float offsetClamp = 2f;
        public bool occupancyNormalizedByDepth = true;

        [Tooltip("For display modes that need an interactor selected to show its information in the grid")]
        public int interactorSelected = 0;

        public List<Color> rayHitColors = new List<Color>() { Color.cyan, Color.blue, Color.green, Color.magenta };

        public WaveMakerSurface _surface;

        MeshRenderer _meshRenderer;
        Material _material;
        Material _materialBackup;
        Mesh _mesh;
        Color[] _colors;
        Color[] _colorsBackup;
        bool _materialIsOverriden = false;
        bool _initialized = false;

        #endregion

        #region Default Methods

        private void OnEnable()
        {
            _surface = GetComponent<WaveMakerSurface>();
            _surface.OnInitialized.AddListener(Initialize);
            _surface.OnUninitialized.AddListener(Uninitialize);

            if (_surface.IsInitialized)
                Initialize();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            Uninitialize();
        }

        private void OnValidate()
        {
            if (interactorSelected < 0)
                interactorSelected = 0;

            if (rayVisualScale < 0)
                rayVisualScale = 0;

            if (offsetClamp < 0)
                offsetClamp = 0;

            if (interactorSelected < 0)
                interactorSelected = 0;

            if (_surface != null && interactorSelected > _surface.nMaxInteractorsDetected - 1)
                interactorSelected = _surface.nMaxInteractorsDetected - 1;
        }

        private void LateUpdate()
        {
            if (!_initialized)
                return;

            ApplyChangeOfDrawMode(drawMode);
            Draw();
        }

        private void OnDestroy()
        {
            Uninitialize();
            _surface?.OnInitialized.RemoveListener(Initialize);
            _surface?.OnUninitialized.RemoveListener(Uninitialize);
        }

        #endregion

        #region Other Methods

        public void Initialize()
        {
            _materialIsOverriden = false;
            _meshRenderer = GetComponent<MeshRenderer>();
            _mesh = _surface.MeshManager.Mesh;
            _colors = new Color[_surface._resolution.x * _surface._resolution.z];

            ApplyChangeOfDrawMode(drawMode);

            _initialized = true;
        }

        public void Uninitialize()
        {
            if (!_initialized)
                return;

            ApplyChangeOfDrawMode(DrawMode.none);
        }

        private void Draw()
        {
            switch (currentDrawMode)
            {
                case DrawMode.none:
                    break;
                case DrawMode.grid:
                    DrawGrid();
                    break;
                case DrawMode.asleepStatus:
                    ShowAsleepStatus();
                    break;
                case DrawMode.activeArea:
                    ShowActiveArea();
                    break;
                case DrawMode.gradients:
                    DrawGradients();
                    break;
                case DrawMode.normals:
                    DrawVectorsGrid(ref _surface._normals, true);
                    break;
                case DrawMode.meshNormals:
                    var norms = _mesh.normals;
                    DrawVectorsGrid(ref norms, true);
                    break;
                case DrawMode.normalMapNormals:
                    DrawNormalMapNormals();
                    break;
                case DrawMode.tangents:
                    DrawVectorsGrid(ref _surface._tangents, true);
                    break;
                case DrawMode.offset:
                    DrawOffset();
                    break;
                case DrawMode.velocity:
                    DrawVerticalVectorsGrid(ref _surface._velocities);
                    break;
                case DrawMode.acceleration:
                    DrawVerticalVectorsGrid(ref _surface._accelerations);
                    break;
                case DrawMode.relativeVelocity:
                    if (_surface.interactionType == WaveMakerSurface.InteractionType.VelocityBased)
                        DrawVectorsGrid(ref _surface._relativeVelocities, false);
                    break;
                case DrawMode.interactionData:
                    if (_surface.interactionType == WaveMakerSurface.InteractionType.OccupancyBased)
                        ShowInteractionData();
                    break;
                case DrawMode.globalOccupancy:
                    if (_surface.interactionType == WaveMakerSurface.InteractionType.OccupancyBased)
                        ShowGlobalOccupancy();
                    break;
                case DrawMode.interactorOccupancy:
                    if (_surface.interactionType == WaveMakerSurface.InteractionType.OccupancyBased)
                        ShowInteractorOccupancy();
                    break;
                case DrawMode.buoyantForces:
                    if (_surface.interactionType == WaveMakerSurface.InteractionType.OccupancyBased)
                        ShowBuoyantForces();
                    break;
                case DrawMode.interactorBounds:
                    ShowInteractorBounds();
                    break;
            }

            if (showDetectionDepth)
                DrawDetectionDepth();

            if (printDetectedInteractors)
                PrintInteractors();
        }

        public void ApplyChangeOfDrawMode(DrawMode mode)
        {
            if (currentDrawMode == mode)
                return;

            // Leaving buoyant forces draw mode
            if (currentDrawMode == DrawMode.buoyantForces)
                _surface.DisableExportBuoyantForces();

            currentDrawMode = mode;

            // Entering buoyant forces draw mode
            if (currentDrawMode == DrawMode.buoyantForces)
                _surface.EnableExportBuoyantForces();

            if (IsDrawModeOverridingMaterial(mode))
            {
                if (!OverrideMaterial())
                    return;
            }
            else
                RestoreMaterial();
        }

        private bool IsDrawModeOverridingMaterial(DrawMode mode)
        {
            return (mode == DrawMode.asleepStatus ||
                    mode == DrawMode.activeArea ||
                    mode == DrawMode.globalOccupancy ||
                    mode == DrawMode.interactorOccupancy ||
                    mode == DrawMode.offset ||
                    mode == DrawMode.interactionData);
        }

        public void ResetColorGrid()
        {
            for (int i = 0; i < _colors.Length; i++)
            {
                _colors[i].r = 0;
                _colors[i].g = 0;
                _colors[i].b = 0;
            }
        }

        private bool OverrideMaterial()
        {
            if (_materialIsOverriden)
                return true;

            Shader shader = Shader.Find("WaveMaker/WaveMakerDebugShader");
            if (shader == null)
            {
                Utils.LogError("Can't find WaveMaker Debug Shader in the resources folder", gameObject);
                return false;
            }
            _materialBackup = _meshRenderer.material;
            _colorsBackup = _surface.MeshManager.GetMeshColorsCopy();

            _material = new Material(shader);
            _meshRenderer.sharedMaterial = _material;
            ResetColors();

            _materialIsOverriden = true;
            return true;
        }

        private void ResetColors()
        {
            for (int i = 0; i < _colors.Length; i++)
            {
                _colors[i].r = 0;
                _colors[i].g = 0;
                _colors[i].b = 0;
            }

            _surface.MeshManager.SetMeshColors(ref _colors);
        }

        private void RestoreMaterial()
        {
            if (!_materialIsOverriden)
                return;

            if (_materialBackup != null)
                _meshRenderer.sharedMaterial = _materialBackup;

            _surface.MeshManager.SetMeshColors(ref _colorsBackup);
            _colorsBackup = null;
            _materialBackup = null;
            _materialIsOverriden = false;
        }

        public void DisruptSurface()
        {
            if (!enabled)
                return;

            for (int i = 0; i < 10; i++)
            {
                var index = UnityEngine.Random.Range(0, _surface._velocities.Length);
                _surface?.SetHeightOffset(index, 1);
            }
        }

        #endregion

        #region Draw Methods

        private void ShowGlobalOccupancy()
        {
            float depth = _surface.detectionDepth;
            var occupancy = _surface.Occupancy;
            for (int i = 0; i < _colors.Length; i++)
            {
                float occ = occupancy[i];

                if (occ > depth || occ < 0)
                {
                    _colors[i] = Color.red;
                    continue;
                }

                if (occupancyNormalizedByDepth)
                    occ /= depth;

                _colors[i] = new Color(occ, occ, occ);
            }

            _surface.MeshManager.SetMeshColors(ref _colors);
        }

        private void ShowInteractorOccupancy()
        {
            ResetColorGrid();
            var data = _surface._interactionData;
            float depth = _surface.detectionDepth;
            var d = new InteractionData();

            // Draw only interation data for the selected interactor
            for (int i = 0; i < _surface.nMaxCellsPerInteractor; i++)
            {
                InteractionDataArray.GetData(in data,  _surface.nMaxCellsPerInteractor, interactorSelected, i, ref d);
                if (d.IsNull)
                    break;

                var occ = data[i].occupancy;

                if (_colors[d.cellIndex] == Color.red || occ < 0 || occ > depth)
                {
                    _colors[d.cellIndex] = Color.red;
                    continue;
                }

                if (occupancyNormalizedByDepth)
                    occ /= depth;

                Color col = new Color(_colors[d.cellIndex].r + occ, _colors[d.cellIndex].g + occ, _colors[d.cellIndex].b + occ);

                if (col.r > 1 || col.g > 1 || col.b > 1)
                    col = Color.blue;

                _colors[d.cellIndex] = col;
            }
            _surface.MeshManager.SetMeshColors(ref _colors);
        }

        private void ShowBuoyantForces()
        {
            if (!_surface.exportForces)
                return;

            for (int i = 0; i < _surface._buoyantForces.Length; i++)
            {
                float4 force = _surface._buoyantForces[i].force_ws;
                if (force.x != 0 || force.y != 0 || force.z != 0)
                    Debug.DrawRay(_surface._buoyantForces[i].hitPos_ws.xyz, force.xyz * rayVisualScale, Color.red);
            }
        }

        private void ShowInteractorBounds()
        {
            foreach (var collider in _surface._interactorColliders)
            {
                float4 min = collider.boundsMin;
                float4 max = collider.boundsMax;
                //Utils.TransformBounds(ref min, ref max, in _surface._w2lTransformMatrix);
                DrawWireframeBox(min, max);
            }
        }

        private void ShowAsleepStatus()
        {
            var counter = _surface._asleepCounterLimit - _surface._asleepCounter;
            var normalization = Mathf.Clamp01((float)counter / _surface._asleepCounterLimit);
            
            if (_colors[0].r != normalization)
            {
                for (int i = 0; i < _colors.Length; i++)
                {
                    _colors[i].r = normalization;
                    _colors[i].g = normalization;
                    _colors[i].b = normalization;
                }

                _surface.MeshManager.SetMeshColors(ref _colors);
            }
        }

        private void ShowActiveArea()
        {
            ResetColorGrid();

            foreach (var interactor in _surface._interactorsDetected)
                interactor.UpdateNativeCollider();

            _surface.CalculateMinimumSharedAreaOfInteractors(out IntegerPair areaResolution, out int xOffset, out int zOffset);

            if (areaResolution.x > 0 && areaResolution.z > 0)
            {
                int sampleX, sampleZ;

                for (int i = 0; i < _colors.Length; i++)
                {
                    Utils.FromIndexToSampleIndices(i, _surface._resolution, out sampleX, out sampleZ);

                    bool insideActiveArea = sampleX >= xOffset &&
                                            sampleZ >= zOffset &&
                                            sampleX < xOffset + areaResolution.x &&
                                            sampleZ < zOffset + areaResolution.z;

                    _colors[i].r = insideActiveArea ? 1 : 0;
                    _colors[i].g = insideActiveArea ? 1 : 0;
                    _colors[i].b = insideActiveArea ? 1 : 0;
                }
            }

            _surface.MeshManager.SetMeshColors(ref _colors);
        }

        private void ShowInteractionData()
        {
            ResetColorGrid();

            var data = _surface._interactionData;
            InteractionData d = new InteractionData();

            // Draw only interaction data for the selected interactor
            for (int i = 0; i < _surface.nMaxCellsPerInteractor; i++)
            {
                InteractionDataArray.GetData(in data, _surface.nMaxCellsPerInteractor, interactorSelected, i, ref d);
                if (d.IsNull)
                    break;

                _colors[d.cellIndex].r += 1;
                _colors[d.cellIndex].g += 1;
                _colors[d.cellIndex].b += 1;

                var pos_ws = Utils.GetLocalPositionFromSample(d.cellIndex, _surface._resolution, _surface._sampleSize_ls);
                pos_ws.y = -_surface.detectionDepth;

                var l2wMat = _surface._l2wTransformMatrix;
                pos_ws = new float4(math.mul(l2wMat, new float4(pos_ws.xyz, 1)).xyz, 0);
                var dir_ws = math.mul(l2wMat, new float4(0, d.distance, 0, 0));

                Debug.DrawRay(pos_ws.xyz, dir_ws.xyz, Color.green);
            }

            _surface.MeshManager.SetMeshColors(ref _colors);
        }

        private void DrawDetectionDepth()
        {
            Utils.DrawDetectionDepth(_surface);
        }

        public void PrintInteractors()
        {
            if (!enabled)
                return;

            foreach (var interactor in _surface._interactorsDetected)
            {
                if (!_surface._colliderIdsToIndices.TryGetValue(interactor.NativeCollider.instanceId, out int index))
                    Utils.Log(string.Format("Interactor {0} NOT Stored in the indices list of this surface. Error!.", interactor.gameObject), gameObject);
                else
                    Utils.Log(string.Format("Interactor {0}. Instance ID: {1}. Index: {2}", interactor.gameObject, interactor.NativeCollider.instanceId, index), gameObject);
            }
        }

        private void DrawGrid()
        {
            var botLeft = _surface.GetPositionFromSample(0, true, false, true);
            var botRight = _surface.GetPositionFromSample(_surface._resolutionGhost.x - 1, true, false, true);
            int topIndex = Utils.FromSampleIndicesToIndex(_surface._resolutionGhost, 0, _surface._resolutionGhost.z - 1);
            var topLeft = _surface.GetPositionFromSample(topIndex, true, false, true);

            var dirX = math.normalize(botRight - botLeft);
            var dirZ = math.normalize(topLeft - botLeft);

            var sampleSizeX = _surface.transform.localScale.x * _surface._sampleSize_ls.x;
            var sampleSizeZ = _surface.transform.localScale.z * _surface._sampleSize_ls.y;

            // For each row
            var posA = botLeft;
            var posB = botRight;
            for (int z = 0; z < _surface._resolutionGhost.z; z++)
            {
                Debug.DrawLine(posA.xyz, posB.xyz, Color.red);
                posA += dirZ * sampleSizeZ;
                posB += dirZ * sampleSizeZ;
            }

            // For each col
            posA = botLeft;
            posB = topLeft;
            for (int x = 0; x < _surface._resolutionGhost.x; x++)
            {
                Debug.DrawLine(posA.xyz, posB.xyz, Color.red);
                posA += dirX * sampleSizeX;
                posB += dirX * sampleSizeX;
            }

        }

        private void DrawOffset()
        {
            ResetColorGrid();

            var heights = _surface._heights;
            var res = _surface._resolution;
            var gRes = _surface._resolutionGhost;

            for (int i = 0; i < _colors.Length; i++)
            {
                int gI = Utils.FromNoGhostIndexToGhostIndex(i, in res, in gRes);
                var offset = heights[gI];

                if (offset > offsetClamp)
                    offset = offsetClamp;

                if (offset < -offsetClamp)
                    offset = -offsetClamp;

                var normalized = offset / offsetClamp;

                _colors[i].r = offset < 0 ? -normalized : 0;
                _colors[i].g = 0;
                _colors[i].b = offset > 0 ? normalized : 0;
            }

            _surface.MeshManager.SetMeshColors(ref _colors);
        }

        private void DrawGradients()
        {
            var gradients = _surface._gradients;
            
            var transformMat = _surface._l2wTransformMatrix;
            var resolution = _surface._resolution;
            var sampleSize = _surface._sampleSize_ls;

            for (int i = 0; i < gradients.Length; i++)
            {
                var currentPos = Utils.GetLocalPositionFromSample(i, resolution, sampleSize);
                currentPos.y = _surface.GetHeight(i);
                currentPos[3] = 1;

                var gradient = gradients[i];
                gradient.y = 0;
                gradient.w = 0; // remove translation

                Debug.DrawRay(math.mul(transformMat, currentPos).xyz,
                              math.mul(transformMat, gradient * rayVisualScale).xyz,
                              Color.red);
            }
        }

        private void DrawVectorsGrid(ref NativeArray<Vector3> array, bool offsetByHeight)
        {
            var transformMat = _surface._l2wTransformMatrix;
            var resolution = _surface._resolution;
            var sampleSize = _surface._sampleSize_ls;

            for (int i = 0; i < array.Length; i++)
            {
                var currentPos = Utils.GetLocalPositionFromSample(i, resolution, sampleSize);

                if (offsetByHeight)
                    currentPos.y = _surface.GetHeight(i);
                currentPos.w = 1;

                float4 vec = new float4(array[i], 0);

                Debug.DrawRay(math.mul(transformMat, currentPos).xyz,
                              math.mul(transformMat, vec * rayVisualScale).xyz,
                              Color.red);
            }
        }

        private void DrawVectorsGrid(ref NativeArray<Vector4> array, bool offsetByHeight)
        {
            var transformMat = _surface._l2wTransformMatrix;
            var resolution = _surface._resolution;
            var sampleSize = _surface._sampleSize_ls;

            for (int i = 0; i < array.Length; i++)
            {
                var currentPos = Utils.GetLocalPositionFromSample(i, resolution, sampleSize);

                if (offsetByHeight)
                    currentPos.y = _surface.GetHeight(i);
                currentPos.w = 1;
                Debug.DrawRay(math.mul(transformMat, currentPos).xyz,
                              math.mul(transformMat, array[i] * rayVisualScale).xyz,
                              Color.red);
            }
        }

        // TODO: make a generic method for array and nativearray
        private void DrawVectorsGrid(ref Vector3[] array, bool offsetByHeight)
        {
            var transformMat = _surface._l2wTransformMatrix;
            var resolution = _surface._resolution;
            var sampleSize = _surface._sampleSize_ls;

            for(int i = 0; i < array.Length; i++)
            {
                var currentPos = Utils.GetLocalPositionFromSample(i, resolution, sampleSize);

                if(offsetByHeight)
                    currentPos.y = _surface.GetHeight(i);
                currentPos.w = 1;

                float4 vec = new float4(array[i], 0);

                Debug.DrawRay(math.mul(transformMat, currentPos).xyz,
                              math.mul(transformMat, vec * rayVisualScale).xyz,
                              Color.red);
            }
        }

        private void DrawVerticalVectorsGrid(ref NativeArray<float> array)
        {
            var transformMat = _surface._l2wTransformMatrix;
            var resolution = _surface._resolution;
            var sampleSize = _surface._sampleSize_ls;

            for (int i = 0; i < array.Length; i++)
            {
                if (math.abs(array[i]) < 0.00001)
                    continue;

                var vect = float4.zero;
                vect.y = array[i];

                var currentPos = Utils.GetLocalPositionFromSample(i, resolution, sampleSize);
                currentPos.y = _surface.GetHeight(i);
                currentPos[3] = 1;

                Debug.DrawRay(math.mul(transformMat, currentPos).xyz,
                              math.mul(transformMat, vect * rayVisualScale).xyz,
                              Color.red);
            }
        }

        private void DrawVectorsGrid(ref NativeArray<float4> array, bool offsetByHeight)
        {
            var transformMat = _surface._l2wTransformMatrix;
            var resolution = _surface._resolution;
            var sampleSize = _surface._sampleSize_ls;

            for (int i = 0; i < array.Length; i++)
            {
                var data = array[i];
                data.w = 0; // remove translation

                if (math.lengthsq(data) < 0.0001f)
                    continue;

                var rayOrigin = Utils.GetLocalPositionFromSample(i, resolution, sampleSize);
                rayOrigin.w = 1;

                if (offsetByHeight)
                    rayOrigin.y = _surface.GetHeight(i);

                Debug.DrawRay(math.mul(transformMat, rayOrigin).xyz,
                              math.mul(transformMat, data * rayVisualScale).xyz,
                              Color.red);
            }
        }

        private void DrawVectorsFromNormalMap(ref Texture2D normalMap, ref Vector2[] uvs)
        {
            var transformMat = _surface._l2wTransformMatrix;
            var resolution = _surface._resolution;
            var sampleSize = _surface._sampleSize_ls;
            var pixels = normalMap.GetPixels();

            for(int i = 0; i < uvs.Length; i++)
            {
                var data = new float3(pixels[i].r, pixels[i].b, pixels[i].g) * 2 - 1;
                
                if(math.lengthsq(data) < 0.0001f)
                    continue;

                var rayOrigin = Utils.GetLocalPositionFromSample(i, resolution, sampleSize);
                rayOrigin.w = 1;
                rayOrigin.y = _surface.GetHeight(i);

                Debug.DrawRay(math.mul(transformMat, rayOrigin).xyz,
                              math.mul(transformMat, new float4(data, 0)* rayVisualScale).xyz,
                              Color.red);
            }
        }

        private void DrawWireframeBox(float4 min_ws, float4 max_ws)
        {
            float sizeX = max_ws.x - min_ws.x;
            float sizeY = max_ws.y - min_ws.y;
            float sizeZ = max_ws.z - min_ws.z;

            Debug.DrawRay(min_ws.xyz, Vector3.right * sizeX);
            Debug.DrawRay(min_ws.xyz, Vector3.up * sizeY);
            Debug.DrawRay(min_ws.xyz, Vector3.forward * sizeZ);

            Debug.DrawRay(max_ws.xyz, Vector3.left* sizeX);
            Debug.DrawRay(max_ws.xyz, Vector3.down * sizeY);
            Debug.DrawRay(max_ws.xyz, Vector3.back * sizeZ);

            Debug.DrawRay((Vector3)min_ws.xyz + Vector3.up * sizeY, Vector3.right * sizeX);
            Debug.DrawRay((Vector3)min_ws.xyz + Vector3.up * sizeY, Vector3.forward * sizeZ);

            Debug.DrawRay((Vector3)max_ws.xyz + Vector3.down * sizeY, Vector3.left * sizeX);
            Debug.DrawRay((Vector3)max_ws.xyz + Vector3.down * sizeY, Vector3.back * sizeZ);

            Debug.DrawRay((Vector3)min_ws.xyz + Vector3.forward * sizeZ, Vector3.up * sizeY);
            Debug.DrawRay((Vector3)min_ws.xyz + Vector3.right * sizeX, Vector3.up * sizeY);
        }

        private void DrawNormalMapNormals()
        {
            if(_surface.SmoothedNormals)
            {
                var tex = _meshRenderer.sharedMaterial.GetTexture("_BumpMap") as RenderTexture;

                // HRDP
                if (tex == null)
                    tex = _meshRenderer.sharedMaterial.GetTexture("_NormalMap") as RenderTexture;

                if(tex == null)
                    return;

                var tex2D = new Texture2D(tex.width, tex.height);
                RenderTexture.active = tex;
                tex2D.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                tex2D.Apply();
                var uvs = _mesh.uv4;
                DrawVectorsFromNormalMap(ref tex2D, ref uvs);
            }
        }

        #endregion
#endif
    }
}
