using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TeamDev.Redis;

[ExecuteInEditMode]
public class TableTest : MonoBehaviour {

	public Texture2D TableTexture;

	// Use this for initialization
	void Start () {
		Debug.Log("Start");
		RedisConnection connection = new RedisConnection();
		bool connected = connection.TryConnection();
		if (!connected)
		{
			Debug.Log("Not connected");
			return;
		}
			Debug.Log("Connected");
		TableTexture = Utils.GetImageAsTexture(connection.GetDataAccessProvider(), "camera0:view1");
	}
	
	// Update is called once per frame
	void Update () {

	}
}
