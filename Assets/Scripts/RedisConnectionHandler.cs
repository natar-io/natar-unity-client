using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using TeamDev.Redis;
using UnityEngine;

[ExecuteInEditMode]
public class RedisConnectionHandler : MonoBehaviour {

	public string IpAddress = "127.0.0.1";
	public int Port = 6379;
	
	[HideInInspector]
	public bool IsConnected = false;
	[HideInInspector]
	public RedisDataAccessProvider redis;

	// Use this for initialization
	void Start () {
		Debug.Log ("Starting ...");
		if (!IsConnected || ApplicationParameters.RedisConnection == null) {
			SetupRedis ();
			TryConnection ();
			ApplicationParameters.RedisConnection = this;
		}
	}

	void SetupRedis () {
		redis = new RedisDataAccessProvider ();
		redis.Configuration.Host = IpAddress;
		redis.Configuration.Port = Port;
	}

	public void TryConnection () {
		Debug.Log ("Trying to connect to redis server: " + IpAddress + ":" + Port + " ...");
		try {
			redis.Connect ();
		} catch (SocketException e) {
			Debug.Log ("Error: " + e.ToString ());
			IsConnected = false;
			return;
		}
		Debug.Log ("Connected to: " + IpAddress + ":" + Port);
		IsConnected = true;
	}

	void Update () { }
}