using System;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Threading;

/// <summary>
/// Fork projet de OVRLipSyncMicInput (Assets/Oculus/LipSync/Scripts/OVRLipSyncMicInput.cs).
///
/// Le script d'origine coupe le micro des que l'application perd le focus
/// (OnApplicationFocus / OnApplicationPause -> StopMicrophone, plus un early return
/// dans Update). Dans l'editeur, "l'application" c'est la vue Game : cliquer dans la
/// Scene, l'Inspector ou une autre fenetre suffit a tuer le lipsync.
///
/// Ce fork rend ce comportement optionnel via keepRecordingWhenUnfocused.
/// Le reste est identique au SDK, a deux details pres :
///  - la logique HoldToSpeak / PushToSpeak, commentee dans le SDK, est retablie
///    (micControl = ConstantSpeak donne exactement le meme resultat qu'avant) ;
///  - StopMicrophone ne plante plus si aucun OVRLipSyncContext n'est present.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ToxxicLipSyncMicInput : MonoBehaviour
{
    public enum micActivation
    {
        HoldToSpeak,
        PushToSpeak,
        ConstantSpeak
    }

    // PUBLIC MEMBERS
    [Tooltip("Manual specification of Audio Source - " +
        "by default will use any attached to the same object.")]
    public AudioSource audioSource = null;

    [Header("Toxxic")]
    [Tooltip("Garde le micro actif quand la vue Game / l'application perd le focus. " +
        "Necessite aussi Project Settings > Player > Run In Background.")]
    public bool keepRecordingWhenUnfocused = true;

    [Tooltip("Enable a keypress to toggle the microphone device selection GUI.")]
    public bool enableMicSelectionGUI = false;
    [Tooltip("Key to toggle the microphone selection GUI if enabled.")]
    public KeyCode micSelectionGUIKey = KeyCode.M;

    [SerializeField]
    [Range(0.0f, 100.0f)]
    [Tooltip("Microphone input volume control.")]
    private float micInputVolume = 100;

    [SerializeField]
    [Tooltip("Requested microphone input frequency")]
    private int micFrequency = 48000;
    public float MicFrequency
    {
        get { return micFrequency; }
        set { micFrequency = (int)Mathf.Clamp((float)value, 0, 96000); }
    }

    [Tooltip("Microphone input control method. Hold To Speak and Push" +
        " To Speak are driven with the Mic Activation Key.")]
    public micActivation micControl = micActivation.ConstantSpeak;
    [Tooltip("Key used to drive Hold To Speak and Push To Speak methods" +
        " of microphone input control.")]
    public KeyCode micActivationKey = KeyCode.Space;

    [Tooltip("Will contain the string name of the selected microphone device - read only.")]
    public string selectedDevice;

    // PRIVATE MEMBERS
    private bool micSelected = false;
    private int minFreq, maxFreq;
    private bool focused = true;
    private bool initialized = false;

    //----------------------------------------------------
    // MONOBEHAVIOUR OVERRIDE FUNCTIONS
    //----------------------------------------------------

    void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (!audioSource) return; // this should never happen
    }

    void Start()
    {
        if (keepRecordingWhenUnfocused)
        {
            // Sans ca, Unity suspend la boucle de jeu hors focus et Update ne tourne plus.
            Application.runInBackground = true;
        }

        audioSource.loop = true;     // Set the AudioClip to loop
        audioSource.mute = false;

        InitializeMicrophone();
    }

    private void InitializeMicrophone()
    {
        if (initialized)
        {
            return;
        }
        if (Microphone.devices.Length == 0)
        {
            return;
        }
        selectedDevice = Microphone.devices[0].ToString();
        micSelected = true;
        GetMicCaps();
        initialized = true;
    }

    void Update()
    {
        if (!focused && !keepRecordingWhenUnfocused)
        {
            if (Microphone.IsRecording(selectedDevice))
            {
                StopMicrophone();
            }
            return;
        }

        if (!Application.isPlaying)
        {
            StopMicrophone();
            return;
        }

        // Lazy Microphone initialization (needed on Android)
        if (!initialized)
        {
            InitializeMicrophone();
        }

        audioSource.volume = (micInputVolume / 100);

        //Hold To Speak
        if (micControl == micActivation.HoldToSpeak)
        {
            if (Input.GetKey(micActivationKey))
            {
                if (!Microphone.IsRecording(selectedDevice))
                {
                    StartMicrophone();
                }
            }
            else
            {
                if (Microphone.IsRecording(selectedDevice))
                {
                    StopMicrophone();
                }
            }
        }

        //Push To Talk
        if (micControl == micActivation.PushToSpeak)
        {
            if (Input.GetKeyDown(micActivationKey))
            {
                if (Microphone.IsRecording(selectedDevice))
                {
                    StopMicrophone();
                }
                else
                {
                    StartMicrophone();
                }
            }
        }

        //Constant Speak
        if (micControl == micActivation.ConstantSpeak)
        {
            if (!Microphone.IsRecording(selectedDevice))
            {
                StartMicrophone();
            }
        }

        //Mic Selected = False
        if (enableMicSelectionGUI)
        {
            if (Input.GetKeyDown(micSelectionGUIKey))
            {
                micSelected = false;
            }
        }
    }

    void OnApplicationFocus(bool focus)
    {
        focused = focus;

        if (!focused && !keepRecordingWhenUnfocused)
            StopMicrophone();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        focused = !pauseStatus;

        if (!focused && !keepRecordingWhenUnfocused)
            StopMicrophone();
    }

    void OnDisable()
    {
        StopMicrophone();
    }

    void OnGUI()
    {
        MicDeviceGUI((Screen.width / 2) - 150, (Screen.height / 2) - 75, 300, 50, 10, -300);
    }

    //----------------------------------------------------
    // PUBLIC FUNCTIONS
    //----------------------------------------------------

    public void MicDeviceGUI(
        float left,
        float top,
        float width,
        float height,
        float buttonSpaceTop,
        float buttonSpaceLeft)
    {
        //If there is more than one device, choose one.
        if (Microphone.devices.Length >= 1 && enableMicSelectionGUI == true && micSelected == false)
        {
            for (int i = 0; i < Microphone.devices.Length; ++i)
            {
                if (GUI.Button(new Rect(left + ((width + buttonSpaceLeft) * i),
                                        top + ((height + buttonSpaceTop) * i), width, height),
                               Microphone.devices[i].ToString()))
                {
                    StopMicrophone();
                    selectedDevice = Microphone.devices[i].ToString();
                    micSelected = true;
                    GetMicCaps();
                    StartMicrophone();
                }
            }
        }
    }

    public void GetMicCaps()
    {
        if (micSelected == false) return;

        //Gets the frequency of the device
        Microphone.GetDeviceCaps(selectedDevice, out minFreq, out maxFreq);

        if (minFreq == 0 && maxFreq == 0)
        {
            Debug.LogWarning("GetMicCaps warning:: min and max frequencies are 0");
            minFreq = 44100;
            maxFreq = 44100;
        }

        if (micFrequency > maxFreq)
            micFrequency = maxFreq;
    }

    public void StartMicrophone()
    {
        if (micSelected == false) return;

        //Starts recording
        audioSource.clip = Microphone.Start(selectedDevice, true, 1, micFrequency);

        Stopwatch timer = Stopwatch.StartNew();

        // Wait until the recording has started
        while (!(Microphone.GetPosition(selectedDevice) > 0) && timer.Elapsed.TotalMilliseconds < 1000)
        {
            Thread.Sleep(50);
        }

        if (Microphone.GetPosition(selectedDevice) <= 0)
        {
            throw new Exception("Timeout initializing microphone " + selectedDevice);
        }
        // Play the audio source
        audioSource.Play();
    }

    public void StopMicrophone()
    {
        if (micSelected == false) return;

        // Overriden with a clip to play? Don't stop the audio source
        if ((audioSource != null) &&
            (audioSource.clip != null) &&
            (audioSource.clip.name == "Microphone"))
        {
            audioSource.Stop();
        }

        // Reset to stop mouth movement
        OVRLipSyncContext context = GetComponent<OVRLipSyncContext>();
        if (context != null)
        {
            context.ResetContext();
        }

        Microphone.End(selectedDevice);
    }
}
