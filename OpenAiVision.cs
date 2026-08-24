using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MathOCRTeacherPro;

public sealed class OpenAiVision
{
    private readonly HttpClient _http = new();
    private readonly AppSettings _settings;

    public OpenAiVision(AppSettings settings) => _settings = settings;

    private static string ToDataUrl(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
        return "data:image/jpeg;base64," + Convert.ToBase64String(ms.ToArray());
    }

    private async Task<string> AskAsync(Bitmap image, string prompt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new InvalidOperationException("OpenAI API Key가 설정되지 않았습니다.");

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        var body = new
        {
            model = _settings.Model,
            input = new object[]
            {
                new {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "input_text", text = prompt },
                        new { type = "input_image", image_url = ToDataUrl(image) }
                    }
                }
            }
        };

        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var res = await _http.SendAsync(req, ct);
        var json = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI API 오류 {(int)res.StatusCode}\r\n{json}");

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("output_text", out var direct) && direct.ValueKind == JsonValueKind.String)
            return direct.GetString() ?? "";

        if (doc.RootElement.TryGetProperty("output", out var output))
        {
            foreach (var item in output.EnumerateArray())
            {
                if (!item.TryGetProperty("content", out var content)) continue;
                foreach (var c in content.EnumerateArray())
                {
                    if (c.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                        return text.GetString() ?? "";
                }
            }
        }
        throw new InvalidOperationException("AI 응답에서 텍스트를 찾지 못했습니다.");
    }

    private static string ExtractJson(string s)
    {
        s = s.Trim();
        if (s.StartsWith("```"))
        {
            var firstNl = s.IndexOf('\n');
            if (firstNl >= 0) s = s[(firstNl + 1)..];
            var end = s.LastIndexOf("```", StringComparison.Ordinal);
            if (end >= 0) s = s[..end];
        }
        var a = s.IndexOf('{');
        var b = s.LastIndexOf('}');
        return a >= 0 && b > a ? s[a..(b + 1)] : s;
    }

    public async Task<List<RegionItem>> DetectRegionsAsync(Bitmap page, int pageIndex, CancellationToken ct)
    {
        const string prompt = """
당신은 한국 수학 문제지 레이아웃 분석기다.
이미지에서 실제로 풀어야 하는 개별 문제 영역을 각각 사각형으로 찾는다.
문제 번호와 선택지, 조건, 해당 문제의 도형/그래프가 함께 들어가도록 문제 영역을 넉넉히 잡는다.
페이지 제목, 단원 설명, 개념 설명은 문제로 잡지 않는다.
별도로 분리할 가치가 있는 그림/그래프는 image 유형으로 추가할 수 있다.
좌표는 이미지 전체를 기준으로 x,y,w,h를 각각 0~1000 범위의 정수로 정규화한다.
JSON 외의 문장은 쓰지 마라.
{"regions":[{"kind":"problem","x":100,"y":100,"w":300,"h":200}]}
kind는 problem, image, solution 중 하나다.
""";
        var text = await AskAsync(page, prompt, ct);
        using var doc = JsonDocument.Parse(ExtractJson(text));
        var list = new List<RegionItem>();
        var w = page.Width;
        var h = page.Height;

        if (doc.RootElement.TryGetProperty("regions", out var regions))
        {
            foreach (var r in regions.EnumerateArray())
            {
                string kind = r.TryGetProperty("kind", out var k) ? (k.GetString() ?? "problem") : "problem";
                int x = r.GetProperty("x").GetInt32() * w / 1000;
                int y = r.GetProperty("y").GetInt32() * h / 1000;
                int rw = r.GetProperty("w").GetInt32() * w / 1000;
                int rh = r.GetProperty("h").GetInt32() * h / 1000;
                if (rw > 20 && rh > 20)
                    list.Add(new RegionItem { PageIndex = pageIndex, Kind = kind, Rect = new Rectangle(x, y, rw, rh) });
            }
        }
        return list;
    }

    public async Task<(string text, string latex)> OcrAsync(Bitmap crop, CancellationToken ct)
    {
        const string prompt = """
당신은 한국 고등학교 수학 문제 OCR 편집기다.
이미지를 정확히 전사한다. 문항 번호, 한글 문장, 조건, ①②③④⑤ 선택지를 빠뜨리지 않는다.
수식의 분수, 근호, 지수, 로그, 삼각함수, 적분, 절댓값, 집합 기호를 정확히 읽는다.
보이지 않는 내용을 추측하지 않는다.
text에는 한글 워드프로세서에서 읽기 쉬운 형태로 문제 전체를 작성한다.
latex에는 등장하는 핵심 수식을 LaTeX 형태로만 모아 작성한다.
JSON 외의 문장은 쓰지 마라.
{"text":"문제 전체","latex":"수식"}
""";
        var text = await AskAsync(crop, prompt, ct);
        using var doc = JsonDocument.Parse(ExtractJson(text));
        string t = doc.RootElement.TryGetProperty("text", out var te) ? (te.GetString() ?? "") : "";
        string l = doc.RootElement.TryGetProperty("latex", out var la) ? (la.GetString() ?? "") : "";
        return (t, l);
    }
}
