using System.Collections;
using System.Collections.Generic;

using TeamDev.Redis;
using UnityEngine;

public class PoseExtractor : MonoBehaviour {

	public string PoseKey = "custom:image:detected-pose";

	public bool poseUpdated = true;
	public Matrix4x4 pose3D = new Matrix4x4();
	// Use this for initialization
	void Start () {
		RedisConnectionHandler.Instance.redis.ChannelSubscribed += new ChannelSubscribedHandler (OnChannelSubscribed);
		RedisConnectionHandler.Instance.redis.MessageReceived += new MessageReceivedHandler (OnMessageRecieved);
		RedisConnectionHandler.Instance.redis.Messaging.Subscribe (PoseKey);
	}

	void OnChannelSubscribed (string channelName) {
		Debug.Log ("[POSE] Successfully subscribe to: " + channelName);
	}

	void OnMessageRecieved (string channelName, string message) {
		if (channelName != PoseKey) {
			return;
		}
		poseUpdated = false;
		pose3D = Utils.JSONToPose3D (message);
		Debug.Log(pose3D);
	}

	void Update()
	{
		if (!poseUpdated)
		{
			this.transform.position = Utils.ExtractTranslation ((Matrix4x4) pose3D);
			this.transform.rotation = Utils.ExtractRotation ((Matrix4x4) pose3D);
			poseUpdated = true;
		}
	}
}