using UnityEngine;

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnityExample.DnnModel;
using OpenCVForUnity.ImgprocModule;
using System.Collections.Generic;
using System.Text;


[RequireComponent(typeof(WebCamTextureToMatHelper))]
public class MediaPipeHandTracking : MonoBehaviour
{
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

                //palmDetector.visualize(rgbaMat, palms, false, true);
                foreach (var hand in hands)
                {
                    handPoseEstimator.visualize(rgbaMat, hand, true, true);
                }
    
            }

            //Utils.matToTexture2D(rgbaMat, texture);
        }
    }

    void Run()
    {
        //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
        Utils.setDebugMode(true);


        if (string.IsNullOrEmpty(palm_detection_model_filepath))
        {
            Debug.LogError(PALM_DETECTION_MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
        }
        else
        {
            palmDetector = new MediaPipePalmDetector(palm_detection_model_filepath, 0.3f, 0.6f);
        }

        if (string.IsNullOrEmpty(handpose_estimation_model_filepath))
        {
            Debug.LogError(HANDPOSE_ESTIMATION_MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
        }
        else
        {
            handPoseEstimator = new MediaPipeHandPoseEstimator(handpose_estimation_model_filepath, 0.9f);
        }

        webCamTextureToMatHelper.Initialize();
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

}
