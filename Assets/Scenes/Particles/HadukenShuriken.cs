using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HadukenShuriken : MonoBehaviour
{
    public Transform handLeft, handRight;
    public float size, sensitivity, minDistance, maxDistance;
    public float upForceDissolve;
    ParticleSystem ps;
    float emissionRate, upForce;
    public bool isActive;
    // Start is called before the first frame update
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        emissionRate = ps.emission.rateOverTime.constant;
        upForce = ps.forceOverLifetime.y.constant;
    }

    // Update is called once per frame
    void Update()
    {
        float distance = (handLeft.position - handRight.position).magnitude;

        if (distance > minDistance && distance < maxDistance)
        {
            play(distance);
        }
        else if (isActive)
            dissolve();
    }

    void play(float distance)
    {
        if (!isActive)
            createHaduken();
        
        Vector3 middle = (handLeft.position + handRight.position) / 2f;
        transform.position = middle;
        float currentSize = distance;// * size;
        float evolution = Time.deltaTime * sensitivity;
        float scale = Mathf.Lerp(size, currentSize, evolution);
        transform.localScale = new Vector3(scale, scale, scale);
    }

    void createHaduken()
    {
        var emission = ps.emission;
        emission.rateOverTime = emissionRate;
        var fo = ps.forceOverLifetime;
        fo.y = upForce;
        ps.Play();
        isActive = true;
    }

    void dissolve()
    {
        var emission = ps.emission;
        var fo = ps.forceOverLifetime;
        fo.y = upForceDissolve;

        emission.rateOverTime = 0;
        if (ps.particleCount == 0)
        {
            isActive = false;
            ps.Stop();
        }
    }
}
