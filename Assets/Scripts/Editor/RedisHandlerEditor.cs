using UnityEngine;
using UnityEditor;

namespace Natar 
{
    namespace NatarEditor
    {    
        [CustomEditor(typeof(RedisHandler))]
        public class RedisHandlerEditor :  Editor {

            private RedisHandler R { get { return target as RedisHandler; } }
            private MonoScript script;

            public bool SettingsFoldout = true;

            protected SerializedProperty RedisServerHost, RedisServerPort, PingLatency;

            private void OnEnable() {

                script = MonoScript.FromMonoBehaviour(R);
                RedisServerHost = serializedObject.FindProperty("RedisServerHost");    
                RedisServerPort = serializedObject.FindProperty("RedisServerPort");    

                PingLatency = serializedObject.FindProperty("PingLatency");    
            }

            public override void OnInspectorGUI() {

                serializedObject.Update();

                EditorGUI.BeginChangeCheck();

                // This will show the current used script and make it clickable. When clicked, the script's code is open into the default editor.
                EditorGUI.BeginDisabledGroup(true);
                script = (MonoScript)EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUI.indentLevel++;
                SettingsFoldout = EditorGUILayout.Foldout(SettingsFoldout, "Options");
                EditorGUI.indentLevel--;

                if (SettingsFoldout) {
                    EditorGUILayout.BeginVertical(NatarEditor.FlatBox);
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.PropertyField(RedisServerHost, new GUIContent("Connection settings", "Input ip adress and host" +
                                                                            "\nSupported IP formats are the following:" +
                                                                            "\n127.0.0.1" +
                                                                            "\nlocalhost" +
                                                                            "\nhttp://127.0.0.1" +
                                                                            "\nhttp://host-example.com"), GUILayout.MinWidth(48));
                            EditorGUIUtility.labelWidth = 18;
                            EditorGUILayout.PropertyField(RedisServerPort, new GUIContent("", "Port"), GUILayout.MaxWidth(35));
                            EditorGUIUtility.labelWidth = 0;
                            R.NoDelaySocket = GUILayout.Toggle(R.NoDelaySocket, new GUIContent("D","Toggle no delay sockets for this connection."), EditorStyles.miniButton, GUILayout.Width(18));

                            if (R.IsConnected()) {
                                NatarEditor.DrawServiceStatus(ServiceStatus.WORKING);
                            }
                            else {
                                NatarEditor.DrawServiceStatus(ServiceStatus.DISCONNECTED);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.BeginVertical(NatarEditor.FlatBox);
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.PropertyField(PingLatency, new GUIContent("Ping delay (ms)", "Ping the redis server to ensure that it is alive." + 
                                                                        "\nToo short value may cause lags or even crashes while higher values can lead to unexpected behavior when a Redis server is disconnected while services are still running."));
                            if (GUILayout.Button(new GUIContent("Ping", "Manually ping the service to check its state."), EditorStyles.miniButton, GUILayout.MaxWidth(30))) {
                                R.Ping();
                            }
                        }
                        EditorGUILayout.EndHorizontal();
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
                    Undo.RecordObject(target, "RedisHandler values changed");
                    EditorUtility.SetDirty(target);
                }
            }
        }
    }
}