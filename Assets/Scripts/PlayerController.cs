using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public FMODUnity.StudioEventEmitter shotSfx;
    public Rigidbody shotPrefab;
    public float shotVelocity = 100;
    [Range(0.1f, 1f)] public float shotPeriod = 0.1f;
    [Range(0, 0.1f)] public float shotAudioDelay = 0.02f;

    private Timer shotTimer;

    private void Awake()
    {
        shotTimer = Timer.Find(gameObject, "GunTimer");
        shotTimer.period = shotPeriod;
        FMODUtil.SetParam(shotSfx, "SpawnRate", 0.1f / shotPeriod);
    }

    private void Update()
    {
        if (Input.anyKeyDown)
        {
            StartShooting();
        }
        else if (!Input.anyKey && shotTimer.running)
        {
            StopShooting();
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
        if (shotTimer.elapsed < shotAudioDelay)
        {
            // If we shot < shotAudioDelay ago then wait a bit to tell FMOD to stop its audio,
            // otherwise it might cut out too soon.
            Timer.OneShot(gameObject, shotAudioDelay).tick += StopSFX;
        }
        else
        {
            StopSFX(null);
        }
        shotTimer.StopTimer();
    }

    private void StopSFX(Timer timer)
    {
        shotSfx.SetParameter("Stop", 1);
    }

    private void Shoot()
    {
        var shot = Instantiate(shotPrefab, transform.position, transform.rotation);
        shot.AddForce(shotVelocity * Vector3.up);
    }

    private void OnTimerTick(Timer timer)
    {
        if (timer == shotTimer)
        {
            Shoot();
        }
    }
}
