using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.ParticleSystem;

public class Foosteps : MonoBehaviour
{
    ParticleSystem ps;
    // Start is called before the first frame update
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        var p = new ParticleSystem.Particle();
        SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
        //smr.
    }

    private void OnTriggerEnter(Collider other)
    {
        print("Collision");
        ps.Emit(1);
    }

}
