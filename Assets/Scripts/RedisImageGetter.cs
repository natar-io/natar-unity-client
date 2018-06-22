using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TeamDev.Redis;
using UnityEngine;
using UnityEngine.UI;

using Rch = RedisConnectionHandler;

/// <summary>
/// Gets an image from redis a display it as a texture2D on the parent game object
/// </summary>
public class RedisImageGetter : MonoBehaviour {

	public string ImageKey = "frame";
	public bool DebugPublish = false;

	private RedisDataAccessProvider redis;
	private RawImage rawImage;
	private Texture2D videoTexture;
	private byte[] imageData;

	void Start () {
		rawImage = GetComponent<RawImage> ();
		RedisSubscriptionHandler.Instance.Sub (ImageKey);
		RedisConnectionHandler.Instance.redis.BinaryMessageReceived += new BinaryMessageReceivedHandler (OnImageReceived);

		int width = ApplicationParameters.camParams.width;
		int height = ApplicationParameters.camParams.height;
		int channels = 3;
		if (videoTexture == null || videoTexture.width != (int) width || videoTexture.height != (int) height) {
			videoTexture = new Texture2D ((int) width, (int) height, TextureFormat.RGB24, false);
			imageData = new byte[width * height * channels];
		}
	}

	void OnImageReceived (string channelName, byte[] message) {
		if (channelName != ImageKey) {
			return;
		}
		imageData = message.ToArray ();
	}

	// Update is called once per frame
	void Update () {
		if (DebugPublish) {
			Debug.Log ("Publishing ...");
			RedisConnectionHandler.Instance.redis.Messaging.Publish (ImageKey, "Hello");
		}

		// Image width / height is equal to camera width / height
		if (imageData == null) {
			return;
		}

		videoTexture.LoadRawTextureData (imageData);
		videoTexture.Apply ();
		rawImage.texture = videoTexture;
	}

	// OnApplicationQuit is called when the editor stops playing.
	void OnApplicationQuit () {
		RedisSubscriptionHandler.Instance.Unsub (ImageKey);
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