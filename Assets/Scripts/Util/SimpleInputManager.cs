using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleInputManager : MonoBehaviour
{
    public struct Modifiers
    {
        public bool shift;
        public bool control;
        public bool alt;
        public bool meta;
    }

    public struct MouseEvent
    {
        public Modifiers modifiers;
        public bool left;
        public bool right;
        public bool middle;
        public System.Nullable<RaycastHit> raycastHit;
    }

    private delegate bool MouseButtonGetter(int button);

    [SerializeField]
    [Tooltip("Raycast mouse up/down events (not move)")]
    private bool mouseRaycast;

    private void Update()
    {
        var modifiers = new Modifiers
        {
            shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
            control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
            alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt),
            meta = Input.GetKey(KeyCode.LeftMeta) || Input.GetKey(KeyCode.RightMeta),
        };

        var events = new MouseEvent[]
        {
            new MouseEvent { modifiers = modifiers }, // button down
            new MouseEvent { modifiers = modifiers }, // button up
            new MouseEvent { modifiers = modifiers }, // button
        };

        var getters = new MouseButtonGetter[]
        {
            Input.GetMouseButtonDown,
            Input.GetMouseButtonUp,
            Input.GetMouseButton
        };

        System.Nullable<RaycastHit> raycastHit = null;

        if (
            mouseRaycast
            && (
                Input.GetMouseButtonDown(0)
                || Input.GetMouseButtonDown(1)
                || Input.GetMouseButtonDown(2)
                || Input.GetMouseButtonUp(0)
                || Input.GetMouseButtonUp(1)
                || Input.GetMouseButtonUp(2)
            )
        )
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                raycastHit = hit;
            }
        }

        for (int i = 0; i < events.Length; i++)
        {
            MouseButtonGetter getter = getters[i];
            events[i].left = getter(0);
            events[i].right = getter(1);
            events[i].middle = getter(2);

            if (getter == Input.GetMouseButtonDown || getter == Input.GetMouseButtonUp)
            {
                events[i].raycastHit = raycastHit;
            }

            if (events[i].left || events[i].right || events[i].middle)
            {
                SendMessage(
                    i == 0
                        ? "OnInputMouseButtonDown"
                        : i == 1
                            ? "OnInputMouseButtonUp"
                            : "OnInputMouseButton",
                    events[i],
                    SendMessageOptions.DontRequireReceiver
                );
            }
        }
    }
}
