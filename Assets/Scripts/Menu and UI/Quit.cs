using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quit : MonoBehaviour
{
    public void QuitGame()
    {
        staticQuit();
    }

    /// <summary>
    /// Exits the game. Application.Quit() is a no-op inside the Editor, so in
    /// play mode we stop playback instead - otherwise the quit button appears
    /// to do nothing and the menu just stays up.
    /// </summary>
    public static void staticQuit() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
