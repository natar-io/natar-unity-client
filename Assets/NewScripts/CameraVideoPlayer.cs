using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TeamDev.Redis;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class CameraVideoPlayer : MonoBehaviour {

	public ComponentState State = ComponentState.DISCONNECTED;
	public Camera ARCamera;

	private string className;
	private QuickCameraSetup ARCameraSetup;

	private RedisDataAccessProvider redis;
	private RedisConnection connection;
	private Subscriber subscriber;
	private RawImage cameraImagePlayback;

	private Texture2D videoTexture;
	private byte[] imageData;

	private bool isConnected = false;

	// Use this for initialization
	void Start () {
		className = transform.gameObject.name;
		cameraImagePlayback = this.GetComponent<RawImage> ();
		ARCameraSetup = ARCamera.GetComponent<QuickCameraSetup> ();
		Connect ();
	}

	void Connect () {
		if (connection == null) {
			connection = new RedisConnection ();
		}
		isConnected = connection.TryConnection ();
		Utils.Log (className, (isConnected ? "Connection succeed." : "Connection failed."));
		if (!isConnected) {
			State = ComponentState.DISCONNECTED;
			return;
		}
		State = ComponentState.CONNECTED;
		Initialize ();
	}

	void Initialize () {
		if (ARCameraSetup.State != ComponentState.WORKING) {
			Utils.Log(className, "Failed to initialize video playback. Attached camera is not working.");
			return;
		}

		redis = connection.GetDataAccessProvider ();
		if (Application.isPlaying) {
			subscriber = new Subscriber (redis);
			subscriber.Subscribe (ARCameraSetup.BaseKey + ARCameraSetup.DataKey, OnImageReceived);
		}

		int width = ARCameraSetup.IntrinsicsParameters.width;
		int height = ARCameraSetup.IntrinsicsParameters.height;
		videoTexture = new Texture2D (width, height, TextureFormat.RGB24, false);
		imageData = new byte[width * height * 3];

		Utils.Log (className, "Successfully initialized video playback.");
		State = ComponentState.WORKING;
	}

	void OnImageReceived (string channelName, byte[] message) {
		if (channelName != ARCameraSetup.BaseKey + ARCameraSetup.DataKey) {
			return;
		}
		imageData = message.ToArray ();
	}

	void OnChannelUnsubscribed (string channelName) {
		Debug.Log ("Nice !");
	}

	// Update is called once per frame
	void Update () {
		if (State != ComponentState.WORKING) {
			if (State != ComponentState.CONNECTED) {
				Utils.Log (className, "Retrying to connect to the redis server.");
				Connect ();
			} else {
				Utils.Log (className, "Retrying to initialize video playback.");
				Initialize ();
			}
			return;
		}

		if (!Application.isPlaying && redis != null) {
			int commandId = redis.SendCommand (RedisCommand.GET, ARCameraSetup.BaseKey + ARCameraSetup.DataKey);
			imageData = Utils.RedisTryReadData (redis, commandId);
		}

		if (imageData != null) {
			videoTexture.LoadRawTextureData (imageData);
			videoTexture.Apply ();
			cameraImagePlayback.texture = videoTexture;
		}
	}

	void OnApplicationQuit () {
		//subscriber.Unsubscribe(ARCameraSetup.DataKey, OnChannelUnsubscribed);
	}
}