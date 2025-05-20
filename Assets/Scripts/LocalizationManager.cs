using System.Collections;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Sirenix.OdinInspector;
using static LocalizationManager;
using Sirenix.OdinInspector.Editor;
using System.Runtime.CompilerServices;

public class LocalizationManager : MonoBehaviour
{

    Dictionary<int, string> englishTextData = new Dictionary<int, string>();        // koreanTextData�� value���� �����ؼ� �ش� ��ųʸ��� �־��ٰ���

    Dictionary<int, string> koreanTextData = new Dictionary<int, string>();

    public string LocalPathName = "C:\\CSVFiles";

    [Header("���� ��ο� ����� CSV ���ϵ�")][Space(10f)]
    public List<string> ListCSVFileName = new List<string>();       // csv ���� �̸���

    [Header("�ε� �� ���ϸ���Ʈ�� ���� �ε���")]
    [SerializeField, ReadOnly]
    public int currentCSVFileIndex;

    [Header("�ε� �ϴ� ���� ��")]
    [SerializeField, ReadOnly]
    public string FileFullPath;

    public Dictionary<int, string> KoreanTextDatas => koreanTextData;
    public Dictionary<int, string> EnglishTextDatas => englishTextData;
    public int TotalTranslateCount => koreanTextData.Count;
    public List<List<string>> ListOriginParsedRows { private set; get; }        // ���� CSV �� ������ ����

    // Į�� �ε��� ã��
    int keyIndex = -1;
    int koreanIndex = -1;
    int englishIndex = -1;

    public void SetTranslationComplatedText(int key, string translatedText)
    {
        englishTextData[key] = translatedText;
    }

    public void OnClickLoadFile(int index)
    {
        currentCSVFileIndex = index;

        LoadCSVFile(index);
    }

    void LoadCSVFile(int fileIndex)
    {
        FileFullPath = Path.Combine(LocalPathName, ListCSVFileName[fileIndex]);
        if (File.Exists(FileFullPath))
        {
            Debug.Log($"CSV ���� ��� : {FileFullPath}");

            // ���� ��ü�� �� ���� �о ó�� (�ٹٲ� ���� �ذ�)
            string fileContent = File.ReadAllText(FileFullPath, Encoding.UTF8);
            LoadCSVData(fileContent);
        }
        else
        {
            Debug.LogError($"{FileFullPath}��ο��� {ListCSVFileName[fileIndex]}�̸��� CSV ������ ã�� �� �����ϴ�.");
        }
    }

    #region Load CSV Data
    void LoadCSVData(string content)
    {
        try
        {
            // CSV �ٰ� �ʵ� �Ľ�
            List<List<string>> parsedRows = ParseCSVToRows(content);

            if (parsedRows.Count <= 1)
            {
                Debug.LogError("CSV ������ ��� �ְų� ����� �ֽ��ϴ�.");
                return;
            }

            Debug.Log("LoadVRparsed Count");
            ListOriginParsedRows = new List<List<string>>(parsedRows.Count);
            foreach (var row in parsedRows)
            {
                ListOriginParsedRows.Add(new List<string>(row));
                
            }
            // ��� �� ó��
            List<string> headers = parsedRows[0];

            // Į�� �ε��� ã��
            keyIndex = -1;
            koreanIndex = -1;
            englishIndex = -1;

            for (int i = 0; i < headers.Count; i++)
            {
                string header = headers[i].Trim();
                if (header.Equals("Key", StringComparison.OrdinalIgnoreCase))
                {
                    keyIndex = i;
                }
                else if (header.Equals("English(en)", StringComparison.OrdinalIgnoreCase))
                {
                    englishIndex = i;
                }
                else if (header.Equals("Korean(ko)", StringComparison.OrdinalIgnoreCase))
                {
                    koreanIndex = i;
                }
            }

            // �ʿ��� ���� �ִ��� Ȯ��
            if (keyIndex == -1)
            {
                Debug.LogError("Key Į���� ã�� �� �����ϴ�.");
                return;
            }

            if (koreanIndex == -1)
            {
                Debug.LogError("Korean(ko) Į���� ã�� �� �����ϴ�.");
                return;
            }

            if (englishIndex == -1)
            {
                Debug.LogError("English(en) Į���� ã�� �� �����ϴ�.");
                return;
            }

            koreanTextData.Clear();

            // ������ �� ó��
            for (int i = 1; i < parsedRows.Count; i++)
            {
                List<string> row = parsedRows[i];

                //// ���� �ʹ� ª���� �ǳʶٱ�
                //if (row.Count <= Math.Max(keyIndex, koreanIndex))
                //{
                //    Debug.LogWarning($"CSV ���� {i}��° ���� �ʹ� ª���ϴ�. �ǳʶݴϴ�.");
                //    continue;
                //}

                // Key �� �����ϰ� �Ľ�
                string keyStr = row[keyIndex].Trim();
                string korValue = row[koreanIndex]; 

                if (int.TryParse(keyStr, out int key))
                {
                    if (!koreanTextData.ContainsKey(key))
                    {
                        koreanTextData.Add(key, korValue);
                        englishTextData.Add(key, "");
                        Debug.Log($"<color=cyan>Add Data --> Key: {key}, Text: {koreanTextData[key]}</color>");
                    }
                    else
                    {
                        Debug.LogWarning($"�ߺ��� Ű�� �߰ߵǾ����ϴ�: {key}. �̹� '{koreanTextData[key]}'��(��) ����Ǿ� �ֽ��ϴ�.");
                    }
                }
                else
                {
                    Debug.LogError($"CSV ���� {i}��° ���� Key '{keyStr}'�� ���ڷ� ��ȯ�� �� �����ϴ�.");
                }
            }

            Debug.Log($"�� <color=cyan>{koreanTextData.Count}��</color>�� �ѱ��� �׸��� �ε��߽��ϴ�.");
        }
        catch (Exception e)
        {
            Debug.LogError($"CSV ���� ������ �ε� �� ���� �߻�: {e.Message}\n{e.StackTrace}");
        }
    }
    #endregion

    // ���� �Ϸ� �� CSV ���Ϸ� ����
    public void SaveTranslatedCSV()
    {
        if (ListOriginParsedRows == null || ListOriginParsedRows.Count <= 1)
        {
            Debug.LogError("���� CSV ������ �����ϴ�. ���� CSV ������ �ε��ϼ���.");
            return;
        }

        try
        {
            string outputFilePath;

            //if (createNewFile)
            //{
            //    // �� ���� ����
            //    string fileName = Path.GetFileNameWithoutExtension(ListCSVFileName[currentCSVFileIndex]);
            //    string extension = Path.GetExtension(ListCSVFileName[currentCSVFileIndex]);
            //    string newFileName = fileName + translatedFileSuffix + extension;
            //    outputFilePath = Path.Combine(LocalPathName, newFileName);
            //}
            //else
            //{
            //    // ���� ���� �����
            //    outputFilePath = Path.Combine(LocalPathName, ListCSVFileName[currentCSVFileIndex]);

            //    // ���� ���� ��� (����� ����)
            //    string backupPath = outputFilePath + ".bak";
            //    File.Copy(outputFilePath, backupPath, true);
            //    Debug.Log($"���� ���� ���: {backupPath}");
            //}

            // ���� ���� �����
            outputFilePath = Path.Combine(LocalPathName, ListCSVFileName[currentCSVFileIndex]);

            // ���� ���� ��� (����� ����)
            string backupPath = outputFilePath + ".bak";
            File.Copy(outputFilePath, backupPath, true);
            Debug.Log($"���� ���� ���: {backupPath}");

            // ���� ����� ���� ������ ����
            for (int i = 1; i < ListOriginParsedRows.Count; i++)
            {
                List<string> row = ListOriginParsedRows[i];

                // ���� �ʹ� ª���� �ǳʶٱ�
                if (row.Count <= Math.Max(keyIndex, englishIndex))
                {
                    continue;
                }

                string keyStr = row[keyIndex].Trim();

                if (int.TryParse(keyStr, out int key) && englishTextData.ContainsKey(key))
                {
                    // ���� ���� ��� ����
                    while (row.Count <= englishIndex)
                    {
                        row.Add(""); // �ʿ��� ��� �� ĭ �߰�
                    }

                    row[englishIndex] = englishTextData[key];
                }
            }

            // CSV �������� ��ȯ�Ͽ� ����
            StringBuilder csv = new StringBuilder();

            foreach (var row in ListOriginParsedRows)
            {
                List<string> formattedFields = new List<string>();

                // �� �ʵ� CSV �������� ������
                foreach (var field in row)
                {
                    formattedFields.Add(FormatCSVField(field));
                }

                csv.AppendLine(string.Join(",", formattedFields)); // �� �߰�
            }

            // ���Ϸ� ����
            File.WriteAllText(outputFilePath, csv.ToString(), Encoding.UTF8);

            Debug.Log($"������ CSV ������ ����Ǿ����ϴ�: {outputFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"������ CSV ���� ���� �� ���� �߻�: {e.Message}");
        }
    }


    #region CSV To Row
    // CSV ������ ��� ���� �Ľ� (�ٹٲ� ���� ó��)
    List<List<string>> ParseCSVToRows(string content)
    {
        List<List<string>> rows = new List<List<string>>();
        List<string> currentRow = new List<string>();
        StringBuilder field = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];

            // ����ǥ ó��
            if (c == '"')
            {
                if (i + 1 < content.Length && content[i + 1] == '"')
                {
                    // �̽��������� ����ǥ (""�� "�� �ؼ�)
                    field.Append('"');
                    i++;
                }
                else
                {
                    // ����ǥ ����/����
                    inQuotes = !inQuotes;
                }
            }
            // �ٹٲ� ó�� - ����ǥ ���� �ٹٲ��� �ʵ��� �Ϻη� ����
            else if ((c == '\r' || c == '\n') && !inQuotes)
            {
                // ����ǥ ���� �ٹٲ� - �� ����
                currentRow.Add(field.ToString());
                field.Clear();

                // �� �߰�
                if (currentRow.Count > 0)
                {
                    rows.Add(new List<string>(currentRow));
                    currentRow.Clear();
                }

                // \r\n ó�� (Windows �ٹٲ�)
                if (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                {
                    i++;
                }
            }
            // ��ǥ ó��
            else if (c == ',' && !inQuotes)
            {
                // �ʵ� ����
                currentRow.Add(field.ToString());
                field.Clear();
            }
            // �Ϲ� ���� ó�� (�ٹٲ� ����)
            else
            {
                field.Append(c);
            }
        }

        // ������ �ʵ�� �� �߰�
        if (field.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(field.ToString());
            rows.Add(new List<string>(currentRow));
        }

        return rows;
    }

    // CSV �ʵ� ����ȭ (��ǥ, ����ǥ ó��)
    private string FormatCSVField(string field)
    {
        // �ʵ忡 ��ǥ, ����ǥ �Ǵ� �ٹٲ��� ���Ե� ��� ū����ǥ�� ����
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
        {
            // ����ǥ�� �� ���� ����ǥ�� �̽�������
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }
        return field;
    }
    #endregion

}
