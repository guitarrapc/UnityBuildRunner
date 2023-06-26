#nullable enable
using UnityEngine;
using UnityEngine.UI;

public class CanvasControl : MonoBehaviour
{
    public Button? ExitButton;

    private void Start()
    {
        ExitButton?.onClick.AddListener(() => QuitGame());
    }

    private void QuitGame()
    {
        Debug.Log("Quitting game.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
