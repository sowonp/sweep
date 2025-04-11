using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if MATHEMATICS_INSTALLED && BURST_INSTALLED && COLLECTIONS_INSTALLED
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
#endif

namespace WaveMaker.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(WaveMakerSurface))]
    public class WaveMakerSurfaceEditor : UnityEditor.Editor
    {
#if MATHEMATICS_INSTALLED && BURST_INSTALLED && COLLECTIONS_INSTALLED

        float _auxWidth, _auxDepth;
        bool _haveSameSize;
        bool showWaveSimulationProperties = true;
        bool showInteractionProperties = true;
        bool showCellProperties = false;
        bool showOtherSettings = false;
        bool _meshSizeChanged = false;

        List<WaveMakerSurface> _surfaces;

        // General properties
        SerializedProperty simulate;
        SerializedProperty smoothedNormals;
        SerializedProperty interactionType;
        SerializedProperty substeps;
        SerializedProperty damping;
        SerializedProperty propagationSpeed;
        SerializedProperty waveSmoothness;
        SerializedProperty speedTweak;
        SerializedProperty upwardsDetectionDistance;
        SerializedProperty downwardsDetectionDistance;
        SerializedProperty showDetailedLogMessages;
        SerializedProperty smoothedNormalsUVFix;
        SerializedProperty smoothedNormalsExtraBlur;

        // Velocity based
        SerializedProperty verticalPushScale;
        SerializedProperty horizontalPushScale;
        SerializedProperty interactorMaximumSpeedClamp;
        SerializedProperty interactorMinimumSpeedClamp;

        // Buoyancy based
        SerializedProperty buoyancy;
        SerializedProperty horizontalBuoyancy;
        SerializedProperty detectionDepth;
        SerializedProperty density;
        SerializedProperty buoyancyDamping;
        SerializedProperty nMaxCellsPerInteractor;
        SerializedProperty nMaxInteractorsDetected;
        SerializedProperty effectScale;
        SerializedProperty showSettingWarningLogs;

        // Paint Mode
        bool paintModeEnabled = false;
        static PaintMode paintMode = PaintMode.FIX;
        [Min(0)] static float pencilRadius = 1;
        static Color paintAreaColor = Color.red;
        Mesh _cellPropertiesMesh;

        // Fixed cells
        public enum PaintMode { FIX, UNFIX }
        bool showFixedSamplesToggle = false;
        bool showFixedSamples = false;
        int fixDetectionLayer = 0;

        NativeArray<Color32> _fixedSampleColors;

        // Interaction mode
        bool interactionModeEnabled = false;

        /// <summary>
        /// Material used for the paint mode. It contains a custom shader that draws colors on mesh
        /// </summary>
        Material MaterialFixedCells { get => _materialPaintMode; set => _materialPaintMode = value; }

        Material _materialPaintMode;

        bool _keyPressed = false;


        /***********************************************/

        #region METHODS

        private void OnEnable()
        {
            _keyPressed = false;
            _meshSizeChanged = false;

            // Cast all selected surfaces
            _surfaces = new List<WaveMakerSurface>();
            foreach (var item in serializedObject.targetObjects)
                _surfaces.Add(item as WaveMakerSurface);
            
            Undo.undoRedoPerformed += UndoRedoPerformed;

            if (Application.isPlaying)
                _surfaces[0].OnAwakeStatusChanged.AddListener(OnAwakeStatusChanged);

            // General
            simulate = serializedObject.FindProperty("simulate");
            smoothedNormals = serializedObject.FindProperty("smoothedNormals");
            interactionType = serializedObject.FindProperty("interactionType");
            substeps = serializedObject.FindProperty("substeps");
            damping = serializedObject.FindProperty("damping");
            propagationSpeed = serializedObject.FindProperty("propagationSpeed");
            waveSmoothness = serializedObject.FindProperty("waveSmoothness");
            speedTweak = serializedObject.FindProperty("speedTweak");
            upwardsDetectionDistance = serializedObject.FindProperty("upwardsDetectionDistance");
            downwardsDetectionDistance = serializedObject.FindProperty("downwardsDetectionDistance");
            showDetailedLogMessages = serializedObject.FindProperty("showDetailedLogMessages");
            smoothedNormalsUVFix = serializedObject.FindProperty("smoothedNormalsUVFix");
            smoothedNormalsExtraBlur = serializedObject.FindProperty("smoothedNormalsExtraBlur");


            // Velocity based
            verticalPushScale = serializedObject.FindProperty("verticalPushScale");
            horizontalPushScale = serializedObject.FindProperty("horizontalPushScale");
            interactorMaximumSpeedClamp = serializedObject.FindProperty("interactorMaximumSpeedClamp");
            interactorMinimumSpeedClamp = serializedObject.FindProperty("interactorMinimumSpeedClamp");

            // Buoyancy Based
            buoyancy = serializedObject.FindProperty("buoyancy");
            horizontalBuoyancy = serializedObject.FindProperty("horizontalBuoyancy");
            detectionDepth = serializedObject.FindProperty("detectionDepth");
            density = serializedObject.FindProperty("density");
            buoyancyDamping = serializedObject.FindProperty("buoyancyDamping");
            nMaxCellsPerInteractor = serializedObject.FindProperty("nMaxCellsPerInteractor");
            nMaxInteractorsDetected = serializedObject.FindProperty("nMaxInteractorsDetected");

            effectScale = serializedObject.FindProperty("effectScale");
            showSettingWarningLogs = serializedObject.FindProperty("showSettingWarningLogs");


            _auxWidth = _surfaces[0]._size_ls.x;
            _auxDepth = _surfaces[0]._size_ls.y;
            _haveSameSize = true;

            for (int i = 0; i < _surfaces.Count; i++)
            {
                //FIXME: Provisional solution, scriptableObjects deleted via the project view don't execute OnDestroy. 
                // If the descriptor has been deleted before selecting the waveMakerSurface...
                if (_surfaces[i].Descriptor == null && _surfaces[i].IsInitialized)
                    _surfaces[i].Uninitialize();

                _haveSameSize = _haveSameSize && _auxWidth == _surfaces[i]._size_ls.x;
                _haveSameSize = _haveSameSize && _auxDepth == _surfaces[i]._size_ls.y;
            }

            if(!_haveSameSize)
            {
                _auxWidth = 0;
                _auxDepth = 0;
            }
        }

        private void InitializeCellPropertiesData()
        {
            if (Application.isPlaying || serializedObject.isEditingMultipleObjects || _surfaces[0].Descriptor == null)
                return;

            if (_cellPropertiesMesh != null)
                DestroyImmediate(_cellPropertiesMesh);

            // Sometimes Application.isPlaying is not detected at this point
            if (_surfaces[0].MeshManager == null || _surfaces[0].MeshManager.Mesh == null)
                return; 

            Mesh sharedMesh = _surfaces[0].MeshManager.Mesh;

            _cellPropertiesMesh = new Mesh
            {
                vertices = sharedMesh.vertices,
                triangles = sharedMesh.triangles,
                uv = sharedMesh.uv,
                normals = sharedMesh.normals,
                tangents = sharedMesh.tangents
            };

            //FIXME: Assign default material first. find a way to find a material and shader not by name
            //  mr.material = new Material(Shader.Find("diffuse"))??  Test this
            if (MaterialFixedCells == null)
            {
                Shader shader = Shader.Find("WaveMaker/WaveMakerFixedPreviewShader");

                if (shader != null)
                    MaterialFixedCells = new Material(shader);
                else
                {
                    Utils.LogError("'WaveMakerFixedPreviewShader' not found. Please fix the name or reinstall WaveMaker", _surfaces[0].gameObject);
                    MaterialFixedCells = new Material(Shader.Find("Diffuse"));
                }
            }

            UpdateFixedSamplesMeshColors();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;

            if (Application.isPlaying)
            {
                for (int i = 0; i < _surfaces.Count; i++)
                    _surfaces[i].OnAwakeStatusChanged.RemoveListener(OnAwakeStatusChanged);
            }
            else
            {
                if (paintModeEnabled)
                    DisablePaintMode();
                if (interactionModeEnabled)
                    DisableInteractionMode();
                UninitializeCellPropertiesData();
            }
        }

        private void UninitializeCellPropertiesData()
        {
            if(_fixedSampleColors.IsCreated)
                _fixedSampleColors.Dispose();
            DestroyImmediate(MaterialFixedCells);
            DestroyImmediate(_cellPropertiesMesh);
        }

        private void OnAwakeStatusChanged()
        {
            Repaint();
        }

        #endregion
#endif
        /***********************************************/

        #region INSPECTOR GUI METHODS

        public override void OnInspectorGUI()
        {
#if !MATHEMATICS_INSTALLED || !BURST_INSTALLED || !COLLECTIONS_INSTALLED
            EditorGUILayout.HelpBox("PACKAGES MISSING: Please follow the 'Readme First' in the main WaveMaker folder or visit the official website linked in the help icon on this component.", MessageType.Warning);
            return;
#endif

#if MATHEMATICS_INSTALLED && BURST_INSTALLED && COLLECTIONS_INSTALLED
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Read tooltips for details and visit the online documentation linked in the help icon next to the component name.", MessageType.Info);

            if (!Application.isPlaying)
            {
                EditorGUILayout.Space();
                DrawDescriptorSelectionGUI();
            }

            if(!serializedObject.isEditingMultipleObjects && _surfaces[0].Descriptor == null)
                return;

            EditorGUILayout.Space();
            if (!Application.isPlaying)
                DrawMeshSizeGUI();

            if (Application.isPlaying && !serializedObject.isEditingMultipleObjects)
                DrawAwakeStatusGUI();

            EditorGUILayout.Space();
            DrawWaveSimulationPropertiesGUI();

            EditorGUILayout.Space();
            DrawInteractionPropertiesGUI();
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.Space();
                DrawCellProperties();
                
                // TODO: Interaction mode not finished
                //EditorGUILayout.Space();
                //DrawInteractionModeGUI();
            }

            EditorGUILayout.Space();
            DrawOtherSettings();

            serializedObject.ApplyModifiedProperties();

#endif

        }

#if MATHEMATICS_INSTALLED && BURST_INSTALLED && COLLECTIONS_INSTALLED

        private void DrawDescriptorSelectionGUI()
        {
            bool descriptorsShared = true;
            WaveMakerDescriptor sharedDescriptor = _surfaces[0].Descriptor;
            foreach (var s in _surfaces)
            {
                if ((s.Descriptor == null && sharedDescriptor != null) ||
                    (s.Descriptor != null && s.Descriptor != sharedDescriptor))
                {
                    descriptorsShared = false;
                    sharedDescriptor = null;
                    break;
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Descriptor", GUILayout.ExpandWidth(false), GUILayout.MaxWidth(70));

            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = !descriptorsShared;
            var newDescriptor = (WaveMakerDescriptor)EditorGUILayout.ObjectField(sharedDescriptor, typeof(WaveMakerDescriptor), false, GUILayout.ExpandWidth(true));
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                UninitializeCellPropertiesData();
                foreach (var s in _surfaces)
                {
                    Undo.RecordObject(s, "WaveMaker Descriptor changed");
                    s.Descriptor = newDescriptor;
                }

                if (!serializedObject.isEditingMultipleObjects)
                    InitializeCellPropertiesData();
            }
            EditorGUILayout.EndHorizontal();

            if (descriptorsShared && sharedDescriptor == null)
                EditorGUILayout.HelpBox("Attach a Wave Maker Descriptor file (Assets -> Create -> WaveMakerDescriptor) that defines the resolution and stores properties for each cell of the surface.", MessageType.Warning);
        }

        private void DrawWaveSimulationPropertiesGUI()
        {
            showWaveSimulationProperties = EditorGUILayout.Foldout(showWaveSimulationProperties, "Wave Simulation Properties", EditorStyles.foldoutHeader);
            if (showWaveSimulationProperties)
            {
                EditorGUIUtility.labelWidth = 150;
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(simulate, new GUIContent("Simulate Surface"));
                EditorGUILayout.PropertyField(propagationSpeed);

                EditorGUIUtility.labelWidth = 0;
                EditorGUILayout.PropertyField(damping);
                EditorGUILayout.PropertyField(waveSmoothness);
                EditorGUILayout.PropertyField(speedTweak);

                EditorGUIUtility.labelWidth = 150;
                EditorGUILayout.PropertyField(substeps, new GUIContent("Substeps (Advanced)"));

                DrawStabilityWarningGUI();

                EditorGUIUtility.labelWidth = 0;
            }
        }

        private void DrawInteractionPropertiesGUI()
        {
            showInteractionProperties = EditorGUILayout.Foldout(showInteractionProperties, "Interaction Properties", EditorStyles.foldoutHeader);
            if (showInteractionProperties)
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                if (!Application.isPlaying)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(interactionType);
                    if (EditorGUI.EndChangeCheck())
                    {
                        foreach (var s in _surfaces)
                        {
                            if(s.interactionType == WaveMakerSurface.InteractionType.VelocityBased)
                            {
                                Undo.RecordObject(s, "WaveMaker Surface simulation enabled");
                                s.simulate = true;
                            }
                        }
                    }
                }

                EditorGUILayout.Space();

                if (!interactionType.hasMultipleDifferentValues && _surfaces[0].interactionType == WaveMakerSurface.InteractionType.OccupancyBased)
                    DrawBuoyancyBasedPropertiesGUI();

                else if (!interactionType.hasMultipleDifferentValues && _surfaces[0].interactionType == WaveMakerSurface.InteractionType.VelocityBased)
                    DrawVelocityBasedPropertiesGUI();
            }
        }

        private void DrawVelocityBasedPropertiesGUI()
        {
            EditorGUIUtility.labelWidth = 150;
            EditorGUILayout.PropertyField(nMaxInteractorsDetected, new GUIContent("Max Detected Interactors"));
            EditorGUILayout.PropertyField(verticalPushScale);
            EditorGUILayout.PropertyField(horizontalPushScale);
            EditorGUILayout.PropertyField(interactorMinimumSpeedClamp, new GUIContent("Interactor Min speed"));
            EditorGUILayout.PropertyField(interactorMaximumSpeedClamp, new GUIContent("Interactor Max speed"));
            EditorGUIUtility.labelWidth = 0;
        }

        private void DrawBuoyancyBasedPropertiesGUI()
        {
            EditorGUIUtility.labelWidth = 135;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(detectionDepth, new GUIContent("Depth"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                for (int i = 0; i < _surfaces.Count; i++)
                    _surfaces[i].UpdateCollider();
            }

            /******************* SIMULATION ****************************/
            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(!simulate.boolValue);
            EditorGUILayout.PropertyField(effectScale, new GUIContent("Simulation scale"));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            /******************* LIMITS *******************************/
            EditorGUIUtility.labelWidth = 170;
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            EditorGUILayout.PropertyField(nMaxInteractorsDetected, new GUIContent("Max Detected Interactors"));
            EditorGUILayout.PropertyField(nMaxCellsPerInteractor, new GUIContent("Max Cells per Interactor"));

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();

            /********************  BUOYANCY ****************************/
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(buoyancy, new GUIContent("Buoyancy   ↑"));
            if (EditorGUI.EndChangeCheck())
            {
                if (!buoyancy.boolValue && !simulate.boolValue)
                {
                    buoyancy.boolValue = true;
                    Utils.LogWarning("Instead of disabling simulation and buoyancy, please disable the whole component.", _surfaces[0].gameObject);
                }
            }


            EditorGUI.BeginDisabledGroup(!buoyancy.boolValue);
            EditorGUILayout.PropertyField(horizontalBuoyancy, new GUIContent("Drifting   ⇄"));
            EditorGUILayout.PropertyField(density);
            EditorGUILayout.PropertyField(buoyancyDamping, new GUIContent("Damping"));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(showSettingWarningLogs);

            EditorGUIUtility.labelWidth = 0;
        }

        private void DrawStabilityWarningGUI()
        {
            foreach(var t in targets)
            {
                var surface = (WaveMakerSurface)t;

                // Check Stability condition
                if (!surface.CheckStabilityCondition())
                {
                    EditorGUILayout.Space();

                    if (serializedObject.isEditingMultipleObjects)
                    {
                        EditorGUILayout.HelpBox(string.Format("Warning! At least one of the selected surfaces '{0}' has a settings warning. Please select only that surface for more info.", surface.name), MessageType.Warning);
                        break;
                    }
                    else
                        EditorGUILayout.HelpBox("Warning! The surface can be unstable with these settings. Reduce descriptor resolution or reduce propagation speed. The last option is to increase substeps but it will be take more time to compute.", MessageType.Warning);
                }
            }
        }

        private void DrawSizeRatioWarningGUI()
        {
            foreach (var t in targets)
            {
                var surface = (WaveMakerSurface)t;

                // Check size ratio
                if (surface._sampleSize_ls.x / surface._sampleSize_ls.y > 2 || surface._sampleSize_ls.y / surface._sampleSize_ls.x > 2)
                {
                    EditorGUILayout.Space();

                    if (serializedObject.isEditingMultipleObjects)
                    {
                        EditorGUILayout.HelpBox(string.Format("Warning! At least one of the selected surfaces '{0}' has a settings warning. Please select only that surface for more info.", surface.name), MessageType.Warning);
                        break;
                    }
                    else
                        EditorGUILayout.HelpBox("Warning! Cells must be approximately square in local space. Set scale to 1. Activate the Wireframe shading in the Scene View to find a proper combination of resolution and surface size.", MessageType.Warning);
                }
            }
        }
        
        private void DrawAwakeStatusGUI()
        {
            if (serializedObject.isEditingMultipleObjects)
                return;

            if (!_surfaces[0].IsInitialized)
            {
                EditorGUILayout.LabelField(new GUIContent(string.Format("Status: Disabled!")));
                return;
            }

            string text = "";
            if (_surfaces[0].IsAwake)
            {
                text += "Awake.";

                if (_surfaces[0].IsAwakeDueToInteraction() || _surfaces[0].IsAwakeDueToSimulation())
                {
                    if (_surfaces[0].IsAwakeDueToInteraction())
                        text += " Interacting with collider.";

                    if (_surfaces[0].IsAwakeDueToSimulation())
                        text += " Simulating.";
                }
                else
                    text += " Getting asleep...";
            }
            else
                text = "Asleep (not simulating or interacting).";

            EditorGUILayout.LabelField(new GUIContent(string.Format("Status: {0}", text)));
        }

        private void DrawMeshSizeGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Local Mesh Size", EditorStyles.boldLabel);

            if(!_haveSameSize)
            {
                EditorGUILayout.HelpBox("Surfaces have different sizes", MessageType.Warning);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Width", GUILayout.MinWidth(15));
            _auxWidth = EditorGUILayout.FloatField(_haveSameSize? _auxWidth : 0, GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("Depth", GUILayout.MinWidth(15));
            _auxDepth = EditorGUILayout.FloatField(_haveSameSize? _auxDepth : 0, GUILayout.ExpandWidth(true));
            EditorGUILayout.Space();

            if (EditorGUI.EndChangeCheck())
            {
                _meshSizeChanged = true;
                _haveSameSize = true;
            }
            
            EditorGUILayout.EndHorizontal();

            if (_meshSizeChanged)
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Apply"))
                {
                    for (int i = 0; i < _surfaces.Count; i++)
                    {
                        Undo.RecordObject(_surfaces[i], "WaveMaker Surface resolution changed");
                        _surfaces[i].Size_ls = new Vector2(_auxWidth, _auxDepth);

                        // Get clamped vals
                        _auxWidth = _surfaces[i].Size_ls.x;
                        _auxDepth = _surfaces[i].Size_ls.y;
                        _meshSizeChanged = false;
                    }
                }
            }

            DrawSizeRatioWarningGUI();
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
        }

        private void DrawCellProperties()
        {
            showCellProperties = EditorGUILayout.Foldout(showCellProperties, "Fixed Cells", EditorStyles.foldoutHeader);
            if (showCellProperties)
            {
                if(serializedObject.isEditingMultipleObjects)
                {
                    EditorGUILayout.HelpBox("This section in multiple selected surfaces is currently not supported. Edit just one, if the descriptor is shared, the rest will be updated.", MessageType.Info);
                    return;
                }

                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Changes will be stored in the Descriptor and can be shared between surfaces.", MessageType.Info);
                EditorGUILayout.Space();

                EditorGUI.BeginDisabledGroup(paintModeEnabled);

                EditorGUI.BeginChangeCheck();
                showFixedSamplesToggle = EditorGUILayout.ToggleLeft(" Show in Scene View", showFixedSamplesToggle);
                if (EditorGUI.EndChangeCheck())
                {
                    // Turned on
                    if (showFixedSamplesToggle)
                    {
                        foreach (SceneView view in SceneView.sceneViews)
                            view.drawGizmos = true;

                        EnableFixedSamplesDisplay();
                    }
                    // turned off
                    else
                    {
                        DisableFixedSamplesDisplay();
                    }
                }

                EditorGUILayout.Space();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Fix All"))
                {
                    Undo.RecordObject(_surfaces[0].Descriptor, "WaveMaker Descriptor Fixed Cells Change");
                    _surfaces[0].Descriptor.RequestData();
                    _surfaces[0].Descriptor.SetAllFixStatus(true);
                    _surfaces[0].Descriptor.ReleaseData();
                    UpdateFixedSamplesMeshColors();
                }
                if (GUILayout.Button("Unix All"))
                {
                    Undo.RecordObject(_surfaces[0].Descriptor, "WaveMaker Descriptor Fixed Cells Change");
                    _surfaces[0].Descriptor.RequestData();
                    _surfaces[0].Descriptor.SetAllFixStatus(false);
                    _surfaces[0].Descriptor.ReleaseData();
                    UpdateFixedSamplesMeshColors();
                }
                if (GUILayout.Button("Fix Borders"))
                {
                    Undo.RecordObject(_surfaces[0].Descriptor, "WaveMaker Descriptor Fixed Cells Change");
                    _surfaces[0].Descriptor.RequestData();
                    _surfaces[0].Descriptor.FixBorders();
                    _surfaces[0].Descriptor.ReleaseData();
                    UpdateFixedSamplesMeshColors();
                }

                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();

                if(GUILayout.Button("Dilate"))
                {
                    Undo.RegisterCompleteObjectUndo(_surfaces[0].Descriptor, "Wave Maker Descriptor fixed cells dilate");
                    _surfaces[0].Descriptor.RequestData();
                    _surfaces[0].Descriptor.DilateFixedCells();
                    _surfaces[0].Descriptor.ReleaseData();
                    UpdateFixedSamplesMeshColors();
                }

                if(GUILayout.Button("Erode"))
                {
                    Undo.RegisterCompleteObjectUndo(_surfaces[0].Descriptor, "Wave Maker Descriptor fixed cells erode");
                    _surfaces[0].Descriptor.RequestData();
                    _surfaces[0].Descriptor.ErodeFixedCells();
                    _surfaces[0].Descriptor.ReleaseData();
                    UpdateFixedSamplesMeshColors();
                }

                GUILayout.EndHorizontal();

                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space();
                DrawPaintModeGUI();

                EditorGUILayout.Space();
                DrawAutomaticDetectionGUI();
            }
        }
        
        private void DrawAutomaticDetectionGUI()
        {
            EditorGUI.BeginDisabledGroup(paintModeEnabled);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Automatic detection", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(upwardsDetectionDistance, new GUIContent("Upwards Distance"));
            EditorGUILayout.PropertyField(downwardsDetectionDistance, new GUIContent("Downwards Distance"));
            fixDetectionLayer = EditorGUILayout.LayerField(new GUIContent("Layer Detected"), fixDetectionLayer);
            if (GUILayout.Button("Fix cells touching colliders in that layer"))
            {
                _surfaces[0].FixCollisions(fixDetectionLayer);
                if(showFixedSamples)
                    UpdateFixedSamplesMeshColors();
            }
            EditorGUILayout.EndVertical();

            EditorGUI.EndDisabledGroup();
        }

        private void DrawPaintModeGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Paint Mode (by hand)", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var newPaintModeEnabled = GUILayout.Toggle(paintModeEnabled, new GUIContent(paintModeEnabled ? "Disable" : "Enable"), EditorStyles.miniButton, GUILayout.ExpandWidth(false), GUILayout.MinHeight(20));
            if (EditorGUI.EndChangeCheck())
            {
                if (newPaintModeEnabled)
                {
                    foreach (SceneView view in SceneView.sceneViews)
                        view.drawGizmos = true;

                    EnablePaintMode();
                }
                else
                    DisablePaintMode();
            }

            if (paintModeEnabled)
            {
                paintMode = (PaintMode)EditorGUILayout.EnumPopup(new GUIContent("Mode"), paintMode);
                pencilRadius = EditorGUILayout.FloatField(new GUIContent("Pencil Radius"), pencilRadius);
                paintAreaColor = EditorGUILayout.ColorField(new GUIContent("Pencil Color"), paintAreaColor);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawOtherSettings()
        {
            showOtherSettings = EditorGUILayout.Foldout(showOtherSettings, "Other Settings", EditorStyles.foldoutHeader);
            if (showOtherSettings)
            {
                EditorGUIUtility.labelWidth = 200;

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(showDetailedLogMessages, new GUIContent("Show Detailed Log Messages"));

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(smoothedNormals, new GUIContent("Smoothed Normals"));
                if (EditorGUI.EndChangeCheck())
                {
                    if (Application.isPlaying)
                    {
                        for (int i = 0; i < _surfaces.Count; i++)
                        {
                            if(!smoothedNormals.boolValue == true || !_surfaces[i].EnableSmoothedNormals())
                            {    
                                smoothedNormals.boolValue = false;
                                _surfaces[i].DisableSmoothedNormals();
                            }
                        }
                    }
                }

                if (smoothedNormals.boolValue)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(smoothedNormalsUVFix, new GUIContent("    Normal UVs offset fix"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        for (int i = 0; i < _surfaces.Count; i++)
                        {
                            if (smoothedNormalsUVFix.boolValue)
                                _surfaces[i].MeshManager.EnableNormalUVs();
                            else
                                _surfaces[i].MeshManager.DisableNormalUVs();
                        }
                    }
                    EditorGUILayout.PropertyField(smoothedNormalsExtraBlur, new GUIContent("    Extra blur"));
                }
                EditorGUILayout.Space();
            }
        }

        //TODO: Interaction mode not finished
        /*
        private void DrawInteractionModeGUI()
        {
            EditorGUI.BeginChangeCheck();
            var newInteractionModeEnabled = GUILayout.Toggle(interactionModeEnabled, new GUIContent(interactionModeEnabled ? "Interaction Mode (Enabled)" : "Interaction Mode (Disabled)"), EditorStyles.miniButton, GUILayout.ExpandWidth(false), GUILayout.MinHeight(20));
            if (EditorGUI.EndChangeCheck())
            {
                if (newInteractionModeEnabled)
                    EnableInteractionMode();
                else
                    DisableInteractionMode();
            }
        }
        */
#endif

        #endregion

        /***********************************************/

        #region SCENE PAINT METHODS

#if MATHEMATICS_INSTALLED && BURST_INSTALLED && COLLECTIONS_INSTALLED

        /// <summary>
        /// Called for each selected object. "target" can be used here safely when multiselection is used
        /// </summary>
        private void OnSceneGUI()
        {
            if (target == null)
                return;

            var surface = target as WaveMakerSurface;

            if (!Application.isPlaying && (paintModeEnabled || interactionModeEnabled))
            {
                if (Event.current.type == EventType.KeyDown)
                    _keyPressed = true;

                if (Event.current.type == EventType.KeyUp)
                    _keyPressed = false;

                if (_keyPressed)
                    return;

                //TODO: Maybe do just once
                // Don't allow selecting another object
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                // Get ray from the camera at the mouse position
                var mousePos = EditorGUIUtility.PointsToPixels(Event.current.mousePosition);

                mousePos.y = Camera.current.pixelHeight - mousePos.y;
                var ray = Camera.current.ScreenPointToRay(mousePos);

                // Hit with this wave maker object
                var plane = new Plane(surface.transform.up, surface.transform.position);
                if (plane.Raycast(ray, out float dist))
                {
                    float4 hitPos = new float4(ray.origin + ray.direction * dist, 0);

                    if (paintModeEnabled)
                    {
                        Handles.color = paintAreaColor;
                        Handles.DrawWireDisc(hitPos.xyz, surface.transform.up, pencilRadius);
                    }

                    SceneView.RepaintAll();

                    if (Event.current.button == 0 && (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag))
                    {
                        if (paintModeEnabled)
                        {
                            if (Event.current.type == EventType.MouseDown)
                                Undo.RegisterCompleteObjectUndo(surface.Descriptor, "WaveMaker Descriptor Fixed Cells Change");

                            PaintArea(hitPos);
                        }
                        else if (interactionModeEnabled)
                            InteractWithPos(hitPos);
                    }
                }
            }

            if (showFixedSamples)
                Graphics.DrawMesh(_cellPropertiesMesh, surface.transform.localToWorldMatrix, _materialPaintMode, 0);
        }

        private void UpdateFixedSamplesMeshColors()
        {
            if (serializedObject.isEditingMultipleObjects)
                return;

            var desc = _surfaces[0].Descriptor;
            if (desc == null)
                return;

            if(_fixedSampleColors.IsCreated)
                _fixedSampleColors.Dispose();
            _fixedSampleColors = new NativeArray<Color32>(desc.ResolutionX * desc.ResolutionZ, Allocator.Persistent);

            for (int i = 0; i < _fixedSampleColors.Length; i++)
                _fixedSampleColors[i] = desc.IsFixed(i) ? desc.fixedColor : desc.defaultColor;
        
            if (_cellPropertiesMesh != null)
                _cellPropertiesMesh.colors32 = _fixedSampleColors.ToArray();
        }

        //TODO: Interaction Mode
        private void InteractWithPos(float4 pos_ws)
        {
            float4 pos_ls = new float4(math.mul(_surfaces[0].transform.worldToLocalMatrix, new float4(pos_ws.xyz, 1)).xyz, 0);
            var size_ls = _surfaces[0]._sampleSize_ls;
            float2 new_size_ls = new float2(size_ls.x, size_ls.y);
            int index = Utils.GetNearestSampleFromLocalPosition(pos_ls, in _surfaces[0]._resolution, in new_size_ls);
            _surfaces[0].SetHeightOffset(index, 10f);
        }

        private void EnableFixedSamplesDisplay()
        {
            if (showFixedSamples || serializedObject.isEditingMultipleObjects)
                return;

            showFixedSamples = true;
            InitializeCellPropertiesData();
            UpdateFixedSamplesMeshColors();
        }

        private void DisableFixedSamplesDisplay()
        {
            if (!showFixedSamples || serializedObject.isEditingMultipleObjects)
                return;

            showFixedSamples = false;

            UninitializeCellPropertiesData();

            //FIXME: In 2019+ stopping Graphics.DrawMesh for the fixed samples mesh doesn't stop it from showing up. 
            // This is a workaround until a solution is found.
            _surfaces[0].enabled = false;
            _surfaces[0].enabled = true;

            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private void EnablePaintMode()
        {
            if (Application.isPlaying || serializedObject.isEditingMultipleObjects)
                return;

            if (interactionModeEnabled)
                DisableInteractionMode();

            paintModeEnabled = true;
            ((WaveMakerSurface)serializedObject.targetObject).Descriptor.RequestData();

            if(!showFixedSamplesToggle)
                EnableFixedSamplesDisplay();

            Tools.current = Tool.None;
        }

        private void DisablePaintMode()
        {
            if (Application.isPlaying || serializedObject.isEditingMultipleObjects)
                return;

            if(!showFixedSamplesToggle)
                DisableFixedSamplesDisplay();

            ((WaveMakerSurface)serializedObject.targetObject).Descriptor.ReleaseData();
            paintModeEnabled = false;
            Tools.current = Tool.Move;
        }

        private void EnableInteractionMode()
        {
            if (Application.isPlaying || serializedObject.isEditingMultipleObjects)
                return;

            if (paintModeEnabled)
                DisablePaintMode();

            ((WaveMakerSurface)serializedObject.targetObject).Uninitialize();
            ((WaveMakerSurface)serializedObject.targetObject).Initialize();
            interactionModeEnabled = true;
            Tools.current = Tool.None;
        }

        private void DisableInteractionMode()
        {
            if (Application.isPlaying || serializedObject.isEditingMultipleObjects)
                return;

            interactionModeEnabled = false;
            Tools.current = Tool.Move;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        /// <param name="hitPoint_ws">A position in worldspace where to paint</param>
        private void PaintArea(float4 hitPoint_ws)
        {
            if (Application.isPlaying)
                return;

            if (!_surfaces[0].Descriptor.IsInitialized)
                return;

            // NOTE: In edit mode surface transforms are not stored
            var transform = new AffineTransform(new float4(_surfaces[0].transform.position, 0), _surfaces[0].transform.rotation, new float4(_surfaces[0].transform.localScale, 0));
            var hitPoint_ls = transform.InverseTransformPoint(hitPoint_ws);

            var size_ls = _surfaces[0]._sampleSize_ls;
            float2 new_size_ls = new float2(size_ls.x, size_ls.y);
            int centerIndex  = Utils.GetNearestSampleFromLocalPosition(hitPoint_ls, in _surfaces[0]._resolution, in new_size_ls);
            Utils.FromIndexToSampleIndices(centerIndex, in _surfaces[0]._resolution, out int centerX, out int centerZ);

            // Area size in cells
            var sampleSize = _surfaces[0]._sampleSize_ls;
            sampleSize.x *= _surfaces[0].transform.localScale.x;
            sampleSize.y *= _surfaces[0].transform.localScale.z;
            int radiusCellSizeX = Mathf.Max(1, Mathf.FloorToInt(pencilRadius / sampleSize.x));
            int radiusCellSizeZ = Mathf.Max(1, Mathf.FloorToInt(pencilRadius / sampleSize.y));

            // Clamp to the edges
            IntegerPair areaOffset = new IntegerPair(math.max(centerX - radiusCellSizeX, 0), math.max(centerZ - radiusCellSizeZ, 0));
            IntegerPair areaEnd = new IntegerPair(math.min(centerX + radiusCellSizeX, _surfaces[0]._resolution.x - 1), math.min(centerZ + radiusCellSizeZ, _surfaces[0]._resolution.z - 1));
            IntegerPair areaSize = new IntegerPair(areaEnd.x - areaOffset.x + 1, areaEnd.z - areaOffset.z + 1);
            
            var job = new DrawAreaJob()
            {
                fixedSamples = _surfaces[0].Descriptor.SharedFixedGrid,
                fixedSampleColors = _fixedSampleColors,
                hitPoint_ls = hitPoint_ls,
                resolution = _surfaces[0]._resolution,
                areaOffset = areaOffset,
                areaSize = areaSize,
                sampleSize_ls = _surfaces[0]._sampleSize_ls,
                paintMode = paintMode,
                defaultColor = _surfaces[0].Descriptor.defaultColor,
                fixedColor = _surfaces[0].Descriptor.fixedColor,
                pencilRadius = pencilRadius
            };
            
            var handle = job.Schedule(areaSize.x * areaSize.z, 64, default);
            handle.Complete();

            if (showFixedSamples)
                _cellPropertiesMesh.colors32 = _fixedSampleColors.ToArray();

            _surfaces[0].Descriptor.CopyNativeDataToSerializedData(areaSize, areaOffset);
        }

        private void UndoRedoPerformed()
        {
            if (serializedObject.isEditingMultipleObjects)
                return;

            if (_surfaces[0]._size_ls.x != _auxWidth || _surfaces[0]._size_ls.y != _auxDepth)
            {
                _auxWidth = _surfaces[0]._size_ls.x;
                _auxDepth = _surfaces[0]._size_ls.y;
                _surfaces[0].Size_ls = new Vector2(_auxWidth, _auxDepth);
            }

            UpdateFixedSamplesMeshColors();
        }

        [BurstCompile]
        private struct DrawAreaJob : IJobParallelFor
        {
            [WriteOnly] [NativeDisableParallelForRestriction] public NativeArray<int> fixedSamples;
            [WriteOnly] [NativeDisableParallelForRestriction] public NativeArray<Color32> fixedSampleColors;

            [ReadOnly] public float4 hitPoint_ls;
            [ReadOnly] public IntegerPair resolution;
            [ReadOnly] public IntegerPair areaSize;
            [ReadOnly] public IntegerPair areaOffset;
            [ReadOnly] public float2 sampleSize_ls;
            [ReadOnly] public PaintMode paintMode;
            [ReadOnly] public Color32 fixedColor;
            [ReadOnly] public Color32 defaultColor;
            [ReadOnly] public float pencilRadius;

            public void Execute(int index)
            {
                int sampleX = index % areaSize.x;
                int sampleZ = index / areaSize.x;
                index = resolution.x * (areaOffset.z + sampleZ) + (areaOffset.x + sampleX);
                sampleX += areaOffset.x;
                sampleZ += areaOffset.z;

                if (sampleX >= resolution.x || sampleZ >= resolution.z)
                    return;

                // Ignore out of the circle area
                var pos_ls = new float4(sampleX * sampleSize_ls.x, 0, sampleZ * sampleSize_ls.y, 0);
                if (math.length(hitPoint_ls - pos_ls) > pencilRadius)
                    return;

                switch (paintMode)
                {
                    case PaintMode.FIX:
                        fixedSamples[index] = 1;
                        fixedSampleColors[index] = fixedColor;
                        break;

                    case PaintMode.UNFIX:
                        fixedSamples[index] = 0;
                        fixedSampleColors[index] = defaultColor;
                        break;

                    default:
                        break;
                }
            }
        }

#endif
    }
    #endregion
}
