using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (QuickCameraSetup))]
public class CameraSetupEditor : Editor {

	override public void OnInspectorGUI () {
		QuickCameraSetup setup = target as QuickCameraSetup;
		setup.Type = (CameraType) EditorGUILayout.EnumPopup ("Camera Type:", setup.Type);
		setup.State = (ComponentState) EditorGUILayout.EnumPopup ("Internal State:", setup.State);
		setup.BaseKey = EditorGUILayout.TextField ("Base camera key:", setup.BaseKey);
		setup.IntrinsicsKey = EditorGUILayout.TextField ("Intrinsics parameters key:", setup.IntrinsicsKey);

		setup.HasExtrinsics = setup.Type != CameraType.RGB;
		if (setup.HasExtrinsics) {
			setup.ExtrinsicsKey = EditorGUILayout.TextField ("Extrinsics parameters key:", setup.ExtrinsicsKey);
		}

		setup.HasData = (setup.Type == CameraType.RGB || setup.Type == CameraType.DEPTH);
		if (setup.HasData) {
			setup.DataKey = EditorGUILayout.TextField ("Data key:", setup.DataKey);
		}

		if(GUILayout.Button("Restart")) {
			/* Execute code when button is clicked */
			setup.State = ComponentState.DISCONNECTED;
		}
	}
}