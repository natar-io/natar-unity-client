using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using TeamDev.Redis;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class SetupIntrinsics : MonoBehaviour {
	public string Key = "intrisics";

	private RedisConnection connection;
	private RedisDataAccessProvider redis;
	private bool isConnected = false;

	// This is only public to deal with it in custom inspector
	public string objectName;
	public ComponentState state = ComponentState.DISCONNECTED;

    private Camera camera;

	// Use this for initialization
	void Start () {
		Connect();
	}

	public void Connect() {
		objectName = transform.gameObject.name;
		camera = this.GetComponent<Camera>();

		if (connection == null) {
			connection = new RedisConnection();
		}
		isConnected = connection.TryConnection();
		state = isConnected ? ComponentState.CONNECTED : ComponentState.DISCONNECTED;
		Utils.Log(objectName, (isConnected ? "Redis connection succeed." : "Redis connection failed."));
		if (isConnected) {
			redis = connection.GetDataAccessProvider();
			Initialize();
		}
	}

	public void Initialize() {
		// Update object name every time in case it has been renamed.
		objectName = transform.gameObject.name;
		bool intrinsicsUpdated = SetIntrinsics();
		Utils.Log(objectName, "Intrinsics parameters initialization " + (intrinsicsUpdated ? "succeed" : "failed") + ".");
		state = intrinsicsUpdated ? ComponentState.WORKING : ComponentState.CONNECTED;
	}

	bool SetIntrinsics() {
		IntrinsicsParameters intrinsicsParameters = Utils.RedisTryGetIntrinsics(redis, objectName.ToLower() + ":" + Key);
		if (intrinsicsParameters == null) {
			return false;
		}
        
		float near = camera.nearClipPlane;
		float far = camera.farClipPlane;

		Matrix4x4 projectionMatrix = new Matrix4x4 ();
		Vector4 row0 = new Vector4 ((2f * intrinsicsParameters.fx / intrinsicsParameters.width), 0f, -((float) intrinsicsParameters.cx / (float) intrinsicsParameters.width * 2f - 1f), 0f);
		Vector4 row1 = new Vector4 (0f, 2f * intrinsicsParameters.fy / intrinsicsParameters.height, -((float) intrinsicsParameters.cy / (float) intrinsicsParameters.height * 2f - 1f),0f);
		Vector4 row2 = new Vector4 (0, 0, -(far + near) / (far - near), -near * (1 + (far + near) / (far - near)));
		Vector4 row3 = new Vector4 (0, 0, -1, 0);

		projectionMatrix.SetRow (0, row0);
		projectionMatrix.SetRow (1, row1);
		projectionMatrix.SetRow (2, row2);
		projectionMatrix.SetRow (3, row3);
		camera.projectionMatrix = projectionMatrix;

		state = ComponentState.WORKING;
		return true;
	}
	
	// Update is called once per frame
	void Update () {
		switch (state) {
			case ComponentState.DISCONNECTED:
				Connect();
				break;
			case ComponentState.CONNECTED:
				Initialize();
				break;
			default:
				break;
		}
	}
}

[CustomEditor(typeof(SetupIntrinsics))]
public class SetupIntrinsicsEditor : Editor 
{
    public override void OnInspectorGUI()
    {
        SetupIntrinsics script = (SetupIntrinsics)target;

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

		// Start stop buttons to tell nectar to start, stop a service
		GUILayout.BeginHorizontal();
		if (GUILayout.Button(new GUIContent("Start", "Ask Nectar to start the desired service."))) {
			// Start logic goes here (http request)
			UnityWebRequest request = UnityWebRequest.Get("http://localhost:8124/nectar/" + script.objectName.ToLower() + "/start");
			request.SendWebRequest();
		}
		
		if (GUILayout.Button(new GUIContent("Stop", "Ask Nectar to stop the desired service."))) {
			// Stop logic goes here (http request)
		}
		GUILayout.EndHorizontal();
    }
}
