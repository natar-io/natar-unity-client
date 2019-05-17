using System;
using System.Collections;
using System.Net.Sockets;

using TeamDev.Redis;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class RedisHandler : Singleton<RedisHandler> {
	
	public String RedisServerHost = "localhost";
	private String redisServerHost = "localhost";
	private Boolean hostChanged = false;

	public int RedisServerPort = 6379;
	private int redisServerPort = 6379;
	private Boolean portChanged = false;

	public Boolean NoDelaySocket = true;

	private RedisDataAccessProvider parent;
	private Boolean redisConnected = false;

	private IEnumerator pingRoutine;
	public float PingLatency = 2000.0f; //ms

	[SerializeField]
	private Color Status = Color.red;

	/* Event used to notify every subscribed services of a status change in the redis handler */
	public delegate void ConnectionStatusChangedHandler(bool connected);
	public ConnectionStatusChangedHandler ConnectionStatusChanged;

	/* Event used to notify new service when they first hook up to RedisHandler */
	public delegate void ConnectionStatusNotificationHandler (bool connected);
	public ConnectionStatusNotificationHandler ConnectionStatusNotification;

	/* Event triggered by new services to tell the RedisHandler to send them its state */
	public delegate void NewServiceHandler(string name);
	public NewServiceHandler NewService;

	protected RedisHandler() { }

	public void Start() {
		parent = new RedisDataAccessProvider();
		parent.Configuration.Host = RedisServerHost;
		parent.Configuration.Port = RedisServerPort;
		parent.Configuration.NoDelaySocket = NoDelaySocket;

		ConnectionStatusChanged += OnConnectionStatusChanged;
		NewService += OnNewServiceArrived;
		
		Connect();

		pingRoutine = PingRedis();
		StartCoroutine(pingRoutine);
	}

	public void OnNewServiceArrived(string service) {
		ConnectionStatusNotification(this.redisConnected);
	}

	public void OnConnectionStatusChanged(bool connected) {
		Status = connected ? Color.green : Color.red;
	}

    private void Connect () {
#if UNITY_EDITOR
		if (!Application.isPlaying) updateParentConnection();
#endif

		if (parent == null) {
			// This can not happen unless in editor mod when start is not systematically called
			updateParentConnection();
		}

		try {
			parent.Connect();
		} catch (SocketException) {
			if (redisConnected == false) {
				return;
			}
			redisConnected = false;
			ConnectionStatusChanged(redisConnected);
			return;
		}
		// parent.SendCommand(RedisCommand.CLIENT_SETNAME, "unity:redis-handler");
		redisConnected = true;
		ConnectionStatusChanged(redisConnected);
	}

	private void Update() {
		if (parent == null || !redisConnected || portChanged || hostChanged) {
			Connect();
			hostChanged = false; portChanged = false;
		}
#if UNITY_EDITOR
		if (!Application.isPlaying) {
			// Simulates coroutine call : In editor coroutines are not called to prevent heavy job running on the main thread
			if (pingRoutine != null && redisConnected) { pingRoutine.MoveNext(); }
		}
#endif
	}

	private IEnumerator PingRedis() {
		while (true) {
			yield return new WaitForSeconds(PingLatency / 1000.0f);
			Ping();
		}
	}

	public bool IsConnected() {
		return redisConnected;
	}
	
	public RedisDataAccessProvider CreateConnection() {
		if (parent == null || !redisConnected) {
			Debug.LogError("No reference connection found or Redis server is unreachable: " + redisConnected);
			return null;
		}

		RedisDataAccessProvider redis = new RedisDataAccessProvider();
		redis.Configuration.Host = parent.Configuration.Host;
		redis.Configuration.Port = parent.Configuration.Port;
		redis.Configuration.NoDelaySocket = parent.Configuration.NoDelaySocket;

		return redis;
	}

	public void Ping() {
		// If we believe that Redis is connected, ping it
		if (redisConnected) {
			bool alive = SocketConnected(parent.GetSocket());
			if (!alive) {
				redisConnected = false;
				ConnectionStatusChanged(redisConnected);
			}
		}
		/*
		if (redisConnected) {
				bool redisAlive = pingServer();
				if (!redisAlive) {
					redisConnected = false;
					ConnectionStatusChanged(redisConnected);
				}
			}
		if (!redisConnected) {
			return false;
		}
		return SocketConnected(parent.GetSocket());
		*/
	}


	// Check wheter or not if a Socket is connected
	public static bool SocketConnected(Socket socket) {
		try {
			return !(socket.Available == 0 && socket.Poll(1, SelectMode.SelectRead));
		}
		catch (Exception) { 
			return false;
		}
	}

	/// <summary>
	/// OnValide is called when an Editor variable field is changed
	/// Allows to check if any field has been modified since previous calls.
	/// Used to update the connection when a new host or port is 
	/// </summary>
	new private void OnValidate() {
		hostChanged = redisServerHost != RedisServerHost;
		portChanged = redisServerPort != RedisServerPort;

		if (hostChanged) {
			redisServerHost = RedisServerHost;
		}

		if (portChanged) {
			redisServerPort = RedisServerPort;
		}

		if (hostChanged || portChanged) {
			updateParentConnection();
		}
	}

	private void updateParentConnection() {
		parent = new RedisDataAccessProvider();
		parent.Configuration.Host = RedisServerHost;
		parent.Configuration.Port = RedisServerPort;
		parent.Configuration.NoDelaySocket = NoDelaySocket;

		ConnectionStatusChanged += OnConnectionStatusChanged;
		NewService += OnNewServiceArrived;
	}
}