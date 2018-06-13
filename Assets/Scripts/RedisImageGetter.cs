using System.Collections;
using System.Collections.Generic;
using System;

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

	void Start () {
		rawImage = GetComponent<RawImage>();
	}

	// Update is called once per frame
	void Update () {
		RedisImageToTexture ();
	}

	void RedisImageToTexture () {
		int commandId;
		commandId = Rch.Instance.redis.SendCommand (RedisCommand.GET, ImageKey + ":width");
		int? width = Utils.RedisTryReadInt (Rch.Instance.redis, commandId);

		commandId = Rch.Instance.redis.SendCommand (RedisCommand.GET, ImageKey + ":height");
		int? height = Utils.RedisTryReadInt (Rch.Instance.redis, commandId);

		if (!width.HasValue || !height.HasValue) {
			throw new ArgumentException ("Could not fetch image width or height from redis server. Please check connection settings.");
		}

		// Get this particular commandId
		commandId = Rch.Instance.redis.SendCommand (RedisCommand.GET, ImageKey);
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