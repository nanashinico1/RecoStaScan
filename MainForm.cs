using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RecoStaScan
{
    public partial class MainForm : Form
    {
        private List<CcprojData> allData = new List<CcprojData>();
        private List<CcprojData> filteredData = new List<CcprojData>();
        private List<DuplicateGroup> duplicateGroups = new List<DuplicateGroup>();
        private int skippedDuplicates = 0;

        public MainForm()
        {
            InitializeComponent();
            InitializeDataGridView();
            this.Load += MainForm_Load;
            this.Resize += MainForm_Resize;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            UpdateLayout();
            UpdateCacheInfo();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            // フォルダパステキストボックスのサイズ調整
            this.txtFolderPath.Width = this.ClientSize.Width - this.txtFolderPath.Left - this.btnScan.Width - 18;
            
            // スキャンボタンの位置調整
            this.btnScan.Left = this.ClientSize.Width - this.btnScan.Width - 12;
            
            // DataGridViewのサイズ調整
            this.dgvResults.Width = this.ClientSize.Width - 24;
            this.dgvResults.Height = this.ClientSize.Height - this.dgvResults.Top - this.progressBar.Height - 24;
            
            // プログレスバーの位置とサイズ調整
            this.progressBar.Top = this.ClientSize.Height - this.progressBar.Height - 12;
            this.progressBar.Width = this.ClientSize.Width - this.lblStatus.Width - 24;
            
            // ステータスラベルの位置調整
            this.lblStatus.Top = this.ClientSize.Height - this.lblStatus.Height - 12;
            this.lblStatus.Left = this.ClientSize.Width - this.lblStatus.Width - 12;
        }

        private void UpdateCacheInfo()
        {
            try
            {
                var cache = CacheManager.LoadCache();
                var stats = CacheManager.GetCacheStats(cache);
                
                if (stats.TotalCachedFiles > 0)
                {
                    this.lblCacheInfo.Text = $"キャッシュ: {stats.TotalCachedFiles}件のファイル ({stats.CacheSizeKB}KB) - 最終スキャン: {stats.LastScanTime:yyyy/MM/dd HH:mm}";
                }
                else
                {
                    this.lblCacheInfo.Text = "キャッシュ: データなし";
                }
            }
            catch
            {
                this.lblCacheInfo.Text = "キャッシュ: 情報取得エラー";
            }
        }

        private void InitializeComponent()
        {
            this.btnSelectFolder = new Button();
            this.txtFolderPath = new TextBox();
            this.btnScan = new Button();
            this.txtCharacterSearch = new TextBox();
            this.txtTextSearch = new TextBox();
            this.btnSearchCharacter = new Button();
            this.btnSearchText = new Button();
            this.btnClearFilter = new Button();
            this.dgvResults = new DataGridView();
            this.progressBar = new ProgressBar();
            this.lblStatus = new Label();
            this.lblCharacterSearch = new Label();
            this.lblTextSearch = new Label();
            this.chkUseCache = new CheckBox();
            this.btnClearCache = new Button();
            this.lblCacheInfo = new Label();
            this.chkFilterDuplicates = new CheckBox();
            this.btnViewDuplicates = new Button();
            this.lblDuplicateInfo = new Label();

            this.SuspendLayout();

            // フォルダ選択
            this.btnSelectFolder.Location = new Point(12, 12);
            this.btnSelectFolder.Size = new Size(100, 23);
            this.btnSelectFolder.Text = "フォルダ選択";
            this.btnSelectFolder.UseVisualStyleBackColor = true;
            this.btnSelectFolder.Click += new EventHandler(this.btnSelectFolder_Click);

            this.txtFolderPath.Location = new Point(118, 12);
            this.txtFolderPath.Size = new Size(400, 23);
            this.txtFolderPath.ReadOnly = true;

            this.btnScan.Location = new Point(524, 12);
            this.btnScan.Size = new Size(75, 23);
            this.btnScan.Text = "スキャン";
            this.btnScan.UseVisualStyleBackColor = true;
            this.btnScan.Click += new EventHandler(this.btnScan_Click);

            // キャラクター検索
            this.lblCharacterSearch.Location = new Point(12, 50);
            this.lblCharacterSearch.Size = new Size(100, 23);
            this.lblCharacterSearch.Text = "キャラクター:";
            this.lblCharacterSearch.TextAlign = ContentAlignment.MiddleLeft;

            this.txtCharacterSearch.Location = new Point(118, 50);
            this.txtCharacterSearch.Size = new Size(200, 23);

            this.btnSearchCharacter.Location = new Point(324, 50);
            this.btnSearchCharacter.Size = new Size(75, 23);
            this.btnSearchCharacter.Text = "検索";
            this.btnSearchCharacter.UseVisualStyleBackColor = true;
            this.btnSearchCharacter.Click += new EventHandler(this.btnSearchCharacter_Click);

            // テキスト検索
            this.lblTextSearch.Location = new Point(12, 85);
            this.lblTextSearch.Size = new Size(100, 23);
            this.lblTextSearch.Text = "セリフ:";
            this.lblTextSearch.TextAlign = ContentAlignment.MiddleLeft;

            this.txtTextSearch.Location = new Point(118, 85);
            this.txtTextSearch.Size = new Size(200, 23);

            this.btnSearchText.Location = new Point(324, 85);
            this.btnSearchText.Size = new Size(75, 23);
            this.btnSearchText.Text = "検索";
            this.btnSearchText.UseVisualStyleBackColor = true;
            this.btnSearchText.Click += new EventHandler(this.btnSearchText_Click);

            // フィルタークリア
            this.btnClearFilter.Location = new Point(405, 67);
            this.btnClearFilter.Size = new Size(100, 23);
            this.btnClearFilter.Text = "フィルタークリア";
            this.btnClearFilter.UseVisualStyleBackColor = true;
            this.btnClearFilter.Click += new EventHandler(this.btnClearFilter_Click);

            // キャッシュ使用チェックボックス
            this.chkUseCache.Location = new Point(520, 50);
            this.chkUseCache.Size = new Size(120, 23);
            this.chkUseCache.Text = "キャッシュ使用";
            this.chkUseCache.Checked = true;
            this.chkUseCache.UseVisualStyleBackColor = true;

            // キャッシュクリアボタン
            this.btnClearCache.Location = new Point(520, 85);
            this.btnClearCache.Size = new Size(100, 23);
            this.btnClearCache.Text = "キャッシュクリア";
            this.btnClearCache.UseVisualStyleBackColor = true;
            this.btnClearCache.Click += new EventHandler(this.btnClearCache_Click);

            // 重複フィルターチェックボックス
            this.chkFilterDuplicates.Location = new Point(650, 50);
            this.chkFilterDuplicates.Size = new Size(120, 23);
            this.chkFilterDuplicates.Text = "重複ファイル除外";
            this.chkFilterDuplicates.Checked = true;
            this.chkFilterDuplicates.UseVisualStyleBackColor = true;

            // 重複表示ボタン
            this.btnViewDuplicates.Location = new Point(650, 85);
            this.btnViewDuplicates.Size = new Size(100, 23);
            this.btnViewDuplicates.Text = "重複ファイル表示";
            this.btnViewDuplicates.UseVisualStyleBackColor = true;
            this.btnViewDuplicates.Enabled = false;
            this.btnViewDuplicates.Click += new EventHandler(this.btnViewDuplicates_Click);

            // キャッシュ情報ラベル
            this.lblCacheInfo.Location = new Point(12, 120);
            this.lblCacheInfo.Size = new Size(400, 23);
            this.lblCacheInfo.Text = "キャッシュ情報を読み込み中...";
            this.lblCacheInfo.TextAlign = ContentAlignment.MiddleLeft;
            this.lblCacheInfo.ForeColor = Color.DarkGreen;

            // 重複情報ラベル
            this.lblDuplicateInfo.Location = new Point(420, 120);
            this.lblDuplicateInfo.Size = new Size(350, 23);
            this.lblDuplicateInfo.Text = "";
            this.lblDuplicateInfo.TextAlign = ContentAlignment.MiddleLeft;
            this.lblDuplicateInfo.ForeColor = Color.DarkBlue;

            // DataGridView
            this.dgvResults.Location = new Point(12, 150);
            this.dgvResults.Size = new Size(760, 400);
            this.dgvResults.AllowUserToAddRows = false;
            this.dgvResults.AllowUserToDeleteRows = false;
            this.dgvResults.ReadOnly = true;
            this.dgvResults.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvResults.MultiSelect = false;
            this.dgvResults.DoubleClick += new EventHandler(this.dgvResults_DoubleClick);

            // プログレスバー
            this.progressBar.Location = new Point(12, 530);
            this.progressBar.Size = new Size(600, 23);

            // ステータスラベル
            this.lblStatus.Location = new Point(618, 530);
            this.lblStatus.Size = new Size(154, 23);
            this.lblStatus.Text = "準備完了";
            this.lblStatus.TextAlign = ContentAlignment.MiddleLeft;

            // MainForm
            this.ClientSize = new Size(784, 561);
            this.MinimumSize = new Size(600, 400);
            this.Controls.Add(this.btnSelectFolder);
            this.Controls.Add(this.txtFolderPath);
            this.Controls.Add(this.btnScan);
            this.Controls.Add(this.lblCharacterSearch);
            this.Controls.Add(this.txtCharacterSearch);
            this.Controls.Add(this.btnSearchCharacter);
            this.Controls.Add(this.lblTextSearch);
            this.Controls.Add(this.txtTextSearch);
            this.Controls.Add(this.btnSearchText);
            this.Controls.Add(this.btnClearFilter);
            this.Controls.Add(this.chkUseCache);
            this.Controls.Add(this.btnClearCache);
            this.Controls.Add(this.chkFilterDuplicates);
            this.Controls.Add(this.btnViewDuplicates);
            this.Controls.Add(this.lblCacheInfo);
            this.Controls.Add(this.lblDuplicateInfo);
            this.Controls.Add(this.dgvResults);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.lblStatus);
            this.Text = "RecoStaScan - RecotteStudio プロジェクトスキャナー";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private Button btnSelectFolder;
        private TextBox txtFolderPath;
        private Button btnScan;
        private TextBox txtCharacterSearch;
        private TextBox txtTextSearch;
        private Button btnSearchCharacter;
        private Button btnSearchText;
        private Button btnClearFilter;
        private DataGridView dgvResults;
        private ProgressBar progressBar;
        private Label lblStatus;
        private Label lblCharacterSearch;
        private Label lblTextSearch;
        private CheckBox chkUseCache;
        private Button btnClearCache;
        private Label lblCacheInfo;
        private CheckBox chkFilterDuplicates;
        private Button btnViewDuplicates;
        private Label lblDuplicateInfo;

        private void InitializeDataGridView()
        {
            dgvResults.Columns.Add("FileName", "ファイル名");
            dgvResults.Columns.Add("ProjectName", "プロジェクト名");
            dgvResults.Columns.Add("Characters", "キャラクター");
            dgvResults.Columns.Add("TextCount", "テキスト数");
            dgvResults.Columns.Add("SampleText", "サンプルテキスト");

            dgvResults.Columns["FileName"].Width = 150;
            dgvResults.Columns["ProjectName"].Width = 150;
            dgvResults.Columns["Characters"].Width = 200;
            dgvResults.Columns["TextCount"].Width = 80;
            dgvResults.Columns["SampleText"].Width = 300;
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "ccprojファイルが格納されているフォルダを選択してください";
                dialog.SelectedPath = @"K:\jsapp\RecoStaScan\ccproj";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtFolderPath.Text = dialog.SelectedPath;
                }
            }
        }

        private async void btnScan_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtFolderPath.Text) || !Directory.Exists(txtFolderPath.Text))
            {
                MessageBox.Show("有効なフォルダを選択してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnScan.Enabled = false;
            progressBar.Style = ProgressBarStyle.Marquee;
            lblStatus.Text = "スキャン中...";

            try
            {
                var progress = new Progress<string>(message => lblStatus.Text = message);
                var useCache = chkUseCache.Checked;
                var filterDuplicates = chkFilterDuplicates.Checked;
                
                var scanResult = await Task.Run(() => CcprojParser.ScanDirectoryWithStats(txtFolderPath.Text, progress, useCache, filterDuplicates));
                allData = scanResult.Data;
                filteredData = allData;
                duplicateGroups = scanResult.DuplicateGroups;
                skippedDuplicates = scanResult.SkippedDuplicates;
                
                UpdateDataGridView();
                UpdateCacheInfo();
                UpdateDuplicateInfo();
                
                var durationText = scanResult.ScanDuration.TotalSeconds < 1 
                    ? $"{scanResult.ScanDuration.TotalMilliseconds:F0}ms"
                    : $"{scanResult.ScanDuration.TotalSeconds:F1}秒";
                    
                var statusText = $"完了: {allData.Count}件のファイルを読み込みました (処理時間: {durationText})";
                if (skippedDuplicates > 0)
                {
                    statusText += $" - {skippedDuplicates}件の重複ファイルをスキップ";
                }
                lblStatus.Text = statusText;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"スキャンエラー: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "エラーが発生しました";
            }
            finally
            {
                progressBar.Style = ProgressBarStyle.Blocks;
                btnScan.Enabled = true;
            }
        }

        private void btnSearchCharacter_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCharacterSearch.Text))
            {
                MessageBox.Show("検索するキャラクター名を入力してください。", "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            filteredData = CcprojParser.SearchByCharacter(allData, txtCharacterSearch.Text);
            UpdateDataGridView();
            lblStatus.Text = $"キャラクター検索: {filteredData.Count}件見つかりました";
        }

        private void btnSearchText_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTextSearch.Text))
            {
                MessageBox.Show("検索するテキストを入力してください。", "入力エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            filteredData = CcprojParser.SearchByText(allData, txtTextSearch.Text);
            UpdateDataGridView();
            lblStatus.Text = $"テキスト検索: {filteredData.Count}件見つかりました";
        }

        private void btnClearFilter_Click(object sender, EventArgs e)
        {
            filteredData = allData;
            txtCharacterSearch.Text = "";
            txtTextSearch.Text = "";
            UpdateDataGridView();
            lblStatus.Text = $"フィルタークリア: {allData.Count}件表示中";
        }

        private void UpdateDataGridView()
        {
            dgvResults.Rows.Clear();

            foreach (var data in filteredData)
            {
                var characters = string.Join(", ", data.Speakers.Select(s => s.Name));
                var sampleText = data.Texts.FirstOrDefault()?.Text ?? "";
                if (sampleText.Length > 50)
                {
                    sampleText = sampleText.Substring(0, 50) + "...";
                }

                dgvResults.Rows.Add(
                    data.FileName,
                    data.ProjectName,
                    characters,
                    data.Texts.Count,
                    sampleText
                );
            }
        }

        private void dgvResults_DoubleClick(object sender, EventArgs e)
        {
            if (dgvResults.CurrentRow != null)
            {
                var fileName = dgvResults.CurrentRow.Cells["FileName"].Value?.ToString();
                if (!string.IsNullOrEmpty(fileName))
                {
                    var data = filteredData.FirstOrDefault(d => d.FileName == fileName);
                    if (data != null)
                    {
                        ShowDetailForm(data);
                    }
                }
            }
        }

        private void ShowDetailForm(CcprojData data)
        {
            // テキスト検索が実行されている場合は検索文字を詳細画面に渡す
            string searchText = "";
            if (!string.IsNullOrWhiteSpace(txtTextSearch.Text))
            {
                searchText = txtTextSearch.Text;
            }
            
            var detailForm = new DetailForm(data, searchText);
            detailForm.ShowDialog(this);
        }

        private void btnClearCache_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "キャッシュをクリアしますか？\n次回スキャン時に全ファイルが再解析されます。",
                "キャッシュクリア確認",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                try
                {
                    CacheManager.ClearCache();
                    UpdateCacheInfo();
                    lblStatus.Text = "キャッシュをクリアしました";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"キャッシュクリアエラー: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void UpdateDuplicateInfo()
        {
            if (duplicateGroups.Count > 0)
            {
                this.lblDuplicateInfo.Text = $"重複: {duplicateGroups.Count}グループ ({skippedDuplicates}ファイル)";
                this.btnViewDuplicates.Enabled = true;
            }
            else
            {
                this.lblDuplicateInfo.Text = "重複: なし";
                this.btnViewDuplicates.Enabled = false;
            }
        }

        private void btnViewDuplicates_Click(object sender, EventArgs e)
        {
            if (duplicateGroups.Count > 0)
            {
                var duplicateForm = new DuplicateFilesForm(duplicateGroups);
                duplicateForm.ShowDialog();
            }
            else
            {
                MessageBox.Show("重複ファイルが見つかりませんでした。", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}