using UnityEngine;

public class AddScriptToChildren : MonoBehaviour
{
    public MonoBehaviour scriptToAdd;

    void Start()
    {
        if (scriptToAdd == null)
        {
            return;
        }

        System.Type scriptType = scriptToAdd.GetType();

        Transform[] children = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child == transform) continue;

            if (child.gameObject.GetComponent(scriptType) == null)
            {
                child.gameObject.AddComponent(scriptType);
            }
        }
    }
}
