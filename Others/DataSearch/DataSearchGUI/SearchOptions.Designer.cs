namespace DataSearchGUI
{
    partial class SearchOptions
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnOK = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txtMaxNumberOfNodes = new System.Windows.Forms.MaskedTextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtMaxNumberOfGeneration = new System.Windows.Forms.MaskedTextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtMaxNumberOfResults = new System.Windows.Forms.MaskedTextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.cbUseTextOnlySearch = new System.Windows.Forms.CheckBox();
            this.cbDontCompleteGraphs = new System.Windows.Forms.CheckBox();
            this.cbReportNonLeafResults = new System.Windows.Forms.CheckBox();
            this.cbReportSearchGraph = new System.Windows.Forms.CheckBox();
            this.btnCancel = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(254, 12);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(140, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Maximum Number of Nodes:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.txtMaxNumberOfResults);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.txtMaxNumberOfGeneration);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txtMaxNumberOfNodes);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(236, 103);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Engine Limits";
            // 
            // txtMaxNumberOfNodes
            // 
            this.txtMaxNumberOfNodes.Location = new System.Drawing.Point(169, 23);
            this.txtMaxNumberOfNodes.Mask = "00000";
            this.txtMaxNumberOfNodes.Name = "txtMaxNumberOfNodes";
            this.txtMaxNumberOfNodes.Size = new System.Drawing.Size(61, 20);
            this.txtMaxNumberOfNodes.TabIndex = 2;
            this.txtMaxNumberOfNodes.ValidatingType = typeof(int);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 50);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(162, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Maximum number of generations:";
            // 
            // txtMaxNumberOfGeneration
            // 
            this.txtMaxNumberOfGeneration.Location = new System.Drawing.Point(169, 47);
            this.txtMaxNumberOfGeneration.Mask = "00";
            this.txtMaxNumberOfGeneration.Name = "txtMaxNumberOfGeneration";
            this.txtMaxNumberOfGeneration.Size = new System.Drawing.Size(61, 20);
            this.txtMaxNumberOfGeneration.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 74);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(137, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Maximum number of results:";
            // 
            // txtMaxNumberOfResults
            // 
            this.txtMaxNumberOfResults.Location = new System.Drawing.Point(169, 71);
            this.txtMaxNumberOfResults.Mask = "00000";
            this.txtMaxNumberOfResults.Name = "txtMaxNumberOfResults";
            this.txtMaxNumberOfResults.Size = new System.Drawing.Size(61, 20);
            this.txtMaxNumberOfResults.TabIndex = 5;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.cbReportSearchGraph);
            this.groupBox2.Controls.Add(this.cbReportNonLeafResults);
            this.groupBox2.Controls.Add(this.cbDontCompleteGraphs);
            this.groupBox2.Controls.Add(this.cbUseTextOnlySearch);
            this.groupBox2.Location = new System.Drawing.Point(12, 121);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(236, 113);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Engine Options";
            // 
            // cbUseTextOnlySearch
            // 
            this.cbUseTextOnlySearch.AutoSize = true;
            this.cbUseTextOnlySearch.Location = new System.Drawing.Point(6, 19);
            this.cbUseTextOnlySearch.Name = "cbUseTextOnlySearch";
            this.cbUseTextOnlySearch.Size = new System.Drawing.Size(104, 17);
            this.cbUseTextOnlySearch.TabIndex = 0;
            this.cbUseTextOnlySearch.Text = "Text only search";
            this.cbUseTextOnlySearch.UseVisualStyleBackColor = true;
            // 
            // cbDontCompleteGraphs
            // 
            this.cbDontCompleteGraphs.AutoSize = true;
            this.cbDontCompleteGraphs.Location = new System.Drawing.Point(6, 42);
            this.cbDontCompleteGraphs.Name = "cbDontCompleteGraphs";
            this.cbDontCompleteGraphs.Size = new System.Drawing.Size(132, 17);
            this.cbDontCompleteGraphs.TabIndex = 1;
            this.cbDontCompleteGraphs.Text = "Don\'t complete graphs";
            this.cbDontCompleteGraphs.UseVisualStyleBackColor = true;
            // 
            // cbReportNonLeafResults
            // 
            this.cbReportNonLeafResults.AutoSize = true;
            this.cbReportNonLeafResults.Location = new System.Drawing.Point(6, 65);
            this.cbReportNonLeafResults.Name = "cbReportNonLeafResults";
            this.cbReportNonLeafResults.Size = new System.Drawing.Size(138, 17);
            this.cbReportNonLeafResults.TabIndex = 2;
            this.cbReportNonLeafResults.Text = "Report Non Leaf results";
            this.cbReportNonLeafResults.UseVisualStyleBackColor = true;
            // 
            // cbReportSearchGraph
            // 
            this.cbReportSearchGraph.AutoSize = true;
            this.cbReportSearchGraph.Location = new System.Drawing.Point(6, 88);
            this.cbReportSearchGraph.Name = "cbReportSearchGraph";
            this.cbReportSearchGraph.Size = new System.Drawing.Size(102, 17);
            this.cbReportSearchGraph.TabIndex = 3;
            this.cbReportSearchGraph.Text = "Generate Graph";
            this.cbReportSearchGraph.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(254, 38);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // SearchOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(331, 243);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SearchOptions";
            this.ShowInTaskbar = false;
            this.Text = "Change Search Options";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.MaskedTextBox txtMaxNumberOfNodes;
        private System.Windows.Forms.MaskedTextBox txtMaxNumberOfGeneration;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.MaskedTextBox txtMaxNumberOfResults;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox cbUseTextOnlySearch;
        private System.Windows.Forms.CheckBox cbDontCompleteGraphs;
        private System.Windows.Forms.CheckBox cbReportNonLeafResults;
        private System.Windows.Forms.CheckBox cbReportSearchGraph;
        private System.Windows.Forms.Button btnCancel;
    }
}