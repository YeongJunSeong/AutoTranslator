using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;

public static class Define
{
    /// <summary>
    /// 클루드 AI
    /// </summary>
    public const string CLAUDE_API_URL = "https://api.anthropic.com/v1/messages";
    public const string CLAUDE_API_KEY = "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";

    /// <summary>
    /// DEEPL 번역기
    /// </summary>
    public const string DEEPL_API_KEY = "8e65198b-ead5-4c6a-9ade-063b8cc7ed38:fx";
    public const string DEEPL_API_URL_FREE = "https://api-free.deepl.com/v2/translate";
    public const string DEEPL_API_URL_PRO = "https://api.deepl.com/v2/translate";
}

public static class TextFormatHelper
{
    /// <summary>
    /// 문장의 첫 글자를 대문자로 표현
    /// </summary>
    public static string CapitalizeFirstLetter(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // 공백을 버리고 첫 글자 찾기
        int firstCharIndex = 0;
        while (firstCharIndex < text.Length && char.IsWhiteSpace(text[firstCharIndex]))
        {
            firstCharIndex++;
        }

        if (firstCharIndex >= text.Length) return text;

        // 첫 글자만 대문자 사용
        char[] chars = text.ToCharArray();
        chars[firstCharIndex] = char.ToUpper(chars[firstCharIndex]);

        return new string(chars);
    }
}

/// <summary>
/// 로컬에 저장된 CSV 파일 명
/// </summary>
public enum CSVFile
{
    Init,
}

#region 지원 되는 언어
public enum TargetLanguage
{
    EN,     // 영어 (영국)
    EN_US,  // 영어 (미국)

    //EN_GB,  // 영어 (영국)
    //JA,     // 일본어
    //ZH,     // 중국어 (간체)
    //DE,     // 독일어
    //FR,     // 프랑스어
    //ES,     // 스페인어
    //IT,     // 이탈리아어
    //NL,     // 네덜란드어
    //PL,     // 폴란드어
    //PT_PT,  // 포르투갈어 (포르투갈)
    //PT_BR,  // 포르투갈어 (브라질)
    //RU,     // 러시아어
    //        // 기타 지원 언어...
}
#endregion

public enum TranslatorTools
{
    DeepL,        // 딥엘 번역기
    Claude,      // Claude AI
    ChatGPT,     // Chat GPT AI    
}

[Serializable]
public static class TranslatorTool
{
    #region Claude API
    [Serializable]
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

    #region DeepL Translator
    // DeepL API 응답 형식에 맞는 클래스 (JSON 역직렬화용)
    [Serializable]
    public class DeepLResponse
    {
        public Translation[] translations;
    }

    [Serializable]
    public class Translation
    {
        public string detected_source_language;
        public string text;
    }
    #endregion

    // TODO Chat GPT에 대한 API 데이터도 추가 될 수 있음
}
