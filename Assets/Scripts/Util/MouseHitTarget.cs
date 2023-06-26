using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Generates OnMouseHitDown, OnMouseHitUp, and optionally OnMouseHitHover events.
///   - OnMouseHitDown is sent whenever this collider is clicked on. An OnMouseHitUp event will
///     always eventually be sent.
///   - OnMouseHitUp is generated when the mouse event from OnMouseHitDown is released, regardless
///     of whether the mouse is currently over the object.
///   - OnMouseHitClick is generated after an OnMouseHitUp if there was previously an
///     OnMouseHitDown for that object.
///   - OnMouseHitHover is generated only if hover events are enabled, and is sent whenever the
///     mouse is hovering over this collider.
///   - OnMouseHitHoveStart is generated only if hover events are enabled, and is sent the first
///     time the mouse hovers over the collider since it was last hovering.
///   - OnMouseHitHoverEnd is generated only if hover events are enabled, and is sent after the
///     mouse is no longer hovering over the collider after a series of OnMouseHitHover events.
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

    public interface ButtonHandler
    {
        void OnMouseHitDown(MouseHitTarget.Event e);
        void OnMouseHitUp(MouseHitTarget.Event e);
        void OnMouseHitClick(MouseHitTarget.Event e);
    }

    public interface HoverHandler
    {
        void OnMouseHitHover(MouseHitTarget.Event e);
        void OnMouseHitHoverStart(MouseHitTarget.Event e);
        void OnMouseHitHoverEnd(MouseHitTarget.Event e);
    }

    [System.Serializable]
    public class ClickEvent : UnityEvent<Event> { }

    public struct Modifiers
    {
        public bool shift;
        public bool control;
        public bool alt;
        public bool meta;
    }

    private delegate bool MouseButtonGetter(int button);

    [SerializeField]
    [Tooltip("Filter mouse events to specific collider layers, default Everything")]
    private LayerMask layerMask = -1;

    [SerializeField]
    [Tooltip("(Optional) Collider to trigger events from, if this object doesn't have one")]
    private Collider targetCollider;

    [SerializeField]
    [Tooltip("Continuously calculate hits for hover events")]
    private bool hoverEvents = false;

    [SerializeField]
    [Tooltip("Functions to run when clicked, the argument is a MouseHitTarget.Event")]
    private ClickEvent clicked = new ClickEvent();

    // Share the raycast result across all mouse handlers. There is no need for every handler
    // to recalculate it since it'll be the same every time.
    private static bool didRaycast = false;
    private static RaycastHit[] raycastHits = new RaycastHit[10];
    private static int raycastHitsCount = 0;

    private bool down = false;
    private bool hover = false;

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
        else if (hoverEvents)
        {
            UpdateMouseHover();
        }
    }

    private void UpdateMouseDown()
    {
        UpdateRaycast();

        for (int i = 0; i < raycastHitsCount; i++)
        {
            if (raycastHits[i].collider == targetCollider)
            {
                down = true;

                SendMessage(
                    "OnMouseHitDown",
                    GetEvent(Input.GetMouseButtonDown, raycastHits[i]),
                    SendMessageOptions.DontRequireReceiver
                );
                break;
            }
        }
    }

    private void UpdateMouseUp()
    {
        UpdateRaycast();

        bool wasDown = down;
        down = false;

        var upEvent = GetEvent(Input.GetMouseButtonUp, null);
        SendMessage("OnMouseHitUp", upEvent, SendMessageOptions.DontRequireReceiver);

        if (wasDown)
        {
            SendMessage("OnMouseHitClick", upEvent, SendMessageOptions.DontRequireReceiver);
            clicked.Invoke(upEvent);
        }
    }

    private void UpdateMouseHover()
    {
        UpdateRaycast();

        bool wasHover = hover;
        hover = false;

        for (int i = 0; i < raycastHitsCount; i++)
        {
            if (raycastHits[i].collider.gameObject == gameObject)
            {
                hover = true;
                if (wasHover)
                {
                    SendMessage(
                        "OnMouseHitHover",
                        GetEvent(Input.GetMouseButton, raycastHits[i]),
                        SendMessageOptions.DontRequireReceiver
                    );
                }
                else
                {
                    SendMessage(
                        "OnMouseHitHoverStart",
                        GetEvent(Input.GetMouseButton, raycastHits[i]),
                        SendMessageOptions.DontRequireReceiver
                    );
                }
                break;
            }
        }

        if (!hover && wasHover)
        {
            SendMessage(
                "OnMouseHitHoverEnd",
                GetEvent(Input.GetMouseButton, null),
                SendMessageOptions.DontRequireReceiver
            );
        }
    }

    private void LateUpdate()
    {
        didRaycast = false;
    }

    private void UpdateRaycast()
    {
        if (!didRaycast)
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            raycastHitsCount = Physics.RaycastNonAlloc(
                ray,
                raycastHits,
                float.PositiveInfinity,
                layerMask
            );
            didRaycast = true;
        }
    }

    private Event GetEvent(MouseButtonGetter getter, RaycastHit? raycastHit)
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
