using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PaperPlaneFlight : MonoBehaviour
{
    [Header("Forces (paper feel)")]
    public float liftPower = 10f;        // lift ~ speed^2
    public float dragPower = 10.0052f;      // hoger = sneller afremmen
    public float sideDragPower = 10.10f;    // hoger = minder slip

    [Header("Stall / lift limits")]
    public float stallSpeed = 10.35f;       // hoger = minder lift bij lage snelheid
    public float maxLift = 16.5f;

    [Header("Keep flying (but NOT climbing forever)")]
    [Range(0f, 1.2f)] public float gravityComp = 0.40f;
    public float gravityCompSpeed = 0.9f;

    [Tooltip("Tiny push to keep it cruising.")]
    public float cruisePush = 0.035f;

    [Tooltip("Extra push when speed is low, so it doesn't drop dead.")]
    public float slowCruiseBoost = 0.06f;
    public float slowSpeed = 0.7f;

    [Header("Climb damper (stops step-by-step rising)")]
    public float climbDamp = 0.9f;
    public float climbDeadzone = 0.15f;

    [Header("Playfulness (WOBBLE)")]
    [Tooltip("Small wobble force (up + sideways).")]
    public float flutterStrength = 0.14f;      // start 0.10 - 0.16
    public float flutterFrequency = 1.1f;
    public float flutterMinSpeed = 0.9f;

    [Header("Playfulness (TURNS / WEAVE)")]
    [Tooltip("Sideways turning force. This makes natural paperplane arcs.")]
    public float turnStrength = 0.28f;         // start 0.15 - 0.35
    public float turnFrequency = 0.45f;        // slower = wider arcs
    public float turnMinSpeed = 0.9f;

    [Tooltip("Max visual bank angle when turning (degrees).")]
    public float maxBankDeg = 18f;             // start 12 - 22

    [Header("Rotation follow")]
    public float stability = 1.7f;             // lower = looser / more playful
    public float maxAngularSpeed = 5.0f;

    public bool flightEnabled = false;

    Rigidbody rb;
    float seedA;
    float seedB;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        seedA = Random.value * 1000f;
        seedB = Random.value * 2000f + 1000f;

        rb.maxAngularVelocity = Mathf.Max(7f, maxAngularSpeed);
    }

    void FixedUpdate()
    {
        if (!flightEnabled) return;

        // Hard cap angular velocity (prevents spin-out)
        Vector3 av = rb.angularVelocity;
        float avMag = av.magnitude;
        if (avMag > maxAngularSpeed)
            rb.angularVelocity = av * (maxAngularSpeed / avMag);

        Vector3 v = rb.linearVelocity;
        float speed = v.magnitude;
        if (speed < 0.01f) return;

        Vector3 airflow = v / speed;

        // Stable right axis perpendicular to airflow and world up
        Vector3 right = Vector3.Cross(Vector3.up, airflow);
        float rMag = right.magnitude;
        if (rMag < 0.001f)
            right = Vector3.Cross(transform.right, airflow);
        right.Normalize();

        // 1) Stall gate by speed
        float stall01 = Mathf.InverseLerp(0f, stallSpeed, speed);
        stall01 = Mathf.Clamp01(stall01);

        // 2) Lift (world-up)
        float lift = liftPower * speed * speed * stall01;
        lift = Mathf.Min(lift, maxLift);
        rb.AddForce(Vector3.up * lift, ForceMode.Force);

        // 3) Drag along velocity
        rb.AddForce(-v * dragPower * speed, ForceMode.Force);

        // 4) Side drag (damp sideways slip)
        float sideSpeed = Vector3.Dot(v, right);
        rb.AddForce(-right * sideSpeed * sideDragPower, ForceMode.Force);

        // 5) Gravity compensation (keeps gliding)
        float glide01 = Mathf.Clamp01(speed / gravityCompSpeed);
        rb.AddForce(-Physics.gravity * rb.mass * gravityComp * glide01, ForceMode.Force);

        // 6) Cruise push along airflow
        float slow01 = Mathf.InverseLerp(slowSpeed, 0f, speed);
        slow01 = Mathf.Clamp01(slow01);
        float push = cruisePush + slowCruiseBoost * slow01;
        rb.AddForce(airflow * push, ForceMode.Force);

        // 7) Climb damper (prevents slow creeping upward)
        float climbSpeed = Vector3.Dot(v, Vector3.up);
        float climbOver = Mathf.Max(0f, climbSpeed - climbDeadzone);
        if (climbOver > 0f)
        {
            rb.AddForce(Vector3.down * climbOver * climbDamp, ForceMode.Force);
        }

        // === PLAYFUL PARTS ===

        // 8) Wobble/flutter: tiny up + sideways force (safe, no torque)
        if (speed >= flutterMinSpeed)
        {
            float t = Time.time * flutterFrequency;
            float nUp = Mathf.PerlinNoise(seedA, t) * 2f - 1f;
            float nSide = Mathf.PerlinNoise(seedA + 33.3f, t + 10f) * 2f - 1f;

            float f01 = Mathf.Clamp01((speed - flutterMinSpeed) / 2.0f);
            Vector3 flutterForce = (Vector3.up * nUp + right * nSide) * (flutterStrength * f01);
            rb.AddForce(flutterForce, ForceMode.Force);
        }

        // 9) Turns/weave: sideways force makes gentle arcs (like real paper)
        float bankSigned = 0f;
        if (speed >= turnMinSpeed)
        {
            float t2 = Time.time * turnFrequency;
            float nTurn = Mathf.PerlinNoise(seedB, t2) * 2f - 1f; // -1..1

            float t01 = Mathf.Clamp01((speed - turnMinSpeed) / 2.0f);

            // Sideways acceleration: right * noise
            Vector3 turnForce = right * (nTurn * turnStrength * t01);
            rb.AddForce(turnForce, ForceMode.Force);

            // For visuals: bank with the turn
            bankSigned = nTurn * Mathf.Deg2Rad * maxBankDeg * t01;
        }

        // 10) Rotation: face airflow + small bank (roll) around airflow axis
        // We create a "banked up" reference by rotating world up around airflow
        Quaternion bankRot = Quaternion.AngleAxis(bankSigned * Mathf.Rad2Deg, airflow);
        Vector3 bankedUp = bankRot * Vector3.up;

        Quaternion targetRot = Quaternion.LookRotation(airflow, bankedUp);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, stability * Time.fixedDeltaTime));
    }

    public void EnableFlight() => flightEnabled = true;
    public void DisableFlight() => flightEnabled = false;
}
