# -*- coding: utf-8 -*-
"""
Generate an interactive, printable HTML report file (EasyStay_Hotel_Project_Report.html)
from EasyStay_Hotel_Project_Report.md with full Thai typography, Mermaid diagrams,
syntax highlighting, and A4 print stylesheet.
"""

import os
import json

md_path = "EasyStay_Hotel_Project_Report.md"
html_path = "EasyStay_Hotel_Project_Report.html"

with open(md_path, "r", encoding="utf-8") as f:
    md_content = f.read()

# Escape json for embedding in script tag
md_json = json.dumps(md_content)

html_template = f"""<!DOCTYPE html>
<html lang="th">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>เอกสารรายงานโปรเจกต์: Easy Stay Hotel & Resort</title>
    <!-- Google Fonts: Sarabun & Kanit & JetBrains Mono -->
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Fira+Code:wght@400;500&family=Kanit:wght@300;400;500;600;700&family=Sarabun:ital,wght@0,300;0,400;0,500;0,600;0,700;1,400&display=swap" rel="stylesheet">
    
    <!-- Marked.js (Markdown Parser) -->
    <script src="https://cdn.jsdelivr.net/npm/marked/marked.min.js"></script>
    <!-- Highlight.js (Code Syntax) -->
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/styles/github.min.css">
    <script src="https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/highlight.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/csharp.min.js"></script>
    <!-- Mermaid.js (Diagrams) -->
    <script src="https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js"></script>
    <!-- Bootstrap Icons -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css">

    <style>
        :root {{
            --primary: #1e3a8a;
            --primary-light: #3b82f6;
            --accent: #0284c7;
            --text-dark: #1e293b;
            --text-muted: #64748b;
            --bg-page: #f8fafc;
            --border-color: #e2e8f0;
        }}

        * {{
            box-sizing: border-box;
        }}

        body {{
            font-family: 'Sarabun', -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
            font-size: 15px;
            line-height: 1.75;
            color: var(--text-dark);
            background-color: var(--bg-page);
            margin: 0;
            padding: 0;
        }}

        .top-bar {{
            background: #ffffff;
            border-bottom: 1px solid var(--border-color);
            padding: 12px 24px;
            position: sticky;
            top: 0;
            z-index: 1000;
            display: flex;
            justify-content: space-between;
            align-items: center;
            box-shadow: 0 1px 3px rgba(0,0,0,0.05);
        }}

        .top-bar h1 {{
            font-family: 'Kanit', sans-serif;
            font-size: 18px;
            margin: 0;
            color: var(--primary);
            font-weight: 600;
        }}

        .print-btn {{
            background: var(--primary);
            color: white;
            border: none;
            padding: 8px 18px;
            border-radius: 8px;
            font-family: 'Kanit', sans-serif;
            font-size: 14px;
            font-weight: 500;
            cursor: pointer;
            display: inline-flex;
            align-items: center;
            gap: 8px;
            transition: all 0.2s ease;
        }}

        .print-btn:hover {{
            background: #172554;
            transform: translateY(-1px);
        }}

        .doc-container {{
            max-width: 900px;
            margin: 32px auto;
            background: #ffffff;
            padding: 56px 64px;
            box-shadow: 0 4px 20px rgba(0,0,0,0.06);
            border-radius: 12px;
            border: 1px solid var(--border-color);
        }}

        /* Headings */
        h1, h2, h3, h4, h5, h6 {{
            font-family: 'Kanit', sans-serif;
            color: #0f172a;
            font-weight: 600;
            line-height: 1.35;
        }}

        h1 {{
            font-size: 26px;
            border-bottom: 2px solid var(--primary);
            padding-bottom: 8px;
            margin-top: 36px;
            margin-bottom: 20px;
            color: var(--primary);
        }}

        h2 {{
            font-size: 22px;
            border-bottom: 1px solid #cbd5e1;
            padding-bottom: 6px;
            margin-top: 32px;
            margin-bottom: 16px;
            color: #1e3a8a;
        }}

        h3 {{
            font-size: 18px;
            margin-top: 24px;
            margin-bottom: 12px;
            color: #0369a1;
        }}

        h4 {{
            font-size: 16px;
            margin-top: 20px;
            margin-bottom: 10px;
        }}

        /* Tables */
        table {{
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
            font-size: 14px;
            background: #ffffff;
            border: 1px solid var(--border-color);
            border-radius: 8px;
            overflow: hidden;
        }}

        th {{
            background: #f1f5f9;
            color: #1e293b;
            font-family: 'Kanit', sans-serif;
            font-weight: 600;
            text-align: left;
            padding: 10px 14px;
            border-bottom: 2px solid #cbd5e1;
            border-right: 1px solid var(--border-color);
        }}

        td {{
            padding: 10px 14px;
            border-bottom: 1px solid var(--border-color);
            border-right: 1px solid var(--border-color);
            vertical-align: top;
        }}

        tr:nth-child(even) {{
            background-color: #f8fafc;
        }}

        tr:hover {{
            background-color: #f1f5f9;
        }}

        /* Code Blocks */
        pre {{
            background: #f8fafc;
            border: 1px solid #e2e8f0;
            border-radius: 8px;
            padding: 16px;
            overflow-x: auto;
            margin: 16px 0;
            font-size: 13px;
        }}

        code {{
            font-family: 'Fira Code', 'JetBrains Mono', Consolas, monospace;
            background: #f1f5f9;
            color: #0f172a;
            padding: 2px 6px;
            border-radius: 4px;
            font-size: 13px;
        }}

        pre code {{
            background: transparent;
            padding: 0;
            font-size: 12.5px;
            line-height: 1.55;
        }}

        /* Blockquotes */
        blockquote {{
            border-left: 4px solid var(--primary-light);
            background: #f0fdf4;
            padding: 12px 18px;
            margin: 16px 0;
            border-radius: 0 8px 8px 0;
            color: #166534;
        }}

        /* Diagrams */
        .mermaid {{
            background: #ffffff;
            border: 1px solid #e2e8f0;
            border-radius: 8px;
            padding: 20px;
            margin: 20px 0;
            text-align: center;
            overflow-x: auto;
        }}

        hr {{
            border: none;
            border-top: 1px solid var(--border-color);
            margin: 32px 0;
        }}

        /* Screen Output Placeholder Box */
        p:has(em:contains("ภาพหน้าจอ")), em {{
            color: var(--text-muted);
        }}

        /* Print Style */
        @media print {{
            body {{
                background: #ffffff;
                font-size: 12pt;
                line-height: 1.5;
            }}
            .top-bar {{
                display: none;
            }}
            .doc-container {{
                max-width: 100%;
                margin: 0;
                padding: 0;
                border: none;
                box-shadow: none;
            }}
            h1, h2 {{
                page-break-after: avoid;
            }}
            pre, table, .mermaid {{
                page-break-inside: avoid;
            }}
            div[style*="page-break-after"] {{
                page-break-after: always;
            }}
        }}
    </style>
</head>
<body>

    <div class="top-bar">
        <h1><i class="bi bi-book-half me-2"></i> Easy Stay Hotel & Resort - รายงานโครงงานฉบับสมบูรณ์</h1>
        <button class="print-btn" onclick="window.print()">
            <i class="bi bi-printer"></i> พิมพ์เอกสาร / บันทึกเป็น PDF (Ctrl+P)
        </button>
    </div>

    <div class="doc-container" id="content">
        <!-- Markdown rendered content will appear here -->
    </div>

    <script>
        const rawMarkdown = {md_json};

        // Initialize Mermaid
        mermaid.initialize({{
            startOnLoad: false,
            theme: 'default',
            flowchart: {{
                useMaxWidth: true,
                htmlLabels: true,
                curve: 'basis'
            }}
        }});

        // Custom renderer for marked
        const renderer = new marked.Renderer();
        const origCodeRenderer = renderer.code.bind(renderer);

        renderer.code = function(code, language) {{
            if (language === 'mermaid') {{
                return '<div class="mermaid">' + code + '</div>';
            }}
            return origCodeRenderer(code, language);
        }};

        marked.setOptions({{
            renderer: renderer,
            highlight: function(code, lang) {{
                const language = hljs.getLanguage(lang) ? lang : 'plaintext';
                return hljs.highlight(code, {{ language }}).value;
            }},
            breaks: true,
            gfm: true
        }});

        // Render Markdown to HTML
        document.getElementById('content').innerHTML = marked.parse(rawMarkdown);

        // Render Mermaid Diagrams
        setTimeout(() => {{
            mermaid.run();
        }}, 100);
    </script>
</body>
</html>
"""

with open(html_path, "w", encoding="utf-8") as f:
    f.write(html_template)

print(f"Generated {html_path} successfully ({len(html_template)} bytes)")
