using UnityEngine;

public class VelocityLookTarget : MonoBehaviour
{
    [Header( "Target Settings" )]
    public Transform car;
    public Rigidbody carRb;

    public float lookAheadDistance = 0f;
    public float smoothSpeed = 3f;

    [Range( 0f, 1f )]
    public float velocityBlend = 0.7f;

    private Vector3 smoothedPosition;

    private void Start()
    {
        smoothedPosition = car.position + car.forward * lookAheadDistance;
    }

    private void Update()
    {
        Vector3 velocityDir = carRb.linearVelocity.magnitude > 1f ? carRb.linearVelocity.normalized : car.forward;

        Vector3 blendedDir = Vector3.Lerp( car.forward, velocityDir, this.velocityBlend );

        Vector3 targetPosition = car.position + blendedDir * lookAheadDistance;

        smoothedPosition = Vector3.Lerp( smoothedPosition, targetPosition, Time.deltaTime * smoothSpeed );
        
        this.transform.position = car.position + ( smoothedPosition - car.position ).normalized * lookAheadDistance;
    }
}