using UnityEngine;
using System.Collections;

// Author: Dave Hampson (http://wiki.unity3d.com/index.php?title=FramesPerSecond)
// Modified by: nclsp
public class FpsDisplay : MonoBehaviour
{
	float deltaTime = 0.0f;

	void Start() {
		if (!Debug.isDebugBuild)
			Destroy(this.gameObject);
	}
 
	void Update()
	{
		deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
	}
 
	void OnGUI()
	{
		int w = Screen.width, h = Screen.height;
 
		GUIStyle style = new GUIStyle();
 
		Rect rect = new Rect(0, 0, w, h * 2 / 100);
		style.alignment = TextAnchor.UpperLeft;
		style.fontSize = h * 2 / 75;
		float msec = deltaTime * 1000.0f;
		float fps = 1.0f / deltaTime;
		if (fps > 30)
			style.normal.textColor = new Color (0.0f, 1.0f, 0.0f, 1.0f);
		else if (fps > 15)
			style.normal.textColor = new Color (1.0f, 0.65f, 0.0f, 1.0f);
		else
			style.normal.textColor = new Color (1.0f, 0.0f, 0.0f, 1.0f);
		
		style.fontStyle = FontStyle.Bold;
		string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
		GUI.Label(rect, text, style);
	}
}