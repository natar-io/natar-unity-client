using System;
using System.Collections;
using UnityEngine;

using TeamDev.Redis;

namespace Natar
{
	[ExecuteInEditMode]
	public class Texturer : MonoBehaviour {
		private RedisHandler rHandler;
		private RedisDataAccessProvider redis;

		public string Key = "camera0:view1";

		public ServiceStatus state = ServiceStatus.DISCONNECTED;

		public GameObject targetModel;
		private Texture2D currentTexture = null;

		private delegate void OnServiceConnectionStateChangedHandler(bool connected);
		private event OnServiceConnectionStateChangedHandler ServiceConnectionStateChanged;


		public void Start() {
			state = ServiceStatus.DISCONNECTED;
			
			rHandler = RedisHandler.Instance;
			rHandler.ConnectionStatusChanged += OnRedisHandlerConnectionStateChanged;
			rHandler.ConnectionStatusNotification += OnRedisHandlerConnectionStatusNotification;

			rHandler.NewService("texturer");
			
			ServiceConnectionStateChanged += OnServiceConnectionStateChanged;
		}


	#region event

		public void OnRedisHandlerConnectionStatusNotification(bool handlerConnected) {
			if (this.state == ServiceStatus.DISCONNECTED && handlerConnected) {
				this.connect();
			}
		}
		
		public void OnRedisHandlerConnectionStateChanged(bool handlerConnected) {
			if (handlerConnected) { this.connect(); }
			else { this.disconnect(); }
		}

		private void OnServiceConnectionStateChanged(bool connected) {
			Debug.Log("[" + transform.gameObject.name + "] Service " + (connected ? "connected" : "disconnected"));
			this.state = connected ? ServiceStatus.CONNECTED : ServiceStatus.DISCONNECTED;

			if (connected) {
				init();
			}
			else {
				kill();
			}
		}

	#endregion

	#region core
	
		private void connect() {
			redis = rHandler.CreateConnection();
			try {
				redis.Connect();
			} catch (Exception) {
				OnServiceConnectionStateChanged(false);
				return;
			}
			OnServiceConnectionStateChanged(true);
		}

		private void disconnect() {
			OnServiceConnectionStateChanged(false);
		}

		private Texture2D load() {
			if (redis == null) { return null; }
			return Utils.GetImageAsTexture(redis, Key);	
		}

		public void init() {
			Texture2D texture = load();
			if (applyTexture(texture, targetModel)) {
				this.state = ServiceStatus.WORKING;
			}
			else {
				this.state = ServiceStatus.CONNECTED;
			}
		}

		private void kill() {}
	
	#endregion

		private bool applyTexture(Texture2D texture, GameObject target) {
			if (texture == null) { return false; }
			Renderer targetRenderer = target.GetComponent<Renderer>();
			if (targetRenderer == null) { return false; }
			targetRenderer.sharedMaterial.mainTexture = texture;
			currentTexture = texture;
			return true;
		}

		public Texture2D GetCurrentTexture() {
			return this.currentTexture;
		}
	}
}

// [ExecuteInEditMode]
// public class Texturer : MonoBehaviour, INectarService {

// 	public string Key = "camera0:view1";

// 	public string objectName;
// 	public ServiceStatus state;

// 	private RedisConnection connection;
// 	private RedisDataAccessProvider redis;
	
// 	private Texture2D texture;

// 	void Start() {
// 		Connect();
// 	}

// 	// Use this for initialization
// 	/* Connect
// 	*  This function creates a new connection to Redis and tries to join it.
// 	*  If succeed, this function will call the initialization fuction. 
// 	*/
// 	public void Connect() {
// 		// Since this has to work in editor, we are getting component informations each time we try to connect/init in case they changed
// 		objectName = transform.gameObject.name;

// 		if (connection == null) {
// 			connection = new RedisConnection();
// 		}
// 		bool redisConnected = connection.TryConnection();
// 		state = redisConnected ? ServiceStatus.CONNECTED : ServiceStatus.DISCONNECTED;
// 		Utils.Log(objectName, "Redis connection: " + (redisConnected ? "succeed." : "failed."), (redisConnected ? 0 : 1));
// 		if (redisConnected) {
// 			redis = connection.GetDataAccessProvider();
// 			Initialize();
// 		}
// 	}

// 	/* Initiliaze
// 	 * This function initialize everything the script/component needs to work.
// 	 * If succeed, the component can be used
// 	 */
// 	public void Initialize() {
// 		// Since this has to work in editor, we are getting component informations each time we try to connect/init in case they changed
// 		objectName = transform.gameObject.name;
// 		bool isLoaded = Load();
// 		Utils.Log(objectName, this.GetType() + ": " + (isLoaded ? "succeed" : "failed") + ".", (isLoaded ? 0 : 1));
// 		state = isLoaded ? ServiceStatus.WORKING : ServiceStatus.CONNECTED;
// 	}

// 	public bool Load() {
// 		GameObject goToTexture = null;
// 		foreach (Transform child in transform) {
// 			if (child.gameObject.name == "Model") {
// 					goToTexture = child.gameObject;
// 					break;
// 			}
// 		}

// 		if (goToTexture == null) {
// 			Utils.Log(objectName, "No model found to apply texture to.", 1);
// 			return false;
// 		}

// 		Texture2D texture = Utils.GetImageAsTexture(redis, Key);
// 		if (texture == null) {
// 			Utils.Log(objectName, "Texture could not be loaded from redis at specific key.");
// 			return false;
// 		}
// 		// Breaking all material instance
// 		goToTexture.GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
// 		return true;
// 	}
	
// 	// Update is called once per frame
// 	void Update () {
// 		/*
// 		switch (state) {
// 			case ServiceStatus.DISCONNECTED:
// 				Connect();
// 				break;
// 			case ServiceStatus.CONNECTED:
// 				Initialize();
// 				break;
// 			default:
// 				break;
// 		}
// 		*/
// 	}
// }

// #if UNITY_EDITOR
// [CustomEditor(typeof(Texturer))]
// public class TexturerEditor : Editor 
// {
// 	//Creating serialized properties so we can retrieve variable attributes without having to recreate them in the custom editor
// 	SerializedProperty mscript = null;
// 	SerializedProperty key = null;

// 	private void OnEnable()
// 	{
// 		mscript = serializedObject.FindProperty("m_Script");
// 		key = serializedObject.FindProperty("Key");
// 	}

//     public override void OnInspectorGUI()
//     {
//         Texturer script = (Texturer)target;

// 		// This will show the current used script and make it clickable. When clicked, the script's code is open into the default editor.
// 		GUI.enabled = false;
//      	EditorGUILayout.PropertyField(mscript, true, new GUILayoutOption[0]);
// 		GUI.enabled = true;

// 		EditorGUILayout.PropertyField(key);

// 		// Control layout : [current state] [restart button]
// 		GUILayout.BeginHorizontal();
// 		GUI.enabled = false;
// 		script.state = (ServiceStatus)EditorGUILayout.EnumPopup("Internal state", script.state);
// 		GUI.enabled = true;
// 		if (GUILayout.Button("Reinitialize")) {
// 			script.Connect();
// 		}
// 		GUILayout.EndHorizontal();

// 		// Find all the PropertyField and apply layout and style to them so they can be displayed
// 		serializedObject.ApplyModifiedProperties();
//     }
// }
// #endif