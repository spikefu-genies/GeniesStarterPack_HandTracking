using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Haduken : MonoBehaviour
{
    public float minDistance = 0.24f;
    public Transform rightHand, leftHand;
    LineRenderer lr;
    // Start is called before the first frame update
    void Start()
    {
        lr = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        float distance = getDistanceBetweenHands();
        if (distance < minDistance)
        {
            lr.SetPosition(0, leftHand.position);
            lr.SetPosition(1, rightHand.position);
            lr.enabled = true;
        }
        else
            lr.enabled = false;

        if (Input.GetKey(KeyCode.Q)) { print(distance); }
    }

    float getDistanceBetweenHands()
    {
        Vector3 pos1 = rightHand.position;
        Vector3 pos2 = leftHand.position;
        return (pos1 - pos2).magnitude;
    }
}
