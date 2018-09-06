/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;

[ExecuteInEditMode]
public class SetupExtrinsics : MonoBehaviour {
	[Tooltip("The redis key where to look for extrinsics parameters.")]
	public string Key = "extrinsics";
	[Tooltip("If set to true, object extrinsics parameters are going to be updated in every update loop. This is used for tracked object where the pose 3d is constantly changing.")]
	public bool KeepTracking = false;
	[Tooltip("If set to true, a scale of -1 in Y will be applied to the object")]
	public bool ReverseY = true;
	
	private RedisConnection connection;
	private bool isConnected = false;

	// Fake public variables (not shown in the inspector)
	public string objectName;	
	public ComponentState state = ComponentState.DISCONNECTED;

	// If a gameobject is set there, happend his name as a parent to the key because the extrinsics parameters depends off it.
	public GameObject baseObject;

	// Use this for initialization
	void Start () {
		connection = new RedisConnection();
		Connect();
	}

	public void Connect() {
		if (connection == null) {
			connection = new RedisConnection();
			Connect();
		}
		isConnected = connection.TryConnection();
		state = isConnected ? ComponentState.CONNECTED : ComponentState.DISCONNECTED;
		Utils.Log(objectName, (isConnected ? "Redis connection succeed." : "Redis connection failed."));
		if (isConnected)
			Initialize();
	}

	public void Initialize() {
		// Update object name every time in case it has been renamed.
		objectName = transform.gameObject.name;
		bool extrinsicsUpdated = UpdateExtrinsics();
		Utils.Log(objectName, "Extrinsics parameters initialization " + (extrinsicsUpdated ? "succeed" : "failed") + ".");
		state = extrinsicsUpdated ? ComponentState.WORKING : ComponentState.CONNECTED;
	}

	bool UpdateExtrinsics() {
		ExtrinsicsParameters extrinsicsParameters = Utils.RedisTryGetExtrinsics(connection.GetDataAccessProvider(), objectName.ToLower() + ":" + Key);
		if (extrinsicsParameters == null) {
			Utils.Log(objectName, "Failed to update object extrinsics parameters.");
			return false;
		}

		Matrix4x4 transform = new Matrix4x4();
		transform.SetRow(0, new Vector4(extrinsicsParameters.matrix[0],		extrinsicsParameters.matrix[1],		extrinsicsParameters.matrix[2],		extrinsicsParameters.matrix[3]));
		transform.SetRow(1, new Vector4(extrinsicsParameters.matrix[4],		extrinsicsParameters.matrix[5],		extrinsicsParameters.matrix[6],		extrinsicsParameters.matrix[7]));
		transform.SetRow(2, new Vector4(extrinsicsParameters.matrix[8],		extrinsicsParameters.matrix[9],		extrinsicsParameters.matrix[10],	extrinsicsParameters.matrix[11]));
		transform.SetRow(3, new Vector4(extrinsicsParameters.matrix[12],	extrinsicsParameters.matrix[13],	extrinsicsParameters.matrix[14],	extrinsicsParameters.matrix[15]));

		if (ReverseY) {
			Matrix4x4 scale = Matrix4x4.Scale(new Vector3(1, -1, 1));
			transform = scale * transform;
		}

		this.transform.localPosition = Utils.ExtractTranslation((Matrix4x4)transform);
		this.transform.localRotation = Utils.ExtractRotation((Matrix4x4)transform);

		Utils.Log(objectName, "Successfully updated object extrinsics parameters.");
		state = ComponentState.WORKING;
		return true;
	}
	
	// Update is called once per frame
	void Update () {
		if (!KeepTracking && state == ComponentState.WORKING) {
			return;
		}

		switch(state) {
			case ComponentState.DISCONNECTED:
				Connect();
				break;
			case ComponentState.CONNECTED:
				Initialize();
				break;
			case ComponentState.WORKING:
				UpdateExtrinsics();
				break;
			default:
				break;
		}
	}
}

[CustomEditor(typeof(SetupExtrinsics))]
public class SetupExtrinsicsEditor : Editor 
{
    public override void OnInspectorGUI()
    {
        SetupExtrinsics script = (SetupExtrinsics)target;

		// Intrinsics key layout : label [objcctName:](not editable) [key]
		GUILayout.BeginHorizontal();
		EditorGUILayout.LabelField(new GUIContent("Key", "The redis key where to look for intrinsics parameters. The key prefix is defined by the GameObject name."));
		// Non modifiable value so disable GUI, print and renable it.
		GUI.enabled = false;
		EditorGUILayout.TextField(script.objectName.ToLower() + ":");
		GUI.enabled = true;
		// Key text field
		script.Key = EditorGUILayout.TextField (script.Key);
		GUILayout.EndHorizontal();

		// Control layout : [current state] [restart button] [update button]
		GUILayout.BeginHorizontal();
		GUI.enabled = false;
		script.state = (ComponentState)EditorGUILayout.EnumPopup("Internal state", script.state);
		GUI.enabled = true;
		if (GUILayout.Button("Restart")) {
			script.Connect();
		}
		if (GUILayout.Button("Force update")) {
			script.Initialize();
		}
		GUILayout.EndHorizontal();

		// If setup intrinsics already added buttons, do not redo it.
		if (script.gameObject.GetComponent<SetupIntrinsics>() == null) {
			// Start stop restart buttons to tell nectar to start, stop a service
			GUILayout.BeginHorizontal();
			if (GUILayout.Button(new GUIContent("Start", "Ask Nectar to start the desired service."))) {
				// Start logic goes here (http request)
				UnityWebRequest request = UnityWebRequest.Get("http://localhost:8124/nectar/" + script.objectName.ToLower() + "/start");
				request.SendWebRequest();
			}
			
			if (GUILayout.Button(new GUIContent("Stop", "Ask Nectar to stop the desired service."))) {
				// Stop logic goes here (http request)
				UnityWebRequest request = UnityWebRequest.Get("http://localhost:8124/nectar/" + script.objectName.ToLower() + "/stop");
				request.SendWebRequest();
			}
			
			if (GUILayout.Button(new GUIContent("Restart", "Ask Nectar to restart the desired service."))) {
				// Stop logic goes here (http request)
				UnityWebRequest request = UnityWebRequest.Get("http://localhost:8124/nectar/" + script.objectName.ToLower() + "/restart");
				request.SendWebRequest();
			}
			GUILayout.EndHorizontal();
		}
    }
}
*/
