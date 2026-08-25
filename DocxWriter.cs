using System.IO.Compression;
using System.Security;

namespace MathOCRTeacherPro;

public static class DocxWriter
{
    private static string X(string s) => SecurityElement.Escape(s) ?? "";

    private static string P(string text, bool bold = false)
    {
        var lines = (text ?? "").Replace("\r\n", "\n").Split('\n');
        var runs = string.Join("", lines.Select((line, i) =>
            $"<w:r>{(bold ? "<w:rPr><w:b/></w:rPr>" : "")}<w:t xml:space=\"preserve\">{X(line)}</w:t></w:r>" +
            (i < lines.Length - 1 ? "<w:r><w:br/></w:r>" : "")));
        return $"<w:p>{runs}</w:p>";
    }

    public static void Save(string path, string title, IReadOnlyList<RegionItem> problems)
    {
        if (File.Exists(path)) File.Delete(path);
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);

        void Add(string name, string content)
        {
            var e = zip.CreateEntry(name);
            using var sw = new StreamWriter(e.Open(), new System.Text.UTF8Encoding(false));
            sw.Write(content);
        }

        Add("[Content_Types].xml", """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
<Default Extension="xml" ContentType="application/xml"/>
<Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
<Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
</Types>
""");

        Add("_rels/.rels", """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>
""");

        Add("word/_rels/document.xml.rels", """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"/>
""");

        Add("word/styles.xml", """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
<w:style w:type="paragraph" w:default="1" w:styleId="Normal">
<w:name w:val="Normal"/>
<w:rPr><w:rFonts w:ascii="Malgun Gothic" w:hAnsi="Malgun Gothic" w:eastAsia="Malgun Gothic"/><w:sz w:val="22"/></w:rPr>
</w:style>
</w:styles>
""");

        var body = "";
        if (!string.IsNullOrWhiteSpace(title))
            body += P(title, true);

        int n = 1;
        foreach (var r in problems)
        {
            var text = r.OcrText.Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(text, @"^\s*\d+\s*[\.\)]"))
                text = $"{n}. {text}";
            body += P(text);
            if (!string.IsNullOrWhiteSpace(r.Answer))
                body += P($"정답: {r.Answer}", true);
            body += "<w:p/>";
            n++;
        }

        Add("word/document.xml", $"""
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
<w:body>
{body}
<w:sectPr>
<w:pgSz w:w="11906" w:h="16838"/>
<w:pgMar w:top="1020" w:right="1134" w:bottom="1020" w:left="1134"/>
</w:sectPr>
</w:body>
</w:document>
""");
    }
}
