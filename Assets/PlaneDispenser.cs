using System.Collections;
using UnityEngine;
using Oculus.Interaction;

public class PlaneDispenser : MonoBehaviour
{
    public GameObject planePrefab;
    public Transform spawnPoint;
    public float respawnDelay = 2f;

    [Header("Spawn safety")]
    public float enableGrabAfter = 0.15f;     // voorkomt auto-grab/half-select op spawn
    public float ignoreDispenserCollisionFor = 0.2f;

    bool respawning;
    Collider dispenserCol;

    void Start()
    {
        dispenserCol = GetComponent<Collider>();
        Spawn();
    }

    void Spawn()
    {
        if (!planePrefab || !spawnPoint) return;

        GameObject plane = Instantiate(planePrefab, spawnPoint.position, spawnPoint.rotation);

        // Spawn cleanup
        var rb = plane.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.WakeUp();
        }

        // Prevent instant grab/select issues
        var grabbable = plane.GetComponent<Grabbable>();
        if (grabbable)
        {
            grabbable.enabled = false;
            StartCoroutine(EnableGrabLater(grabbable, enableGrabAfter));
        }

        // Ignore dispenser collision briefly (optional but very effective)
        if (dispenserCol)
        {
            var planeCols = plane.GetComponentsInChildren<Collider>();
            foreach (var c in planeCols)
            {
                Physics.IgnoreCollision(c, dispenserCol, true);
            }
            StartCoroutine(ReenableCollisionLater(plane, ignoreDispenserCollisionFor));
        }

        PlaneNotifyGrab notify = plane.GetComponent<PlaneNotifyGrab>();
        if (!notify) notify = plane.AddComponent<PlaneNotifyGrab>();
        notify.dispenser = this;
    }

    IEnumerator EnableGrabLater(Grabbable g, float t)
    {
        yield return new WaitForSeconds(t);
        if (g) g.enabled = true;
    }

    IEnumerator ReenableCollisionLater(GameObject plane, float t)
    {
        yield return new WaitForSeconds(t);
        if (!plane || !dispenserCol) yield break;

        var planeCols = plane.GetComponentsInChildren<Collider>();
        foreach (var c in planeCols)
        {
            if (c) Physics.IgnoreCollision(c, dispenserCol, false);
        }
    }

    public void OnPlaneGrabbed()
    {
        if (respawning) return;
        StartCoroutine(Respawn());
    }

    IEnumerator Respawn()
    {
        respawning = true;
        yield return new WaitForSeconds(respawnDelay);
        respawning = false;
        Spawn();
    }
}
