# SocialDistance

作者: `Roxyz0501`

FFXIVで、自分のキャラクターから周囲のプレイヤーまでの直線距離を表示するACTプラグインです。

ACT設定画面はTarget Marker Overlayと共通の、白いタイトルヘッダー・金色区切り線・タブ形式のデザインです。SocialDistanceにはサンプル表示機能はありません。

## 機能

- 実際のFFXIVジョブアイコン、プレイヤー名、距離を表示
- 自キャラクターに近い順で自動ソート
- 最近接プレイヤーが設定距離未満なら、赤字と赤い背景で強調
- `SocialDistance` タブからオーバーレイの表示・非表示を切り替え
- 初期状態ではドラッグ移動可能。固定時はクリック透過
- FFXIVが非アクティブなときに自動で非表示
- 表示人数、最大距離、警告距離、透明度を調整可能
- 固定解除中は四辺・四隅のドラッグでウィンドウを直接リサイズ
- 小さいウィンドウでも名前と距離が重ならないレスポンシブ表示
- UIは英語・日本語・簡体字中国語・韓国語に対応し、設定画面から即時切替可能
- プレイヤー名の表示・非表示を切替可能
- 強調対象を「もっとも近いプレイヤー」または「警告距離内の全員」から選択可能
- ACT設定画面の右下にプラグインバージョンを表示
- `DIFF`カラムで、自分→最近接、以降はひとつ上のプレイヤー→その行のプレイヤーの距離を表示
- 距離カラムは左から `toME`、`DIFF` の順で表示
- `2行目のDIFF < 1行目のtoME` の場合の黄橙色警告（カラム非表示時も判定可能）
- 距離単位を `y`（1y = 0.9144m）または `m` から選択可能。既定は `y`
- 設定したechoチャット本文でオーバーレイ表示を切替可能（例: `/echo SocialDistance`）
- システムメッセージやNPCの台詞に含まれる文言を使い、表示ON／OFFを個別に制御可能
- キャラクター名を `Player 01` 形式に置換する匿名モード（既定OFF）
- 背景だけを0～100%で透明化可能
- 固定解除中の右下に半透明のリサイズガイドを常時表示
- オーバーレイ位置と設定を自動保存
- 独立した「支援」タブから、任意で開発者を支援可能
- 起動時の安定版更新確認、手動確認、SHA-256検証、安全な更新準備に対応

## 必要環境

- Windows版 Advanced Combat Tracker
- FFXIV_ACT_Plugin（ACT上で先に有効化）
- .NET Framework 4.8

OverlayPluginは不要です。本プラグイン自身が軽量なトップレベルオーバーレイを表示します。

## インストール

1. `SocialDistance.dll` を任意のフォルダーへ置きます。
2. Windowsのファイルのプロパティに「ブロックの解除」が表示される場合は解除します。
3. ACTを起動し、`Plugins` → `Plugin Listing` → `Browse...` から `SocialDistance.dll` を選びます。
4. `Add/Enable Plugin` を押します。
5. 追加された `SocialDistance` タブで設定します。

初期状態ではオーバーレイを移動しやすいよう、ロックと「FFXIVが非アクティブなら隠す」はオフです。配置後に必要な項目をオンにしてください。

FFXIV_ACT_PluginはSocialDistanceより先にロードしてください。接続できない場合は、ACTのPlugin Listingで読み込み順を確認してからSocialDistanceを一度無効化・再有効化してください。

## 任意支援

「支援」タブから、Ko-fiでRoxyz0501の開発を任意で支援できます。

支援しなくてもSocialDistanceの全機能を利用でき、機能差はありません。起動時のポップアップ、自動遷移、繰り返し通知、機能制限はありません。支援ボタンを明示的にクリックした場合のみ、既定のブラウザで次のページを開きます。

[Ko-fiでRoxyz0501を支援する](https://ko-fi.com/roxyz0501)

## 言語

英語、日本語、簡体字中国語、韓国語を利用できます。新規Config、または旧Configに言語設定がない初回だけWindowsのUIカルチャーを確認し、ja系は日本語、zh系は簡体字中国語、ko系は韓国語、それ以外は英語を選びます。一度保存された言語は、以後OS言語で上書きされません。

翻訳キーが対象言語にない場合と未対応言語の場合は英語へフォールバックします。言語変更は設定・支援・更新・オーバーレイへ即時反映されます。

## 更新機能

更新確認は既定でONです。「更新」タブからOFFにしたり、「今すぐ確認」を実行できます。起動時確認はUIをブロックしない非同期処理で1回だけ行い、GitHub Releasesのdraftとprereleaseを除外した安定版だけを対象にします。通信失敗、タイムアウト、API制限、JSON不正が発生しても通常機能は継続します。

更新は利用者が「更新をダウンロードして準備」を押した場合だけ開始します。Release assetとSHA-256を確認し、ZIPのパストラバーサル、Zip Slip、異なるプラグイン、Releaseタグと異なるバージョン、作者情報の不一致を拒否します。検証後は補助プロセスがACT終了を待ち、現行DLLをバックアップしてから置換します。置換またはハッシュ確認に失敗した場合はバックアップから復元します。

更新元は公開リポジトリ [Roxyz0501/socialdistance-act-plugin](https://github.com/Roxyz0501/socialdistance-act-plugin) の安定版Releasesです。認証トークンは不要で、トークンをソースやDLLへ埋め込んでいません。公開ReleaseのHTTPS APIとRelease assetだけを使用します。

プロジェクト本体のライセンスは現時点では未設定です。ジョブアイコン素材には、同梱の `AetherRange/Assets/Jobs/XIVAPI-LICENSE.txt` が適用されます。

### Release assetの作成

AssemblyInfoのバージョンを更新後、次を実行します。

~~~powershell
.\release.ps1 -Version 2.5.0
~~~

releaseフォルダへSocialDistance-v2.5.0.zipとSocialDistance-v2.5.0.zip.sha256が生成されます。タグはv2.5.0のようなSemVerにし、draft／prereleaseではないGitHub Releaseへ両方を添付します。

GitHub CLIを使用する例:

~~~powershell
gh release create v2.5.0 .\release\SocialDistance-v2.5.0.zip .\release\SocialDistance-v2.5.0.zip.sha256 --title "SocialDistance v2.5.0" --notes-file .\RELEASE_NOTES.md
~~~

通常は `vMAJOR.MINOR.PATCH` タグをmainへpushすると、GitHub ActionsがWindows上で公式ACT ZIPを取得し、署名済みACTアセンブリの公開鍵トークンを検証してからビルドとテストを行います。成功時だけ同じ名前のZIPとSHA-256マニフェストを添付した安定版Releaseを作成します。ACT本体はRelease assetへ含めません。

## ビルド

PowerShellでリポジトリのルートから実行します。

```powershell
.\build.ps1
```

ACTを標準以外の場所へインストールしている場合:

```powershell
.\build.ps1 -ActPath "D:\Apps\ACT\Advanced Combat Tracker.exe"
```

出力先は `AetherRange\bin\Release\net48\SocialDistance.dll` です。FFXIV_ACT_Pluginの型は実行時に公開APIを通して取得するため、そのDLLをビルド成果物へコピーする必要はありません。

## 距離について

距離はFFXIV_ACT_Pluginから得た両キャラクターのX/Y/Z座標による3次元直線距離です。内部値はメートルとして扱い、`y` 選択時は `1y = 0.9144m` で換算します。表示値は小数第1位までです。

## アイコン素材

ジョブアイコンはMITライセンスの [xivapi/classjob-icons](https://github.com/xivapi/classjob-icons) を使用し、DLLへ埋め込んでいます。ライセンス本文は `AetherRange/Assets/Jobs/XIVAPI-LICENSE.txt` に収録しています。

## 注意

ACTを含む外部ツールの利用は自己責任です。ゲーム内で取得情報を用いて他プレイヤーを非難・迷惑行為に利用しないでください。
