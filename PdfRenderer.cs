using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace MathOCRTeacherPro;

public static class PdfRenderer
{
    public static async Task<List<Bitmap>> LoadAsync(string path)
    {
        var file = await StorageFile.GetFileFromPathAsync(path);
        var pdf = await PdfDocument.LoadFromFileAsync(file);
        var pages = new List<Bitmap>();

        for (uint i = 0; i < pdf.PageCount; i++)
        {
            using var page = pdf.GetPage(i);
            using var ras = new InMemoryRandomAccessStream();

            var options = new PdfPageRenderOptions
            {
                DestinationWidth = (uint)Math.Max(1, page.Size.Width * 1.7),
                DestinationHeight = (uint)Math.Max(1, page.Size.Height * 1.7)
            };

            await page.RenderToStreamAsync(ras, options);
            ras.Seek(0);
            using var net = ras.AsStreamForRead();
            using var tmp = new Bitmap(net);
            pages.Add(new Bitmap(tmp));
        }
        return pages;
    }
}
