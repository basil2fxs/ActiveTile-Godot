namespace MysticClue.Chroma.HardwareEmulator;

partial class Main
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        tabControl1 = new TabControl();
        inboundTabPage = new TabPage();
        loadInboundSpecButton = new Button();
        inboundErrorIndicatorLabel = new Label();
        inboundSpecTextBox = new TextBox();
        outboundTabPage = new TabPage();
        outboundErrorIndicatorLabel = new Label();
        loadOutboundSpecButton = new Button();
        outboundSpecTextBox = new TextBox();
        inboundErrorToolTip = new ToolTip(components);
        splitContainerVertical = new SplitContainer();
        gridContainer = new Panel();
        splitContainerHorizontal = new SplitContainer();
        consoleTextBox = new TextBox();
        outboundErrorToolTip = new ToolTip(components);
        openFileDialog = new OpenFileDialog();
        tabControl1.SuspendLayout();
        inboundTabPage.SuspendLayout();
        outboundTabPage.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainerVertical).BeginInit();
        splitContainerVertical.Panel1.SuspendLayout();
        splitContainerVertical.Panel2.SuspendLayout();
        splitContainerVertical.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainerHorizontal).BeginInit();
        splitContainerHorizontal.Panel1.SuspendLayout();
        splitContainerHorizontal.Panel2.SuspendLayout();
        splitContainerHorizontal.SuspendLayout();
        SuspendLayout();
        //
        // tabControl1
        //
        tabControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        tabControl1.Controls.Add(inboundTabPage);
        tabControl1.Controls.Add(outboundTabPage);
        tabControl1.Location = new Point(3, 3);
        tabControl1.Name = "tabControl1";
        tabControl1.SelectedIndex = 0;
        tabControl1.Size = new Size(250, 335);
        tabControl1.TabIndex = 0;
        //
        // inboundTabPage
        //
        inboundTabPage.Controls.Add(loadInboundSpecButton);
        inboundTabPage.Controls.Add(inboundErrorIndicatorLabel);
        inboundTabPage.Controls.Add(inboundSpecTextBox);
        inboundTabPage.Location = new Point(4, 29);
        inboundTabPage.Name = "inboundTabPage";
        inboundTabPage.Padding = new Padding(3);
        inboundTabPage.Size = new Size(242, 302);
        inboundTabPage.TabIndex = 1;
        inboundTabPage.Text = "Inbound";
        inboundTabPage.UseVisualStyleBackColor = true;
        //
        // loadInboundSpecButton
        //
        loadInboundSpecButton.Location = new Point(6, 6);
        loadInboundSpecButton.Name = "loadInboundSpecButton";
        loadInboundSpecButton.Size = new Size(94, 29);
        loadInboundSpecButton.TabIndex = 1;
        loadInboundSpecButton.Text = "Load";
        loadInboundSpecButton.UseVisualStyleBackColor = true;
        loadInboundSpecButton.Click += loadInboundSpecButton_Click;
        //
        // inboundErrorIndicatorLabel
        //
        inboundErrorIndicatorLabel.AutoSize = true;
        inboundErrorIndicatorLabel.BackColor = Color.FromArgb(255, 128, 128);
        inboundErrorIndicatorLabel.Dock = DockStyle.Right;
        inboundErrorIndicatorLabel.Location = new Point(239, 3);
        inboundErrorIndicatorLabel.Name = "inboundErrorIndicatorLabel";
        inboundErrorIndicatorLabel.Size = new Size(0, 20);
        inboundErrorIndicatorLabel.TabIndex = 0;
        //
        // inboundSpecTextBox
        //
        inboundSpecTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        inboundSpecTextBox.Font = new Font("Courier New", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
        inboundSpecTextBox.Location = new Point(6, 41);
        inboundSpecTextBox.Multiline = true;
        inboundSpecTextBox.Name = "inboundSpecTextBox";
        inboundSpecTextBox.ScrollBars = ScrollBars.Both;
        inboundSpecTextBox.Size = new Size(228, 255);
        inboundSpecTextBox.TabIndex = 0;
        inboundSpecTextBox.WordWrap = false;
        inboundSpecTextBox.TextChanged += inboundSpecTextBox_TextChanged;
        //
        // outboundTabPage
        //
        outboundTabPage.Controls.Add(outboundErrorIndicatorLabel);
        outboundTabPage.Controls.Add(loadOutboundSpecButton);
        outboundTabPage.Controls.Add(outboundSpecTextBox);
        outboundTabPage.Location = new Point(4, 29);
        outboundTabPage.Name = "outboundTabPage";
        outboundTabPage.Padding = new Padding(3);
        outboundTabPage.Size = new Size(242, 302);
        outboundTabPage.TabIndex = 0;
        outboundTabPage.Text = "Outbound";
        outboundTabPage.UseVisualStyleBackColor = true;
        //
        // outboundErrorIndicatorLabel
        //
        outboundErrorIndicatorLabel.AutoSize = true;
        outboundErrorIndicatorLabel.BackColor = Color.FromArgb(255, 128, 128);
        outboundErrorIndicatorLabel.Dock = DockStyle.Right;
        outboundErrorIndicatorLabel.Location = new Point(239, 3);
        outboundErrorIndicatorLabel.Name = "outboundErrorIndicatorLabel";
        outboundErrorIndicatorLabel.Size = new Size(0, 20);
        outboundErrorIndicatorLabel.TabIndex = 3;
        //
        // loadOutboundSpecButton
        //
        loadOutboundSpecButton.Location = new Point(7, 6);
        loadOutboundSpecButton.Name = "loadOutboundSpecButton";
        loadOutboundSpecButton.Size = new Size(94, 29);
        loadOutboundSpecButton.TabIndex = 2;
        loadOutboundSpecButton.Text = "Load";
        loadOutboundSpecButton.UseVisualStyleBackColor = true;
        loadOutboundSpecButton.Click += loadOutboundSpecButton_Click;
        //
        // outboundSpecTextBox
        //
        outboundSpecTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        outboundSpecTextBox.Font = new Font("Courier New", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
        outboundSpecTextBox.Location = new Point(7, 41);
        outboundSpecTextBox.Multiline = true;
        outboundSpecTextBox.Name = "outboundSpecTextBox";
        outboundSpecTextBox.ScrollBars = ScrollBars.Both;
        outboundSpecTextBox.Size = new Size(228, 255);
        outboundSpecTextBox.TabIndex = 1;
        outboundSpecTextBox.WordWrap = false;
        outboundSpecTextBox.TextChanged += outboundSpecTextBox_TextChanged;
        //
        // splitContainerVertical
        //
        splitContainerVertical.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        splitContainerVertical.Location = new Point(3, 3);
        splitContainerVertical.Name = "splitContainerVertical";
        //
        // splitContainerVertical.Panel1
        //
        splitContainerVertical.Panel1.Controls.Add(tabControl1);
        //
        // splitContainerVertical.Panel2
        //
        splitContainerVertical.Panel2.Controls.Add(gridContainer);
        splitContainerVertical.Size = new Size(770, 341);
        splitContainerVertical.SplitterDistance = 256;
        splitContainerVertical.TabIndex = 2;
        //
        // gridContainer
        //
        gridContainer.Location = new Point(3, 3);
        gridContainer.Name = "gridContainer";
        gridContainer.Size = new Size(250, 125);
        gridContainer.TabIndex = 0;
        //
        // splitContainerHorizontal
        //
        splitContainerHorizontal.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        splitContainerHorizontal.Location = new Point(12, 12);
        splitContainerHorizontal.Name = "splitContainerHorizontal";
        splitContainerHorizontal.Orientation = Orientation.Horizontal;
        //
        // splitContainerHorizontal.Panel1
        //
        splitContainerHorizontal.Panel1.Controls.Add(splitContainerVertical);
        //
        // splitContainerHorizontal.Panel2
        //
        splitContainerHorizontal.Panel2.Controls.Add(consoleTextBox);
        splitContainerHorizontal.Size = new Size(776, 426);
        splitContainerHorizontal.SplitterDistance = 347;
        splitContainerHorizontal.TabIndex = 1;
        //
        // consoleTextBox
        //
        consoleTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        consoleTextBox.Location = new Point(3, 3);
        consoleTextBox.Multiline = true;
        consoleTextBox.Name = "consoleTextBox";
        consoleTextBox.ScrollBars = ScrollBars.Vertical;
        consoleTextBox.Size = new Size(770, 69);
        consoleTextBox.TabIndex = 0;
        //
        // Main
        //
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 450);
        Controls.Add(splitContainerHorizontal);
        Name = "Main";
        Text = "Cubetown Hardware Emulator";
        tabControl1.ResumeLayout(false);
        inboundTabPage.ResumeLayout(false);
        inboundTabPage.PerformLayout();
        outboundTabPage.ResumeLayout(false);
        outboundTabPage.PerformLayout();
        splitContainerVertical.Panel1.ResumeLayout(false);
        splitContainerVertical.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitContainerVertical).EndInit();
        splitContainerVertical.ResumeLayout(false);
        splitContainerHorizontal.Panel1.ResumeLayout(false);
        splitContainerHorizontal.Panel2.ResumeLayout(false);
        splitContainerHorizontal.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainerHorizontal).EndInit();
        splitContainerHorizontal.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TabControl tabControl1;
    private TabPage outboundTabPage;
    private TabPage inboundTabPage;
    private TextBox inboundSpecTextBox;
    private Label inboundErrorIndicatorLabel;
    private ToolTip inboundErrorToolTip;
    private SplitContainer splitContainerVertical;
    private Panel gridContainer;
    private SplitContainer splitContainerHorizontal;
    private TextBox consoleTextBox;
    private TextBox outboundSpecTextBox;
    private Button loadInboundSpecButton;
    private Button loadOutboundSpecButton;
    private ToolTip outboundErrorToolTip;
    private Label outboundErrorIndicatorLabel;
    private OpenFileDialog openFileDialog;
}
