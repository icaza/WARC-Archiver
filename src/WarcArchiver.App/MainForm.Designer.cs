using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WarcArchiver.App.Controls;

namespace WarcArchiver.App
{
    partial class MainForm
    {
        /// <summary>Required designer variable.</summary>
        private IContainer components = null;

        /// <summary>Clean up any resources being used.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify the contents of this method with the code editor.
        /// Every control of the application is declared, configured and placed here, as plain property assignments
        /// that the Visual Studio designer can read back and rewrite.
        /// </summary>
        private void InitializeComponent()
        {
            components = new Container();
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle6 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle7 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle10 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle8 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle9 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle11 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle12 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle14 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle13 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle15 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle16 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle19 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle17 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle18 = new DataGridViewCellStyle();
            ComponentResourceManager resources = new ComponentResourceManager(typeof(MainForm));
            rootLayout = new TableLayoutPanel();
            sidebarPanel = new Panel();
            sidebarLayout = new TableLayoutPanel();
            brandPanel = new Panel();
            brandTitleLabel = new Label();
            brandSubtitleLabel = new Label();
            navCaptureButton = new Button();
            navArchivesButton = new Button();
            navVerifyButton = new Button();
            navAuditButton = new Button();
            navSettingsButton = new Button();
            versionLabel = new Label();
            contentPanel = new Panel();
            capturePage = new TableLayoutPanel();
            captureHeaderPanel = new Panel();
            captureTitleLabel = new Label();
            captureSubtitleLabel = new Label();
            urlLayout = new TableLayoutPanel();
            urlLabel = new Label();
            urlTextBox = new TextBox();
            captureButton = new Button();
            cancelButton = new Button();
            caseLayout = new TableLayoutPanel();
            caseRefLabel = new Label();
            operatorLabel = new Label();
            notesLabel = new Label();
            caseRefTextBox = new TextBox();
            operatorTextBox = new TextBox();
            notesTextBox = new TextBox();
            optionsLayout = new FlowLayoutPanel();
            optionsLabel = new Label();
            scriptsCheckBox = new CheckBox();
            stylesheetsCheckBox = new CheckBox();
            imagesCheckBox = new CheckBox();
            otherAssetsCheckBox = new CheckBox();
            thirdPartyCheckBox = new CheckBox();
            signLayout = new TableLayoutPanel();
            signLabel = new Label();
            signComboBox = new ComboBox();
            refreshCertsButton = new Button();
            progressLayout = new TableLayoutPanel();
            captureProgressBar = new DarkProgressBar();
            progressLabel = new Label();
            resultsGrid = new DataGridView();
            resTimeColumn = new DataGridViewTextBoxColumn();
            resStatusColumn = new DataGridViewTextBoxColumn();
            resTypeColumn = new DataGridViewTextBoxColumn();
            resUrlColumn = new DataGridViewTextBoxColumn();
            resSizeColumn = new DataGridViewTextBoxColumn();
            resShaColumn = new DataGridViewTextBoxColumn();
            resMsColumn = new DataGridViewTextBoxColumn();
            resNoteColumn = new DataGridViewTextBoxColumn();
            summaryLayout = new TableLayoutPanel();
            summaryLabel = new Label();
            openFolderButton = new Button();
            verifyThisButton = new Button();
            copyHashButton = new Button();
            archivesPage = new TableLayoutPanel();
            archivesHeaderPanel = new Panel();
            archivesTitleLabel = new Label();
            archivesSubtitleLabel = new Label();
            archivesToolbar = new FlowLayoutPanel();
            refreshArchivesButton = new Button();
            openOutputFolderButton = new Button();
            verifySelectedButton = new Button();
            copySha256Button = new Button();
            archivesFilterTextBox = new TextBox();
            archivesGrid = new DataGridView();
            arcDateColumn = new DataGridViewTextBoxColumn();
            arcUrlColumn = new DataGridViewTextBoxColumn();
            arcCaseColumn = new DataGridViewTextBoxColumn();
            arcRecordsColumn = new DataGridViewTextBoxColumn();
            arcSizeColumn = new DataGridViewTextBoxColumn();
            arcSignedColumn = new DataGridViewTextBoxColumn();
            arcShaColumn = new DataGridViewTextBoxColumn();
            arcFileColumn = new DataGridViewTextBoxColumn();
            archivesCountLabel = new Label();
            verifyPage = new TableLayoutPanel();
            verifyHeaderPanel = new Panel();
            verifyTitleLabel = new Label();
            verifySubtitleLabel = new Label();
            verifyInputLayout = new TableLayoutPanel();
            verifyPathTextBox = new TextBox();
            verifyBrowseButton = new Button();
            verifyRunButton = new Button();
            verifyStatusLabel = new Label();
            verifyGrid = new DataGridView();
            vfStatusColumn = new DataGridViewTextBoxColumn();
            vfCheckColumn = new DataGridViewTextBoxColumn();
            vfDetailColumn = new DataGridViewTextBoxColumn();
            verifyFooterLayout = new TableLayoutPanel();
            verifyVerdictLabel = new Label();
            verifyExportButton = new Button();
            auditPage = new TableLayoutPanel();
            auditHeaderPanel = new Panel();
            auditTitleLabel = new Label();
            auditSubtitleLabel = new Label();
            auditToolbar = new FlowLayoutPanel();
            auditRefreshButton = new Button();
            auditVerifyButton = new Button();
            auditCopyHeadButton = new Button();
            auditOpenButton = new Button();
            auditGrid = new DataGridView();
            audSeqColumn = new DataGridViewTextBoxColumn();
            audTimeColumn = new DataGridViewTextBoxColumn();
            audEventColumn = new DataGridViewTextBoxColumn();
            audDetailsColumn = new DataGridViewTextBoxColumn();
            audHashColumn = new DataGridViewTextBoxColumn();
            auditStatusLabel = new Label();
            settingsPage = new TableLayoutPanel();
            settingsHeaderPanel = new Panel();
            settingsTitleLabel = new Label();
            settingsSubtitleLabel = new Label();
            configPathLabel = new Label();
            settingsTextBox = new TextBox();
            settingsToolbar = new FlowLayoutPanel();
            openConfigButton = new Button();
            reloadConfigButton = new Button();
            changeOutputButton = new Button();
            settingsNoteLabel = new Label();
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            chainStatusLabel = new ToolStripStatusLabel();
            outputStatusLabel = new ToolStripStatusLabel();
            toolTip = new ToolTip(components);
            folderBrowserDialog = new FolderBrowserDialog();
            openFileDialog = new OpenFileDialog();
            saveFileDialog = new SaveFileDialog();
            rootLayout.SuspendLayout();
            sidebarPanel.SuspendLayout();
            sidebarLayout.SuspendLayout();
            brandPanel.SuspendLayout();
            contentPanel.SuspendLayout();
            capturePage.SuspendLayout();
            captureHeaderPanel.SuspendLayout();
            urlLayout.SuspendLayout();
            caseLayout.SuspendLayout();
            optionsLayout.SuspendLayout();
            signLayout.SuspendLayout();
            progressLayout.SuspendLayout();
            ((ISupportInitialize)resultsGrid).BeginInit();
            summaryLayout.SuspendLayout();
            archivesPage.SuspendLayout();
            archivesHeaderPanel.SuspendLayout();
            archivesToolbar.SuspendLayout();
            ((ISupportInitialize)archivesGrid).BeginInit();
            verifyPage.SuspendLayout();
            verifyHeaderPanel.SuspendLayout();
            verifyInputLayout.SuspendLayout();
            ((ISupportInitialize)verifyGrid).BeginInit();
            verifyFooterLayout.SuspendLayout();
            auditPage.SuspendLayout();
            auditHeaderPanel.SuspendLayout();
            auditToolbar.SuspendLayout();
            ((ISupportInitialize)auditGrid).BeginInit();
            settingsPage.SuspendLayout();
            settingsHeaderPanel.SuspendLayout();
            settingsToolbar.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // rootLayout
            // 
            rootLayout.BackColor = Color.FromArgb(18, 20, 24);
            rootLayout.ColumnCount = 2;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 212F));
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.Controls.Add(sidebarPanel, 0, 0);
            rootLayout.Controls.Add(contentPanel, 1, 0);
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.Location = new Point(0, 0);
            rootLayout.Margin = new Padding(0);
            rootLayout.Name = "rootLayout";
            rootLayout.RowCount = 1;
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.Size = new Size(1200, 758);
            rootLayout.TabIndex = 0;
            // 
            // sidebarPanel
            // 
            sidebarPanel.BackColor = Color.FromArgb(26, 29, 35);
            sidebarPanel.Controls.Add(sidebarLayout);
            sidebarPanel.Dock = DockStyle.Fill;
            sidebarPanel.Location = new Point(0, 0);
            sidebarPanel.Margin = new Padding(0);
            sidebarPanel.Name = "sidebarPanel";
            sidebarPanel.Size = new Size(212, 758);
            sidebarPanel.TabIndex = 0;
            // 
            // sidebarLayout
            // 
            sidebarLayout.ColumnCount = 1;
            sidebarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            sidebarLayout.Controls.Add(brandPanel, 0, 0);
            sidebarLayout.Controls.Add(navCaptureButton, 0, 1);
            sidebarLayout.Controls.Add(navArchivesButton, 0, 2);
            sidebarLayout.Controls.Add(navVerifyButton, 0, 3);
            sidebarLayout.Controls.Add(navAuditButton, 0, 4);
            sidebarLayout.Controls.Add(navSettingsButton, 0, 5);
            sidebarLayout.Controls.Add(versionLabel, 0, 7);
            sidebarLayout.Dock = DockStyle.Fill;
            sidebarLayout.Location = new Point(0, 0);
            sidebarLayout.Margin = new Padding(0);
            sidebarLayout.Name = "sidebarLayout";
            sidebarLayout.RowCount = 8;
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            sidebarLayout.Size = new Size(212, 758);
            sidebarLayout.TabIndex = 0;
            // 
            // brandPanel
            // 
            brandPanel.Controls.Add(brandTitleLabel);
            brandPanel.Controls.Add(brandSubtitleLabel);
            brandPanel.Dock = DockStyle.Fill;
            brandPanel.Location = new Point(0, 0);
            brandPanel.Margin = new Padding(0);
            brandPanel.Name = "brandPanel";
            brandPanel.Size = new Size(212, 88);
            brandPanel.TabIndex = 0;
            // 
            // brandTitleLabel
            // 
            brandTitleLabel.AutoSize = true;
            brandTitleLabel.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Regular, GraphicsUnit.Point, 0);
            brandTitleLabel.ForeColor = Color.FromArgb(230, 232, 235);
            brandTitleLabel.Location = new Point(20, 22);
            brandTitleLabel.Name = "brandTitleLabel";
            brandTitleLabel.Size = new Size(143, 25);
            brandTitleLabel.TabIndex = 0;
            brandTitleLabel.Text = "WARC Archiver";
            // 
            // brandSubtitleLabel
            // 
            brandSubtitleLabel.AutoSize = true;
            brandSubtitleLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            brandSubtitleLabel.ForeColor = Color.FromArgb(154, 163, 175);
            brandSubtitleLabel.Location = new Point(22, 50);
            brandSubtitleLabel.Name = "brandSubtitleLabel";
            brandSubtitleLabel.Size = new Size(124, 15);
            brandSubtitleLabel.TabIndex = 1;
            brandSubtitleLabel.Text = "Web evidence capture";
            // 
            // navCaptureButton
            // 
            navCaptureButton.BackColor = Color.FromArgb(26, 29, 35);
            navCaptureButton.Cursor = Cursors.Hand;
            navCaptureButton.Dock = DockStyle.Fill;
            navCaptureButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            navCaptureButton.FlatAppearance.BorderSize = 0;
            navCaptureButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(44, 49, 59);
            navCaptureButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(34, 38, 46);
            navCaptureButton.FlatStyle = FlatStyle.Flat;
            navCaptureButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            navCaptureButton.ForeColor = Color.FromArgb(154, 163, 175);
            navCaptureButton.Location = new Point(0, 88);
            navCaptureButton.Margin = new Padding(0);
            navCaptureButton.Name = "navCaptureButton";
            navCaptureButton.Padding = new Padding(22, 0, 0, 0);
            navCaptureButton.Size = new Size(212, 46);
            navCaptureButton.TabIndex = 1;
            navCaptureButton.Tag = "nav";
            navCaptureButton.Text = "Capture";
            navCaptureButton.TextAlign = ContentAlignment.MiddleLeft;
            navCaptureButton.UseVisualStyleBackColor = false;
            navCaptureButton.Click += NavButton_Click;
            navCaptureButton.Paint += NavButton_Paint;
            // 
            // navArchivesButton
            // 
            navArchivesButton.BackColor = Color.FromArgb(26, 29, 35);
            navArchivesButton.Cursor = Cursors.Hand;
            navArchivesButton.Dock = DockStyle.Fill;
            navArchivesButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            navArchivesButton.FlatAppearance.BorderSize = 0;
            navArchivesButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(44, 49, 59);
            navArchivesButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(34, 38, 46);
            navArchivesButton.FlatStyle = FlatStyle.Flat;
            navArchivesButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            navArchivesButton.ForeColor = Color.FromArgb(154, 163, 175);
            navArchivesButton.Location = new Point(0, 134);
            navArchivesButton.Margin = new Padding(0);
            navArchivesButton.Name = "navArchivesButton";
            navArchivesButton.Padding = new Padding(22, 0, 0, 0);
            navArchivesButton.Size = new Size(212, 46);
            navArchivesButton.TabIndex = 2;
            navArchivesButton.Tag = "nav";
            navArchivesButton.Text = "Archives";
            navArchivesButton.TextAlign = ContentAlignment.MiddleLeft;
            navArchivesButton.UseVisualStyleBackColor = false;
            navArchivesButton.Click += NavButton_Click;
            navArchivesButton.Paint += NavButton_Paint;
            // 
            // navVerifyButton
            // 
            navVerifyButton.BackColor = Color.FromArgb(26, 29, 35);
            navVerifyButton.Cursor = Cursors.Hand;
            navVerifyButton.Dock = DockStyle.Fill;
            navVerifyButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            navVerifyButton.FlatAppearance.BorderSize = 0;
            navVerifyButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(44, 49, 59);
            navVerifyButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(34, 38, 46);
            navVerifyButton.FlatStyle = FlatStyle.Flat;
            navVerifyButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            navVerifyButton.ForeColor = Color.FromArgb(154, 163, 175);
            navVerifyButton.Location = new Point(0, 180);
            navVerifyButton.Margin = new Padding(0);
            navVerifyButton.Name = "navVerifyButton";
            navVerifyButton.Padding = new Padding(22, 0, 0, 0);
            navVerifyButton.Size = new Size(212, 46);
            navVerifyButton.TabIndex = 3;
            navVerifyButton.Tag = "nav";
            navVerifyButton.Text = "Verify";
            navVerifyButton.TextAlign = ContentAlignment.MiddleLeft;
            navVerifyButton.UseVisualStyleBackColor = false;
            navVerifyButton.Click += NavButton_Click;
            navVerifyButton.Paint += NavButton_Paint;
            // 
            // navAuditButton
            // 
            navAuditButton.BackColor = Color.FromArgb(26, 29, 35);
            navAuditButton.Cursor = Cursors.Hand;
            navAuditButton.Dock = DockStyle.Fill;
            navAuditButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            navAuditButton.FlatAppearance.BorderSize = 0;
            navAuditButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(44, 49, 59);
            navAuditButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(34, 38, 46);
            navAuditButton.FlatStyle = FlatStyle.Flat;
            navAuditButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            navAuditButton.ForeColor = Color.FromArgb(154, 163, 175);
            navAuditButton.Location = new Point(0, 226);
            navAuditButton.Margin = new Padding(0);
            navAuditButton.Name = "navAuditButton";
            navAuditButton.Padding = new Padding(22, 0, 0, 0);
            navAuditButton.Size = new Size(212, 46);
            navAuditButton.TabIndex = 4;
            navAuditButton.Tag = "nav";
            navAuditButton.Text = "Audit log";
            navAuditButton.TextAlign = ContentAlignment.MiddleLeft;
            navAuditButton.UseVisualStyleBackColor = false;
            navAuditButton.Click += NavButton_Click;
            navAuditButton.Paint += NavButton_Paint;
            // 
            // navSettingsButton
            // 
            navSettingsButton.BackColor = Color.FromArgb(26, 29, 35);
            navSettingsButton.Cursor = Cursors.Hand;
            navSettingsButton.Dock = DockStyle.Fill;
            navSettingsButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            navSettingsButton.FlatAppearance.BorderSize = 0;
            navSettingsButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(44, 49, 59);
            navSettingsButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(34, 38, 46);
            navSettingsButton.FlatStyle = FlatStyle.Flat;
            navSettingsButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            navSettingsButton.ForeColor = Color.FromArgb(154, 163, 175);
            navSettingsButton.Location = new Point(0, 272);
            navSettingsButton.Margin = new Padding(0);
            navSettingsButton.Name = "navSettingsButton";
            navSettingsButton.Padding = new Padding(22, 0, 0, 0);
            navSettingsButton.Size = new Size(212, 46);
            navSettingsButton.TabIndex = 5;
            navSettingsButton.Tag = "nav";
            navSettingsButton.Text = "Settings";
            navSettingsButton.TextAlign = ContentAlignment.MiddleLeft;
            navSettingsButton.UseVisualStyleBackColor = false;
            navSettingsButton.Click += NavButton_Click;
            navSettingsButton.Paint += NavButton_Paint;
            // 
            // versionLabel
            // 
            versionLabel.Dock = DockStyle.Fill;
            versionLabel.ForeColor = Color.FromArgb(154, 163, 175);
            versionLabel.Location = new Point(0, 718);
            versionLabel.Margin = new Padding(0);
            versionLabel.Name = "versionLabel";
            versionLabel.Padding = new Padding(22, 0, 0, 0);
            versionLabel.Size = new Size(212, 40);
            versionLabel.TabIndex = 6;
            versionLabel.Text = "Version";
            versionLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // contentPanel
            // 
            contentPanel.BackColor = Color.FromArgb(18, 20, 24);
            contentPanel.Controls.Add(capturePage);
            contentPanel.Controls.Add(archivesPage);
            contentPanel.Controls.Add(verifyPage);
            contentPanel.Controls.Add(auditPage);
            contentPanel.Controls.Add(settingsPage);
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Location = new Point(212, 0);
            contentPanel.Margin = new Padding(0);
            contentPanel.Name = "contentPanel";
            contentPanel.Padding = new Padding(28, 22, 28, 14);
            contentPanel.Size = new Size(988, 758);
            contentPanel.TabIndex = 1;
            // 
            // capturePage
            // 
            capturePage.ColumnCount = 1;
            capturePage.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            capturePage.Controls.Add(captureHeaderPanel, 0, 0);
            capturePage.Controls.Add(urlLayout, 0, 1);
            capturePage.Controls.Add(caseLayout, 0, 2);
            capturePage.Controls.Add(optionsLayout, 0, 3);
            capturePage.Controls.Add(signLayout, 0, 4);
            capturePage.Controls.Add(progressLayout, 0, 5);
            capturePage.Controls.Add(resultsGrid, 0, 6);
            capturePage.Controls.Add(summaryLayout, 0, 7);
            capturePage.Dock = DockStyle.Fill;
            capturePage.Location = new Point(28, 22);
            capturePage.Margin = new Padding(0);
            capturePage.Name = "capturePage";
            capturePage.RowCount = 8;
            capturePage.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            capturePage.RowStyles.Add(new RowStyle());
            capturePage.RowStyles.Add(new RowStyle());
            capturePage.RowStyles.Add(new RowStyle());
            capturePage.RowStyles.Add(new RowStyle());
            capturePage.RowStyles.Add(new RowStyle());
            capturePage.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            capturePage.RowStyles.Add(new RowStyle());
            capturePage.Size = new Size(932, 722);
            capturePage.TabIndex = 0;
            // 
            // captureHeaderPanel
            // 
            captureHeaderPanel.Controls.Add(captureTitleLabel);
            captureHeaderPanel.Controls.Add(captureSubtitleLabel);
            captureHeaderPanel.Dock = DockStyle.Fill;
            captureHeaderPanel.Location = new Point(0, 0);
            captureHeaderPanel.Margin = new Padding(0);
            captureHeaderPanel.Name = "captureHeaderPanel";
            captureHeaderPanel.Size = new Size(932, 66);
            captureHeaderPanel.TabIndex = 0;
            // 
            // captureTitleLabel
            // 
            captureTitleLabel.AutoSize = true;
            captureTitleLabel.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Regular, GraphicsUnit.Point, 0);
            captureTitleLabel.ForeColor = Color.FromArgb(230, 232, 235);
            captureTitleLabel.Location = new Point(0, 0);
            captureTitleLabel.Name = "captureTitleLabel";
            captureTitleLabel.Size = new Size(221, 31);
            captureTitleLabel.TabIndex = 0;
            captureTitleLabel.Text = "Capture a web page";
            // 
            // captureSubtitleLabel
            // 
            captureSubtitleLabel.AutoSize = true;
            captureSubtitleLabel.ForeColor = Color.FromArgb(154, 163, 175);
            captureSubtitleLabel.Location = new Point(2, 38);
            captureSubtitleLabel.Name = "captureSubtitleLabel";
            captureSubtitleLabel.Size = new Size(616, 15);
            captureSubtitleLabel.TabIndex = 1;
            captureSubtitleLabel.Text = "Preserves the page and everything it loads as a WARC archive (ISO 28500), fingerprinted with SHA-256 and SHA-512.";
            // 
            // urlLayout
            // 
            urlLayout.AutoSize = true;
            urlLayout.ColumnCount = 3;
            urlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            urlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            urlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            urlLayout.Controls.Add(urlLabel, 0, 0);
            urlLayout.Controls.Add(urlTextBox, 0, 1);
            urlLayout.Controls.Add(captureButton, 1, 1);
            urlLayout.Controls.Add(cancelButton, 2, 1);
            urlLayout.Dock = DockStyle.Fill;
            urlLayout.Location = new Point(0, 66);
            urlLayout.Margin = new Padding(0, 0, 0, 12);
            urlLayout.Name = "urlLayout";
            urlLayout.RowCount = 2;
            urlLayout.RowStyles.Add(new RowStyle());
            urlLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            urlLayout.Size = new Size(932, 65);
            urlLayout.TabIndex = 1;
            // 
            // urlLabel
            // 
            urlLabel.AutoSize = true;
            urlLayout.SetColumnSpan(urlLabel, 3);
            urlLabel.ForeColor = Color.FromArgb(154, 163, 175);
            urlLabel.Location = new Point(0, 0);
            urlLabel.Margin = new Padding(0, 0, 0, 6);
            urlLabel.Name = "urlLabel";
            urlLabel.Size = new Size(108, 15);
            urlLabel.TabIndex = 0;
            urlLabel.Text = "Page address (URL)";
            // 
            // urlTextBox
            // 
            urlTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            urlTextBox.BackColor = Color.FromArgb(34, 38, 46);
            urlTextBox.BorderStyle = BorderStyle.FixedSingle;
            urlTextBox.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            urlTextBox.ForeColor = Color.FromArgb(230, 232, 235);
            urlTextBox.Location = new Point(0, 29);
            urlTextBox.Margin = new Padding(0, 0, 10, 0);
            urlTextBox.MaxLength = 2048;
            urlTextBox.Name = "urlTextBox";
            urlTextBox.PlaceholderText = "https://example.com/page-to-archive";
            urlTextBox.Size = new Size(662, 27);
            urlTextBox.TabIndex = 0;
            toolTip.SetToolTip(urlTextBox, "Only http:// and https:// addresses are accepted. Press Enter to start.");
            urlTextBox.KeyDown += UrlTextBox_KeyDown;
            // 
            // captureButton
            // 
            captureButton.BackColor = Color.FromArgb(76, 154, 255);
            captureButton.Cursor = Cursors.Hand;
            captureButton.Dock = DockStyle.Fill;
            captureButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            captureButton.FlatAppearance.BorderSize = 0;
            captureButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 126, 224);
            captureButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(105, 172, 255);
            captureButton.FlatStyle = FlatStyle.Flat;
            captureButton.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            captureButton.ForeColor = Color.FromArgb(10, 16, 28);
            captureButton.Location = new Point(672, 21);
            captureButton.Margin = new Padding(0, 0, 10, 0);
            captureButton.Name = "captureButton";
            captureButton.Size = new Size(140, 44);
            captureButton.TabIndex = 1;
            captureButton.Tag = "primary";
            captureButton.Text = "Start capture";
            toolTip.SetToolTip(captureButton, "Fetch the page and everything it references, and write a WARC archive with SHA-256 / SHA-512 fingerprints.");
            captureButton.UseVisualStyleBackColor = false;
            captureButton.Click += CaptureButton_Click;
            // 
            // cancelButton
            // 
            cancelButton.BackColor = Color.FromArgb(34, 38, 46);
            cancelButton.Cursor = Cursors.Hand;
            cancelButton.Dock = DockStyle.Fill;
            cancelButton.Enabled = false;
            cancelButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            cancelButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 58, 68);
            cancelButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 49, 59);
            cancelButton.FlatStyle = FlatStyle.Flat;
            cancelButton.ForeColor = Color.FromArgb(230, 232, 235);
            cancelButton.Location = new Point(822, 21);
            cancelButton.Margin = new Padding(0);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(110, 44);
            cancelButton.TabIndex = 2;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = false;
            cancelButton.Click += CancelButton_Click;
            // 
            // caseLayout
            // 
            caseLayout.AutoSize = true;
            caseLayout.ColumnCount = 3;
            caseLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            caseLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            caseLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            caseLayout.Controls.Add(caseRefLabel, 0, 0);
            caseLayout.Controls.Add(operatorLabel, 1, 0);
            caseLayout.Controls.Add(notesLabel, 2, 0);
            caseLayout.Controls.Add(caseRefTextBox, 0, 1);
            caseLayout.Controls.Add(operatorTextBox, 1, 1);
            caseLayout.Controls.Add(notesTextBox, 2, 1);
            caseLayout.Dock = DockStyle.Fill;
            caseLayout.Location = new Point(0, 143);
            caseLayout.Margin = new Padding(0, 0, 0, 12);
            caseLayout.Name = "caseLayout";
            caseLayout.RowCount = 2;
            caseLayout.RowStyles.Add(new RowStyle());
            caseLayout.RowStyles.Add(new RowStyle());
            caseLayout.Size = new Size(932, 44);
            caseLayout.TabIndex = 2;
            // 
            // caseRefLabel
            // 
            caseRefLabel.AutoSize = true;
            caseRefLabel.ForeColor = Color.FromArgb(154, 163, 175);
            caseRefLabel.Location = new Point(0, 0);
            caseRefLabel.Margin = new Padding(0, 0, 0, 6);
            caseRefLabel.Name = "caseRefLabel";
            caseRefLabel.Size = new Size(92, 15);
            caseRefLabel.TabIndex = 0;
            caseRefLabel.Text = "Case / reference";
            // 
            // operatorLabel
            // 
            operatorLabel.AutoSize = true;
            operatorLabel.ForeColor = Color.FromArgb(154, 163, 175);
            operatorLabel.Location = new Point(310, 0);
            operatorLabel.Margin = new Padding(0, 0, 0, 6);
            operatorLabel.Name = "operatorLabel";
            operatorLabel.Size = new Size(54, 15);
            operatorLabel.TabIndex = 1;
            operatorLabel.Text = "Operator";
            // 
            // notesLabel
            // 
            notesLabel.AutoSize = true;
            notesLabel.ForeColor = Color.FromArgb(154, 163, 175);
            notesLabel.Location = new Point(620, 0);
            notesLabel.Margin = new Padding(0, 0, 0, 6);
            notesLabel.Name = "notesLabel";
            notesLabel.Size = new Size(38, 15);
            notesLabel.TabIndex = 2;
            notesLabel.Text = "Notes";
            // 
            // caseRefTextBox
            // 
            caseRefTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            caseRefTextBox.BackColor = Color.FromArgb(34, 38, 46);
            caseRefTextBox.BorderStyle = BorderStyle.FixedSingle;
            caseRefTextBox.ForeColor = Color.FromArgb(230, 232, 235);
            caseRefTextBox.Location = new Point(0, 21);
            caseRefTextBox.Margin = new Padding(0, 0, 12, 0);
            caseRefTextBox.MaxLength = 200;
            caseRefTextBox.Name = "caseRefTextBox";
            caseRefTextBox.PlaceholderText = "e.g. CASE-2026-014";
            caseRefTextBox.Size = new Size(298, 23);
            caseRefTextBox.TabIndex = 0;
            // 
            // operatorTextBox
            // 
            operatorTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            operatorTextBox.BackColor = Color.FromArgb(34, 38, 46);
            operatorTextBox.BorderStyle = BorderStyle.FixedSingle;
            operatorTextBox.ForeColor = Color.FromArgb(230, 232, 235);
            operatorTextBox.Location = new Point(310, 21);
            operatorTextBox.Margin = new Padding(0, 0, 12, 0);
            operatorTextBox.MaxLength = 200;
            operatorTextBox.Name = "operatorTextBox";
            operatorTextBox.PlaceholderText = "Your name";
            operatorTextBox.Size = new Size(298, 23);
            operatorTextBox.TabIndex = 1;
            // 
            // notesTextBox
            // 
            notesTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            notesTextBox.BackColor = Color.FromArgb(34, 38, 46);
            notesTextBox.BorderStyle = BorderStyle.FixedSingle;
            notesTextBox.ForeColor = Color.FromArgb(230, 232, 235);
            notesTextBox.Location = new Point(620, 21);
            notesTextBox.Margin = new Padding(0);
            notesTextBox.MaxLength = 500;
            notesTextBox.Name = "notesTextBox";
            notesTextBox.PlaceholderText = "Why this page is being preserved";
            notesTextBox.Size = new Size(312, 23);
            notesTextBox.TabIndex = 2;
            // 
            // optionsLayout
            // 
            optionsLayout.AutoSize = true;
            optionsLayout.Controls.Add(optionsLabel);
            optionsLayout.Controls.Add(scriptsCheckBox);
            optionsLayout.Controls.Add(stylesheetsCheckBox);
            optionsLayout.Controls.Add(imagesCheckBox);
            optionsLayout.Controls.Add(otherAssetsCheckBox);
            optionsLayout.Controls.Add(thirdPartyCheckBox);
            optionsLayout.Dock = DockStyle.Fill;
            optionsLayout.Location = new Point(0, 199);
            optionsLayout.Margin = new Padding(0, 0, 0, 10);
            optionsLayout.Name = "optionsLayout";
            optionsLayout.Size = new Size(932, 19);
            optionsLayout.TabIndex = 3;
            // 
            // optionsLabel
            // 
            optionsLabel.AutoSize = true;
            optionsLabel.ForeColor = Color.FromArgb(154, 163, 175);
            optionsLabel.Location = new Point(0, 4);
            optionsLabel.Margin = new Padding(0, 4, 14, 0);
            optionsLabel.Name = "optionsLabel";
            optionsLabel.Size = new Size(76, 15);
            optionsLabel.TabIndex = 0;
            optionsLabel.Text = "Also capture:";
            // 
            // scriptsCheckBox
            // 
            scriptsCheckBox.AutoSize = true;
            scriptsCheckBox.CheckState = CheckState.Checked;
            scriptsCheckBox.Checked = true;
            scriptsCheckBox.FlatAppearance.BorderColor = Color.FromArgb(154, 163, 175);
            scriptsCheckBox.FlatAppearance.CheckedBackColor = Color.FromArgb(76, 154, 255);
            scriptsCheckBox.FlatAppearance.MouseDownBackColor = Color.FromArgb(44, 49, 59);
            scriptsCheckBox.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 49, 59);
            scriptsCheckBox.FlatStyle = FlatStyle.Flat;
            scriptsCheckBox.ForeColor = Color.FromArgb(230, 232, 235);
            scriptsCheckBox.Location = new Point(90, 0);
            scriptsCheckBox.Margin = new Padding(0, 0, 20, 0);
            scriptsCheckBox.Name = "scriptsCheckBox";
            scriptsCheckBox.Size = new Size(80, 19);
            scriptsCheckBox.TabIndex = 1;
            scriptsCheckBox.Text = "Scripts (.js)";
            scriptsCheckBox.UseVisualStyleBackColor = true;
            // 
            // stylesheetsCheckBox
            // 
            stylesheetsCheckBox.AutoSize = true;
            stylesheetsCheckBox.CheckState = CheckState.Checked;
            stylesheetsCheckBox.Checked = true;
            stylesheetsCheckBox.FlatAppearance.BorderColor = Color.FromArgb(154, 163, 175);
            stylesheetsCheckBox.FlatAppearance.CheckedBackColor = Color.FromArgb(76, 154, 255);
            stylesheetsCheckBox.FlatAppearance.MouseDownBackColor = Color.FromArgb(44, 49, 59);
            stylesheetsCheckBox.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 49, 59);
            stylesheetsCheckBox.FlatStyle = FlatStyle.Flat;
            stylesheetsCheckBox.ForeColor = Color.FromArgb(230, 232, 235);
            stylesheetsCheckBox.Location = new Point(190, 0);
            stylesheetsCheckBox.Margin = new Padding(0, 0, 20, 0);
            stylesheetsCheckBox.Name = "stylesheetsCheckBox";
            stylesheetsCheckBox.Size = new Size(81, 19);
            stylesheetsCheckBox.TabIndex = 2;
            stylesheetsCheckBox.Text = "Stylesheets";
            stylesheetsCheckBox.UseVisualStyleBackColor = true;
            // 
            // imagesCheckBox
            // 
            imagesCheckBox.AutoSize = true;
            imagesCheckBox.CheckState = CheckState.Checked;
            imagesCheckBox.Checked = true;
            imagesCheckBox.FlatAppearance.BorderColor = Color.FromArgb(154, 163, 175);
            imagesCheckBox.FlatAppearance.CheckedBackColor = Color.FromArgb(76, 154, 255);
            imagesCheckBox.FlatAppearance.MouseDownBackColor = Color.FromArgb(44, 49, 59);
            imagesCheckBox.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 49, 59);
            imagesCheckBox.FlatStyle = FlatStyle.Flat;
            imagesCheckBox.ForeColor = Color.FromArgb(230, 232, 235);
            imagesCheckBox.Location = new Point(291, 0);
            imagesCheckBox.Margin = new Padding(0, 0, 20, 0);
            imagesCheckBox.Name = "imagesCheckBox";
            imagesCheckBox.Size = new Size(61, 19);
            imagesCheckBox.TabIndex = 3;
            imagesCheckBox.Text = "Images";
            imagesCheckBox.UseVisualStyleBackColor = true;
            // 
            // otherAssetsCheckBox
            // 
            otherAssetsCheckBox.AutoSize = true;
            otherAssetsCheckBox.CheckState = CheckState.Checked;
            otherAssetsCheckBox.Checked = true;
            otherAssetsCheckBox.FlatAppearance.BorderColor = Color.FromArgb(154, 163, 175);
            otherAssetsCheckBox.FlatAppearance.CheckedBackColor = Color.FromArgb(76, 154, 255);
            otherAssetsCheckBox.FlatAppearance.MouseDownBackColor = Color.FromArgb(44, 49, 59);
            otherAssetsCheckBox.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 49, 59);
            otherAssetsCheckBox.FlatStyle = FlatStyle.Flat;
            otherAssetsCheckBox.ForeColor = Color.FromArgb(230, 232, 235);
            otherAssetsCheckBox.Location = new Point(372, 0);
            otherAssetsCheckBox.Margin = new Padding(0, 0, 20, 0);
            otherAssetsCheckBox.Name = "otherAssetsCheckBox";
            otherAssetsCheckBox.Size = new Size(153, 19);
            otherAssetsCheckBox.TabIndex = 4;
            otherAssetsCheckBox.Text = "Fonts, media and frames";
            otherAssetsCheckBox.UseVisualStyleBackColor = true;
            // 
            // thirdPartyCheckBox
            // 
            thirdPartyCheckBox.AutoSize = true;
            thirdPartyCheckBox.CheckState = CheckState.Checked;
            thirdPartyCheckBox.Checked = true;
            thirdPartyCheckBox.FlatAppearance.BorderColor = Color.FromArgb(154, 163, 175);
            thirdPartyCheckBox.FlatAppearance.CheckedBackColor = Color.FromArgb(76, 154, 255);
            thirdPartyCheckBox.FlatAppearance.MouseDownBackColor = Color.FromArgb(44, 49, 59);
            thirdPartyCheckBox.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 49, 59);
            thirdPartyCheckBox.FlatStyle = FlatStyle.Flat;
            thirdPartyCheckBox.ForeColor = Color.FromArgb(230, 232, 235);
            thirdPartyCheckBox.Location = new Point(545, 0);
            thirdPartyCheckBox.Margin = new Padding(0, 0, 20, 0);
            thirdPartyCheckBox.Name = "thirdPartyCheckBox";
            thirdPartyCheckBox.Size = new Size(185, 19);
            thirdPartyCheckBox.TabIndex = 5;
            thirdPartyCheckBox.Text = "Resources from other domains";
            toolTip.SetToolTip(thirdPartyCheckBox, "Archive images, scripts and styles served from other domains (CDNs, trackers) as well.");
            thirdPartyCheckBox.UseVisualStyleBackColor = true;
            // 
            // signLayout
            // 
            signLayout.AutoSize = true;
            signLayout.ColumnCount = 3;
            signLayout.ColumnStyles.Add(new ColumnStyle());
            signLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 480F));
            signLayout.ColumnStyles.Add(new ColumnStyle());
            signLayout.Controls.Add(signLabel, 0, 0);
            signLayout.Controls.Add(signComboBox, 1, 0);
            signLayout.Controls.Add(refreshCertsButton, 2, 0);
            signLayout.Dock = DockStyle.Fill;
            signLayout.Location = new Point(0, 228);
            signLayout.Margin = new Padding(0, 0, 0, 12);
            signLayout.Name = "signLayout";
            signLayout.RowCount = 1;
            signLayout.RowStyles.Add(new RowStyle());
            signLayout.Size = new Size(932, 29);
            signLayout.TabIndex = 4;
            // 
            // signLabel
            // 
            signLabel.Anchor = AnchorStyles.Left;
            signLabel.AutoSize = true;
            signLabel.ForeColor = Color.FromArgb(154, 163, 175);
            signLabel.Location = new Point(0, 7);
            signLabel.Margin = new Padding(0, 0, 14, 0);
            signLabel.Name = "signLabel";
            signLabel.Size = new Size(128, 15);
            signLabel.TabIndex = 0;
            signLabel.Text = "Sign the manifest with:";
            // 
            // signComboBox
            // 
            signComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            signComboBox.BackColor = Color.FromArgb(34, 38, 46);
            signComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            signComboBox.FlatStyle = FlatStyle.Flat;
            signComboBox.ForeColor = Color.FromArgb(230, 232, 235);
            signComboBox.FormattingEnabled = true;
            signComboBox.Location = new Point(142, 3);
            signComboBox.Margin = new Padding(0, 3, 10, 3);
            signComboBox.Name = "signComboBox";
            signComboBox.Size = new Size(470, 23);
            signComboBox.TabIndex = 0;
            toolTip.SetToolTip(signComboBox, "Optional. Signs the manifest with a certificate from your Windows \"Personal\" store, so the archive's origin can be proven later.");
            // 
            // refreshCertsButton
            // 
            refreshCertsButton.Anchor = AnchorStyles.Left;
            refreshCertsButton.BackColor = Color.FromArgb(34, 38, 46);
            refreshCertsButton.Cursor = Cursors.Hand;
            refreshCertsButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            refreshCertsButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 58, 68);
            refreshCertsButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 49, 59);
            refreshCertsButton.FlatStyle = FlatStyle.Flat;
            refreshCertsButton.ForeColor = Color.FromArgb(230, 232, 235);
            refreshCertsButton.Location = new Point(622, 0);
            refreshCertsButton.Margin = new Padding(0);
            refreshCertsButton.Name = "refreshCertsButton";
            refreshCertsButton.Size = new Size(96, 28);
            refreshCertsButton.TabIndex = 1;
            refreshCertsButton.Text = "Refresh";
            refreshCertsButton.UseVisualStyleBackColor = false;
            refreshCertsButton.Click += RefreshCertsButton_Click;
            // 
            // progressLayout
            // 
            progressLayout.AutoSize = true;
            progressLayout.ColumnCount = 1;
            progressLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            progressLayout.Controls.Add(captureProgressBar, 0, 0);
            progressLayout.Controls.Add(progressLabel, 0, 1);
            progressLayout.Dock = DockStyle.Fill;
            progressLayout.Location = new Point(0, 269);
            progressLayout.Margin = new Padding(0, 0, 0, 8);
            progressLayout.Name = "progressLayout";
            progressLayout.RowCount = 2;
            progressLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 14F));
            progressLayout.RowStyles.Add(new RowStyle());
            progressLayout.Size = new Size(932, 33);
            progressLayout.TabIndex = 5;
            // 
            // captureProgressBar
            // 
            captureProgressBar.Dock = DockStyle.Fill;
            captureProgressBar.Location = new Point(0, 0);
            captureProgressBar.Margin = new Padding(0, 0, 0, 4);
            captureProgressBar.Name = "captureProgressBar";
            captureProgressBar.Size = new Size(932, 10);
            captureProgressBar.TabIndex = 0;
            captureProgressBar.TabStop = false;
            // 
            // progressLabel
            // 
            progressLabel.AutoSize = true;
            progressLabel.ForeColor = Color.FromArgb(154, 163, 175);
            progressLabel.Location = new Point(0, 18);
            progressLabel.Margin = new Padding(0, 4, 0, 0);
            progressLabel.Name = "progressLabel";
            progressLabel.Size = new Size(42, 15);
            progressLabel.TabIndex = 1;
            progressLabel.Text = "Ready.";
            // 
            // resultsGrid
            // 
            resultsGrid.AllowUserToAddRows = false;
            resultsGrid.AllowUserToDeleteRows = false;
            resultsGrid.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(30, 33, 40);
            dataGridViewCellStyle1.ForeColor = Color.FromArgb(230, 232, 235);
            dataGridViewCellStyle1.SelectionBackColor = Color.FromArgb(38, 79, 140);
            dataGridViewCellStyle1.SelectionForeColor = Color.FromArgb(230, 232, 235);
            resultsGrid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            resultsGrid.BackgroundColor = Color.FromArgb(26, 29, 35);
            resultsGrid.BorderStyle = BorderStyle.None;
            resultsGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            resultsGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = Color.FromArgb(34, 38, 46);
            dataGridViewCellStyle2.ForeColor = Color.FromArgb(154, 163, 175);
            dataGridViewCellStyle2.Padding = new Padding(6, 0, 6, 0);
            dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(34, 38, 46);
            dataGridViewCellStyle2.SelectionForeColor = Color.FromArgb(154, 163, 175);
            resultsGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            resultsGrid.ColumnHeadersHeight = 34;
            resultsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            resultsGrid.Columns.AddRange(new DataGridViewColumn[] { resTimeColumn, resStatusColumn, resTypeColumn, resUrlColumn, resSizeColumn, resShaColumn, resMsColumn, resNoteColumn });
            dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = Color.FromArgb(26, 29, 35);
            dataGridViewCellStyle5.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle5.ForeColor = Color.FromArgb(230, 232, 235);
            dataGridViewCellStyle5.Padding = new Padding(6, 0, 6, 0);
            dataGridViewCellStyle5.SelectionBackColor = Color.FromArgb(38, 79, 140);
            dataGridViewCellStyle5.SelectionForeColor = Color.FromArgb(230, 232, 235);
            dataGridViewCellStyle5.WrapMode = DataGridViewTriState.False;
            resultsGrid.DefaultCellStyle = dataGridViewCellStyle5;
            resultsGrid.Dock = DockStyle.Fill;
            resultsGrid.EnableHeadersVisualStyles = false;
            resultsGrid.GridColor = Color.FromArgb(52, 58, 68);
            resultsGrid.Location = new Point(0, 310);
            resultsGrid.Margin = new Padding(0, 0, 0, 10);
            resultsGrid.MultiSelect = false;
            resultsGrid.Name = "resultsGrid";
            resultsGrid.ReadOnly = true;
            resultsGrid.RowHeadersVisible = false;
            resultsGrid.RowTemplate.Height = 28;
            resultsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            resultsGrid.Size = new Size(932, 370);
            resultsGrid.TabIndex = 6;
            toolTip.SetToolTip(resultsGrid, "The SHA-256 shown is that of the response payload exactly as the server sent it (compressed bodies stay compressed).");
            // 
            // resTimeColumn
            // 
            resTimeColumn.HeaderText = "Time";
            resTimeColumn.MinimumWidth = 40;
            resTimeColumn.Name = "resTimeColumn";
            resTimeColumn.ReadOnly = true;
            resTimeColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            resTimeColumn.Width = 78;
            // 
            // resStatusColumn
            // 
            resStatusColumn.HeaderText = "Status";
            resStatusColumn.MinimumWidth = 40;
            resStatusColumn.Name = "resStatusColumn";
            resStatusColumn.ReadOnly = true;
            resStatusColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            resStatusColumn.Width = 74;
            // 
            // resTypeColumn
            // 
            resTypeColumn.HeaderText = "Type";
            resTypeColumn.MinimumWidth = 40;
            resTypeColumn.Name = "resTypeColumn";
            resTypeColumn.ReadOnly = true;
            resTypeColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            resTypeColumn.Width = 86;
            // 
            // resUrlColumn
            // 
            resUrlColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            resUrlColumn.FillWeight = 70F;
            resUrlColumn.HeaderText = "URL";
            resUrlColumn.MinimumWidth = 140;
            resUrlColumn.Name = "resUrlColumn";
            resUrlColumn.ReadOnly = true;
            resUrlColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // resSizeColumn
            // 
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleRight;
            resSizeColumn.DefaultCellStyle = dataGridViewCellStyle3;
            resSizeColumn.HeaderText = "Size";
            resSizeColumn.MinimumWidth = 40;
            resSizeColumn.Name = "resSizeColumn";
            resSizeColumn.ReadOnly = true;
            resSizeColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            resSizeColumn.Width = 86;
            // 
            // resShaColumn
            // 
            resShaColumn.HeaderText = "SHA-256 (payload)";
            resShaColumn.MinimumWidth = 40;
            resShaColumn.Name = "resShaColumn";
            resShaColumn.ReadOnly = true;
            resShaColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            resShaColumn.Width = 150;
            // 
            // resMsColumn
            // 
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleRight;
            resMsColumn.DefaultCellStyle = dataGridViewCellStyle4;
            resMsColumn.HeaderText = "ms";
            resMsColumn.MinimumWidth = 40;
            resMsColumn.Name = "resMsColumn";
            resMsColumn.ReadOnly = true;
            resMsColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            resMsColumn.Width = 64;
            // 
            // resNoteColumn
            // 
            resNoteColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            resNoteColumn.FillWeight = 30F;
            resNoteColumn.HeaderText = "Note";
            resNoteColumn.MinimumWidth = 140;
            resNoteColumn.Name = "resNoteColumn";
            resNoteColumn.ReadOnly = true;
            resNoteColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // summaryLayout
            // 
            summaryLayout.AutoSize = true;
            summaryLayout.ColumnCount = 4;
            summaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            summaryLayout.ColumnStyles.Add(new ColumnStyle());
            summaryLayout.ColumnStyles.Add(new ColumnStyle());
            summaryLayout.ColumnStyles.Add(new ColumnStyle());
            summaryLayout.Controls.Add(summaryLabel, 0, 0);
            summaryLayout.Controls.Add(openFolderButton, 1, 0);
            summaryLayout.Controls.Add(verifyThisButton, 2, 0);
            summaryLayout.Controls.Add(copyHashButton, 3, 0);
            summaryLayout.Dock = DockStyle.Fill;
            summaryLayout.Location = new Point(0, 690);
            summaryLayout.Margin = new Padding(0);
            summaryLayout.Name = "summaryLayout";
            summaryLayout.RowCount = 1;
            summaryLayout.RowStyles.Add(new RowStyle());
            summaryLayout.Size = new Size(932, 32);
            summaryLayout.TabIndex = 7;
            // 
            // summaryLabel
            // 
            summaryLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            summaryLabel.AutoEllipsis = true;
            summaryLabel.ForeColor = Color.FromArgb(154, 163, 175);
            summaryLabel.Location = new Point(0, 6);
            summaryLabel.Margin = new Padding(0, 0, 12, 0);
            summaryLabel.Name = "summaryLabel";
            summaryLabel.Size = new Size(518, 20);
            summaryLabel.TabIndex = 0;
            summaryLabel.Text = "No capture yet.";
            summaryLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // openFolderButton
            // 
            openFolderButton.BackColor = Color.FromArgb(34, 38, 46);
            openFolderButton.Cursor = Cursors.Hand;
            openFolderButton.Enabled = false;
            openFolderButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            openFolderButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 58, 68);
            openFolderButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 49, 59);
            openFolderButton.FlatStyle = FlatStyle.Flat;
            openFolderButton.ForeColor = Color.FromArgb(230, 232, 235);
            openFolderButton.Location = new Point(530, 0);
            openFolderButton.Margin = new Padding(0, 0, 8, 0);
            openFolderButton.Name = "openFolderButton";
            openFolderButton.Size = new Size(112, 32);
            openFolderButton.TabIndex = 1;
            openFolderButton.Text = "Open folder";
            openFolderButton.UseVisualStyleBackColor = false;
            openFolderButton.Click += OpenFolderButton_Click;
            // 
            // verifyThisButton
            // 
            verifyThisButton.BackColor = Color.FromArgb(34, 38, 46);
            verifyThisButton.Cursor = Cursors.Hand;
            verifyThisButton.Enabled = false;
            verifyThisButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            verifyThisButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 58, 68);
            verifyThisButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 49, 59);
            verifyThisButton.FlatStyle = FlatStyle.Flat;
            verifyThisButton.ForeColor = Color.FromArgb(230, 232, 235);
            verifyThisButton.Location = new Point(650, 0);
            verifyThisButton.Margin = new Padding(0, 0, 8, 0);
            verifyThisButton.Name = "verifyThisButton";
            verifyThisButton.Size = new Size(150, 32);
            verifyThisButton.TabIndex = 2;
            verifyThisButton.Text = "Verify this archive";
            verifyThisButton.UseVisualStyleBackColor = false;
            verifyThisButton.Click += VerifyThisButton_Click;
            // 
            // copyHashButton
            // 
            copyHashButton.BackColor = Color.FromArgb(34, 38, 46);
            copyHashButton.Cursor = Cursors.Hand;
            copyHashButton.Enabled = false;
            copyHashButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            copyHashButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 58, 68);
            copyHashButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 49, 59);
            copyHashButton.FlatStyle = FlatStyle.Flat;
            copyHashButton.ForeColor = Color.FromArgb(230, 232, 235);
            copyHashButton.Location = new Point(808, 0);
            copyHashButton.Margin = new Padding(0);
            copyHashButton.Name = "copyHashButton";
            copyHashButton.Size = new Size(124, 32);
            copyHashButton.TabIndex = 3;
            copyHashButton.Text = "Copy SHA-256";
            copyHashButton.UseVisualStyleBackColor = false;
            copyHashButton.Click += CopyHashButton_Click;
            // 
            // archivesPage
            // 
            archivesPage.ColumnCount = 1;
            archivesPage.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            archivesPage.Controls.Add(archivesHeaderPanel, 0, 0);
            archivesPage.Controls.Add(archivesToolbar, 0, 1);
            archivesPage.Controls.Add(archivesGrid, 0, 2);
            archivesPage.Controls.Add(archivesCountLabel, 0, 3);
            archivesPage.Dock = DockStyle.Fill;
            archivesPage.Location = new Point(28, 22);
            archivesPage.Margin = new Padding(0);
            archivesPage.Name = "archivesPage";
            archivesPage.RowCount = 4;
            archivesPage.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            archivesPage.RowStyles.Add(new RowStyle());
            archivesPage.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            archivesPage.RowStyles.Add(new RowStyle());
            archivesPage.Size = new Size(932, 722);
            archivesPage.TabIndex = 1;
            archivesPage.Visible = false;
            // 
            // archivesHeaderPanel
            // 
            archivesHeaderPanel.Controls.Add(archivesTitleLabel);
            archivesHeaderPanel.Controls.Add(archivesSubtitleLabel);
            archivesHeaderPanel.Dock = DockStyle.Fill;
            archivesHeaderPanel.Location = new Point(0, 0);
            archivesHeaderPanel.Margin = new Padding(0);
            archivesHeaderPanel.Name = "archivesHeaderPanel";
            archivesHeaderPanel.Size = new Size(932, 66);
            archivesHeaderPanel.TabIndex = 0;
            // 
            // archivesTitleLabel
            // 
            archivesTitleLabel.AutoSize = true;
            archivesTitleLabel.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Regular, GraphicsUnit.Point, 0);
            archivesTitleLabel.ForeColor = Color.FromArgb(230, 232, 235);
            archivesTitleLabel.Location = new Point(0, 0);
            archivesTitleLabel.Name = "archivesTitleLabel";
            archivesTitleLabel.Size = new Size(102, 31);
            archivesTitleLabel.TabIndex = 0;
            archivesTitleLabel.Text = "Archives";
            // 
            // archivesSubtitleLabel
            // 
            archivesSubtitleLabel.AutoSize = true;
            archivesSubtitleLabel.ForeColor = Color.FromArgb(154, 163, 175);
            archivesSubtitleLabel.Location = new Point(2, 38);
            archivesSubtitleLabel.Name = "archivesSubtitleLabel";
            archivesSubtitleLabel.Size = new Size(417, 15);
            archivesSubtitleLabel.TabIndex = 1;
            archivesSubtitleLabel.Text = "Evidence captured into your output folder. Double-click an archive to verify it.";
            // 
            // archivesToolbar
            // 
            archivesToolbar.AutoSize = true;
            archivesToolbar.Controls.Add(refreshArchivesButton);
            archivesToolbar.Controls.Add(openOutputFolderButton);
            archivesToolbar.Controls.Add(verifySelectedButton);
            archivesToolbar.Controls.Add(copySha256Button);
            archivesToolbar.Controls.Add(archivesFilterTextBox);
            archivesToolbar.Dock = DockStyle.Fill;
            archivesToolbar.Location = new Point(0, 66);
            archivesToolbar.Margin = new Padding(0, 0, 0, 10);
            archivesToolbar.Name = "archivesToolbar";
            archivesToolbar.Size = new Size(932, 32);
            archivesToolbar.TabIndex = 1;
            archivesToolbar.WrapContents = false;
            // 
            // refreshArchivesButton
            // 
            refreshArchivesButton.BackColor = Color.FromArgb(34, 38, 46);
            refreshArchivesButton.Cursor = Cursors.Hand;
            refreshArchivesButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            refreshArchivesButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 58, 68);
            refreshArchivesButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 49, 59);
            refreshArchivesButton.FlatStyle = FlatStyle.Flat;
            refreshArchivesButton.ForeColor = Color.FromArgb(230, 232, 235);
            refreshArchivesButton.Location = new Point(0, 0);
            refreshArchivesButton.Margin = new Padding(0, 0, 8, 0);
            refreshArchivesButton.Name = "refreshArchivesButton";
            refreshArchivesButton.Size = new Size(96, 32);
            refreshArchivesButton.TabIndex = 0;
            refreshArchivesButton.Text = "Refresh";
            refreshArchivesButton.UseVisualStyleBackColor = false;
            refreshArchivesButton.Click += RefreshArchivesButton_Click;
            // 
            // openOutputFolderButton
            // 
            openOutputFolderButton.BackColor = Color.FromArgb(34, 38, 46);
            openOutputFolderButton.Cursor = Cursors.Hand;
            openOutputFolderButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            openOutputFolderButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 58, 68);
            openOutputFolderButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 49, 59);
            openOutputFolderButton.FlatStyle = FlatStyle.Flat;
            openOutputFolderButton.ForeColor = Color.FromArgb(230, 232, 235);
            openOutputFolderButton.Location = new Point(104, 0);
            openOutputFolderButton.Margin = new Padding(0, 0, 8, 0);
            openOutputFolderButton.Name = "openOutputFolderButton";
            openOutputFolderButton.Size = new Size(112, 32);
            openOutputFolderButton.TabIndex = 1;
            openOutputFolderButton.Text = "Open folder";
            openOutputFolderButton.UseVisualStyleBackColor = false;
            openOutputFolderButton.Click += OpenOutputFolderButton_Click;
            // 
            // verifySelectedButton
            // 
            verifySelectedButton.BackColor = Color.FromArgb(76, 154, 255);
            verifySelectedButton.Cursor = Cursors.Hand;
            verifySelectedButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            verifySelectedButton.FlatAppearance.BorderSize = 0;
            verifySelectedButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 126, 224);
            verifySelectedButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(105, 172, 255);
            verifySelectedButton.FlatStyle = FlatStyle.Flat;
            verifySelectedButton.ForeColor = Color.FromArgb(10, 16, 28);
            verifySelectedButton.Location = new Point(224, 0);
            verifySelectedButton.Margin = new Padding(0, 0, 8, 0);
            verifySelectedButton.Name = "verifySelectedButton";
            verifySelectedButton.Size = new Size(130, 32);
            verifySelectedButton.TabIndex = 2;
            verifySelectedButton.Tag = "primary";
            verifySelectedButton.Text = "Verify selected";
            verifySelectedButton.UseVisualStyleBackColor = false;
            verifySelectedButton.Click += VerifySelectedButton_Click;
            // 
            // copySha256Button
            // 
            copySha256Button.BackColor = Color.FromArgb(34, 38, 46);
            copySha256Button.Cursor = Cursors.Hand;
            copySha256Button.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            copySha256Button.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 58, 68);
            copySha256Button.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 49, 59);
            copySha256Button.FlatStyle = FlatStyle.Flat;
            copySha256Button.ForeColor = Color.FromArgb(230, 232, 235);
            copySha256Button.Location = new Point(362, 0);
            copySha256Button.Margin = new Padding(0, 0, 8, 0);
            copySha256Button.Name = "copySha256Button";
            copySha256Button.Size = new Size(124, 32);
            copySha256Button.TabIndex = 3;
            copySha256Button.Text = "Copy SHA-256";
            copySha256Button.UseVisualStyleBackColor = false;
            copySha256Button.Click += CopySha256Button_Click;
            // 
            // archivesFilterTextBox
            // 
            archivesFilterTextBox.BackColor = Color.FromArgb(34, 38, 46);
            archivesFilterTextBox.BorderStyle = BorderStyle.FixedSingle;
            archivesFilterTextBox.ForeColor = Color.FromArgb(230, 232, 235);
            archivesFilterTextBox.Location = new Point(504, 4);
            archivesFilterTextBox.Margin = new Padding(10, 4, 0, 0);
            archivesFilterTextBox.Name = "archivesFilterTextBox";
            archivesFilterTextBox.PlaceholderText = "Filter by URL, case or file name…";
            archivesFilterTextBox.Size = new Size(280, 23);
            archivesFilterTextBox.TabIndex = 4;
            archivesFilterTextBox.TextChanged += ArchivesFilterTextBox_TextChanged;
            // 
            // archivesGrid
            // 
            archivesGrid.AllowUserToAddRows = false;
            archivesGrid.AllowUserToDeleteRows = false;
            archivesGrid.AllowUserToResizeRows = false;
            dataGridViewCellStyle6.BackColor = Color.FromArgb(30, 33, 40);
            dataGridViewCellStyle6.ForeColor = Color.FromArgb(230, 232, 235);
            dataGridViewCellStyle6.SelectionBackColor = Color.FromArgb(38, 79, 140);
            dataGridViewCellStyle6.SelectionForeColor = Color.FromArgb(230, 232, 235);
            archivesGrid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle6;
            archivesGrid.BackgroundColor = Color.FromArgb(26, 29, 35);
            archivesGrid.BorderStyle = BorderStyle.None;
            archivesGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            archivesGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle7.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle7.BackColor = Color.FromArgb(34, 38, 46);
            dataGridViewCellStyle7.ForeColor = Color.FromArgb(154, 163, 175);
            dataGridViewCellStyle7.Padding = new Padding(6, 0, 6, 0);
            dataGridViewCellStyle7.SelectionBackColor = Color.FromArgb(34, 38, 46);
            dataGridViewCellStyle7.SelectionForeColor = Color.FromArgb(154, 163, 175);
            archivesGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle7;
            archivesGrid.ColumnHeadersHeight = 34;
            archivesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            archivesGrid.Columns.AddRange(new DataGridViewColumn[] { arcDateColumn, arcUrlColumn, arcCaseColumn, arcRecordsColumn, arcSizeColumn, arcSignedColumn, arcShaColumn, arcFileColumn });
            dataGridViewCellStyle10.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle10.BackColor = Color.FromArgb(26, 29, 35);
            dataGridViewCellStyle10.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle10.ForeColor = Color.FromArgb(230, 232, 235);
            dataGridViewCellStyle10.Padding = new Padding(6, 0, 6, 0);
            dataGridViewCellStyle10.SelectionBackColor = Color.FromArgb(38, 79, 140);
            dataGridViewCellStyle10.SelectionForeColor = Color.FromArgb(230, 232, 235);
            dataGridViewCellStyle10.WrapMode = DataGridViewTriState.False;
            archivesGrid.DefaultCellStyle = dataGridViewCellStyle10;
            archivesGrid.Dock = DockStyle.Fill;
            archivesGrid.EnableHeadersVisualStyles = false;
            archivesGrid.GridColor = Color.FromArgb(52, 58, 68);
            archivesGrid.Location = new Point(0, 108);
            archivesGrid.Margin = new Padding(0, 0, 0, 8);
            archivesGrid.MultiSelect = false;
            archivesGrid.Name = "archivesGrid";
            archivesGrid.ReadOnly = true;
            archivesGrid.RowHeadersVisible = false;
            archivesGrid.RowTemplate.Height = 28;
            archivesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            archivesGrid.Size = new Size(932, 591);
            archivesGrid.TabIndex = 2;
            archivesGrid.CellDoubleClick += ArchivesGrid_CellDoubleClick;
            // 
            // arcDateColumn
            // 
            arcDateColumn.HeaderText = "Captured (local time)";
            arcDateColumn.MinimumWidth = 40;
            arcDateColumn.Name = "arcDateColumn";
            arcDateColumn.ReadOnly = true;
            arcDateColumn.Width = 150;
            // 
            // arcUrlColumn
            // 
            arcUrlColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            arcUrlColumn.FillWeight = 60F;
            arcUrlColumn.HeaderText = "URL";
            arcUrlColumn.MinimumWidth = 140;
            arcUrlColumn.Name = "arcUrlColumn";
            arcUrlColumn.ReadOnly = true;
            // 
            // arcCaseColumn
            // 
            arcCaseColumn.HeaderText = "Case / reference";
            arcCaseColumn.MinimumWidth = 40;
            arcCaseColumn.Name = "arcCaseColumn";
            arcCaseColumn.ReadOnly = true;
            arcCaseColumn.Width = 130;
            // 
            // arcRecordsColumn
            // 
            dataGridViewCellStyle8.Alignment = DataGridViewContentAlignment.MiddleRight;
            arcRecordsColumn.DefaultCellStyle = dataGridViewCellStyle8;
            arcRecordsColumn.HeaderText = "Records";
            arcRecordsColumn.MinimumWidth = 40;
            arcRecordsColumn.Name = "arcRecordsColumn";
            arcRecordsColumn.ReadOnly = true;
            arcRecordsColumn.Width = 76;
            // 
            // arcSizeColumn
            // 
            dataGridViewCellStyle9.Alignment = DataGridViewContentAlignment.MiddleRight;
            arcSizeColumn.DefaultCellStyle = dataGridViewCellStyle9;
            arcSizeColumn.HeaderText = "Size";
            arcSizeColumn.MinimumWidth = 40;
            arcSizeColumn.Name = "arcSizeColumn";
            arcSizeColumn.ReadOnly = true;
            arcSizeColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            arcSizeColumn.Width = 90;
            // 
            // arcSignedColumn
            // 
            arcSignedColumn.HeaderText = "Signed";
            arcSignedColumn.MinimumWidth = 40;
            arcSignedColumn.Name = "arcSignedColumn";
            arcSignedColumn.ReadOnly = true;
            arcSignedColumn.Width = 66;
            // 
            // arcShaColumn
            // 
            arcShaColumn.HeaderText = "SHA-256";
            arcShaColumn.MinimumWidth = 40;
            arcShaColumn.Name = "arcShaColumn";
            arcShaColumn.ReadOnly = true;
            arcShaColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            arcShaColumn.Width = 150;
            // 
            // arcFileColumn
            // 
            arcFileColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            arcFileColumn.FillWeight = 40F;
            arcFileColumn.HeaderText = "File";
            arcFileColumn.MinimumWidth = 140;
            arcFileColumn.Name = "arcFileColumn";
            arcFileColumn.ReadOnly = true;
            // 
            // archivesCountLabel
            // 
            archivesCountLabel.AutoSize = true;
            archivesCountLabel.ForeColor = Color.FromArgb(154, 163, 175);
            archivesCountLabel.Location = new Point(0, 707);
            archivesCountLabel.Margin = new Padding(0);
            archivesCountLabel.Name = "archivesCountLabel";
            archivesCountLabel.Size = new Size(59, 15);
            archivesCountLabel.TabIndex = 3;
            archivesCountLabel.Text = "0 archives";
            // 
            // verifyPage
            // 
            verifyPage.AllowDrop = true;
            verifyPage.ColumnCount = 1;
            verifyPage.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            verifyPage.Controls.Add(verifyHeaderPanel, 0, 0);
            verifyPage.Controls.Add(verifyInputLayout, 0, 1);
            verifyPage.Controls.Add(verifyStatusLabel, 0, 2);
            verifyPage.Controls.Add(verifyGrid, 0, 3);
            verifyPage.Controls.Add(verifyFooterLayout, 0, 4);
            verifyPage.Dock = DockStyle.Fill;
            verifyPage.Location = new Point(28, 22);
            verifyPage.Margin = new Padding(0);
            verifyPage.Name = "verifyPage";
            verifyPage.RowCount = 5;
            verifyPage.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            verifyPage.RowStyles.Add(new RowStyle());
            verifyPage.RowStyles.Add(new RowStyle());
            verifyPage.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            verifyPage.RowStyles.Add(new RowStyle());
            verifyPage.Size = new Size(932, 722);
            verifyPage.TabIndex = 2;
            verifyPage.Visible = false;
            verifyPage.DragDrop += VerifyDrop_DragDrop;
            verifyPage.DragEnter += VerifyDrop_DragEnter;
            // 
            // verifyHeaderPanel
            // 
            verifyHeaderPanel.Controls.Add(verifyTitleLabel);
            verifyHeaderPanel.Controls.Add(verifySubtitleLabel);
            verifyHeaderPanel.Dock = DockStyle.Fill;
            verifyHeaderPanel.Location = new Point(0, 0);
            verifyHeaderPanel.Margin = new Padding(0);
            verifyHeaderPanel.Name = "verifyHeaderPanel";
            verifyHeaderPanel.Size = new Size(932, 66);
            verifyHeaderPanel.TabIndex = 0;
            // 
            // verifyTitleLabel
            // 
            verifyTitleLabel.AutoSize = true;
            verifyTitleLabel.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Regular, GraphicsUnit.Point, 0);
            verifyTitleLabel.ForeColor = Color.FromArgb(230, 232, 235);
            verifyTitleLabel.Location = new Point(0, 0);
            verifyTitleLabel.Name = "verifyTitleLabel";
            verifyTitleLabel.Size = new Size(187, 31);
            verifyTitleLabel.TabIndex = 0;
            verifyTitleLabel.Text = "Verify an archive";
            // 
            // verifySubtitleLabel
            // 
            verifySubtitleLabel.AutoSize = true;
            verifySubtitleLabel.ForeColor = Color.FromArgb(154, 163, 175);
            verifySubtitleLabel.Location = new Point(2, 38);
            verifySubtitleLabel.Name = "verifySubtitleLabel";
            verifySubtitleLabel.Size = new Size(665, 15);
            verifySubtitleLabel.TabIndex = 1;
            verifySubtitleLabel.Text = "Re-checks file fingerprints, every WARC record, the signature and the audit trail, independently of how the archive was made.";
            // 
            // verifyInputLayout
            // 
            verifyInputLayout.AutoSize = true;
            verifyInputLayout.ColumnCount = 3;
            verifyInputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            verifyInputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            verifyInputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
            verifyInputLayout.Controls.Add(verifyPathTextBox, 0, 0);
            verifyInputLayout.Controls.Add(verifyBrowseButton, 1, 0);
            verifyInputLayout.Controls.Add(verifyRunButton, 2, 0);
            verifyInputLayout.Dock = DockStyle.Fill;
            verifyInputLayout.Location = new Point(0, 66);
            verifyInputLayout.Margin = new Padding(0, 0, 0, 8);
            verifyInputLayout.Name = "verifyInputLayout";
            verifyInputLayout.RowCount = 1;
            verifyInputLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            verifyInputLayout.Size = new Size(932, 42);
            verifyInputLayout.TabIndex = 1;
            // 
            // verifyPathTextBox
            // 
            verifyPathTextBox.AllowDrop = true;
            verifyPathTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            verifyPathTextBox.BackColor = Color.FromArgb(34, 38, 46);
            verifyPathTextBox.BorderStyle = BorderStyle.FixedSingle;
            verifyPathTextBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            verifyPathTextBox.ForeColor = Color.FromArgb(230, 232, 235);
            verifyPathTextBox.Location = new Point(0, 8);
            verifyPathTextBox.Margin = new Padding(0, 0, 10, 0);
            verifyPathTextBox.Name = "verifyPathTextBox";
            verifyPathTextBox.PlaceholderText = "Drop a .warc, .warc.gz or .manifest.json file here, or click Browse…";
            verifyPathTextBox.Size = new Size(652, 25);
            verifyPathTextBox.TabIndex = 0;
            toolTip.SetToolTip(verifyPathTextBox, "You can also drag and drop a file onto this page.");
            verifyPathTextBox.DragDrop += VerifyDrop_DragDrop;
            verifyPathTextBox.DragEnter += VerifyDrop_DragEnter;
            // 
            // verifyBrowseButton
            // 
            verifyBrowseButton.BackColor = Color.FromArgb(34, 38, 46);
            verifyBrowseButton.Cursor = Cursors.Hand;
            verifyBrowseButton.Dock = DockStyle.Fill;
            verifyBrowseButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            verifyBrowseButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 58, 68);
            verifyBrowseButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 49, 59);
            verifyBrowseButton.FlatStyle = FlatStyle.Flat;
            verifyBrowseButton.ForeColor = Color.FromArgb(230, 232, 235);
            verifyBrowseButton.Location = new Point(662, 0);
            verifyBrowseButton.Margin = new Padding(0, 0, 10, 0);
            verifyBrowseButton.Name = "verifyBrowseButton";
            verifyBrowseButton.Size = new Size(100, 42);
            verifyBrowseButton.TabIndex = 1;
            verifyBrowseButton.Text = "Browse…";
            verifyBrowseButton.UseVisualStyleBackColor = false;
            verifyBrowseButton.Click += VerifyBrowseButton_Click;
            // 
            // verifyRunButton
            // 
            verifyRunButton.BackColor = Color.FromArgb(76, 154, 255);
            verifyRunButton.Cursor = Cursors.Hand;
            verifyRunButton.Dock = DockStyle.Fill;
            verifyRunButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            verifyRunButton.FlatAppearance.BorderSize = 0;
            verifyRunButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 126, 224);
            verifyRunButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(105, 172, 255);
            verifyRunButton.FlatStyle = FlatStyle.Flat;
            verifyRunButton.ForeColor = Color.FromArgb(10, 16, 28);
            verifyRunButton.Location = new Point(772, 0);
            verifyRunButton.Margin = new Padding(0);
            verifyRunButton.Name = "verifyRunButton";
            verifyRunButton.Size = new Size(160, 42);
            verifyRunButton.TabIndex = 2;
            verifyRunButton.Tag = "primary";
            verifyRunButton.Text = "Run verification";
            verifyRunButton.UseVisualStyleBackColor = false;
            verifyRunButton.Click += VerifyRunButton_Click;
            // 
            // verifyStatusLabel
            // 
            verifyStatusLabel.AutoSize = true;
            verifyStatusLabel.ForeColor = Color.FromArgb(154, 163, 175);
            verifyStatusLabel.Location = new Point(0, 116);
            verifyStatusLabel.Margin = new Padding(0, 0, 0, 8);
            verifyStatusLabel.Name = "verifyStatusLabel";
            verifyStatusLabel.Size = new Size(115, 15);
            verifyStatusLabel.TabIndex = 2;
            verifyStatusLabel.Text = "Nothing verified yet.";
            // 
            // verifyGrid
            // 
            verifyGrid.AllowDrop = true;
            verifyGrid.AllowUserToAddRows = false;
            verifyGrid.AllowUserToDeleteRows = false;
            verifyGrid.AllowUserToResizeRows = false;
            dataGridViewCellStyle11.BackColor = Color.FromArgb(30, 33, 40);
            dataGridViewCellStyle11.ForeColor = Color.FromArgb(230, 232, 235);
            dataGridViewCellStyle11.SelectionBackColor = Color.FromArgb(38, 79, 140);
            dataGridViewCellStyle11.SelectionForeColor = Color.FromArgb(230, 232, 235);
            verifyGrid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle11;
            verifyGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            verifyGrid.BackgroundColor = Color.FromArgb(26, 29, 35);
            verifyGrid.BorderStyle = BorderStyle.None;
            verifyGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            verifyGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle12.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle12.BackColor = Color.FromArgb(34, 38, 46);
            dataGridViewCellStyle12.ForeColor = Color.FromArgb(154, 163, 175);
            dataGridViewCellStyle12.Padding = new Padding(6, 0, 6, 0);
            dataGridViewCellStyle12.SelectionBackColor = Color.FromArgb(34, 38, 46);
            dataGridViewCellStyle12.SelectionForeColor = Color.FromArgb(154, 163, 175);
            verifyGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle12;
            verifyGrid.ColumnHeadersHeight = 34;
            verifyGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            verifyGrid.Columns.AddRange(new DataGridViewColumn[] { vfStatusColumn, vfCheckColumn, vfDetailColumn });
            dataGridViewCellStyle14.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle14.BackColor = Color.FromArgb(26, 29, 35);
            dataGridViewCellStyle14.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle14.ForeColor = Color.FromArgb(230, 232, 235);
            dataGridViewCellStyle14.Padding = new Padding(6, 0, 6, 0);
            dataGridViewCellStyle14.SelectionBackColor = Color.FromArgb(38, 79, 140);
            dataGridViewCellStyle14.SelectionForeColor = Color.FromArgb(230, 232, 235);
            dataGridViewCellStyle14.WrapMode = DataGridViewTriState.False;
            verifyGrid.DefaultCellStyle = dataGridViewCellStyle14;
            verifyGrid.Dock = DockStyle.Fill;
            verifyGrid.EnableHeadersVisualStyles = false;
            verifyGrid.GridColor = Color.FromArgb(52, 58, 68);
            verifyGrid.Location = new Point(0, 139);
            verifyGrid.Margin = new Padding(0, 0, 0, 10);
            verifyGrid.MultiSelect = false;
            verifyGrid.Name = "verifyGrid";
            verifyGrid.ReadOnly = true;
            verifyGrid.RowHeadersVisible = false;
            verifyGrid.RowTemplate.Height = 28;
            verifyGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            verifyGrid.Size = new Size(932, 541);
            verifyGrid.TabIndex = 3;
            verifyGrid.DragDrop += VerifyDrop_DragDrop;
            verifyGrid.DragEnter += VerifyDrop_DragEnter;
            // 
            // vfStatusColumn
            // 
            vfStatusColumn.HeaderText = "Result";
            vfStatusColumn.MinimumWidth = 40;
            vfStatusColumn.Name = "vfStatusColumn";
            vfStatusColumn.ReadOnly = true;
            vfStatusColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            vfStatusColumn.Width = 96;
            // 
            // vfCheckColumn
            // 
            vfCheckColumn.HeaderText = "Check";
            vfCheckColumn.MinimumWidth = 40;
            vfCheckColumn.Name = "vfCheckColumn";
            vfCheckColumn.ReadOnly = true;
            vfCheckColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            vfCheckColumn.Width = 270;
            // 
            // vfDetailColumn
            // 
            vfDetailColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewCellStyle13.Padding = new Padding(6);
            dataGridViewCellStyle13.WrapMode = DataGridViewTriState.True;
            vfDetailColumn.DefaultCellStyle = dataGridViewCellStyle13;
            vfDetailColumn.HeaderText = "Details";
            vfDetailColumn.MinimumWidth = 140;
            vfDetailColumn.Name = "vfDetailColumn";
            vfDetailColumn.ReadOnly = true;
            vfDetailColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // verifyFooterLayout
            // 
            verifyFooterLayout.AutoSize = true;
            verifyFooterLayout.ColumnCount = 2;
            verifyFooterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            verifyFooterLayout.ColumnStyles.Add(new ColumnStyle());
            verifyFooterLayout.Controls.Add(verifyVerdictLabel, 0, 0);
            verifyFooterLayout.Controls.Add(verifyExportButton, 1, 0);
            verifyFooterLayout.Dock = DockStyle.Fill;
            verifyFooterLayout.Location = new Point(0, 690);
            verifyFooterLayout.Margin = new Padding(0);
            verifyFooterLayout.Name = "verifyFooterLayout";
            verifyFooterLayout.RowCount = 1;
            verifyFooterLayout.RowStyles.Add(new RowStyle());
            verifyFooterLayout.Size = new Size(932, 32);
            verifyFooterLayout.TabIndex = 4;
            // 
            // verifyVerdictLabel
            // 
            verifyVerdictLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            verifyVerdictLabel.AutoEllipsis = true;
            verifyVerdictLabel.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            verifyVerdictLabel.ForeColor = Color.FromArgb(154, 163, 175);
            verifyVerdictLabel.Location = new Point(0, 2);
            verifyVerdictLabel.Margin = new Padding(0, 0, 12, 0);
            verifyVerdictLabel.Name = "verifyVerdictLabel";
            verifyVerdictLabel.Size = new Size(780, 28);
            verifyVerdictLabel.TabIndex = 0;
            verifyVerdictLabel.Text = "No verification run yet.";
            verifyVerdictLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // verifyExportButton
            // 
            verifyExportButton.BackColor = Color.FromArgb(34, 38, 46);
            verifyExportButton.Cursor = Cursors.Hand;
            verifyExportButton.Enabled = false;
            verifyExportButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            verifyExportButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 58, 68);
            verifyExportButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 49, 59);
            verifyExportButton.FlatStyle = FlatStyle.Flat;
            verifyExportButton.ForeColor = Color.FromArgb(230, 232, 235);
            verifyExportButton.Location = new Point(792, 0);
            verifyExportButton.Margin = new Padding(0);
            verifyExportButton.Name = "verifyExportButton";
            verifyExportButton.Size = new Size(140, 32);
            verifyExportButton.TabIndex = 1;
            verifyExportButton.Text = "Export report…";
            verifyExportButton.UseVisualStyleBackColor = false;
            verifyExportButton.Click += VerifyExportButton_Click;
            // 
            // auditPage
            // 
            auditPage.ColumnCount = 1;
            auditPage.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            auditPage.Controls.Add(auditHeaderPanel, 0, 0);
            auditPage.Controls.Add(auditToolbar, 0, 1);
            auditPage.Controls.Add(auditGrid, 0, 2);
            auditPage.Controls.Add(auditStatusLabel, 0, 3);
            auditPage.Dock = DockStyle.Fill;
            auditPage.Location = new Point(28, 22);
            auditPage.Margin = new Padding(0);
            auditPage.Name = "auditPage";
            auditPage.RowCount = 4;
            auditPage.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            auditPage.RowStyles.Add(new RowStyle());
            auditPage.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            auditPage.RowStyles.Add(new RowStyle());
            auditPage.Size = new Size(932, 722);
            auditPage.TabIndex = 3;
            auditPage.Visible = false;
            // 
            // auditHeaderPanel
            // 
            auditHeaderPanel.Controls.Add(auditTitleLabel);
            auditHeaderPanel.Controls.Add(auditSubtitleLabel);
            auditHeaderPanel.Dock = DockStyle.Fill;
            auditHeaderPanel.Location = new Point(0, 0);
            auditHeaderPanel.Margin = new Padding(0);
            auditHeaderPanel.Name = "auditHeaderPanel";
            auditHeaderPanel.Size = new Size(932, 66);
            auditHeaderPanel.TabIndex = 0;
            // 
            // auditTitleLabel
            // 
            auditTitleLabel.AutoSize = true;
            auditTitleLabel.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Regular, GraphicsUnit.Point, 0);
            auditTitleLabel.ForeColor = Color.FromArgb(230, 232, 235);
            auditTitleLabel.Location = new Point(0, 0);
            auditTitleLabel.Name = "auditTitleLabel";
            auditTitleLabel.Size = new Size(110, 31);
            auditTitleLabel.TabIndex = 0;
            auditTitleLabel.Text = "Audit log";
            // 
            // auditSubtitleLabel
            // 
            auditSubtitleLabel.AutoSize = true;
            auditSubtitleLabel.ForeColor = Color.FromArgb(154, 163, 175);
            auditSubtitleLabel.Location = new Point(2, 38);
            auditSubtitleLabel.Name = "auditSubtitleLabel";
            auditSubtitleLabel.Size = new Size(519, 15);
            auditSubtitleLabel.TabIndex = 1;
            auditSubtitleLabel.Text = "A tamper-evident record of what this app did. Every entry contains the hash of the entry before it.";
            // 
            // auditToolbar
            // 
            auditToolbar.AutoSize = true;
            auditToolbar.Controls.Add(auditRefreshButton);
            auditToolbar.Controls.Add(auditVerifyButton);
            auditToolbar.Controls.Add(auditCopyHeadButton);
            auditToolbar.Controls.Add(auditOpenButton);
            auditToolbar.Dock = DockStyle.Fill;
            auditToolbar.Location = new Point(0, 66);
            auditToolbar.Margin = new Padding(0, 0, 0, 10);
            auditToolbar.Name = "auditToolbar";
            auditToolbar.Size = new Size(932, 32);
            auditToolbar.TabIndex = 1;
            auditToolbar.WrapContents = false;
            // 
            // auditRefreshButton
            // 
            auditRefreshButton.BackColor = Color.FromArgb(34, 38, 46);
            auditRefreshButton.Cursor = Cursors.Hand;
            auditRefreshButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            auditRefreshButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 58, 68);
            auditRefreshButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 49, 59);
            auditRefreshButton.FlatStyle = FlatStyle.Flat;
            auditRefreshButton.ForeColor = Color.FromArgb(230, 232, 235);
            auditRefreshButton.Location = new Point(0, 0);
            auditRefreshButton.Margin = new Padding(0, 0, 8, 0);
            auditRefreshButton.Name = "auditRefreshButton";
            auditRefreshButton.Size = new Size(96, 32);
            auditRefreshButton.TabIndex = 0;
            auditRefreshButton.Text = "Refresh";
            auditRefreshButton.UseVisualStyleBackColor = false;
            auditRefreshButton.Click += AuditRefreshButton_Click;
            // 
            // auditVerifyButton
            // 
            auditVerifyButton.BackColor = Color.FromArgb(76, 154, 255);
            auditVerifyButton.Cursor = Cursors.Hand;
            auditVerifyButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            auditVerifyButton.FlatAppearance.BorderSize = 0;
            auditVerifyButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 126, 224);
            auditVerifyButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(105, 172, 255);
            auditVerifyButton.FlatStyle = FlatStyle.Flat;
            auditVerifyButton.ForeColor = Color.FromArgb(10, 16, 28);
            auditVerifyButton.Location = new Point(104, 0);
            auditVerifyButton.Margin = new Padding(0, 0, 8, 0);
            auditVerifyButton.Name = "auditVerifyButton";
            auditVerifyButton.Size = new Size(120, 32);
            auditVerifyButton.TabIndex = 1;
            auditVerifyButton.Tag = "primary";
            auditVerifyButton.Text = "Verify chain";
            auditVerifyButton.UseVisualStyleBackColor = false;
            auditVerifyButton.Click += AuditVerifyButton_Click;
            // 
            // auditCopyHeadButton
            // 
            auditCopyHeadButton.BackColor = Color.FromArgb(34, 38, 46);
            auditCopyHeadButton.Cursor = Cursors.Hand;
            auditCopyHeadButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            auditCopyHeadButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 58, 68);
            auditCopyHeadButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 49, 59);
            auditCopyHeadButton.FlatStyle = FlatStyle.Flat;
            auditCopyHeadButton.ForeColor = Color.FromArgb(230, 232, 235);
            auditCopyHeadButton.Location = new Point(232, 0);
            auditCopyHeadButton.Margin = new Padding(0, 0, 8, 0);
            auditCopyHeadButton.Name = "auditCopyHeadButton";
            auditCopyHeadButton.Size = new Size(132, 32);
            auditCopyHeadButton.TabIndex = 2;
            auditCopyHeadButton.Text = "Copy head hash";
            toolTip.SetToolTip(auditCopyHeadButton, "Copy the hash of the newest audit entry. Sending it to a third party (e-mail, notary) anchors the whole history at this moment.");
            auditCopyHeadButton.UseVisualStyleBackColor = false;
            auditCopyHeadButton.Click += AuditCopyHeadButton_Click;
            // 
            // auditOpenButton
            // 
            auditOpenButton.BackColor = Color.FromArgb(34, 38, 46);
            auditOpenButton.Cursor = Cursors.Hand;
            auditOpenButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            auditOpenButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 58, 68);
            auditOpenButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 49, 59);
            auditOpenButton.FlatStyle = FlatStyle.Flat;
            auditOpenButton.ForeColor = Color.FromArgb(230, 232, 235);
            auditOpenButton.Location = new Point(372, 0);
            auditOpenButton.Margin = new Padding(0);
            auditOpenButton.Name = "auditOpenButton";
            auditOpenButton.Size = new Size(100, 32);
            auditOpenButton.TabIndex = 3;
            auditOpenButton.Text = "Show file";
            auditOpenButton.UseVisualStyleBackColor = false;
            auditOpenButton.Click += AuditOpenButton_Click;
            // 
            // auditGrid
            // 
            auditGrid.AllowUserToAddRows = false;
            auditGrid.AllowUserToDeleteRows = false;
            auditGrid.AllowUserToResizeRows = false;
            dataGridViewCellStyle15.BackColor = Color.FromArgb(30, 33, 40);
            dataGridViewCellStyle15.ForeColor = Color.FromArgb(230, 232, 235);
            dataGridViewCellStyle15.SelectionBackColor = Color.FromArgb(38, 79, 140);
            dataGridViewCellStyle15.SelectionForeColor = Color.FromArgb(230, 232, 235);
            auditGrid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle15;
            auditGrid.BackgroundColor = Color.FromArgb(26, 29, 35);
            auditGrid.BorderStyle = BorderStyle.None;
            auditGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            auditGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle16.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle16.BackColor = Color.FromArgb(34, 38, 46);
            dataGridViewCellStyle16.ForeColor = Color.FromArgb(154, 163, 175);
            dataGridViewCellStyle16.Padding = new Padding(6, 0, 6, 0);
            dataGridViewCellStyle16.SelectionBackColor = Color.FromArgb(34, 38, 46);
            dataGridViewCellStyle16.SelectionForeColor = Color.FromArgb(154, 163, 175);
            auditGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle16;
            auditGrid.ColumnHeadersHeight = 34;
            auditGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            auditGrid.Columns.AddRange(new DataGridViewColumn[] { audSeqColumn, audTimeColumn, audEventColumn, audDetailsColumn, audHashColumn });
            dataGridViewCellStyle19.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle19.BackColor = Color.FromArgb(26, 29, 35);
            dataGridViewCellStyle19.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle19.ForeColor = Color.FromArgb(230, 232, 235);
            dataGridViewCellStyle19.Padding = new Padding(6, 0, 6, 0);
            dataGridViewCellStyle19.SelectionBackColor = Color.FromArgb(38, 79, 140);
            dataGridViewCellStyle19.SelectionForeColor = Color.FromArgb(230, 232, 235);
            dataGridViewCellStyle19.WrapMode = DataGridViewTriState.False;
            auditGrid.DefaultCellStyle = dataGridViewCellStyle19;
            auditGrid.Dock = DockStyle.Fill;
            auditGrid.EnableHeadersVisualStyles = false;
            auditGrid.GridColor = Color.FromArgb(52, 58, 68);
            auditGrid.Location = new Point(0, 108);
            auditGrid.Margin = new Padding(0, 0, 0, 8);
            auditGrid.MultiSelect = false;
            auditGrid.Name = "auditGrid";
            auditGrid.ReadOnly = true;
            auditGrid.RowHeadersVisible = false;
            auditGrid.RowTemplate.Height = 28;
            auditGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            auditGrid.Size = new Size(932, 591);
            auditGrid.TabIndex = 2;
            // 
            // audSeqColumn
            // 
            dataGridViewCellStyle17.Alignment = DataGridViewContentAlignment.MiddleRight;
            audSeqColumn.DefaultCellStyle = dataGridViewCellStyle17;
            audSeqColumn.HeaderText = "#";
            audSeqColumn.MinimumWidth = 40;
            audSeqColumn.Name = "audSeqColumn";
            audSeqColumn.ReadOnly = true;
            audSeqColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            audSeqColumn.Width = 60;
            // 
            // audTimeColumn
            // 
            audTimeColumn.HeaderText = "Local time";
            audTimeColumn.MinimumWidth = 40;
            audTimeColumn.Name = "audTimeColumn";
            audTimeColumn.ReadOnly = true;
            audTimeColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            audTimeColumn.Width = 160;
            // 
            // audEventColumn
            // 
            audEventColumn.HeaderText = "Event";
            audEventColumn.MinimumWidth = 40;
            audEventColumn.Name = "audEventColumn";
            audEventColumn.ReadOnly = true;
            audEventColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            audEventColumn.Width = 150;
            // 
            // audDetailsColumn
            // 
            audDetailsColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridViewCellStyle18.WrapMode = DataGridViewTriState.False;
            audDetailsColumn.DefaultCellStyle = dataGridViewCellStyle18;
            audDetailsColumn.HeaderText = "Details";
            audDetailsColumn.MinimumWidth = 140;
            audDetailsColumn.Name = "audDetailsColumn";
            audDetailsColumn.ReadOnly = true;
            audDetailsColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // audHashColumn
            // 
            audHashColumn.HeaderText = "Entry hash";
            audHashColumn.MinimumWidth = 40;
            audHashColumn.Name = "audHashColumn";
            audHashColumn.ReadOnly = true;
            audHashColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            audHashColumn.Width = 130;
            // 
            // auditStatusLabel
            // 
            auditStatusLabel.AutoSize = true;
            auditStatusLabel.ForeColor = Color.FromArgb(154, 163, 175);
            auditStatusLabel.Location = new Point(0, 707);
            auditStatusLabel.Margin = new Padding(0);
            auditStatusLabel.Name = "auditStatusLabel";
            auditStatusLabel.Size = new Size(123, 15);
            auditStatusLabel.TabIndex = 3;
            auditStatusLabel.Text = "Chain not verified yet.";
            // 
            // settingsPage
            // 
            settingsPage.ColumnCount = 1;
            settingsPage.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            settingsPage.Controls.Add(settingsHeaderPanel, 0, 0);
            settingsPage.Controls.Add(configPathLabel, 0, 1);
            settingsPage.Controls.Add(settingsTextBox, 0, 2);
            settingsPage.Controls.Add(settingsToolbar, 0, 3);
            settingsPage.Controls.Add(settingsNoteLabel, 0, 4);
            settingsPage.Dock = DockStyle.Fill;
            settingsPage.Location = new Point(28, 22);
            settingsPage.Margin = new Padding(0);
            settingsPage.Name = "settingsPage";
            settingsPage.RowCount = 5;
            settingsPage.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            settingsPage.RowStyles.Add(new RowStyle());
            settingsPage.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            settingsPage.RowStyles.Add(new RowStyle());
            settingsPage.RowStyles.Add(new RowStyle());
            settingsPage.Size = new Size(932, 722);
            settingsPage.TabIndex = 4;
            settingsPage.Visible = false;
            // 
            // settingsHeaderPanel
            // 
            settingsHeaderPanel.Controls.Add(settingsTitleLabel);
            settingsHeaderPanel.Controls.Add(settingsSubtitleLabel);
            settingsHeaderPanel.Dock = DockStyle.Fill;
            settingsHeaderPanel.Location = new Point(0, 0);
            settingsHeaderPanel.Margin = new Padding(0);
            settingsHeaderPanel.Name = "settingsHeaderPanel";
            settingsHeaderPanel.Size = new Size(932, 66);
            settingsHeaderPanel.TabIndex = 0;
            // 
            // settingsTitleLabel
            // 
            settingsTitleLabel.AutoSize = true;
            settingsTitleLabel.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Regular, GraphicsUnit.Point, 0);
            settingsTitleLabel.ForeColor = Color.FromArgb(230, 232, 235);
            settingsTitleLabel.Location = new Point(0, 0);
            settingsTitleLabel.Name = "settingsTitleLabel";
            settingsTitleLabel.Size = new Size(98, 31);
            settingsTitleLabel.TabIndex = 0;
            settingsTitleLabel.Text = "Settings";
            // 
            // settingsSubtitleLabel
            // 
            settingsSubtitleLabel.AutoSize = true;
            settingsSubtitleLabel.ForeColor = Color.FromArgb(154, 163, 175);
            settingsSubtitleLabel.Location = new Point(2, 38);
            settingsSubtitleLabel.Name = "settingsSubtitleLabel";
            settingsSubtitleLabel.Size = new Size(444, 15);
            settingsSubtitleLabel.TabIndex = 1;
            settingsSubtitleLabel.Text = "Every setting lives in config.json. Edit it with any text editor, save, then click Reload.";
            // 
            // configPathLabel
            // 
            configPathLabel.AutoSize = true;
            configPathLabel.ForeColor = Color.FromArgb(154, 163, 175);
            configPathLabel.Location = new Point(0, 66);
            configPathLabel.Margin = new Padding(0, 0, 0, 8);
            configPathLabel.Name = "configPathLabel";
            configPathLabel.Size = new Size(103, 15);
            configPathLabel.TabIndex = 1;
            configPathLabel.Text = "Configuration file:";
            // 
            // settingsTextBox
            // 
            settingsTextBox.BackColor = Color.FromArgb(34, 38, 46);
            settingsTextBox.BorderStyle = BorderStyle.FixedSingle;
            settingsTextBox.Dock = DockStyle.Fill;
            settingsTextBox.Font = new Font("Consolas", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
            settingsTextBox.ForeColor = Color.FromArgb(230, 232, 235);
            settingsTextBox.Location = new Point(0, 89);
            settingsTextBox.Margin = new Padding(0, 0, 0, 10);
            settingsTextBox.Multiline = true;
            settingsTextBox.Name = "settingsTextBox";
            settingsTextBox.ReadOnly = true;
            settingsTextBox.ScrollBars = ScrollBars.Both;
            settingsTextBox.Size = new Size(932, 568);
            settingsTextBox.TabIndex = 2;
            settingsTextBox.WordWrap = false;
            // 
            // settingsToolbar
            // 
            settingsToolbar.AutoSize = true;
            settingsToolbar.Controls.Add(openConfigButton);
            settingsToolbar.Controls.Add(reloadConfigButton);
            settingsToolbar.Controls.Add(changeOutputButton);
            settingsToolbar.Dock = DockStyle.Fill;
            settingsToolbar.Location = new Point(0, 667);
            settingsToolbar.Margin = new Padding(0, 0, 0, 8);
            settingsToolbar.Name = "settingsToolbar";
            settingsToolbar.Size = new Size(932, 32);
            settingsToolbar.TabIndex = 3;
            settingsToolbar.WrapContents = false;
            // 
            // openConfigButton
            // 
            openConfigButton.BackColor = Color.FromArgb(76, 154, 255);
            openConfigButton.Cursor = Cursors.Hand;
            openConfigButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            openConfigButton.FlatAppearance.BorderSize = 0;
            openConfigButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 126, 224);
            openConfigButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(105, 172, 255);
            openConfigButton.FlatStyle = FlatStyle.Flat;
            openConfigButton.ForeColor = Color.FromArgb(10, 16, 28);
            openConfigButton.Location = new Point(0, 0);
            openConfigButton.Margin = new Padding(0, 0, 8, 0);
            openConfigButton.Name = "openConfigButton";
            openConfigButton.Size = new Size(140, 32);
            openConfigButton.TabIndex = 0;
            openConfigButton.Tag = "primary";
            openConfigButton.Text = "Open config.json";
            openConfigButton.UseVisualStyleBackColor = false;
            openConfigButton.Click += OpenConfigButton_Click;
            // 
            // reloadConfigButton
            // 
            reloadConfigButton.BackColor = Color.FromArgb(34, 38, 46);
            reloadConfigButton.Cursor = Cursors.Hand;
            reloadConfigButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            reloadConfigButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 58, 68);
            reloadConfigButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 49, 59);
            reloadConfigButton.FlatStyle = FlatStyle.Flat;
            reloadConfigButton.ForeColor = Color.FromArgb(230, 232, 235);
            reloadConfigButton.Location = new Point(148, 0);
            reloadConfigButton.Margin = new Padding(0, 0, 8, 0);
            reloadConfigButton.Name = "reloadConfigButton";
            reloadConfigButton.Size = new Size(100, 32);
            reloadConfigButton.TabIndex = 1;
            reloadConfigButton.Text = "Reload";
            reloadConfigButton.UseVisualStyleBackColor = false;
            reloadConfigButton.Click += ReloadConfigButton_Click;
            // 
            // changeOutputButton
            // 
            changeOutputButton.BackColor = Color.FromArgb(34, 38, 46);
            changeOutputButton.Cursor = Cursors.Hand;
            changeOutputButton.FlatAppearance.BorderColor = Color.FromArgb(52, 58, 68);
            changeOutputButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 58, 68);
            changeOutputButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 49, 59);
            changeOutputButton.FlatStyle = FlatStyle.Flat;
            changeOutputButton.ForeColor = Color.FromArgb(230, 232, 235);
            changeOutputButton.Location = new Point(256, 0);
            changeOutputButton.Margin = new Padding(0);
            changeOutputButton.Name = "changeOutputButton";
            changeOutputButton.Size = new Size(180, 32);
            changeOutputButton.TabIndex = 2;
            changeOutputButton.Text = "Change output folder…";
            changeOutputButton.UseVisualStyleBackColor = false;
            changeOutputButton.Click += ChangeOutputButton_Click;
            // 
            // settingsNoteLabel
            // 
            settingsNoteLabel.AutoSize = true;
            settingsNoteLabel.ForeColor = Color.FromArgb(154, 163, 175);
            settingsNoteLabel.Location = new Point(0, 707);
            settingsNoteLabel.Margin = new Padding(0);
            settingsNoteLabel.Name = "settingsNoteLabel";
            settingsNoteLabel.Size = new Size(0, 15);
            settingsNoteLabel.TabIndex = 4;
            // 
            // statusStrip
            // 
            statusStrip.BackColor = Color.FromArgb(26, 29, 35);
            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, chainStatusLabel, outputStatusLabel });
            statusStrip.Location = new Point(0, 758);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(1200, 22);
            statusStrip.SizingGrip = false;
            statusStrip.TabIndex = 1;
            // 
            // statusLabel
            // 
            statusLabel.ForeColor = Color.FromArgb(154, 163, 175);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(1018, 17);
            statusLabel.Spring = true;
            statusLabel.Text = "Ready";
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // chainStatusLabel
            // 
            chainStatusLabel.ForeColor = Color.FromArgb(154, 163, 175);
            chainStatusLabel.Name = "chainStatusLabel";
            chainStatusLabel.Padding = new Padding(0, 0, 18, 0);
            chainStatusLabel.Size = new Size(104, 17);
            chainStatusLabel.Text = "Audit chain: —";
            // 
            // outputStatusLabel
            // 
            outputStatusLabel.ForeColor = Color.FromArgb(154, 163, 175);
            outputStatusLabel.Name = "outputStatusLabel";
            outputStatusLabel.Size = new Size(63, 17);
            outputStatusLabel.Text = "Output: —";
            // 
            // toolTip
            // 
            toolTip.AutoPopDelay = 15000;
            toolTip.InitialDelay = 450;
            toolTip.OwnerDraw = true;
            toolTip.ReshowDelay = 100;
            toolTip.Draw += ToolTip_Draw;
            toolTip.Popup += ToolTip_Popup;
            // 
            // folderBrowserDialog
            // 
            folderBrowserDialog.Description = "Choose the folder that will receive archives, manifests and the audit log.";
            folderBrowserDialog.UseDescriptionForTitle = true;
            // 
            // openFileDialog
            // 
            openFileDialog.Filter = "WARC archives and manifests|*.warc;*.warc.gz;*.manifest.json|All files|*.*";
            openFileDialog.Title = "Choose an archive to verify";
            // 
            // saveFileDialog
            // 
            saveFileDialog.DefaultExt = "txt";
            saveFileDialog.Filter = "Text report|*.txt|All files|*.*";
            saveFileDialog.Title = "Save verification report";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(18, 20, 24);
            ClientSize = new Size(1200, 780);
            Controls.Add(rootLayout);
            Controls.Add(statusStrip);
            ForeColor = Color.FromArgb(230, 232, 235);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(1040, 700);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "WARC Archiver — Web Evidence Capture";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            rootLayout.ResumeLayout(false);
            sidebarPanel.ResumeLayout(false);
            sidebarLayout.ResumeLayout(false);
            brandPanel.ResumeLayout(false);
            brandPanel.PerformLayout();
            contentPanel.ResumeLayout(false);
            capturePage.ResumeLayout(false);
            capturePage.PerformLayout();
            captureHeaderPanel.ResumeLayout(false);
            captureHeaderPanel.PerformLayout();
            urlLayout.ResumeLayout(false);
            urlLayout.PerformLayout();
            caseLayout.ResumeLayout(false);
            caseLayout.PerformLayout();
            optionsLayout.ResumeLayout(false);
            optionsLayout.PerformLayout();
            signLayout.ResumeLayout(false);
            signLayout.PerformLayout();
            progressLayout.ResumeLayout(false);
            progressLayout.PerformLayout();
            ((ISupportInitialize)resultsGrid).EndInit();
            summaryLayout.ResumeLayout(false);
            archivesPage.ResumeLayout(false);
            archivesPage.PerformLayout();
            archivesHeaderPanel.ResumeLayout(false);
            archivesHeaderPanel.PerformLayout();
            archivesToolbar.ResumeLayout(false);
            archivesToolbar.PerformLayout();
            ((ISupportInitialize)archivesGrid).EndInit();
            verifyPage.ResumeLayout(false);
            verifyPage.PerformLayout();
            verifyHeaderPanel.ResumeLayout(false);
            verifyHeaderPanel.PerformLayout();
            verifyInputLayout.ResumeLayout(false);
            verifyInputLayout.PerformLayout();
            ((ISupportInitialize)verifyGrid).EndInit();
            verifyFooterLayout.ResumeLayout(false);
            auditPage.ResumeLayout(false);
            auditPage.PerformLayout();
            auditHeaderPanel.ResumeLayout(false);
            auditHeaderPanel.PerformLayout();
            auditToolbar.ResumeLayout(false);
            ((ISupportInitialize)auditGrid).EndInit();
            settingsPage.ResumeLayout(false);
            settingsPage.PerformLayout();
            settingsHeaderPanel.ResumeLayout(false);
            settingsHeaderPanel.PerformLayout();
            settingsToolbar.ResumeLayout(false);
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        // ------------------------------------------------------------------------------------------
        // Control fields
        // ------------------------------------------------------------------------------------------

        // shell
        private TableLayoutPanel rootLayout;
        private Panel sidebarPanel;
        private TableLayoutPanel sidebarLayout;
        private Panel brandPanel;
        private Label brandTitleLabel;
        private Label brandSubtitleLabel;
        private Button navCaptureButton;
        private Button navArchivesButton;
        private Button navVerifyButton;
        private Button navAuditButton;
        private Button navSettingsButton;
        private Label versionLabel;
        private Panel contentPanel;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripStatusLabel chainStatusLabel;
        private ToolStripStatusLabel outputStatusLabel;
        private ToolTip toolTip;
        private FolderBrowserDialog folderBrowserDialog;
        private OpenFileDialog openFileDialog;
        private SaveFileDialog saveFileDialog;

        // capture page
        private TableLayoutPanel capturePage;
        private Panel captureHeaderPanel;
        private Label captureTitleLabel;
        private Label captureSubtitleLabel;
        private TableLayoutPanel urlLayout;
        private Label urlLabel;
        private TextBox urlTextBox;
        private Button captureButton;
        private Button cancelButton;
        private TableLayoutPanel caseLayout;
        private Label caseRefLabel;
        private Label operatorLabel;
        private Label notesLabel;
        private TextBox caseRefTextBox;
        private TextBox operatorTextBox;
        private TextBox notesTextBox;
        private FlowLayoutPanel optionsLayout;
        private Label optionsLabel;
        private CheckBox scriptsCheckBox;
        private CheckBox stylesheetsCheckBox;
        private CheckBox imagesCheckBox;
        private CheckBox otherAssetsCheckBox;
        private CheckBox thirdPartyCheckBox;
        private TableLayoutPanel signLayout;
        private Label signLabel;
        private ComboBox signComboBox;
        private Button refreshCertsButton;
        private TableLayoutPanel progressLayout;
        private DarkProgressBar captureProgressBar;
        private Label progressLabel;
        private DataGridView resultsGrid;
        private DataGridViewTextBoxColumn resTimeColumn;
        private DataGridViewTextBoxColumn resStatusColumn;
        private DataGridViewTextBoxColumn resTypeColumn;
        private DataGridViewTextBoxColumn resUrlColumn;
        private DataGridViewTextBoxColumn resSizeColumn;
        private DataGridViewTextBoxColumn resShaColumn;
        private DataGridViewTextBoxColumn resMsColumn;
        private DataGridViewTextBoxColumn resNoteColumn;
        private TableLayoutPanel summaryLayout;
        private Label summaryLabel;
        private Button openFolderButton;
        private Button verifyThisButton;
        private Button copyHashButton;

        // archives page
        private TableLayoutPanel archivesPage;
        private Panel archivesHeaderPanel;
        private Label archivesTitleLabel;
        private Label archivesSubtitleLabel;
        private FlowLayoutPanel archivesToolbar;
        private Button refreshArchivesButton;
        private Button openOutputFolderButton;
        private Button verifySelectedButton;
        private Button copySha256Button;
        private TextBox archivesFilterTextBox;
        private DataGridView archivesGrid;
        private DataGridViewTextBoxColumn arcDateColumn;
        private DataGridViewTextBoxColumn arcUrlColumn;
        private DataGridViewTextBoxColumn arcCaseColumn;
        private DataGridViewTextBoxColumn arcRecordsColumn;
        private DataGridViewTextBoxColumn arcSizeColumn;
        private DataGridViewTextBoxColumn arcSignedColumn;
        private DataGridViewTextBoxColumn arcShaColumn;
        private DataGridViewTextBoxColumn arcFileColumn;
        private Label archivesCountLabel;

        // verify page
        private TableLayoutPanel verifyPage;
        private Panel verifyHeaderPanel;
        private Label verifyTitleLabel;
        private Label verifySubtitleLabel;
        private TableLayoutPanel verifyInputLayout;
        private TextBox verifyPathTextBox;
        private Button verifyBrowseButton;
        private Button verifyRunButton;
        private Label verifyStatusLabel;
        private DataGridView verifyGrid;
        private DataGridViewTextBoxColumn vfStatusColumn;
        private DataGridViewTextBoxColumn vfCheckColumn;
        private DataGridViewTextBoxColumn vfDetailColumn;
        private TableLayoutPanel verifyFooterLayout;
        private Label verifyVerdictLabel;
        private Button verifyExportButton;

        // audit page
        private TableLayoutPanel auditPage;
        private Panel auditHeaderPanel;
        private Label auditTitleLabel;
        private Label auditSubtitleLabel;
        private FlowLayoutPanel auditToolbar;
        private Button auditRefreshButton;
        private Button auditVerifyButton;
        private Button auditCopyHeadButton;
        private Button auditOpenButton;
        private DataGridView auditGrid;
        private DataGridViewTextBoxColumn audSeqColumn;
        private DataGridViewTextBoxColumn audTimeColumn;
        private DataGridViewTextBoxColumn audEventColumn;
        private DataGridViewTextBoxColumn audDetailsColumn;
        private DataGridViewTextBoxColumn audHashColumn;
        private Label auditStatusLabel;

        // settings page
        private TableLayoutPanel settingsPage;
        private Panel settingsHeaderPanel;
        private Label settingsTitleLabel;
        private Label settingsSubtitleLabel;
        private Label configPathLabel;
        private TextBox settingsTextBox;
        private FlowLayoutPanel settingsToolbar;
        private Button openConfigButton;
        private Button reloadConfigButton;
        private Button changeOutputButton;
        private Label settingsNoteLabel;
    }
}
