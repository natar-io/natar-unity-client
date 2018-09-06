using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TeamDev.Redis;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class CameraPlayback : MonoBehaviour {

	public Camera ARCamera;

	private RedisDataAccessProvider redis;
	private RedisConnection connection;
	private Subscriber subscriber;
	private bool isConnected = false;

	// Fake public variables (not shown in the inspector)
	public string objectName;
	public ComponentState state = ComponentState.DISCONNECTED;

	public Texture2D videoTexture;
	private byte[] imageData;
	private ImageInformations previousImage;
	private ImageInformations currentImage;

	private bool imageDataUpdated = false;

	// Use this for initialization
	void Start () {
		Connect();
	}

	public void Connect() {
		objectName = transform.gameObject.name;

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
		
		if (ARCamera == null) {
			Utils.Log(objectName, "A camera component needs to be attached to this script. Please add a camera in the correct field and try again.");
			return;
		}

		SetupIntrinsics intrinsics = ARCamera.GetComponent<SetupIntrinsics>();
		if (intrinsics == null) {
			Utils.Log(objectName, "Attached camera should contain the SetupIntrinsics script. Add the scripts to your camera and start again.");
			return;
		}

		Utils.Log(objectName, "Camera Playback initialization " + (true ? "succeed" : "failed") + ".");
		state = true ? ComponentState.WORKING : ComponentState.CONNECTED;

		videoTexture = new Texture2D (640, 480, TextureFormat.RGB24, false);
		imageData = new byte[640 * 480 * 3];
		if (subscriber == null) {
			subscriber = new Subscriber(redis);
		}
		subscriber.Subscribe(ARCamera.name.ToLower(), OnImageReceived);
	}

	void OnImageReceived (string channelName, byte[] message) {
		string rawImageInfos = Utils.ByteToString (message);
		currentImage = JsonUtility.FromJson<ImageInformations> (rawImageInfos);
		Utils.Log(objectName, "Frame " + currentImage.imageCount + ": " + currentImage.timestamp);
		if (currentImage.imageCount != previousImage.imageCount) {
			previousImage = currentImage;
			imageDataUpdated = true;
		}
	}
	
	// Update is called once per frame
	void Update () {
		if (this.state == ComponentState.DISCONNECTED) {
			Connect();
		}

		if (imageDataUpdated) {
			int commandId = redis.SendCommand (RedisCommand.GET, ARCamera.name);
			imageData = Utils.RedisTryReadData (redis, commandId);
			if (imageData != null) {	
				videoTexture.LoadRawTextureData (imageData);
				videoTexture.Apply();
				imageDataUpdated = false;
			}
		}
	}
}
