using UnityEngine;
using System.Collections;

// Author: Dave Hampson (http://wiki.unity3d.com/index.php?title=FramesPerSecond)
// Modified by: nclsp
public class FpsDisplay : MonoBehaviour
{
	public bool OnlyDebugBuild = true;
	public TextAnchor DisplayLocation;
	[Range(8, 24)]
	public int TextSize = 13;
	
	float elapsedTime = 0.0f;


	void Start() {
		if (!Debug.isDebugBuild && OnlyDebugBuild)
			Destroy(this.gameObject);
	}
 
	void Update()
	{
		// Using elapsed delta time
		elapsedTime += (Time.unscaledDeltaTime - elapsedTime) * 0.1f;
	}
 
	void OnGUI()
	{
		int w = Screen.width, h = Screen.height;
 
		GUIStyle style = new GUIStyle();
 
		Rect rect = new Rect(0, 0, w, h);

		style.alignment = DisplayLocation;
		style.fontSize = TextSize;
		float msec = elapsedTime * 1000.0f;
		float fps = 1.0f / elapsedTime;
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