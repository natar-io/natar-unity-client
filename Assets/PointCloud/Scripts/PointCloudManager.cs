using UnityEngine;
using System.Collections;
using System.IO;

public class PointCloudManager : MonoBehaviour {

	// File
	public string dataPath;
	private string filename = "PointCloudViewer";
	public Material matVertex;

	// GUI
	private float progress = 0;
	private string guiText;
	private bool loaded = false;

	// PointCloud
	private GameObject pointCloud;

	public float scale = 1;
	public bool invertYZ = false;
	public bool forceReload = false;

	public int numPoints;
	public int numPointGroups;
	/// <summary>
  /// Subdivisions of the point cloud
  /// </summary>
  private int limitPoints = 65000; 


	private Vector3[] points;
	private Color[] colors;
	private Vector3 minValue;

	
	void Start () {
		// Create Resources folder
		// createFolders ();

		// Get Filename
		// filename = Path.GetFileName(dataPath);

    // prepare the mesh for the depth data
		// loadScene ();
    // loadPointCloud ();

    // TODO: Pre-create the mesh, and just update the ponits ? 
    //    createMesh(); 


      //	StartCoroutine ("loadOFF", dataPath + ".off");
    //loadStoredMeshes();
	}

  // UpdateMesh 

	public void CreateMesh(Vector3[] depthPoints, Color[] colorPoints, int numPoints){

		// Read file
		// StreamReader sr = new StreamReader (Application.dataPath + dPath);
		// sr.ReadLine (); // OFF
		// string[] buffer = sr.ReadLine ().Split(); // nPoints, nFaces
		
    
		// numPoints = int.Parse (buffer[0]);
		// points = new Vector3[numPoints];
		// colors = new Color[numPoints];
		minValue = new Vector3();
		
    this.points = depthPoints;
    this.colors = colorPoints;

    // // Load the buffer... 
		// for (int i = 0; i< numPoints; i++){
			
    //   // buffer = sr.ReadLine ().Split ();

		// 	if (!invertYZ)
		// 		points[i] = new Vector3 (float.Parse (buffer[0])*scale, float.Parse (buffer[1])*scale,float.Parse (buffer[2])*scale) ;
		// 	else
		// 		points[i] = new Vector3 (float.Parse (buffer[0])*scale, float.Parse (buffer[2])*scale,float.Parse (buffer[1])*scale) ;
			
		// 	if (buffer.Length >= 5)
		// 		colors[i] = new Color (int.Parse (buffer[3])/255.0f,int.Parse (buffer[4])/255.0f,int.Parse (buffer[5])/255.0f);
		// 	else
		// 		colors[i] = Color.cyan;

		// 	// Relocate Points near the origin
		// 	//calculateMin(points[i]);

		// 	// GUI
		// 	// progress = i *1.0f/(numPoints-1)*1.0f;
		// 	// if (i%Mathf.FloorToInt(numPoints/20) == 0){
		// 	// 	guiText=i.ToString() + " out of " + numPoints.ToString() + " loaded";
		// 	// 	yield return null;
		// 	// }
		// }

		
		// Instantiate Point Groups
		numPointGroups = Mathf.CeilToInt (numPoints*1.0f / limitPoints*1.0f);

    Debug.Log("Point cloud creation... " + filename);
		pointCloud = new GameObject (filename);

		for (int i = 0; i < numPointGroups-1; i ++) {
      
      // Create the Mesh and load the points into it
			InstantiateMesh (i, limitPoints);
			// if (i%10==0){
			// 	guiText = i.ToString() + " out of " + numPointGroups.ToString() + " PointGroups loaded";
			// 	yield return null;
			// }
		}

    // Create the Mesh and load the points into it
		InstantiateMesh (numPointGroups-1, numPoints- (numPointGroups-1) * limitPoints);

		//Store PointCloud
		// UnityEditor.PrefabUtility.CreatePrefab ("Assets/Resources/PointCloudMeshes/" + filename + ".prefab", pointCloud);

		loaded = true;
	}


	// // Start Coroutine of reading the points from the OFF file and creating the meshes
	// IEnumerator loadOFF(string dPath){

	// 	// Read file
	// 	StreamReader sr = new StreamReader (Application.dataPath + dPath);
	// 	sr.ReadLine (); // OFF
	// 	string[] buffer = sr.ReadLine ().Split(); // nPoints, nFaces
		
	// 	numPoints = int.Parse (buffer[0]);
	// 	points = new Vector3[numPoints];
	// 	colors = new Color[numPoints];
	// 	minValue = new Vector3();
		
	// 	for (int i = 0; i< numPoints; i++){
	// 		buffer = sr.ReadLine ().Split ();

	// 		if (!invertYZ)
	// 			points[i] = new Vector3 (float.Parse (buffer[0])*scale, float.Parse (buffer[1])*scale,float.Parse (buffer[2])*scale) ;
	// 		else
	// 			points[i] = new Vector3 (float.Parse (buffer[0])*scale, float.Parse (buffer[2])*scale,float.Parse (buffer[1])*scale) ;
			
	// 		if (buffer.Length >= 5)
	// 			colors[i] = new Color (int.Parse (buffer[3])/255.0f,int.Parse (buffer[4])/255.0f,int.Parse (buffer[5])/255.0f);
	// 		else
	// 			colors[i] = Color.cyan;

	// 		// Relocate Points near the origin
	// 		//calculateMin(points[i]);

	// 		// GUI
	// 		progress = i *1.0f/(numPoints-1)*1.0f;
	// 		if (i%Mathf.FloorToInt(numPoints/20) == 0){
	// 			guiText=i.ToString() + " out of " + numPoints.ToString() + " loaded";
	// 			yield return null;
	// 		}
	// 	}

		
	// 	// Instantiate Point Groups
	// 	numPointGroups = Mathf.CeilToInt (numPoints*1.0f / limitPoints*1.0f);

	// 	pointCloud = new GameObject (filename);

	// 	for (int i = 0; i < numPointGroups-1; i ++) {
	// 		InstantiateMesh (i, limitPoints);
	// 		if (i%10==0){
	// 			guiText = i.ToString() + " out of " + numPointGroups.ToString() + " PointGroups loaded";
	// 			yield return null;
	// 		}
	// 	}
	// 	InstantiateMesh (numPointGroups-1, numPoints- (numPointGroups-1) * limitPoints);

	// 	//Store PointCloud
	// 	UnityEditor.PrefabUtility.CreatePrefab ("Assets/Resources/PointCloudMeshes/" + filename + ".prefab", pointCloud);

	// 	loaded = true;
	// }

	
	void InstantiateMesh(int meshInd, int nPoints){
		// Create Mesh
		GameObject pointGroup = new GameObject (filename + meshInd);
		pointGroup.AddComponent<MeshFilter> ();
		pointGroup.AddComponent<MeshRenderer> ();
		pointGroup.GetComponent<Renderer>().material = matVertex;

    // Points are loaded here
		pointGroup.GetComponent<MeshFilter> ().mesh = CreateMesh (meshInd, nPoints, limitPoints);
		pointGroup.transform.parent = pointCloud.transform;


		// Store Mesh
		// UnityEditor.AssetDatabase.CreateAsset(pointGroup.GetComponent<MeshFilter> ().mesh, "Assets/Resources/PointCloudMeshes/" + filename + @"/" + filename + meshInd + ".asset");
		// UnityEditor.AssetDatabase.SaveAssets ();
		// UnityEditor.AssetDatabase.Refresh();
	}

  // Create the mesh with the correct points
	Mesh CreateMesh(int id, int nPoints, int limitPoints){
		
		Mesh mesh = new Mesh ();
		
		Vector3[] myPoints = new Vector3[nPoints]; 
		int[] indecies = new int[nPoints];
		Color[] myColors = new Color[nPoints];

		for(int i=0;i<nPoints;++i) {
			myPoints[i] = points[id*limitPoints + i] - minValue;
			indecies[i] = i;
			myColors[i] = colors[id*limitPoints + i];
		}

		mesh.vertices = myPoints;
		mesh.colors = myColors;
		mesh.SetIndices(indecies, MeshTopology.Points,0);
		mesh.uv = new Vector2[nPoints];
		mesh.normals = new Vector3[nPoints];

		return mesh;
	}

	void calculateMin(Vector3 point){
		if (minValue.magnitude == 0)
			minValue = point;


		if (point.x < minValue.x)
			minValue.x = point.x;
		if (point.y < minValue.y)
			minValue.y = point.y;
		if (point.z < minValue.z)
			minValue.z = point.z;
	}

}
