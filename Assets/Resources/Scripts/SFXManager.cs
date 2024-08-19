using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public AudioSource source;

    public AudioClip moveSFX;
    public AudioClip quickFallSFX;
    public AudioClip rotateSFX;
    public AudioClip preExplodeSFX;
    public AudioClip explodeSFX;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayMoveSFX()
    {
        source.PlayOneShot(moveSFX, 0.2f);
    }

    public void PlayQuickFallSFX()
    {
        source.PlayOneShot(quickFallSFX, 0.5f);
    }

    public void PlayRotateSFX()
    {
        source.PlayOneShot(rotateSFX, 0.2f);
    }

    public void PlayPreExplodeSFX()
    {
        source.PlayOneShot(preExplodeSFX, 0.1f);
    }

    public void PlayExplodeSFX()
    {
        source.PlayOneShot(explodeSFX, 1.0f);
    }
}
