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

    public struct KeyEvent
    {
        public Modifiers modifiers;
        public KeyCode keyCode;
    }

    public struct MouseEvent
    {
        public Modifiers modifiers;
        public bool left;
        public bool right;
        public bool middle;
        public System.Nullable<RaycastHit> raycastHit;
    }

    [SerializeField]
    [Tooltip("Comma-separated list of keys to watch for")]
    private string keyCodes;

    [SerializeField]
    [Tooltip("Watch for mouse button events")]
    private bool mouseEvents;

    [SerializeField]
    [Tooltip("Raycast mouse up/down events (not move)")]
    private bool mouseRaycast;

    private List<KeyCode> keyCodeList = new List<KeyCode>();

    private void Start()
    {
        InitKeyboardInput();
    }

    private void InitKeyboardInput()
    {
        if (keyCodes.Trim().Length == 0)
        {
            return;
        }

        foreach (var keyCodeStringIter in keyCodes.Split(","))
        {
            var keyCodeString = keyCodeStringIter.Trim().ToLower();
            if (keyCodeString == "shift")
            {
                keyCodeList.Add(KeyCode.LeftShift);
                keyCodeList.Add(KeyCode.RightShift);
            }
            else if (keyCodeString == "control")
            {
                keyCodeList.Add(KeyCode.LeftControl);
                keyCodeList.Add(KeyCode.RightControl);
            }
            else if (keyCodeString == "alt")
            {
                keyCodeList.Add(KeyCode.LeftAlt);
                keyCodeList.Add(KeyCode.RightAlt);
            }
            else if (keyCodeString == "meta")
            {
                keyCodeList.Add(KeyCode.LeftMeta);
                keyCodeList.Add(KeyCode.RightMeta);
            }
            else
            {
                try
                {
                    keyCodeList.Add(System.Enum.Parse<KeyCode>(keyCodeString, true));
                }
                catch (System.Exception)
                {
                    Debug.LogErrorFormat("Unrecognised KeyCode {0}", keyCodeStringIter);
                }
            }
        }
    }

    private void Update()
    {
        var modifiers = new Modifiers
        {
            shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
            control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
            alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt),
            meta = Input.GetKey(KeyCode.LeftMeta) || Input.GetKey(KeyCode.RightMeta),
        };

        foreach (var keyCode in keyCodeList)
        {
            if (Input.GetKeyDown(keyCode))
            {
                SendMessage(
                    "OnInputKeyDown",
                    new KeyEvent { keyCode = keyCode, modifiers = modifiers },
                    SendMessageOptions.DontRequireReceiver
                );
            }
            if (Input.GetKeyUp(keyCode))
            {
                SendMessage(
                    "OnInputKeyUp",
                    new KeyEvent { keyCode = keyCode, modifiers = modifiers },
                    SendMessageOptions.DontRequireReceiver
                );
            }
            if (Input.GetKey(keyCode))
            {
                SendMessage(
                    "OnInputKey",
                    new KeyEvent { keyCode = keyCode, modifiers = modifiers },
                    SendMessageOptions.DontRequireReceiver
                );
            }
        }

        if (mouseEvents)
        {
            var events = new MouseEvent[]
            {
                new MouseEvent { modifiers = modifiers }, // down
                new MouseEvent { modifiers = modifiers }, // up
                new MouseEvent { modifiers = modifiers },
            };

            System.Nullable<RaycastHit> raycastHit = null;

            if (mouseRaycast && HasMouseButtonDownOrUp())
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
                MouseButtonGetter getter =
                    i == 0
                        ? Input.GetMouseButtonDown
                        : i == 1
                            ? Input.GetMouseButtonUp
                            : Input.GetMouseButton;
                events[i].left = getter(0);
                events[i].right = getter(1);
                events[i].middle = getter(2);

                if (i == 0 || i == 1)
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

    delegate bool MouseButtonGetter(int button);

    bool HasMouseButtonDownOrUp()
    {
        return Input.GetMouseButtonDown(0)
            || Input.GetMouseButtonDown(1)
            || Input.GetMouseButtonDown(2)
            || Input.GetMouseButtonUp(0)
            || Input.GetMouseButtonUp(1)
            || Input.GetMouseButtonUp(2);
    }
}
