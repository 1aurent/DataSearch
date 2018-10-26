using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using DataLink.Core;

namespace DataSearchEngine.Upload
{
    public class Context
    {
        #region Database Proxy interface
        public interface IContextDbProxy
        {
            [SqlStatement("INSERT INTO [main_MASTER] (ItemId,DomainId) VALUES (@itemId,@domainId)")]
            void InsertElement(int itemId, int domainId);

            [SqlStatement("INSERT INTO [main_DOMAIN] (DomainId,DomainName,flags) VALUES (@domainId,@domainName,@flags)")]
            void InsertDomain(int domainId, string domainName, int flags);

            [SqlStatement(@"INSERT INTO [main_FullText] (docid,FullText) VALUES (@docid,@fulltext);INSERT INTO [main_FullTextToMaster] (FullTextId,ItemId) VALUES (@docid,@itemId)")]
            void InsertFullText(int docId, int itemId, string fulltext);

            [SqlStatement(@"INSERT INTO [main_DomainMetadata] (DomainId,ValueName,Type,Infos) VALUES (@domainId,@valueName,@type,@infos)")]
            void SetDomainMetadata(int domainId, string valueName, string type, string infos);

        }
        #endregion

        static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        readonly public object Lock = new object();

        private readonly SQLiteConnection _backend;
        private readonly IContextDbProxy _proxy;

        private readonly Dictionary<string, DataSource> _sources;

        private int _nextDocument = 1;
        private int _nextDomain = 1;
        private int _nextDocId = 1;

        public Context(string backendFile, IEnumerable<DataSource> sources)
        {
            _sources = new Dictionary<string, DataSource>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var source in sources) _sources.Add(source.Name, source);

            if (System.IO.File.Exists(backendFile))
                System.IO.File.Delete(backendFile);

            var cxStr = new SQLiteConnectionStringBuilder
            {
                DataSource = backendFile,
                FailIfMissing = false,
                SyncMode = SynchronizationModes.Off,
                JournalMode = SQLiteJournalModeEnum.Off
            };
            _backend = new SQLiteConnection(cxStr.ConnectionString);
            _backend.Open();

            _proxy = BridgeCompiler.CreateInstance<IContextDbProxy>(_backend);

            RunSql(@"
CREATE TABLE [main_Master] ( ItemId integer not null primary key, DomainId integer not null );

CREATE TABLE [main_Domain] ( 
            DomainId   integer not null primary key, 
            DomainName TEXT  not null,
            Flags      integer not null);
CREATE TABLE [main_DomainMetadata] (
            DomainId  integer not null, 
            ValueName TEXT    not null,
            Type      TEXT    not null,
            Infos     TEXT);

CREATE VIRTUAL TABLE [main_FullText] USING fts3( FullText TEXT ) ;
CREATE TABLE [main_FullTextToMaster] ( FullTextId integer not null primary key, ItemId integer not null);
            ");
        }

        public IDbConnection GetSource(string name)
        {
            var sourceCnx = _sources[name].CreateConnection();
            sourceCnx.Open();
            return sourceCnx;
        }

        public DataSet ExecSourceQuery(string source,string SQL)
        {
            return _sources[source].ExecuteQuery(SQL);
        }

        /// <summary>
        /// Run an arbitrary SQL statement on the back-end database
        /// </summary>
        /// <param name="statement">SQL statement</param>
        /// <returns>Scalar result</returns>
        public object RunSql(string statement)
        {
            lock (Lock)
            {
                log.InfoFormat("RunSql [{0}]", statement);
                using (var cmd = _backend.CreateCommand())
                {
                    cmd.CommandText = statement;
                    return cmd.ExecuteScalar();
                }
            }
        }

        public SQLiteCommand CreateCommand()
        {
            lock (Lock)
            {
                return _backend.CreateCommand();
            }
        }

        /// <summary>
        /// Allocate a new domain
        /// </summary>
        /// <param name="domainName">Domain name</param>
        /// <returns>Domain unique identifier</returns>
        public int AllocateDomain(string domainName,int flags)
        {
            lock (Lock)
            {
                var domId = _nextDomain++;
                _proxy.InsertDomain(domId, domainName, flags);
                return domId;
            }
        }

        /// <summary>
        /// Define domain meta data
        /// </summary>
        /// <param name="domainId">Associate domain identifier</param>
        /// <param name="valueName">Column name</param>
        /// <param name="type">Type of the column</param>
        /// <param name="infos">Extra information</param>
        public void SetDomainMetadata(int domainId, string valueName, string type, string infos)
        {
            lock (Lock) _proxy.SetDomainMetadata(domainId, valueName, type, infos);
        }


        public int AllocateDocument(int domainId)
        {
            lock (Lock)
            {
                var itemId = _nextDocument++;
                _proxy.InsertElement(itemId, domainId);
                if ((itemId % 5000) == 0) log.DebugFormat("Allocated {0} id ({1})", itemId, domainId);
                return itemId;
            }
        }


        /// <summary>
        /// Add searcheable full text element
        /// </summary>
        /// <param name="itemId">Current item identifier</param>
        /// <param name="text">Text</param>
        /// <returns>Full text identifier</returns>
        public int AppendFullText(int itemId, string text)
        {
            lock (Lock)
            {
                var docId = _nextDocId++;
                _proxy.InsertFullText(docId, itemId, text);
                return docId;
            }
        }

    }
}
