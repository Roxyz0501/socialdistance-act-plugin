using System;
using System.Collections.Generic;

namespace SocialDistance
{
    internal static class Localization
    {
        public const string English = "en";
        public const string Japanese = "ja";
        public const string SimplifiedChinese = "zh-CN";
        public const string Korean = "ko";

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            ["HeaderSubtitle"] = "Configure display, distance, alerts, and updates here",
            ["TabDisplay"] = "Display", ["TabDistance"] = "Distance & Alerts", ["TabSupport"] = "Support", ["TabUpdate"] = "Updates",
            ["ShowOverlay"] = "Show overlay", ["HideInactive"] = "Hide when FFXIV is not active",
            ["ShowNames"] = "Show player names", ["Anonymous"] = "Anonymize character names",
            ["LockOverlay"] = "Lock overlay (click-through)", ["OverlayOpacity"] = "Overlay opacity",
            ["BackgroundOpacity"] = "Background opacity", ["EchoSection"] = "Echo chat control",
            ["EchoEnabled"] = "Toggle overlay with the configured echo message", ["EchoText"] = "Echo message",
            ["EchoExample"] = "Example: /echo {0}", ["GameMessageSection"] = "Game message control",
            ["GameMessageEnabled"] = "Control the overlay with system messages and NPC dialogue",
            ["GameMessageOn"] = "Show message", ["GameMessageOff"] = "Hide message",
            ["GameMessageHint"] = "Matches configured text contained in ACT log lines. Show and hide messages are independent.",
            ["MoveHint"] = "While unlocked, drag the header to move and an edge to resize.\r\nWhile locked, the overlay is click-through.",
            ["Language"] = "Language", ["DistanceUnit"] = "Distance unit", ["ShowDiff"] = "Show player-to-player distance (DIFF)",
            ["DiffExplanation"] = "toME: you → each player  /  DIFF: previous nearer player → this row\r\nThe first row's DIFF is the same as toME.",
            ["SpacingAlert"] = "Alert when row 2 DIFF is shorter than row 1 toME", ["WarningOverlay"] = "Show warning overlay",
            ["AlertExplanation"] = "Alert condition: B (nearest → second) < A (you → nearest)",
            ["MaxPlayers"] = "Maximum players", ["MaxDistance"] = "Maximum distance ({0})",
            ["AlertDistance"] = "Alert distance ({0})", ["AlertTarget"] = "Distance-alert target",
            ["NearestInRange"] = "Nearest player within alert range", ["AllInRange"] = "All players within alert range",
            ["SupportTitle"] = "Support Roxyz0501's development",
            ["SupportDescription"] = "You can optionally support the development and continued improvement of SocialDistance through Ko-fi.",
            ["SupportOptional"] = "Support is entirely optional. Every feature remains available with no functional difference if you do not contribute.",
            ["SupportButton"] = "Support Roxyz0501 on Ko-fi",
            ["SupportSafety"] = "The external site opens in your default browser only when you click this button.",
            ["SupportOpened"] = "Ko-fi was opened in your default browser.", ["SupportFailed"] = "Could not open Ko-fi: {0}",
            ["Connected"] = "FFXIV data connected", ["Waiting"] = "Waiting for FFXIV...",
            ["OverlayTitle"] = "SOCIAL DISTANCE", ["PlayerCountOne"] = "1 PLAYER", ["PlayerCountMany"] = "{0} PLAYERS",
            ["WarningText"] = "ALERT", ["Started"] = "SocialDistance: Started", ["Stopped"] = "SocialDistance: Stopped",
            ["OverlayChanged"] = "SocialDistance: Overlay {0} by {1}", ["Enabled"] = "enabled", ["Disabled"] = "disabled",
            ["SourceEcho"] = "echo", ["SourceGameMessage"] = "game message", ["SaveFailed"] = "SocialDistance: Could not save settings — {0}",
            ["UpdateTitle"] = "SocialDistance updates", ["UpdateDescription"] = "Check stable GitHub Releases and prepare a verified update.",
            ["CheckAtStartup"] = "Check for updates at startup", ["CheckNow"] = "Check now",
            ["CurrentVersion"] = "Current version", ["LatestVersion"] = "Latest version", ["ReleaseNotes"] = "Release notes",
            ["UpdateButton"] = "Download and prepare update", ["LaterButton"] = "Later",
            ["RepositoryMissing"] = "Update repository is not configured. Set the verified GitHub owner and repository before publishing.",
            ["UpdateChecking"] = "Checking for updates...", ["UpdateUpToDate"] = "You are using the latest stable version.",
            ["UpdateAvailable"] = "A stable update is available: {0} → {1}", ["UpdateFailed"] = "Update check failed: {0}",
            ["UpdateDownloading"] = "Downloading and verifying the update...",
            ["UpdatePrepared"] = "Update verified. Close ACT to apply it, then restart ACT.",
            ["UpdatePrepareFailed"] = "Could not prepare the update: {0}", ["UpdateSkipped"] = "Version {0} was postponed.",
            ["NoReleaseNotes"] = "No release notes were provided.", ["NotChecked"] = "Not checked yet",
            ["InvalidAddress"] = "Invalid web address.", ["UnknownError"] = "Unknown error"
        };

        private static readonly Dictionary<string, string> Ja = new Dictionary<string, string>
        {
            ["HeaderSubtitle"] = "表示・距離・警告・更新をここから調整できます",
            ["TabDisplay"] = "表示", ["TabDistance"] = "距離・警告", ["TabSupport"] = "支援", ["TabUpdate"] = "更新",
            ["ShowOverlay"] = "オーバーレイを表示する", ["HideInactive"] = "FFXIVが非アクティブなら隠す",
            ["ShowNames"] = "プレイヤー名を表示する", ["Anonymous"] = "キャラクター名を匿名化する",
            ["LockOverlay"] = "オーバーレイを固定（クリック透過）", ["OverlayOpacity"] = "オーバーレイの透明度",
            ["BackgroundOpacity"] = "背景の透明度", ["EchoSection"] = "echoチャット連動",
            ["EchoEnabled"] = "指定したechoチャットで表示を切り替える", ["EchoText"] = "echoの本文",
            ["EchoExample"] = "使用例: /echo {0}", ["GameMessageSection"] = "ゲーム内メッセージ連動",
            ["GameMessageEnabled"] = "システムメッセージ・NPCの台詞で表示を切り替える",
            ["GameMessageOn"] = "表示ONの文言", ["GameMessageOff"] = "表示OFFの文言",
            ["GameMessageHint"] = "ACTログに指定文言が含まれたときに反応します。ONとOFFは別々に設定できます。",
            ["MoveHint"] = "固定OFF中はヘッダーをドラッグして移動、縁をドラッグしてリサイズできます。\r\n固定ON中はゲーム操作を妨げないようクリック透過になります。",
            ["Language"] = "表示言語", ["DistanceUnit"] = "距離単位", ["ShowDiff"] = "プレイヤー間距離（DIFF）を表示する",
            ["DiffExplanation"] = "toME：自分→各プレイヤー　/　DIFF：ひとつ上の近いプレイヤー→その行\r\n先頭行のDIFFはtoMEと同じ値です。",
            ["SpacingAlert"] = "2行目のDIFFが1行目のtoMEより短い場合に警告する", ["WarningOverlay"] = "警告オーバーレイを表示する",
            ["AlertExplanation"] = "警告条件：B（最近接→次点）< A（自分→最近接）", ["MaxPlayers"] = "最大表示人数",
            ["MaxDistance"] = "最大表示距離（{0}）", ["AlertDistance"] = "警告距離（{0}）", ["AlertTarget"] = "警告距離の強調対象",
            ["NearestInRange"] = "警告距離内の最も近いプレイヤー", ["AllInRange"] = "警告距離内の全プレイヤー",
            ["SupportTitle"] = "Roxyz0501の開発を支援", ["SupportDescription"] = "SocialDistanceの開発と継続的な改善を、Ko-fiから任意で支援できます。",
            ["SupportOptional"] = "支援は完全に任意です。支援しなくても全機能を利用でき、機能差はありません。",
            ["SupportButton"] = "Ko-fiでRoxyz0501を支援する", ["SupportSafety"] = "このボタンをクリックした場合のみ、既定のブラウザで外部サイトを開きます。",
            ["SupportOpened"] = "既定のブラウザでKo-fiを開きました。", ["SupportFailed"] = "Ko-fiを開けませんでした: {0}",
            ["Connected"] = "FFXIVデータに接続済み", ["Waiting"] = "FFXIVの接続を待っています…",
            ["PlayerCountOne"] = "1 人", ["PlayerCountMany"] = "{0} 人", ["WarningText"] = "警告あり",
            ["Started"] = "SocialDistance: 開始しました", ["Stopped"] = "SocialDistance: 停止しました",
            ["OverlayChanged"] = "SocialDistance: {1}によりオーバーレイを{0}にしました", ["Enabled"] = "表示", ["Disabled"] = "非表示",
            ["SourceEcho"] = "echo", ["SourceGameMessage"] = "ゲーム内メッセージ", ["SaveFailed"] = "SocialDistance: 設定を保存できませんでした — {0}",
            ["UpdateTitle"] = "SocialDistanceの更新", ["UpdateDescription"] = "GitHub Releasesの安定版を確認し、検証済み更新を準備します。",
            ["CheckAtStartup"] = "起動時に更新を確認する", ["CheckNow"] = "今すぐ確認", ["CurrentVersion"] = "現在のバージョン",
            ["LatestVersion"] = "最新バージョン", ["ReleaseNotes"] = "更新内容", ["UpdateButton"] = "更新をダウンロードして準備",
            ["LaterButton"] = "後で", ["RepositoryMissing"] = "更新元リポジトリが未設定です。公開前に正しいGitHub所有者とリポジトリ名を設定してください。",
            ["UpdateChecking"] = "更新を確認しています…", ["UpdateUpToDate"] = "最新の安定版を使用しています。",
            ["UpdateAvailable"] = "安定版の更新があります: {0} → {1}", ["UpdateFailed"] = "更新確認に失敗しました: {0}",
            ["UpdateDownloading"] = "更新をダウンロードして検証しています…", ["UpdatePrepared"] = "更新を検証しました。ACTを終了すると適用されます。完了後にACTを再起動してください。",
            ["UpdatePrepareFailed"] = "更新を準備できませんでした: {0}", ["UpdateSkipped"] = "バージョン {0} を後回しにしました。",
            ["NoReleaseNotes"] = "更新内容はありません。", ["NotChecked"] = "未確認", ["InvalidAddress"] = "Webアドレスが無効です。", ["UnknownError"] = "不明なエラー"
        };

        private static readonly Dictionary<string, string> Zh = new Dictionary<string, string>
        {
            ["HeaderSubtitle"] = "在此调整显示、距离、警告和更新", ["TabDisplay"] = "显示", ["TabDistance"] = "距离与警告", ["TabSupport"] = "支持", ["TabUpdate"] = "更新",
            ["ShowOverlay"] = "显示悬浮窗", ["HideInactive"] = "FFXIV未激活时隐藏", ["ShowNames"] = "显示玩家名称", ["Anonymous"] = "匿名化角色名称",
            ["LockOverlay"] = "锁定悬浮窗（鼠标穿透）", ["OverlayOpacity"] = "悬浮窗透明度", ["BackgroundOpacity"] = "背景透明度",
            ["EchoSection"] = "echo聊天联动", ["EchoEnabled"] = "使用指定echo消息切换显示", ["EchoText"] = "echo消息", ["EchoExample"] = "示例: /echo {0}",
            ["GameMessageSection"] = "游戏消息联动", ["GameMessageEnabled"] = "使用系统消息或NPC台词控制显示", ["GameMessageOn"] = "显示文本", ["GameMessageOff"] = "隐藏文本",
            ["GameMessageHint"] = "当ACT日志包含指定文本时触发。显示与隐藏文本可分别设置。",
            ["MoveHint"] = "未锁定时拖动标题移动，拖动边缘调整大小。\r\n锁定时悬浮窗会启用鼠标穿透。",
            ["Language"] = "语言", ["DistanceUnit"] = "距离单位", ["ShowDiff"] = "显示玩家间距离（DIFF）",
            ["DiffExplanation"] = "toME：你→各玩家 / DIFF：上一名较近玩家→本行玩家\r\n第一行DIFF与toME相同。",
            ["SpacingAlert"] = "第二行DIFF小于第一行toME时发出警告", ["WarningOverlay"] = "显示警告悬浮窗",
            ["AlertExplanation"] = "警告条件：B（最近→第二近）< A（你→最近）", ["MaxPlayers"] = "最多显示人数", ["MaxDistance"] = "最大显示距离（{0}）",
            ["AlertDistance"] = "警告距离（{0}）", ["AlertTarget"] = "距离警告强调对象", ["NearestInRange"] = "警告距离内最近的玩家", ["AllInRange"] = "警告距离内的所有玩家",
            ["SupportTitle"] = "支持Roxyz0501的开发", ["SupportDescription"] = "你可以通过Ko-fi自愿支持SocialDistance的开发与持续改进。",
            ["SupportOptional"] = "支持完全自愿。即使不支持也能使用全部功能，不存在功能差异。", ["SupportButton"] = "在Ko-fi支持Roxyz0501",
            ["SupportSafety"] = "仅在你点击此按钮时，才会使用默认浏览器打开外部网站。", ["SupportOpened"] = "已使用默认浏览器打开Ko-fi。", ["SupportFailed"] = "无法打开Ko-fi：{0}",
            ["Connected"] = "已连接FFXIV数据", ["Waiting"] = "正在等待FFXIV连接…", ["PlayerCountOne"] = "1 人", ["PlayerCountMany"] = "{0} 人", ["WarningText"] = "警告",
            ["Started"] = "SocialDistance：已启动", ["Stopped"] = "SocialDistance：已停止", ["OverlayChanged"] = "SocialDistance：已通过{1}将悬浮窗设为{0}",
            ["Enabled"] = "显示", ["Disabled"] = "隐藏", ["SourceEcho"] = "echo", ["SourceGameMessage"] = "游戏消息", ["SaveFailed"] = "SocialDistance：无法保存设置 — {0}",
            ["UpdateTitle"] = "SocialDistance更新", ["UpdateDescription"] = "检查GitHub Releases稳定版并准备经过验证的更新。", ["CheckAtStartup"] = "启动时检查更新",
            ["CheckNow"] = "立即检查", ["CurrentVersion"] = "当前版本", ["LatestVersion"] = "最新版本", ["ReleaseNotes"] = "更新内容",
            ["UpdateButton"] = "下载并准备更新", ["LaterButton"] = "稍后", ["RepositoryMissing"] = "尚未配置更新仓库。发布前请设置正确的GitHub所有者和仓库名。",
            ["UpdateChecking"] = "正在检查更新…", ["UpdateUpToDate"] = "当前已是最新稳定版。", ["UpdateAvailable"] = "发现稳定版更新：{0} → {1}",
            ["UpdateFailed"] = "检查更新失败：{0}", ["UpdateDownloading"] = "正在下载并验证更新…", ["UpdatePrepared"] = "更新已验证。关闭ACT后将应用更新，然后请重新启动ACT。",
            ["UpdatePrepareFailed"] = "无法准备更新：{0}", ["UpdateSkipped"] = "已暂缓版本 {0}。", ["NoReleaseNotes"] = "未提供更新内容。", ["NotChecked"] = "尚未检查",
            ["InvalidAddress"] = "网页地址无效。", ["UnknownError"] = "未知错误"
        };

        private static readonly Dictionary<string, string> Ko = new Dictionary<string, string>
        {
            ["HeaderSubtitle"] = "표시, 거리, 경고 및 업데이트를 설정합니다", ["TabDisplay"] = "표시", ["TabDistance"] = "거리·경고", ["TabSupport"] = "후원", ["TabUpdate"] = "업데이트",
            ["ShowOverlay"] = "오버레이 표시", ["HideInactive"] = "FFXIV가 비활성 상태일 때 숨기기", ["ShowNames"] = "플레이어 이름 표시", ["Anonymous"] = "캐릭터 이름 익명화",
            ["LockOverlay"] = "오버레이 고정(클릭 통과)", ["OverlayOpacity"] = "오버레이 투명도", ["BackgroundOpacity"] = "배경 투명도",
            ["EchoSection"] = "echo 채팅 연동", ["EchoEnabled"] = "지정한 echo 메시지로 표시 전환", ["EchoText"] = "echo 메시지", ["EchoExample"] = "예: /echo {0}",
            ["GameMessageSection"] = "게임 메시지 연동", ["GameMessageEnabled"] = "시스템 메시지와 NPC 대사로 표시 제어", ["GameMessageOn"] = "표시 문구", ["GameMessageOff"] = "숨김 문구",
            ["GameMessageHint"] = "ACT 로그에 지정 문구가 포함되면 작동합니다. 표시와 숨김 문구는 별도로 설정할 수 있습니다.",
            ["MoveHint"] = "잠금 해제 시 헤더를 드래그해 이동하고 가장자리를 드래그해 크기를 조절합니다.\r\n잠금 시 클릭이 통과됩니다.",
            ["Language"] = "언어", ["DistanceUnit"] = "거리 단위", ["ShowDiff"] = "플레이어 간 거리(DIFF) 표시",
            ["DiffExplanation"] = "toME: 나→각 플레이어 / DIFF: 바로 위의 가까운 플레이어→현재 행\r\n첫 행의 DIFF는 toME와 같습니다.",
            ["SpacingAlert"] = "2행 DIFF가 1행 toME보다 짧으면 경고", ["WarningOverlay"] = "경고 오버레이 표시",
            ["AlertExplanation"] = "경고 조건: B(최근접→차순위) < A(나→최근접)", ["MaxPlayers"] = "최대 표시 인원", ["MaxDistance"] = "최대 표시 거리({0})",
            ["AlertDistance"] = "경고 거리({0})", ["AlertTarget"] = "거리 경고 강조 대상", ["NearestInRange"] = "경고 거리 내 가장 가까운 플레이어", ["AllInRange"] = "경고 거리 내 모든 플레이어",
            ["SupportTitle"] = "Roxyz0501의 개발 후원", ["SupportDescription"] = "Ko-fi를 통해 SocialDistance 개발과 지속적인 개선을 선택적으로 후원할 수 있습니다.",
            ["SupportOptional"] = "후원은 완전히 선택 사항입니다. 후원하지 않아도 모든 기능을 동일하게 사용할 수 있습니다.", ["SupportButton"] = "Ko-fi에서 Roxyz0501 후원",
            ["SupportSafety"] = "이 버튼을 클릭한 경우에만 기본 브라우저로 외부 사이트를 엽니다.", ["SupportOpened"] = "기본 브라우저에서 Ko-fi를 열었습니다.", ["SupportFailed"] = "Ko-fi를 열 수 없습니다: {0}",
            ["Connected"] = "FFXIV 데이터 연결됨", ["Waiting"] = "FFXIV 연결 대기 중…", ["PlayerCountOne"] = "1명", ["PlayerCountMany"] = "{0}명", ["WarningText"] = "경고",
            ["Started"] = "SocialDistance: 시작됨", ["Stopped"] = "SocialDistance: 중지됨", ["OverlayChanged"] = "SocialDistance: {1}(으)로 오버레이를 {0} 상태로 변경했습니다",
            ["Enabled"] = "표시", ["Disabled"] = "숨김", ["SourceEcho"] = "echo", ["SourceGameMessage"] = "게임 메시지", ["SaveFailed"] = "SocialDistance: 설정을 저장하지 못했습니다 — {0}",
            ["UpdateTitle"] = "SocialDistance 업데이트", ["UpdateDescription"] = "GitHub Releases의 안정 버전을 확인하고 검증된 업데이트를 준비합니다.", ["CheckAtStartup"] = "시작할 때 업데이트 확인",
            ["CheckNow"] = "지금 확인", ["CurrentVersion"] = "현재 버전", ["LatestVersion"] = "최신 버전", ["ReleaseNotes"] = "업데이트 내용",
            ["UpdateButton"] = "업데이트 다운로드 및 준비", ["LaterButton"] = "나중에", ["RepositoryMissing"] = "업데이트 저장소가 설정되지 않았습니다. 배포 전에 올바른 GitHub 소유자와 저장소를 설정하세요.",
            ["UpdateChecking"] = "업데이트 확인 중…", ["UpdateUpToDate"] = "최신 안정 버전을 사용 중입니다.", ["UpdateAvailable"] = "안정 버전 업데이트가 있습니다: {0} → {1}",
            ["UpdateFailed"] = "업데이트 확인 실패: {0}", ["UpdateDownloading"] = "업데이트 다운로드 및 검증 중…", ["UpdatePrepared"] = "업데이트가 검증되었습니다. ACT를 종료하면 적용됩니다. 이후 ACT를 다시 시작하세요.",
            ["UpdatePrepareFailed"] = "업데이트 준비 실패: {0}", ["UpdateSkipped"] = "버전 {0}을(를) 나중으로 미뤘습니다.", ["NoReleaseNotes"] = "업데이트 내용이 없습니다.", ["NotChecked"] = "확인하지 않음",
            ["InvalidAddress"] = "웹 주소가 올바르지 않습니다.", ["UnknownError"] = "알 수 없는 오류"
        };

        public static string NormalizeLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language)) return English;
            var value = language.Trim();
            if (value.StartsWith("ja", StringComparison.OrdinalIgnoreCase)) return Japanese;
            if (value.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) return SimplifiedChinese;
            if (value.StartsWith("ko", StringComparison.OrdinalIgnoreCase)) return Korean;
            return English;
        }

        public static string ResolveInitialLanguage(string configuredLanguage, string osUiCulture)
        {
            return string.IsNullOrWhiteSpace(configuredLanguage)
                ? NormalizeLanguage(osUiCulture)
                : NormalizeLanguage(configuredLanguage);
        }

        public static string Text(string language, string key, params object[] args)
        {
            var table = Table(NormalizeLanguage(language));
            string value;
            if (!table.TryGetValue(key, out value) && !En.TryGetValue(key, out value))
                value = key;
            return args == null || args.Length == 0 ? value : string.Format(value, args);
        }

        public static bool IsJapanese(string language) => NormalizeLanguage(language) == Japanese;
        public static string OverlayTitle(string language) => Text(language, "OverlayTitle");
        public static string PlayerCount(string language, int count) => Text(language, count == 1 ? "PlayerCountOne" : "PlayerCountMany", count);
        public static string Waiting(string language) => Text(language, "Waiting");
        public static string Connected(string language) => Text(language, "Connected");
        public static string WarningText(string language) => Text(language, "WarningText");

        private static Dictionary<string, string> Table(string language)
        {
            if (language == Japanese) return Ja;
            if (language == SimplifiedChinese) return Zh;
            if (language == Korean) return Ko;
            return En;
        }
    }
}
