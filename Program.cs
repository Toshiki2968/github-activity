using System.Text.Json;
using GithubActivity;

PrintWelcomeMessage();
var username = Console.ReadLine();

if (string.IsNullOrWhiteSpace(username))
{
    ConsoleMessage.PrintErrorMessage("エラー: ユーザー名が入力されていません。");
    return;
}

using var client = new HttpClient();
client.DefaultRequestHeaders.UserAgent.Clear();
client.DefaultRequestHeaders.UserAgent.ParseAdd("github-activity");

try
{
    // Githubユーザーのイベントを取得
    var response = await client.GetAsync($"https://api.github.com/users/{username}/events");
    if (!response.IsSuccessStatusCode)
    {
        ConsoleMessage.PrintErrorMessage($"イベントの取得に失敗しました。HTTP status: {response.StatusCode}");
        return;
    }
    var data = await response.Content.ReadAsStringAsync();
    var events = JsonDocument.Parse(data).RootElement;
    foreach (var item in events.EnumerateArray())
    {
        var type = item.GetProperty("type").GetString();
        var repoName = item.GetProperty("repo").GetProperty("name").GetString();
        switch (type)
        {
            case "CreateEvent":
                ConsoleMessage.PrintCommandMessage($"[Create] ユーザー {username} が ブランチ/タグ/リポジトリ:{repoName} を作成しました");
                break;
            case "PushEvent":
                var commitCount = item.GetProperty("payload").GetProperty("commits").GetArrayLength();
                ConsoleMessage.PrintCommandMessage($"[Push] ユーザー {username} が {repoName} に {commitCount} 件のコミットをプッシュしました");
                break;
            case "IssuesEvent":
                var issueNumber = item.GetProperty("payload").GetProperty("issue").GetProperty("number").GetString();
                var issueTitle = item.GetProperty("payload").GetProperty("issue").GetProperty("title").GetString();
                var action = item.GetProperty("payload").GetProperty("action").GetString();
                ConsoleMessage.PrintCommandMessage($"[Issue] ユーザー {username} が Issue #{issueNumber}「{issueTitle}」を {action}（opened/closed/reopened）しました");
                break;
            default:
                break;
        }
    }
}
catch (JsonException ex)
{
    ConsoleMessage.PrintErrorMessage($"JSON の解析中にエラーが発生しました: {ex.Message}");
}

static void PrintWelcomeMessage()
{
    ConsoleMessage.PrintInfoMessage("こんにちは! GitHub Activity Consoleへようこそ。");
    ConsoleMessage.PrintInfoMessage("Githubユーザー名を入力してください。");
}