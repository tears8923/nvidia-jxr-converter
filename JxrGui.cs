using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WinColor = System.Drawing.Color;
using WinFont = System.Drawing.Font;
using WinFontStyle = System.Drawing.FontStyle;
using WinGraphics = System.Drawing.Graphics;
using WinImage = System.Drawing.Image;
using AnchorStyles = System.Windows.Forms.AnchorStyles;

namespace JxrConverter
{
    public class MainForm : Form
    {
        private ListBox lstFiles;
        private List<string> fullPaths = new List<string>();
        private RadioButton rbJpg, rbPng, rbBoth;
        private RadioButton rbSameDir, rbCustomDir;
        private TextBox txtOutDir;
        private Button btnBrowseDir, btnAddFiles, btnAddFolder, btnRemove, btnClear, btnConvert, btnLang, btnOpenDir, btnCancel;
        private Label lblTitle, lblList, lblStatus, lblQuality, lblMode;
        private GroupBox grpFmt, grpOut, grpOpt;
        private TrackBar tbQuality;
        private ComboBox cbMode;
        private ProgressBar pbar;
        private PictureBox picPreview;

        private bool isZh = true;
        private float scale = 1.0f;
        private volatile bool cancelRequested = false;
        private volatile bool isConverting = false;
        private Dictionary<string, string> zh = new Dictionary<string, string>();
        private Dictionary<string, string> en = new Dictionary<string, string>();

        private string cfgPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JxrConverter", "settings.ini");

        // 逻辑坐标 → 物理像素
        private int S(float v) { return (int)Math.Round(v * scale); }
        // 逻辑像素字号 → 物理像素（Pixel 单位，避免 point 双重换算）
        private WinFont PxFont(float logicalPx) { return new WinFont("Microsoft YaHei UI", logicalPx * scale, System.Drawing.GraphicsUnit.Pixel); }
        private WinFont PxFont(float logicalPx, bool bold) { return new WinFont("Microsoft YaHei UI", logicalPx * scale, bold ? WinFontStyle.Bold : WinFontStyle.Regular, System.Drawing.GraphicsUnit.Pixel); }

        public MainForm()
        {
            InitCommon();
        }

        public MainForm(IEnumerable<string> initialFiles)
        {
            InitCommon();
            if (initialFiles != null)
                AddFiles(initialFiles);
        }

        private void InitCommon()
        {
            try
            {
                using (var g = CreateGraphics())
                    scale = g.DpiX / 96.0f;
            }
            catch { scale = 1.0f; }
            if (scale < 1f) scale = 1f;

            isZh = CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
            BuildDicts();
            InitUi();
            LoadSettings();
            ApplyLang();
            AutoScanNvidiaFolder();
        }

        private void BuildDicts()
        {
            zh["title"] = "JXR 转格式工具";
            zh["subtitle"] = "把 NVIDIA HDR 截图 (.jxr) 转成手机能看的 JPG / PNG";
            zh["list"] = "待转换的文件（可拖拽 .jxr 到这里，双击预览）：";
            zh["add_files"] = "添加文件";
            zh["add_folder"] = "添加文件夹";
            zh["remove"] = "移除选中";
            zh["clear"] = "清空列表";
            zh["fmt_group"] = "输出格式";
            zh["jpg"] = "JPG（体积小，发微信）";
            zh["png"] = "PNG（无损画质）";
            zh["both"] = "两者都要";
            zh["out_group"] = "输出位置";
            zh["same_dir"] = "每个文件旁的 done 文件夹";
            zh["custom_dir"] = "统一输出到：";
            zh["browse"] = "浏览";
            zh["opt_group"] = "画质选项";
            zh["quality"] = "JPG 质量：{0}";
            zh["mode"] = "色调映射：";
            zh["mode_natural"] = "自然";
            zh["mode_vivid"] = "鲜艳";
            zh["mode_film"] = "电影感";
            zh["convert"] = "开 始 转 换";
            zh["open_dir"] = "打开输出文件夹";
            zh["ready"] = "就绪";
            zh["scanned"] = "已自动扫描到 {0} 个 .jxr 文件";
            zh["converting"] = "正在转换 ({0}/{1})：{2}";
            zh["done_msg"] = "转换完成！\n\n成功：{0} 个\n失败：{1} 个";
            zh["done_err"] = "\n\n失败详情：\n{0}";
            zh["done_title"] = "完成";
            zh["done_status"] = "完成：成功 {0} 个，失败 {1} 个";
            zh["no_files"] = "请先添加要转换的 .jxr 文件。\n\n可以直接拖拽 .jxr 文件到窗口。";
            zh["no_outdir"] = "请选择统一输出文件夹。";
            zh["tip_title"] = "提示";
            zh["no_jxr_in_folder"] = "该文件夹下没有找到 .jxr 文件。";
            zh["open_title"] = "选择 .jxr 文件";
            zh["open_filter"] = "JXR 截图 (*.jxr)|*.jxr|所有文件 (*.*)|*.*";
            zh["folder_title"] = "选择包含 .jxr 文件的文件夹";
            zh["folder_out_title"] = "选择统一输出文件夹";
            zh["lang_btn"] = "English";
            zh["cancel"] = "取消转换";
            zh["cancelled"] = "已取消：成功 {0} 个，剩余 {1} 个未处理";

            en["title"] = "JXR Converter";
            en["subtitle"] = "Convert NVIDIA HDR screenshots (.jxr) to JPG / PNG for phones";
            en["list"] = "Files to convert (drag .jxr here, double-click to preview):";
            en["add_files"] = "Add Files";
            en["add_folder"] = "Add Folder";
            en["remove"] = "Remove";
            en["clear"] = "Clear";
            en["fmt_group"] = "Output Format";
            en["jpg"] = "JPG (small, for chat apps)";
            en["png"] = "PNG (lossless)";
            en["both"] = "Both";
            en["out_group"] = "Output Location";
            en["same_dir"] = "done folder next to each file";
            en["custom_dir"] = "Custom folder:";
            en["browse"] = "Browse";
            en["opt_group"] = "Quality Options";
            en["quality"] = "JPG quality: {0}";
            en["mode"] = "Tone map:";
            en["mode_natural"] = "Natural";
            en["mode_vivid"] = "Vivid";
            en["mode_film"] = "Filmic";
            en["convert"] = "CONVERT";
            en["open_dir"] = "Open Output Folder";
            en["ready"] = "Ready";
            en["scanned"] = "Auto-scanned {0} .jxr file(s)";
            en["converting"] = "Converting ({0}/{1}): {2}";
            en["done_msg"] = "Conversion complete!\n\nSuccess: {0}\nFailed: {1}";
            en["done_err"] = "\n\nFailed details:\n{0}";
            en["done_title"] = "Done";
            en["done_status"] = "Done: {0} success, {1} failed";
            en["no_files"] = "Please add .jxr files first.\n\nYou can drag .jxr files onto this window.";
            en["no_outdir"] = "Please choose a custom output folder.";
            en["tip_title"] = "Notice";
            en["no_jxr_in_folder"] = "No .jxr files found in that folder.";
            en["open_title"] = "Select .jxr files";
            en["open_filter"] = "JXR screenshots (*.jxr)|*.jxr|All files (*.*)|*.*";
            en["folder_title"] = "Select a folder containing .jxr files";
            en["folder_out_title"] = "Select output folder";
            en["lang_btn"] = "中文";
            en["cancel"] = "Cancel";
            en["cancelled"] = "Cancelled: {0} done, {1} remaining";
        }

        private string T(string key) { string v; return (isZh ? zh : en).TryGetValue(key, out v) ? v : key; }
        private string TF(string key, params object[] args) { return string.Format(T(key), args); }

        private void ApplyLang()
        {
            Text = T("title");
            lblTitle.Text = T("subtitle");
            lblList.Text = T("list");
            btnAddFiles.Text = T("add_files");
            btnAddFolder.Text = T("add_folder");
            btnRemove.Text = T("remove");
            btnClear.Text = T("clear");
            grpFmt.Text = T("fmt_group");
            rbJpg.Text = T("jpg");
            rbPng.Text = T("png");
            rbBoth.Text = T("both");
            grpOut.Text = T("out_group");
            rbSameDir.Text = T("same_dir");
            rbCustomDir.Text = T("custom_dir");
            btnBrowseDir.Text = T("browse");
            grpOpt.Text = T("opt_group");
            lblMode.Text = T("mode");
            btnConvert.Text = T("convert");
            btnOpenDir.Text = T("open_dir");
            btnLang.Text = T("lang_btn");
            btnCancel.Text = T("cancel");
            UpdateModeCombo();
            UpdateQualityLabel();
        }

        private void UpdateModeCombo()
        {
            if (cbMode == null) return;
            int sel = cbMode.SelectedIndex;
            cbMode.Items.Clear();
            cbMode.Items.Add(T("mode_natural"));
            cbMode.Items.Add(T("mode_vivid"));
            cbMode.Items.Add(T("mode_film"));
            cbMode.SelectedIndex = (sel >= 0 && sel < 3) ? sel : 0;
        }

        private void UpdateQualityLabel()
        {
            if (lblQuality != null && tbQuality != null)
                lblQuality.Text = TF("quality", tbQuality.Value);
        }

        private void InitUi()
        {
            ClientSize = new System.Drawing.Size(S(780), S(600));
            MinimumSize = new System.Drawing.Size(S(780), S(600));
            Font = PxFont(12.5f);
            AllowDrop = true;

            // 语言切换（右上角）
            btnLang = new Button();
            btnLang.SetBounds(S(680), S(12), S(84), S(30));
            btnLang.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLang.Click += (s, e) => { isZh = !isZh; ApplyLang(); };
            Controls.Add(btnLang);

            // 标题（限宽，避免顶到语言按钮）
            lblTitle = new Label();
            lblTitle.Font = PxFont(15f, true);
            lblTitle.SetBounds(S(16), S(14), S(650), S(26));
            lblTitle.AutoSize = false;
            lblTitle.AutoEllipsis = true;
            lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            Controls.Add(lblTitle);

            // 列表标签
            lblList = new Label();
            lblList.SetBounds(S(16), S(48), S(650), S(20));
            lblList.AutoSize = false;
            lblList.AutoEllipsis = true;
            Controls.Add(lblList);

            // 预览图（右上）
            picPreview = new PictureBox();
            picPreview.SetBounds(S(580), S(74), S(184), S(116));
            picPreview.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            picPreview.SizeMode = PictureBoxSizeMode.Zoom;
            picPreview.BorderStyle = BorderStyle.FixedSingle;
            picPreview.BackColor = WinColor.FromArgb(24, 24, 24);
            Controls.Add(picPreview);

            // 文件列表
            lstFiles = new ListBox();
            lstFiles.SetBounds(S(16), S(74), S(556), S(190));
            lstFiles.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lstFiles.HorizontalScrollbar = true;
            lstFiles.IntegralHeight = false;
            lstFiles.SelectedIndexChanged += (s, e) => PreviewSelected();
            Controls.Add(lstFiles);

            // 文件操作按钮
            btnAddFiles = MakeButton("", S(16), S(274), S(112));
            btnAddFiles.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnAddFiles.Click += (s, e) => AddFilesDialog();
            Controls.Add(btnAddFiles);

            btnAddFolder = MakeButton("", S(134), S(274), S(112));
            btnAddFolder.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnAddFolder.Click += (s, e) => AddFolderDialog();
            Controls.Add(btnAddFolder);

            btnRemove = MakeButton("", S(252), S(274), S(112));
            btnRemove.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnRemove.Click += (s, e) => RemoveSelected();
            Controls.Add(btnRemove);

            btnClear = MakeButton("", S(370), S(274), S(112));
            btnClear.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnClear.Click += (s, e) => ClearList();
            Controls.Add(btnClear);

            // 输出格式（竖排）
            grpFmt = new GroupBox();
            grpFmt.SetBounds(S(16), S(316), S(248), S(118));
            grpFmt.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Controls.Add(grpFmt);

            rbJpg = new RadioButton();
            rbJpg.SetBounds(S(14), S(26), S(220), S(24));
            rbJpg.Checked = true;
            grpFmt.Controls.Add(rbJpg);

            rbPng = new RadioButton();
            rbPng.SetBounds(S(14), S(54), S(220), S(24));
            grpFmt.Controls.Add(rbPng);

            rbBoth = new RadioButton();
            rbBoth.SetBounds(S(14), S(82), S(220), S(24));
            grpFmt.Controls.Add(rbBoth);

            // 输出位置（竖排）
            grpOut = new GroupBox();
            grpOut.SetBounds(S(272), S(316), S(250), S(118));
            grpOut.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            Controls.Add(grpOut);

            rbSameDir = new RadioButton();
            rbSameDir.SetBounds(S(14), S(26), S(226), S(24));
            rbSameDir.Checked = true;
            grpOut.Controls.Add(rbSameDir);

            rbCustomDir = new RadioButton();
            rbCustomDir.SetBounds(S(14), S(54), S(96), S(24));
            grpOut.Controls.Add(rbCustomDir);

            txtOutDir = new TextBox();
            txtOutDir.SetBounds(S(110), S(56), S(100), S(24));
            txtOutDir.Enabled = false;
            grpOut.Controls.Add(txtOutDir);

            btnBrowseDir = MakeButton("", S(214), S(55), S(34));
            btnBrowseDir.Enabled = false;
            btnBrowseDir.Click += (s, e) => BrowseOutDir();
            grpOut.Controls.Add(btnBrowseDir);

            rbCustomDir.CheckedChanged += (s, e) =>
            {
                txtOutDir.Enabled = rbCustomDir.Checked;
                btnBrowseDir.Enabled = rbCustomDir.Checked;
            };

            // 画质选项
            grpOpt = new GroupBox();
            grpOpt.SetBounds(S(530), S(316), S(234), S(118));
            grpOpt.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(grpOpt);

            lblQuality = new Label();
            lblQuality.SetBounds(S(14), S(26), S(200), S(20));
            lblQuality.AutoSize = true;
            grpOpt.Controls.Add(lblQuality);

            tbQuality = new TrackBar();
            tbQuality.SetBounds(S(14), S(50), S(110), S(40));
            tbQuality.Minimum = 40;
            tbQuality.Maximum = 100;
            tbQuality.Value = 95;
            tbQuality.TickFrequency = 10;
            tbQuality.Scroll += (s, e) => UpdateQualityLabel();
            grpOpt.Controls.Add(tbQuality);

            lblMode = new Label();
            lblMode.SetBounds(S(132), S(28), S(96), S(20));
            lblMode.AutoSize = true;
            grpOpt.Controls.Add(lblMode);

            cbMode = new ComboBox();
            cbMode.SetBounds(S(132), S(52), S(92), S(24));
            cbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            grpOpt.Controls.Add(cbMode);

            // 转换按钮 + 打开目录 + 取消
            btnConvert = new Button();
            btnConvert.Font = PxFont(15f, true);
            btnConvert.SetBounds(S(16), S(446), S(430), S(46));
            btnConvert.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnConvert.Click += (s, e) => DoConvert();
            Controls.Add(btnConvert);

            btnCancel = new Button();
            btnCancel.SetBounds(S(454), S(446), S(92), S(46));
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.Enabled = false;
            btnCancel.Click += (s, e) => { cancelRequested = true; btnCancel.Enabled = false; lblStatus.Text = (isZh ? "正在取消..." : "Cancelling..."); };
            Controls.Add(btnCancel);

            btnOpenDir = new Button();
            btnOpenDir.SetBounds(S(554), S(446), S(210), S(46));
            btnOpenDir.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOpenDir.Click += (s, e) => OpenOutputDir();
            Controls.Add(btnOpenDir);

            // 进度条
            pbar = new ProgressBar();
            pbar.SetBounds(S(16), S(502), S(748), S(18));
            pbar.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(pbar);

            // 状态栏
            lblStatus = new Label();
            lblStatus.SetBounds(S(16), S(528), S(748), S(30));
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.ForeColor = WinColor.DimGray;
            Controls.Add(lblStatus);

            DragEnter += OnDragEnter;
            DragDrop += OnDragDrop;
            FormClosing += (s, e) =>
            {
                if (isConverting)
                {
                    cancelRequested = true;
                    // 让后台线程尽快退出；窗口直接关闭，后台线程是 IsBackground 会自动终止
                }
            };
        }

        private Button MakeButton(string text, int x, int y, int w)
        {
            var b = new Button();
            b.Text = text;
            b.SetBounds(x, y, w, S(30));
            return b;
        }

        // ============ 预览（WPF 解码 JXR，GDI+ 不支持 JXR） ============
        private void PreviewSelected()
        {
            int idx = lstFiles.SelectedIndex;
            if (idx < 0 || idx >= fullPaths.Count) { picPreview.Image = null; return; }
            try
            {
                var uri = new Uri(fullPaths[idx]);
                var dec = BitmapDecoder.Create(uri, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                var frame = dec.Frames[0];

                // 转成 Bgra32 后写进内存流（PNG），交给 GDI+ 显示
                var conv = new FormatConvertedBitmap();
                conv.BeginInit();
                conv.Source = frame;
                conv.DestinationFormat = PixelFormats.Bgra32;
                conv.EndInit();

                int w = conv.PixelWidth, h = conv.PixelHeight;
                int stride = w * 4;
                byte[] px = new byte[stride * h];
                conv.CopyPixels(px, stride, 0);

                var bmpSrc = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, px, stride);
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bmpSrc));

                using (var ms = new MemoryStream())
                {
                    enc.Save(ms);
                    ms.Position = 0;
                    var old = picPreview.Image;
                    picPreview.Image = WinImage.FromStream(ms);
                    if (old != null) old.Dispose();
                }
            }
            catch
            {
                picPreview.Image = null;
            }
        }

        // ============ 设置持久化 ============
        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(cfgPath)) return;
                foreach (var line in File.ReadAllLines(cfgPath))
                {
                    var kv = line.Split(new[] { '=' }, 2);
                    if (kv.Length != 2) continue;
                    string k = kv[0].Trim(), v = kv[1].Trim();
                    if (k == "fmt" && v == "png") rbPng.Checked = true;
                    else if (k == "fmt" && v == "both") rbBoth.Checked = true;
                    else if (k == "quality") { int q; if (int.TryParse(v, out q)) tbQuality.Value = Math.Max(40, Math.Min(100, q)); }
                    else if (k == "mode") { int m; if (int.TryParse(v, out m)) cbMode.SelectedIndex = Math.Max(0, Math.Min(2, m)); }
                    else if (k == "outdir") { if (!string.IsNullOrEmpty(v)) { rbCustomDir.Checked = true; txtOutDir.Text = v; } }
                }
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cfgPath));
                var lines = new List<string>();
                lines.Add("fmt=" + (rbPng.Checked ? "png" : rbBoth.Checked ? "both" : "jpg"));
                lines.Add("quality=" + tbQuality.Value);
                lines.Add("mode=" + cbMode.SelectedIndex);
                if (rbCustomDir.Checked && !string.IsNullOrEmpty(txtOutDir.Text))
                    lines.Add("outdir=" + txtOutDir.Text);
                File.WriteAllLines(cfgPath, lines);
            }
            catch { }
        }

        // ============ 文件操作 ============
        private void AutoScanNvidiaFolder()
        {
            string nvidiaDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "NVIDIA");
            if (Directory.Exists(nvidiaDir))
            {
                var files = Directory.GetFiles(nvidiaDir, "*.jxr", SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    AddFiles(files);
                    lblStatus.Text = TF("scanned", files.Length);
                }
            }
        }

        private void AddFiles(IEnumerable<string> paths)
        {
            foreach (var p in paths)
            {
                if (!p.ToLower().EndsWith(".jxr")) continue;
                string full = Path.GetFullPath(p);
                if (fullPaths.Contains(full)) continue;
                fullPaths.Add(full);
                lstFiles.Items.Add(Path.GetFileName(full));
            }
        }

        private void AddFilesDialog()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = T("open_title");
                dlg.Filter = T("open_filter");
                dlg.Multiselect = true;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    AddFiles(dlg.FileNames);
            }
        }

        private void AddFolderDialog()
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = T("folder_title");
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var files = Directory.GetFiles(dlg.SelectedPath, "*.jxr", SearchOption.AllDirectories);
                    if (files.Length == 0)
                        MessageBox.Show(this, T("no_jxr_in_folder"), T("tip_title"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    else
                        AddFiles(files);
                }
            }
        }

        private void RemoveSelected()
        {
            if (lstFiles.SelectedIndices.Count == 0) return;
            var idxs = new List<int>();
            foreach (int i in lstFiles.SelectedIndices) idxs.Add(i);
            idxs.Sort();
            idxs.Reverse();
            foreach (int i in idxs)
            {
                lstFiles.Items.RemoveAt(i);
                fullPaths.RemoveAt(i);
            }
            PreviewSelected();
        }

        private void ClearList()
        {
            lstFiles.Items.Clear();
            fullPaths.Clear();
            picPreview.Image = null;
        }

        private void BrowseOutDir()
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = T("folder_out_title");
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    txtOutDir.Text = dlg.SelectedPath;
            }
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null) return;
            AddFiles(files);
        }

        // ============ 转换（后台线程 + 不冻结 UI + 可取消） ============
        private void DoConvert()
        {
            if (fullPaths.Count == 0)
            {
                MessageBox.Show(this, T("no_files"), T("tip_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string customDir = rbCustomDir.Checked ? txtOutDir.Text.Trim() : null;
            if (rbCustomDir.Checked && string.IsNullOrEmpty(customDir))
            {
                MessageBox.Show(this, T("no_outdir"), T("tip_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SaveSettings();
            isConverting = true;
            cancelRequested = false;
            btnConvert.Enabled = false;
            btnCancel.Enabled = true;
            pbar.Value = 0;

            int total = fullPaths.Count;
            int mode = cbMode.SelectedIndex;
            bool doJpg = rbJpg.Checked;
            bool doPng = rbPng.Checked;
            bool doBoth = rbBoth.Checked;
            int quality = tbQuality.Value;
            var paths = new List<string>(fullPaths);

            var worker = new Thread(() =>
            {
                int ok = 0, fail = 0, done = 0;
                var errors = new List<string>();
                int maxThreads = Math.Max(2, Math.Min(8, Environment.ProcessorCount));

                using (var sem = new SemaphoreSlim(maxThreads))
                {
                    var jobs = new List<Thread>();
                    for (int i = 0; i < total; i++)
                    {
                        if (cancelRequested) break;
                        int idx = i;
                        var t = new Thread(() =>
                        {
                            try
                            {
                                sem.Wait();
                                if (!cancelRequested)
                                {
                                    try
                                    {
                                        ConvertOne(paths[idx], customDir, doJpg, doPng, doBoth, mode, quality);
                                        Interlocked.Increment(ref ok);
                                    }
                                    catch (Exception ex)
                                    {
                                        Interlocked.Increment(ref fail);
                                        lock (errors) { errors.Add(Path.GetFileName(paths[idx]) + ": " + ex.Message); }
                                    }
                                }
                            }
                            finally
                            {
                                sem.Release();
                                int now = Interlocked.Increment(ref done);
                                TryUpdateProgress(now, total, paths[idx]);
                            }
                        });
                        t.IsBackground = true;
                        jobs.Add(t);
                        t.Start();
                    }
                    foreach (var t in jobs) t.Join();
                }

                int remaining = total - done;
                TryFinish(ok, fail, remaining, errors);
            });
            worker.IsBackground = true;
            worker.Start();
        }

        private void TryUpdateProgress(int done, int total, string file)
        {
            try
            {
                if (IsHandleCreated && !IsDisposed)
                    BeginInvoke((Action)(() =>
                    {
                        pbar.Value = (int)(done * 100.0 / total);
                        lblStatus.Text = TF("converting", done, total, Path.GetFileName(file));
                    }));
            }
            catch { }
        }

        private void TryFinish(int ok, int fail, int remaining, List<string> errors)
        {
            try
            {
                if (!IsHandleCreated || IsDisposed) return;
                BeginInvoke((Action)(() =>
                {
                    isConverting = false;
                    btnConvert.Enabled = true;
                    btnCancel.Enabled = false;
                    pbar.Value = 100;

                    if (remaining > 0)
                    {
                        lblStatus.Text = TF("cancelled", ok, remaining);
                        return;
                    }

                    string msg = TF("done_msg", ok, fail);
                    if (errors.Count > 0)
                        msg += TF("done_err", string.Join("\n", errors));
                    MessageBox.Show(this, msg, T("done_title"), MessageBoxButtons.OK,
                        fail > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                    lblStatus.Text = TF("done_status", ok, fail);
                }));
            }
            catch { }
        }

        private void ConvertOne(string src, string customDir, bool toJpg, bool toPng, bool toBoth, int mode, int quality)
        {
            var uri = new Uri(src);
            var dec = BitmapDecoder.Create(uri, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var frame = dec.Frames[0];

            var conv = new FormatConvertedBitmap();
            conv.BeginInit();
            conv.Source = frame;
            conv.DestinationFormat = PixelFormats.Rgba128Float;
            conv.EndInit();

            int w = conv.PixelWidth, h = conv.PixelHeight;
            int stride = w * 16;
            byte[] buf = new byte[stride * h];
            conv.CopyPixels(buf, stride, 0);

            float[] rgba = new float[w * h * 4];
            System.Buffer.BlockCopy(buf, 0, rgba, 0, w * h * 16);

            byte[] outBuf = Process(rgba, w, h, mode);

            var bmp = BitmapSource.Create(w, h, 96, 96, PixelFormats.Rgb24, null, outBuf, w * 3);

            string outDir = (customDir != null)
                ? customDir
                : Path.Combine(Path.GetDirectoryName(src), "done");
            Directory.CreateDirectory(outDir);

            string baseName = Path.GetFileNameWithoutExtension(src);
            bool doJpgOut = toBoth || toJpg;
            bool doPngOut = toBoth || toPng;
            if (!doJpgOut && !doPngOut) doJpgOut = true;

            if (doJpgOut) SaveImage(bmp, Path.Combine(outDir, baseName + ".jpg"), false, quality);
            if (doPngOut) SaveImage(bmp, Path.Combine(outDir, baseName + ".png"), true, quality);
        }

        private void SaveImage(BitmapSource bmp, string path, bool toPng, int quality)
        {
            BitmapEncoder enc;
            if (toPng)
                enc = new PngBitmapEncoder();
            else
            {
                var j = new JpegBitmapEncoder();
                j.QualityLevel = quality;
                enc = j;
            }
            enc.Frames.Add(BitmapFrame.Create(bmp));
            using (var fs = File.Create(path)) enc.Save(fs);
        }

        // 色调映射：0=自然 1=鲜艳 2=电影感
        private byte[] Process(float[] rgba, int w, int h, int mode)
        {
            int n = w * h;
            byte[] outBuf = new byte[n * 3];
            int o = 0;
            for (int i = 0; i < n; i++)
            {
                int b = i * 4;
                for (int c = 0; c < 3; c++)
                {
                    double v = rgba[b + c];
                    if (v < 0) v = 0;
                    if (mode == 0) v = v / (1.0 + v * 0.35);       // 自然
                    else if (mode == 1) v = v / (1.0 + v * 0.15);  // 鲜艳（高光少压）
                    else v = v / (1.0 + v * 0.55);                 // 电影感（高光多压）
                    if (v > 1.0) v = 1.0;
                    if (v <= 0.0031308) v = 12.92 * v;
                    else v = 1.055 * Math.Pow(v, 1.0 / 2.4) - 0.055;
                    outBuf[o++] = (byte)Math.Round(v * 255.0);
                }
            }
            return outBuf;
        }

        private void OpenOutputDir()
        {
            string dir;
            if (rbCustomDir.Checked && !string.IsNullOrEmpty(txtOutDir.Text))
                dir = txtOutDir.Text;
            else if (fullPaths.Count > 0)
                dir = Path.Combine(Path.GetDirectoryName(fullPaths[0]), "done");
            else
            {
                MessageBox.Show(this, T("no_files"), T("tip_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                Directory.CreateDirectory(dir);
                System.Diagnostics.Process.Start("explorer.exe", dir);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, T("tip_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    static class Program
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main(string[] args)
        {
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 右键菜单模式：-tojpg / -topng <file>，静默转换单个文件
            if (args != null && args.Length >= 2)
            {
                string cmd = args[0].ToLower();
                if ((cmd == "-tojpg" || cmd == "-topng" || cmd == "-toboth") && File.Exists(args[1]))
                {
                    SilentConvert(args[1], cmd);
                    return;
                }
            }

            // 收集命令行传入的 .jxr 文件（拖到 exe 图标时）
            var initial = new List<string>();
            if (args != null)
            {
                foreach (var a in args)
                {
                    if (!string.IsNullOrEmpty(a) && a.ToLower().EndsWith(".jxr") && File.Exists(a))
                        initial.Add(a);
                }
            }

            Application.Run(initial.Count > 0 ? new MainForm(initial) : new MainForm());
        }

        // 静默转换：不弹 GUI，转换单个文件（供右键菜单调用）
        private static void SilentConvert(string src, string cmd)
        {
            try
            {
                var uri = new Uri(src);
                var dec = BitmapDecoder.Create(uri, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                var frame = dec.Frames[0];

                var conv = new FormatConvertedBitmap();
                conv.BeginInit();
                conv.Source = frame;
                conv.DestinationFormat = PixelFormats.Rgba128Float;
                conv.EndInit();

                int w = conv.PixelWidth, h = conv.PixelHeight;
                int stride = w * 16;
                byte[] buf = new byte[stride * h];
                conv.CopyPixels(buf, stride, 0);

                float[] rgba = new float[w * h * 4];
                System.Buffer.BlockCopy(buf, 0, rgba, 0, w * h * 16);

                byte[] outBuf = ProcessStatic(rgba, w, h);

                var bmp = BitmapSource.Create(w, h, 96, 96, PixelFormats.Rgb24, null, outBuf, w * 3);
                string dir = Path.Combine(Path.GetDirectoryName(src), "done");
                Directory.CreateDirectory(dir);
                string baseName = Path.GetFileNameWithoutExtension(src);

                bool doJpg = cmd == "-tojpg" || cmd == "-toboth";
                bool doPng = cmd == "-topng" || cmd == "-toboth";
                if (!doJpg && !doPng) doJpg = true;

                if (doJpg)
                {
                    var j = new JpegBitmapEncoder();
                    j.QualityLevel = 95;
                    j.Frames.Add(BitmapFrame.Create(bmp));
                    using (var fs = File.Create(Path.Combine(dir, baseName + ".jpg"))) j.Save(fs);
                }
                if (doPng)
                {
                    var p = new PngBitmapEncoder();
                    p.Frames.Add(BitmapFrame.Create(bmp));
                    using (var fs = File.Create(Path.Combine(dir, baseName + ".png"))) p.Save(fs);
                }
            }
            catch
            {
                // 静默失败，忽略（或可写日志，这里保持简单）
            }
        }

        private static byte[] ProcessStatic(float[] rgba, int w, int h)
        {
            int n = w * h;
            byte[] outBuf = new byte[n * 3];
            int o = 0;
            for (int i = 0; i < n; i++)
            {
                int b = i * 4;
                for (int c = 0; c < 3; c++)
                {
                    double v = rgba[b + c];
                    if (v < 0) v = 0;
                    v = v / (1.0 + v * 0.35);
                    if (v > 1.0) v = 1.0;
                    if (v <= 0.0031308) v = 12.92 * v;
                    else v = 1.055 * Math.Pow(v, 1.0 / 2.4) - 0.055;
                    outBuf[o++] = (byte)Math.Round(v * 255.0);
                }
            }
            return outBuf;
        }
    }
}
