using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fireball : MonoBehaviour
{
    public Transform handLeft, handRight;
    public GameObject particleSystems;
    public float minDistance, maxDistance;
    public float size, sensitivity;
    public float dbg_distance, dbg_size;
    float fireballEvolution, previousSize;
    Vector3 previousPosition;

    void Start()
    {
        transform.position = (handLeft.position + handRight.position) / 2f;
    }

    // Update is called once per frame
    void Update()
    {
        
        float distance = (handLeft.position -  handRight.position).magnitude;
        dbg_distance = distance;
        if (distance > minDistance && distance < maxDistance)
            createFireball(distance);
        else
            disableFireball();
           
    }


    void createFireball(float distance)
    {
        Vector3 middle = (handLeft.position + handRight.position) / 2f;
        if (!particleSystems.activeInHierarchy)
        {
            particleSystems.SetActive(true);
            previousSize = distance * size;
            previousPosition = middle;
            fireballEvolution = 0;
        }
        float currentSize = distance * size;
        float evolution = Time.deltaTime * sensitivity;
        float scale = Mathf.Lerp(size, currentSize, evolution);
        transform.localScale = new Vector3(scale, scale, scale);
        Vector3 position = Vector3.Lerp(transform.position, middle, evolution);
        transform.position = position;
        previousPosition = position;
        previousSize = currentSize;
        dbg_size = currentSize;
    }
    
    void disableFireball()
    {
        particleSystems.SetActive(false);
        fireballEvolution = 0;
        previousSize = 0;
    }

}
