using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using TeamDev.Redis;

/// <summary>
/// 
/// </summary>
[ExecuteInEditMode]
public class SetupExtrinsics : MonoBehaviour, INectarService {
	public string Key = "extrinsics";
	[Tooltip("If set to true, a scale of -1 in Y will be applied to the object")]
	public bool ReverseY = true;
	[Tooltip("If set to true, object extrinsics parameters are going to be updated in every update loop. This is used for tracked object where the pose 3d is constantly changing.")]
	public bool KeepTracking = false;

	private RedisConnection connection;
	private RedisDataAccessProvider redis;
	private Subscriber subscriber;

	// Fake public variables (not shown in the inspector)
	public string objectName;
	public ComponentState state = ComponentState.DISCONNECTED;
	public ExtrinsicsParameters ExtrinsicsParameters;

	private string UnsubKey = "Kill";

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
		
		if (KeepTracking) {
			// Before that ask nectar is the service is up and running
			subscriber = new Subscriber(redis);
			subscriber.Subscribe(OnExtrinsicsReceived, objectName.ToLower() + ":" + Key, UnsubKey);
			state = ComponentState.WORKING;
		}
		else {
			bool isLoaded = Load();
			Utils.Log(objectName, this.GetType() + ": " + (isLoaded ? "succeed" : "failed") + ".", (isLoaded ? 0 : 1));
			state = isLoaded ? ComponentState.WORKING : ComponentState.CONNECTED;
		}
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

	void OnExtrinsicsReceived(string channelName, byte[] message) {
		if (channelName == UnsubKey) {
			subscriber.Unsubscribe(Key, UnsubKey);
			return;
		}
		string extrinsics = Utils.ByteToString(message);
		try {
			ExtrinsicsParameters = JsonUtility.FromJson<ExtrinsicsParameters>(extrinsics);
		}
		catch (Exception e) {
			Debug.Log(e.Message);
		}
	}

	void UpdateExtrinsics() {
		Matrix4x4 transRot = new Matrix4x4();
		transRot = Utils.FloatArrayToMatrix4x4(ExtrinsicsParameters.matrix);
		/*
		Matrix4x4 transform = new Matrix4x4();
		transform.SetRow(0, new Vector4(ExtrinsicsParameters.matrix[0],		ExtrinsicsParameters.matrix[1],		ExtrinsicsParameters.matrix[2],		ExtrinsicsParameters.matrix[3]));
		transform.SetRow(1, new Vector4(ExtrinsicsParameters.matrix[4],		ExtrinsicsParameters.matrix[5],		ExtrinsicsParameters.matrix[6],		ExtrinsicsParameters.matrix[7]));
		transform.SetRow(2, new Vector4(ExtrinsicsParameters.matrix[8],		ExtrinsicsParameters.matrix[9],		ExtrinsicsParameters.matrix[10],	ExtrinsicsParameters.matrix[11]));
		transform.SetRow(3, new Vector4(ExtrinsicsParameters.matrix[12],	ExtrinsicsParameters.matrix[13],	ExtrinsicsParameters.matrix[14],	ExtrinsicsParameters.matrix[15]));
		*/

		if (ReverseY) {
			Matrix4x4 scale = Matrix4x4.Scale(new Vector3(1, -1, 1));
			transRot = scale * transRot;
		}

		this.transform.localPosition = Utils.ExtractTranslation((Matrix4x4)transRot);
		this.transform.localRotation = Utils.ExtractRotation((Matrix4x4)transRot);
	}

	public bool IsWorking() {
		return this.state == ComponentState.WORKING;
	}

	// Update is called once per frame
	void Update () {
		if (!KeepTracking && state == ComponentState.WORKING) {
			return;
		}

		// If we keep tracking extrinsics updates, then update it everytime
		if (KeepTracking && state == ComponentState.WORKING) {
			UpdateExtrinsics();
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

	void OnApplicationQuit() {
		// If we were in subscribe mode (due to keep tracking) we unsubscribe
		if (KeepTracking)
			redis.SendCommand(RedisCommand.PUBLISH, UnsubKey, "");
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(SetupExtrinsics))]
public class SetupExtrinsicsEditor : Editor 
{
	//Creating serialized properties so we can retrieve variable attributes without having to recreate them in the custom editor
	SerializedProperty mscript = null;
	SerializedProperty reverseY = null;
	SerializedProperty keepTracking = null;

	private void OnEnable()
	{
		mscript = serializedObject.FindProperty("m_Script");
		reverseY = serializedObject.FindProperty("ReverseY");
		keepTracking = serializedObject.FindProperty("KeepTracking");
	}

    public override void OnInspectorGUI()
    {
        SetupExtrinsics script = (SetupExtrinsics)target;

		// This will show the current used script and make it clickable. When clicked, the script's code is open into the default editor.
		GUI.enabled = false;
     	EditorGUILayout.PropertyField(mscript, true, new GUILayoutOption[0]);
		GUI.enabled = true;

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

		// Control layout : [current state] [restart button]
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

		// Script options
		GUILayout.BeginHorizontal();
		EditorGUILayout.PropertyField(reverseY);
		EditorGUILayout.PropertyField(keepTracking);
		GUILayout.EndHorizontal();

		serializedObject.ApplyModifiedProperties();
    }
}
#endif
