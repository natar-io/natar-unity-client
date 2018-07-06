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
	private byte[] imageDataRGB;
	
	private int width;
	private int height;

	private bool isConnected = false;

	public bool DebugMarkers = true;
	private string markersData;

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
			if (ARCameraSetup.Type == CameraType.RGB) {
				subscriber.Subscribe (ARCameraSetup.BaseKey, OnImageReceived);
				//subscriber.Subscribe (ARCameraSetup.BaseKey + ":" + "markers", OnMarkersReceived);
			} else {
				subscriber.Subscribe (ARCameraSetup.BaseKey + ":" + ARCameraSetup.DataKey, OnImageReceived);
			}
		}

		width = ARCameraSetup.IntrinsicsParameters.width;
		height = ARCameraSetup.IntrinsicsParameters.height;
		videoTexture = new Texture2D (width, height, TextureFormat.RGB24, false);
		if (ARCameraSetup.Type == CameraType.RGB) {
			imageData = new byte[width * height * 3];
		}
		if (ARCameraSetup.Type == CameraType.DEPTH) {
			imageData = new byte[width * height * 2];
			imageDataRGB = new byte[width * height * 3];
		}

		Utils.Log (className, "Successfully initialized video playback.");
		State = ComponentState.WORKING;
	}

	void OnImageReceived (string channelName, byte[] message) {
		if (channelName != ARCameraSetup.BaseKey) {
			return;
		}
		imageData = message.ToArray ();
	}

	void OnMarkersReceived (string channelName, byte[] message) {
		if (channelName != ARCameraSetup.BaseKey + ":" + "markers") {
			return;
		}
		markersData = Utils.ByteToString(message);
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
			int commandId;
			if (ARCameraSetup.Type == CameraType.DEPTH) {
				commandId = redis.SendCommand (RedisCommand.GET, ARCameraSetup.BaseKey + ":" + ARCameraSetup.DataKey);
				imageData = Utils.RedisTryReadData (redis, commandId);
				imageDataRGB = Utils.GRAY16ToRGB24(width, height, imageData);
			} else {
				commandId = redis.SendCommand (RedisCommand.GET, ARCameraSetup.BaseKey);
				imageData = Utils.RedisTryReadData (redis, commandId);
			}

			if (DebugMarkers && ARCameraSetup.Type == CameraType.RGB) {
				commandId = redis.SendCommand (RedisCommand.GET, ARCameraSetup.BaseKey + ":" + "markers");
				byte[] data = Utils.RedisTryReadData (redis, commandId);
				markersData = Utils.ByteToString (data);
			}
		}

		if (imageData != null) {
			if (ARCameraSetup.Type == CameraType.DEPTH) {
				videoTexture.LoadRawTextureData (imageDataRGB);
			} else {
				videoTexture.LoadRawTextureData (imageData);
			
				// If we received markers information and that we want to debug markers position
				if (markersData != null && DebugMarkers) {
					Markers markers = JsonUtility.FromJson<Markers> (markersData);
					for (int i = 0; i < markers.markers.Length; ++i) {
						Marker m = markers.markers[i];
						for (int j = 0; j < m.corners.Length; j += 2) {
							Utils.Circle (this.videoTexture, (int) m.corners[j], (int) m.corners[j + 1], 2, Color.green);
						}
					}
				}
			}

			videoTexture.Apply ();
			cameraImagePlayback.texture = videoTexture;
		} else {
			/* Do something to handle null image */
			// Count null images and if too much -> DISCONNECTED ?
		}
	}

	void OnApplicationQuit () {
		subscriber.Unsubscribe(ARCameraSetup.BaseKey + ":" + ARCameraSetup.DataKey, OnChannelUnsubscribed);
	}
}