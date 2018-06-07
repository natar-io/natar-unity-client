using System.Collections;
using System.Collections.Generic;
using System.Linq; // Int32
using System; // Exception

using UnityEngine; // Color32, Texture2D
using TeamDev.Redis; // RedisDataAccessProvider

public static class Utils {
    
    /// <summary>
    /// Tries to read redis command output as an int.
    /// </summary>
    /// <param name="redis"></param>
    /// <param name="commandId">the command to read the output</param>
    /// <returns>the int if success, else null</returns>
    public static int? RedisTryReadInt(RedisDataAccessProvider redis, int commandId)
    {
        string value = null;
        value = RedisTryReadString(redis, commandId);
        int val;
        if (Int32.TryParse(value, out val)) {
            return val;
        }
        return null;
    }

    /// <summary>
    /// Tries to read redis command output as a string.
    /// </summary>
    /// <param name="redis"></param>
    /// <param name="commandId">the command to read the output</param>
    /// <returns>the string if success, else null.</returns>
    public static string RedisTryReadString(RedisDataAccessProvider redis, int commandId)
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

    /// <summary>
    /// Tries to read redis command output as raw data (byte)
    /// </summary>
    /// <param name="redis"></param>
    /// <param name="commandId">the command to read the output from</param>
    /// <returns></returns>
    public static byte[] RedisTryReadData(RedisDataAccessProvider redis, int commandId)
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

    /// <summary>
    /// Convert raw image data (byte array) into Color32 array.
    /// </summary>
    /// <param name="data">Raw image data</param>
    /// <returns></returns>
    public static Color32[] ByteArrayToColor(byte[] data)
    {
        Color32[] colorData = new Color32[data.Length/3];
        for (int i = 0 ; i < data.Length ; i += 3) {
            Color32 value = new Color32(data[i], data[i+1], data[i+2], 255);
            colorData[i/3] = value;
        }
        return colorData;
    }

    /// <summary>
    /// Draw a plain circle on a texture at a specified position
    /// </summary>
    /// <param name="tex">the texture to draw on</param>
    /// <param name="cx">the x position</param>
    /// <param name="cy">the y position</param>
    /// <param name="r">the circle radius</param>
    /// <param name="col">the circle color</param>
    public static void Circle(Texture2D tex, int cx, int cy, int r, Color col) {
         int x, y, px, nx, py, ny, d;
         
         for (x = 0; x <= r; x++)
         {
             d = (int)Mathf.Ceil(Mathf.Sqrt(r * r - x * x));
             for (y = 0; y <= d; y++)
             {
                 px = cx + x;
                 nx = cx - x;
                 py = cy + y;
                 ny = cy - y;
 
                 tex.SetPixel(px, py, col);
                 tex.SetPixel(nx, py, col);
  
                 tex.SetPixel(px, ny, col);
                 tex.SetPixel(nx, ny, col);
             }
         }    
    }
}
