using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates OnMouseHitDown, OnMouseHitUp, and optionally OnMouseHitHover events.
///   - OnMouseHitDown is sent whenever this collider is clicked on. An OnMouseHitUp event will
///     always eventually be sent.
///   - OnMouseHitUp is generated when the mouse event from OnMouseHitDown is released, regardless
///     of whether the mouse is currently over the object.
///   - OnMouseHitHover is generated only if hover events are enabled, and is sent whenever the
///     mouse is hovering over this collider.
/// </summary>
public class MouseHitTarget : MonoBehaviour
{
    public struct Event
    {
        public Modifiers modifiers;
        public bool left;
        public bool right;
        public bool middle;
        public System.Nullable<RaycastHit> raycastHit;
    }

    public struct Modifiers
    {
        public bool shift;
        public bool control;
        public bool alt;
        public bool meta;
    }

    private delegate bool MouseButtonGetter(int button);

    [SerializeField]
    private Collider targetCollider;

    [SerializeField]
    [Tooltip("Calculate hits while hovering (expensive!)")]
    private bool hoverEvents = false;

    // Share the raycast result across all mouse handlers. There is no need for every handler
    // to recalculate it since it'll be the same every time.
    private static bool didRaycast = false;
    private static System.Nullable<RaycastHit> raycastHit;

    private bool down = false;

    private void Awake()
    {
        if (targetCollider == null)
        {
            if (!TryGetComponent(out targetCollider))
            {
                Debug.LogWarning(
                    "MouseHitTarget must either have a targetCollider or be on an object with a collider"
                );
            }
        }
    }

    private void Update()
    {
        if (!targetCollider)
        {
            return;
        }

        if (
            Input.GetMouseButtonDown(0)
            || Input.GetMouseButtonDown(1)
            || Input.GetMouseButtonDown(2)
        )
        {
            UpdateMouseDown();
        }
        else if (
            down
            && (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
        )
        {
            UpdateMouseUp();
        }
        else if (
            hoverEvents
            && (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
        )
        {
            UpdateMouseHover();
        }
    }

    private void UpdateMouseDown()
    {
        UpdateRaycast();

        if (raycastHit?.collider == targetCollider)
        {
            down = true;

            SendMessage(
                "OnMouseHitDown",
                GetEvent(Input.GetMouseButtonDown),
                SendMessageOptions.DontRequireReceiver
            );
        }
    }

    private void UpdateMouseUp()
    {
        UpdateRaycast();

        down = false;

        SendMessage(
            "OnMouseHitUp",
            GetEvent(Input.GetMouseButtonUp),
            SendMessageOptions.DontRequireReceiver
        );
    }

    private void UpdateMouseHover()
    {
        if (!hoverEvents)
        {
            return;
        }

        UpdateRaycast();

        if (raycastHit?.collider.gameObject == gameObject)
        {
            SendMessage(
                "OnMouseHitHover",
                GetEvent(Input.GetMouseButton),
                SendMessageOptions.DontRequireReceiver
            );
        }
    }

    private void LateUpdate()
    {
        didRaycast = false;
        raycastHit = null;
    }

    private void UpdateRaycast()
    {
        if (!didRaycast)
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                raycastHit = hit;
            }
            didRaycast = true;
        }
    }

    private Event GetEvent(MouseButtonGetter getter)
    {
        var e = new Event();
        e.modifiers = new Modifiers
        {
            shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
            control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
            alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt),
            meta = Input.GetKey(KeyCode.LeftMeta) || Input.GetKey(KeyCode.RightMeta),
        };
        e.left = getter(0);
        e.right = getter(1);
        e.middle = getter(2);
        e.raycastHit = raycastHit;
        return e;
    }
}
