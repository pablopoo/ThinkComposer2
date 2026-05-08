using System.Net;

namespace Instrumind.ThinkComposer.Core.Rendering;

public static class CompositionSnapshotHtmlExporter
{
    public static string Export(CompositionViewSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        var title = WebUtility.HtmlEncode(snapshot.Title);
        var svg = CompositionSnapshotSvgExporter.Export(snapshot);
        return $$"""
            <!doctype html>
            <html>
            <head>
              <meta charset="utf-8">
              <title>{{title}}</title>
              <style>
                body { margin: 24px; font-family: Segoe UI, Arial, sans-serif; color: #1f1f1f; }
                h1 { font-size: 18px; font-weight: 600; margin: 0 0 16px; }
                svg { max-width: 100%; height: auto; border: 1px solid #e5e5e5; }
                @media print { body { margin: 0; } h1 { margin: 12px; } svg { border: 0; } }
              </style>
            </head>
            <body>
              <h1>{{title}}</h1>
              {{svg}}
            </body>
            </html>
            """;
    }
}
