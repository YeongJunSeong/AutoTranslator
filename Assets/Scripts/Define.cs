using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;

public static class Define
{
    /// <summary>
    /// Ŭ��� AI
    /// </summary>
    public const string CLAUDE_API_URL = "https://api.anthropic.com/v1/messages";
    public const string CLAUDE_API_KEY = "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";

    /// <summary>
    /// DEEPL ������
    /// </summary>
    public const string DEEPL_API_KEY = "8e65198b-ead5-4c6a-9ade-063b8cc7ed38:fx";
    public const string DEEPL_API_URL_FREE = "https://api-free.deepl.com/v2/translate";
    public const string DEEPL_API_URL_PRO = "https://api.deepl.com/v2/translate";
}

public static class TextFormatHelper
{
    /// <summary>
    /// ������ ù ���ڸ� �빮�ڷ� ǥ��
    /// </summary>
    public static string CapitalizeFirstLetter(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // ������ ������ ù ���� ã��
        int firstCharIndex = 0;
        while (firstCharIndex < text.Length && char.IsWhiteSpace(text[firstCharIndex]))
        {
            firstCharIndex++;
        }

        if (firstCharIndex >= text.Length) return text;

        // ù ���ڸ� �빮�� ���
        char[] chars = text.ToCharArray();
        chars[firstCharIndex] = char.ToUpper(chars[firstCharIndex]);

        return new string(chars);
    }
}

/// <summary>
/// ���ÿ� ����� CSV ���� ��
/// </summary>
public enum CSVFile
{
    Init,
}

#region ���� �Ǵ� ���
public enum TargetLanguage
{
    EN,     // ���� (����)
    EN_US,  // ���� (�̱�)

    //EN_GB,  // ���� (����)
    //JA,     // �Ϻ���
    //ZH,     // �߱��� (��ü)
    //DE,     // ���Ͼ�
    //FR,     // ��������
    //ES,     // �����ξ�
    //IT,     // ��Ż���ƾ�
    //NL,     // �״������
    //PL,     // �������
    //PT_PT,  // ���������� (��������)
    //PT_BR,  // ���������� (�����)
    //RU,     // ���þƾ�
    //        // ��Ÿ ���� ���...
}
#endregion

public enum TranslatorTools
{
    DeepL,        // ���� ������
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
    // DeepL API ���� ���Ŀ� �´� Ŭ���� (JSON ������ȭ��)
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

    // TODO Chat GPT�� ���� API �����͵� �߰� �� �� ����
}
