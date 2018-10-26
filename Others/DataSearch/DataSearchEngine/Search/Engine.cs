using System;
using System.Linq;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Xml;
using DataLink.Core;
using DataSearchEngine.Utils;

namespace DataSearchEngine.Search
{
    public class EngineSearchOptions
    {
        public int MaxNumberOfGeneration { get; set; }
        public int MaxNumberOfNodes      { get; set; }
        public int MaxNumberOfResults    { get; set; }

        public bool DontCompleteGraphs   { get; set; }
        public bool ReportNonLeafResults { get; set; }
        public bool ReportSearchGraph    { get; set; }
        public bool GraphSetIncommingLinks { get; set; }

        public bool UseTextOnlySearch    { get; set; }

        public EngineSearchOptions()
        {
            MaxNumberOfGeneration   = 2;
            MaxNumberOfNodes        = 1500;
            MaxNumberOfResults      = 50;
            DontCompleteGraphs      = true;
            ReportNonLeafResults    = false;
            ReportSearchGraph       = false;
            UseTextOnlySearch       = false;
            GraphSetIncommingLinks  = false;
        }
    }

    public class EngineSearchResults
    {
        public System.Data.DataTable TabularData { get; internal set; }
        public XmlDocument           SearchGraph { get; internal set; }
    }


    public class Engine
    {
        static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        readonly SQLiteConnection _backend;
        readonly Data.IBackendProxy _proxy;
        readonly string _backendPath;

        readonly Dictionary<string, int> _domainDict;
        readonly Dictionary<int, Domain> _domains;

        public class StepEventArgs : EventArgs
        {
            public string Step { get; internal set; }
        }
        public class ProgressEventArgs : EventArgs
        {
            public int Current { get; internal set; }
            public int Total   { get; internal set; }
        }


        public delegate void NotifyStep(object sender, StepEventArgs e);
        public delegate void NotifyProgress(object sender, ProgressEventArgs e);

        public event NotifyStep OnStep;
        public event NotifyProgress OnProgress;

        private Engine(string backend)
        {
            _backendPath = backend;

            log.DebugFormat("Opening back-end [{0}]",backend);
            var cxStr = new SQLiteConnectionStringBuilder
            {
                DataSource = _backendPath,
                FailIfMissing = true,
                ReadOnly = true,
                SyncMode = SynchronizationModes.Off,
                JournalMode = SQLiteJournalModeEnum.Off
            };
            _backend = new SQLiteConnection(cxStr.ConnectionString);
            _backend.Open();

            _proxy = BridgeCompiler.CreateInstance<Data.IBackendProxy>(_backend);

            _domainDict = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
            var domFlags = new Dictionary<int, int>();
            using (var domains = _proxy.GetDomains())
            {
                while (domains.MoveNext())
                {
                    log.DebugFormat("Domain [{0}] = {1}", domains.Current.Name, domains.Current.ID);
                    domFlags.Add(domains.Current.ID, domains.Current.Flags);
                    _domainDict.Add(domains.Current.Name,domains.Current.ID);
                }
            }

            _domains = new Dictionary<int, Domain>(_domainDict.Count);
            foreach (var domain in _domainDict)
            {
                _domains.Add(domain.Value, 
                    new Domain(domain.Value, domain.Key, domFlags[domain.Value], this, _proxy)
                );
            }

            // Create domain link queries
            foreach (var domain in _domains.Values) domain.CreateQueries();
        }

        public int          GetDomainID(string name) { return _domainDict[name];  }
        public DomainLink[] GetDomainLinks(int id)   { return _domains[id].Links; }
        public bool         IsDomainLeaf(int id)     { return _domains[id].IsLeaf; }

        internal Domain GetDomain(int id) { return _domains[id]; }
        internal SQLiteConnection BackEnd { get { return _backend; }}

        public string[] AllDomains   { get { return _domainDict.Keys.ToArray(); } }
        public int[]    AllDomainIds { get { return _domainDict.Values.ToArray(); } }


        System.Data.DataTable SearchBasic(string searchText)
        {
            var ret = new System.Data.DataTable("Search Results");
            ret.Columns.Add("Domain", typeof(string));
            ret.Columns.Add("ItemID", typeof(int));
            ret.Columns.Add("Text",   typeof(string));

            using(var matchData = _proxy.SearchText(searchText))
            {
                while (matchData.MoveNext())
                {
                    var domain = GetDomain(matchData.Current.Domain);
                    ret.Rows.Add(
                        domain.Name, matchData.Current.Item,
                        domain.GetTextDescription(matchData.Current.Item)
                        );
                }
            }

            return ret;
        }

        internal void NotifyStepToClient(string text)
        {
            if (OnStep != null) OnStep(this, new StepEventArgs {Step = text});
        }
        internal void NotifyProgressToClient(int current,int total)
        {
            if (OnProgress != null) OnProgress(this, new ProgressEventArgs { Current = current, Total = total});
        }

        public EngineSearchResults Search(EngineSearchOptions options, string searchText)
        {
            if(options.UseTextOnlySearch)
            {
                var simple = new EngineSearchResults { TabularData = SearchBasic(searchText) };
                return simple;
            }

            SearchGraph graph;

            NotifyStepToClient("Searching text in database");
            using(var matchData = _proxy.SearchText(searchText))
            {
                graph = SearchGraph.Create(this, options, matchData);
            }
            log.InfoFormat("Raw Search results - Total nodes: {0}", graph.TotalNodes);

            for (var i = 0; i < options.MaxNumberOfGeneration; ++i)
            {
                var status = string.Format("Processing Generation {0} ({1} results so far)", i + 1, graph.TotalNodes);
                NotifyStepToClient(status);
                using (new Timer(status))
                {
                    if(!graph.ExtendChart(i==0))
                    {
                        log.Info("No more node issued or reached limit ... stop chart");
                        break;
                    }
                }
                log.InfoFormat("Generation {0} - Total nodes: {1}",i,graph.TotalNodes);
            }

            NotifyStepToClient(string.Format("Calculating HITS on {0} nodes",graph.TotalNodes));
            graph.ComputeHITS();
            NotifyStepToClient("Fetching results ...");
            var result = new EngineSearchResults { TabularData = graph.CreateTabularResult() };
            if (options.ReportSearchGraph)
            {
                NotifyStepToClient("Dumping search chart ...");
                result.SearchGraph = graph.CreateGraphDump();
            }
            return result;
        }

        public static Engine CreateEngine(string backendPath)
        {
            var engine = new Engine(backendPath);
            return engine;
        }

        public static Engine CreateEngine()
        {
            var appConfig = new XmlDocument();
            appConfig.Load(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);

            var rootConfig = appConfig.SelectSingleNode("/configuration/DataSearch/Database") as XmlElement;
            if(rootConfig==null)
            {
                throw new Exception("Unable to get the backend database from the application configuration");
            }
            return CreateEngine(rootConfig.InnerText);
        }
    }
}
