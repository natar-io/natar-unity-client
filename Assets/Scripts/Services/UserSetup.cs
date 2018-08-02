using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TeamDev.Redis;

[ExecuteInEditMode]
public class UserSetup : MonoBehaviour {

	public ComponentState State = ComponentState.DISCONNECTED;
	public string Key = "user:position";

	private string className;
	private RedisDataAccessProvider redis;
	private RedisConnection connection;
	private bool isConnected = false;

	public GameObject goTable;

	void Start () {
		className = transform.gameObject.name;
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
		redis = connection.GetDataAccessProvider ();
		if (SetupPosition()) {
			Utils.Log (className, "Successfully initialized " + this.GetType ().Name + ".");
			State = ComponentState.WORKING;
		}
	}

	bool SetupPosition () {
		Position pos = Utils.RedisTryGetPosition(connection.GetDataAccessProvider(), Key);
		if (pos == null) {
			Utils.Log(className, "Failed to load user position.");
			State = ComponentState.CONNECTED;
			return false;
		}

		TableSetup table = goTable.GetComponent<TableSetup>();
		Vector3 position = new Vector3(pos.x, pos.y, pos.z);
		this.transform.position = table.GetPosition() - position;
		this.transform.LookAt(table.GetPosition(), new Vector3(0, -1, 0));
		Utils.Log (className, "Successfully loaded and setup user position.");
		State = ComponentState.WORKING;
		return true;
	}

	// Update is called once per frame
	void Update () {
		if (State != ComponentState.WORKING) {
			if (State != ComponentState.CONNECTED) {
				Utils.Log (className, "Retrying to connect to the redis server.");
				Connect ();
			} else {
				Utils.Log (className, "Retrying to initialize user position.");
				Initialize ();
			}
			return;
		}
	}
}