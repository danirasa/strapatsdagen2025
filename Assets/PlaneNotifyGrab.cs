using UnityEngine;
using Oculus.Interaction;

[RequireComponent(typeof(Grabbable))]
public class PlaneNotifyGrab : MonoBehaviour
{
    public PlaneDispenser dispenser;

    Grabbable grabbable;
    bool requested;

    void Awake()
    {
        grabbable = GetComponent<Grabbable>();
    }

    void Update()
    {
        if (requested) return;

        if (grabbable.SelectingPointsCount > 0)
        {
            requested = true;
            dispenser?.OnPlaneGrabbed();
        }
    }
}
