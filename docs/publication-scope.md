# Publication scope

以白名單挑選代表性程式碼，重新建立獨立 repository；沒有搬移原 repository 或其 Git 物件。

| Source | Treatment | Public boundary |
| --- | --- | --- |
| GenotypeCsvParser | 保留結構並清理 | 移除 assay marker 排除規則；大小寫不敏感；一般化錯誤訊息 |
| LabCaseAccessGuard | 保留主要規則 | 僅取消狀態與五類操作限制 |
| CaseFileStorageService | 簡化與重整 | 參數化根目錄、operation folder、路徑檢查、複製與補償；無部署設定 |
| PositiveControlQualityService | 簡化 | 呼叫端提供 expected markers；無正式 reference repository |
| PaternityCalculationService | 簡化 | 計算器介面與 CPI 彙整；無 PI 計算實作或頻率查詢 |
| WordTemplateService | 簡化 | 四個通用欄位、跨 run 文字替換、marker table；無正式結論或醫療格式 |
| AuthenticationService | 簡化 | 查詢與稽核介面、BCrypt 驗證、一般化錯誤；無帳號資料 |
| Models | 最小化 | 僅上述範例需要的資料型別；無身分或病例欄位 |
| Tests | 重新撰寫 | 兩個簡單 xUnit 測試；無正式計算測試或參考 profile |

排除 `SingleParentPiCalculator`、`PositiveControlExpectedRepository`、`AlleleFrequencyRepository`、`LabPaternityWorkflowService`、完整 repositories / schema、正式 Word templates、資料庫、原始檢驗 CSV、部署設定及原專案測試。

唯一 sample CSV 由虛構 MarkerA / MarkerB / MarkerC 建立。未複製任何圖片，`screenshots/` 僅保留空目錄標記。公開 README 只說明高層流程，不記錄機構名稱、內部路徑或正式報告規則。

發佈時以 staged files 為準逐檔檢查，搭配敏感字詞與憑證格式掃描；程式中的 `password` 等參數名稱及文件中的排除說明需人工判讀，不能只依關鍵字判定是否含有實際秘密。
