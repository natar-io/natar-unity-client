using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

using TeamDev.Redis;

using Rch = RedisConnectionHandler;

/// <summary>
/// Gets an image from redis a display it as a texture2D on the parent game object
/// </summary>
public class RedisImageGetter : MonoBehaviour {

	public string ImageKey = "custom:image";

	private RedisDataAccessProvider redis;
	private RawImage rawImage;
	private Texture2D videoTexture;

	byte[] imageData;

	void Start () {
		rawImage = GetComponent<RawImage>();
		RedisConnectionHandler.Instance.redis.ChannelSubscribed += new ChannelSubscribedHandler (OnChannelSubscribed);
		RedisConnectionHandler.Instance.redis.MessageReceived += new MessageReceivedHandler (OnMessageRecieved);
		RedisConnectionHandler.Instance.redis.Messaging.Subscribe (ImageKey);
	}

	void OnChannelSubscribed (string channelName) {
		Debug.Log ("[IMAGE] Successfully subscribe to: " + channelName);
	}

	void OnMessageRecieved (string channelName, string message) {
		if (channelName != ImageKey) {
			return;
		}
		Debug.Log(message.Length);
		//imageData = Encoding.UTF8.GetBytes(message);
		//Debug.Log(imageData.Length);
		Debug.Log("[IMAGE] Image received.");
	}

	// Update is called once per frame
	void Update () {
		// Image width / height is equal to camera width / height
		/*
		int width = ApplicationParameters.camParams.width;
		int height = ApplicationParameters.camParams.height;		
		if (videoTexture == null || videoTexture.width != (int) width || videoTexture.height != (int) height) {
			videoTexture = new Texture2D ((int) width, (int) height, TextureFormat.RGB24, false);
		}
		videoTexture.LoadRawTextureData (imageData);
		//RedisImageToTexture ();
		*/
	}

	void RedisImageToTexture () {
		// Image width / height is equal to camera width / height
		int width = ApplicationParameters.camParams.width;
		int height = ApplicationParameters.camParams.height;

		// Get this particular commandId
		int commandId = Rch.Instance.redis.SendCommand (RedisCommand.GET, ImageKey);
		// Get image data from this particular command to avoid unexpected results
		byte[] imageData = Utils.RedisTryReadData (Rch.Instance.redis, commandId);

		if (videoTexture == null || videoTexture.width != (int) width || videoTexture.height != (int) height) {
			videoTexture = new Texture2D ((int) width, (int) height, TextureFormat.RGB24, false);
		}
		videoTexture.LoadRawTextureData (imageData);

		// Get markers informations
		commandId = Rch.Instance.redis.SendCommand (RedisCommand.GET, ImageKey + ":detected-markers");
		string markersData = Utils.RedisTryReadString (Rch.Instance.redis, commandId);
		if (markersData != null) {

			Markers markers = JsonUtility.FromJson<Markers> (markersData);
			// Debug code (print red circles on markers corners position)
			for (int i = 0; i < markers.markers.Length; ++i) {
				Marker m = markers.markers[i];
				for (int j = 0; j < m.corners.Length; j += 2) {
					Utils.Circle (this.videoTexture, (int) m.corners[j], (int) m.corners[j + 1], 5, Color.red);
				}
			}
		}

		// Render the image on the texture
		videoTexture.Apply ();
		rawImage.texture = videoTexture;
	}
}