using UnityEditor;
using UnityEngine;

namespace Natar
{
    namespace NatarEditor
    {
        [CustomEditor(typeof(SetupExtrinsics))]
        public class SetupExtrinsicsEditor : Editor 
        {
            private SetupExtrinsics S { get { return target as SetupExtrinsics; } }
            private MonoScript script;

            //Creating serialized properties so we can retrieve variable attributes without having to recreate them in the custom editor
            protected SerializedProperty key;

            private bool OptionFoldout = true, MatrixFoldout = true;

            private void OnEnable() {   
                script = MonoScript.FromMonoBehaviour(S);
                key = serializedObject.FindProperty("Key");
            }

            public override void OnInspectorGUI() {

                serializedObject.Update();

                EditorGUI.BeginChangeCheck();

                // This will show the current used script and make it clickable. When clicked, the script's code is open into the default editor.
                EditorGUI.BeginDisabledGroup(true);
                {
                    script = (MonoScript)EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
                }
                EditorGUI.EndDisabledGroup();


                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUI.indentLevel++;
                    OptionFoldout = EditorGUILayout.Foldout(OptionFoldout, "Options");
                    EditorGUI.indentLevel--;

                    if (OptionFoldout) {
                        EditorGUILayout.BeginVertical(NatarEditor.FlatBox);
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                EditorGUILayout.PropertyField(key, new GUIContent("Data key", "Redis data pointer key."));
                                
                                S.LiveUpdate = GUILayout.Toggle(S.LiveUpdate, new GUIContent("U","Enable live update mod."), EditorStyles.miniButton, GUILayout.Width(18));
                                S.ReverseYAxis = GUILayout.Toggle(S.ReverseYAxis, new GUIContent("Y","Toggle reverse y axis mod."), EditorStyles.miniButton, GUILayout.Width(18));
                                if (GUILayout.Button(new GUIContent("T", "Test if the key contains data."), EditorStyles.miniButton, GUILayout.Width(18))) {
                                    if (S.state != ServiceStatus.DISCONNECTED) {
                                        S.init();
                                    }
                                }
                                NatarEditor.DrawServiceStatus(S.state);
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.EndVertical();
                    }

                    EditorGUILayout.BeginVertical(NatarEditor.FlatBox);
                    {
                        EditorGUI.indentLevel++;
                        MatrixFoldout = EditorGUILayout.Foldout(MatrixFoldout, "Matrix preview");
                        EditorGUI.indentLevel--;
                        
                        if (MatrixFoldout) {
                            Matrix4x4 m = S.GetTransformationMatrix();
                            NatarEditor.DrawMatrix4x4(m);
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
                
                serializedObject.ApplyModifiedProperties();
                
                if (EditorGUI.EndChangeCheck()) 
                {
                    // When a property is accessed directly from script instead of via serialized properties
                    // pressing play causes the property to reset to its original value.
                    // Setting the target dirty prevent this effect
                    Undo.RecordObject(target, "SetupExtrinsics values changed");
                    EditorUtility.SetDirty(target);
                }
            }
        }
    }
}