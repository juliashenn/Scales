using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ScaleAudio : MonoBehaviour
{
    public Scale scale; // drag the same GameObject that has Scale.cs
    public AudioClip tiltClip;

    AudioSource audioSource;
    float lastDifference;

    void Awake() => audioSource = GetComponent<AudioSource>();

    void Update()
    {
        float currentDifference = scale.rightPan.totalWeight - scale.leftPan.totalWeight;

        if (currentDifference != lastDifference)
        {
            lastDifference = currentDifference;
            audioSource.PlayOneShot(tiltClip);
        }
    }
}