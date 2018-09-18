using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using TeamDev.Redis;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class SetupIntrinsics : MonoBehaviour, NectarService {
	public string Key = "intrisics";

	private RedisConnection connection;
	private RedisDataAccessProvider redis;

	// Fake public variables (not shown in the inspector)
	public string objectName;
	public ComponentState state = ComponentState.DISCONNECTED;
	public IntrinsicsParameters IntrinsicsParameters;

    private Camera targetCamera;

	// Use this for initialization
	void Start () {
		Connect();
	}

	/* Connect
	*  This function creates a new connection to Redis and tries to join it.
	*  If succeed, this function will call the initialization fuction. 
	*/
	public void Connect() {
		// Since this has to work in editor, we are getting component informations each time we try to connect/init in case they changed
		objectName = transform.gameObject.name;
		targetCamera = this.GetComponent<Camera>();

		if (connection == null) {
			connection = new RedisConnection();
		}
		bool redisConnected = connection.TryConnection();
		state = redisConnected ? ComponentState.CONNECTED : ComponentState.DISCONNECTED;
		Utils.Log(objectName, "Redis connection: " + (redisConnected ? "succeed." : "failed."), (redisConnected ? 0 : 1));
		if (redisConnected) {
			redis = connection.GetDataAccessProvider();
			Initialize();
		}
	}

	/* Initiliaze
	 * This function initialize everything the script/component needs to work.
	 * If succeed, the component can be used
	 */
	public void Initialize() {
		// Since this has to work in editor, we are getting component informations each time we try to connect/init in case they changed
		objectName = transform.gameObject.name;
		bool isLoaded = Load();
		Utils.Log(objectName, this.GetType() + ": " + (isLoaded ? "succeed" : "failed") + ".", (isLoaded ? 0 : 1));
		state = isLoaded ? ComponentState.WORKING : ComponentState.CONNECTED;
	}

	/* Load
	 * This function load data from Redis. If loading fails, it asks Nectar to start the desired service.
	 * Returns true if you data are succesfully loaded
	 */
	public bool Load() {
		IntrinsicsParameters = Utils.RedisTryGetIntrinsics(redis, objectName.ToLower() + ":" + Key);
		// If data are not in redis, then we ask Nectar to set them
		if (IntrinsicsParameters == null) {
			Utils.Log(objectName, "Data not found in Redis. If Nectar is up, we will restart the service", 1);
			// Ask nectar to start the associated service
			UnityWebRequest request = UnityWebRequest.Get("http://localhost:8124/nectar/" + objectName.ToLower() + "/intrinsics");
			request.SendWebRequest();
			// Retries to load the parameters once the request has been sent
			return false;
		}
		UpdateIntrinsics();
		return true;
	}

	void UpdateIntrinsics() {
		float near = targetCamera.nearClipPlane;
		float far = targetCamera.farClipPlane;

		Matrix4x4 projectionMatrix = new Matrix4x4 ();
		Vector4 row0 = new Vector4 ((2f * IntrinsicsParameters.fx / IntrinsicsParameters.width), 0f, -((float) IntrinsicsParameters.cx / (float) IntrinsicsParameters.width * 2f - 1f), 0f);
		Vector4 row1 = new Vector4 (0f, 2f * IntrinsicsParameters.fy / IntrinsicsParameters.height, -((float) IntrinsicsParameters.cy / (float) IntrinsicsParameters.height * 2f - 1f),0f);
		Vector4 row2 = new Vector4 (0, 0, -(far + near) / (far - near), -near * (1 + (far + near) / (far - near)));
		Vector4 row3 = new Vector4 (0, 0, -1, 0);

		projectionMatrix.SetRow (0, row0);
		projectionMatrix.SetRow (1, row1);
		projectionMatrix.SetRow (2, row2);
		projectionMatrix.SetRow (3, row3);
		targetCamera.projectionMatrix = projectionMatrix;
	}

	public bool IsWorking() {
		return this.state == ComponentState.WORKING;
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

#if UNITY_EDITOR
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
		if (GUILayout.Button("Reinitialize")) {
			script.Connect();
		}
		GUILayout.EndHorizontal();

		// Start stop buttons to tell nectar to start, stop a service
		GUILayout.BeginHorizontal();
		if (GUILayout.Button(new GUIContent("Load", "Ask Nectar to load the intrinsics parameters if they are not already loaded."))) {
			if (script.state == ComponentState.DISCONNECTED) {
				script.Connect();
			}
			else {
				script.Initialize();
			}
		}
		GUILayout.EndHorizontal();
    }
}
#endif
