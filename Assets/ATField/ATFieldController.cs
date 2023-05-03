using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class ATFieldController : MonoBehaviour
{
    [Header("Components")]
    public string colliderTag;
    public BoxCollider collider;
    public GameObject shieldMesh;
    Vector3 spawnLocation = Vector3.zero;

    [Header("VFX Prefabs")]
    public VisualEffect hitEffect;
    public VisualEffect breakEffect;
    public VisualEffect initEffect;

    [Header("Shield Health")]
    [Tooltip("Number of hite before the shield breaks, if broken shield must fully reset before reinitializing.")]
    public float shieldHealth = 20;
    public float damagePerHit = 1;
    public float totalDamage = 0;
    public float hitRecoveryRate = 1f;
    [GradientUsage(hdr:true)]
    public Gradient shieldColorOverDamage;
    bool broken = false;
    float timer;
    Material m;
    private void Start()
    {
        m = shieldMesh.GetComponent<Renderer>().material;
    }
    private void Update()
    {
        if(totalDamage > 0)
        {
            totalDamage -= hitRecoveryRate * Time.deltaTime;
            if(totalDamage > shieldHealth && !broken)
            {
                broken = true;
                breakEffect.Play();
            }
        }
        if(totalDamage <= 0 && broken)
        {
            broken = false;
            initEffect.Play();
        }
        if (broken)
        {
            if (collider.enabled)
            {
                collider.enabled = false;
            }
            if (shieldMesh.activeInHierarchy)
            {
                shieldMesh.SetActive(false);
            }
        }
        else
        {
            if (!collider.enabled)
            {
                collider.enabled = true;
            }
            if (!shieldMesh.activeInHierarchy)
            {
                shieldMesh.SetActive(true);
            }
        }

        m.SetColor("Color_497c8daef4f84b28aa3f1304441a0315", shieldColorOverDamage.Evaluate(totalDamage/shieldHealth));
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(colliderTag.Length > 0)
        {
            if (collision.gameObject.tag == colliderTag)
            {
                spawnLocation = collision.contacts[0].point;
                spawnLocation.z = 0;
                hitEffect.SetVector3("SpawnLocation", spawnLocation);
                hitEffect.Play();
            }

        }
        else
        {
            spawnLocation = collision.contacts[0].point;
            spawnLocation.z = 0;
            hitEffect.SetVector3("SpawnLocation", collision.contacts[0].point);
            hitEffect.Play();
        }

        totalDamage += damagePerHit;
    }
}
