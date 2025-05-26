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

    [Header("----AI ����----")][Space(5f)]
    public TranslatorTools eCurrentTool = TranslatorTools.DeepL;

    [Header("----���� Ÿ��----")][Space(5f)]
    public TargetLanguage eCurrentTargetLanguage = TargetLanguage.EN;

    [Header("----���� ����----")][Space(5f)]
    public int maxConcurrentTranslations = 3;       // ���ÿ� ó���� �ִ� ���� ��
    public float delay = 1f;                        // ��û ������ ���� �ð�(��)

    [Header("----------------")][Space(10f)]
    [SerializeField] Text txtResult;
    [SerializeField] InputField inputMessage;
    [SerializeField] Button[] arrloadButtons;       // csv �ε� ��ư
    [SerializeField] Button btnTranslator;           // �����ϱ� ��ư

    [Header("-------���� ����ϰ� �ִ� Tool------")][Space(10f)]
    [SerializeField] Text txtTranslating;
    [SerializeField] Text txtUsingToolName;

    [Header("-------���� ���� ������ UI ����------")][Space(10f)]
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
    int _translatedCount = 0;       // ���� �Ϸ�� ����

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
            case TranslatorTools.DeepL:   txtUsingToolName.text = $"[  DeepL ������  ] ��� ��.."; break;
            case TranslatorTools.Claude:  txtUsingToolName.text = $"[  Claude AI  ] ��� ��.."; break;            
            case TranslatorTools.ChatGPT: txtUsingToolName.text = $"[  CHAT GPT AI  ] ��� ��.."; break;
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
        Debug.Log($"{csvFileName} ���� �۾� ����...");
        txtTranslating.text = $"{csvFileName} ���� �۾� ����...";
        TranslatorButtonInteratable(false);

        var itemsToTranslate = new List<KeyValuePair<int, string>>();
        var koreanTextDatas = localizationManager.KoreanTextDatas;
        var englishTextDatas = localizationManager.EnglishTextDatas;
        int totalItems = localizationManager.TotalTranslateCount;

        foreach (var item in koreanTextDatas)
        {
            // �̹� ���� ������ �ִٸ� �ǳʶٱ�
            if (englishTextDatas.ContainsKey(item.Key) && !string.IsNullOrWhiteSpace(englishTextDatas[item.Key]))
            {
                _translatedCount++;
                UpdateTranslationUI(_translatedCount, totalItems);
                continue;
            }

            itemsToTranslate.Add(new KeyValuePair<int, string>(item.Key, item.Value));
        }

        // ������ �ʿ� ������ �ڷ�ƾ Ż��
        if (itemsToTranslate.Count == 0)
        {
            _isTranslating = false;
            Debug.Log("��� �׸��� �̹� �����Ǿ� �ֽ��ϴ�.");
            txtTranslating.text = $"��� �׸��� �̹� �����Ǿ� �ֽ��ϴ�.";
            UpdateTranslationUI(totalItems, totalItems);
            yield break;
        }

        // ���� ���� ó���� ���� ����
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

                // �ִ� ���� ���� ����ŭ ���� ��û ����
                for (int i = 0; i < batchSize && currentIndex < itemsToTranslate.Count; i++)
                {
                    var item = itemsToTranslate[currentIndex];
                    Coroutine coTranslation = StartCoroutine(CoTranslateItem(item.Key, item.Value));
                    listConCurrentTranslation.Add(coTranslation);

                    currentIndex++;

                    yield return new WaitForSeconds(delay / batchSize);  // API ��û ���̿� ª�� ������ �ֱ�
                }

                foreach (var coroutine in listConCurrentTranslation)
                {
                    yield return coroutine;
                }
            }
        }

        Debug.Log("��� �׸� ���� �Ϸ�!");
        txtTranslating.text = $"{localizationManager.ListCSVFileName[currentCSVFileIdx]} ��� �׸� ���� �Ϸ�!";

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
            // ���� ��ġ ũ�� ���
            int currentBatchSize = Math.Min(batchSize, itemsToTranslate.Count - currentIndex);

            // ��ġ�� ���Ե� �׸��
            List<int> batchKeys = new List<int>(currentBatchSize);
            List<string> batchTexts = new List<string>(currentBatchSize);

            // ��ġ ������ �غ�
            for (int i = 0; i < currentBatchSize; i++)
            {
                var item = itemsToTranslate[currentIndex + i];
                batchKeys.Add(item.Key);
                batchTexts.Add(item.Value);
            }

            // DeepL �ϰ� ���� ��û
            bool batchComplete = false;
            string[] translatedTexts = null;

            deepLTranslatorController.TranslateBatch(batchTexts.ToArray(), (results) => 
            {
                translatedTexts = results;
                batchComplete = true;
            });

            // ���� �Ϸ� ���
            while (!batchComplete)
            {
                yield return null;
            }

            // ���� ��� ó��
            if (translatedTexts != null && translatedTexts.Length == batchTexts.Count)
            {
                for (int i = 0; i < translatedTexts.Length; i++)
                {
                    int key = batchKeys[i];
                    string koreanText = batchTexts[i];
                    string translatedText = translatedTexts[i];

                    translatedText = TextFormatHelper.CapitalizeFirstLetter(translatedText);    // ù ���� �빮�� ��ȯ    
                    localizationManager.SetTranslationComplatedText(key, translatedText);       // ���� ��� ����

                    Debug.Log($"�׸� ���� �Ϸ� <color=cyan>(Key: {key})</color>: {koreanText} -> {translatedText}");
                    txtTranslating.text = $"�׸� ���� �Ϸ� <color=cyan>(Key: {key})</color>: {koreanText} -> {translatedText}";
                }

                // ���� ���� ������Ʈ
                _translatedCount += translatedTexts.Length;
                UpdateTranslationUI(_translatedCount, localizationManager.TotalTranslateCount);
            }
            else
            {
                Debug.LogError("��ġ ���� ��� ����: ��� ���� ��û ���� ��ġ���� �ʽ��ϴ�.");
                txtTranslating.text = "��ġ ���� ��� ����: ��� ���� ��û ���� ��ġ���� �ʽ��ϴ�.";
            }

            // �ε��� �̵�
            currentIndex += currentBatchSize;

            // API ������ ���� ������
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

    #region AI �̿��ϴ� ���
    IEnumerator CoTranslateItem(int key, string koreanText)
    {
        bool bComplete = false;
        string translatedText = "";

        string promptTemplate = "{0}��(��) ����� �����ؼ� ������ ����� �˷���. �����̳� �ٸ� ���� ���� ���� ������ ����� �ۼ���. �߰��� ���� ��� ���� ���� ��� �ظ��ϸ� ����ؼ� ��Ī�ؼ� ��������";
        string prompt = string.Format(promptTemplate, koreanText);

        SendMessage(eCurrentTool, prompt, (res) => 
        {
            translatedText = res;
            bComplete = true;
        });

        // ���� �Ϸ� �� ������ ���
        while (!bComplete) yield return null;

        if (!string.IsNullOrEmpty(translatedText))
        {
            localizationManager.SetTranslationComplatedText(key, translatedText);  // ���� ��� ����

            _translatedCount++;  // ���� ���� ������Ʈ
            UpdateTranslationUI(_translatedCount, localizationManager.TotalTranslateCount);

            Debug.Log($"�׸� ���� �Ϸ� <color=cyan>(Key: {key})</color>: {koreanText} -> {translatedText}");
        }
        else
        {
            Debug.LogError($"�׸� ���� ���� (Key: {key}): {koreanText}");
        }
    }

    // AI �׽�Ʈ��
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

    #region ����
    void UpdateTranslationUI(int current, int total)
    {
        txtTranslationStatus.text = $"���� ����: {current}/{total} ({(total > 0 ? (current * 100f / total) : 0):F1}%)";

        if (imgTranslationGauge != null)
        {
            float targetFillAmount = total > 0 ? (float)current / total : 0;
            imgTranslationGauge.DOFillAmount(targetFillAmount, 0.5f).SetEase(Ease.OutQuad); // �ִϸ��̼� ��¡ ����
        }
    }

    bool IsItTranslatable()
    {
        if (_isTranslating)
        {
            Debug.LogWarning("�̹� ���� �۾��� ���� ���Դϴ�.");
            return false;
        }

        switch (eCurrentTool)
        {
            case TranslatorTools.DeepL:
                if (deepLTranslatorController == null)
                {
                    Debug.LogError("DeepLTranslatorController�� �����ϴ�. ������ ������ �� �����ϴ�.");
                    return false;
                }
                break;
            case TranslatorTools.Claude:
                if (claudeAIController == null)
                {
                    Debug.LogError("ClaudeAIController�� �����ϴ�. ������ ������ �� �����ϴ�.");
                    return false;
                }
                break;
            case TranslatorTools.ChatGPT:
                Debug.LogWarning("ChatGPT ����� ���� �������� �ʾҽ��ϴ�.");
                return false;
            default:
                Debug.LogError("�������� �ʴ� ������ �Դϴ�.");
                return false;
        }

        if (localizationManager == null)
        {
            Debug.LogError("LocalizationManager�� �����ϴ�. ������ ������ �� �����ϴ�.");
            return false;
        }

        if (localizationManager.KoreanTextDatas == null || localizationManager.KoreanTextDatas.Count <= 0)
        {
            Debug.LogWarning("������ �ѱ��� �����Ͱ� �����ϴ�. ���� CSV ������ �ε��ϼ���.");
            return false;
        }

        return true;
    }
    #endregion
}
