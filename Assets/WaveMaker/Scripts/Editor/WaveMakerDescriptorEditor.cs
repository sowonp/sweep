using UnityEngine;
using UnityEditor;
using System.IO;

namespace WaveMaker.Editor
{
    [CustomEditor(typeof(WaveMakerDescriptor))]
    public class WaveMakerDescriptorEditor : UnityEditor.Editor
    {
        int newWidth, newDepth;
        WaveMakerDescriptor descriptor;
        bool isDirty;

        private void Awake()
        {
            descriptor = (WaveMakerDescriptor)target;
        }

#if MATHEMATICS_INSTALLED && BURST_INSTALLED && COLLECTIONS_INSTALLED
        private void OnEnable()
        {
            Undo.undoRedoPerformed += UndoRedoPerformed;
            isDirty = false;
            descriptor = (WaveMakerDescriptor)target;
            newWidth = descriptor.ResolutionX;
            newDepth = descriptor.ResolutionZ;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }
#endif

        public override void OnInspectorGUI()
        {
#if !MATHEMATICS_INSTALLED || !BURST_INSTALLED || !COLLECTIONS_INSTALLED
            EditorGUILayout.HelpBox("PACKAGES MISSING: PACKAGES MISSING. Please follow the QuickStart in the main WaveMaker folder or visit the official website linked in the help icon on this component.", MessageType.Warning);
            return;
#else
            serializedObject.Update();
            EditorGUILayout.HelpBox("Attach this object to a WaveMaker Surface to set the properties stored here.", MessageType.Info, true);

            DrawResolutionGUI();
            EditorGUILayout.Space();
            DrawFixingGUI();
            EditorGUILayout.Space();
            DrawExportImportGUI();
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
            if (GUI.changed)
                EditorUtility.SetDirty(target);
#endif
        }

#if MATHEMATICS_INSTALLED && BURST_INSTALLED && COLLECTIONS_INSTALLED

        private void DrawResolutionGUI()
        {
            GUI.SetNextControlName("LabelGrid");
            EditorGUILayout.LabelField("Grid Resolution", EditorStyles.boldLabel);
            EditorGUIUtility.labelWidth = 180;

            EditorGUI.BeginChangeCheck();
            newWidth = EditorGUILayout.IntField("Width", newWidth);
            if (EditorGUI.EndChangeCheck())
                isDirty = true;

            EditorGUI.BeginChangeCheck();
            newDepth = EditorGUILayout.IntField("Depth", newDepth);
            if (EditorGUI.EndChangeCheck())
                isDirty = true;

            EditorGUILayout.BeginHorizontal();
            if (isDirty && GUILayout.Button("Apply"))
            {
                GUIUtility.keyboardControl = 0;
                Undo.RegisterCompleteObjectUndo(descriptor, "Wave Maker Descriptor resolution change");
                descriptor.SetResolution(newWidth, newDepth);
                newWidth = descriptor.ResolutionX;
                newDepth = descriptor.ResolutionZ;
                isDirty = false;
            }

            if (isDirty && GUILayout.Button("Cancel"))
            {
                newWidth = descriptor.ResolutionX;
                newDepth = descriptor.ResolutionZ;
                GUI.FocusControl("LabelGrid");
                isDirty = false;
            }   

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            GUILayout.Label(string.Format("Number of cells/verts: {0} ({1} max)", newWidth * newDepth, WaveMakerDescriptor.MaxVertices));

            EditorGUIUtility.fieldWidth = 0;
        }

        private void DrawFixingGUI()
        {
            EditorGUILayout.LabelField("Fixed Cells", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Fix All"))
            {
                Undo.RegisterCompleteObjectUndo(descriptor, "Wave Maker Descriptor fixed cells fix");
                descriptor.RequestData();
                descriptor.SetAllFixStatus(true);
                descriptor.ReleaseData();
            }

            if (GUILayout.Button("Unfix All"))
            {
                Undo.RegisterCompleteObjectUndo(descriptor, "Wave Maker Descriptor fixed cells unfix");
                descriptor.RequestData();
                descriptor.SetAllFixStatus(false);
                descriptor.ReleaseData();
            }

            if (GUILayout.Button("Fix Borders"))
            {
                Undo.RegisterCompleteObjectUndo(descriptor, "Wave Maker Descriptor fixed cells fix borders");
                descriptor.RequestData();
                descriptor.FixBorders();
                descriptor.ReleaseData();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if(GUILayout.Button("Dilate"))
            {
                Undo.RegisterCompleteObjectUndo(descriptor, "Wave Maker Descriptor fixed cells dilate");
                descriptor.RequestData();
                descriptor.DilateFixedCells();
                descriptor.ReleaseData();
            }

            if(GUILayout.Button("Erode"))
            {
                Undo.RegisterCompleteObjectUndo(descriptor, "Wave Maker Descriptor fixed cells erode");
                descriptor.RequestData();
                descriptor.ErodeFixedCells();
                descriptor.ReleaseData();
            }

            GUILayout.EndHorizontal();


            EditorGUILayout.HelpBox("You can paint by hand using the Paint Mode in the surface where this descriptor is used. Open the preview window on the bottom to see the cell properties.", MessageType.Info);
        }

        private void DrawExportImportGUI()
        {
            EditorGUILayout.LabelField("Export/Import", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Export as PNG"))
                ExportAsTexture();

            if (GUILayout.Button("Import from PNG"))
                ImportFromTexture();

            GUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox("You can paint by hand using the Paint Mode in the surface where this descriptor is used. Open the preview window on the bottom to see the cell properties.", MessageType.Info);
        }

        private void ExportAsTexture()
        {
            string name = "DescriptorData" + descriptor.ResolutionX + "x" + descriptor.ResolutionZ;
            string path = EditorUtility.SaveFilePanel("Save descriptor as a texture", Application.dataPath, name, "png");
            if (path.Length <= 0)
                return;

            Texture2D tex = descriptor.ToTexture();
            if (tex == null)
            {
                Debug.LogError("Error generating a texture with the descriptor data.");
                return;
            }

            var pngData = ImageConversion.EncodeToPNG(tex);
            DestroyImmediate(tex);

            try
            {
                if (pngData != null)
                {
                    File.WriteAllBytes(path, pngData);
                    Debug.Log("Exporting... Please wait for the image to appear in " + path);
                }
                else
                    throw new IOException("Not able to encode the descriptor into image data");
            }
            catch (IOException e)
            {
                Debug.LogError("Error exporting image to path. Try a different path: " + e.ToString());
            }
        }

        private void ImportFromTexture()
        {
            string path = EditorUtility.OpenFilePanel("Open exported descriptor", Application.dataPath, "png");
            if (path.Length <= 0)
                return;

            byte[] pngData = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(descriptor.ResolutionX, descriptor.ResolutionZ);
            
            try
            {
                if (!ImageConversion.LoadImage(tex, pngData))
                    throw new System.Exception("");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Impossible to load file data into a texture. Check if the resolution matches and the format is correct. Error msg :" + e.ToString());
                DestroyImmediate(tex);
                return;
            }

            if ((tex.width == descriptor.ResolutionX && tex.height == descriptor.ResolutionZ) ||
                EditorUtility.DisplayDialog("Resolution change!", "The resolution of the input image is different from the current resolution", "Change resolution", "Cancel"))
            {
                descriptor.FromTexture(tex);

                // Update editor
                newWidth = tex.width;
                newDepth = tex.height;
            }

            DestroyImmediate(tex);
        }

        private void UndoRedoPerformed()
        {
            //FIXME: Hack to avoid this weird error. Target is working, but returns an exception, but still works.
            try
            {
                newWidth = ((WaveMakerDescriptor)target).ResolutionX;
                newDepth = ((WaveMakerDescriptor)target).ResolutionZ;

                //FIXME: Updating resolution on related planes not working. Target is somehow problematic.
                ((WaveMakerDescriptor)target).SetResolution(newWidth, newDepth);
            }
            catch (System.IndexOutOfRangeException)
            {
                return;
            }
        }
#endif
    }

#if MATHEMATICS_INSTALLED && BURST_INSTALLED && COLLECTIONS_INSTALLED

    [CustomPreview(typeof(WaveMakerDescriptor))]
    public class WaveMakerDescriptorPreview : ObjectPreview
    {
        Texture2D _previewTexture;
        WaveMakerDescriptor descriptor;

        public override bool HasPreviewGUI()
        {
            return true;
        }

        private void CreateTexture()
        {
             descriptor = (WaveMakerDescriptor)target;

            _previewTexture = new Texture2D(descriptor.ResolutionX, descriptor.ResolutionZ, TextureFormat.RGBAHalf, false);
            _previewTexture.wrapMode = TextureWrapMode.Clamp;
        }

        private void UpdateTexture()
        {
            // Apply properties as colors
            int index = 0;
            for (int z = 0; z < descriptor.ResolutionZ; z++)
               for (int x = 0; x < descriptor.ResolutionX; x++)
                {
                    Color newColor = descriptor.IsFixed(index) ? descriptor.fixedColor : descriptor.defaultColor;
                    _previewTexture.SetPixel(x, z, newColor);
                    index++;
                }

            _previewTexture.Apply();
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            descriptor = (WaveMakerDescriptor)target;

            // Update texture data if resolution changed
            if (_previewTexture == null || descriptor.ResolutionX != _previewTexture.width || descriptor.ResolutionZ != _previewTexture.height)
                CreateTexture();

            UpdateTexture();

            GUI.DrawTexture(r, _previewTexture, ScaleMode.ScaleToFit, false, 1);
        }


    }

#endif
}
