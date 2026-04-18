using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class DeepSeekDialogueClient : MonoBehaviour {
    public static DeepSeekDialogueClient Instance;

    [SerializeField] private string apiUrl = "https://api.deepseek.com/chat/completions";
    [SerializeField] private string model = "deepseek-chat";
    [SerializeField] private int maxTokens = 300;
    [SerializeField] private float temperature = 0.7f;
    [SerializeField] private int timeoutSeconds = 30;

    private const string ApiKeyEnvironmentVariable = "DEEPSEEK_API_KEY";

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    public IEnumerator SendDialogueRequest(string systemPrompt, string playerInput, Action<string> onSuccess, Action<string> onError) {
        string apiKey = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable, EnvironmentVariableTarget.User);
        if (string.IsNullOrWhiteSpace(apiKey)) {
            onError?.Invoke("DeepSeek API key is missing. Set the DEEPSEEK_API_KEY environment variable.");
            yield break;
        }

        DeepSeekChatRequest requestBody = new DeepSeekChatRequest {
            model = model,
            max_tokens = maxTokens,
            temperature = temperature,
            messages = new[] {
                new DeepSeekMessage {
                    role = "system",
                    content = systemPrompt
                },
                new DeepSeekMessage {
                    role = "user",
                    content = playerInput
                }
            }
        };

        string jsonBody = JsonUtility.ToJson(requestBody);
        byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, UnityWebRequest.kHttpVerbPOST)) {
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = timeoutSeconds;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) {
                onError?.Invoke(request.error);
                yield break;
            }

            DeepSeekChatResponse response = JsonUtility.FromJson<DeepSeekChatResponse>(request.downloadHandler.text);
            if (response == null || response.choices == null || response.choices.Length == 0 || response.choices[0].message == null) {
                onError?.Invoke("DeepSeek response did not contain a dialogue message.");
                yield break;
            }

            onSuccess?.Invoke(response.choices[0].message.content);
        }
    }
}

[Serializable]
public class DeepSeekChatRequest {
    public string model;
    public DeepSeekMessage[] messages;
    public int max_tokens;
    public float temperature;
}

[Serializable]
public class DeepSeekMessage {
    public string role;
    public string content;
}

[Serializable]
public class DeepSeekChatResponse {
    public DeepSeekChoice[] choices;
}

[Serializable]
public class DeepSeekChoice {
    public DeepSeekMessage message;
}
