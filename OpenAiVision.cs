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

    public async Task<(string text, string latex, List<OcrSegment> segments, string layoutType, string boxTitle, List<string> choices)> OcrAsync(Bitmap crop, CancellationToken ct)
    {
        const string prompt = """
당신은 한국 고등학교 수학 문제 OCR 및 한컴 한글 수식 변환기다.
문제를 정확히 OCR하고, 일반 문장과 수학식을 반드시 분리한다.
이미지에서 수학 글꼴(이탤릭 변수, 위첨자, 아래첨자, 루트, 분수, 부등호)로 보이는 부분은 단 한 글자라도 text에 넣지 말고 equation 세그먼트로 만든다.
특히 n, m, f(n), g(n), n^2, n-12, m-2n, 2≤n≤10, f(n)=2g(n), f(2)+f(3)+f(4)=3 같은 표현은 모두 equation이다.
한국어 조사와 결합된 경우에도 변수만 equation으로 분리한다. 예: '자연수 n에 대하여' => text:'자연수 ', equation:'n', text:'에 대하여'.
수학식은 반드시 한컴 한글 수식 스크립트로 작성한다. LaTeX를 쓰지 않는다.

한글 수식 스크립트 예:
- n의 제곱: n ^{2}
- 아래첨자: a _{n}
- 분수: {a+b} over {c+d}
- 제곱근: sqrt {x+1}
- 곱하기: times
- 부등식: <=, >=, <, >
- LaTeX 명령어 \\frac, \\sqrt, \\ge, \\le, \\text 는 절대 쓰지 않는다.
- 역슬래시를 사용하지 않는다.

문장 속 변수와 식도 equation 세그먼트로 분리한다.
줄바꿈은 newline 세그먼트를 사용한다.
문항번호/출처/선택지 ①②③④⑤는 text로 둔다.

추가 레이아웃 분석:
- <조건>, <보기>, <자료>, <가정> 등의 제목과 사각형 테두리가 있으면 layout_type="condition_box".
- 연립방정식/연립부등식처럼 여러 식을 큰 왼쪽 중괄호로 묶어야 하면 layout_type="system".
- box_title에는 조건/보기/자료/가정 같은 제목만 넣는다.
- choices에는 객관식 선택지 내용만 순서대로 넣는다.
- 부등호 앞뒤에는 반드시 공백을 둔다. 특히 "< -"와 "> -" 사이를 띄운다.
- 연립식의 중괄호 문자를 text로 출력하지 말고 각 식을 equation으로 분리한다.

반드시 JSON만 출력:
{"text":"전체 문제 백업 텍스트","latex":"","segments":[{"type":"text","content":"문장"},{"type":"equation","content":"n ^{2}+1"},{"type":"newline","content":""}]}
""";
        var raw = await AskAsync(crop, prompt, ct);
        using var doc = JsonDocument.Parse(ExtractJson(raw));
        string text = doc.RootElement.TryGetProperty("text", out var te) ? (te.GetString() ?? "") : "";
        string latex = "";
        var segments = new List<OcrSegment>();
        if (doc.RootElement.TryGetProperty("segments", out var segs) && segs.ValueKind == JsonValueKind.Array)
        {
            foreach (var seg in segs.EnumerateArray())
            {
                var type = seg.TryGetProperty("type", out var ty) ? (ty.GetString() ?? "text") : "text";
                var content = seg.TryGetProperty("content", out var co) ? (co.GetString() ?? "") : "";
                if (type != "text" && type != "equation" && type != "newline") type = "text";
                if (type == "equation")
                    content = content.Replace("\\\\le","<=").Replace("\\\\ge",">=").Replace("\\\\times","times").Replace("\\\\cdot","times");
                segments.Add(new OcrSegment { Type = type, Content = content });
            }
        }
        if (segments.Count == 0) segments.Add(new OcrSegment { Type = "text", Content = text });
        string layoutType = doc.RootElement.TryGetProperty("layout_type", out var lt) ? (lt.GetString() ?? "normal") : "normal";
        string boxTitle = doc.RootElement.TryGetProperty("box_title", out var bt) ? (bt.GetString() ?? "") : "";
        var choices = new List<string>();
        if (doc.RootElement.TryGetProperty("choices", out var ch) && ch.ValueKind == JsonValueKind.Array)
            foreach (var item in ch.EnumerateArray()) choices.Add(item.GetString() ?? "");

        return (text, latex, segments, layoutType, boxTitle, choices);
    }
}
