using System.Collections;
using System.Collections.Generic;
using TeamDev.Redis;
using UnityEngine;

public class PoseExtractor : MonoBehaviour {

	public string PoseKey = "pose";

	public bool poseUpdated = true;
	public Matrix4x4 pose3D = new Matrix4x4 ();

	// Use this for initialization
	void Start () {
		RedisSubscriptionHandler.Instance.Sub (PoseKey);
		RedisConnectionHandler.Instance.redis.BinaryMessageReceived += new BinaryMessageReceivedHandler (OnPoseReceived);
	}

	void OnPoseReceived (string channelName, byte[] message) {
		if (channelName != PoseKey) {
			return;
		}
		poseUpdated = false;
		string data = Utils.ByteToString (message);
		pose3D = Utils.JSONToPose3D (data);
	}

	void Update () {
		if (!poseUpdated) {
			this.transform.position = Utils.ExtractTranslation ((Matrix4x4) pose3D);
			this.transform.rotation = Utils.ExtractRotation ((Matrix4x4) pose3D);
			poseUpdated = true;
		}
	}

	void OnApplicationQuit () {
		RedisSubscriptionHandler.Instance.Unsub (PoseKey);
	}
}