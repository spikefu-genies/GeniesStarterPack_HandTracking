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

    private List<Vector3> desiredLandmarks;

    // Start is called before the first frame update
    void Start()
    {
        desiredLandmarks = new List<Vector3>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdatePose(Vector3[] landmarks_world) {
        if (landmarks_world == null || landmarks_world.Length < 21)
            return;

        // Vector3[] landmarks_world is raw MediaPipe output:

        /// The 21 hand landmarks are also presented in world coordinates.
        /// Each landmark is composed of x, y, and z
        /// Representing real-world 3D coordinates in meters
        /// with the origin at the hand’s geometric center.
        /// Ignore #4 #8 #12 #16 #20 from MediaPipe output(finger tip joints)
        for (int i = 0; i < 20; ++i)
        {
            if (i != 4 && i != 8 && i != 12 && i != 16 && i != 20)
            {
                desiredLandmarks.Add(landmarks_world[i]);
            }
        }

        //Shift origin from center to wirst location
        float x_offset = desiredLandmarks[0].x;
        float y_offset = desiredLandmarks[0].y;
        float z_offset = desiredLandmarks[0].z;
        for (int i = 0; i < desiredLandmarks.Count; i++)
        {
            float new_x = desiredLandmarks[i].x - x_offset;
            float new_y = desiredLandmarks[i].y - y_offset;
            float new_z = desiredLandmarks[i].z - z_offset;
            desiredLandmarks[i] = new Vector3(new_x, new_y, new_z);
            Debug.Log(desiredLandmarks[i]);
        }

        // Scale the translation (only for visual debug purposes)
        // Need to cache data of 10 previous frame
        // convert from a right-handed coordinate system to a left-handed coordinate system (Unity).


        // TO-DO:
        /// 1. Cache data from 10 previous frames
        /// 2. Derive translation and rotation value
        /// 3. Convert from a right-handed coordinate system to a left-handed coordinate system (Unity).
        /// 4. Drive Genie's skeleton with the coordinates in LateUpdate()

    }

    void LateUpdate()
    {
        // Overwrite the joint's position
        // (MediaPipe doesn't have rotation value)

        /// for each joint
        /// joint.localPosition = desiredPosition;
    }
}
