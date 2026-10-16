# PaternityTestSystem

**C# / .NET 8 / WPF / SQLite**

以實際臨床親子鑑定流程為需求來源所開發的 Windows 桌面應用程式。

系統整合案件管理、STR 檢驗資料處理、Positive Control 品質確認、親子鑑定計算、Word 報告產生與 Audit Log，目前已部署於實際工作環境進行使用者測試。

> 此 Repository 為求職作品集版本。\
> 已移除真實醫療資料、院內設定、正式文件與部分核心專業邏輯。

---

## Project Background

我目前從事臨床醫學檢驗工作。

實際親子鑑定流程除了計算結果，也涉及案件資料、受檢者資料、原始 STR CSV、品質控制、報告產生與歷史紀錄追溯。

因此我嘗試將原本分散的工作流程轉換為一套資訊系統，也藉由此專案學習如何將：

**Clinical Workflow → Software Workflow**

---

## Workflow
```
登入
 ↓
建立案件
 ↓
受檢者資料
 ↓
匯入 STR CSV
 ↓
Positive Control QC
 ↓
┌──────────────┐
│              │
PASS           FAIL
│              │
↓              ↓
正式計算        試算模式
│              │
↓              └─ 不允許正式報告
PI / CPI / PP
 ↓
結果確認
 ↓
Word Report
 ↓
案件紀錄 / Audit Log
```

---

## Tech Stack

| <br>            | <br>                  |
| --------------- | --------------------- |
| Language        | C#                    |
| Framework       | .NET 8                |
| Desktop UI      | WPF / XAML            |
| Database        | SQLite                |
| Database Access | Microsoft.Data.Sqlite |
| Authentication  | BCrypt                |
| Testing         | xUnit                 |
| Version Control | Git / GitHub          |

---

## Key Features

### STR Data Processing

解析檢驗儀器產生的 genotype CSV，進行資料驗證並轉換為系統內部資料模型，供後續 QC 與計算流程使用。

### Quality Control

正式計算前執行 Positive Control 驗證。

QC 未通過時僅允許試算，Service Layer 會阻止正式報告產生，而不只是停用 UI 按鈕。

### Case & File Management

依案件管理原始 CSV、計算資料及報告檔案，並針對流程失敗設計檔案清理與資料一致性處理。

### Report Generation

使用既有 Word Template 自動產生檢驗報告，包含動態 STR marker rows 與共有 allele 格式處理。

### Audit Trail

重要操作會保存使用者、操作類型及時間，提供案件歷程追溯。

---

## Engineering Highlights

### Data Integrity

涉及多筆資料寫入時使用 Database Transaction。
```
成功
→ Commit

失敗
→ Rollback
```

資料庫 Transaction 無法處理檔案系統，因此另建立檔案補償與失敗清理流程。

### Cancelled Case Protection

取消案件仍保留歷史資料，但禁止：
```
重新計算
產生新報告
產生新計算文件
開啟既有正式文件
```

相關限制放在 Service / Domain Layer，而不是單純依靠畫面控制。

### Automated Tests

公開版本保留少量 xUnit 測試，用於展示 CSV Parsing 與案件狀態規則的自動化驗證。

---

## Code Highlights

第一次閱讀此作品集，可以優先查看：

`GenotypeCsvParser.cs`\
展示 CSV parsing、資料驗證與錯誤處理。

`PositiveControlQualityService.cs`\
展示如何將實驗室品質控制流程轉換為程式邏輯。

`CaseFileStorageService.cs`\
展示案件檔案、operation folder 與失敗清理流程。

`LabCaseAccessGuard.cs`\
展示案件狀態相關 Business Rule。

`WordTemplateService.cs`\
展示 Word Template placeholder 與動態表格處理。

> 親子鑑定核心 PI 公式、完整 allele frequency 資料及正式 production workflow 不包含於公開版本。

---

## Screenshots

### Case Workflow



### Calculation & QC



### Case Search



所有畫面皆使用虛構測試資料。

---

## Privacy

此作品集不包含：

- 真實病人或案件資料
- Production database
- 醫院帳號、網路資訊與部署設定
- 正式醫療文件
- 完整 allele frequency dataset
- 完整 PI 計算公式
- 其他可能涉及個資或院內資訊的內容

---

## About Me

具生物化學研究所與臨床醫學檢驗背景，目前持續學習 C# / .NET 軟體開發。

希望將：

**Clinical Laboratory Knowledge × Software Engineering**

結合，往醫療資訊、LIS、實驗室自動化及醫療軟體工程領域發展。
