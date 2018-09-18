using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TeamDev.Redis;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Networking;

/// <summary>
/// CameraPlayback provides the base logic to deal with real time camera frames obtained from a Nectar service.
/// It also provides a simple configuration menu and a simple visualisation of the obtained frames.
/// </summary>
[ExecuteInEditMode]
public class CameraPlayback : MonoBehaviour, NectarService {
	/// <summary>
	/// The camera from which frames will be acquired.
	/// This property is configurable in editor
	/// </summary>
	[Tooltip("The camera to which the player should get frames from.")]
	public Camera ARCamera;

	/// <summary>
	/// Data structure holding redis connection properties such as Host, Port, Timeout and more.
	/// </summary>
	/// 
	private RedisConnection connection;

	/// <summary>
	/// Redis connection object containing the connection socket. 
	/// </summary>
	private RedisDataAccessProvider redis;
	
	/// <summary>
	/// Redis subscriber 
	/// </summary>
	private Subscriber subscriber;
	
	/// <summary>
	/// State boolean indicating whether or not the script is connected to Redis.
	/// </summary>
	private bool isConnected = false;

	/// <summary>
	/// Fake public variable (not accessible from the editor) getting the current object name to use it as a base key to get data in Redis
	/// </summary>
	public string objectName;
	
	/// <summary>
	/// The current state of the component. Can be DISCONNECTED, CONNECTED or WORKING.
	/// DISCONNECTED - When the component fails to connect to Redis.
	/// CONNECTED - When the component is successfully connected to Redis but failed to initialize its data.
	/// The initialization can fail for several reasons. Most common one is that the data in Redis are not available due to the lack of the service providing them.
	/// WORKING - When the component is connected and initializated.
	/// </summary>
	public ComponentState state = ComponentState.DISCONNECTED;
	
	/// <summary>
	/// State boolean used to override the key (which is by default the component name) when neeeded.
	/// </summary>
	[Tooltip("Usually the key is based on the component name however by checking this property you have the ability to override the predefined key.")]
	public bool OverrideKey = false;

	/// <summary>
	/// String key used to get data from Redis.
	/// </summary>
	[Tooltip("The redis key to get frame data from.")]
	public string Key;

	/// <summary>
	/// Camera frame width
	/// </summary>
	private int imageWidth;

	/// <summary>
	/// Camera frame height
	/// </summary>
	private int imageHeight;

	/// <summary>
	/// Camera frame channels
	/// </summary>
	private int imageChannels;
	
	/// <summary>
	/// The Unity 2D Texture storing the current frame.
	/// </summary>
	public Texture2D videoTexture;

	/// <summary>
	/// Byte array holding the current frame data
	/// </summary>
	private byte[] imageData;

	/// <summary>
	/// State boolean indicating whether a new image has been uploaded or not.
	/// </summary>
	private bool imageDataUpdated = false;

	/// <summary>
	/// The previous frame informations (frameCount, timestamp)
	/// </summary>
	private ImageInformations previousImage;

	/// <summary>
	/// The current frame informations (frameCount, timestamp)
	/// </summary>
	private ImageInformations currentImage;

	private RawImage rawImage;


	/// <summary>
	/// Start method called by Unity main loop.
	/// This function is used for initialization
	/// </summary>
	void Start () {
		Connect();
	}

	/// <summary>
	/// Creates a new connection to Redis and tries to connect.
	/// If succeed, this function will call the initialization fuction.  
	/// </summary>
	public void Connect() {
		// Since this has to work in editor, we are getting component informations each time we try to connect/init in case they changed
		objectName = transform.gameObject.name;

		if (connection != null) {
			connection.GetDataAccessProvider().Dispose();
		}
		
		connection = new RedisConnection();
		bool redisConnected = connection.TryConnection();
		state = redisConnected ? ComponentState.CONNECTED : ComponentState.DISCONNECTED;
		Utils.Log(objectName, "Redis connection: " + (redisConnected ? "succeed." : "failed."), (redisConnected ? 0 : 1));
		if (redisConnected) {
			redis = connection.GetDataAccessProvider();
			Initialize();
		}
	}

	/// <summary>
	/// Initialize and test everything the component need to work.
	/// If succeed, the component is ready to be used.
	/// </summary>
	public void Initialize() {
		// Since this has to work in editor, we are getting component informations each time we try to connect/init in case they changed
		objectName = transform.gameObject.name;

		rawImage = this.GetComponent<RawImage> ();
		if (rawImage == null) {
			Utils.Log(objectName, "Failed to initialize camera playback. A raw image component is required.", 2);
			return;
		}

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
	
	/// <summary>
	/// This function load data from Redis. If loading fails, it asks Nectar to start the desired service. 
	/// Returns true if you data are succesfully loaded
	/// </summary>
	/// <returns>true if the service is up and the data available, false else.</returns>
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

	/// <summary>
	/// Callback method called by the Redis Subscriber when image data are published in the listened channel.
	/// </summary>
	/// <param name="channelName">The channel where data were published</param>
	/// <param name="message">The published data</param>
	void OnImageReceived (string channelName, byte[] message) {
		Debug.Log("Frame received :" + channelName);
		// This code here is causing TeamDev.Redis to throw null object exception when OnImageReceived is called from event raise (subscriber -> LanguageMessaging).
		//string rawImageInfos = Utils.ByteToString (message);
		// This line in particular seems to be the reason of the trouble -> Just set the byte data and do the checking stuff in update ?
		//currentImage = JsonUtility.FromJson<ImageInformations> (rawImageInfos);
		/*if (currentImage.imageCount != previousImage.imageCount) {
			previousImage = currentImage;
			imageDataUpdated = true;
		}*/
		imageDataUpdated = true;
	}
	
	/// <summary>
	/// Update method called by Unity main loop.
	/// This function is called exactly once per frame.
	/// </summary>
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
			return;
		}
		
		if (redis == null) {
			Connect();
			return;
		}

		if (imageDataUpdated) {
			Utils.GetImageIntoPreallocatedTexture(redis, Key, videoTexture, imageData, imageWidth, imageHeight, imageChannels);
			/*
			// Get image data
			int commandId = redis.SendCommand (RedisCommand.GET, Key);
			// 70-90% of the update time is spent here.
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
				*/
				rawImage.texture = videoTexture;
				imageDataUpdated = false;
			}
		}
	}
//}

#if UNITY_EDITOR
/// <summary>
/// Unity Editor custom class overriding Unity Editor default layout to provide more friendly and comprehensive GUI for the user.
/// </summary>
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
			UnityWebRequest request = UnityWebRequest.Get("http://localhost:8124/nectar/" + script.Key + "/start");
			request.SendWebRequest();
			//script.Connect();
		}
		if (GUILayout.Button(new GUIContent("Stop", "Stop the associated camera service"))) {
			UnityWebRequest request = UnityWebRequest.Get("http://localhost:8124/nectar/" + script.Key + "/stop");
			request.SendWebRequest();
		}
		if (GUILayout.Button(new GUIContent("Restart", "Restart the associated camera service"))) {
			UnityWebRequest request = UnityWebRequest.Get("http://localhost:8124/nectar/" + script.Key + "/restart");
			request.SendWebRequest();
		}
		if (GUILayout.Button(new GUIContent("Test", "Test the current camera and display visual feedback"))) {
			UnityWebRequest request = UnityWebRequest.Get("http://localhost:8124/nectar/" + script.Key + "/test");
			request.SendWebRequest();
		}
		GUILayout.EndHorizontal();

		GUILayout.Label(script.videoTexture);

		// Find all the PropertyField and apply layout and style to them so they can be displayed
		serializedObject.ApplyModifiedProperties();
	}
	
}
#endif
