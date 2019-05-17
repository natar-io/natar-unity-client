using UnityEditor;
using UnityEngine;

namespace Natar
{
    namespace NatarEditor
    {
        [CustomEditor(typeof(SetupIntrinsics))]
        public class SetupIntrinsicsEditor : Editor 
        {
            private SetupIntrinsics S { get { return target as SetupIntrinsics; } }
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
                        EditorGUILayout.BeginHorizontal(NatarEditor.FlatBox);
                        {   
                            EditorGUILayout.PropertyField(key, new GUIContent("Data key", "Redis data key holding intrinsics parameters informations."), GUILayout.MinWidth(50));
                            if (GUILayout.Button(new GUIContent("T", "Test if the key contains data."), EditorStyles.miniButton, GUILayout.Width(18))) {
                                if (S.state != ServiceStatus.DISCONNECTED) {
                                    S.init();
                                }
                            }
                            NatarEditor.DrawServiceStatus(S.state);
                        
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.BeginVertical(NatarEditor.FlatBox);
                    {
                        EditorGUI.indentLevel++;
                        MatrixFoldout = EditorGUILayout.Foldout(MatrixFoldout, "Intrinsics matrix");
                        EditorGUI.indentLevel--;
                        
                        if (MatrixFoldout) {
                            Matrix4x4 m = S.GetProjectionMatrix();
                            NatarEditor.DrawMatrix4x4(m);
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}