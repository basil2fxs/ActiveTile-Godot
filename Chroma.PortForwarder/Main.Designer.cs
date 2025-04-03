namespace MysticClue.Chroma.PortForwarder;

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
        localAddressListBox = new ListBox();
        portTextBox = new TextBox();
        localAddressLabel = new Label();
        portLabel = new Label();
        remoteAddressListBox = new ListBox();
        remoteAddressLabel = new Label();
        startButton = new Button();
        stopButton = new Button();
        verticalSplitContainer = new SplitContainer();
        horizontalSplitContainer = new SplitContainer();
        consoleTextBox = new TextBox();
        ((System.ComponentModel.ISupportInitialize)verticalSplitContainer).BeginInit();
        verticalSplitContainer.Panel1.SuspendLayout();
        verticalSplitContainer.Panel2.SuspendLayout();
        verticalSplitContainer.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)horizontalSplitContainer).BeginInit();
        horizontalSplitContainer.Panel1.SuspendLayout();
        horizontalSplitContainer.Panel2.SuspendLayout();
        horizontalSplitContainer.SuspendLayout();
        SuspendLayout();
        // 
        // localAddressListBox
        // 
        localAddressListBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        localAddressListBox.Font = new Font("Segoe UI", 10.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
        localAddressListBox.FormattingEnabled = true;
        localAddressListBox.ItemHeight = 19;
        localAddressListBox.Location = new Point(3, 18);
        localAddressListBox.Margin = new Padding(3, 2, 3, 2);
        localAddressListBox.Name = "localAddressListBox";
        localAddressListBox.Size = new Size(260, 137);
        localAddressListBox.TabIndex = 0;
        // 
        // portTextBox
        // 
        portTextBox.Location = new Point(3, 18);
        portTextBox.Margin = new Padding(3, 2, 3, 2);
        portTextBox.Name = "portTextBox";
        portTextBox.Size = new Size(83, 23);
        portTextBox.TabIndex = 1;
        portTextBox.Text = "2317";
        // 
        // localAddressLabel
        // 
        localAddressLabel.AutoSize = true;
        localAddressLabel.Location = new Point(3, 1);
        localAddressLabel.Name = "localAddressLabel";
        localAddressLabel.Size = new Size(80, 15);
        localAddressLabel.TabIndex = 2;
        localAddressLabel.Text = "Local Address";
        // 
        // portLabel
        // 
        portLabel.AutoSize = true;
        portLabel.Location = new Point(3, 1);
        portLabel.Name = "portLabel";
        portLabel.Size = new Size(29, 15);
        portLabel.TabIndex = 3;
        portLabel.Text = "Port";
        // 
        // remoteAddressListBox
        // 
        remoteAddressListBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        remoteAddressListBox.Font = new Font("Segoe UI", 10.8F, FontStyle.Regular, GraphicsUnit.Point, 0);
        remoteAddressListBox.FormattingEnabled = true;
        remoteAddressListBox.ItemHeight = 19;
        remoteAddressListBox.Location = new Point(90, 18);
        remoteAddressListBox.Margin = new Padding(3, 2, 3, 2);
        remoteAddressListBox.Name = "remoteAddressListBox";
        remoteAddressListBox.Size = new Size(309, 137);
        remoteAddressListBox.TabIndex = 4;
        // 
        // remoteAddressLabel
        // 
        remoteAddressLabel.AutoSize = true;
        remoteAddressLabel.Location = new Point(90, 1);
        remoteAddressLabel.Name = "remoteAddressLabel";
        remoteAddressLabel.Size = new Size(93, 15);
        remoteAddressLabel.TabIndex = 5;
        remoteAddressLabel.Text = "Remote Address";
        // 
        // startButton
        // 
        startButton.Location = new Point(3, 62);
        startButton.Margin = new Padding(3, 2, 3, 2);
        startButton.Name = "startButton";
        startButton.Size = new Size(82, 22);
        startButton.TabIndex = 6;
        startButton.Text = "Start";
        startButton.UseVisualStyleBackColor = true;
        startButton.Click += startButton_Click;
        // 
        // stopButton
        // 
        stopButton.Location = new Point(3, 108);
        stopButton.Margin = new Padding(3, 2, 3, 2);
        stopButton.Name = "stopButton";
        stopButton.Size = new Size(82, 22);
        stopButton.TabIndex = 7;
        stopButton.Text = "Stop";
        stopButton.UseVisualStyleBackColor = true;
        stopButton.Click += stopButton_Click;
        // 
        // verticalSplitContainer
        // 
        verticalSplitContainer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        verticalSplitContainer.Location = new Point(3, 2);
        verticalSplitContainer.Margin = new Padding(3, 2, 3, 2);
        verticalSplitContainer.Name = "verticalSplitContainer";
        // 
        // verticalSplitContainer.Panel1
        // 
        verticalSplitContainer.Panel1.Controls.Add(localAddressLabel);
        verticalSplitContainer.Panel1.Controls.Add(localAddressListBox);
        // 
        // verticalSplitContainer.Panel2
        // 
        verticalSplitContainer.Panel2.Controls.Add(portTextBox);
        verticalSplitContainer.Panel2.Controls.Add(remoteAddressLabel);
        verticalSplitContainer.Panel2.Controls.Add(stopButton);
        verticalSplitContainer.Panel2.Controls.Add(remoteAddressListBox);
        verticalSplitContainer.Panel2.Controls.Add(portLabel);
        verticalSplitContainer.Panel2.Controls.Add(startButton);
        verticalSplitContainer.Size = new Size(670, 153);
        verticalSplitContainer.SplitterDistance = 265;
        verticalSplitContainer.TabIndex = 8;
        // 
        // horizontalSplitContainer
        // 
        horizontalSplitContainer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        horizontalSplitContainer.Location = new Point(12, 12);
        horizontalSplitContainer.Name = "horizontalSplitContainer";
        horizontalSplitContainer.Orientation = Orientation.Horizontal;
        // 
        // horizontalSplitContainer.Panel1
        // 
        horizontalSplitContainer.Panel1.Controls.Add(verticalSplitContainer);
        // 
        // horizontalSplitContainer.Panel2
        // 
        horizontalSplitContainer.Panel2.Controls.Add(consoleTextBox);
        horizontalSplitContainer.Size = new Size(676, 314);
        horizontalSplitContainer.SplitterDistance = 157;
        horizontalSplitContainer.TabIndex = 9;
        // 
        // consoleTextBox
        // 
        consoleTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        consoleTextBox.Location = new Point(3, 3);
        consoleTextBox.Multiline = true;
        consoleTextBox.Name = "consoleTextBox";
        consoleTextBox.ReadOnly = true;
        consoleTextBox.Size = new Size(668, 147);
        consoleTextBox.TabIndex = 0;
        consoleTextBox.WordWrap = false;
        // 
        // Main
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(700, 338);
        Controls.Add(horizontalSplitContainer);
        Margin = new Padding(3, 2, 3, 2);
        Name = "Main";
        Text = "Port Forwarder";
        verticalSplitContainer.Panel1.ResumeLayout(false);
        verticalSplitContainer.Panel1.PerformLayout();
        verticalSplitContainer.Panel2.ResumeLayout(false);
        verticalSplitContainer.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)verticalSplitContainer).EndInit();
        verticalSplitContainer.ResumeLayout(false);
        horizontalSplitContainer.Panel1.ResumeLayout(false);
        horizontalSplitContainer.Panel2.ResumeLayout(false);
        horizontalSplitContainer.Panel2.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)horizontalSplitContainer).EndInit();
        horizontalSplitContainer.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private ListBox localAddressListBox;
    private TextBox portTextBox;
    private Label localAddressLabel;
    private Label portLabel;
    private ListBox remoteAddressListBox;
    private Label remoteAddressLabel;
    private Button startButton;
    private Button stopButton;
    private SplitContainer verticalSplitContainer;
    private SplitContainer horizontalSplitContainer;
    private TextBox consoleTextBox;
}
