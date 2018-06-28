using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class QuickCameraSetup : MonoBehaviour {
	
	private RedisConnection connection;
	private bool isConnected = false;

	public CameraType Type = CameraType.RGB;
	public string IntrinsicsKey = "camera0:calibration";
	public string ExtrinsicsKey = "camera0:extrinsincs";
	public string DataKey = "camera0:data";

	public bool HasData = true;
	public bool HasExtrinsics = false;

	[HideInInspector]
	public IntrinsicsParameters IntrinsicsParameters;
	public ExtrinsicsParameters ExtrinsicsParameters;
	
	// Use this for initialization
	void Start () {
		connection = new RedisConnection();
		isConnected = connection.TryConnection();
		Debug.Log("[" + this.GetType().Name + "] " + (isConnected ? "Connection succeed." : "Connexion failed."));
		if (!isConnected) {
			// TODO: Faire quelque chose quand le script qui gere la camera ne peut pas se connecter a redis.
			// - Crasher le programme.
			// - Creer un thread qui va continuellement essayer de se connecter ?
		}
		SetupIntrinsics();
		if (Type != CameraType.RGB) {
			SetupExtrinsics();
		}
	}


	bool SetupIntrinsics() {
		this.IntrinsicsParameters = Utils.RedisTryGetIntrinsics (connection.GetDataAccessProvider(), IntrinsicsKey);
		if (this.IntrinsicsParameters == null) {
			Debug.Log("Failed to set camera intrinsics parameters.");
			return false;
		}
		Debug.Log("Successfully set camera parameters.");
		return true;
	}

	bool SetupExtrinsics() {
		this.ExtrinsicsParameters = Utils.RedisTryGetExtrinsics (connection.GetDataAccessProvider(), ExtrinsicsKey);
		if (this.IntrinsicsParameters == null) {
			Debug.Log("Failed to set camera intrinsics parameters.");
			return false;
		}
		Debug.Log("Successfully set camera parameters.");
		return true;
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(QuickCameraSetup))]
public class QuickCameraSetupEditor : Editor {
	
	override public void OnInspectorGUI()
	{
		QuickCameraSetup setup = target as QuickCameraSetup;
		setup.Type = (CameraType)EditorGUILayout.EnumPopup("Camera Type:", setup.Type);
		setup.IntrinsicsKey = EditorGUILayout.TextField("Intrinsics parameters key:", setup.IntrinsicsKey);

		setup.HasExtrinsics = setup.Type != CameraType.RGB;
		if (setup.HasExtrinsics) {
			setup.ExtrinsicsKey = EditorGUILayout.TextField("Extrinsics parameters key:", setup.ExtrinsicsKey);
		}

		setup.HasData = (setup.Type == CameraType.RGB || setup.Type == CameraType.DEPTH);
		if (setup.HasData) {
			setup.DataKey = EditorGUILayout.TextField("Data key:", setup.DataKey);
		}
	}
}
#endif
