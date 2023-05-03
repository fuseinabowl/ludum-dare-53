using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Pan")]
    public float panSpeed = 10f;

    [Header("Rotate")]
    public float rotationSpeed = 90f;

    [Header("Zoom")]
    public float zoomSpeed = 10f;

    [Range(0f, 1f)]
    public float zoomExp = 1f;

    [Range(0f, 2f)]
    public float zoomSpeedExp = 0f;
    public float minY = 3f;
    public float maxY = 60f;
    public float minXSkew = 40;
    public float maxXSkew = 75;

    [Header("Follow")]
    public Vector3 followPositionOffset = Vector3.zero;

    [Range(0.1f, 1f)]
    public float followSmoothTime = 1f;
    public bool alwaysEnableFollowCamera = false;

    [Range(0f, 90f)]
    public float followXRotation = 45f;

    private Camera cam;
    private Vector3 freePosition;
    private Quaternion freeRotation;
    private Vector3 freeLookTarget;
    private TrainMover followTrain = null;
    private Vector3 followTarget;
    private Vector3 followForwardVelocity;
    private Vector3 followPositionVelocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        freePosition = transform.position;
        freeRotation = transform.rotation;
        SafeApplyCameraZoom(0);
    }

    private void Update()
    {
        if (alwaysEnableFollowCamera || GameController.singleton.gameOver)
        {
            // reward for finishing the game... free camera!
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (!FollowNextTrain())
                {
                    transform.position = freePosition;
                    transform.rotation = freeRotation;
                }
            }
        }
        else
        {
            // just to make sure if a track is deleted, you can't get stuck
            followTrain = null;
        }

        if (followTrain != null)
        {
            UpdateFollowTarget();
        }
    }

    private void LateUpdate()
    {
        if (followTrain != null)
        {
            return;
        }

        var panInput = InputPan();
        if (panInput != Vector3.zero)
        {
            var panDelta = transform.right * (panInput.x * -panSpeed);
            panDelta += transform.up * (panInput.y * -panSpeed);
            panDelta = Vectors.Y(0, panDelta);
            transform.position += panDelta * Time.deltaTime;
        }

        float rotateInput = InputRotation();
        if (rotateInput != 0)
        {
            transform.RotateAround(
                freeLookTarget,
                Vector3.up,
                rotateInput * rotationSpeed * Time.deltaTime
            );
        }

        float zoomInput = InputZoom();
        if (zoomInput != 0)
        {
            SafeApplyCameraZoom(zoomInput * Time.deltaTime);
        }
    }

    private void SafeApplyCameraZoom(float yDiff)
    {
        var adjZoomSpeed = zoomSpeed * Mathf.Pow(HeightPct(transform.position.y) + 1, zoomSpeedExp);
        var y = transform.position.y + yDiff * adjZoomSpeed;
        transform.position = Vectors.Y(Mathf.Clamp(y, minY, maxY), transform.position);
        var xSkew = Mathf.Pow(HeightPct(transform.position.y), zoomExp);
        var rot = Vectors.X(
            Mathf.SmoothStep(minXSkew, maxXSkew, xSkew),
            transform.rotation.eulerAngles
        );
        transform.rotation = Quaternion.Euler(rot);
    }

    private float HeightPct(float y)
    {
        return (y - minY) / (maxY - minY);
    }

    private Vector3 InputPan()
    {
        float x = Input.GetKey(KeyCode.A)
            ? 1
            : Input.GetKey(KeyCode.D)
                ? -1
                : 0;
        float y = Input.GetKey(KeyCode.W)
            ? -1
            : Input.GetKey(KeyCode.S)
                ? 1
                : 0;
        return new Vector3(x, y, 0).normalized;
    }

    private void UpdateLookTarget()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            freeLookTarget = hit.point;
        }
        else
        {
            freeLookTarget = Vector3.zero;
        }
    }

    private float InputRotation()
    {
        return CameraPlayerPrefs.CameraRotationMultiplier
            * (
                Input.GetKey(KeyCode.Q)
                    ? -1
                    : Input.GetKey(KeyCode.E)
                        ? 1
                        : 0
            );
    }

    private float InputZoom()
    {
        return CameraPlayerPrefs.CameraZoomMultiplier
            * (
                Input.GetKey(KeyCode.Z)
                    ? -1
                    : Input.GetKey(KeyCode.C)
                        ? 1
                        : 0
            );
    }

    private bool FollowNextTrain()
    {
        var trains = GameObject.FindObjectsOfType<TrainMover>();
        int idx;

        if (followTrain == null)
        {
            idx = -1;
        }
        else
        {
            idx = 0;
            // find currently running train
            for (; idx < trains.Length && trains[idx] != followTrain; idx++) { }
        }

        // find the next running train after that
        idx++;
        for (; idx < trains.Length && !trains[idx].Running(); idx++) { }

        if (idx >= trains.Length - 1)
        {
            // no trains can be followed, or this was the last train so go back to the free camera
            followTrain = null;
            return false;
        }

        followTrain = trains[idx];
        return true;
    }

    private void UpdateFollowTarget()
    {
        Vector3 targetForward;
        var forwardTrain = followTrain.ForwardLocomitiveController(out targetForward);
        var targetPosition = forwardTrain.transform.position + followPositionOffset;
        transform.forward = Vector3.SmoothDamp(
            transform.forward,
            targetForward,
            ref followForwardVelocity,
            followSmoothTime
        );
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref followPositionVelocity,
            followSmoothTime
        );
    }
}
