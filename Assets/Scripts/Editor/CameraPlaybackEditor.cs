using UnityEditor;
using UnityEngine;

namespace Natar
{
    namespace NatarEditor
    {
        [CustomEditor(typeof(CameraPlayback))]
        public class CameraPlaybackEditor : Editor 
        {
            private CameraPlayback C { get { return target as CameraPlayback; } }
            private MonoScript script;

            //Creating serialized properties so we can retrieve variable attributes without having to recreate them in the custom editor
            protected SerializedProperty key, outImage, use16BitDepth;

            private bool OptionFoldout = true;

            private void OnEnable() {   
                script = MonoScript.FromMonoBehaviour(C);
                key = serializedObject.FindProperty("Key");
                outImage = serializedObject.FindProperty("OutImage");
                use16BitDepth = serializedObject.FindProperty("Use16BitDepth");
            }

            public override void OnInspectorGUI() {

                serializedObject.Update();

                EditorGUI.BeginChangeCheck();

                EditorGUI.BeginDisabledGroup(true);
                {
                    script = (MonoScript)EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
                }
                EditorGUI.EndDisabledGroup();


                EditorGUILayout.PropertyField(use16BitDepth, new GUIContent("16Bit Depth", "Check for depth Camera like Orbbec Astra plus."), GUILayout.MinWidth(50));
                          
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUI.indentLevel++;
                    OptionFoldout = EditorGUILayout.Foldout(OptionFoldout, "Options");
                    EditorGUI.indentLevel--;

                    if (OptionFoldout) {

                        EditorGUILayout.BeginHorizontal(NatarEditor.FlatBox);
                        {
                            EditorGUILayout.PropertyField(key, new GUIContent("Data key", "Redis data key holding intrinsics parameters informations."), GUILayout.MinWidth(50));  
                            if (GUILayout.Button(new GUIContent("T", "Test if the key contains data."), EditorStyles.miniButton, GUILayout.Width(18))) {
                                if (C.state != ServiceStatus.DISCONNECTED) {
                                    C.init();
                                }
                            }
                            NatarEditor.DrawServiceStatus(C.state);
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal(NatarEditor.FlatBox);
                        {
                            EditorGUILayout.PropertyField(outImage, new GUIContent("Out image", ""), GUILayout.MinWidth(50));
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal(NatarEditor.FlatBox);
                        {
                            GUILayout.FlexibleSpace();
                            Texture2D tex = C.GetCurrentTexture();
                            if (tex != null) {
                                NatarEditor.DrawTexture(tex);
                            }
                            else {
                                GUILayout.Label("No preview available.");
                            }
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndVertical();

                serializedObject.ApplyModifiedProperties();

                if (EditorGUI.EndChangeCheck()) 
                {
                    // When a property is accessed directly from script instead of via serialized properties
                    // pressing play causes the property to reset to its original value.
                    // Setting the target dirty prevent this effect
                    Undo.RecordObject(target, "CameraPlayback values changed");
                    EditorUtility.SetDirty(target);
                }
            }
        }
    }
}
