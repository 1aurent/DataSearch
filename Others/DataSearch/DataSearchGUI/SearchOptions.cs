using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DataSearchEngine.Search;

namespace DataSearchGUI
{
    public partial class SearchOptions : Form
    {
        readonly EngineSearchOptions _options;

        public SearchOptions(EngineSearchOptions options)
        {
            InitializeComponent();

            _options = options;
            txtMaxNumberOfNodes.Text = _options.MaxNumberOfNodes.ToString();
            txtMaxNumberOfResults.Text = _options.MaxNumberOfResults.ToString();
            txtMaxNumberOfGeneration.Text = _options.MaxNumberOfGeneration.ToString();

            cbUseTextOnlySearch.Checked = _options.UseTextOnlySearch;
            cbDontCompleteGraphs.Checked = _options.DontCompleteGraphs;
            cbReportNonLeafResults.Checked = _options.ReportNonLeafResults;
            cbReportSearchGraph.Checked = _options.ReportSearchGraph;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            _options.MaxNumberOfNodes = Convert.ToInt32(txtMaxNumberOfNodes.Text);
            _options.MaxNumberOfResults = Convert.ToInt32(txtMaxNumberOfResults.Text);
            _options.MaxNumberOfGeneration = Convert.ToInt32(txtMaxNumberOfGeneration.Text);

            _options.UseTextOnlySearch = cbUseTextOnlySearch.Checked;
            _options.DontCompleteGraphs = cbDontCompleteGraphs.Checked;
            _options.ReportNonLeafResults = cbReportNonLeafResults.Checked;
            _options.ReportSearchGraph = cbReportSearchGraph.Checked;

            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
            Close();
        }
    }
}
