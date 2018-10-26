using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using DataSearchEngine.Search.Data;
using DataSearchEngine.Utils;

namespace DataSearchEngine.Search
{
    class SearchGraph
    {
        static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        class Node : IComparable<Node>
        {
            public int MasterID { get; private set; }
            public int DomainID { get; private set; }
            
            public bool   Processed { get; set; }
            public double Hub { get; set; }
            public double Auth { get; set; }
            public int DistanceScore { get; set; }

            public int[] IncommingLinks { get; private set; }
            public int[] OutgoingLinks { get; private set; }
            
            public Node(int domainId,int masterId)
            {
                MasterID = masterId;
                DomainID = domainId;

                Hub = Auth = 1;
                IncommingLinks = new int[0];
                OutgoingLinks = new int[0];
            }

            public int CompareTo(Node other)
            {
                if (MasterID == other.MasterID) return 0;
                var c = -DistanceScore.CompareTo(other.DistanceScore);
                if (c == 0) c = Auth.CompareTo(other.Auth);
                if (c == 0) c = Hub.CompareTo(other.Hub);
                return -c;
            }

            public void AddIncommingLink(int masterId)
            {
                if (Array.Find(IncommingLinks, p => p == masterId) != default(int)) return;
                var t = new int[IncommingLinks.Length + 1];
                Array.Copy(IncommingLinks, t, IncommingLinks.Length);
                t[IncommingLinks.Length] = masterId;
                IncommingLinks = t;
            }
            public void AddOutgoingLink(int masterId)
            {
                if (Array.Find(OutgoingLinks, p => p == masterId) != default(int)) return;
                var t = new int[OutgoingLinks.Length + 1];
                Array.Copy(OutgoingLinks, t, OutgoingLinks.Length);
                t[OutgoingLinks.Length] = masterId;
                OutgoingLinks = t;
            }
        }

        readonly Dictionary<int, Node> _allNodes = new Dictionary<int, Node>();
        readonly Engine _parent;
        readonly EngineSearchOptions _options;

        private SearchGraph(Engine parent, EngineSearchOptions options)
        {
            _parent = parent;
            _options = options;
        }

        public int TotalNodes { get { return _allNodes.Count; } }

        bool AddNode(int distance,int domainId,int itemId)
        {
            Node node;
            return AddNode(distance, domainId, itemId, out node);
        }

        bool AddNode(int distance, int domainId, int itemId, out Node node)
        {
            if (_allNodes.TryGetValue(itemId, out node))
            {
                node.DistanceScore = Math.Min(distance, node.DistanceScore);
                return false;
            }
            node = new Node(domainId, itemId);
            node.DistanceScore = distance;
            _allNodes.Add(itemId, node);
            return true;
        }

        void EnsureChartComplete()
        {
            var step = 0;
            var nbNodes = _allNodes.Count;
            foreach (var node in _allNodes.Values)
            {
                ++step;
                _parent.NotifyProgressToClient(step, nbNodes);
                if (node.Processed) continue;

                var potentialDistance = node.DistanceScore + 1;
                var domain = _parent.GetDomain(node.DomainID);

                foreach (var allNode in domain.GetForwardLinks(node.MasterID))
                {
                    Node link;
                    if (!_allNodes.TryGetValue(allNode.Key, out link)) continue;
                    link.DistanceScore = Math.Min(potentialDistance, link.DistanceScore);
                    node.AddOutgoingLink(link.MasterID);
                    link.AddIncommingLink(node.MasterID);
                }
                foreach (var allNode in domain.GetIncommingLinks(node.MasterID))
                {
                    Node link;
                    if (!_allNodes.TryGetValue(allNode.Key, out link)) continue;
                    link.DistanceScore = Math.Min(potentialDistance, link.DistanceScore);
                    node.AddIncommingLink(link.MasterID);
                    link.AddOutgoingLink(node.MasterID);
                }
                node.Processed = true;
            }
        }

        public bool ExtendChart(bool firstGeneration)
        {
            var extended = false;
            var allNodes = _allNodes.Values.ToArray();
            var step = 0;
            foreach (var node in allNodes)
            {
                ++step;
                _parent.NotifyProgressToClient(step, allNodes.Length);
                if (node.Processed) continue;

                var potentialDistance = node.DistanceScore + 1;
                var domain = _parent.GetDomain(node.DomainID);
                foreach (var allNode in domain.GetForwardLinks(node.MasterID))
                {
                    Node link;
                    extended |= AddNode(potentialDistance,allNode.Value, allNode.Key, out link);
                    node.AddOutgoingLink (link.MasterID);
                    link.AddIncommingLink(node.MasterID);
                }
                foreach (var allNode in domain.GetIncommingLinks(node.MasterID))
                {
                    Node link;
                    extended |= AddNode(potentialDistance, allNode.Value, allNode.Key, out link);
                    node.AddIncommingLink(link.MasterID);
                    link.AddOutgoingLink (node.MasterID);
                }
                node.Processed = true;
                if (TotalNodes > _options.MaxNumberOfNodes && !firstGeneration) break;
            }
            if (TotalNodes > _options.MaxNumberOfNodes)
            {
                log.InfoFormat("Reached maximum number of items ({0})- Stop extending chart", TotalNodes);
                allNodes = null; //< Free some memory
                if (!_options.DontCompleteGraphs)
                {
                    _parent.NotifyStepToClient(string.Format("Completing chart ({0} nodes)", TotalNodes));
                    EnsureChartComplete(); //< Ensure remaining nodes are correctly linked
                }
                return false;
            }
            return extended;
        }

        public void ComputeHITS()
        {
            log.Info("Start ComputeHITS");
            double deltaAuth, deltaHub;

            using (new Timer("ComputeHITS")) 
                for (var k = 0; k < 1000; k++)
                {
                    double norm = 0;
                    deltaAuth = 0;

                    var oldVal = new double[_allNodes.Count];
                    var i = 0;
                    foreach (var node in _allNodes.Values)
                    {
                        oldVal[i] = node.Auth; i++;
                        foreach (var iLink in node.IncommingLinks) // IncommingLinks
                        {
                            node.Auth += _allNodes[iLink].Hub;
                        }
                        norm += (node.Auth * node.Auth);
                    }
                    norm = Math.Sqrt(norm);

                    i = 0;
                    foreach (var node in _allNodes.Values)
                    {
                        node.Auth = node.Auth / norm;
                        deltaAuth += Math.Abs(oldVal[i] - node.Auth); i++;
                    }
                    deltaAuth /= _allNodes.Count;

                    norm = 0;
                    deltaHub = 0;
                    i = 0;
                    foreach (var node in _allNodes.Values)
                    {
                        oldVal[i] = node.Hub; i++;
                        foreach (var iLink in node.OutgoingLinks) // OutgoingLinks
                        {
                            node.Hub += _allNodes[iLink].Auth;
                        }
                        norm += (node.Hub * node.Hub);
                    }
                    norm = Math.Sqrt(norm);

                    i = 0;
                    foreach (var node in _allNodes.Values)
                    {
                        node.Hub = node.Hub / norm;
                        deltaHub += Math.Abs(oldVal[i] - node.Hub); i++;
                    }
                    deltaHub /= _allNodes.Count;

                    if (deltaAuth < 1E-06 && deltaHub < 1E-06) //< Arbitrary 
                    {
                        log.DebugFormat("{0} - Convergence detected / Stop - deltaAuth={1} deltaHub={2}", k, deltaAuth, deltaHub);
                        break;
                    }
                }

            // Rescale values
            foreach (var node in _allNodes.Values)
            {
                node.Hub = Math.Round(node.Hub * 10000, 6);
                node.Auth = Math.Round(node.Auth * 10000, 6);
            }
        }

        public System.Data.DataTable CreateTabularResult()
        {
            var nodes = _allNodes.Values.ToArray();
            Array.Sort(nodes);

            var ret = new System.Data.DataTable("Search Results");
            ret.Columns.Add("Position",     typeof(int));
            ret.Columns.Add("Domain",       typeof(string));
            ret.Columns.Add("Leaf",         typeof(bool));
            ret.Columns.Add("ItemID",       typeof(int));
            ret.Columns.Add("Hub Score",    typeof(double));
            ret.Columns.Add("Auth Score",   typeof(double));
            ret.Columns.Add("Distance",     typeof(int));
            ret.Columns.Add("Text",         typeof(string));

            var position = 0;
            var total = Math.Min(nodes.Length, _options.MaxNumberOfResults);
            foreach (var node in nodes)
            {
                var domain = _parent.GetDomain(node.DomainID);
                if (!_options.ReportNonLeafResults && !domain.IsLeaf && node.DistanceScore>0) continue;
                _parent.NotifyProgressToClient(position,total);
                ret.Rows.Add(
                    ++position,
                    domain.Name,
                    domain.IsLeaf,
                    node.MasterID,
                    node.Hub,
                    node.Auth,
                    node.DistanceScore,
                    domain.GetTextDescription(node.MasterID)
                );
                if (position > _options.MaxNumberOfResults) break;
            }

            return ret;
        }

        public XmlDocument CreateGraphDump()
        {
            var report = new XmlDocument();
            report.LoadXml("<Graph/>");

            var docElmnt = report.DocumentElement;

            foreach (var node in _allNodes.Values)
            {
                var repNode = report.CreateElement("Node");
                repNode.SetAttribute("id",   node.MasterID.ToString());
                repNode.SetAttribute("domainId", node.DomainID.ToString());
                repNode.SetAttribute("processed", node.Processed?"1":"0");

                var repScores = report.CreateElement("Scores");
                repScores.SetAttribute("distance", node.DistanceScore.ToString());
                repScores.SetAttribute("hub", node.Hub.ToString());
                repScores.SetAttribute("auth", node.Auth.ToString());

                repNode.AppendChild(repScores);
                foreach (var link in node.OutgoingLinks)
                {
                    var repLink = report.CreateElement("Link");
                    repLink.SetAttribute("to", link.ToString());
                    repNode.AppendChild(repLink);
                }
                if (_options.GraphSetIncommingLinks)
                {
                    foreach (var link in node.IncommingLinks)
                    {
                        var repLink = report.CreateElement("Link");
                        repNode.SetAttribute("from", link.ToString());
                        repNode.AppendChild(repLink);
                    }
                }

                docElmnt.AppendChild(repNode);
            }

            return report;
        }


        static public SearchGraph Create(Engine parent, EngineSearchOptions options, IEnumerator<MatchData> searchResults)
        {
            var graph = new SearchGraph(parent,options);
            while (searchResults.MoveNext())
            {
                graph.AddNode(0,searchResults.Current.Domain, searchResults.Current.Item);
            }

            return graph;
        }

    }
}
