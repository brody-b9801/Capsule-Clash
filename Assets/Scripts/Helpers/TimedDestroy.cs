using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedDestroy : MonoBehaviour
{
    void Start()
    {
       StartCoroutine(wholePointOfThisScript()); 
    }
    public IEnumerator wholePointOfThisScript() {
        yield return new WaitForSeconds(1f);
        Destroy(transform.gameObject);
    }
}
