using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using static DeepLTranslatorController;

public class DeepLTranslatorController : MonoBehaviour
{
    [Header("----------���� ����----------")]
    //[SerializeField] TargetLanguage eTargetLanguage = TargetLanguage.EN;
    [SerializeField] bool bUseFreeTier = true;
    [SerializeField] bool preserveFormatting = true;     // ���� ���� ����

    string ApiUrl => bUseFreeTier ? Define.DEEPL_API_URL_FREE : Define.DEEPL_API_URL_PRO;

    #region ���� ���� �۵��ϴ��� �׽�Ʈ�뵵
    public void OnClickTest()
    {
        TranslateText("�̾��� ����ũ�� ���� �۵��ϴ��� Ȯ�����ּ���.", TranslateCompleted);
    }
    #endregion

    void TranslateCompleted(string translatedText)
    {
        Debug.Log($"translatedText : {translatedText}");
    }

    /// <summary>
    /// ���� �ؽ�Ʈ ���� �޼���
    /// </summary>
    public void TranslateText(string text, UnityAction<string> callback)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            callback?.Invoke("");
            return;
        }

        StartCoroutine(CoTranslate(text, callback));
    }

    /// <summary>
    /// ���� �ؽ�Ʈ ���� �ڷ�ƾ
    /// </summary>
    IEnumerator CoTranslate(string text, UnityAction<string> callback)
    {
        // HTTP ��û �غ�
        using (UnityWebRequest request = CreateTranslationRequest(new string[] { text }))
        {
            // ��û ���� �� �Ϸ� ���
            yield return request.SendWebRequest();

            // ��û ��� ó��
            ProcessTranslationResponse(request, (translations) => {
                if (translations != null && translations.Length > 0)
                {
                    callback?.Invoke(translations[0]);
                }
                else
                {
                    callback?.Invoke(null);
                }
            });
        }
    }

    /// <summary>
    /// ���� �ؽ�Ʈ �ϰ� ����
    /// </summary>
    public void TranslateBatch(string[] texts, UnityAction<string[]> callbacks)
    {
        if (texts == null || texts.Length == 0)
        {
            callbacks?.Invoke(new string[0]);
            return;
        }

        StartCoroutine(CoTranslateBatch(texts, callbacks));
    }

    /// <summary>
    /// ���� �ؽ�Ʈ �ϰ� ���� �ڷ�ƾ
    /// </summary>
    IEnumerator CoTranslateBatch(string[] texts, UnityAction<string[]> callbacks)
    {
        using (UnityWebRequest request = CreateTranslationRequest(texts))
        {
            yield return request.SendWebRequest();  
            ProcessTranslationResponse(request, callbacks);   // ��û ��� ó��
        }
    }
    /// <summary>
    /// DeepL API ��û ����
    /// </summary>
    private UnityWebRequest CreateTranslationRequest(string[] texts)
    {
        // ��û ���� ����: form-data (x-www-form-urlencoded)
        WWWForm form = new WWWForm();
        TargetLanguage eTargetLanguage = AppManager.Instance.eCurrentTargetLanguage;

        // ������ �ؽ�Ʈ �߰� (���� �� ����)
        foreach (string text in texts)
        {
            form.AddField("text", text);
        }

        // Ÿ�� ��� ����
        form.AddField("target_lang", eTargetLanguage.ToString());

        // �ɼ�: ���� ����
        if (preserveFormatting)
        {
            form.AddField("preserve_formatting", "1");
        }

        UnityWebRequest request = UnityWebRequest.Post(ApiUrl, form);                               // POST ��û ����
        request.SetRequestHeader("Authorization", $"DeepL-Auth-Key {Define.DEEPL_API_KEY}");        // ��� ����: API Ű ����

        // ����׿� �α�
        Debug.Log($"DeepL API ��û: {ApiUrl}");
        Debug.Log($"Ÿ�� ���: {eTargetLanguage}");

        return request;
    }

    /// <summary>
    /// DeepL API ���� ó��
    /// </summary>
    void ProcessTranslationResponse(UnityWebRequest request, UnityAction<string[]> callback)
    {
        // ��û ���� ó��
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"DeepL API ����: {request.error}");
            Debug.LogError($"���� ���� �ڵ�: {request.responseCode}");
            Debug.LogError($"���� ����: {request.downloadHandler.text}");
            callback?.Invoke(null);
            return;
        }

        string responseBody = request.downloadHandler.text;
        Debug.Log($"DeepL API ����: {responseBody}");

        try
        {
            // JSON ���� �Ľ�
            TranslatorTool.DeepLResponse response = JsonConvert.DeserializeObject<TranslatorTool.DeepLResponse>(responseBody);

            if (response != null && response.translations != null && response.translations.Length > 0)
            {
                // ���� ��� ����
                string[] translations = new string[response.translations.Length];

                for (int i = 0; i < response.translations.Length; i++)
                {
                    translations[i] = response.translations[i].text;
                }

                callback?.Invoke(translations);
            }
            else
            {
                Debug.LogError("DeepL API ���信 ���� ����� �����ϴ�.");
                callback?.Invoke(null);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"DeepL API ���� �Ľ� ����: {ex.Message}");
            Debug.LogError($"���� ����: {responseBody}");
            callback?.Invoke(null);
        }
    }

    #region DEEPL ��뷮 Ȯ��
    // API ��뷮 Ȯ�� �޼���
    public void CheckUsage(UnityAction<string> callback)
    {
        StartCoroutine(CheckUsageCoroutine(callback));
    }

    private IEnumerator CheckUsageCoroutine(UnityAction<string> callback)
    {
        string usageUrl = bUseFreeTier
            ? "https://api-free.deepl.com/v2/usage"
            : "https://api.deepl.com/v2/usage";

        // GET ��û ����
        UnityWebRequest request = UnityWebRequest.Get(usageUrl);

        // ��� ����: API Ű ����
        request.SetRequestHeader("Authorization", $"DeepL-Auth-Key {Define.DEEPL_API_KEY}");

        // ��û ���� �� �Ϸ� ���
        yield return request.SendWebRequest();

        // ��û ��� ó��
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"DeepL ��뷮 Ȯ�� ����: {request.error}");
            callback?.Invoke(null);
            yield break;
        }

        // ��� ��ȯ
        callback?.Invoke(request.downloadHandler.text);
    }
    #endregion
}
