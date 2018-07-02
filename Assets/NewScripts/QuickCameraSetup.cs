using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class QuickCameraSetup : MonoBehaviour {
	private string className;

	private RedisConnection connection;
	private bool isConnected = false;

	public ComponentState State = ComponentState.DISCONNECTED;
	public CameraType Type = CameraType.RGB;
	public string BaseKey = "camera0";
	public string IntrinsicsKey = ":calibration";
	public bool HasData = true;
	public string DataKey = "";
	public bool HasExtrinsics = false;
	public string ExtrinsicsKey = ":extrinsincs";

	[HideInInspector]
	public IntrinsicsParameters IntrinsicsParameters;
	public ExtrinsicsParameters ExtrinsicsParameters;

	// Use this for initialization
	void Start () {
		className = transform.gameObject.name;
		Connect ();
	}

	void Connect () {
		if (connection == null) {
			connection = new RedisConnection ();
		}
		isConnected = connection.TryConnection ();
		Utils.Log (className, (isConnected ? "Connection succeed." : "Connection failed."));
		if (!isConnected) {
			State = ComponentState.DISCONNECTED;
			return;
		}
		State = ComponentState.CONNECTED;
		Initialize ();
	}

	void Initialize () {
		if (!SetupIntrinsics ()) {
			return;
		}
		if (Type != CameraType.RGB) {
			if (!SetupExtrinsics ()) {
				return;
			}
		}
	}

	bool SetupIntrinsics () {
		this.IntrinsicsParameters = Utils.RedisTryGetIntrinsics (connection.GetDataAccessProvider (), BaseKey + IntrinsicsKey);
		if (this.IntrinsicsParameters == null) {
			Utils.Log (className, "Failed to load (and set) camera intrinsics parameters.");
			State = ComponentState.CONNECTED;
			return false;
		}
		Utils.Log (className, "Successfully loaded camera intrinsics parameters.");
		State = ComponentState.WORKING;
		return true;
	}

	bool SetupExtrinsics () {
		this.ExtrinsicsParameters = Utils.RedisTryGetExtrinsics (connection.GetDataAccessProvider (), BaseKey + ExtrinsicsKey);
		if (this.IntrinsicsParameters == null) {
			Utils.Log (className, "Failed to load (and set) camera extrinsics parameters.");
			State = ComponentState.CONNECTED;
			return false;
		}
		Utils.Log (className, "Successfully loaded camera extrinsics parameters.");
		State = ComponentState.WORKING;
		return true;
	}

	void Update () {
		if (State != ComponentState.WORKING) {
			if (State != ComponentState.CONNECTED) {
				Utils.Log (className, "Retrying to connect to the redis server.");
				Connect ();
			} else {
				Utils.Log (className, "Retrying to initialize camera parameters.");
				Initialize ();
			}
		}
	}
}

#if UNITY_EDITOR
[CustomEditor (typeof (QuickCameraSetup))]
public class QuickCameraSetupEditor : Editor {

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
	}
}
#endif