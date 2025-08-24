using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RecoStaScan
{
    public partial class DetailForm : Form
    {
        private CcprojData data;
        private List<TextData> allTexts = new List<TextData>();
        private List<TextData> filteredTexts = new List<TextData>();

        public DetailForm(CcprojData projectData, string searchText = "")
        {
            this.data = projectData;
            InitializeComponent();
            LoadData();
            
            // 検索文字が指定されている場合は設定して検索実行
            if (!string.IsNullOrEmpty(searchText))
            {
                this.txtSearch.Text = searchText;
                PerformSearch();
            }
            
            this.Load += DetailForm_Load;
            this.Resize += DetailForm_Resize;
        }

        private void DetailForm_Load(object sender, EventArgs e)
        {
            UpdateLayout();
            this.Activate();
            this.BringToFront();
            this.TopMost = false;
        }

        private void DetailForm_Resize(object sender, EventArgs e)
        {
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            // ラベルの幅調整
            this.lblFileName.Width = this.ClientSize.Width - 24;
            this.lblProjectName.Width = this.ClientSize.Width - 24;
            
            // スピーカーリストの幅調整
            this.lstSpeakers.Width = this.ClientSize.Width - 24;
            
            // 検索コントロールの幅調整
            var searchAreaWidth = this.ClientSize.Width - 24;
            this.txtSearch.Width = Math.Max(150, searchAreaWidth - 320); // 他のコントロール分を差し引く
            this.btnSearch.Left = this.txtSearch.Right + 6;
            this.btnClearSearch.Left = this.btnSearch.Right + 6;
            this.lblSearchResults.Left = this.btnClearSearch.Right + 10;
            this.lblSearchResults.Width = this.ClientSize.Width - this.lblSearchResults.Left - 12;
            
            // DataGridViewのサイズ調整
            this.dgvTexts.Width = this.ClientSize.Width - 24;
            this.dgvTexts.Height = this.ClientSize.Height - this.dgvTexts.Top - this.btnClose.Height - 24;
            
            // 閉じるボタンの位置調整
            this.btnClose.Top = this.ClientSize.Height - this.btnClose.Height - 12;
            this.btnClose.Left = this.ClientSize.Width - this.btnClose.Width - 12;
        }

        private void InitializeComponent()
        {
            this.lblFileName = new Label();
            this.lblProjectName = new Label();
            this.lblSpeakers = new Label();
            this.lstSpeakers = new ListBox();
            this.lblTexts = new Label();
            this.dgvTexts = new DataGridView();
            this.btnClose = new Button();
            this.lblSearch = new Label();
            this.txtSearch = new TextBox();
            this.btnSearch = new Button();
            this.btnClearSearch = new Button();
            this.lblSearchResults = new Label();

            this.SuspendLayout();

            // ファイル名ラベル
            this.lblFileName.Location = new Point(12, 12);
            this.lblFileName.Size = new Size(560, 23);
            this.lblFileName.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold);
            this.lblFileName.Text = "ファイル名: ";

            // プロジェクト名ラベル
            this.lblProjectName.Location = new Point(12, 35);
            this.lblProjectName.Size = new Size(560, 23);
            this.lblProjectName.Text = "プロジェクト名: ";

            // スピーカーラベル
            this.lblSpeakers.Location = new Point(12, 65);
            this.lblSpeakers.Size = new Size(100, 23);
            this.lblSpeakers.Text = "キャラクター:";

            // スピーカーリスト
            this.lstSpeakers.Location = new Point(12, 88);
            this.lstSpeakers.Size = new Size(560, 100);

            // 検索ラベル
            this.lblSearch.Location = new Point(12, 195);
            this.lblSearch.Size = new Size(60, 23);
            this.lblSearch.Text = "検索:";
            this.lblSearch.TextAlign = ContentAlignment.MiddleLeft;

            // 検索テキストボックス
            this.txtSearch.Location = new Point(78, 195);
            this.txtSearch.Size = new Size(200, 23);
            this.txtSearch.KeyPress += new KeyPressEventHandler(this.txtSearch_KeyPress);

            // 検索ボタン
            this.btnSearch.Location = new Point(284, 195);
            this.btnSearch.Size = new Size(60, 23);
            this.btnSearch.Text = "検索";
            this.btnSearch.UseVisualStyleBackColor = true;
            this.btnSearch.Click += new EventHandler(this.btnSearch_Click);

            // 検索クリアボタン
            this.btnClearSearch.Location = new Point(350, 195);
            this.btnClearSearch.Size = new Size(60, 23);
            this.btnClearSearch.Text = "クリア";
            this.btnClearSearch.UseVisualStyleBackColor = true;
            this.btnClearSearch.Click += new EventHandler(this.btnClearSearch_Click);

            // 検索結果ラベル
            this.lblSearchResults.Location = new Point(420, 195);
            this.lblSearchResults.Size = new Size(150, 23);
            this.lblSearchResults.Text = "";
            this.lblSearchResults.TextAlign = ContentAlignment.MiddleLeft;
            this.lblSearchResults.ForeColor = Color.DarkBlue;

            // テキストラベル
            this.lblTexts.Location = new Point(12, 225);
            this.lblTexts.Size = new Size(100, 23);
            this.lblTexts.Text = "テキスト:";

            // テキストDataGridView
            this.dgvTexts.Location = new Point(12, 248);
            this.dgvTexts.Size = new Size(560, 220);
            this.dgvTexts.AllowUserToAddRows = false;
            this.dgvTexts.AllowUserToDeleteRows = false;
            this.dgvTexts.ReadOnly = true;
            this.dgvTexts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // 閉じるボタン
            this.btnClose.Location = new Point(497, 480);
            this.btnClose.Size = new Size(75, 23);
            this.btnClose.Text = "閉じる";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new EventHandler(this.btnClose_Click);

            // DetailForm
            this.ClientSize = new Size(584, 515);
            this.MinimumSize = new Size(400, 350);
            this.Controls.Add(this.lblFileName);
            this.Controls.Add(this.lblProjectName);
            this.Controls.Add(this.lblSpeakers);
            this.Controls.Add(this.lstSpeakers);
            this.Controls.Add(this.lblSearch);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.btnSearch);
            this.Controls.Add(this.btnClearSearch);
            this.Controls.Add(this.lblSearchResults);
            this.Controls.Add(this.lblTexts);
            this.Controls.Add(this.dgvTexts);
            this.Controls.Add(this.btnClose);
            this.Text = "プロジェクト詳細";
            this.StartPosition = FormStartPosition.CenterParent;
            this.TopMost = true;
            this.ResumeLayout(false);

            InitializeTextDataGridView();
        }

        private Label lblFileName;
        private Label lblProjectName;
        private Label lblSpeakers;
        private ListBox lstSpeakers;
        private Label lblTexts;
        private DataGridView dgvTexts;
        private Button btnClose;
        private Label lblSearch;
        private TextBox txtSearch;
        private Button btnSearch;
        private Button btnClearSearch;
        private Label lblSearchResults;

        private void InitializeTextDataGridView()
        {
            dgvTexts.Columns.Add("Speaker", "話者");
            dgvTexts.Columns.Add("StartTime", "開始時刻");
            dgvTexts.Columns.Add("EndTime", "終了時刻");
            dgvTexts.Columns.Add("Text", "テキスト");

            dgvTexts.Columns["Speaker"].Width = 100;
            dgvTexts.Columns["StartTime"].Width = 80;
            dgvTexts.Columns["EndTime"].Width = 80;
            dgvTexts.Columns["Text"].Width = 300;
        }

        private void LoadData()
        {
            lblFileName.Text = $"ファイル名: {data.FileName}";
            lblProjectName.Text = $"プロジェクト名: {data.ProjectName}";

            // スピーカー情報をロード
            lstSpeakers.Items.Clear();
            foreach (var speaker in data.Speakers)
            {
                lstSpeakers.Items.Add($"{speaker.Name} ({speaker.FilePath})");
            }

            // テキストデータを初期化
            allTexts = data.Texts.OrderBy(t => t.StartTime).ToList();
            filteredTexts = allTexts;
            
            UpdateTextDisplay();
            UpdateSearchResults();
        }

        private void UpdateTextDisplay()
        {
            dgvTexts.Rows.Clear();
            foreach (var text in filteredTexts)
            {
                var rowIndex = dgvTexts.Rows.Add(
                    text.SpeakerName,
                    $"{text.StartTime:F2}s",
                    $"{text.EndTime:F2}s",
                    text.Text
                );

                // 検索結果のハイライト
                if (!string.IsNullOrEmpty(txtSearch?.Text) && 
                    text.Text.Contains(txtSearch.Text, StringComparison.OrdinalIgnoreCase))
                {
                    dgvTexts.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightYellow;
                    dgvTexts.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.DarkBlue;
                }
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            PerformSearch();
        }

        private void btnClearSearch_Click(object sender, EventArgs e)
        {
            txtSearch.Text = "";
            filteredTexts = allTexts;
            UpdateTextDisplay();
            UpdateSearchResults();
        }

        private void txtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                PerformSearch();
                e.Handled = true;
            }
        }

        private void PerformSearch()
        {
            var searchText = txtSearch.Text.Trim();
            
            if (string.IsNullOrEmpty(searchText))
            {
                filteredTexts = allTexts;
            }
            else
            {
                filteredTexts = allTexts.Where(t => 
                    t.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    t.SpeakerName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }
            
            UpdateTextDisplay();
            UpdateSearchResults();
        }

        private void UpdateSearchResults()
        {
            if (string.IsNullOrEmpty(txtSearch.Text.Trim()))
            {
                lblSearchResults.Text = $"全 {allTexts.Count} 件";
            }
            else
            {
                lblSearchResults.Text = $"{filteredTexts.Count} / {allTexts.Count} 件";
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}