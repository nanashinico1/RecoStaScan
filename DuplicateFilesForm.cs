using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RecoStaScan
{
    public partial class DuplicateFilesForm : Form
    {
        private List<DuplicateGroup> duplicateGroups;

        public DuplicateFilesForm(List<DuplicateGroup> duplicateGroups)
        {
            this.duplicateGroups = duplicateGroups;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.treeViewDuplicates = new TreeView();
            this.lblInfo = new Label();
            this.btnClose = new Button();
            this.btnOpenFile = new Button();
            this.btnOpenFolder = new Button();

            this.SuspendLayout();

            // 情報ラベル
            this.lblInfo.Location = new Point(12, 12);
            this.lblInfo.Size = new Size(760, 23);
            this.lblInfo.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold);
            this.lblInfo.Text = $"重複ファイル一覧 ({duplicateGroups.Count}グループ)";

            // TreeView
            this.treeViewDuplicates.Location = new Point(12, 45);
            this.treeViewDuplicates.Size = new Size(760, 400);
            this.treeViewDuplicates.FullRowSelect = true;
            this.treeViewDuplicates.ShowLines = true;
            this.treeViewDuplicates.ShowPlusMinus = true;
            this.treeViewDuplicates.ShowRootLines = true;
            this.treeViewDuplicates.NodeMouseDoubleClick += new TreeNodeMouseClickEventHandler(this.treeViewDuplicates_NodeMouseDoubleClick);

            // ファイルを開くボタン
            this.btnOpenFile.Location = new Point(12, 455);
            this.btnOpenFile.Size = new Size(100, 30);
            this.btnOpenFile.Text = "ファイルを開く";
            this.btnOpenFile.UseVisualStyleBackColor = true;
            this.btnOpenFile.Click += new EventHandler(this.btnOpenFile_Click);

            // フォルダを開くボタン
            this.btnOpenFolder.Location = new Point(125, 455);
            this.btnOpenFolder.Size = new Size(100, 30);
            this.btnOpenFolder.Text = "フォルダを開く";
            this.btnOpenFolder.UseVisualStyleBackColor = true;
            this.btnOpenFolder.Click += new EventHandler(this.btnOpenFolder_Click);

            // 閉じるボタン
            this.btnClose.Location = new Point(697, 455);
            this.btnClose.Size = new Size(75, 30);
            this.btnClose.Text = "閉じる";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new EventHandler(this.btnClose_Click);

            // DuplicateFilesForm
            this.ClientSize = new Size(784, 497);
            this.MinimumSize = new Size(600, 400);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.treeViewDuplicates);
            this.Controls.Add(this.btnOpenFile);
            this.Controls.Add(this.btnOpenFolder);
            this.Controls.Add(this.btnClose);
            this.Text = "重複ファイル - RecoStaScan";
            this.StartPosition = FormStartPosition.CenterParent;
            this.ResumeLayout(false);
        }

        private TreeView treeViewDuplicates;
        private Label lblInfo;
        private Button btnClose;
        private Button btnOpenFile;
        private Button btnOpenFolder;

        private void LoadData()
        {
            treeViewDuplicates.Nodes.Clear();

            foreach (var group in duplicateGroups.OrderBy(g => g.ProjectName))
            {
                var groupNode = new TreeNode($"{group.ProjectName} ({group.Files.Count}ファイル)")
                {
                    Tag = group,
                    ImageIndex = 0,
                    SelectedImageIndex = 0
                };

                foreach (var file in group.Files.OrderByDescending(f => f.LastModified))
                {
                    var fileName = Path.GetFileName(file.FilePath);
                    var fileSize = FormatFileSize(file.FileSize);
                    var lastModified = file.LastModified.ToString("yyyy/MM/dd HH:mm:ss");
                    var status = file.IsSelected ? " [選択済み]" : "";
                    
                    var fileNode = new TreeNode($"{fileName} - {fileSize} - {lastModified}{status}")
                    {
                        Tag = file,
                        ImageIndex = file.IsSelected ? 1 : 2,
                        SelectedImageIndex = file.IsSelected ? 1 : 2
                    };

                    if (file.IsSelected)
                    {
                        fileNode.ForeColor = Color.DarkGreen;
                        fileNode.NodeFont = new Font(treeViewDuplicates.Font, FontStyle.Bold);
                    }
                    else
                    {
                        fileNode.ForeColor = Color.Gray;
                    }

                    groupNode.Nodes.Add(fileNode);
                }

                treeViewDuplicates.Nodes.Add(groupNode);
            }

            // 全てのノードを展開
            treeViewDuplicates.ExpandAll();
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            else
                return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }

        private void treeViewDuplicates_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is FileEntry file)
            {
                OpenFile(file.FilePath);
            }
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            var selectedNode = treeViewDuplicates.SelectedNode;
            if (selectedNode?.Tag is FileEntry file)
            {
                OpenFile(file.FilePath);
            }
            else
            {
                MessageBox.Show("ファイルを選択してください。", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            var selectedNode = treeViewDuplicates.SelectedNode;
            if (selectedNode?.Tag is FileEntry file)
            {
                OpenFolderAndSelectFile(file.FilePath);
            }
            else
            {
                MessageBox.Show("ファイルを選択してください。", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OpenFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("ファイルが見つかりません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ファイルを開けませんでした: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenFolderAndSelectFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{filePath}\"",
                        UseShellExecute = false
                    });
                }
                else
                {
                    MessageBox.Show("ファイルが見つかりません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"フォルダを開けませんでした: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}