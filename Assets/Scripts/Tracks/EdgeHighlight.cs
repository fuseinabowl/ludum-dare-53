using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EdgeHighlight : MonoBehaviour, MouseHitTarget.HoverHandler
{
    [SerializeField]
    private Material hoverMaterial;

    private Material defaultMaterial;

    public void OnMouseHitHover(MouseHitTarget.Event e) { }

    public void OnMouseHitHoverStart(MouseHitTarget.Event e)
    {
        var meshRenderer = GetComponentInChildren<MeshRenderer>();
        defaultMaterial = meshRenderer.material;
        meshRenderer.material = hoverMaterial;

        SFX.PlayOneShot(gameObject, SFX.singleton.trackHover);
    }

    public void OnMouseHitHoverEnd(MouseHitTarget.Event e)
    {
        var meshRenderer = GetComponentInChildren<MeshRenderer>();
        meshRenderer.material = defaultMaterial;
    }
}
