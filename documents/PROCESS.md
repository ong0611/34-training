# PROCESS.md — 我的練習心得

> 一個原則：**寫「具體發生的事」，不寫感想文。**
> 貼上當時真實的 prompt、真實的數字、真實的錯誤訊息——三個月後的你（和你的同事）才用得上。

#### 使用的 agent 與模型：
Claude Code
Sonnet 5
---

## 通用四問

### 1. 我的任務拆解

就照 activity-guideline 練習1~4的順序做。

練習1先把CLAUDE.md、settings.json、hooks、subagent、fix-bug skill建起來，跑起來確認能用就commit。

練習2的bug1我自己手動重現（建新單、翻頁看第一頁跟最後一頁），bug2跟bug3因為想省時間，直接跟agent說「你幫我處理」，讓它自己從Controller一路追到Service/Repository找根因，根因講得通我才讓它動手，三個bug分開commit。

練習3有先要求agent只出計畫不要寫code，我對照規格看有沒有偷加東西、分層有沒有跑掉，OK才讓它做。

練習4也是先出計畫再做，重構完測試要全綠。

跟原本規劃比：bug2、3沒有照練習規則自己在頁面重現，是我自己選擇跳過的，不是agent建議的。

### 2. AI 幫上大忙的地方

練習3我沒有直接丟「幫我做低庫存頁」，是先要求：

> 先不要寫程式，給我一份計畫，包含要動哪些檔、分層怎麼分工、近30天銷量怎麼算會不會N+1、threshold驗證放哪層、要補哪3個測試。動手前先讀ProductsController跟既有的Products頁面，照同一套慣例做。

好處是它會老實把「一條查詢搞定不要逐筆查DB」這種細節寫出來，我核准前就能看到它打算怎麼做，不用等做完才發現查詢方式不對重來。

### 3. AI 誤導我的地方，與我如何發現

練習3做完、測試全部綠燈之後，agent自己多做了一次用curl打頁面+直接查SQL的交叉核對（我沒特別要求），結果抓到自己寫的一個bug：

threshold包在巢狀的Query物件底下，用`?threshold=abc`測試繫結失敗時，頁面上完全沒顯示驗證錯誤訊息。查了才發現：查詢有正確被擋下，只是ModelState記錄錯誤的key跟畫面顯示用的key對不上，訊息存到別的地方去了，畫面等於沒顯示。

這個問題單元測試測不出來，因為單元測試是直接呼叫Service，不會經過ASP.NET Core真正的GET查詢字串綁定那一層。改法是把巢狀的Query拿掉，讓ViewModel直接被綁定就好了。

心得：測試全綠不代表使用者真的會看到對的畫面，尤其是表單綁定這種東西。

### 4. 我會帶回日常工作的一招

功能做完、測試全綠之後，多花兩分鐘用curl打幾個邊界值（正常值/0/負數/亂打字型別），肉眼看回應內容對不對，不要看到測試綠就結案：

```bash
curl -s "http://localhost:5150/Products/LowStock?threshold=0" -o out.html -w "HTTP %{http_code}\n"
grep -o 'field-validation-error\|field-validation-valid' out.html
```

有辦法連DB的話，再拿SQL直接算一次答案跟頁面對一下，比只信程式邏輯踏實很多。

## 自我驗證（做到哪個階段答哪題）

> 這份清單agent先幫忙整理過現況，但每一題還是要自己確認過再打勾，不是它能替你回答的。

### 第一階段 — Agentic Coding

練習 1

1. [ ] 我能不看筆記說出三個專案（Web/Core/Infrastructure）各自的職責
2. [ ] 我核對過 agent 描述的建單流程，且至少找出一處不精確或過度簡化的說法
3. [ ] 我知道商業邏輯應該放在哪一層、新增頁面要動哪些地方
4. [ ] （這個沒人測過）hooks/permission的驗證清單（故意跑TRUNCATE、故意要求裝套件、git push --force）要在training-repo目錄下開新的Claude Code session才能真的觸發，還沒實際測過

練習 2

1. 三個 bug 我都先在頁面上重現過，才開始找程式
2. 我給 agent 的資訊包含具體觀察（頁碼／金額數字／庫存數字），而不是只貼客訴原文
3. 每個修復都回到頁面驗證過症狀消失
4. 每個 bug 都補了一個回歸測試，`dotnet test` 全綠
5. 三個獨立 commit，message 說明症狀與根因
6. （思考題）為什麼原本的測試沒抓到這三個 bug？

練習 3

1. `/Products/LowStock` 不帶參數 → 門檻 10 的結果；帶 `?threshold=3` → 結果隨之改變
2. `?threshold=0`、`?threshold=-1` → 頁面顯示驗證錯誤，不是 500
3. 售出數量欄位排除了 Cancelled 訂單（可用一筆已取消的訂單驗證）
4. 停售（已停售 badge）商品不出現在列表
5. 程式分層與命名跟既有的 Products 功能一致（請 agent 自我 review 一次，並自己確認）
6. 至少 3 個新測試，`dotnet test` 全綠

練習 4

1. 重構後 `dotnet test` 全綠
2. 我能說出這次重構「改善了什麼、沒有改變什麼」
3. 我有在 code review 的角度看過 diff（不是 agent 說好就好）

---

## 附錄：值得留下的對話片段

（貼 1–2 段最有代表性的 prompt 與回應**摘要**——不用貼全文，重點是「我怎麼問」和「它怎麼答」。）
<img width="1225" height="457" alt="image" src="https://github.com/user-attachments/assets/5eda1901-77f5-4e20-9913-4b023bf6b898" />
