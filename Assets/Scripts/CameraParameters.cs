using System;

[Serializable]
public class CameraParameters
{
	public int fx;
	public int fy;
	public int cx;
	public int cy;
	public int[] intrinsics;
	public int width;
	public int height;
}