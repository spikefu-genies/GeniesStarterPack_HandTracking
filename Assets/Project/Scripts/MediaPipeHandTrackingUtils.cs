#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System.Collections.Generic;
using UnityEngine;
using KeyPoint = OpenCVForUnityExample.DnnModel.MediaPipeHandPoseEstimator.KeyPoint;

/// <summary>
/// Based on MediaPipeHandPoseSkeletonVisualizer
/// from namespace OpenCVForUnityExample.DnnModel
/// 
/// Expanded and modified by Spike Fu
///
/// </summary>

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MediaPipeHandTrackingUtils : MonoBehaviour
{
    public class Skeleton
    {
        public GameObject LineObject;
        public LineRenderer Line;
    }

    private const int numSkeletons = 20;
    private List<Skeleton> skeletons = new List<Skeleton>();
    public Material skeletonMaterial;

    private bool _showSkeleton = true;
    public bool showSkeleton
    {
        get
        {
            return _showSkeleton;
        }
        set
        {
            _showSkeleton = value;
            ClearLine();
        }
    }

    public float skeletonX;
    public float skeletonY;
    public float skeletonZ;
    public float skeletonScale = 1f;

    private Vector3[] landmarks_world_buffer;

    private void Start()
    {

    }

    public void UpdatePoseAndNormal(Vector3[] landmarks_world, Vector3 normal)
    {
        if (landmarks_world == null || landmarks_world.Length < 21)
            return;

        if (skeletons.Count == 0)
        {
            for (int i = 0; i < numSkeletons; ++i)
            {
                AddSkeleton();
            }
        }

        if (normal != null && skeletons.Count == 20)
        {
            AddSkeleton();
        }

        // Add scaling and shifting to world landmark coordinates, and convert from a right-handed coordinate system to a left-handed coordinate system (Unity).
        void _set_line_position(int index, int idx1, int idx2)
        {
            skeletons[index].Line.SetPosition(0, MappingCoordinate(landmarks_world[idx1]));
            skeletons[index].Line.SetPosition(1, MappingCoordinate(landmarks_world[idx2]));
        }

        //hand skeleton
        _set_line_position(0, (int)KeyPoint.Wrist, (int)KeyPoint.Thumb1);
        _set_line_position(1, (int)KeyPoint.Thumb1, (int)KeyPoint.Thumb2);
        _set_line_position(2, (int)KeyPoint.Thumb2, (int)KeyPoint.Thumb3);
        _set_line_position(3, (int)KeyPoint.Thumb3, (int)KeyPoint.Thumb4);

        _set_line_position(4, (int)KeyPoint.Wrist, (int)KeyPoint.Index1);
        _set_line_position(5, (int)KeyPoint.Index1, (int)KeyPoint.Index2);
        _set_line_position(6, (int)KeyPoint.Index2, (int)KeyPoint.Index3);
        _set_line_position(7, (int)KeyPoint.Index3, (int)KeyPoint.Index4);

        _set_line_position(8, (int)KeyPoint.Wrist, (int)KeyPoint.Middle1);
        _set_line_position(9, (int)KeyPoint.Middle1, (int)KeyPoint.Middle2);
        _set_line_position(10, (int)KeyPoint.Middle2, (int)KeyPoint.Middle3);
        _set_line_position(11, (int)KeyPoint.Middle3, (int)KeyPoint.Middle4);

        _set_line_position(12, (int)KeyPoint.Wrist, (int)KeyPoint.Ring1);
        _set_line_position(13, (int)KeyPoint.Ring1, (int)KeyPoint.Ring2);
        _set_line_position(14, (int)KeyPoint.Ring2, (int)KeyPoint.Ring3);
        _set_line_position(15, (int)KeyPoint.Ring3, (int)KeyPoint.Ring4);

        _set_line_position(16, (int)KeyPoint.Wrist, (int)KeyPoint.Pinky1);
        _set_line_position(17, (int)KeyPoint.Pinky1, (int)KeyPoint.Pinky2);
        _set_line_position(18, (int)KeyPoint.Pinky2, (int)KeyPoint.Pinky3);
        _set_line_position(19, (int)KeyPoint.Pinky3, (int)KeyPoint.Pinky4);

        //normal
        if (skeletons[20] != null)
        {
            skeletons[20].Line.SetPosition(0, MappingCoordinate(landmarks_world[0]));
            skeletons[20].Line.SetPosition(1, MappingCoordinate(landmarks_world[0] + normal));
        }
    }

    public Vector3 MappingCoordinate(Vector3 point)
    {
        // Add scaling and shifting to world landmark coordinates.
        return new Vector3(
            point.x * skeletonScale * 1 + skeletonX,
            point.y * skeletonScale * -1 + skeletonY,
            point.z * skeletonScale * 1 + skeletonZ);
    }


    private void AddSkeleton()
    {
        var lineObject = new GameObject("Line");
        lineObject.transform.parent = gameObject.transform;

        lineObject.layer = gameObject.layer;

        var sk = new Skeleton()
        {
            LineObject = lineObject
        };

        sk.Line = sk.LineObject.AddComponent<LineRenderer>();
        sk.Line.startWidth = 0.004f * skeletonScale;
        sk.Line.endWidth = 0.001f * skeletonScale;

        // define the number of vertex
        sk.Line.positionCount = 2;
        sk.Line.material = skeletonMaterial;

        skeletons.Add(sk);
    }

    private void ClearLine()
    {
        if (skeletons.Count != numSkeletons)
            return;

        for (int i = 0; i < numSkeletons; ++i)
        {
            skeletons[i].Line.positionCount = 0;
            skeletons[i].Line.positionCount = 2;
        }
    }
}
#endif