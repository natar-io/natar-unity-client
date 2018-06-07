using System;

[Serializable]
public class Markers {
	public Marker[] markers;
	public float[] pose;
}

[Serializable]
public class Marker {
    public int id;
    public int dir;
    public int confidence;
    public string type;
    public float[] center;
    public float[] corners;
}
