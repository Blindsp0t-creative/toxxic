using UnityEngine;
using UnityEngine.Video;

public class StartStopVideo : MonoBehaviour
{
    public VideoPlayer _player;

    public void toggleVideo()
    {
        if (_player.isPlaying)
        {
            Debug.Log("is playing");
            _player.Stop();
            _player.Pause();
        }
        else if(_player.isPlaying == false)
        {
            _player.Play();
        }
    }
}
