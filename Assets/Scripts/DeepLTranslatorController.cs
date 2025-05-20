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
    [Header("----------번역 설정----------")]
    //[SerializeField] TargetLanguage eTargetLanguage = TargetLanguage.EN;
    [SerializeField] bool bUseFreeTier = true;
    [SerializeField] bool preserveFormatting = true;     // 서식 보존 여부

    string ApiUrl => bUseFreeTier ? Define.DEEPL_API_URL_FREE : Define.DEEPL_API_URL_PRO;

    #region 딥엘 정상 작동하는지 테스트용도
    public void OnClickTest()
    {
        TranslateText("이어폰 마이크가 정상 작동하는지 확인해주세요.", TranslateCompleted);
    }
    #endregion

    void TranslateCompleted(string translatedText)
    {
        Debug.Log($"translatedText : {translatedText}");
    }

    /// <summary>
    /// 단일 텍스트 번역 메서드
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
    /// 단일 텍스트 번역 코루틴
    /// </summary>
    IEnumerator CoTranslate(string text, UnityAction<string> callback)
    {
        // HTTP 요청 준비
        using (UnityWebRequest request = CreateTranslationRequest(new string[] { text }))
        {
            // 요청 전송 및 완료 대기
            yield return request.SendWebRequest();

            // 요청 결과 처리
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
    /// 여러 텍스트 일괄 번역
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
    /// 여러 텍스트 일괄 번역 코루틴
    /// </summary>
    IEnumerator CoTranslateBatch(string[] texts, UnityAction<string[]> callbacks)
    {
        using (UnityWebRequest request = CreateTranslationRequest(texts))
        {
            yield return request.SendWebRequest();  
            ProcessTranslationResponse(request, callbacks);   // 요청 결과 처리
        }
    }
    /// <summary>
    /// DeepL API 요청 생성
    /// </summary>
    private UnityWebRequest CreateTranslationRequest(string[] texts)
    {
        // 요청 본문 형태: form-data (x-www-form-urlencoded)
        WWWForm form = new WWWForm();
        TargetLanguage eTargetLanguage = AppManager.Instance.eCurrentTargetLanguage;

        // 번역할 텍스트 추가 (여러 개 가능)
        foreach (string text in texts)
        {
            form.AddField("text", text);
        }

        // 타겟 언어 설정
        form.AddField("target_lang", eTargetLanguage.ToString());

        // 옵션: 서식 보존
        if (preserveFormatting)
        {
            form.AddField("preserve_formatting", "1");
        }

        UnityWebRequest request = UnityWebRequest.Post(ApiUrl, form);                               // POST 요청 생성
        request.SetRequestHeader("Authorization", $"DeepL-Auth-Key {Define.DEEPL_API_KEY}");        // 헤더 설정: API 키 인증

        // 디버그용 로그
        Debug.Log($"DeepL API 요청: {ApiUrl}");
        Debug.Log($"타겟 언어: {eTargetLanguage}");

        return request;
    }

    /// <summary>
    /// DeepL API 응답 처리
    /// </summary>
    void ProcessTranslationResponse(UnityWebRequest request, UnityAction<string[]> callback)
    {
        // 요청 실패 처리
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"DeepL API 오류: {request.error}");
            Debug.LogError($"응답 상태 코드: {request.responseCode}");
            Debug.LogError($"응답 내용: {request.downloadHandler.text}");
            callback?.Invoke(null);
            return;
        }

        string responseBody = request.downloadHandler.text;
        Debug.Log($"DeepL API 응답: {responseBody}");

        try
        {
            // JSON 응답 파싱
            TranslatorTool.DeepLResponse response = JsonConvert.DeserializeObject<TranslatorTool.DeepLResponse>(responseBody);

            if (response != null && response.translations != null && response.translations.Length > 0)
            {
                // 번역 결과 추출
                string[] translations = new string[response.translations.Length];

                for (int i = 0; i < response.translations.Length; i++)
                {
                    translations[i] = response.translations[i].text;
                }

                callback?.Invoke(translations);
            }
            else
            {
                Debug.LogError("DeepL API 응답에 번역 결과가 없습니다.");
                callback?.Invoke(null);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"DeepL API 응답 파싱 오류: {ex.Message}");
            Debug.LogError($"응답 내용: {responseBody}");
            callback?.Invoke(null);
        }
    }

    #region DEEPL 사용량 확인
    // API 사용량 확인 메서드
    public void CheckUsage(UnityAction<string> callback)
    {
        StartCoroutine(CheckUsageCoroutine(callback));
    }

    private IEnumerator CheckUsageCoroutine(UnityAction<string> callback)
    {
        string usageUrl = bUseFreeTier
            ? "https://api-free.deepl.com/v2/usage"
            : "https://api.deepl.com/v2/usage";

        // GET 요청 생성
        UnityWebRequest request = UnityWebRequest.Get(usageUrl);

        // 헤더 설정: API 키 인증
        request.SetRequestHeader("Authorization", $"DeepL-Auth-Key {Define.DEEPL_API_KEY}");

        // 요청 전송 및 완료 대기
        yield return request.SendWebRequest();

        // 요청 결과 처리
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"DeepL 사용량 확인 오류: {request.error}");
            callback?.Invoke(null);
            yield break;
        }

        // 결과 반환
        callback?.Invoke(request.downloadHandler.text);
    }
    #endregion
}
