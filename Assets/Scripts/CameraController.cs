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
    public float minZoom = 5f;
    public float maxZoom = 50f;
    
    private Camera cam;
    private Vector3 prevPosition;
    private Vector3 lookTarget;

    private void Awake() {
        cam = GetComponent<Camera>();
    }

    private void Start() {
        prevPosition = transform.position;
        UpdateLookTarget();
    }

    private void Update()
    {
        if (prevPosition != transform.position) {
            UpdateLookTarget();
            prevPosition = transform.position;
        }
    }

    private void UpdateLookTarget() {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) {
            lookTarget = hit.point;
        } else {
            lookTarget = Vector3.zero;
        }
    }


    private void LateUpdate() {
        var panInput = InputPan();
        var panDelta = transform.right * (panInput.x * -panSpeed);
        panDelta += transform.up * (panInput.y * -panSpeed);
        transform.position += panDelta * Time.deltaTime;

        float rotateInput = InputRotation();
        transform.RotateAround(lookTarget, Vector3.up, rotateInput * rotationSpeed * Time.deltaTime);

        float zoomInput = InputZoom();
        cam.orthographicSize = Mathf.Clamp(
            minZoom,
            cam.orthographicSize + zoomInput * zoomSpeed * Time.deltaTime,
            maxZoom);
    }

    private Vector3 InputPan() {
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

    private float InputRotation() {
        return Input.GetKey(KeyCode.Q)
            ? -1
            : Input.GetKey(KeyCode.E)
            ? 1
            : 0;
    }

    private float InputZoom() {
        return Input.GetKey(KeyCode.Z)
            ? -1
            : Input.GetKey(KeyCode.C)
            ? 1
            : 0;
    }
}
