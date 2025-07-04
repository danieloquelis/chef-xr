using UnityEngine;
using NaughtyAttributes;
using ChefMR.AI;

public class VoiceModule : MonoBehaviour
{
    [Header("Voice Settings")]
    [Tooltip("Duration to record and transcribe audio")]
    public float recordDuration = 5f;

    [Button("Start Voice Recording")]
    public void StartRecording()
    {
        ChefAIFramework.Instance.StartVoiceRecording();
    }

    [Button("Stop Voice Recording")]
    public void StopRecording()
    {
        ChefAIFramework.Instance.StopVoiceRecording((transcript) =>
        {
            Debug.Log($"🎙️ Transcript: {transcript}");
            // TODO: Use transcript for command parsing or UI feedback
            // You can pass this to TimerModule or trigger commands from here
        });
    }

    [Button("Record & Transcribe (One Shot)")]
    public void RecordAndTranscribe(float duration = 5f)
    {
        ChefAIFramework.Instance.RecordAndTranscribe(duration, (transcript) =>
        {
            Debug.Log($"🗣️ One-Shot Voice result: {transcript}");
        });
    }
}