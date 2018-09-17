using UnityEngine;
using System.Collections;

//target & obstacle layer
//script on controlled char
public class FieldOfView : MonoBehaviour {

    public float viewRadius;
    public float viewAngle;

    //take an angle and spit out the direction of that angle
    public Vector3 DirFromAngle(float angleInDegrees)
    {
        //trigonometrie circle, unity starts with 0 on top and goes counter clockwise
        //convert unity circle to trig circle, 90-x = swap sin with cos
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }



}
