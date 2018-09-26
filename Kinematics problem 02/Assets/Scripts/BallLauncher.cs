using UnityEngine;
using System.Collections;

public class BallLauncher : MonoBehaviour {

	public Rigidbody ball;
	public Transform target;

	public float h = 25;
	public float gravity = -18;

	public bool debugPath;

	void Start() {
        //dont let gravity take effect until the ball is launches
		ball.useGravity = false;
	}

    //spacekey call the launch message
	void Update() {
		if (Input.GetKeyDown (KeyCode.Space)) {
			Launch ();
		}

		if (debugPath) {
			DrawPath ();
		}
	}

    //set the balls velocity 
	void Launch() {
        //set the gravity of the rigidbody to the own gravity value
		Physics.gravity = Vector3.up * gravity;
		ball.useGravity = true;
		ball.velocity = CalculateLaunchData ().initialVelocity;
	}

	LaunchData CalculateLaunchData() {
        //pY
		float displacementY = target.position.y - ball.position.y;
        //pX in 3D
		Vector3 displacementXZ = new Vector3 (target.position.x - ball.position.x, 0, target.position.z - ball.position.z);
        //vertical velocity
        //pX / (sqrt(-2h/g)) (sqrt(2(py-h) / g)
		float time = Mathf.Sqrt(-2*h/gravity) + Mathf.Sqrt(2*(displacementY - h)/gravity);
        //horicontal velocity
		Vector3 velocityY = Vector3.up * Mathf.Sqrt (-2 * gravity * h);
		Vector3 velocityXZ = displacementXZ / time;

        //Mathf.Sign makes able to give positive gravity
		return new LaunchData(velocityXZ + velocityY * -Mathf.Sign(gravity), time);
	}

    //Draw arc and split it up to 30 line segments
	void DrawPath() {
		LaunchData launchData = CalculateLaunchData ();
		Vector3 previousDrawPoint = ball.position;

        //number of line segments
		int resolution = 30;
		for (int i = 1; i <= resolution; i++) {
            //gets the current time to draw the line, value between 0 and overall time
			float simulationTime = i / (float)resolution * launchData.timeToTarget;
            //s = ut + (at^2 / 2)
			Vector3 displacement = launchData.initialVelocity * simulationTime + Vector3.up *gravity * simulationTime * simulationTime / 2f;
			Vector3 drawPoint = ball.position + displacement;
			Debug.DrawLine (previousDrawPoint, drawPoint, Color.green);
			previousDrawPoint = drawPoint;
		}
	}

	struct LaunchData {
		public readonly Vector3 initialVelocity;
		public readonly float timeToTarget;

		public LaunchData (Vector3 initialVelocity, float timeToTarget)
		{
			this.initialVelocity = initialVelocity;
			this.timeToTarget = timeToTarget;
		}
		
	}
}
	