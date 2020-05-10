using UnityEngine;
using System.Collections;

/// MouseLook rotates the transform based on the mouse delta.
/// Minimum and Maximum values can be used to constrain the possible rotation

/// To make an FPS style character:
/// - Create a capsule.
/// - Add the MouseLook script to the capsule.
///   -> Set the mouse look to use LookX. (You want to only turn character but not tilt it)
/// - Add FPSInputController script to the capsule
///   -> A CharacterMotor and a CharacterController component will be automatically added.

/// - Create a camera. Make the camera a child of the capsule. Reset it's transform.
/// - Add a MouseLook script to the camera.
///   -> Set the mouse look to use LookY. (You want the camera to tilt up and down like a head. The character already turns.)
[AddComponentMenu("Camera-Control/Mouse Look")]
public class MyMouseLook : MonoBehaviour {

	public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }

	public RotationAxes axes = RotationAxes.MouseXAndY;

	public float sensitivityX = 15F;
	public float sensitivityY = 15F;

	public float minimumX = -360F;
	public float maximumX = 360F;

	public float minimumY = -60F;
	public float maximumY = 60F;

	float rotationY = 0F;
	float rotationX = 0F;


	void Start ()
	{
		// Freeze rotation due to Physics Engine
		if (GetComponent<Rigidbody>()) {
			
			GetComponent<Rigidbody>().freezeRotation = true;
		}
	
	} // end Start


	void Update ()
	{
		// Handle the branch cut
		// Make sure the minimums are always less than the maximums
		if(minimumX > maximumX) {
			minimumX -= 360;
		}
		if(minimumY > maximumY) {
			minimumY -= 360;
		}

		// Get Horizontal axis input?
		if( axes == RotationAxes.MouseXAndY || axes == RotationAxes.MouseX) {
	
			rotationX += Input.GetAxis("Mouse X") * sensitivityX;

			// Limit the rotation in either direction only if the ranges is less than 360
			if(maximumX - minimumX < 360.0) {
				rotationX = Mathf.Clamp(rotationX, minimumX, maximumX);
			}
		}

		// Get Vertical axis input?
		if( axes == RotationAxes.MouseXAndY || axes == RotationAxes.MouseY) {

			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;

			// Limit the rotation in either direction only if the ranges is less than 360
			if(maximumY - minimumY < 360.0) {
				rotationY = Mathf.Clamp (rotationY, minimumY, maximumY);
			}
		}

		// Adjust the transform
		if (axes == RotationAxes.MouseXAndY)
		{
			transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
		}
		else if (axes == RotationAxes.MouseX)
		{
			transform.localEulerAngles = new Vector3( transform.localEulerAngles.x, rotationX, 0);

		}
		else // RotationAxes.MouseY
		{
			transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
		}

	} // end Update
	
} // end MyMouseLook