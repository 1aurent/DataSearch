using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Data.SQLite;
using DataLink.Core;

namespace DataSearchEngine.Optimize
{
    namespace Data
    {
        public class Domain
        {
            [ColumnMap("DomainId")]
            public int ID { get; set; }
            [ColumnMap("DomainName")]
            public string Name { get; set; }
        }

        public class DomainMetaData
        {
            [ColumnMap("ValueName")]
            public string Name { get; set; }
            [ColumnMap("Type")]
            public string Type { get; set; }
            [ColumnMap("Infos")]
            public string Infos { get; set; }
        }

        public interface IDbProxy
        {
            [SqlStatement("VACUUM")]
            void Vacuum();
            [SqlStatement("select DomainId,DomainName from main_Domain")]
            IEnumerator<Domain> GetDomains();
            [SqlStatement("select ValueName,Type,Infos from main_DomainMetadata where DomainId = @id")]
            IEnumerator<DomainMetaData> GetDomainMetadata(int id);
        }
    }

    public class Optimizer
    {
        static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void ProcessDomain(SQLiteConnection backend, Data.IDbProxy proxy, Dictionary<int, string> domains, int domainID)
        {
            var domainName = domains[domainID];
            var indexPrefix = string.Format("opt_{0}_domain_{1}", (DateTime.Now.Ticks / 1000L).ToString("X"), domainName);

            var sb = new StringBuilder();
            sb.AppendFormat("CREATE TABLE [optimized_domain_{0}] AS SELECT ItemId ", domainName);

            var sbPost = new StringBuilder();
            sbPost.AppendFormat("CREATE UNIQUE INDEX [{0}_ItemId] ON [optimized_domain_{1}](ItemID);", indexPrefix,domainName);
            sbPost.AppendLine();

            var optimizationRequired = false;
            using (var rdr = proxy.GetDomainMetadata(domainID))
            {
                while (rdr.MoveNext())
                {
                    var cur = rdr.Current;
                    sb.Append(',');
                    switch (cur.Type)
                    {
                        case "text":
                            sb.Append(cur.Name);
                            break;
                        case "key":
                            sb.Append(cur.Name);
                            sbPost.AppendFormat("CREATE UNIQUE INDEX [{0}_{2}] ON [optimized_domain_{1}]({2});", indexPrefix, domainName, cur.Name);
                            sbPost.AppendLine();
                            break;
                        case "link":
                            var decodedInfos = cur.Infos.Split('/');
                            switch (decodedInfos.Length)
                            {
                                case 1:
                                    sb.Append(cur.Name);
                                    sbPost.AppendFormat("CREATE UNIQUE INDEX [{0}_{2}] ON [optimized_domain_{1}]({2});", indexPrefix, domainName, cur.Name);
                                    sbPost.AppendLine();
                                    break;
                                case 2:
                                    sb.AppendFormat("(SELECT t.ItemID FROM [domain_{0}] t WHERE t.[{1}]=src.[{2}]) [{2}]", decodedInfos[0], decodedInfos[1], cur.Name);
                                    sbPost.AppendFormat("CREATE INDEX [{0}_{2}] ON [optimized_domain_{1}]({2});", indexPrefix, domainName, cur.Name);
                                    sbPost.AppendLine();
                                    sbPost.AppendFormat("UPDATE main_DomainMetadata SET Infos='{0}' WHERE DomainId={1} AND ValueName='{2}';", decodedInfos[0], domainID, cur.Name);
                                    sbPost.AppendLine();
                                    optimizationRequired = true;
                                    break;
                                default:
                                    throw new Exception("Invalid link description");
                            }
                            break;
                    }
                }
                if (!optimizationRequired) return;

                sb.AppendFormat(" FROM [domain_{0}] src;",domainName); sb.AppendLine();
                sb.Append(sbPost); sb.AppendLine();
                sb.AppendFormat("DROP TABLE [domain_{0}];", domainName); sb.AppendLine();
                sb.AppendFormat("ALTER TABLE [optimized_domain_{0}] RENAME TO [domain_{0}];", domainName);

                using (new Utils.Timer("Updating Domain"))
                using (var cmd = backend.CreateCommand())
                {
                    cmd.CommandText = sb.ToString();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        static public void ProcessBackend(string databaseFile)
        {
            var cxStr = new SQLiteConnectionStringBuilder
            {
                DataSource = databaseFile,
                FailIfMissing = true,
                SyncMode = SynchronizationModes.Off,
                JournalMode = SQLiteJournalModeEnum.Off,
                ReadOnly = false
            };
            using (var backend = new SQLiteConnection(cxStr.ConnectionString))
            {
                backend.Open();

                var proxy = BridgeCompiler.CreateInstance<Data.IDbProxy>(backend);
                var domains=new Dictionary<int, string> ();
                using (var allDoms = proxy.GetDomains()) while (allDoms.MoveNext()) domains.Add(allDoms.Current.ID, allDoms.Current.Name);

                foreach (var domainID in domains.Keys)
                {
                    log.InfoFormat("Process Domain {0} ({1})", domainID, domains[domainID]);
                    using (new Utils.Timer("Optimize Domain " + domainID))
                    {
                        ProcessDomain(backend, proxy, domains, domainID);
                    }
                }

                using (new Utils.Timer("VACUUM !!!")) proxy.Vacuum();
            }
        }
        static public void ProcessBackend()
        {
            var appConfig = new XmlDocument();
            appConfig.Load(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);

            var rootConfig = appConfig.SelectSingleNode("/configuration/DataUpload/Database") as XmlElement;
            if (rootConfig == null)
            {
                throw new Exception("Unable to get the backend database from the application configuration");
            }

            ProcessBackend(rootConfig.InnerText);
        }
    }
}
