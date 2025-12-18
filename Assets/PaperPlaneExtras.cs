using System.Collections;
using UnityEngine;
using Oculus.Interaction;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Grabbable))]
public class PaperPlaneExtras : MonoBehaviour
{
    public float despawnAfterSeconds = 100f;

    Rigidbody rb;
    Grabbable grabbable;
    PaperPlaneFlight flight;

    bool wasGrabbed;
    Coroutine despawnRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();
        flight = GetComponent<PaperPlaneFlight>();

        flight.DisableFlight();
    }

    void Update()
    {
        if (grabbable.SelectingPointsCount > 0 && !wasGrabbed)
        {
            wasGrabbed = true;
            OnGrabbed();
        }

        if (grabbable.SelectingPointsCount == 0 && wasGrabbed)
        {
            wasGrabbed = false;
            OnReleased();
        }
    }

    void OnGrabbed()
    {
        flight.DisableFlight();

        if (despawnRoutine != null)
        {
            StopCoroutine(despawnRoutine);
            despawnRoutine = null;
        }
    }

    void OnReleased()
    {
        // ISDK bepaalt velocity → wij voegen alleen aerodynamica toe
        flight.EnableFlight();
        despawnRoutine = StartCoroutine(DespawnTimer());
    }

    IEnumerator DespawnTimer()
    {
        yield return new WaitForSeconds(despawnAfterSeconds);
        Destroy(gameObject);
    }
}
