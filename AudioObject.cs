using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioObject : MonoBehaviour
{
    AudioSource source;
    public void Setup(AudioClip sound, Transform target)
    {
        source = GetComponent<AudioSource>();
        source.clip = sound;
        transform.position = target.position;
        source.Play();
    }

    // Update is called once per frame
    void Update()
    {

    }
}