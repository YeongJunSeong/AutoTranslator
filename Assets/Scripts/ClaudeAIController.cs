using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class ClaudeAIController : MonoBehaviour
{
    public void SendMessageToClaude(string userMessage, UnityAction<string> callback)
    {
        StartCoroutine(Co_SendMessageToClaude(userMessage, callback));
    }

    IEnumerator Co_SendMessageToClaude(string userMessage, UnityAction<string> callback)
    {
        Debug.Log($"Sending Message : {userMessage}");

        // Newtonsoft.Json�� ����Ͽ� ���� Dictionary ���� �� ����ȭ
        var requestData = new Dictionary<string, object>
        {
            { "model", "claude-3-sonnet-20240229" },
            { "max_tokens", 4096 },
            { "messages", new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string>
                    {
                        { "role", "user" },
                        { "content", userMessage }
                    }
                }
            }
        };

        // Newtonsoft.Json���� ����ȭ
        string jsonData = JsonConvert.SerializeObject(requestData);
        Debug.Log($"JSON Request: {jsonData}"); // ������ ���

        using (UnityWebRequest webRequest = new UnityWebRequest(Define.CLAUDE_API_URL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            // ��� ����
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("x-api-key", Define.CLAUDE_API_KEY);
            webRequest.SetRequestHeader("anthropic-version", "2023-06-01");

            // ���ε� ��û
            yield return webRequest.SendWebRequest();

            // ���� ��ü ���� �α� (������)
            Debug.Log($"Response Code: {webRequest.responseCode}");
            Debug.Log($"Response Text: {webRequest.downloadHandler.text}");

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    // Newtonsoft.Json���� ���� �Ľ�
                    var responseJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(webRequest.downloadHandler.text);

                    string responseText = "";
                    if (responseJson.ContainsKey("content"))
                    {
                        var contentArray = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(responseJson["content"].ToString());
                        foreach (var contentItem in contentArray)
                        {
                            if (contentItem["type"].ToString() == "text")
                            {
                                responseText += contentItem["text"].ToString();
                            }
                        }
                    }

                    callback(responseText);
                    Debug.Log($"Claude Response Text: {responseText}");
                }
                catch (Exception ex)
                {
                    string errorMsg = $"���� �Ľ� ����: {ex.Message}";
                    Debug.LogError(errorMsg);
                    callback(errorMsg);
                }
            }
            else
            {
                string errorDetails = webRequest.downloadHandler.text;
                string errorMsg = $"API Request Error: {webRequest.error}\n���� ����: {errorDetails}";
                Debug.LogError(errorMsg);
                callback(errorMsg);
            }
        }
    }
}
