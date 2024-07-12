using UnityEngine;
using UnityEngine.UI;

public class CameraInputController : MonoBehaviour
{
    public Toggle handTrackingToggle;

    private WebCamTexture webcamTexture;
    private GameObject handTrackingManager;

    private void Start()
    { 
        // Get available devices
        //WebCamDevice[] devices = WebCamTexture.devices;

        // Check if there are at least one device
        //if (devices.Length >= 1)
        //{
        //    // Initialize and start the first webcam texture
        //    // Still need to test out if iOS devices[0] is front facing camera
        //    webcamTexture = new WebCamTexture(devices[0].name);
        //    webcamTexture.Play();
        //}
        //else
        //{
        //    Debug.LogError("Not enough webcam devices found.");
        //}


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
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
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
