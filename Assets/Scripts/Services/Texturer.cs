using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TeamDev.Redis;

[ExecuteInEditMode]
public class Texturer : MonoBehaviour, INectarService {

	public string Key = "camera0:view1";

	public string objectName;
	public ComponentState state;

	private RedisConnection connection;
	private RedisDataAccessProvider redis;
	
	private Texture2D texture;

	void Start() {
		Connect();
	}

	// Use this for initialization
	/* Connect
	*  This function creates a new connection to Redis and tries to join it.
	*  If succeed, this function will call the initialization fuction. 
	*/
	public void Connect() {
		// Since this has to work in editor, we are getting component informations each time we try to connect/init in case they changed
		objectName = transform.gameObject.name;

		if (connection == null) {
			connection = new RedisConnection();
		}
		bool redisConnected = connection.TryConnection();
		state = redisConnected ? ComponentState.CONNECTED : ComponentState.DISCONNECTED;
		Utils.Log(objectName, "Redis connection: " + (redisConnected ? "succeed." : "failed."), (redisConnected ? 0 : 1));
		if (redisConnected) {
			redis = connection.GetDataAccessProvider();
			Initialize();
		}
	}

	/* Initiliaze
	 * This function initialize everything the script/component needs to work.
	 * If succeed, the component can be used
	 */
	public void Initialize() {
		// Since this has to work in editor, we are getting component informations each time we try to connect/init in case they changed
		objectName = transform.gameObject.name;
		bool isLoaded = Load();
		Utils.Log(objectName, this.GetType() + ": " + (isLoaded ? "succeed" : "failed") + ".", (isLoaded ? 0 : 1));
		state = isLoaded ? ComponentState.WORKING : ComponentState.CONNECTED;
	}

	public bool Load() {
		GameObject goToTexture = null;
		foreach (Transform child in transform) {
			if (child.gameObject.name == "Model") {
					goToTexture = child.gameObject;
					break;
			}
		}

		if (goToTexture == null) {
			Utils.Log(objectName, "No model found to apply texture to.", 1);
		}

		Texture2D texture = Utils.GetImageAsTexture(redis, Key);
		// Breaking all material instance
		goToTexture.GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
		return true;
	}
	
	// Update is called once per frame
	void Update () {
		switch (state) {
			case ComponentState.DISCONNECTED:
				Connect();
				break;
			case ComponentState.CONNECTED:
				Initialize();
				break;
			default:
				break;
		}
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(Texturer))]
public class TexturerEditor : Editor 
{
	//Creating serialized properties so we can retrieve variable attributes without having to recreate them in the custom editor
	SerializedProperty mscript = null;
	SerializedProperty key = null;

	private void OnEnable()
	{
		mscript = serializedObject.FindProperty("m_Script");
		key = serializedObject.FindProperty("Key");
	}

    public override void OnInspectorGUI()
    {
        Texturer script = (Texturer)target;

		// This will show the current used script and make it clickable. When clicked, the script's code is open into the default editor.
		GUI.enabled = false;
     	EditorGUILayout.PropertyField(mscript, true, new GUILayoutOption[0]);
		GUI.enabled = true;

		EditorGUILayout.PropertyField(key);

		// Control layout : [current state] [restart button]
		GUILayout.BeginHorizontal();
		GUI.enabled = false;
		script.state = (ComponentState)EditorGUILayout.EnumPopup("Internal state", script.state);
		GUI.enabled = true;
		if (GUILayout.Button("Reinitialize")) {
			script.Connect();
		}
		GUILayout.EndHorizontal();

		// Find all the PropertyField and apply layout and style to them so they can be displayed
		serializedObject.ApplyModifiedProperties();
    }
}
#endif
