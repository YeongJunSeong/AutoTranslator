using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;

public static class Define
{
    public const string CLAUDE_API_URL = "https://api.anthropic.com/v1/messages";
    public const string CLAUDE_API_KEY = "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";
}

public enum AIType
{
    Claude, 
    ChatGPT     // TODO
}

[Serializable]
public static class TranslateAI
{
    #region Claude API Data
    public class ClaudeAPIData
    {
        [Serializable]
        public class MessageRequest
        {
            public string model;
            public List<Message> messages;
            public float max_tokens;
        }

        [Serializable]
        public class Message
        {
            public string role;
            public string content;
        }

        [Serializable]
        public class ApiResponse
        {
            public string id;
            public string type;
            public string role;
            public ContentBlock[] content;
        }

        [Serializable]
        public class ContentBlock
        {
            public string type;
            public string text;
        }
    }
    #endregion
    // TODO Chat GPT에 대한 API 데이터도 추가 될 수 있음
}
