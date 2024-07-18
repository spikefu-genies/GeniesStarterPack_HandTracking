using UnityEngine;

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnityExample.DnnModel;
using OpenCVForUnity.ImgprocModule;
using System.Collections.Generic;
using System.Text;
using System;

[RequireComponent(typeof(WebCamTextureToMatHelper))]
public class MediaPipeHandTracking : MonoBehaviour
{
    public MediaPipeHandPoseSkeletonVisualizer skeletonVisualizer_leftHand;
    public MediaPipeHandPoseSkeletonVisualizer skeletonVisualizer_rightHand;

    public MediaPipeHandToGenieHand toGenie_leftHand;
    public MediaPipeHandToGenieHand toGenie_rightHand;

    private Vector3[] landmarks_world_buffer;
    private CappedList<Vector3[]> landmarks_world_cache_leftHand;
    private CappedList<Vector3[]> landmarks_world_cache_rightHand;

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

                //TickMeter tm = new TickMeter();
                //tm.start();

                Mat palms = palmDetector.infer(bgrMat);

                //tm.stop();
                //Debug.Log("MediaPipePalmDetector Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                List<Mat> hands = new List<Mat>();

                Debug.Log("Camera detected " + palms.rows() + " hands");

                // Estimate the pose of each hand
                for (int i = 0; i < palms.rows(); ++i)
                {
                    //tm.reset();
                    //tm.start();

                    // Handpose estimator inference
                    Mat handpose = handPoseEstimator.infer(bgrMat, palms.row(i));

                    //tm.stop();
                    //Debug.Log("MediaPipeHandPoseEstimator Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                    if (!handpose.empty())
                        hands.Add(handpose);
                }

                Imgproc.cvtColor(bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

                foreach (var hand in hands)
                {
                    handPoseEstimator.visualize(rgbaMat, hand, true, true);
                    var handEstimationData = handPoseEstimator.getData(hand);
                    Vector3[] handWorldLandmarks = ConvertMatToVector(hand);
                    Vector3 handNormal = CalculateHandNormal(handWorldLandmarks[0], handWorldLandmarks[5], handWorldLandmarks[17]);
                    //if is left hand
                    if (handEstimationData.handedness <= 0.5f)
                    {
                        if (skeletonVisualizer_leftHand != null && skeletonVisualizer_leftHand.showSkeleton && toGenie_leftHand != null)
                        {
                            // Invert the direction of normal to inside of the hand
                            handNormal *= -1;
                            skeletonVisualizer_leftHand.UpdatePoseAndNormal(handWorldLandmarks, handNormal);
                            // CacheLandmarkInfo(bool isLefthand,Vector3[] landmarks_world_buffer);
                            landmarks_world_cache_leftHand.Add(handWorldLandmarks);
                            //toGenie_leftHand.UpdatePose(handWorldLandmarks);

                        }
                    }
                    //if is right hand
                    else
                    {
                        if (skeletonVisualizer_rightHand != null && skeletonVisualizer_rightHand.showSkeleton && toGenie_rightHand != null)
                        {
                            skeletonVisualizer_rightHand.UpdatePoseAndNormal(handWorldLandmarks, handNormal);
                            // CacheLandmarkInfo(bool isLefthand,Vector3[] landmarks_world_buffer);
                            landmarks_world_cache_rightHand.Add(handWorldLandmarks);
                            //toGenie_rightHand.UpdatePose(handWorldLandmarks);
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

        landmarks_world_cache_leftHand = new CappedList<Vector3[]>();
        landmarks_world_cache_rightHand = new CappedList<Vector3[]>();
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

    public Vector3 CalculateHandNormal(Vector3 landmark0, Vector3 landmark5, Vector3 landmark17)
    {
        Vector3 Line1 = landmark5 - landmark0;
        Vector3 Line2 = landmark17 - landmark0;

        Vector3 crossProduct = Vector3.Cross(Line1, Line2).normalized;
        return crossProduct;
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
