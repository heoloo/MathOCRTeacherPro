using System.Drawing.Drawing2D;

namespace MathOCRTeacherPro;

public sealed class MainForm : Form
{
    readonly AppSettings settings = AppSettings.Load();
    readonly List<Bitmap> pages = new();
    readonly Dictionary<int, List<RegionItem>> regions = new();

    readonly PictureBox canvas = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(245,246,248), SizeMode = PictureBoxSizeMode.Zoom };
    readonly DataGridView grid = new() { Dock = DockStyle.Fill, AllowUserToAddRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
    readonly Label pageLabel = new() { AutoSize = true, Text = "0 / 0" };
    readonly Label countLabel = new() { AutoSize = true, Text = "문제 0 · 그림 0 · 해설 0" };
    readonly TextBox titleBox = new() { PlaceholderText = "시험지 명 입력 (선택)", Dock = DockStyle.Top };
    readonly ToolStripButton problemBtn = new("문제") { CheckOnClick = true, Checked = true };
    readonly ToolStripButton imageBtn = new("그림") { CheckOnClick = true };
    readonly ToolStripButton solutionBtn = new("해설") { CheckOnClick = true };

    int pageIndex = 0;
    string mode = "problem";
    Point? dragStart;
    Rectangle dragRect;
public MainForm()
    {
        Text = "MathOCR Teacher Pro — PDF/이미지 → HWP";
        Width = 1400;
        Height = 900;
        MinimumSize = new Size(1000, 680);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        Font = new Font("Malgun Gothic", 10);

        var tool = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(12,8,12,8), ImageScalingSize = new Size(24,24) };
        var open = new ToolStripButton("파일 열기");
        var auto = new ToolStripButton("✨ 자동 인식");
        var settingsBtn = new ToolStripButton("설정");
        tool.Items.Add(new ToolStripLabel("∑ MathOCR Teacher Pro") { Font = new Font("Malgun Gothic", 12, FontStyle.Bold) });
        tool.Items.Add(new ToolStripSeparator());
        tool.Items.Add(open);
        tool.Items.Add(new ToolStripSeparator());
        tool.Items.Add(problemBtn);
        tool.Items.Add(imageBtn);
        tool.Items.Add(solutionBtn);
        tool.Items.Add(new ToolStripSeparator());
        tool.Items.Add(auto);
        tool.Items.Add(settingsBtn);

        open.Click += async (_,__) => await OpenFileAsync();
        auto.Click += async (_,__) => await AutoDetectAsync();
        settingsBtn.Click += (_,__) => ShowSettings();
        problemBtn.Click += (_,__) => SetMode("problem");
        imageBtn.Click += (_,__) => SetMode("image");
        solutionBtn.Click += (_,__) => SetMode("solution");

        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 880, FixedPanel = FixedPanel.Panel2, Panel2MinSize = 440 };
        Controls.Add(split);
        Controls.Add(tool);
        tool.Dock = DockStyle.Top;

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        left.Controls.Add(canvas, 0,0);

        var nav = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(15,8,15,8) };
        var prev = new Button { Text = "◀", Width = 55 };
        var next = new Button { Text = "▶", Width = 55 };
        prev.Click += (_,__) => { if(pageIndex>0){ pageIndex--; RefreshAll(); } };
        next.Click += (_,__) => { if(pageIndex+1<pages.Count){ pageIndex++; RefreshAll(); } };
        nav.Controls.Add(prev); nav.Controls.Add(pageLabel); nav.Controls.Add(next);
        left.Controls.Add(nav,0,1);
        split.Panel1.Controls.Add(left);

        canvas.Paint += Canvas_Paint;
        canvas.MouseDown += Canvas_MouseDown;
        canvas.MouseMove += Canvas_MouseMove;
        canvas.MouseUp += Canvas_MouseUp;

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, Padding = new Padding(8) };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        right.Controls.Add(titleBox,0,0);
        right.Controls.Add(countLabel,0,1);

        grid.Columns.Add("num","문항");
        grid.Columns.Add("kind","종류");
        grid.Columns.Add("answer","답안");
        grid.Columns.Add("page","페이지");
        grid.Columns[0].ReadOnly = true;
        grid.Columns[1].ReadOnly = true;
        grid.Columns[3].ReadOnly = true;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.CellEndEdit += (_,__) => SyncAnswers();
        right.Controls.Add(grid,0,2);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var up = new Button { Text = "▲", Width = 55 };
        var down = new Button { Text = "▼", Width = 55 };
        var del = new Button { Text = "삭제", Width = 75 };
        up.Click += (_,__) => MoveSelected(-1);
        down.Click += (_,__) => MoveSelected(1);
        del.Click += (_,__) => DeleteSelected();
        buttons.Controls.Add(up); buttons.Controls.Add(down); buttons.Controls.Add(del);
        right.Controls.Add(buttons,0,3);

        var convert = new Button { Text = "HWP로 변환", Dock = DockStyle.Fill, BackColor = Color.FromArgb(124,58,237), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Malgun Gothic",12,FontStyle.Bold) };
        convert.FlatAppearance.BorderSize = 0;
        convert.Click += async (_,__) => await ConvertAsync();
        right.Controls.Add(convert,0,5);
        split.Panel2.Controls.Add(right);

        Shown += (_,__) =>
        {
            try
            {
                var desiredRight = Math.Min(500, Math.Max(440, ClientSize.Width / 3));
                split.SplitterDistance = Math.Max(500, split.ClientSize.Width - desiredRight);
            }
            catch { }
        };
    }

    void SetMode(string m)
    {
        mode = m;
        problemBtn.Checked = m=="problem";
        imageBtn.Checked = m=="image";
        solutionBtn.Checked = m=="solution";
    }

    async Task OpenFileAsync()
    {
        using var dlg = new OpenFileDialog { Filter = "PDF/이미지|*.pdf;*.png;*.jpg;*.jpeg;*.bmp;*.webp" };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try
        {
            Cursor = Cursors.WaitCursor;
            foreach(var p in pages) p.Dispose();
            pages.Clear(); regions.Clear(); pageIndex = 0;

            if (Path.GetExtension(dlg.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                pages.AddRange(await PdfRenderer.LoadAsync(dlg.FileName));
            else
            {
                using var temp = new Bitmap(dlg.FileName);
                pages.Add(new Bitmap(temp));
            }
            for(int i=0;i<pages.Count;i++) regions[i] = new List<RegionItem>();
            RefreshAll();
        }
        catch(Exception ex){ MessageBox.Show(ex.Message,"파일 열기 오류"); }
        finally { Cursor = Cursors.Default; }
    }

    Rectangle ImageDisplayRect()
    {
        if (pages.Count==0) return Rectangle.Empty;
        var img = pages[pageIndex];
        double scale = Math.Min((double)canvas.ClientSize.Width/img.Width, (double)canvas.ClientSize.Height/img.Height);
        int w=(int)(img.Width*scale), h=(int)(img.Height*scale);
        return new Rectangle((canvas.ClientSize.Width-w)/2,(canvas.ClientSize.Height-h)/2,w,h);
    }

    Rectangle ScreenToImage(Rectangle r)
    {
        var d=ImageDisplayRect();
        r=Rectangle.Intersect(r,d);
        if(r.Width<5||r.Height<5) return Rectangle.Empty;
        var img=pages[pageIndex];
        double sx=(double)img.Width/d.Width, sy=(double)img.Height/d.Height;
        return new Rectangle(
            (int)((r.X-d.X)*sx),(int)((r.Y-d.Y)*sy),
            Math.Max(1,(int)(r.Width*sx)),Math.Max(1,(int)(r.Height*sy)));
    }

    Rectangle ImageToScreen(Rectangle r)
    {
        var d=ImageDisplayRect(); var img=pages[pageIndex];
        double sx=(double)d.Width/img.Width, sy=(double)d.Height/img.Height;
        return new Rectangle(d.X+(int)(r.X*sx),d.Y+(int)(r.Y*sy),(int)(r.Width*sx),(int)(r.Height*sy));
    }

    void Canvas_Paint(object? sender, PaintEventArgs e)
    {
        if(pages.Count==0)
        {
            e.Graphics.DrawString("PDF 또는 이미지 파일을 열어주세요", new Font("Malgun Gothic",15), Brushes.Gray, new PointF(40,40));
            return;
        }
        var d=ImageDisplayRect();
        e.Graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
        e.Graphics.DrawImage(pages[pageIndex],d);

        var list=regions[pageIndex];
        for(int i=0;i<list.Count;i++)
        {
            var rr=ImageToScreen(list[i].Rect);
            var color=list[i].Kind=="image"?Color.SeaGreen:list[i].Kind=="solution"?Color.DarkOrange:Color.DeepSkyBlue;
            using var pen=new Pen(color,3);
            e.Graphics.DrawRectangle(pen,rr);
            using var br=new SolidBrush(color);
            e.Graphics.FillRectangle(br,new Rectangle(rr.X,rr.Y,40,24));
            e.Graphics.DrawString((i+1).ToString(),Font,Brushes.White,new RectangleF(rr.X,rr.Y,40,24));
        }
        if(dragStart!=null && dragRect.Width>0)
        {
            using var p=new Pen(Color.MediumPurple,2){DashStyle=DashStyle.Dash};
            e.Graphics.DrawRectangle(p,dragRect);
        }
    }

    void Canvas_MouseDown(object? s, MouseEventArgs e)
    {
        if(e.Button==MouseButtons.Left && pages.Count>0 && ImageDisplayRect().Contains(e.Location))
            dragStart=e.Location;
    }
    void Canvas_MouseMove(object? s, MouseEventArgs e)
    {
        if(dragStart==null) return;
        var a=dragStart.Value;
        dragRect=Rectangle.FromLTRB(Math.Min(a.X,e.X),Math.Min(a.Y,e.Y),Math.Max(a.X,e.X),Math.Max(a.Y,e.Y));
        canvas.Invalidate();
    }
    void Canvas_MouseUp(object? s, MouseEventArgs e)
    {
        if(dragStart==null) return;
        var ir=ScreenToImage(dragRect);
        dragStart=null; dragRect=Rectangle.Empty;
        if(!ir.IsEmpty) regions[pageIndex].Add(new RegionItem{PageIndex=pageIndex,Kind=mode,Rect=ir});
        RefreshAll();
    }

    List<RegionItem> AllRegions()=>regions.OrderBy(k=>k.Key).SelectMany(k=>k.Value).ToList();

    void RefreshAll()
    {
        if(pages.Count>0){ canvas.Image=null; pageLabel.Text=$"{pageIndex+1} / {pages.Count}"; }
        else pageLabel.Text="0 / 0";
        grid.Rows.Clear();
        int pn=0, pc=0, ic=0, sc=0;
        foreach(var r in AllRegions())
        {
            if(r.Kind=="problem"){pn++;pc++;} else if(r.Kind=="image")ic++; else sc++;
            string kind=r.Kind=="problem"?"문제":r.Kind=="image"?"그림":"해설";
            grid.Rows.Add(r.Kind=="problem"?pn.ToString():"—",kind,r.Answer,(r.PageIndex+1).ToString());
        }
        countLabel.Text=$"문제 {pc} · 그림 {ic} · 해설 {sc}";
        canvas.Invalidate();
    }

    void SyncAnswers()
    {
        var all=AllRegions();
        for(int i=0;i<Math.Min(all.Count,grid.Rows.Count);i++)
            all[i].Answer=grid.Rows[i].Cells[2].Value?.ToString()??"";
    }

    (int page,int local)? Locate(int global)
    {
        int c=0;
        foreach(var kv in regions.OrderBy(k=>k.Key))
            for(int i=0;i<kv.Value.Count;i++,c++)
                if(c==global)return(kv.Key,i);
        return null;
    }

    void DeleteSelected()
    {
        if(grid.SelectedRows.Count==0)return;
        var loc=Locate(grid.SelectedRows[0].Index);
        if(loc==null)return;
        regions[loc.Value.page].RemoveAt(loc.Value.local);
        RefreshAll();
    }

    void MoveSelected(int dir)
    {
        if(grid.SelectedRows.Count==0)return;
        int idx=grid.SelectedRows[0].Index;
        var all=AllRegions();
        int to=idx+dir;
        if(to<0||to>=all.Count)return;
        (all[idx],all[to])=(all[to],all[idx]);

        // Preserve the new global ordering by reassigning into pages sequentially.
        // For simplicity this keeps each region's page assignment but reorders within page when possible.
        var grouped=all.GroupBy(r=>r.PageIndex).ToDictionary(g=>g.Key,g=>g.ToList());
        foreach(var k in regions.Keys.ToList()) regions[k]=grouped.TryGetValue(k,out var l)?l:new List<RegionItem>();
        RefreshAll();
        if(to<grid.Rows.Count) grid.Rows[to].Selected=true;
    }

    Bitmap Crop(RegionItem r)
    {
        var src=pages[r.PageIndex];
        var rect=Rectangle.Intersect(new Rectangle(0,0,src.Width,src.Height),r.Rect);
        return src.Clone(rect,src.PixelFormat);
    }

    bool EnsureApi()
    {
        if(!string.IsNullOrWhiteSpace(settings.ApiKey))return true;
        ShowSettings();
        return !string.IsNullOrWhiteSpace(settings.ApiKey);
    }

    async Task AutoDetectAsync()
    {
        if(pages.Count==0){MessageBox.Show("먼저 PDF 또는 이미지를 열어주세요.");return;}
        if(!EnsureApi())return;
        try
        {
            UseWaitCursor=true;
            var ai=new OpenAiVision(settings);
            var found=await ai.DetectRegionsAsync(pages[pageIndex],pageIndex,CancellationToken.None);
            regions[pageIndex].AddRange(found);
            RefreshAll();
            MessageBox.Show($"{found.Count}개 영역을 찾았습니다.\r\n잘못 잡힌 영역은 삭제하고 직접 다시 지정할 수 있습니다.","자동 인식");
        }
        catch(Exception ex){MessageBox.Show(ex.Message,"AI 오류");}
        finally{UseWaitCursor=false;}
    }

    async Task ConvertAsync()
    {
        SyncAnswers();
        var problems=AllRegions().Where(r=>r.Kind=="problem").ToList();
        if(problems.Count==0){MessageBox.Show("문제 영역을 하나 이상 지정해주세요.");return;}
        if(!EnsureApi())return;

        using var save=new SaveFileDialog{Filter="Word 문서|*.docx",FileName=string.IsNullOrWhiteSpace(titleBox.Text)?"MathOCR_변환.docx":titleBox.Text+".docx"};
        if(save.ShowDialog()!=DialogResult.OK)return;

        try
        {
            var ai=new OpenAiVision(settings);
            using var progress=new ProgressForm(problems.Count);
            progress.Show(this);
            for(int i=0;i<problems.Count;i++)
            {
                progress.SetProgress(i,$"문제 {i+1}/{problems.Count} OCR 중...");
                using var crop=Crop(problems[i]);
                var result=await ai.OcrAsync(crop,CancellationToken.None);
                problems[i].OcrText=result.text;
                problems[i].Latex=result.latex;
                Application.DoEvents();
            }
            progress.SetProgress(problems.Count,"문서 생성 중...");
            DocxWriter.Save(save.FileName,titleBox.Text,problems);

            string msg=$"DOCX 생성 완료:\r\n{save.FileName}";
            if(settings.MakeHwp)
            {
                var hwp=Path.ChangeExtension(save.FileName,".hwp");
                if(HwpExporter.TryExport(save.FileName,hwp,out var err))
                    msg+=$"\r\n\r\nHWP 생성 완료:\r\n{hwp}";
                else
                    msg+=$"\r\n\r\nHWP 자동 저장은 실패했습니다.\r\n{err}\r\nDOCX는 한글에서 바로 열 수 있습니다.";
            }
            progress.Close();
            MessageBox.Show(msg,"변환 완료");
        }
        catch(Exception ex){MessageBox.Show(ex.Message,"변환 오류");}
    }

    void ShowSettings()
    {
        using var f=new Form{Text="설정",Width=560,Height=250,StartPosition=FormStartPosition.CenterParent,Font=Font};
        var api=new TextBox{Text=settings.ApiKey,UseSystemPasswordChar=true,Dock=DockStyle.Fill,PlaceholderText="sk-... API Key 붙여넣기"};
        var model=new TextBox{Text=settings.Model,Dock=DockStyle.Fill};
        var hwp=new CheckBox{Text="한컴오피스가 있으면 HWP도 자동 생성",Checked=settings.MakeHwp,AutoSize=true};
        var ok=new Button{Text="저장",DialogResult=DialogResult.OK,Width=90};
        var t=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(12),ColumnCount=2,RowCount=4};
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,120));t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        t.Controls.Add(new Label{Text="OpenAI API Key",AutoSize=true},0,0);t.Controls.Add(api,1,0);
        t.Controls.Add(new Label{Text="AI 모델",AutoSize=true},0,1);t.Controls.Add(model,1,1);
        t.Controls.Add(hwp,1,2);t.Controls.Add(ok,1,3);
        f.Controls.Add(t);f.AcceptButton=ok;
        if(f.ShowDialog(this)==DialogResult.OK)
        {settings.ApiKey=api.Text.Trim();settings.Model=model.Text.Trim();settings.MakeHwp=hwp.Checked;settings.Save();}
    }
}

public sealed class ProgressForm:Form
{
    readonly ProgressBar bar=new(){Dock=DockStyle.Bottom,Minimum=0};
    readonly Label label=new(){Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,Font=new Font("Malgun Gothic",11)};
    public ProgressForm(int max)
    {
        Text="변환 중";Width=420;Height=150;StartPosition=FormStartPosition.CenterParent;ControlBox=false;
        bar.Maximum=Math.Max(1,max);Controls.Add(label);Controls.Add(bar);
    }
    public void SetProgress(int v,string text){bar.Value=Math.Min(bar.Maximum,Math.Max(0,v));label.Text=text;Refresh();}
}
