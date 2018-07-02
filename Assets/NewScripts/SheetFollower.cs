using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TeamDev.Redis;

[ExecuteInEditMode]
public class SheetFollower : MonoBehaviour {
	public ComponentState State = ComponentState.DISCONNECTED;
	public Camera ARCamera;
	public string Key = ":pose";

	private string className;
	private QuickCameraSetup ARCameraSetup;
	private RedisDataAccessProvider redis;
	private RedisConnection connection;
	private Subscriber subscriber;

	private Matrix4x4 pose;

	private bool isConnected = false;

	void Start () {
		className = transform.gameObject.name;
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
			Utils.Log(className, "Failed to initialize " + this.GetType().Name + ". Attached camera is not working.");
			return;
		}

		redis = connection.GetDataAccessProvider ();
		if (Application.isPlaying) {
			subscriber = new Subscriber (redis);
			subscriber.Subscribe (ARCameraSetup.BaseKey + Key, OnPoseReceived);
		}

		Utils.Log (className, "Successfully initialized "+ this.GetType().Name + ".");
		State = ComponentState.WORKING;
	}

	void OnPoseReceived(string channelName, byte[] message) {
		if (channelName != ARCameraSetup.BaseKey + Key) {
			return;
		}
		string data = Utils.ByteToString (message);
		pose = Utils.JSONToPose3D (data);
	}
	
	// Update is called once per frame
	void Update () {
		if (State != ComponentState.WORKING) {
			if (State != ComponentState.CONNECTED) {
				Utils.Log (className, "Retrying to connect to the redis server.");
				Connect ();
			} else {
				Utils.Log (className, "Retrying to initialize sheet following.");
				Initialize ();
			}
			return;
		}

		if (pose != null) {
			Utils.Log(className, "Pose updated.");
			this.transform.localPosition = Utils.ExtractTranslation ((Matrix4x4) pose);
			this.transform.localRotation = Utils.ExtractRotation ((Matrix4x4) pose);
		}
	}
}
