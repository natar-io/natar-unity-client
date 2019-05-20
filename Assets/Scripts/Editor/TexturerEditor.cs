using UnityEditor;
using UnityEngine;

namespace Natar
{
    namespace NatarEditor
    {
        [CustomEditor(typeof(Texturer))]
        public class TexturerEditor : Editor 
        {
            private Texturer T { get { return target as Texturer; } }
            private MonoScript script;

            //Creating serialized properties so we can retrieve variable attributes without having to recreate them in the custom editor
            protected SerializedProperty key, targetModel;

            private bool OptionFoldout = true;

            private void OnEnable() {   
                script = MonoScript.FromMonoBehaviour(T);
                key = serializedObject.FindProperty("Key");
                targetModel = serializedObject.FindProperty("targetModel");
            }

            public override void OnInspectorGUI() {

                serializedObject.Update();

                EditorGUI.BeginDisabledGroup(true);
                {
                    script = (MonoScript)EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUI.indentLevel++;
                    OptionFoldout = EditorGUILayout.Foldout(OptionFoldout, "Parameters");
                    EditorGUI.indentLevel--;

                    if (OptionFoldout) {
                        
                        #region datakey
                        EditorGUILayout.BeginVertical(NatarEditor.FlatBox);
                        {
                            EditorGUILayout.BeginHorizontal(NatarEditor.FlatBox);
                            {
                                EditorGUILayout.PropertyField(key, new GUIContent("Data key", "Redis data key holding intrinsics parameters informations."), GUILayout.MinWidth(50));
                                if (GUILayout.Button(new GUIContent("T", "Test if the key contains data."), EditorStyles.miniButton, GUILayout.Width(18))) {
                                    if (T.state != ServiceStatus.DISCONNECTED) {
                                        T.init();
                                    }
                                }
                                NatarEditor.DrawServiceStatus(T.state);
                            
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        #endregion

                        #region targetmodel
                        EditorGUILayout.PropertyField(targetModel, new GUIContent("Target model", "3D model to apply the texture to."));
                        if (T.targetModel == null) {
                            EditorGUILayout.BeginVertical(NatarEditor.StyleRed);
                            {
                                EditorGUILayout.HelpBox("The target model is required for the component to work.", MessageType.None, true);
                            }
                            EditorGUILayout.EndVertical();
                        }
                        else if (T.targetModel.GetComponent<Renderer>() == null) {
                            EditorGUILayout.BeginVertical(NatarEditor.StyleOrange);
                            {
                                EditorGUILayout.HelpBox("The target model must have a material in order for the texture to be set.", MessageType.None, true);
                            }
                            EditorGUILayout.EndVertical();
                        }
                        #endregion

                        #region texture
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();
                            Texture2D tex = T.GetCurrentTexture();
                            if (tex != null) {
                                NatarEditor.DrawTexture(tex);
                            }
                            else {
                                GUILayout.Label("No preview available.");
                            }
                            GUILayout.FlexibleSpace();
                        }
                        GUILayout.EndHorizontal();
                        #endregion
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}