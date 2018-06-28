using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TeamDev.Redis;

[ExecuteInEditMode]
public class CameraVideoPlayer : MonoBehaviour {

	public Camera ARCamera;
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
		cameraImagePlayback = this.GetComponent<RawImage>();
		ARCameraSetup = ARCamera.GetComponent<QuickCameraSetup>();

		connection = new RedisConnection();
		isConnected = connection.TryConnection(); 
		redis = connection.GetDataAccessProvider();
		if (Application.isPlaying) {
			subscriber = new Subscriber(redis);
			subscriber.Subscribe(ARCameraSetup.DataKey, OnImageReceived);
		}

		int width = ARCameraSetup.IntrinsicsParameters.width;
		int height = ARCameraSetup.IntrinsicsParameters.height;
		videoTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
		imageData = new byte[width * height * 3];
	}

	void OnImageReceived(string channelName, byte[] message) {
		if (channelName != ARCameraSetup.DataKey) {
			return;
		}

		imageData = message.ToArray();
	}

	void OnChannelUnsubscribed(string channelName) {
		Debug.Log("Nice !");
	}
	
	// Update is called once per frame
	void Update () {
		if (!Application.isPlaying && redis != null && isConnected) {
			int commandId = redis.SendCommand(RedisCommand.GET, ARCameraSetup.DataKey);
			imageData = Utils.RedisTryReadData(redis, commandId);
		}

		if (imageData != null && isConnected) {
			videoTexture.LoadRawTextureData(imageData);
			videoTexture.Apply();
			cameraImagePlayback.texture = videoTexture;
		}
	}

	void OnApplicationQuit () {
		//subscriber.Unsubscribe(ARCameraSetup.DataKey, OnChannelUnsubscribed);
	}
}
