using System.Collections;
using System.Collections.Generic;
using System.Globalization; // '.' in floats
using System.Linq; // Int32
using System.Text; // Encoding.UTF8
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
    public static int? RedisTryReadInt (RedisDataAccessProvider redis, int commandId) {
        string value = null;
        value = RedisTryReadString (redis, commandId);
        int val;
        if (Int32.TryParse (value, out val)) {
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
    public static string RedisTryReadString (RedisDataAccessProvider redis, int commandId) {
        string value = null;
        try {
            value = redis.ReadString (commandId);
        } catch (Exception) {
            Debug.LogError ("Failed to read string.");
        }
        return value;
    }

    /// <summary>
    /// Tries to read redis command output as raw data (byte)
    /// </summary>
    /// <param name="redis"></param>
    /// <param name="commandId">the command to read the output from</param>
    /// <returns></returns>
    public static byte[] RedisTryReadData (RedisDataAccessProvider redis, int commandId) {
        byte[] data = null;
        try {
            data = redis.ReadData (commandId);
        } catch (Exception) {
            Debug.Log ("Failed to read data.");
        }
        return data;
    }

    /// <summary>
    /// Tries to get physical camera parameter saved in redis
    /// </summary>
    /// <param name="key">the key where to look for parameters</param>
    /// <returns></returns>
    public static IntrinsicsParameters RedisTryGetIntrinsics (RedisDataAccessProvider redis, string key) {
        int commandId = redis.SendCommand (RedisCommand.GET, key);
        string intrinsics = Utils.RedisTryReadString (redis, commandId);
        if (intrinsics != null) {
            IntrinsicsParameters intrinsicsParameters = JsonUtility.FromJson<IntrinsicsParameters> (intrinsics);
            return intrinsicsParameters;
        }
        return null;
    }

    public static ExtrinsicsParameters RedisTryGetExtrinsics (RedisDataAccessProvider redis, string key) {
        int commandId = redis.SendCommand (RedisCommand.GET, key);
        string extrinsics = Utils.RedisTryReadString (redis, commandId);
        if (extrinsics != null) {
            ExtrinsicsParameters extrinsicsParameters = JsonUtility.FromJson<ExtrinsicsParameters> (extrinsics);
            return extrinsicsParameters;
        }
        return null;
    }

    public static Position RedisTryGetPosition (RedisDataAccessProvider redis, string key) {
        int commandId = redis.SendCommand (RedisCommand.GET, key);
        string positionData = Utils.RedisTryReadString (redis, commandId);
        if (positionData != null) {
            Position position = JsonUtility.FromJson<Position> (positionData);
            return position;
        }
        return null;
    }

    /// <summary>
    /// Tries to get 3D pose information saved in redis
    /// </summary>
    /// <param name="key">the key whereto look for 3d pose"</param>
    /// <returns></returns>
    public static Matrix4x4? RedisTryGetPose3D (RedisDataAccessProvider redis, string key) {
        int commandId = redis.SendCommand (RedisCommand.GET, key);
        string jsonPose = Utils.RedisTryReadString (redis, commandId);
        if (jsonPose != null) {
            // Hand made parsing
            return JSONToPose3D (jsonPose);
        }
        return null;
    }

    /// <summary>
    /// Convert JSON string into a 4x4 Matrix
    /// </summary>
    /// <param name="json">the JSON string to convert</param>
    /// <returns></returns>
    public static Matrix4x4 JSONToPose3D (string json) {
        var charstoRemove = new string[] { ",", "[", "]", "\n", "\t" };
        foreach (var c in charstoRemove) {
            json = json.Replace (c, string.Empty);
        }
        Matrix4x4 poseMatrix = new Matrix4x4 ();
        string[] poseValue = json.Split (new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0 ; i < 4 ; i++)
        {
            float val0 = float.Parse(poseValue[i*4], CultureInfo.InvariantCulture.NumberFormat);
            float val1 = float.Parse(poseValue[i*4+1], CultureInfo.InvariantCulture.NumberFormat);
            float val2 = float.Parse(poseValue[i*4+2], CultureInfo.InvariantCulture.NumberFormat);
            float val3 = float.Parse(poseValue[i*4+3], CultureInfo.InvariantCulture.NumberFormat);
            poseMatrix.SetRow(i, new Vector4 (val0, val1, val2, val3));
        }
        return poseMatrix;
    }

    /// <summary>
    /// Convert raw image data (byte array) into Color32 array.
    /// </summary>
    /// <param name="data">Raw image data</param>
    /// <returns></returns>
    public static Color32[] ByteArrayToColor (byte[] data) {
        Color32[] colorData = new Color32[data.Length / 3];
        for (int i = 0; i < data.Length; i += 3) {
            Color32 value = new Color32 (data[i], data[i + 1], data[i + 2], 255);
            colorData[i / 3] = value;
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
    public static void Circle (Texture2D tex, int cx, int cy, int r, Color col) {
        int x, y, px, nx, py, ny, d;

        for (x = 0; x <= r; x++) {
            d = (int) Mathf.Ceil (Mathf.Sqrt (r * r - x * x));
            for (y = 0; y <= d; y++) {
                px = cx + x;
                nx = cx - x;
                py = cy + y;
                ny = cy - y;

                tex.SetPixel (px, py, col);
                tex.SetPixel (nx, py, col);

                tex.SetPixel (px, ny, col);
                tex.SetPixel (nx, ny, col);
            }
        }
    }

    /// <summary>
    /// Extract Rotation from a 4x4 matrix
    /// </summary>
    /// <param name="matrix">the 4x4 Matrix</param>
    /// <returns></returns>
    public static Quaternion ExtractRotation (Matrix4x4 matrix) {
        return Quaternion.LookRotation (matrix.GetColumn(2), matrix.GetColumn(1));
    }

    /// <summary>
    /// Extract Translation from a 4x4 matrix
    /// </summary>
    /// <param name="matrix">the 4x4 Matrix</param>
    /// <returns></returns>
    public static Vector3 ExtractTranslation (Matrix4x4 matrix) {
        return matrix.GetColumn(3);
    }

    /// <summary>
    /// Extract Scale from a 4x4 matrix
    /// </summary>
    /// <param name="matrix">the 4x4 Matrix</param>
    /// <returns></returns>
    public static Vector3 ExtractScale (Matrix4x4 matrix) {
        return new Vector3 (matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);
    }

    public static String ByteToString(byte[] data) {
        return Encoding.UTF8.GetString(data);
    }

    public static byte[] GRAY16ToRGB24(int width, int height, byte[] data) {
        byte[] dataRGB = new byte[width * height * 3];
        int cpt = 0;
        for (int i = 0 ; i < data.Length ; i+=2) {
            float ucolor = ((data[i+1] & 0xFF) * 255f) + (float) (data[i] & 0xFF);
            byte color = (byte) (ucolor / 2000f * 255f);
            dataRGB[cpt++] =  color;
            dataRGB[cpt++] =  color;
            dataRGB[cpt++] =  color;
        }
        return dataRGB;
    }

    public static Matrix4x4 FloatArrayToMatrix4x4(float[] array) {
        Matrix4x4 mat = new Matrix4x4();
        mat.SetRow(0, new Vector4(array[0], array[1], array[2], array[3]));
        mat.SetRow(1, new Vector4(array[4], array[5], array[6], array[7]));
        mat.SetRow(2, new Vector4(array[8], array[9], array[10], array[11]));
        mat.SetRow(3, new Vector4(array[12], array[13], array[14], array[15]));

        return mat;
    }

    public static void Log(string objName, string message, int logLevel = 0) {
        string content = "[" + objName + "] " + message;
        switch(logLevel) {
            case 0:
                Debug.Log(content);
                break;
            case 1:
                Debug.LogWarning(content);
                break;
            case 2:
                Debug.LogError(content);
                break;
            default:
                Debug.Log(content);
                break;
        }
    }

    public static Texture2D GetImageAsTexture(RedisDataAccessProvider redis, string key) {
        int commandId =  redis.SendCommand (RedisCommand.GET, key + ":width");
		int? width = Utils.RedisTryReadInt(redis, commandId);

		commandId =  redis.SendCommand (RedisCommand.GET, key + ":height");
		int? height = Utils.RedisTryReadInt(redis, commandId);

		commandId =  redis.SendCommand (RedisCommand.GET, key + ":channels");
		int? channels = Utils.RedisTryReadInt(redis, commandId);

        if (width == null || height == null || channels == null)
            return null;
        
        Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGB24, false, false);
        byte[] imageData = new byte[(int)width * (int) height * (int)channels];

        return GetImageIntoPreallocatedTexture(redis, key, texture, imageData, (int)width, (int)height, (int)channels);
    }

    public static Texture2D GetImageIntoPreallocatedTexture(RedisDataAccessProvider redis, string key, Texture2D texture, byte[] textureRaw, int width, int height, int channels = 3) {
        int commandId = redis.SendCommand(RedisCommand.GET, key);
        // 70-90% of the time is spent here
        textureRaw = Utils.RedisTryReadData(redis, commandId);
        if (textureRaw == null)
            return null;
        
        if (channels == 2) {
            textureRaw = Utils.GRAY16ToRGB24((int)width, (int)height, textureRaw);
        }

        texture.LoadRawTextureData(textureRaw);
        texture.Apply();
        return texture;
    }
}