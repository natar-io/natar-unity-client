using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TeamDev.Redis;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// </summary>
public class RedisImageGetter : MonoBehaviour {

	public string ImageKey = "frame";
	public string MarkerKey = "markers";

	private RedisDataAccessProvider redis;
	private RawImage rawImage;
	private Texture2D videoTexture;
	private byte[] imageData;

	private bool isInitializated = false;

	public bool DebugMarkers = true;
	private string markersData;

	void Start () {
		rawImage = GetComponent<RawImage> ();

		TryInitializeTexture();
		RedisSubscriptionHandler.Instance.Sub (ImageKey);
		RedisConnectionHandler.Instance.redis.BinaryMessageReceived += new BinaryMessageReceivedHandler (OnImageReceived);

		if (DebugMarkers) {
			RedisSubscriptionHandler.Instance.Sub (MarkerKey);
			RedisConnectionHandler.Instance.redis.BinaryMessageReceived += new BinaryMessageReceivedHandler (OnMarkerReceived);
		}
	}

	void TryInitializeTexture() {
		Debug.Log("Trying to create a texture with camera parameters ...");
		if (!ApplicationParameters.RGBCameraAvailable) {
			isInitializated = false;
			return;
		}

		int width = ApplicationParameters.RGBCameraIntrinsics.width;
		int height = ApplicationParameters.RGBCameraIntrinsics.height;
		int channels = 3; // TODO: Get it from intrinsics parameters
		if (videoTexture == null || videoTexture.width != (int) width || videoTexture.height != (int) height) {
			videoTexture = new Texture2D ((int) width, (int) height, TextureFormat.RGB24, false);
			imageData = new byte[width * height * channels];
			isInitializated = true;
		}
	}

	void OnImageReceived (string channelName, byte[] message) {
		if (channelName != ImageKey) {
			return;
		}
		imageData = message.ToArray ();
	}

	void OnMarkerReceived(string channelName, byte[] message) {
		if (channelName != MarkerKey) {
			return;
		}
		markersData = Utils.ByteToString (message);
	}

	// Update is called once per frame
	void Update () {
		// If everything is not set up to start getting & display image try to set up.
		if (!isInitializated) {
			TryInitializeTexture();
			return;
		}

		// If we received null or corrupted data, do nothing (this does not update the texture and will cause small freezes if it happens).
		if (imageData == null) {
			return;
		}

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

		videoTexture.Apply ();
		rawImage.texture = videoTexture;
	}

	// OnApplicationQuit is called when the editor stops playing.
	void OnApplicationQuit () {
		RedisSubscriptionHandler.Instance.Unsub (ImageKey);
		RedisSubscriptionHandler.Instance.Unsub (MarkerKey);
	}
}