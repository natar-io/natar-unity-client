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

            private bool OptionFoldout = true;

            private void OnEnable() {   
                script = MonoScript.FromMonoBehaviour(S);
                key = serializedObject.FindProperty("Key");
            }

            public override void OnInspectorGUI() {

                serializedObject.Update();

                // This will show the current used script and make it clickable. When clicked, the script's code is open into the default editor.
                EditorGUI.BeginDisabledGroup(true);
                script = (MonoScript)EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
                EditorGUI.EndDisabledGroup();


                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
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
                            NatarEditor.DrawServiceStatus(S.state);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
                
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}