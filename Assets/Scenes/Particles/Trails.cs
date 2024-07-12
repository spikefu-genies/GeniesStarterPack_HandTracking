using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trails : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject trailPrefab;
    public Transform[] trailedBones;
    TrailRenderer[] trails;
    void Start()
    {
        int length  = trailedBones.Length;
        for (int i = 0; i < length; i++)
        {
            Transform tb = trailedBones[i];
            GameObject trail = Instantiate(trailPrefab, tb);

        }
        trails = GetComponentsInChildren<TrailRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
