using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using TeamDev.Redis;
using UnityEngine;
public class RedisConnectionHandler : Singleton<RedisConnectionHandler> {

	protected RedisConnectionHandler () { } // To guarentee singleton

	public string IpAddress = "127.0.0.1";
	public int Port = 6379;
	public RedisDataAccessProvider redis;

	[HideInInspector]
	public bool IsConnected = false;

	// Use this for initialization
	void Awake () {
		redis = new RedisDataAccessProvider ();
		redis.Configuration.Host = IpAddress;
		redis.Configuration.Port = Port;
		if (!IsConnected) {
			TryConnection ();
		}
	}

	public void TryConnection () {

		try {
			redis.Connect ();
		} catch (SocketException e) {
			Debug.Log ("Error: " + e.ToString ());
			IsConnected = false;
			return;
		}
		IsConnected = true;
	}

}