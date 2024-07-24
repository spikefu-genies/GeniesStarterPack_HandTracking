using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using KeyPoint = OpenCVForUnityExample.DnnModel.MediaPipeHandPoseEstimator.KeyPoint;
using EstimationData = OpenCVForUnityExample.DnnModel.MediaPipeHandPoseEstimator.EstimationData;

public class MediaPipeHandToGenieHand : MonoBehaviour
{
    private Vector3[] landmarks_world_buffer;
    private EstimationData handEstimationData;

    private Vector3[] jointRotations;
    private Transform[] joints;

    public float x = 0;
    public float z = 0;
    public float y = 0;

    public float elbowX = 0;
    public float elbowY = 0;
    public float elbowZ = 0;

    public Transform elbowJoint;

    public Transform wristJoint;
    public Transform thumb1;
    public Transform thumb2;
    public Transform thumb3;
    public Transform index1;
    public Transform index2;
    public Transform index3;
    public Transform middle1;
    public Transform middle2;
    public Transform middle3;
    public Transform ring1;
    public Transform ring2;
    public Transform ring3;
    public Transform pinky1;
    public Transform pinky2;
    public Transform pinky3;

    // Start is called before the first frame update
    void Start()
    {
        jointRotations = new Vector3[16];
        joints = new Transform[16];
        joints[0] = wristJoint;
        joints[1] = thumb1;
        joints[2] = thumb2;
        joints[3] = thumb3;
        joints[4] = index1;
        joints[5] = index2;
        joints[6] = index3;
        joints[7] = middle1;
        joints[8] = middle2;
        joints[9] = middle3;
        joints[10] = ring1;
        joints[11] = ring2;
        joints[12] = ring3;
        joints[13] = pinky1;
        joints[14] = pinky2;
        joints[15] = pinky3;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdatePose(Vector3[] fingerBones, Vector3 handNormal) {
        if (fingerBones == null || fingerBones.Length < 20)
        {
            Debug.Log("Not enough finegr bones");
            return;
        }

        // Add wrist joint rotation
        jointRotations[0] = handNormal;
        // Add every finger joint rotation
        // Ignore fingerBones #3 #7 #11 #15 #19 (finger tip bones)
        jointRotations[1] = fingerBones[0];
        jointRotations[2] = fingerBones[1];
        jointRotations[3] = fingerBones[2];

        jointRotations[4] = fingerBones[4];
        jointRotations[5] = fingerBones[5];
        jointRotations[6] = fingerBones[6];

        jointRotations[7] = fingerBones[8];
        jointRotations[8] = fingerBones[9];
        jointRotations[9] = fingerBones[10];

        jointRotations[10] = fingerBones[12];
        jointRotations[11] = fingerBones[13];
        jointRotations[12] = fingerBones[14];

        jointRotations[13] = fingerBones[16];
        jointRotations[14] = fingerBones[17];
        jointRotations[15] = fingerBones[18];
    }

    void LateUpdate()
    {
        // Overwrite the joint's position
        // (MediaPipe doesn't have rotation value)

        // Create a quaternion from the desired Euler angles

        for(int i = 0; i<joints.Length; i++)
        {
            Quaternion worldRotation = Quaternion.LookRotation(jointRotations[i], Vector3.up) * Quaternion.Euler(x, y, z);
            joints[i].localRotation = worldRotation;
        }

        elbowJoint.localRotation = Quaternion.Euler(elbowX, elbowY, elbowZ);
        //Quaternion worldRotation = Quaternion.LookRotation(jointRotations[0], Vector3.up)*Quaternion.Euler(x, y, z);

        // Convert the world rotation to local rotation relative to the parent
        // Quaternion localRotation = Quaternion.Inverse(parentTransform.rotation) * worldRotation;

        // Overwrite the joint's local rotation with the calculated local rotation
        //wristJoint.localRotation = worldRotation;

        //elbowJoint.localRotation = Quaternion.Euler(-33.602f, 114.727f, 42.903f);
    }

    private static Vector3 MediaPippeToUnityPosition(Vector3 mPos)
    {
        // Converts right-handed MediaPipe position to left-handed Unity position
        // Center starts
        return new Vector3(mPos.x, -mPos.y, mPos.z);
    }

    private static Quaternion MediaPipeToUnityRotation(Vector3 mRot)
    {
        // Converts a right-handed, xyz order Maya euler rotation to a left-handed Unity quaternion
        var qx = Quaternion.AngleAxis(mRot.x, Vector3.right);
        var qy = Quaternion.AngleAxis(-1 * mRot.y, Vector3.up);
        var qz = Quaternion.AngleAxis(-1 * mRot.z, Vector3.forward);

        return qz * qy * qx;
    }
}
