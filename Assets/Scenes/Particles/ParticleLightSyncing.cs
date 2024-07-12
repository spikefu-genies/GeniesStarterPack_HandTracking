using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class ParticleLightSyncing : MonoBehaviour
{
    public float intensityMultiplier = 1;
    Light lamp;
    ParticleSystem ps;
    Vector3 previousPosition;
    // Start is called before the first frame update
    void Start()
    {
        lamp = GetComponent<Light>();
        ps = GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        
        float distance = (transform.position - previousPosition).magnitude;
        //lamp.intensity = distance * intensityMultiplier;
        lamp.intensity = ps.particleCount / 3000f * intensityMultiplier;
        previousPosition = transform.position;
    }
}
