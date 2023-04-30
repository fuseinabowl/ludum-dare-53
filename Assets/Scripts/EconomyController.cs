using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EconomyController : MonoBehaviour
{
    public class Resources
    {
        public int dollars = 0;
        public bool dirty = false;

        public void Give(Resources res)
        {
            dollars += res.dollars;
            dirty = true;
        }

        public bool CanBuy(Resources cost)
        {
            return cost.dollars <= dollars;
        }

        public bool Buy(Resources cost)
        {
            if (!CanBuy(cost))
            {
                return false;
            }
            dollars -= cost.dollars;
            dirty = true;
            return true;
        }

        public bool ReadDirty()
        {
            if (dirty)
            {
                dirty = false;
                return true;
            }
            return false;
        }
    }

    [Header("UI")]
    public TMPro.TMP_Text dollarsText;

    [Header("Animations")]
    public TransformAnimator buyAnim;
    public TransformAnimator cannotBuyAnim;

    [Header("Configuration")]
    public int startDollars = 10;
    public Resources trackCost = new Resources { dollars = 1 };

    [HideInInspector]
    public Resources resources;

    [HideInInspector]
    public int availableTracks = 0;

    public void GiveResources(int dollars)
    {
        resources.Give(new Resources{dollars = dollars});
        buyAnim.Animate();
    }

    public bool CanBuyTrack()
    {
        return resources.CanBuy(trackCost);
    }

    public bool BuyTrack()
    {
        if (resources.Buy(trackCost))
        {
            availableTracks++;
            buyAnim.Animate();
            return true;
        }
        return false;
    }

    public bool BuyAndPlaceTrack()
    {
        if (BuyTrack())
        {
            availableTracks--;
            return true;
        }
        return false;
    }

    public void CannotBuy()
    {
        cannotBuyAnim.Animate();
    }

    private void Start()
    {
        resources = new Resources { dollars = startDollars };
        resources.dirty = true;
    }

    private void Update()
    {
        if (resources.ReadDirty())
        {
            dollarsText.text = string.Format("${0}", resources.dollars);
        }
    }
}
