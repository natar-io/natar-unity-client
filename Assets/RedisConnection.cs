using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

using UnityEngine;
// using StackExchange.Redis;
using TeamDev.Redis;

public class RedisConnection : MonoBehaviour {
    
    // Set this as a public variable so it can be edited from Unity Editor*
    public String ServerIp = "127.0.0.1";
    public int ServerPort = 6379;
    public String ServerKey = "custom:image";

    private RedisDataAccessProvider redis;
    private Texture2D videoTexture;
    
    void Start() {
        Debug.Log("Redis Connection start");

        redis = new RedisDataAccessProvider ();
        redis.Configuration.Host = ServerIp;
        redis.Configuration.Port = ServerPort;
        // TODO: Check connection status 
        redis.Connect();

        RedisImageToTexture();
    }

    void RedisImageToTexture()
    {
        int commandId;
        commandId = redis.SendCommand(RedisCommand.GET, ServerKey + ":width");
        int? width = RedisTryReadInt(commandId);

        commandId = redis.SendCommand(RedisCommand.GET, ServerKey + ":height");
        int? height = RedisTryReadInt(commandId);

        if (!width.HasValue || !height.HasValue)
        {
            throw new ArgumentException("Could not fetch image width or height from redis server. Please check connection settings.");
        }

        // Get this particular commandId
        commandId = redis.SendCommand(RedisCommand.GET, ServerKey);
        // Get image data from this particular command to avoid unexpected results
        byte[] imageData = RedisTryReadData(commandId);
        Color32[] image = ByteArrayToColor(imageData);

        videoTexture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
        videoTexture.SetPixels32(image);
        videoTexture.Apply(false);
        this.GetComponent<Renderer>().material.mainTexture = videoTexture;
    }
    
    // Update is called once per frame
    void Update() { }

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
        } 
        catch (Exception e) {
            Debug.LogError("Failed read string.");
        }
        return value;
    }

    // Helper function to read byte array from redis server
    byte[] RedisTryReadData(int commandId)
    {
        byte[] data = null;
        try {
            data = redis.ReadData(commandId);
        } 
        catch (Exception e) {
            Debug.Log("Failed to read data.");
        }
        return data;
    }

    Color32[] ByteArrayToColor(byte[] data)
    {
        Color32[] colorData = new Color32[data.Length/3];
        for (int i = 0 ; i < data.Length ; i += 3) {
            Color32 value = new Color32(data[i], data[i+1], data[i+2], 255);
            colorData[i/3] = value;
        }
        return colorData;
    }

    Color32[] SpeByteArrayToColor(byte[] data)
    {
        Color32[] colorData = new Color32[(data.Length-27)/4];
        for (int i = 27 ; i < data.Length ; i += 4) {
            Color32 value = new Color32(data[i], data[i+1], data[i+2], 255);
            colorData[(i-27)/4] = value;
        }
        return colorData;
    }
}
