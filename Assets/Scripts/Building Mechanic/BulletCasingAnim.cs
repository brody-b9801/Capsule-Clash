using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletCasingAnim : MonoBehaviour
{
    public System.Action OnReturnToPool;

    public void EndAnim()
    {
        if (OnReturnToPool != null)
        {
            OnReturnToPool.Invoke();
            OnReturnToPool = null;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}