using UnityEngine;

public class CollisionControl : MonoBehaviour
{
    public GameObject bulletOne;
    public GameObject Visual;
    [SerializeField] private float showDistance = 0.5f;

    private Vector3 bulletStartPos;
    private bool visualEnabled = false;
    private bool rotationApplied = false;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        bulletStartPos = transform.position;
        Visual.SetActive(false);
        bulletOne.SetActive(false);
    }

    void Update()
    {
        if (!rotationApplied && rb != null && rb.linearVelocity != Vector3.zero)
        {
            rb.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
            rotationApplied = true;
        }

        if (visualEnabled) return;
        if ((transform.position - bulletStartPos).magnitude < showDistance) return;

        visualEnabled = true;
        Visual.SetActive(true);
        bulletOne.SetActive(true);
    }
}
