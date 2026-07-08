using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Alteruna;

public class BuildingManager : MonoBehaviour
{
    private int keyPressCount = 0;
    private int buildHitCount = 0;
    public GameObject cube;
    private Alteruna.Avatar avatar;

    void Start() {
        avatar = GetComponent<Alteruna.Avatar>();
    }
    void Update()
    {
        if (avatar.IsOwner) {
            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.C))
            {
                keyPressCount++;

                if (keyPressCount == 5)
                {
                    if (cube != null)
                    {
                        Destroy(cube);
                    }
                }
            }
        }
    }
    public void IncrementBuildHitCount()
    {
        //if (avatar.IsOwner) {
            buildHitCount++;
            if (buildHitCount == 3) {
                Destroy(cube);
            }
        //}
    }
}
