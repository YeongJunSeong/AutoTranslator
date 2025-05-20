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

    Dictionary<int, string> englishTextData = new Dictionary<int, string>();        // koreanTextData의 value값을 번역해서 해당 딕셔너리에 넣어줄거임

    Dictionary<int, string> koreanTextData = new Dictionary<int, string>();

    public string LocalPathName = "C:\\CSVFiles";

    [Header("로컬 경로에 저장된 CSV 파일들")][Space(10f)]
    public List<string> ListCSVFileName = new List<string>();       // csv 파일 이름만

    [Header("로드 할 파일리스트에 대한 인덱스")]
    [SerializeField, ReadOnly]
    public int currentCSVFileIndex;

    [Header("로드 하는 파일 명")]
    [SerializeField, ReadOnly]
    public string FileFullPath;

    public Dictionary<int, string> KoreanTextDatas => koreanTextData;
    public Dictionary<int, string> EnglishTextDatas => englishTextData;
    public int TotalTranslateCount => koreanTextData.Count;
    public List<List<string>> ListOriginParsedRows { private set; get; }        // 원본 CSV 행 데이터 보관

    // 칼럼 인덱스 찾기
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
            Debug.Log($"CSV 파일 경로 : {FileFullPath}");

            // 파일 전체를 한 번에 읽어서 처리 (줄바꿈 문제 해결)
            string fileContent = File.ReadAllText(FileFullPath, Encoding.UTF8);
            LoadCSVData(fileContent);
        }
        else
        {
            Debug.LogError($"{FileFullPath}경로에서 {ListCSVFileName[fileIndex]}이름의 CSV 파일을 찾을 수 없습니다.");
        }
    }

    #region Load CSV Data
    void LoadCSVData(string content)
    {
        try
        {
            // CSV 줄과 필드 파싱
            List<List<string>> parsedRows = ParseCSVToRows(content);

            if (parsedRows.Count <= 1)
            {
                Debug.LogError("CSV 파일이 비어 있거나 헤더만 있습니다.");
                return;
            }

            Debug.Log("LoadVRparsed Count");
            ListOriginParsedRows = new List<List<string>>(parsedRows.Count);
            foreach (var row in parsedRows)
            {
                ListOriginParsedRows.Add(new List<string>(row));
                
            }
            // 헤더 행 처리
            List<string> headers = parsedRows[0];

            // 칼럼 인덱스 찾기
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

            // 필요한 열이 있는지 확인
            if (keyIndex == -1)
            {
                Debug.LogError("Key 칼럼을 찾을 수 없습니다.");
                return;
            }

            if (koreanIndex == -1)
            {
                Debug.LogError("Korean(ko) 칼럼을 찾을 수 없습니다.");
                return;
            }

            if (englishIndex == -1)
            {
                Debug.LogError("English(en) 칼럼을 찾을 수 없습니다.");
                return;
            }

            koreanTextData.Clear();

            // 데이터 행 처리
            for (int i = 1; i < parsedRows.Count; i++)
            {
                List<string> row = parsedRows[i];

                //// 행이 너무 짧으면 건너뛰기
                //if (row.Count <= Math.Max(keyIndex, koreanIndex))
                //{
                //    Debug.LogWarning($"CSV 파일 {i}번째 행이 너무 짧습니다. 건너뜁니다.");
                //    continue;
                //}

                // Key 값 안전하게 파싱
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
                        Debug.LogWarning($"중복된 키가 발견되었습니다: {key}. 이미 '{koreanTextData[key]}'이(가) 저장되어 있습니다.");
                    }
                }
                else
                {
                    Debug.LogError($"CSV 파일 {i}번째 행의 Key '{keyStr}'를 숫자로 변환할 수 없습니다.");
                }
            }

            Debug.Log($"총 <color=cyan>{koreanTextData.Count}개</color>의 한국어 항목을 로드했습니다.");
        }
        catch (Exception e)
        {
            Debug.LogError($"CSV 파일 데이터 로드 중 오류 발생: {e.Message}\n{e.StackTrace}");
        }
    }
    #endregion

    // 번역 완료 후 CSV 파일로 저장
    public void SaveTranslatedCSV()
    {
        if (ListOriginParsedRows == null || ListOriginParsedRows.Count <= 1)
        {
            Debug.LogError("원본 CSV 구조가 없습니다. 먼저 CSV 파일을 로드하세요.");
            return;
        }

        try
        {
            string outputFilePath;

            //if (createNewFile)
            //{
            //    // 새 파일 생성
            //    string fileName = Path.GetFileNameWithoutExtension(ListCSVFileName[currentCSVFileIndex]);
            //    string extension = Path.GetExtension(ListCSVFileName[currentCSVFileIndex]);
            //    string newFileName = fileName + translatedFileSuffix + extension;
            //    outputFilePath = Path.Combine(LocalPathName, newFileName);
            //}
            //else
            //{
            //    // 기존 파일 덮어쓰기
            //    outputFilePath = Path.Combine(LocalPathName, ListCSVFileName[currentCSVFileIndex]);

            //    // 원본 파일 백업 (덮어쓰기 전에)
            //    string backupPath = outputFilePath + ".bak";
            //    File.Copy(outputFilePath, backupPath, true);
            //    Debug.Log($"원본 파일 백업: {backupPath}");
            //}

            // 기존 파일 덮어쓰기
            outputFilePath = Path.Combine(LocalPathName, ListCSVFileName[currentCSVFileIndex]);

            // 원본 파일 백업 (덮어쓰기 전에)
            string backupPath = outputFilePath + ".bak";
            File.Copy(outputFilePath, backupPath, true);
            Debug.Log($"원본 파일 백업: {backupPath}");

            // 번역 결과를 원본 구조에 적용
            for (int i = 1; i < ListOriginParsedRows.Count; i++)
            {
                List<string> row = ListOriginParsedRows[i];

                // 행이 너무 짧으면 건너뛰기
                if (row.Count <= Math.Max(keyIndex, englishIndex))
                {
                    continue;
                }

                string keyStr = row[keyIndex].Trim();

                if (int.TryParse(keyStr, out int key) && englishTextData.ContainsKey(key))
                {
                    // 영어 번역 결과 적용
                    while (row.Count <= englishIndex)
                    {
                        row.Add(""); // 필요한 경우 빈 칸 추가
                    }

                    row[englishIndex] = englishTextData[key];
                }
            }

            // CSV 형식으로 변환하여 저장
            StringBuilder csv = new StringBuilder();

            foreach (var row in ListOriginParsedRows)
            {
                List<string> formattedFields = new List<string>();

                // 각 필드 CSV 형식으로 포맷팅
                foreach (var field in row)
                {
                    formattedFields.Add(FormatCSVField(field));
                }

                csv.AppendLine(string.Join(",", formattedFields)); // 행 추가
            }

            // 파일로 저장
            File.WriteAllText(outputFilePath, csv.ToString(), Encoding.UTF8);

            Debug.Log($"번역된 CSV 파일이 저장되었습니다: {outputFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"번역된 CSV 파일 저장 중 오류 발생: {e.Message}");
        }
    }


    #region CSV To Row
    // CSV 내용을 행과 열로 파싱 (줄바꿈 문제 처리)
    List<List<string>> ParseCSVToRows(string content)
    {
        List<List<string>> rows = new List<List<string>>();
        List<string> currentRow = new List<string>();
        StringBuilder field = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];

            // 따옴표 처리
            if (c == '"')
            {
                if (i + 1 < content.Length && content[i + 1] == '"')
                {
                    // 이스케이프된 따옴표 (""는 "로 해석)
                    field.Append('"');
                    i++;
                }
                else
                {
                    // 따옴표 시작/종료
                    inQuotes = !inQuotes;
                }
            }
            // 줄바꿈 처리 - 따옴표 안의 줄바꿈은 필드의 일부로 유지
            else if ((c == '\r' || c == '\n') && !inQuotes)
            {
                // 따옴표 밖의 줄바꿈 - 행 구분
                currentRow.Add(field.ToString());
                field.Clear();

                // 행 추가
                if (currentRow.Count > 0)
                {
                    rows.Add(new List<string>(currentRow));
                    currentRow.Clear();
                }

                // \r\n 처리 (Windows 줄바꿈)
                if (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                {
                    i++;
                }
            }
            // 쉼표 처리
            else if (c == ',' && !inQuotes)
            {
                // 필드 종료
                currentRow.Add(field.ToString());
                field.Clear();
            }
            // 일반 문자 처리 (줄바꿈 포함)
            else
            {
                field.Append(c);
            }
        }

        // 마지막 필드와 행 추가
        if (field.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(field.ToString());
            rows.Add(new List<string>(currentRow));
        }

        return rows;
    }

    // CSV 필드 형식화 (쉼표, 따옴표 처리)
    private string FormatCSVField(string field)
    {
        // 필드에 쉼표, 따옴표 또는 줄바꿈이 포함된 경우 큰따옴표로 묶음
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
        {
            // 따옴표는 두 개의 따옴표로 이스케이프
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }
        return field;
    }
    #endregion

}
