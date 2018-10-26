using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace DataSearchGUI
{
    public partial class SearchGraph : Form
    {
        public SearchGraph(XmlDocument dump)
        {
            InitializeComponent();

            var graph = new Microsoft.Msagl.Drawing.Graph("Search Graph");


            var allnodes = dump.SelectNodes("/Graph/Node");
            if(allnodes==null) return;
            foreach(XmlElement node in allnodes)
            {
                var gnode = graph.AddNode(node.Attributes["id"].Value);
                gnode.LabelText = node.Attributes["domainId"].Value + "/" + node.Attributes["id"].Value;
            }
            foreach (XmlElement node in allnodes)
            {
                var edges = node.SelectNodes("Link");
                if(edges==null) continue;
                var me = node.Attributes["id"].Value;
                foreach (XmlElement edge in edges)
                {
                    var to = edge.Attributes["to"];
                    if(to!=null) graph.AddEdge(me, to.Value);
                }
            }

            gViewer.Graph = graph;
        }
    }
}
