using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using static TranslatorTool;
using Sirenix.OdinInspector;
using DG.Tweening;
using System.Text;
using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using System.ComponentModel;

public class AppManager : MonoBehaviour
{
    public static AppManager Instance { private set; get; } = null;

    [Header("----AI 선택----")][Space(5f)]
    public TranslatorTools eCurrentTool = TranslatorTools.DeepL;

    [Header("----번역 타겟----")][Space(5f)]
    public TargetLanguage eCurrentTargetLanguage = TargetLanguage.EN;

    [Header("----번역 설정----")][Space(5f)]
    public int maxConcurrentTranslations = 3;       // 동시에 처리할 최대 번역 수
    public float delay = 1f;                        // 요청 사이의 지연 시간(초)

    [Header("----------------")][Space(10f)]
    [SerializeField] Text txtResult;
    [SerializeField] InputField inputMessage;
    [SerializeField] Button[] arrloadButtons;       // csv 로드 버튼
    [SerializeField] Button btnTranslator;           // 번역하기 버튼

    [Header("-------현재 사용하고 있는 Tool------")][Space(10f)]
    [SerializeField] Text txtTranslating;
    [SerializeField] Text txtUsingToolName;

    [Header("-------번역 상태 관련한 UI 변수------")][Space(10f)]
    [SerializeField] Text txtTranslationStatus;
    [SerializeField] Image imgTranslationGauge;

    [Space(20f)]
    [Header("--------Localization Manager--------")]
    [SerializeField] LocalizationManager localizationManager;

    [Space(20f)]
    [Header("--------DeepL Controller--------")]
    [SerializeField] DeepLTranslatorController deepLTranslatorController;

    [Space(20f)]
    [Header("--------Claude AI Controller--------")]
    [SerializeField] ClaudeAIController claudeAIController;

    [SerializeField] public ClaudeAPIData claudeAPIData = null;

    public LocalizationManager LocalizationManager => localizationManager;

    bool _isTouch = false;

    bool _isTranslating = false;
    int _translatedCount = 0;       // 번역 완료된 개수

    void Awake()
    {
        if (Instance == null) Instance = this;

        imgTranslationGauge.fillAmount = 0f;
    }

    void Start()
    {
        //localizationManager.Init();
        TranslatorButtonInteratable(false);

        switch (eCurrentTool)
        {
            case TranslatorTools.DeepL:   txtUsingToolName.text = $"[  DeepL 번역기  ] 사용 중.."; break;
            case TranslatorTools.Claude:  txtUsingToolName.text = $"[  Claude AI  ] 사용 중.."; break;            
            case TranslatorTools.ChatGPT: txtUsingToolName.text = $"[  CHAT GPT AI  ] 사용 중.."; break;
        }
    }

    public void StartTranslation()
    {
        if (!IsItTranslatable()) return;

        _isTranslating = true;
        _translatedCount = 0;
        
        UpdateTranslationUI(0, localizationManager.TotalTranslateCount);

        DOTween.KillAll();

        StartCoroutine(Co_AllItemTranslate());
    }

    IEnumerator Co_AllItemTranslate()
    {
        int currentCSVFileIdx = localizationManager.currentCSVFileIndex;
        string csvFileName = localizationManager.ListCSVFileName[currentCSVFileIdx];
        Debug.Log($"{csvFileName} 번역 작업 시작...");
        txtTranslating.text = $"{csvFileName} 번역 작업 시작...";
        TranslatorButtonInteratable(false);

        var itemsToTranslate = new List<KeyValuePair<int, string>>();
        var koreanTextDatas = localizationManager.KoreanTextDatas;
        var englishTextDatas = localizationManager.EnglishTextDatas;
        int totalItems = localizationManager.TotalTranslateCount;

        foreach (var item in koreanTextDatas)
        {
            // 이미 영어 번역이 있다면 건너뛰기
            if (englishTextDatas.ContainsKey(item.Key) && !string.IsNullOrWhiteSpace(englishTextDatas[item.Key]))
            {
                _translatedCount++;
                UpdateTranslationUI(_translatedCount, totalItems);
                continue;
            }

            itemsToTranslate.Add(new KeyValuePair<int, string>(item.Key, item.Value));
        }

        // 번역이 필요 없으면 코루틴 탈출
        if (itemsToTranslate.Count == 0)
        {
            _isTranslating = false;
            Debug.Log("모든 항목이 이미 번역되어 있습니다.");
            txtTranslating.text = $"모든 항목이 이미 번역되어 있습니다.";
            UpdateTranslationUI(totalItems, totalItems);
            yield break;
        }

        // 동시 번역 처리를 위한 설정
        int batchSize = maxConcurrentTranslations;
        int currentIndex = 0;

        if (eCurrentTool == TranslatorTools.DeepL)
        {
            yield return StartCoroutine(Co_DeepLBatchTranslate(itemsToTranslate, batchSize));
        }
        else
        {
            while (currentIndex < itemsToTranslate.Count)
            {
                List<Coroutine> listConCurrentTranslation = new List<Coroutine>();

                // 최대 동시 번역 수만큼 번역 요청 시작
                for (int i = 0; i < batchSize && currentIndex < itemsToTranslate.Count; i++)
                {
                    var item = itemsToTranslate[currentIndex];
                    Coroutine coTranslation = StartCoroutine(CoTranslateItem(item.Key, item.Value));
                    listConCurrentTranslation.Add(coTranslation);

                    currentIndex++;

                    yield return new WaitForSeconds(delay / batchSize);  // API 요청 사이에 짧은 딜레이 주기
                }

                foreach (var coroutine in listConCurrentTranslation)
                {
                    yield return coroutine;
                }
            }
        }

        Debug.Log("모든 항목 번역 완료!");
        txtTranslating.text = $"{localizationManager.ListCSVFileName[currentCSVFileIdx]} 모든 항목 번역 완료!";

        yield return null;
        localizationManager.SaveTranslatedCSV();
        _isTranslating = false;
        AllLoadButtonInteratable(true);        
    }

    public IEnumerator Co_DeepLBatchTranslate(List<KeyValuePair<int, string>> itemsToTranslate, int batchSize)
    {
        int currentIndex = 0;

        while (currentIndex < itemsToTranslate.Count)
        {
            // 현재 배치 크기 계산
            int currentBatchSize = Math.Min(batchSize, itemsToTranslate.Count - currentIndex);

            // 배치에 포함될 항목들
            List<int> batchKeys = new List<int>(currentBatchSize);
            List<string> batchTexts = new List<string>(currentBatchSize);

            // 배치 데이터 준비
            for (int i = 0; i < currentBatchSize; i++)
            {
                var item = itemsToTranslate[currentIndex + i];
                batchKeys.Add(item.Key);
                batchTexts.Add(item.Value);
            }

            // DeepL 일괄 번역 요청
            bool batchComplete = false;
            string[] translatedTexts = null;

            deepLTranslatorController.TranslateBatch(batchTexts.ToArray(), (results) => 
            {
                translatedTexts = results;
                batchComplete = true;
            });

            // 번역 완료 대기
            while (!batchComplete)
            {
                yield return null;
            }

            // 번역 결과 처리
            if (translatedTexts != null && translatedTexts.Length == batchTexts.Count)
            {
                for (int i = 0; i < translatedTexts.Length; i++)
                {
                    int key = batchKeys[i];
                    string koreanText = batchTexts[i];
                    string translatedText = translatedTexts[i];

                    translatedText = TextFormatHelper.CapitalizeFirstLetter(translatedText);    // 첫 글자 대문자 변환    
                    localizationManager.SetTranslationComplatedText(key, translatedText);       // 번역 결과 저장

                    Debug.Log($"항목 번역 완료 <color=cyan>(Key: {key})</color>: {koreanText} -> {translatedText}");
                    txtTranslating.text = $"항목 번역 완료 <color=cyan>(Key: {key})</color>: {koreanText} -> {translatedText}";
                }

                // 진행 상태 업데이트
                _translatedCount += translatedTexts.Length;
                UpdateTranslationUI(_translatedCount, localizationManager.TotalTranslateCount);
            }
            else
            {
                Debug.LogError("배치 번역 결과 오류: 결과 수가 요청 수와 일치하지 않습니다.");
                txtTranslating.text = "배치 번역 결과 오류: 결과 수가 요청 수와 일치하지 않습니다.";
            }

            // 인덱스 이동
            currentIndex += currentBatchSize;

            // API 제한을 위한 딜레이
            yield return new WaitForSeconds(delay);
        }

        yield return null;

        UpdateTranslationUI(0, 0);
    }

    public void AllLoadButtonInteratable(bool bInteractable)
    {
        for (int i = 0; i < arrloadButtons.Length; i++)
        {
            arrloadButtons[i].interactable = bInteractable;
        }
    }

    public void TranslatorButtonInteratable(bool bInteratable)
    {
        btnTranslator.interactable = bInteratable;
    }

    #region AI 이용하는 방식
    IEnumerator CoTranslateItem(int key, string koreanText)
    {
        bool bComplete = false;
        string translatedText = "";

        string promptTemplate = "{0}을(를) 영어로 번역해서 번역된 결과만 알려줘. 설명이나 다른 문장 없이 오직 번역된 결과만 작성해. 추가로 엔터 띄어 쓰기 같은 경우 왠만하면 고려해서 매칭해서 번역해줘";
        string prompt = string.Format(promptTemplate, koreanText);

        SendMessage(eCurrentTool, prompt, (res) => 
        {
            translatedText = res;
            bComplete = true;
        });

        // 번역 완료 될 때까지 대기
        while (!bComplete) yield return null;

        if (!string.IsNullOrEmpty(translatedText))
        {
            localizationManager.SetTranslationComplatedText(key, translatedText);  // 번역 결과 저장

            _translatedCount++;  // 진행 상태 업데이트
            UpdateTranslationUI(_translatedCount, localizationManager.TotalTranslateCount);

            Debug.Log($"항목 번역 완료 <color=cyan>(Key: {key})</color>: {koreanText} -> {translatedText}");
        }
        else
        {
            Debug.LogError($"항목 번역 실패 (Key: {key}): {koreanText}");
        }
    }

    // AI 테스트용
    public void OnClickSendMessage(int idx)
    {
        if (_isTouch) return;

        _isTouch = true;

        SendMessage((TranslatorTools)idx, inputMessage.text, (res) =>
        {
            txtResult.text = res;
            _isTouch = false;
        });
    }

    void SendMessage(TranslatorTools eAIType, string userMessage, UnityAction<string> callback)
    {
        eCurrentTool = eAIType;
        switch (eCurrentTool)
        {
            case TranslatorTools.Claude: claudeAIController.SendMessageToClaude(userMessage, callback); break;
            case TranslatorTools.ChatGPT: break;
        }
    }
    #endregion

    #region 번역
    void UpdateTranslationUI(int current, int total)
    {
        txtTranslationStatus.text = $"번역 진행: {current}/{total} ({(total > 0 ? (current * 100f / total) : 0):F1}%)";

        if (imgTranslationGauge != null)
        {
            float targetFillAmount = total > 0 ? (float)current / total : 0;
            imgTranslationGauge.DOFillAmount(targetFillAmount, 0.5f).SetEase(Ease.OutQuad); // 애니메이션 이징 설정
        }
    }

    bool IsItTranslatable()
    {
        if (_isTranslating)
        {
            Debug.LogWarning("이미 번역 작업이 진행 중입니다.");
            return false;
        }

        switch (eCurrentTool)
        {
            case TranslatorTools.DeepL:
                if (deepLTranslatorController == null)
                {
                    Debug.LogError("DeepLTranslatorController가 없습니다. 번역을 시작할 수 없습니다.");
                    return false;
                }
                break;
            case TranslatorTools.Claude:
                if (claudeAIController == null)
                {
                    Debug.LogError("ClaudeAIController가 없습니다. 번역을 시작할 수 없습니다.");
                    return false;
                }
                break;
            case TranslatorTools.ChatGPT:
                Debug.LogWarning("ChatGPT 기능은 아직 구현되지 않았습니다.");
                return false;
            default:
                Debug.LogError("지원하지 않는 번역기 입니다.");
                return false;
        }

        if (localizationManager == null)
        {
            Debug.LogError("LocalizationManager가 없습니다. 번역을 시작할 수 없습니다.");
            return false;
        }

        if (localizationManager.KoreanTextDatas == null || localizationManager.KoreanTextDatas.Count <= 0)
        {
            Debug.LogWarning("번역할 한국어 데이터가 없습니다. 먼저 CSV 파일을 로드하세요.");
            return false;
        }

        return true;
    }
    #endregion
}
