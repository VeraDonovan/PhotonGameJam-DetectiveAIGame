using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class DeepSeekDialogueClient : MonoBehaviour {
    public static DeepSeekDialogueClient Instance;

    [SerializeField] private string apiUrl = "https://api.deepseek.com/chat/completions";
    [SerializeField] private string model = "deepseek-v4-flash";
    [SerializeField] private int maxTokens = 300;
    [SerializeField] private int structuredMaxTokens = 600;
    [SerializeField] private float temperature = 0.7f;
    [SerializeField] private float structuredTemperature = 0.2f;
    [SerializeField] private int timeoutSeconds = 30;
    private string apiKey = "sk-4f705cf173694d5ba743a73b0aac36bf";

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    public IEnumerator SendDialogueRequest(string systemPrompt, string playerInput, Action<string> onSuccess, Action<string> onError, int? maxTokensOverride = null) {
        if (string.IsNullOrWhiteSpace(apiKey)) {
            onError?.Invoke("DeepSeek API key is missing. Paste it into DeepSeekDialogueClient.");
            yield break;
        }

        DeepSeekPlainChatRequest requestBody = new DeepSeekPlainChatRequest {
            model = model,
            max_tokens = maxTokensOverride ?? maxTokens,
            temperature = temperature,
            thinking = new DeepSeekThinkingOptions {
                type = "disabled"
            },
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
                onError?.Invoke(BuildHttpErrorMessage(request));
                yield break;
            }

            string rawHttpResponse = request.downloadHandler.text ?? string.Empty;
            DeepSeekChatResponse response = JsonUtility.FromJson<DeepSeekChatResponse>(rawHttpResponse);
            if (response == null || response.choices == null || response.choices.Length == 0 || response.choices[0].message == null) {
                onError?.Invoke("DeepSeek response did not contain a dialogue message.");
                yield break;
            }

            string content = response.choices[0].message.content ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content)) {
                content = TryExtractMessageContent(rawHttpResponse);
            }

            if (string.IsNullOrWhiteSpace(content)) {
                Debug.LogWarning("[DeepSeekDialogueClient] Could not extract plain dialogue content. Full HTTP response:\n" + rawHttpResponse, this);
            }

            onSuccess?.Invoke(content);
        }
    }

    public IEnumerator SendStructuredDialogueRequest(string systemPrompt, string userPrompt, Action<DeepSeekDialogueTurnResponse> onSuccess, Action<string> onError) {
        if (string.IsNullOrWhiteSpace(apiKey)) {
            onError?.Invoke("DeepSeek API key is missing. Paste it into DeepSeekDialogueClient.");
            yield break;
        }

        DeepSeekChatRequest requestBody = new DeepSeekChatRequest {
            model = model,
            max_tokens = structuredMaxTokens,
            temperature = structuredTemperature,
            thinking = new DeepSeekThinkingOptions {
                type = "disabled"
            },
            response_format = new DeepSeekResponseFormat {
                type = "json_object"
            },
            messages = new[] {
                new DeepSeekMessage {
                    role = "system",
                    content = systemPrompt
                },
                new DeepSeekMessage {
                    role = "user",
                    content = userPrompt
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
                onError?.Invoke(BuildHttpErrorMessage(request));
                yield break;
            }

            string rawHttpResponse = request.downloadHandler.text ?? string.Empty;
            DeepSeekChatResponse response = JsonUtility.FromJson<DeepSeekChatResponse>(rawHttpResponse);
            if (response == null || response.choices == null || response.choices.Length == 0 || response.choices[0].message == null) {
                onError?.Invoke("DeepSeek response did not contain a dialogue message.");
                yield break;
            }

            string rawDialogueContent = response.choices[0].message.content ?? string.Empty;
            if (string.IsNullOrWhiteSpace(rawDialogueContent)) {
                rawDialogueContent = TryExtractMessageContent(rawHttpResponse);
            }

            string reasoningContent = response.choices[0].message.reasoning_content ?? string.Empty;
            rawDialogueContent = NormalizeStructuredPayload(rawDialogueContent);
            Debug.Log("[DeepSeekDialogueClient] Raw structured dialogue response:\n" + rawDialogueContent, this);

            DeepSeekDialogueTurnResponse dialogueResponse;
            try {
                dialogueResponse = JsonUtility.FromJson<DeepSeekDialogueTurnResponse>(rawDialogueContent);
            } catch (ArgumentException exception) {
                onError?.Invoke(
                    "DeepSeek dialogue JSON parse failed: " +
                    exception.Message +
                    "\nFull HTTP response:\n" +
                    rawHttpResponse +
                    "\nRaw response:\n" +
                    rawDialogueContent);
                yield break;
            }

            if (dialogueResponse == null || dialogueResponse.interpretation == null || dialogueResponse.response == null || string.IsNullOrWhiteSpace(dialogueResponse.response.prose)) {
                onError?.Invoke(
                    "DeepSeek dialogue message did not match the structured dialogue schema." +
                    (string.IsNullOrWhiteSpace(rawDialogueContent) && !string.IsNullOrWhiteSpace(reasoningContent)
                        ? "\nProvider returned reasoning_content but no final message.content."
                        : string.Empty) +
                    "\nFull HTTP response:\n" +
                    rawHttpResponse +
                    "\nRaw response:\n" +
                    rawDialogueContent);
                yield break;
            }

            onSuccess?.Invoke(dialogueResponse);
        }
    }

    private static string NormalizeStructuredPayload(string rawContent) {
        string content = (rawContent ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(content)) {
            return string.Empty;
        }

        if (content.StartsWith("```")) {
            int firstNewline = content.IndexOf('\n');
            if (firstNewline >= 0) {
                content = content.Substring(firstNewline + 1);
            }

            int closingFence = content.LastIndexOf("```", StringComparison.Ordinal);
            if (closingFence >= 0) {
                content = content.Substring(0, closingFence);
            }
        }

        int firstBrace = content.IndexOf('{');
        int lastBrace = content.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace) {
            content = content.Substring(firstBrace, lastBrace - firstBrace + 1);
        }

        return content.Trim();
    }

    private static string TryExtractMessageContent(string rawHttpResponse) {
        if (string.IsNullOrWhiteSpace(rawHttpResponse)) {
            return string.Empty;
        }

        string content = TryExtractJsonStringValue(rawHttpResponse, "\"content\":\"");
        if (!string.IsNullOrWhiteSpace(content)) {
            return content;
        }

        return TryExtractJsonStringValue(rawHttpResponse, "\"text\":\"");
    }

    private static string BuildHttpErrorMessage(UnityWebRequest request) {
        if (request == null) {
            return "Unknown HTTP error.";
        }

        string errorMessage = request.error ?? "HTTP request failed.";
        string responseBody = request.downloadHandler != null
            ? request.downloadHandler.text ?? string.Empty
            : string.Empty;

        if (string.IsNullOrWhiteSpace(responseBody)) {
            return errorMessage;
        }

        return errorMessage + "\nResponse body:\n" + responseBody;
    }

    private static string TryExtractJsonStringValue(string source, string marker) {
        int markerIndex = source.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0) {
            return string.Empty;
        }

        int startIndex = markerIndex + marker.Length;
        var builder = new StringBuilder();
        bool escaping = false;
        for (int i = startIndex; i < source.Length; i++) {
            char character = source[i];
            if (escaping) {
                builder.Append(DecodeJsonEscape(character));
                escaping = false;
                continue;
            }

            if (character == '\\') {
                escaping = true;
                continue;
            }

            if (character == '"') {
                return builder.ToString();
            }

            builder.Append(character);
        }

        return string.Empty;
    }

    private static char DecodeJsonEscape(char character) {
        return character switch {
            '"' => '"',
            '\\' => '\\',
            '/' => '/',
            'b' => '\b',
            'f' => '\f',
            'n' => '\n',
            'r' => '\r',
            't' => '\t',
            _ => character,
        };
    }
}

[Serializable]
public class DeepSeekChatRequest {
    public string model;
    public DeepSeekMessage[] messages;
    public int max_tokens;
    public float temperature;
    public DeepSeekThinkingOptions thinking;
    public DeepSeekResponseFormat response_format;
}

[Serializable]
public class DeepSeekPlainChatRequest {
    public string model;
    public DeepSeekMessage[] messages;
    public int max_tokens;
    public float temperature;
    public DeepSeekThinkingOptions thinking;
}

[Serializable]
public class DeepSeekMessage {
    public string role;
    public string content;
    public string reasoning_content;
}

[Serializable]
public class DeepSeekThinkingOptions {
    public string type;
}

[Serializable]
public class DeepSeekResponseFormat {
    public string type;
}

[Serializable]
public class DeepSeekChatResponse {
    public DeepSeekChoice[] choices;
}

[Serializable]
public class DeepSeekChoice {
    public DeepSeekMessage message;
}

[Serializable]
public class DeepSeekDialogueTurnResponse {
    public DeepSeekDialogueInterpretation interpretation;
    public DeepSeekDialogueResponse response;
}

[Serializable]
public class DeepSeekDialogueInterpretation {
    public string topicId;
    public float confidence;
    public bool isIrrelevant;
}

[Serializable]
public class DeepSeekDialogueResponse {
    public string prose;
    public string usedBeatId;
    public string usedStatementId;
    public string[] usedRevealIds;
}
