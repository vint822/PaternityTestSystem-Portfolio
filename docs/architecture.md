# Project Architecture

PaternityTestSystem 是以 C# / .NET 8 開發的 Windows 桌面應用程式。

整體程式主要分成以下幾個部分：

```text
WPF / XAML
    ↓
Services
    ↓
Data / Repository
    ↓
SQLite
```

## Main Workflow

親子鑑定主要流程：

```text
建立案件
    ↓
匯入 STR CSV
    ↓
Positive Control QC
    ↓
親子鑑定計算
    ↓
產生 Word 報告
    ↓
保存案件紀錄
```

## Main Components

`GenotypeCsvParser`  
負責解析 STR CSV，並轉換成系統使用的 genotype 資料。

`PositiveControlQualityService`  
負責比較 Positive Control 的實際結果與預期結果。

`PaternityCalculationService`  
負責串接 STR 資料與親子鑑定計算流程。

`CaseFileStorageService`  
負責案件 CSV、計算文件及報告檔案的儲存與失敗清理。

`LabCaseAccessGuard`  
集中管理取消案件不可重新計算或產生文件等 Business Rules。

`AuthenticationService`  
負責使用者登入驗證及 Audit Log 流程。

`WordTemplateService`  
負責 Word Template placeholder replacement、動態 STR row 與 allele 格式處理。
