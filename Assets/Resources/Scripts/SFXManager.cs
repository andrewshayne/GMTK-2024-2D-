using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public AudioSource musicSource;
    public AudioSource sfxSource;

    public AudioClip moveSFX;
    public AudioClip quickFallSFX;
    public AudioClip rotateSFX;
    public AudioClip inflateSFX;
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

    private void Awake()
    {
    }

    public void PlayMoveSFX()
    {
        sfxSource.PlayOneShot(moveSFX, 0.2f);
    }

    public void PlayQuickFallSFX()
    {
        sfxSource.PlayOneShot(quickFallSFX, 0.5f);
    }

    public void PlayRotateSFX()
    {
        sfxSource.PlayOneShot(rotateSFX, 0.2f);
    }

    public void PlayInflateSFX()
    {
        sfxSource.PlayOneShot(inflateSFX, 0.2f);
    }

    public void PlayPreExplodeSFX()
    {
        sfxSource.PlayOneShot(preExplodeSFX, 0.1f);
    }

    public void PlayExplodeSFX()
    {
        sfxSource.PlayOneShot(explodeSFX, 1.0f);
    }
}
