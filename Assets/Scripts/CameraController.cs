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
    public float minOrthoZoom = 4;
    public float maxOrthoZoom = 10;
    public float minY = 3f;
    public float maxY = 60f;
    public float minXSkew = 40;
    public float maxXSkew = 75;

    [Header("Follow")]
    public Vector3 followPositionOffset = Vector3.zero;

    [Range(0f, 90f)]
    public float followXRotation = 45f;

    private Camera cam;
    private Vector3 prevPosition;
    private Quaternion prevRotation;
    private Vector3 lookTarget;
    private TrainMover followTrain = null;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        prevPosition = transform.position;
        prevRotation = transform.rotation;
        cam.orthographicSize = (minOrthoZoom + maxOrthoZoom) / 2f;
        SafeApplyCameraZoom(transform.position.y);
        UpdateLookTarget();
    }

    private void Update()
    {
        if (GameController.singleton.gameOver)
        {
            // reward for finishing the game... free camera!
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (!FollowNextTrain())
                {
                    transform.position = prevPosition;
                    transform.rotation = prevRotation;
                }
            }
        } else {
            // just to make sure if a track is deleted, you can't get stuck
            followTrain = null;
        }

        if (followTrain != null)
        {
            UpdateFollowTarget();
        }
        else if (prevPosition != transform.position || prevRotation != transform.rotation)
        {
            UpdateLookTarget();
            prevPosition = transform.position;
            prevRotation = transform.rotation;
        }
    }

    private void UpdateLookTarget()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            lookTarget = hit.point;
        }
        else
        {
            lookTarget = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        if (followTrain != null) {
            return;
        }

        var panInput = InputPan();
        var panDelta = transform.right * (panInput.x * -panSpeed);
        panDelta += transform.up * (panInput.y * -panSpeed);
        panDelta = Vectors.Y(0, panDelta);
        transform.position += panDelta * Time.deltaTime;

        float rotateInput = InputRotation();
        transform.RotateAround(
            lookTarget,
            Vector3.up,
            rotateInput * rotationSpeed * Time.deltaTime
        );

        float zoomInput = InputZoom();
        cam.orthographicSize = Mathf.Clamp(
            minOrthoZoom,
            cam.orthographicSize + zoomInput * zoomSpeed * Time.deltaTime,
            maxOrthoZoom
        );

        SafeApplyCameraZoom(transform.position.y + zoomInput * zoomSpeed * Time.deltaTime);
    }

    private void SafeApplyCameraZoom(float y)
    {
        transform.position = Vectors.Y(Mathf.Clamp(y, minY, maxY), transform.position);
        var rot = transform.rotation.eulerAngles;
        rot = Vectors.X(
            Mathf.SmoothStep(minXSkew, maxXSkew, (transform.position.y - minY) / (maxY - minY)),
            rot
        );
        transform.rotation = Quaternion.Euler(rot);
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
        Vector3 forwardTransform;
        var forwardTrain = followTrain.ForwardLocomitiveController(out forwardTransform);
        transform.forward = forwardTransform;
        transform.position = forwardTrain.transform.position + followPositionOffset;
        transform.rotation = Quaternion.Euler(
            followXRotation,
            transform.rotation.eulerAngles.y,
            transform.rotation.eulerAngles.z
        );
    }
}
