using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Free")]
    public float panSpeed = 10f;

    public float rotationSpeed = 90f;
    public float zoomSpeed = 10f;
    public float zoomExp = 1f;
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
    public float followMaxXSkew;

    [Range(0f, 90f)]
    public float followXRotation = 45f;

    [Range(0f, 0.01f)]
    public float followFreeTransitionEpsilon = 0.01f;

    private Camera cam;

    private Vector3 freeLookAt;
    private Vector3 freeTargetPosition;
    private Vector3 freeTargetForward;

    private TrainMover followTrain;
    private Vector3 followTarget;
    private Vector3 followForwardVelocity;
    private bool followForwardIsTransition;
    private Vector3 followPositionVelocity;
    private bool followPositionIsTransition;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        freeTargetPosition = transform.position;
        freeTargetForward = transform.forward;
        UpdateFreeCameraZoom(0);
    }

    private void Update()
    {
        bool wasFollowingTrain = followTrain != null;

        if (alwaysEnableFollowCamera || GameController.singleton.gameOver)
        {
            // Reward for finishing the game... free camera!
            if (Input.GetKeyDown(KeyCode.Space))
            {
                FollowNextTrain();
            }
        }
        else
        {
            // Make sure if a track is deleted that you can't get stuck.
            followTrain = null;
        }

        if (!followTrain)
        {
            if (wasFollowingTrain)
            {
                SetIsFreeTransition(true);
            }
            UpdateFreeLookTarget();
        }
        else if (followTrain && !wasFollowingTrain)
        {
            SetIsFreeTransition(false);
        }
    }

    private void SetIsFreeTransition(bool ft)
    {
        followForwardVelocity = Vector3.zero;
        followForwardIsTransition = ft;
        followPositionVelocity = Vector3.zero;
        followPositionIsTransition = ft;
    }

    private void LateUpdate()
    {
        var panInput = InputPan().normalized * Time.deltaTime;
        var rotateInput = InputRotation() * Time.deltaTime;
        var zoomInput = InputZoom() * Time.deltaTime;

        if (followTrain != null)
        {
            UpdateFollowCamera(panInput);
        }
        else
        {
            UpdateFreeCamera(panInput, rotateInput, zoomInput);
        }
    }

    private void UpdateFreeCamera(Vector3 panInput, float rotateInput, float zoomInput)
    {
        if (followPositionIsTransition && panInput.magnitude == 0)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                freeTargetPosition,
                ref followPositionVelocity,
                followSmoothTime
            );
            followPositionIsTransition =
                followPositionVelocity.magnitude > followFreeTransitionEpsilon;
        }
        else
        {
            followPositionIsTransition = false;
            var panDelta = transform.right * (panInput.x * -panSpeed);
            panDelta += transform.up * (panInput.y * panSpeed);
            panDelta = Vectors.Y(0, panDelta);
            transform.position += panDelta;
        }

        if (followForwardIsTransition && rotateInput == 0 && zoomInput == 0)
        {
            transform.forward = Vector3.SmoothDamp(
                transform.forward,
                freeTargetForward,
                ref followForwardVelocity,
                followSmoothTime
            );
            followForwardIsTransition =
                followForwardVelocity.magnitude > followFreeTransitionEpsilon;
        }
        else
        {
            followForwardIsTransition = false;
            if (rotateInput != 0)
            {
                transform.RotateAround(freeLookAt, Vector3.up, rotateInput * rotationSpeed);
            }
            if (zoomInput != 0)
            {
                UpdateFreeCameraZoom(zoomInput);
            }
        }
    }

    private void UpdateFreeCameraZoom(float yDiff)
    {
        var adjZoomSpeed =
            zoomSpeed * Mathf.Pow(HeightFactor() + 1, zoomSpeedExp);
        var y = transform.position.y + yDiff * adjZoomSpeed;
        transform.position = Vectors.Y(Mathf.Clamp(y, minY, maxY), transform.position);
        var xSkew = Mathf.Pow(HeightFactor(), zoomExp);
        var rot = Vectors.X(
            Mathf.SmoothStep(minXSkew, maxXSkew, xSkew),
            transform.rotation.eulerAngles
        );
        transform.rotation = Quaternion.Euler(rot);
    }

    

    public float HeightFactor()
    {
        return (transform.position.y - minY) / (maxY - minY);
    }

    private Vector3 InputPan()
    {
        float x = Input.GetKey(KeyCode.A)
            ? 1
            : Input.GetKey(KeyCode.D)
                ? -1
                : 0;
        float y = Input.GetKey(KeyCode.W)
            ? 1
            : Input.GetKey(KeyCode.S)
                ? -1
                : 0;
        return new Vector3(x, y, 0);
    }

    private void UpdateFreeLookTarget()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            freeLookAt = hit.point;
        }
        else
        {
            freeLookAt = Vectors.Y(minY, transform.position);
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

    private void UpdateFollowCamera(Vector3 panInput)
    {
        Vector3 targetForward;
        var forwardTrain = followTrain.ForwardLocomitiveController(out targetForward);
        var targetPosition = forwardTrain.transform.position + followPositionOffset;

        float followXSkew = 0;
        if (panInput.x != 0)
        {
            followXSkew = panInput.x < 0 ? followMaxXSkew : -followMaxXSkew;
            targetForward = Quaternion.Euler(0, followXSkew, 0) * targetForward;
        }

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
