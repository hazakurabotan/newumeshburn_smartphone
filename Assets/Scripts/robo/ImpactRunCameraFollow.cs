using UnityEngine;

public class ImpactRunCameraFollow : MonoBehaviour
{
    public Transform target;
    [Min(0f)] public float smoothSpeed = 8f;
    public Vector3 offset = new Vector3(2.0f, 0f, 0f);
    public bool followY = false;

    private float fixedY;

    private void Start()
    {
        fixedY = transform.position.y;

        if (target == null)
        {
            ImpactRunnerController runner = FindObjectOfType<ImpactRunnerController>();
            if (runner != null)
                target = runner.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desired = transform.position;
        desired.x = target.position.x + offset.x;
        desired.z = transform.position.z;

        if (followY)
            desired.y = target.position.y + offset.y;
        else
            desired.y = fixedY;

        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }
}