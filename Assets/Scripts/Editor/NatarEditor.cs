﻿using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Natar
{
    namespace NatarEditor
    {
        public class NatarEditor
        {
            public static GUIStyle StyleGray
            {
                get
                {
                    return Style(new Color(0.5f, 0.5f, 0.5f, 0.3f));
                }
            }

            public static GUIStyle StyleBlue
            {
                get
                {
                    return Style(new Color(0, 0.5f, 1f, 0.3f));
                }
            }
            public static GUIStyle StyleGreen
            {
                get
                {
                    return Style(new Color(0f, 1f, 0.5f, 0.3f));
                }
            }

            public static GUIStyle FlatBox
            {
                get
                {
                    return Style(new Color(0.35f, 0.35f, 0.35f, 0.1f));
                }
            }

            public static GUIStyle Style(Color color)
            {
                GUIStyle currentStyle = new GUIStyle(GUI.skin.box)
                {
                    border = new RectOffset(-1, -1, -1, -1)
                };

                Color[] pix = new Color[1];
                pix[0] = color;
                Texture2D bg = new Texture2D(1, 1);
                bg.SetPixels(pix);
                bg.Apply();


                currentStyle.normal.background = bg;
                return currentStyle;
            }

            public static void DrawServiceStatus(ServiceStatus state) {
                // Alternative icons 
                Texture2D statusIcon;
                switch(state) {
                    case ServiceStatus.DISCONNECTED:
                        statusIcon = EditorGUIUtility.FindTexture("d_winbtn_mac_close");
                        break;
                    case ServiceStatus.CONNECTED:
                        statusIcon = EditorGUIUtility.FindTexture("d_winbtn_mac_min");
                        break;
                    case ServiceStatus.WORKING:
                        statusIcon = EditorGUIUtility.FindTexture("d_winbtn_mac_max");
                        break;
                    default:
                        statusIcon = EditorGUIUtility.FindTexture("d_winbtn_mac_inact");
                        break;
                }
                GUILayout.Label(new GUIContent(statusIcon, "Current service status"), GUILayout.Width(18));
            }
        }
    }
}