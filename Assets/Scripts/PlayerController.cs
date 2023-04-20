using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public FMODUnity.EventReference fmodAutoGun;
    public Rigidbody shotPrefab;
    public float shotVelocity = 100;
    [Range(0.1f, 1f)] public float autoGunTime = 0.1f;

    private FMOD.Studio.EventInstance fmodAutoGunInst;
    private float gunTimer = 0;

    private void Update()
    {
        if (Input.anyKeyDown)
        {
            fmodAutoGunInst = SFX.Start(fmodAutoGun, gameObject, "shotPeriod", autoGunTime);
            Shoot();
            gunTimer = autoGunTime;
        }
        else if (!Input.anyKey)
        {
            if (fmodAutoGunInst.isValid())
            {
                SFX.StopRelease(fmodAutoGunInst);
            }
            gunTimer = 0;
        }

        if (gunTimer != 0)
        {
            gunTimer -= Time.deltaTime;
            if (gunTimer <= 0)
            {
                Shoot();
                gunTimer += autoGunTime;
            }
        }
    }

    private void Shoot()
    {
        var shot = Instantiate(shotPrefab, transform.position, transform.rotation);
        shot.AddForce(shotVelocity * Vector3.up);
    }
}
