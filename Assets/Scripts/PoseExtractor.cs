using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoseExtractor : MonoBehaviour {

	public string PoseKey = "custom:image:detected-pose";
	// Use this for initialization
	void Start () {
		Matrix4x4? pose3D = Utils.RedisTryGetPose3D (RedisConnectionHandler.Instance.redis, PoseKey);
		if (pose3D.HasValue) {
			ApplicationParameters.pose3D = (Matrix4x4) pose3D;
			Debug.Log ((Matrix4x4) pose3D);
			Debug.Log (Utils.ExtractTranslation ((Matrix4x4) pose3D));
			this.transform.position = Utils.ExtractTranslation ((Matrix4x4) pose3D);
			this.transform.rotation = Utils.ExtractRotation ((Matrix4x4) pose3D);
		}
	}

	void Update () {
		Matrix4x4? pose3D = Utils.RedisTryGetPose3D (RedisConnectionHandler.Instance.redis, PoseKey);
		if (pose3D.HasValue) {
			ApplicationParameters.pose3D = (Matrix4x4) pose3D;
			Debug.Log ((Matrix4x4) pose3D);
			Debug.Log (Utils.ExtractTranslation ((Matrix4x4) pose3D));
			this.transform.position = Utils.ExtractTranslation ((Matrix4x4) pose3D);
			/*
			Vector3 position = this.transform.position;
			position.x = -position.x;
			this.transform.position = position;
			*/
			this.transform.rotation = Utils.ExtractRotation ((Matrix4x4) pose3D);
		}
	}
}