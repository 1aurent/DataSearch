using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DataSearchEngine.Search;

namespace DataSearchGUI
{
    public partial class SearchUI : Form
    {
        static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Process Console
        [DllImport("kernel32.dll", SetLastError = true)]
        extern static int AllocConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        extern static int FreeConsole();

        bool _consoleIsVisible = false;
        private void showHideConsoleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            log.InfoFormat("Switching process console (Status={0})",_consoleIsVisible);
            if (_consoleIsVisible) FreeConsole(); else AllocConsole();
            _consoleIsVisible = !_consoleIsVisible;
        }
        #endregion

        readonly Engine _searchEngine;
        readonly EngineSearchOptions _options;

        System.Xml.XmlDocument _lastSearchGraphXml;

        public SearchUI()
        {
            InitializeComponent();

            showLastSearchGraphToolStripMenuItem.Enabled = false;

            _options        = new EngineSearchOptions();
            _searchEngine   = Engine.CreateEngine();

            _searchEngine.OnProgress += new Engine.NotifyProgress(searchEngine_OnProgress);
            _searchEngine.OnStep += new Engine.NotifyStep(searchEngine_OnStep);

            showLastSearchGraphToolStripMenuItem.Enabled = false;
            showDomainRelationGraphToolStripMenuItem.Enabled = false;
        }

        void searchEngine_OnStep(object sender, Engine.StepEventArgs e)
        {
            Invoke((UpdateStatusDlg)UpdateStatus, (object)e);
        }

        void searchEngine_OnProgress(object sender, Engine.ProgressEventArgs e)
        {
            Invoke((UpdateStatusDlg)UpdateStatus, (object)e);
        }

        private void showDomainRelationGraphToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // (new DomainRelGraph(_searchEngine)).ShowDialog(this); - Need Microsoft MSAGL
        }

        private delegate void SearchCompletedDlg(object result);
        private delegate void UpdateStatusDlg   (object result);

        private void UpdateStatus(object e)
        {
            if(e is Engine.StepEventArgs)
            {
                toolStripStatusLabel.Text = ((Engine.StepEventArgs) e).Step;
            }
            else if(e is Engine.ProgressEventArgs)
            {
                toolStripProgressBar.Maximum = ((Engine.ProgressEventArgs)e).Total;
                toolStripProgressBar.Value = ((Engine.ProgressEventArgs)e).Current;
            }
        }

        private void SearchCompleted(object result)
        {
            btnSearch.Enabled = true;
            optionsToolStripMenuItem.Enabled = true;
            toolStripProgressBar.Value = 0;
            if(result is Exception)
            {
                MessageBox.Show("Failed " + result);
                toolStripStatusLabel.Text = "Failed";
                return;
            }
            var results = (EngineSearchResults) result;
            toolStripStatusLabel.Text = string.Format("Completed ({0} reported results)",results.TabularData.Rows.Count);
            _lastSearchGraphXml = results.SearchGraph;
            showLastSearchGraphToolStripMenuItem.Enabled = (_lastSearchGraphXml != null);
            dtaResult.DataSource = results.TabularData;
        }

        private void BackgroundTask(object unused)
        {
            try
            {
                var results = _searchEngine.Search(_options, txtSearchText.Text);
                Invoke((SearchCompletedDlg) SearchCompleted, (object) results);
            }
            catch (Exception ex)
            {
                Invoke((SearchCompletedDlg)SearchCompleted, (object)ex);
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            btnSearch.Enabled = false;
            optionsToolStripMenuItem.Enabled = false;
            dtaResult.DataSource = null;

            System.Threading.ThreadPool.QueueUserWorkItem(BackgroundTask);
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new SearchOptions(_options)).ShowDialog(this);
        }

        private void showLastSearchGraphToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // (new SearchGraph(_lastSearchGraphXml)).ShowDialog(this); - Need Microsoft MSAGL
        }

    }
}
