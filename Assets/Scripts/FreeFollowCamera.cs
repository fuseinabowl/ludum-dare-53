using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeFollowCamera : MonoBehaviour
{
    [Header("Free")]
    [SerializeField]
    private float panSpeed = 10f;

    [SerializeField]
    private float panSpeedExp = 0f;

    [SerializeField]
    private float rotationSpeed = 90f;

    [SerializeField]
    private float zoomSpeed = 10f;

    [SerializeField]
    private float zoomExp = 1f;

    [SerializeField]
    private float zoomSpeedExp = 0f;

    [SerializeField]
    private float minY = 3f;

    [SerializeField]
    private float maxY = 60f;

    [SerializeField]
    private float minXSkew = 40;

    [SerializeField]
    private float maxXSkew = 75;

    [Header("Follow")]
    public bool enableFollowCamera = false;

    [SerializeField]
    private Vector3 followPositionOffset = Vector3.zero;

    [Range(0.1f, 1f)]
    [SerializeField]
    private float followSmoothTime = 1f;

    [SerializeField]
    private float followMaxXSkew;

    [Range(0f, 0.01f)]
    [SerializeField]
    private float followFreeTransitionEpsilon = 0.01f;

    [Header("Mouse")]
    [Range(0f, 1f)]
    [SerializeField]
    private float mouseJumpSmoothTime = 0.1f;

    [SerializeField]
    private float mouseZoomFactor = 1f;

    [SerializeField]
    private float mouseRotateFactor = 1f;

    private Camera cam;

    private Vector3 freeLookAt;
    private Vector3 freeTargetPosition;
    private Vector3 freeTargetForward;
    private bool freeJump;

    private List<GameObject> followTargets = new List<GameObject>();
    private Dictionary<GameObject, Pose> followTargetPoses = new Dictionary<GameObject, Pose>();
    private GameObject followTarget;
    private Vector3 followForwardVelocity;
    private bool followForwardIsTransition;
    private Vector3 followPositionVelocity;
    private bool followPositionIsTransition;

    public void AddFollow(GameObject ft, Pose pose = default(Pose))
    {
        if (!followTargets.Contains(ft))
        {
            followTargets.Add(ft);
        }
        followTargetPoses[ft] = pose;
    }

    public void RemoveFollow(GameObject ft)
    {
        if (followTargets.Remove(ft) && ft == followTarget)
        {
            FollowNextTarget();
        }
        followTargetPoses.Remove(ft);
    }

    public void SetFollowPose(GameObject ft, Pose pose)
    {
        followTargetPoses[ft] = pose;
    }

    public float HeightFactor()
    {
        return (transform.position.y - minY) / (maxY - minY);
    }

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
        bool wasFollowing = followTarget != null;

        if (enableFollowCamera)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                FollowNextTarget();
            }
        }
        else
        {
            followTarget = null;
        }

        if (!followTarget)
        {
            if (wasFollowing)
            {
                SwitchCameraType(true, false);
            }
            UpdateFreeLookTarget();
            UpdateFreeMouseControls();
        }
        else if (followTarget && !wasFollowing)
        {
            SwitchCameraType(false, false);
        }
    }

    private void SwitchCameraType(bool free, bool jump)
    {
        if (free)
        {
            followForwardIsTransition = true;
            followPositionIsTransition = true;
            freeJump = jump;
        }
        else
        {
            freeTargetPosition = transform.position;
            freeTargetForward = transform.forward;
            followForwardVelocity = Vector3.zero;
            followPositionVelocity = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        var panInput = InputPan().normalized * Time.deltaTime;
        var rotateInput = InputRotation() * Time.deltaTime;
        var zoomInput = InputZoom() * Time.deltaTime;

        if (followTarget != null)
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
                FreeSmoothTime()
            );
            followPositionIsTransition =
                followPositionVelocity.magnitude > followFreeTransitionEpsilon;
        }
        else
        {
            followPositionIsTransition = false;
            var panSpeedForHeight =
                panSpeed * Mathf.Pow(Mathf.Lerp(0.5f, 1f, HeightFactor()), panSpeedExp);
            var panDelta = transform.right * (panInput.x * -panSpeedForHeight);
            panDelta += transform.up * (panInput.y * panSpeedForHeight);
            panDelta = Vectors.Y(0, panDelta);
            transform.position += panDelta;
        }

        if (followForwardIsTransition && rotateInput == 0 && zoomInput == 0)
        {
            transform.forward = Vector3.SmoothDamp(
                transform.forward,
                freeTargetForward,
                ref followForwardVelocity,
                FreeSmoothTime()
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
        var adjZoomSpeed = zoomSpeed * Mathf.Pow(HeightFactor() + 1, zoomSpeedExp);
        var y = transform.position.y + yDiff * adjZoomSpeed;
        transform.position = Vectors.Y(Mathf.Clamp(y, minY, maxY), transform.position);
        var xSkew = Mathf.Pow(HeightFactor(), zoomExp);
        var rot = Vectors.X(
            Mathf.SmoothStep(minXSkew, maxXSkew, xSkew),
            transform.rotation.eulerAngles
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

    private void UpdateFreeMouseControls()
    {
        if (Input.GetMouseButtonDown(1))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                freeTargetPosition = transform.position + hit.point - freeLookAt;
                freeTargetForward = transform.forward;
                SwitchCameraType(true, true);
            }
        }
    }

    private float InputRotation()
    {
        var mouseRotate = ShiftKey() ? mouseRotateFactor * Input.mouseScrollDelta.y : 0f;
        return CameraPlayerPrefs.CameraRotationMultiplier
            * (
                Input.GetKey(KeyCode.Q)
                    ? -1
                    : Input.GetKey(KeyCode.E)
                        ? 1
                        : mouseRotate
            );
    }

    private float InputZoom()
    {
        var mouseZoom = ShiftKey() ? 0f : mouseZoomFactor * Input.mouseScrollDelta.y;
        return CameraPlayerPrefs.CameraZoomMultiplier
            * (
                Input.GetKey(KeyCode.Z)
                    ? -1
                    : Input.GetKey(KeyCode.C)
                        ? 1
                        : -mouseZoom
            );
    }

    private bool ShiftKey()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    private bool FollowNextTarget()
    {
        if (followTargets.Count == 0)
        {
            followTarget = null;
            return false;
        }

        if (followTarget == null)
        {
            followTarget = followTargets[0];
            return true;
        }

        // Find the currently following train and move to the next one.
        int idx = 0;
        for (; idx < followTargets.Count && followTargets[idx] != followTarget; idx++) { }
        idx++;

        if (idx >= followTargets.Count)
        {
            // no trains can be followed, or this was the last train so go back to the free camera
            followTarget = null;
            return false;
        }

        followTarget = followTargets[idx];
        return true;
    }

    private void UpdateFollowCamera(Vector3 panInput)
    {
        Pose pose;
        if (!followTargetPoses.TryGetValue(followTarget, out pose))
        {
            pose = Poses.FromTransform(followTarget.transform);
        }

        var targetPosition = pose.position + followPositionOffset;
        var targetForward = pose.forward;

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

    private float FreeSmoothTime()
    {
        if (freeJump)
        {
            return mouseJumpSmoothTime;
        }
        return followSmoothTime;
    }
}
