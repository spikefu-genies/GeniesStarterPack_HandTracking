using UnityEngine;
using UnityEngine.UI;

public class CameraInputController : MonoBehaviour
{
    public Toggle handTrackingToggle;


    //Render webcam footage as RGB texture on screen
    //for De-bug purpose
    // NEED TO TAKE OUT FROM PRODUCTION RELEASE
    //private WebCamTexture webcamTexture;
    //public RawImage webcamImage;

    private GameObject handTrackingManager;

    private void Start()
    { 
        //find HandTrackingManager child object under WebcamManager
        string childName = "HandTrackingManager";
        Transform childTransform = transform.Find(childName);
        handTrackingManager = childTransform.gameObject;
        //Set HandTrackingManager active if Hand Tracking is toggled on
        if (handTrackingManager != null && handTrackingToggle.isOn)
        {
            handTrackingManager.SetActive(true);
        }else if(handTrackingManager != null)
        {
            handTrackingManager.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Stop the webcam textures when the object is destroyed
        //if (webcamTexture != null && webcamTexture.isPlaying)
        //{
        //    webcamTexture.Stop();
        //}
    }

    private void onHandTrackingToggleClicked()
    {
        if (handTrackingManager != null && handTrackingToggle.isOn)
        {
            handTrackingManager.SetActive(true);
        }else if (handTrackingManager != null && !handTrackingToggle.isOn)
        {
            handTrackingManager.SetActive(false);
        }
    }

      
}
