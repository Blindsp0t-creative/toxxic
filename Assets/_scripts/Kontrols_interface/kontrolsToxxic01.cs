using UnityEngine;
using UnityEditor;
using Kontrols;

public class kontrolsToxxic01 : KontrolsWindow
{
    [MenuItem("Tools/ToxxicKontrols")]
    static void Open() => GetWindow<kontrolsToxxic01>("ToxxicKontrols");

    [Section("GENERAL")]
    [Button("CALIBRATE", id: "calbibrate")]                         public void LOG_Calib() { Debug.Log("CALIB"); }
    [Button("BLACK OUT", id: "blackout")]                           public void LOG_BlackOut() { Debug.Log("BLACK OUT"); }

    [Section("STRIP CLUB")]
    [Button("Top Scene StripClub", id: "stripclub")]                public void LOG_loadSceneStripClub() { Debug.Log("load Strip Club Scene"); }
    [Button("Next", id: "stripclubN")]                              public void LOG_topNexttripClub() { Debug.Log("Next - Strip Club Scene"); }
    [Button("Previous", id: "stripclubP")]                          public void LOG_topPreviousStripClub() { Debug.Log("Previous - Strip Club Scene"); }

    [Slider("ElevationAvatar", 0.0f, 2.0f, id: "elevationavatar")]  public float avatarElevation;

    [Section("PELLETEUSE")]
    [Button("Top Scene Pelleteuse", id: "pelleteuse")]              public void LOG_loadScenePelleteuse() { Debug.Log("load Pelleteuse Scene"); }
    [Button("Next", id: "pelleteuseN")]                             public void LOG_topNextPelleteuse() { Debug.Log("Next - Pelleteuse Scene"); }
    [Button("Previous", id: "pelleteuseP")]                         public void LOG_topPreviousPelleteuse() { Debug.Log("Previous - Pelleteuse Scene"); }
    [Button("Top Photo", id: "togglePhoto")]                        public void LOG_topPhotoPelleteuse() { Debug.Log("Top Photo - Pelleteuse Scene"); }

    [Section("RAINBOW ROAD")]
    [Button("Top Scene RainbowRoad", id: "rainbowroad")]            public void LOG_loadSceneRainbow() { Debug.Log("load Rainbow Road Scene"); }
    [Button("Next", id: "RainbowRoadN")]                            public void LOG_topNextRainbowRoad() { Debug.Log("Next - RainbowRoad Scene"); }
    [Button("Previous", id: "RainbowRoadP")]                        public void LOG_topPreviousRainbowRoad() { Debug.Log("Previous - RainbowRoad Scene"); }
    [Button("Top Video TV", id: "toggleVideoTV")]                   public void LOG_topTvRainbowRoad() { Debug.Log("Top TV - RainbowRoad Scene"); }



    

}
