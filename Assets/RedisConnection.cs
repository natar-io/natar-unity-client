using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using StackExchange.Redis;
using TeamDev.Redis;

public class RedisConnection : MonoBehaviour {

//    ConnectionMultiplexer redis;

    private RedisDataAccessProvider redis;
    private string[] keys;
    
    void Start () {
	Debug.Log("Redis Connection start");
	
	redis = new RedisDataAccessProvider ();
	redis.Configuration.Host = "127.0.0.1";
	redis.Configuration.Port = 6379;
	redis.Connect ();
	redis.SendCommand (RedisCommand.KEYS, "*");
	keys = redis.ReadMultiString ();
	for (int i = 0; i < keys.Length; i++) {
	    redis.SendCommand (RedisCommand.GET, keys [i]);
	    string value = redis.ReadString ();
	    Debug.Log(i.ToString()+" "+ keys[i] + ":" + value);
	}

    // Create the texture
    }

    Texture2D videoTexture;
    
    void createTexture(){

    // Create a new 2x2 texture ARGB32 (32 bit with alpha) and no mipmaps
         var videoTexture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
         
         // set the pixel values
         videoTexture.SetPixel(0, 0, new Color(1.0f, 1.0f, 1.0f, 0.5f));
         videoTexture.SetPixel(1, 0, Color.clear);
         videoTexture.SetPixel(0, 1, Color.white);
         videoTexture.SetPixel(1, 1, Color.black);
         
         // Apply all SetPixel calls
         videoTexture.Apply();
         
         // connect texture to material of GameObject this script is attached to

         GetComponent<Renderer>().material.mainTexture = videoTexture;
    }
	
    // Update is called once per frame
    void Update () {
	
    }
}
