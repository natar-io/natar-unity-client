using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TeamDev.Redis;

[ExecuteInEditMode]
public class UserSetup : MonoBehaviour {

	public ComponentState State = ComponentState.DISCONNECTED;
	public string Key = "user:position";

	private string objectName;
	private RedisDataAccessProvider redis;
	private RedisConnection connection;
	private bool isConnected = false;

	public GameObject tableObject;

	void Start () {
		objectName = transform.gameObject.name;
		Connect ();
	}

	void Connect () {
		if (connection == null) {
			connection = new RedisConnection ();
		}
		isConnected = connection.TryConnection ();
		Utils.Log (objectName, (isConnected ? "Connection succeed." : "Connection failed."));
		if (!isConnected) {
			State = ComponentState.DISCONNECTED;
			return;
		}
		State = ComponentState.CONNECTED;
		Initialize ();
	}

	void Initialize () {
		redis = connection.GetDataAccessProvider ();
		if (SetupPosition()) {
			Utils.Log (objectName, "Successfully initialized " + this.GetType().Name + ".");
			State = ComponentState.WORKING;
		}
	}

	bool SetupPosition () {
		if (tableObject == null) {
			Utils.Log(objectName, "Table gameobject needs to be attached to simulate user point of view because it depends on it.");
			return false;
		}

		Position userPosition = Utils.RedisTryGetPosition(connection.GetDataAccessProvider(), Key);
		if (userPosition == null) {
			Utils.Log(objectName, "Failed to load user position.");
			State = ComponentState.CONNECTED;
			return false;
		}

		Vector3 position = new Vector3(userPosition.x, userPosition.y, userPosition.z);
		this.transform.position = tableObject.transform.position - position;
		this.transform.LookAt(tableObject.transform.position);
		Utils.Log (objectName, "Successfully loaded and setup user position.");
		State = ComponentState.WORKING;
		return true;
	}

	// Update is called once per frame
	void Update () {
		if (State != ComponentState.WORKING) {
			if (State != ComponentState.CONNECTED) {
				Utils.Log (objectName, "Retrying to connect to the redis server.");
				Connect ();
			} else {
				Utils.Log (objectName, "Retrying to initialize user position.");
				Initialize ();
			}
			return;
		}
	}
}