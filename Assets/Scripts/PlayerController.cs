using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public FMODUnity.StudioEventEmitter shotSfx;
    public Rigidbody shotPrefab;
    public float shotVelocity = 100;
    [Range(0.1f, 1f)] public float shotPeriod = 0.1f;
    [Range(0, 0.1f)] public float shotGracePeriod = 0;

    private Timer shotTimer;
    private bool stopShootingOnTimerTick = false;

    private void Awake()
    {
        shotTimer = Timer.FindTimer(gameObject, "GunTimer");
        shotTimer.period = shotPeriod;
        FMODUtil.SetParam(shotSfx, "SpawnRate", 0.1f / shotPeriod);
    }

    private void Update()
    {
        if (Input.anyKeyDown)
        {
            StartShooting();
        }
        else if (!Input.anyKey && shotTimer.running && !stopShootingOnTimerTick)
        {
            if (shotTimer.nextTick < shotGracePeriod)
            {
                // Make sure there are the same number of sounds and bullets by waiting until the
                // next tick (which fires a bullet) to stop. There is ultimately to account for
                // the fact that the FMOD API isn't instantaneous.
                Debug.LogFormat("About to shoot in {0} seconds, I will stop shooting then", shotTimer.nextTick);
                stopShootingOnTimerTick = true;
            }
            else
            {
                StopShooting();
            }
        }
    }

    private void StartShooting()
    {
        shotTimer.StartTimer();
        shotSfx.Play();
        shotSfx.SetParameter("Stop", 0);
        Shoot();
    }

    private void StopShooting()
    {
        shotTimer.StopTimer();
        shotSfx.SetParameter("Stop", 1);
    }

    private void Shoot()
    {
        var shot = Instantiate(shotPrefab, transform.position, transform.rotation);
        shot.AddForce(shotVelocity * Vector3.up);
    }

    private void OnTimerTick(Timer timer)
    {
        Shoot();

        if (stopShootingOnTimerTick)
        {
            stopShootingOnTimerTick = false;
            StopShooting();
        }
    }

}
