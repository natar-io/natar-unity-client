using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ApplicationParameters {
	public enum CameraType {
		RGB,
		GRAY,
		DEPTH,
		PROJECTOR,
	};
	public static Matrix4x4 pose3D = Matrix4x4.identity;

	public static IntrinsicsParameters RGBCameraIntrinsics = null;
	public static bool RGBCameraAvailable = false;
	
	public static IntrinsicsParameters DepthCameraIntrinsics = null;
	public static ExtrinsicsParameters DepthCameraExtrinsics = null;
	public static bool DepthCameraAvailable = false;

	public static IntrinsicsParameters ProjectorIntrinsics = null;
	public static ExtrinsicsParameters ProjectorExtrinsics = null;
	public static bool ProjectorAvailable = false;
}
