using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DataSearchEngine.Search;

namespace DataSearchGUI
{
    public partial class DomainRelGraph : Form
    {
        public DomainRelGraph(Engine searchEngine)
        {
            InitializeComponent();

            var graph = new Microsoft.Msagl.Drawing.Graph("Domains");
            var allDoms = new Dictionary<int,string>();
            foreach (var domain in searchEngine.AllDomains)
            {
                var domainId = searchEngine.GetDomainID(domain);
                var nde = graph.AddNode(domain);
                nde.LabelText = string.Format("{0} ({1})",domain,domainId) ;

                nde.Attr.Color = searchEngine.IsDomainLeaf(domainId)?
                                     Microsoft.Msagl.Drawing.Color.DarkBlue:
                                     Microsoft.Msagl.Drawing.Color.DarkGray;

                allDoms.Add(domainId,domain);
            }

            foreach (var domain in allDoms)
            {
                foreach (var link in searchEngine.GetDomainLinks(domain.Key))
                {
                    var rel = graph.AddEdge(domain.Value, allDoms[link.TargetDomain]);
                    rel.LabelText = link.TargetKey;
                }
            }

            gViewer.Graph = graph;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
