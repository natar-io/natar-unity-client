using System.Collections;
using System.Collections.Generic;
using TeamDev.Redis;
using UnityEngine;
using System.Net.Sockets;

public class RedisConnectionHandler : Singleton<RedisConnectionHandler> {

	protected RedisConnectionHandler () { } // To guarentee singleton

	public string IpAddress = "127.0.0.1";
	public int Port = 6379;
	public RedisDataAccessProvider redis;
	public bool isConnected = false;

	// Use this for initialization
	void Start () {
		redis = new RedisDataAccessProvider ();
		redis.Configuration.Host = IpAddress;
		redis.Configuration.Port = Port;
		if (!isConnected) {
			TryConnection ();
		}
	}

	public void TryConnection () {

		try {
			redis.Connect ();
		} catch (SocketException e) {
			Debug.Log ("Error");
			isConnected = false;
			return;
		}
		isConnected = true;
	}

}