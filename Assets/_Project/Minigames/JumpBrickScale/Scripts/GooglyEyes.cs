using UnityEngine;

public class GooglyEyes : MonoBehaviour
{
    public Transform eyeBackground;
    public Transform pupil;
    public Transform reflection;

    public float maxRadius = 0.2f;
    public float movementMultiplier = 25f;
    public float springForce = 150f;
    public float damping = 10f;
    public float reflectionParallax = 0.3f;

    private Vector3 lastWorldPos;
    private Vector3 pupilVelocity;
    private Vector3 currentPupilPos;

    private void Start()
    {
        lastWorldPos = transform.position;
        currentPupilPos = Vector3.zero;
    }

    private void LateUpdate()
    {
        Vector3 worldDelta = transform.position - lastWorldPos;
        lastWorldPos = transform.position;

        Vector3 localDelta = transform.InverseTransformDirection(worldDelta);
        Vector3 appliedForce = -localDelta * movementMultiplier;

        Vector3 spring = -currentPupilPos * springForce;
        
        pupilVelocity += (appliedForce + spring) * Time.deltaTime;
        pupilVelocity -= pupilVelocity * damping * Time.deltaTime;

        currentPupilPos += pupilVelocity * Time.deltaTime;
        currentPupilPos.z = 0; 

        currentPupilPos = Vector3.ClampMagnitude(currentPupilPos, maxRadius);

        pupil.localPosition = new Vector3(currentPupilPos.x, currentPupilPos.y, pupil.localPosition.z);
        
        if (reflection != null)
        {
            reflection.localPosition = new Vector3(currentPupilPos.x * reflectionParallax, currentPupilPos.y * reflectionParallax, reflection.localPosition.z);
        }
    }
}