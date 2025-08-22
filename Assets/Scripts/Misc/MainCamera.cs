using UnityEngine;

public class Basic2DCamera : MonoBehaviour
{
    public Transform Target;

    public Vector3 adjustmemt, workingOffset;

    public float SmoothTime = 0.3f;

    private Vector3 velocity = Vector3.zero;

    private void Start()
    {
        workingOffset = transform.position - Target.position; // temp
        workingOffset = new Vector3(workingOffset.x, workingOffset.y - 1, workingOffset.z);
    }

    private void FixedUpdate()
    {
        Vector3 targetPosition = Target.position + workingOffset;

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, SmoothTime);
    }
}
