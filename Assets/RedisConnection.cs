using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
// using StackExchange.Redis;
using TeamDev.Redis;

public class RedisConnection : MonoBehaviour {
    
    // Set this as a public variable so it can be edited from Unity Editor
    public String key = "costum:image";

    private RedisDataAccessProvider redis;
    private Texture2D videoTexture;
    
    void Start() {
        Debug.Log("Redis Connection start");

        redis = new RedisDataAccessProvider ();
        redis.Configuration.Host = "127.0.0.1";
        redis.Configuration.Port = 6379;
        redis.Connect();

        RedisImageToTexture();
    }

    void RedisImageToTexture()
    {
        // Get this particular commandId
        int commandId = redis.SendCommand(RedisCommand.GET, key);
        // Get image data from this particular command to avoid unexpected results
        string imageData = RedisTryReadString(commandId);

        commandId = redis.SendCommand(RedisCommand.GET, key + ":width");
        Debug.Log("Getting " + key + ":width");
        int? width = RedisTryReadInt(commandId);

        commandId = redis.SendCommand(RedisCommand.GET, key + ":height");
        int? height = RedisTryReadInt(commandId);

        if (!width.HasValue || !height.HasValue)
        {
            throw new ArgumentException("Could not create image from data: invalid width or height");
        }

        Debug.Log("Image:" + (int)width + "x" + (int)height);

        videoTexture = new Texture2D((int)width, (int)height, TextureFormat.ARGB32, false);
        
        videoTexture.SetPixel(0, 0, new Color(1.0f, 1.0f, 1.0f, 0.5f));
        videoTexture.SetPixel(1, 0, Color.clear);
        videoTexture.SetPixel(0, 1, Color.white);
        videoTexture.SetPixel(1, 1, Color.black);

        videoTexture.Apply();
        this.GetComponent<Renderer>().material.mainTexture = videoTexture;
    }
    
    // Update is called once per frame
    void Update() {

    }

    // Helper function to read int from redis server
    int? RedisTryReadInt(int commandId)
    {
        string value = null;
        value = RedisTryReadString(commandId);
        int val;
        if (Int32.TryParse(value, out val)) {
            return val;
        }
        return null;
    }

    // Helper function to read string from redis server
    string RedisTryReadString(int commandId)
    {
        string value = null;
        try {
            value = redis.ReadString(commandId);
        } catch (Exception e) {
            Debug.LogError("Failed read string.");
        }
        return value;
    }
}
