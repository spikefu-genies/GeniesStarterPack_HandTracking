using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class ParticleDrawing : MonoBehaviour
{
    public AnimationCurve noiseCurve;
    ParticleSystem ps;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        NoiseModule noise = ps.noise;
        noise.remapEnabled = true;
        noise.remap = new MinMaxCurve(1f, noiseCurve);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
