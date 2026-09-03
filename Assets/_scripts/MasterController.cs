using UnityEngine;
using UnityEngine.Video;

public class MasterController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public VideoClip clipNormal;
    public VideoClip glitchClip;

    void Start()
    {
        videoPlayer.clip = clipNormal;
        videoPlayer.Play();
    }
    public void PasserEnGlitch()
    {
        videoPlayer.clip = glitchClip;
        videoPlayer.Play();
    }
}