using UnityEngine;
using ChefMR.AI;
using NaughtyAttributes;

public class TimerModule : MonoBehaviour
{
    [Header("Voice Command Input")]
    [TextArea]
    public string voiceInputOverride = "";  // Optional manual input for testing

    [Button("Process Timer Command (from Input or Voice)")]
    public void ProcessVoiceCommand(string voiceInput = null)
    {
        ChefAIFramework.Instance.ProcessTimerCommand(voiceInput, (cmd) =>
        {
            if (cmd != null)
                Debug.Log($"⏲️ {cmd.action} '{cmd.timerName}' for {cmd.durationMinutes} mins");
            else
                Debug.LogWarning("🚫 Timer command not understood.");
        });
    }
}