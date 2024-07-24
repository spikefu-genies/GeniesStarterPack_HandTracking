using UnityEngine;

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnityExample.DnnModel;
using OpenCVForUnity.ImgprocModule;
using System.Collections.Generic;
using System.Text;
using System;
using KeyPoint = OpenCVForUnityExample.DnnModel.MediaPipeHandPoseEstimator.KeyPoint;

[RequireComponent(typeof(WebCamTextureToMatHelper))]
public class MediaPipeHandTracking : MonoBehaviour
{
    public MediaPipeHandTrackingUtils skeletonVisualizer_leftHand;
    public MediaPipeHandTrackingUtils skeletonVisualizer_rightHand;
    public DrawTriangle palmVisualizer_leftHand;
    public DrawTriangle palmVisualizer_rightHand;

    public MediaPipeHandToGenieHand toGenie_leftHand;
    public MediaPipeHandToGenieHand toGenie_rightHand;

    private Vector3[] landmarks_world_buffer;

    private CappedList<Vector3> normal_cache_leftHand;
    private CappedList<Vector3> normal_cache_rightHand;

    private CappedList<Vector3[]> landmark_cache_leftHand;
    private CappedList<Vector3[]> landmark_cache_rightHand;

    private CappedList<Vector3[]> fingerBones_cache_leftHand;
    private CappedList<Vector3[]> fingerBones_cache_rightHand;

    // The webcam texture to mat helper.
    WebCamTextureToMatHelper webCamTextureToMatHelper;

    // The bgr mat.
    Mat bgrMat;

    // The palm detector.
    MediaPipePalmDetector palmDetector;

    /// The handpose estimator.
    MediaPipeHandPoseEstimator handPoseEstimator;

    // PALM_DETECTION_MODEL_FILENAME
    protected static readonly string PALM_DETECTION_MODEL_FILENAME = "OpenCVForUnity/dnn/palm_detection_mediapipe_2023feb.onnx";

    // The palm detection model filepath.
    string palm_detection_model_filepath;

    // HANDPOSE_ESTIMATION_MODEL_FILENAME
    protected static readonly string HANDPOSE_ESTIMATION_MODEL_FILENAME = "OpenCVForUnity/dnn/handpose_estimation_mediapipe_2023feb.onnx";

    // The handpose estimation model filepath.
    string handpose_estimation_model_filepath;

#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

    // Start is called before the first frame update
    void Start()
    {
        webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

#if UNITY_WEBGL
            getFilePath_Coroutine = GetFilePath();
            StartCoroutine(getFilePath_Coroutine);
#else
        palm_detection_model_filepath = Utils.getFilePath(PALM_DETECTION_MODEL_FILENAME);
        handpose_estimation_model_filepath = Utils.getFilePath(HANDPOSE_ESTIMATION_MODEL_FILENAME);
        Run();
#endif
    }

#if UNITY_WEBGL
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync(PALM_DETECTION_MODEL_FILENAME, (result) =>
            {
                palm_detection_model_filepath = result;
            });
            yield return getFilePathAsync_0_Coroutine;

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync(HANDPOSE_ESTIMATION_MODEL_FILENAME, (result) =>
            {
                handpose_estimation_model_filepath = result;
            });
            yield return getFilePathAsync_1_Coroutine;

            getFilePath_Coroutine = null;

            Run();
        }
#endif

    // Update is called once per frame
    void Update()
    {
        if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
        {

            Mat rgbaMat = webCamTextureToMatHelper.GetMat();

            if (palmDetector == null || handPoseEstimator == null)
            {
                Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
            }
            else
            {
                Imgproc.cvtColor(rgbaMat, bgrMat, Imgproc.COLOR_RGBA2BGR);

                Mat palms = palmDetector.infer(bgrMat);

                List<Mat> hands = new List<Mat>();

                Debug.Log("Camera detected " + palms.rows() + " hands");

                // Estimate the pose of each hand
                for (int i = 0; i < palms.rows(); ++i)
                {
                    // Handpose estimator inference
                    Mat handpose = handPoseEstimator.infer(bgrMat, palms.row(i));

                    if (!handpose.empty())
                        hands.Add(handpose);
                }

                Imgproc.cvtColor(bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

                foreach (var hand in hands)
                {
                    handPoseEstimator.visualize(rgbaMat, hand, false, true);
                    var handEstimationData = handPoseEstimator.getData(hand);
                    Vector3[] handWorldLandmarks = ConvertMatToVector(hand);
                    // Move the origin of the landmarks from palm to wrist
                    handWorldLandmarks = MoveCoordinateOrigin(handWorldLandmarks);

                    

                    //if is left hand
                    if (handEstimationData.handedness >= 0.5f)
                    {
                        if (skeletonVisualizer_leftHand != null && skeletonVisualizer_leftHand.showSkeleton && toGenie_leftHand != null)
                        {  
                            // Calculate the Wrist Joint Rotation/ Normal of the hand
                            Vector3 handNormal = CalculateHandNormal(handWorldLandmarks[0], handWorldLandmarks[5], handWorldLandmarks[17]);

                            // Smooth the behavior of hand normal based on the previous frame cache
                            if (normal_cache_leftHand.Count != 0)
                            {
                                handNormal = CalculateWeightedAverage(
                                    normal_cache_leftHand[normal_cache_leftHand.Count - 1],
                                    handNormal, 5);
                            }

                            // Smooth the behavior of landmark based on previous frame cache
                            if (landmark_cache_leftHand.Count != 0)
                            {
                                for (int i = 0; i < handWorldLandmarks.Length; i++)
                                {
                                    handWorldLandmarks[i] = 
                                    CalculateWeightedAverage(
                                        landmark_cache_leftHand[landmark_cache_leftHand.Count-1][i],
                                        handWorldLandmarks[i], 20);
                                }
                            }
                            // Calculate vector of every finger bones based on smoothed landmark
                            Vector3[] fingerBones = CalculateFingerBoneVectors(handWorldLandmarks);

                            //Visualize hand skeleton and palm direction
                            skeletonVisualizer_leftHand.UpdatePoseAndNormal(handWorldLandmarks, handNormal);
                            palmVisualizer_leftHand.DrawTriangleUtils(skeletonVisualizer_leftHand.MappingCoordinate(handWorldLandmarks[17]),
                                skeletonVisualizer_leftHand.MappingCoordinate(handWorldLandmarks[5]),
                                skeletonVisualizer_leftHand.MappingCoordinate(handWorldLandmarks[0]));

                            //Cache joint rotations, for smoother output calculation
                            normal_cache_leftHand.Add(handNormal);
                            landmark_cache_leftHand.Add(handWorldLandmarks);

                            toGenie_leftHand.UpdatePose(fingerBones,handNormal);
                        }
                    }
                    //if is right hand
                    else
                    {
                        if (skeletonVisualizer_rightHand != null && skeletonVisualizer_rightHand.showSkeleton && toGenie_rightHand != null)
                        {
                            // Calculate the Wrist Joint Rotation/ Normal of the hand
                            Vector3 handNormal = CalculateHandNormal(handWorldLandmarks[0], handWorldLandmarks[5], handWorldLandmarks[17]);
                            // Invert the direction of normal of the hand (ONLY Right HAND)
                            // Now it will be the palm, not the back to the hand
                            // handNormal *= -1;

                            // Smooth the normal based on the previous frame of normal
                            if (normal_cache_rightHand.Count != 0)
                            {
                                handNormal = CalculateWeightedAverage(
                                    normal_cache_rightHand[normal_cache_rightHand.Count - 1],
                                    handNormal, 5);
                            }

                            // Smooth the behavior of landmark based on previous frame cache
                            if (landmark_cache_rightHand.Count != 0)
                            {
                                for (int i = 0; i < handWorldLandmarks.Length; i++)
                                {
                                    handWorldLandmarks[i] =
                                    CalculateWeightedAverage(
                                        landmark_cache_rightHand[landmark_cache_rightHand.Count - 1][i],
                                        handWorldLandmarks[i], 20);
                                }
                            }
                            // Calculate vector of every finger bones based on smoothed landmark
                            Vector3[] fingerBones = CalculateFingerBoneVectors(handWorldLandmarks);

                            //Visualize hand skeleton and palm direction
                            skeletonVisualizer_rightHand.UpdatePoseAndNormal(handWorldLandmarks, handNormal);
                            palmVisualizer_rightHand.DrawTriangleUtils(skeletonVisualizer_rightHand.MappingCoordinate(handWorldLandmarks[0]),
                                skeletonVisualizer_rightHand.MappingCoordinate(handWorldLandmarks[5]),
                                skeletonVisualizer_rightHand.MappingCoordinate(handWorldLandmarks[17]));

                            //Cache joint rotations, for smoother output calculation
                            normal_cache_rightHand.Add(handNormal);
                            landmark_cache_rightHand.Add(handWorldLandmarks);

                            toGenie_rightHand.UpdatePose(fingerBones,handNormal);
                        }
                    }
                }

            }

            //Take landmark #0, #5 and #17 to calculate the normal of hand
        }
    }

    void Run()
    {
        //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
        Utils.setDebugMode(true);


        if (string.IsNullOrEmpty(palm_detection_model_filepath))
        {
            Debug.LogError(PALM_DETECTION_MODEL_FILENAME + " is not loaded. Please read ?StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf? to make the necessary setup.");
        }
        else
        {
            palmDetector = new MediaPipePalmDetector(palm_detection_model_filepath, 0.3f, 0.6f);
        }

        if (string.IsNullOrEmpty(handpose_estimation_model_filepath))
        {
            Debug.LogError(HANDPOSE_ESTIMATION_MODEL_FILENAME + " is not loaded. Please read ?StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf? to make the necessary setup.");
        }
        else
        {
            handPoseEstimator = new MediaPipeHandPoseEstimator(handpose_estimation_model_filepath, 0.9f);
        }

        webCamTextureToMatHelper.Initialize();

        normal_cache_leftHand = new CappedList<Vector3>();
        normal_cache_rightHand = new CappedList<Vector3>();
        landmark_cache_leftHand = new CappedList<Vector3[]>();
        landmark_cache_rightHand = new CappedList<Vector3[]>();
        fingerBones_cache_leftHand = new CappedList<Vector3[]>();
        fingerBones_cache_rightHand = new CappedList<Vector3[]>();
    }

    // Raises the webcam texture to mat helper initialized event.
    public void OnWebCamTextureToMatHelperInitialized()
    {
        Debug.Log("OnWebCamTextureToMatHelperInitialized");

        Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

        gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);
        Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

        float width = webCamTextureMat.width();
        float height = webCamTextureMat.height();

        float widthScale = (float)Screen.width / width;
        float heightScale = (float)Screen.height / height;
        if (widthScale < heightScale)
        {
            Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
        }
        else
        {
            Camera.main.orthographicSize = height / 2;
        }

        bgrMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC3);
    }

    // Raises the webcam texture to mat helper disposed event.
    public void OnWebCamTextureToMatHelperDisposed()
    {
        Debug.Log("OnWebCamTextureToMatHelperDisposed");

        if (bgrMat != null)
            bgrMat.Dispose();
    }

    // Raises the webcam texture to mat helper error occurred event.
    // <param name="errorCode">Error code.</param>
    public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
    {
        Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
    }

    // Raises the destroy event.
    void OnDestroy()
    {
        webCamTextureToMatHelper.Dispose();

        if (palmDetector != null)
            palmDetector.dispose();

        if (handPoseEstimator != null)
            handPoseEstimator.dispose();

        Utils.setDebugMode(false);

#if UNITY_WEBGL
            if (getFilePath_Coroutine != null)
            {
                StopCoroutine(getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose();
            }
#endif
    }

    public Vector3[] ConvertMatToVector(Mat result)
    {
        if (result.empty() || result.rows() < 132)
        {
            Debug.Log("Mat result invalid");
            return null;
        }

        if (landmarks_world_buffer == null)
            landmarks_world_buffer = new Vector3[21];

        // Copy only world landmarks data from pose data.
        MatUtils.copyFromMat<Vector3>(result.rowRange(67, 67 + 63), landmarks_world_buffer);

        return landmarks_world_buffer;
    }

    public Vector3[] MoveCoordinateOrigin(Vector3[] landmarks_world)
    {
        // Shift center from the skeleton from palm to wrist
        float x_offset = landmarks_world[0].x;
        float y_offset = landmarks_world[0].y;
        float z_offset = landmarks_world[0].z;
        for (int i = 0; i < landmarks_world.Length; i++)
        {
            float new_x = landmarks_world[i].x - x_offset;
            float new_y = landmarks_world[i].y - y_offset;
            float new_z = landmarks_world[i].z - z_offset;
            landmarks_world[i] = new Vector3(new_x, new_y, new_z);
        }

        return landmarks_world;
    }

    public Vector3 CalculateHandNormal(Vector3 landmark0, Vector3 landmark5, Vector3 landmark17)
    {
        //Vector3 Line1 = landmark5 - landmark0;
        //Vector3 Line2 = landmark17 - landmark0;

        //Vector3 crossProduct = Vector3.Cross(Line1, Line2).normalized;
        //return crossProduct;

        Vector3 line1 = landmark5 - landmark0;
        Vector3 line2 = landmark17 - landmark0;

        // Calculate the average direction vector
        Vector3 averageDirection = (line1 + line2) / 2;
        Vector3 normalizedDirection = averageDirection.normalized;

        return normalizedDirection;
    }

    public Vector3[] CalculateFingerBoneVectors(Vector3[] landmarks_world)
    {
        Vector3[] fingerBoneVectors = new Vector3[20];

        // Thumb Finger
        fingerBoneVectors[0] = landmarks_world[(int)KeyPoint.Thumb1] - landmarks_world[(int)KeyPoint.Wrist];
        fingerBoneVectors[1] = landmarks_world[(int)KeyPoint.Thumb2] - landmarks_world[(int)KeyPoint.Thumb1];
        fingerBoneVectors[2] = landmarks_world[(int)KeyPoint.Thumb3] - landmarks_world[(int)KeyPoint.Thumb2];
        fingerBoneVectors[3] = landmarks_world[(int)KeyPoint.Thumb4] - landmarks_world[(int)KeyPoint.Thumb3];

        // Index Finger
        fingerBoneVectors[4] = landmarks_world[(int)KeyPoint.Index1] - landmarks_world[(int)KeyPoint.Wrist];
        fingerBoneVectors[5] = landmarks_world[(int)KeyPoint.Index2] - landmarks_world[(int)KeyPoint.Index1];
        fingerBoneVectors[6] = landmarks_world[(int)KeyPoint.Index3] - landmarks_world[(int)KeyPoint.Index2];
        fingerBoneVectors[7] = landmarks_world[(int)KeyPoint.Index4] - landmarks_world[(int)KeyPoint.Index3];

        // Middle Finger
        fingerBoneVectors[8] = landmarks_world[(int)KeyPoint.Middle1] - landmarks_world[(int)KeyPoint.Wrist];
        fingerBoneVectors[9] = landmarks_world[(int)KeyPoint.Middle2] - landmarks_world[(int)KeyPoint.Middle1];
        fingerBoneVectors[10] = landmarks_world[(int)KeyPoint.Middle3] - landmarks_world[(int)KeyPoint.Middle2];
        fingerBoneVectors[11] = landmarks_world[(int)KeyPoint.Middle4] - landmarks_world[(int)KeyPoint.Middle3];

        // Ring Finger
        fingerBoneVectors[12] = landmarks_world[(int)KeyPoint.Ring1] - landmarks_world[(int)KeyPoint.Wrist];
        fingerBoneVectors[13] = landmarks_world[(int)KeyPoint.Ring2] - landmarks_world[(int)KeyPoint.Ring1];
        fingerBoneVectors[14] = landmarks_world[(int)KeyPoint.Ring3] - landmarks_world[(int)KeyPoint.Ring2];
        fingerBoneVectors[15] = landmarks_world[(int)KeyPoint.Ring4] - landmarks_world[(int)KeyPoint.Ring3];

        // Pinky Finger
        fingerBoneVectors[16] = landmarks_world[(int)KeyPoint.Pinky1] - landmarks_world[(int)KeyPoint.Wrist];
        fingerBoneVectors[17] = landmarks_world[(int)KeyPoint.Pinky2] - landmarks_world[(int)KeyPoint.Pinky1];
        fingerBoneVectors[18] = landmarks_world[(int)KeyPoint.Pinky3] - landmarks_world[(int)KeyPoint.Pinky2];
        fingerBoneVectors[19] = landmarks_world[(int)KeyPoint.Pinky4] - landmarks_world[(int)KeyPoint.Pinky3];

        return fingerBoneVectors;
    }

    public Vector3 CalculateWeightedAverage(Vector3 a, Vector3 b, float weight)
    {
        return (weight * a + b) / (weight + 1);
    }
}


public class CappedList<T>
{
    private const int MaxSize = 10;
    private List<T> internalList = new List<T>();

    public void Add(T item)
    {
        if (internalList.Count >= MaxSize)
        {
            internalList.RemoveAt(0); // Remove the first element
        }
        internalList.Add(item); // Add the new element at the end
    }

    public void Remove(T item)
    {
        internalList.Remove(item);
    }

    public T this[int index]
    {
        get { return internalList[index]; }
        set { internalList[index] = value; }
    }

    public int Count
    {
        get { return internalList.Count; }
    }

    public void Clear()
    {
        internalList.Clear();
    }

    public bool Contains(T item)
    {
        return internalList.Contains(item);
    }

    public void PrintAllItems()
    {
        foreach (var item in internalList)
        {
            Debug.Log(item);
        }
    }
}
