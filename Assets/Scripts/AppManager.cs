using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using static TranslateAI;

public class AppManager : MonoBehaviour
{
    public static AppManager Instance { private set; get; } = null;

    public AIType eCurrentAI = AIType.Claude;

    public ClaudeAPIData claudeAPIData = null;

    [SerializeField] Text txtResult;
    [SerializeField] InputField inputMessage;

    [Space(20f)]
    [Header("--------Claude AI Controller--------")]
    [SerializeField] ClaudeAIController claudeAIController;

    bool _isTouch = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        switch (eCurrentAI)
        {
            case AIType.Claude: claudeAPIData = new ClaudeAPIData(); break;
            case AIType.ChatGPT:
                break;
        }
    }

    public void OnClickSendMessage(int idx)
    {
        if (_isTouch) return;

        _isTouch = true;

        SendMessage((AIType)idx, inputMessage.text, (res) =>
        {
            txtResult.text = res;
            _isTouch = false;
        });
    }

    void SendMessage(AIType eAIType, string userMessage, UnityAction<string> callback)
    {
        eCurrentAI = eAIType;
        switch (eCurrentAI)
        {
            case AIType.Claude: claudeAIController.SendMessageToClaude(userMessage, callback); break;
            case AIType.ChatGPT: break;
        }
    }
}
