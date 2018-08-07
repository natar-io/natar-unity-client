using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using TeamDev.Redis;
using UnityEngine;

public class RedisConnection {

	public string IpAdress = "127.0.0.1";
	public int Port = 6379;

	private RedisDataAccessProvider redis;

	public RedisConnection(string ip, int port) {
		redis = new RedisDataAccessProvider();
		redis.Configuration.Host = IpAdress;
		redis.Configuration.Port = port;
	}
	public RedisConnection () {
		redis = new RedisDataAccessProvider();
		redis.Configuration.Host = IpAdress;
		redis.Configuration.Port = Port;
	}

	public bool TryConnection() {
		try {
			redis.Connect();
		}
		catch (SocketException e) {
			Debug.LogError(e.Message + ": Could not connect to redis server (" + IpAdress  + ":" + Port + ").");
			return false;
		}
		return true;
	}

	public RedisDataAccessProvider GetDataAccessProvider() {
		return redis;
	}
}