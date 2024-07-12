using UnityEngine;
using UnityEngine.UI;

public class DualWebcamDisplay : MonoBehaviour
{
    public RawImage rawImage1;
    public RawImage rawImage2;

    private WebCamTexture webcamTexture1;
    private WebCamTexture webcamTexture2;

    private void Start()
    {
        // Get available devices
        WebCamDevice[] devices = WebCamTexture.devices;

        // Check if there are at least two devices
        if (devices.Length >= 2)
        {
            // Initialize and start the first webcam texture
            webcamTexture1 = new WebCamTexture(devices[0].name);
            rawImage1.texture = webcamTexture1;
            webcamTexture1.Play();

            // Initialize and start the second webcam texture
            webcamTexture2 = new WebCamTexture(devices[1].name);
            rawImage2.texture = webcamTexture2;
            webcamTexture2.Play();
        }
        else
        {
            Debug.LogError("Not enough webcam devices found.");
        }
    }

    private void OnDestroy()
    {
        // Stop the webcam textures when the object is destroyed
        if (webcamTexture1 != null && webcamTexture1.isPlaying)
        {
            webcamTexture1.Stop();
        }

        if (webcamTexture2 != null && webcamTexture2.isPlaying)
        {
            webcamTexture2.Stop();
        }
    }
}
