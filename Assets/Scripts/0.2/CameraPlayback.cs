using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TeamDev.Redis;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Networking;

[ExecuteInEditMode]
public class CameraPlayback : MonoBehaviour, NectarService {
	[Tooltip("The camera to which the player should get frames from.")]
	public Camera ARCamera;

	private RedisDataAccessProvider redis;
	private RedisConnection connection;
	private Subscriber subscriber;
	private bool isConnected = false;

	// Fake public variables (not shown in the inspector)
	public string objectName;
	public ComponentState state = ComponentState.DISCONNECTED;
	
	[Tooltip("Usually the key is based on the component name however by checking this property you have the ability to override the predefined key.")]
	public bool OverrideKey = false;
	[Tooltip("The redis key to get frame data from.")]
	public string Key;

	private int imageWidth;
	private int imageHeight;
	private int imageChannels;
	
	public Texture2D videoTexture;
	private byte[] imageData;
	private bool imageDataUpdated = false;
	private ImageInformations previousImage;
	private ImageInformations currentImage;

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

		if (ARCamera == null) {
			Utils.Log(objectName, "A camera component needs to be attached to this script. Please add a camera in the correct field and try again.", 2);
			return;
		}

		if (!OverrideKey)
			Key = ARCamera.name.ToLower();

		bool isLoaded = Load();
		Utils.Log(objectName, this.GetType() + ": " + (isLoaded ? "succeed" : "failed") + ".");
		state = isLoaded ? ComponentState.WORKING : ComponentState.CONNECTED;
		if (!isLoaded) {
			return;
		}
		
		videoTexture = new Texture2D (imageWidth, imageHeight, TextureFormat.RGB24, false);
		imageData = new byte[imageHeight * imageWidth * imageChannels];
		// if (subscriber == null) -> fix the multiple subscription problem but causes an undefined object ref when it crashes but is not null
		subscriber = new Subscriber(redis);
		subscriber.Subscribe(Key, OnImageReceived);
		
	}

	/* Load
	 * This function load data from Redis. If loading fails, it asks Nectar to start the desired service.
	 * Returns true if you data are succesfully loaded
	 */
	public bool Load() {
		//TODO: Check here if Nectar AND the service are up otherwise when the service is down and we click start, we're screwed.
		int commandId =  redis.SendCommand (RedisCommand.GET, Key + ":width");
		int? width = Utils.RedisTryReadInt(redis, commandId);

		commandId =  redis.SendCommand (RedisCommand.GET, Key + ":height");
		int? height = Utils.RedisTryReadInt(redis, commandId);

		commandId =  redis.SendCommand (RedisCommand.GET, Key + ":channels");
		int? channels = Utils.RedisTryReadInt(redis, commandId);

		if (width == null || height == null || channels == null) {
			Utils.Log(objectName, "Data not found in Redis. If Nectar is up, we will restart the service", 1);
			// Ask nectar to start the associated service
			UnityWebRequest request = UnityWebRequest.Get("http://localhost:8124/nectar/" + Key + "/start");
			request.SendWebRequest();
			return false;
		}

		imageWidth = (int) width;
		imageHeight = (int) height;
		imageChannels = (int) channels;
		return true;
	}

	void OnImageReceived (string channelName, byte[] message) {
		string rawImageInfos = Utils.ByteToString (message);
		currentImage = JsonUtility.FromJson<ImageInformations> (rawImageInfos);
		if (currentImage.imageCount != previousImage.imageCount) {
			previousImage = currentImage;
			imageDataUpdated = true;
		}
	}
	
	// Update is called once per frame
	void Update () {	
		if (this.state != ComponentState.WORKING) {
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
		else if (imageDataUpdated) {
			// Get image data
			int commandId = redis.SendCommand (RedisCommand.GET, Key);
			imageData = Utils.RedisTryReadData (redis, commandId);
			if (imageData != null) {
				if (imageChannels == 2) {
					Debug.Log(imageData.Length);
					byte[] imageDataRGB = Utils.GRAY16ToRGB24(imageWidth, imageHeight, imageData);
					videoTexture.LoadRawTextureData (imageDataRGB);
				}
				else {
					videoTexture.LoadRawTextureData (imageData);
				}
				videoTexture.Apply();
				imageDataUpdated = false;
			}
		}
	}
}

[CustomEditor(typeof(CameraPlayback))]
public class CameraPlaybackEditor : Editor 
{
	//Creating serialized properties so we can retrieve variable attributes without having to recreate them in the custom editor
	SerializedProperty arCamera = null;
	SerializedProperty overrideKey = null;
	SerializedProperty key = null;

	private void OnEnable()
	{
		arCamera = serializedObject.FindProperty("ARCamera");
		overrideKey = serializedObject.FindProperty("OverrideKey");
		key = serializedObject.FindProperty("Key");
	}

    public override void OnInspectorGUI()
    {
        CameraPlayback script = (CameraPlayback)target;
		//script.ARCamera = (Camera)EditorGUILayout.ObjectField(new GUIContent("Camera", "The camera to which the player should get frames from."), script.ARCamera, typeof(Camera), true);
		EditorGUILayout.PropertyField(arCamera);
		EditorGUILayout.PropertyField(overrideKey);
		if (script.OverrideKey) {
			EditorGUILayout.PropertyField(key);
		}

		// Control layout : [current state] [restart button]
		GUILayout.BeginHorizontal();
		GUI.enabled = false;
		script.state = (ComponentState)EditorGUILayout.EnumPopup("Internal state", script.state);
		GUI.enabled = true;
		if (GUILayout.Button("Reinitialize")) {
			script.Connect();
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button(new GUIContent("Start", "Start the associated camera service"))) {
			script.Connect();
		}
		if (GUILayout.Button(new GUIContent("Stop", "Stop the associated camera service"))) {
			UnityWebRequest request = UnityWebRequest.Get("http://localhost:8124/nectar/" + script.Key + "/stop");
			request.SendWebRequest();
		}
		if (GUILayout.Button(new GUIContent("Restart", "Restart the associated camera service"))) {
			UnityWebRequest request = UnityWebRequest.Get("http://localhost:8124/nectar/" + script.Key + "/restart");
			request.SendWebRequest();
		}
		GUILayout.EndHorizontal();

		GUILayout.Label(script.videoTexture);

		// Find all the PropertyField and apply layout and style to them so they can be displayed
		serializedObject.ApplyModifiedProperties();
	}
	
}
