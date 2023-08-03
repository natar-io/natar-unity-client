using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System; // Exception
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
public class CameraPlayback : MonoBehaviour, INectarService {
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
	/// Fake public variable (not accessible from the editor) getting the current object name to use it as a base key to get data in Redis
	/// </summary>
	public string objectName;

  /// <summary>
	/// Decode a 16Bit depth video feed instead of 8bit RGB, like in Orbbec depth cameras.
	/// </summary>
  [Tooltip("Is a Depth camera.")]
	public bool Use16BitDepth = false;

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

	private string UnsubKey = "Kill";
	/// <summary>
	/// 
	/// </summary>
	[Tooltip("The destination RawImage.")]
	public RawImage OutputImage;

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

		currentImage = new ImageInformations();
		previousImage = new ImageInformations();
	}

	/// <summary>
	/// Initialize and test everything the component need to work.
	/// If succeed, the component is ready to be used.
	/// </summary>
	public void Initialize() {
		// Since this has to work in editor, we are getting component informations each time we try to connect/init in case they changed
		objectName = transform.gameObject.name;

		if (OutputImage == null) {
			Utils.Log(objectName, "A Raw Image component needs to be specified. Please add it into the correct field and try again.", 2);
			return;
		}

		if (ARCamera == null) {
			Utils.Log(objectName, "A Camera component needs to be specified. Please add it into the correct field and try again.", 2);
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

		// imageData = new byte[imageHeight * imageWidth * imageChannels];
    imageData = new byte[imageHeight * imageWidth * 3];

		// if (subscriber == null) -> fix the multiple subscription problem but causes an undefined object ref when it crashes but is not null
		subscriber = new Subscriber(redis);
		subscriber.Subscribe(OnImageReceived, Key, UnsubKey);
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
		// Debug.Log("Frame received :" + channelName);
		if (channelName == UnsubKey) {
			subscriber.Unsubscribe(Key, UnsubKey);
			return;
		}
		/*
		// This code here is causing TeamDev.Redis to throw null object exception when OnImageReceived is called from event raise (subscriber -> LanguageMessaging).
		string rawImageInfos = Utils.ByteToString (message);
		// This line in particular seems to be the reason of the trouble -> Just set the byte data and do the checking stuff in update ?
		try {
			currentImage = JsonUtility.FromJson<ImageInformations> (rawImageInfos);
			if (currentImage != null && currentImage.imageCount != previousImage.imageCount) {
				previousImage = currentImage;
				imageDataUpdated = true;
			}
			else {
				imageDataUpdated = false;
			}
		}
		catch (Exception e) {
			Debug.Log(e.StackTrace);
		}
		*/
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

      // Force depth image with 2 channels... even if there are there 3 for now.
			// Utils.GetImageIntoPreallocatedTexture(redis, Key, videoTexture, imageData, imageWidth, imageHeight, imageChannels);

      if(Use16BitDepth){ 
        float[] depthImage = Utils.DecodeDepthImage(redis, Key, imageData, imageWidth, imageHeight, imageChannels);
        Utils.loadDepthImageToIntoPreallocatedTexture(depthImage, videoTexture, imageData, imageWidth, imageHeight, imageChannels);
      } else {
         Utils.GetImageIntoPreallocatedTexture(redis, Key, videoTexture, imageData, imageWidth, imageHeight, imageChannels);
      }
      OutputImage.texture = videoTexture;
      imageDataUpdated = false;
		}
	}

	void OnApplicationQuit() {
		redis.SendCommand(RedisCommand.PUBLISH, UnsubKey, "");
	}
}

#if UNITY_EDITOR
/// <summary>
/// Unity Editor custom class overriding Unity Editor default layout to provide more friendly and comprehensive GUI for the user.
/// </summary>
[CustomEditor(typeof(CameraPlayback))]
public class CameraPlaybackEditor : Editor 
{
	private bool paramsFoldout = true;
	private bool nectarFoldout = true;
	private bool controlFoldout = true;
	private bool videoFoldout = false;

	//Creating serialized properties so we can retrieve variable attributes without having to recreate them in the custom editor
	SerializedProperty mscript = null;
	SerializedProperty arCamera = null;
	SerializedProperty overrideKey = null;
  SerializedProperty use16BitDepth = null;
	SerializedProperty key = null;
	SerializedProperty outputImage = null;

	private void OnEnable()
	{

		mscript = serializedObject.FindProperty("m_Script");
		arCamera = serializedObject.FindProperty("ARCamera");
		overrideKey = serializedObject.FindProperty("OverrideKey");
    use16BitDepth = serializedObject.FindProperty("Use16BitDepth");
    //	public bool Use16BitDepth = false;

		key = serializedObject.FindProperty("Key");
		outputImage = serializedObject.FindProperty("OutputImage");
	}

    public override void OnInspectorGUI()
    {
		GUIStyle foldoutStyle = EditorStyles.foldout;
		foldoutStyle.fontStyle = FontStyle.Bold;

        CameraPlayback script = (CameraPlayback)target;

		// This will show the current used script and make it clickable. When clicked, the script's code is open into the default editor.
		GUI.enabled = false;
     	EditorGUILayout.PropertyField(mscript, true, new GUILayoutOption[0]);
		GUI.enabled = true;
		
		GUILayout.Space(5);
	  EditorGUILayout.PropertyField(use16BitDepth);

		paramsFoldout = EditorGUILayout.Foldout(paramsFoldout, "Parameters", foldoutStyle);
		if (paramsFoldout) 
		{
			EditorGUILayout.PropertyField(arCamera);
			EditorGUILayout.PropertyField(overrideKey);
			if (script.OverrideKey) {
				EditorGUILayout.PropertyField(key);
			}
			EditorGUILayout.PropertyField(outputImage);

			GUILayout.Space(5);
		}
		
		controlFoldout = EditorGUILayout.Foldout(controlFoldout, "Control", foldoutStyle);
		if (controlFoldout) 
		{
			GUILayout.BeginHorizontal();
			GUI.enabled = false;
			script.state = (ComponentState)EditorGUILayout.EnumPopup("Internal state", script.state);
			GUI.enabled = true;
			if (GUILayout.Button("Reinitialize")) {
				script.Connect();
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(5);
		}

		nectarFoldout = EditorGUILayout.Foldout(nectarFoldout, "Nectar", foldoutStyle);
		if (nectarFoldout) 
		{
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
			EditorGUILayout.HelpBox("This buttons provide simple ways to control the current service. It allows you to ask Nectar to do several operations.", MessageType.Info);
		}

		videoFoldout = EditorGUILayout.Foldout(videoFoldout, "Output preview", foldoutStyle);
		if (videoFoldout) 
		{
			GUIStyle videoOutputStyle = new GUIStyle();
			videoOutputStyle.clipping = TextClipping.Clip;
			videoOutputStyle.fixedHeight = 160;
			videoOutputStyle.fixedWidth = 213;
			videoOutputStyle.alignment = TextAnchor.MiddleCenter;
			GUILayout.Label(script.videoTexture, videoOutputStyle);
			if (GUILayout.Button(new GUIContent("Refresh", "Refresh the video output."))) { 
				EditorUtility.SetDirty(target);
			}
		}

		// Find all the PropertyField and apply layout and style to them so they can be displayed
		serializedObject.ApplyModifiedProperties();
	}
	
}
#endif
