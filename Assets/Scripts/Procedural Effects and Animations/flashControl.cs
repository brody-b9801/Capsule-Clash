using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class flashControl : MonoBehaviour
{
    private ParticleSystem ps;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        StartCoroutine(pause());
    }

    IEnumerator pause() {
            yield return new WaitForSeconds(0.6f);
            ps.Pause();
    }
}
