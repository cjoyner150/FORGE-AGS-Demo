using FORGE;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] GameManager gm;
    [SerializeField] SurgeController sc;
    [SerializeField] Reel[] reels;
    [SerializeField] AudioSource reelSpinSource;
    [SerializeField] AudioSource reelStopSource;
    [SerializeField] AudioSource winSource;
    [SerializeField] AudioSource surgeSource;

    [Range(0, .5f)]
    [SerializeField] float pitchRandom;

    private void OnEnable()
    {
        gm.OnSpinBegin += OnBeginSpin;
        gm.OnSpinResolved += OnSpinResolve;

        sc.OnSurgeTriggered += OnSurgeBegin;
        sc.OnSurgeEnded += OnSurgeEnd;

        foreach (var reel in reels)
        {
            reel.OnLanded += OnReelStop;
        }
    }

    private void OnDisable()
    {
        gm.OnSpinBegin -= OnBeginSpin;
        gm.OnSpinResolved -= OnSpinResolve;

        sc.OnSurgeTriggered -= OnSurgeBegin;
        sc.OnSurgeEnded -= OnSurgeEnd;

        foreach (var reel in reels)
        {
            reel.OnLanded -= OnReelStop;
        }
    }

    void OnBeginSpin()
    {
        if (!reelSpinSource.isPlaying)
        {
            reelSpinSource.Play();
        }
    }

    void OnSpinResolve(SpinResult result)
    {
        if (reelSpinSource.isPlaying)
        {
            reelSpinSource.Stop();
        }

        if (result.IsWin)
        {
            winSource.pitch = 1 + Random.Range(-pitchRandom, pitchRandom);

            if (result.PayoutMultiplier < 5)
            {
                winSource.volume = .3f;
            }
            else if (result.PayoutMultiplier < 13)
            {
                winSource.volume = .5f;
            }
            else if (result.PayoutMultiplier < 50)
            {
                winSource.volume = .7f;
            }
            else if (result.PayoutMultiplier < 100)
            {
                winSource.volume = .85f;
            }
            else winSource.volume = 1;

            winSource.Play();
        }
    }

    void OnReelStop(Reel reel)
    {
        reelStopSource.pitch = 1 + Random.Range(-pitchRandom, pitchRandom);
        reelStopSource.PlayOneShot(reelStopSource.clip);
    }

    void OnSurgeBegin()
    {
        surgeSource.pitch = 1 + Random.Range(-pitchRandom, pitchRandom);
        surgeSource.Play();
    }

    void OnSurgeEnd()
    {

    }
}
