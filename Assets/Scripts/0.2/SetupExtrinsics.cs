using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using TeamDev.Redis;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class SetupExtrinsics : MonoBehaviour {
	[Tooltip("The redis key where to look for extrinsics parameters.")]
	public string Key = "extrinsics";
	[Tooltip("If set to true, object extrinsics parameters are going to be updated in every update loop. This is used for tracked object where the pose 3d is constantly changing.")]
	public bool KeepTracking = false;
	[Tooltip("If set to true, a scale of -1 in Y will be applied to the object")]
	public bool ReverseY = true;

	private RedisConnection connection;
	private RedisDataAccessProvider redis;

	// Fake public variables (not shown in the inspector)
	public string objectName;
	public ComponentState state = ComponentState.DISCONNECTED;
	public ExtrinsicsParameters ExtrinsicsParameters;

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
		Utils.Log(objectName, this.GetType() + ": " + (isLoaded ? "succeed" : "failed") + ".");
		state = isLoaded ? ComponentState.WORKING : ComponentState.CONNECTED;
	}

	/* Load
	 * This function load data from Redis. If loading fails, it asks Nectar to start the desired service.
	 * Returns true if you data are succesfully loaded
	 */
	public bool Load() {
		ExtrinsicsParameters = Utils.RedisTryGetExtrinsics(redis, objectName.ToLower() + ":" + Key);
		// If data are not in redis, then we ask Nectar to set them
		if (ExtrinsicsParameters == null) {
			Utils.Log(objectName, "Data not found in Redis. If Nectar is up, we will restart the service", 1);
			// Ask nectar to start the associated service
			UnityWebRequest request = UnityWebRequest.Get("http://localhost:8124/nectar/" + objectName.ToLower() + "/extrinsics");
			request.SendWebRequest();
			// Retries to load the parameters once the request has been sent
			return false;
		}
		UpdateExtrinsics();
		return true;
	}

	void UpdateExtrinsics() {
		Matrix4x4 transform = new Matrix4x4();
		transform.SetRow(0, new Vector4(ExtrinsicsParameters.matrix[0],		ExtrinsicsParameters.matrix[1],		ExtrinsicsParameters.matrix[2],		ExtrinsicsParameters.matrix[3]));
		transform.SetRow(1, new Vector4(ExtrinsicsParameters.matrix[4],		ExtrinsicsParameters.matrix[5],		ExtrinsicsParameters.matrix[6],		ExtrinsicsParameters.matrix[7]));
		transform.SetRow(2, new Vector4(ExtrinsicsParameters.matrix[8],		ExtrinsicsParameters.matrix[9],		ExtrinsicsParameters.matrix[10],	ExtrinsicsParameters.matrix[11]));
		transform.SetRow(3, new Vector4(ExtrinsicsParameters.matrix[12],	ExtrinsicsParameters.matrix[13],	ExtrinsicsParameters.matrix[14],	ExtrinsicsParameters.matrix[15]));

		if (ReverseY) {
			Matrix4x4 scale = Matrix4x4.Scale(new Vector3(1, -1, 1));
			transform = scale * transform;
		}

		this.transform.localPosition = Utils.ExtractTranslation((Matrix4x4)transform);
		this.transform.localRotation = Utils.ExtractRotation((Matrix4x4)transform);
	}

	public bool IsWorking() {
		return this.state == ComponentState.WORKING;
	}

	// Update is called once per frame
	void Update () {
		if (!KeepTracking && state == ComponentState.WORKING) {
			return;
		}
		
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

[CustomEditor(typeof(SetupExtrinsics))]
public class SetupExtrinsicsEditor : Editor 
{
    public override void OnInspectorGUI()
    {
        SetupExtrinsics script = (SetupExtrinsics)target;

		// Extrinsics key layout : label [objcctName:](not editable) [key]
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
