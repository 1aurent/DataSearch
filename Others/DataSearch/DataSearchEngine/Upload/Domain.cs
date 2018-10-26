using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using DataSearchEngine.Utils;

namespace DataSearchEngine.Upload
{
    public class Domain
    {
        static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        readonly List<DomainItem> _items = new List<DomainItem>();

        [UseAttribute("name")]
        public string Name { get; set; }
        [UseAttribute("source")]
        public string Datasource { get; set; }
        [UseAttribute("leaf")]
        public bool Leaf { get; set; }

        public string Sql { get; set; }

        [UseChildren]
        public ICollection<DomainItem> Sources { get { return _items; } }

        // Allocated domainId
        [Ignore]
        public int DomainId { get; set; }


        public interface IDomainPrepare
        {
            int GetSourceColumn(string name);

            int CreateKey       (string name, bool useText);
            int DefineLink      (string name, string linkedDomain, string linkedId, bool useText);
            int DefineSearchText(string name);
        }
        public interface IDomainContext
        {
            object GetSourceColVal(int colId);

            void SetLinkKeyValue    (int colId, object key);
            void SetFullTextSearch  (int colId, string text);
        }

        class DomainContextImpl : IDomainContext
        {
            readonly DataTableReader _reader;
            readonly Context _context;
            readonly Domain _domain;
            readonly System.Data.SQLite.SQLiteCommand _insertCommand;
            readonly System.Data.SQLite.SQLiteParameter[] _insertParams;

            int _currentItemId;

            public DomainContextImpl(Domain domain, Context context, string insertStatement, int nbCols, DataTable sourceData)
            {
                _context = context;
                _domain = domain;
                _reader = sourceData.CreateDataReader();

                _insertCommand = _context.CreateCommand();
                _insertCommand.CommandText = insertStatement;

                _insertParams = new System.Data.SQLite.SQLiteParameter[nbCols];
                for (var u = 0; u < nbCols; ++u)
                {
                    _insertParams[u] = new System.Data.SQLite.SQLiteParameter();
                    _insertCommand.Parameters.Add(_insertParams[u]);
                }
            }

            public bool Read()
            {
                if (!_reader.Read()) return false;
                _currentItemId = _context.AllocateDocument(_domain.DomainId);
                _insertParams[0].Value = _currentItemId;
                //for(var u=1;u<_insertParams.Length;++u) _insertParams[u].Value = DBNull.Value;
                return true;
            }

            public int FlushRow()
            {
                lock (_context.Lock)
                {
                    _insertCommand.ExecuteNonQuery();
                }
                return _currentItemId;
            }


            public object GetSourceColVal(int colId)
            {
                return _reader.GetValue(colId);
            }

            public void SetLinkKeyValue(int colId,object key)
            {
                _insertParams[colId].Value = key==null ? DBNull.Value : (object)key;
            }

            public void SetFullTextSearch(int colId, string text)
            {
                _insertParams[colId].Value = string.IsNullOrEmpty(text) ? DBNull.Value : 
                    (object)_context.AppendFullText(_currentItemId, text);
            }
        }

        class DomainPrepareImpl : IDomainPrepare
        {
            readonly DataTable _sourceData;
            readonly Context _context;
            readonly StringBuilder _sbCreateDomainTable, _sbInsertDomainTable, _sbCreateSubIndexes;
            readonly Domain _domain;
            int _nextCol = 1;

            public DomainPrepareImpl(Domain domain, DataTable sourceData, Context context)
            {
                _sourceData = sourceData;

                _sbCreateDomainTable = new StringBuilder(4096);
                _sbInsertDomainTable = new StringBuilder(4096);
                _sbCreateSubIndexes  = new StringBuilder(4096);

                _domain = domain;
                _context = context;

                _sbCreateDomainTable.AppendFormat(
                    "CREATE TABLE [domain_{0}] (ItemId integer not null primary key",
                    domain.Name);
                _sbInsertDomainTable.AppendFormat("INSERT INTO [domain_{0}] VALUES (?", domain.Name);
            }

            public DomainContextImpl CreateContext()
            {
                _sbCreateDomainTable.Append(')');
                _sbInsertDomainTable.Append(')');

                // Create table and index
                _context.RunSql(_sbCreateDomainTable.ToString());
                _context.RunSql(_sbCreateSubIndexes. ToString());

                // Create a process context
                return new DomainContextImpl(_domain, _context, _sbInsertDomainTable.ToString(), _nextCol, _sourceData);
            }

            public int GetSourceColumn(string name)
            {
                return _sourceData.Columns[name].Ordinal;
            }

            public int CreateKey(string name, bool useText)
            {
                _sbCreateDomainTable.AppendFormat(", [{0}] {1}", name, useText?"varchar(128)":"integer");
                _sbInsertDomainTable.Append(",?");
                _sbCreateSubIndexes.AppendFormat("CREATE INDEX [domain_{0}_key_{1}] ON [domain_{0}]([{1}]);",
                                                 _domain.Name, name);
                _context.SetDomainMetadata(_domain.DomainId,name,"key",null);

                return _nextCol++;
            }

            public int DefineLink(string name, string linkedDomain, string linkedId, bool useText)
            {
                _sbCreateDomainTable.AppendFormat(", [{0}] {1}", name, useText ? "varchar(128)" : "integer");
                _sbInsertDomainTable.Append(",?");
                _sbCreateSubIndexes.AppendFormat("CREATE INDEX [domain_{0}_link_{1}] ON [domain_{0}]([{1}]);",
                                                 _domain.Name, name);
                _context.SetDomainMetadata(_domain.DomainId, name, "link", linkedDomain+"/"+linkedId);

                return _nextCol++;
            }

            public int DefineSearchText(string name)
            {
                _sbCreateDomainTable.AppendFormat(", [{0}] INTEGER", name);
                _sbInsertDomainTable.Append(",?");
                _context.SetDomainMetadata(_domain.DomainId, name, "text", null);
                return _nextCol++;
            }
        }


        public void Upload(Context context)
        {
            DataTable sourceData;

            using (new Timer("Domain / Upload source data"))
            {
                sourceData = context.ExecSourceQuery(Datasource, Sql).Tables[0];
            }

            double totalRows = sourceData.Rows.Count;
            log.DebugFormat("{0} - Found {1} rows", Name, totalRows);

            // Prepare
            DomainContextImpl domContext;
            {
                var domPrep = new DomainPrepareImpl(this, sourceData, context);
                foreach (var item in Sources)
                {
                    item.Prepare(domPrep);
                }
                domContext = domPrep.CreateContext();
            }

            while (domContext.Read())
            {
                foreach (var item in Sources) item.ProcessRow(domContext);
                domContext.FlushRow();
            }
        }
    }
}
