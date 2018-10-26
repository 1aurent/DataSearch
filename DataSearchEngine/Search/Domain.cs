using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataSearchEngine.Search
{
    public struct DomainLink
    {
        public readonly int TargetDomain;
        public readonly string TargetKey;
        public readonly string SourceName;

        public DomainLink(Engine parent, string linkInfos, string sourceName)
        {
            var decodedInfos = linkInfos.Split('/');

            TargetDomain = parent.GetDomainID(decodedInfos[0]);
            TargetKey = decodedInfos.Length>1?decodedInfos[1]:null;
            SourceName = sourceName;
        }
    }

    class Domain
    {
        private enum ValueType
        {
            Key,
            Text,
            Link
        }

        readonly Dictionary<string, ValueType> _domainValues;
        readonly DomainLink[] _domainLinks;
        readonly int _id, _flags;
        readonly string _name;
        readonly Engine _parent;
        private System.Data.SQLite.SQLiteCommand _getOutLinks, _getInLinks, _getText;

        public Domain(int id, string name, int flags, Engine parent, Data.IBackendProxy proxy)
        {
            _id = id;
            _name = name;
            _parent = parent;
            _flags = flags;

            _domainValues = new Dictionary<string, ValueType>(StringComparer.InvariantCultureIgnoreCase);
            var link = new List<DomainLink>();
            using (var metadata = proxy.GetDomainMetadata(id))
            {
                while (metadata.MoveNext())
                {
                    switch (metadata.Current.Type)
                    {
                        case "key":
                            _domainValues.Add(metadata.Current.Name, ValueType.Key);
                            break;
                        case "text":
                            _domainValues.Add(metadata.Current.Name, ValueType.Text);
                            break;
                        case "link":
                            _domainValues.Add(metadata.Current.Name, ValueType.Link);
                            link.Add(new DomainLink(parent, metadata.Current.Infos, metadata.Current.Name));
                            break;
                    }
                }
            }
            _domainLinks = link.ToArray();
        }

        void CreateForwardLinkQuery()
        {
            var sb = new StringBuilder("SELECT ");
            var lkD = false;
            foreach (var link in _domainLinks)
            {
                if (lkD) sb.Append(", "); else lkD = true;

                if (link.TargetKey != null)
                {
                    sb.AppendFormat("(SELECT t.ItemID FROM [domain_{0}] t WHERE t.[{1}]=src.[{2}]),{3}",
                                    _parent.GetDomain(link.TargetDomain).Name,
                                    link.TargetKey, link.SourceName,
                                    link.TargetDomain);
                }
                else
                {
                    sb.AppendFormat("src.[{0}],{1}", link.SourceName, link.TargetDomain);
                }
            }
            if(!lkD)
            {
                _getOutLinks = null;
                return;
            }

            sb.AppendFormat(" FROM [domain_{0}] src where src.ItemId=@itemid",Name);

            _getOutLinks = _parent.BackEnd.CreateCommand();
            _getOutLinks.CommandText = sb.ToString();
            _getOutLinks.Parameters.Add("@itemid", System.Data.DbType.Int32);
            _getOutLinks.Prepare();
        }

        void CreateBackwardLinkQuery()
        {
            var lkD = false;

            var sb = new StringBuilder();
            foreach (var id in _parent.AllDomainIds)
            {
                if(id==ID) continue; // Ignore me
                var domain = _parent.GetDomain(id);

                foreach (var link in domain.Links)
                {
                    if(link.TargetDomain!=ID) continue;
                    if (lkD) sb.AppendLine(" UNION ALL "); else lkD = true;

                    if (link.TargetKey != null)
                    {
                        sb.AppendFormat(
                            "SELECT {0} DomID,ItemID from [domain_{1}] where {2}=(SELECT {3} FROM [domain_{4}] WHERE itemid=@itemid)",
                            domain.ID, domain.Name,
                            link.SourceName,
                            link.TargetKey,
                            Name
                            );
                    }
                    else
                    {
                        sb.AppendFormat(
                            "SELECT {0} DomID,ItemID from [domain_{1}] where {2}=@itemid",
                            domain.ID, domain.Name,
                            link.SourceName
                            );
                    }
                    sb.AppendLine();
                }
            }
            if (!lkD)
            {
                _getInLinks = null;
                return;
            }

            _getInLinks = _parent.BackEnd.CreateCommand();
            _getInLinks.CommandText = sb.ToString();
            _getInLinks.Parameters.Add("@itemid", System.Data.DbType.Int32);
            _getInLinks.Prepare();
        }

        void CreateGetTextQuery()
        {
            if (IsLeaf)
            {
                if (_domainValues.Values.Count(p=>p==ValueType.Text)!=0)
                {
                    var firstText = _domainValues.First(p => p.Value == ValueType.Text).Key;
                    _getText = _parent.BackEnd.CreateCommand();
                    _getText.CommandText = string.Format(
                        "SELECT FullText FROM main_FullText WHERE docid=(SELECT {0} FROM [domain_{1}] where ItemId=@itemid)",
                        firstText, Name);
                    _getText.Parameters.Add("@itemid", System.Data.DbType.Int32);
                    _getText.Prepare();
                }
                return;
            }

            var selectBuilder = new StringBuilder("SELECT ");
            var fromBuilder = new StringBuilder("FROM [domain_" + Name + "] src");
            var l = 'a';
            foreach (var curLink in _domainLinks)
            {
                var targetDom = _parent.GetDomain(curLink.TargetDomain);
                if(!targetDom.IsLeaf) continue;
                if(targetDom._domainValues.Values.Count(p=>p==ValueType.Text)==0) continue;
                var firstText = targetDom._domainValues.First(p => p.Value == ValueType.Text).Key;
                if (l != 'a') selectBuilder.Append(',');
                selectBuilder.AppendFormat("(SELECT fulltext from main_fulltext where docid={0}.{1}) [C{0}]",
                                           l, firstText);
                if (curLink.TargetKey==null)
                {
                    fromBuilder.AppendFormat(" inner join [domain_{1}] {0} on src.{2}={0}.ItemID",
                        l, targetDom.Name, curLink.SourceName);
                }
                else
                {
                    fromBuilder.AppendFormat(" inner join [domain_{1}] {0} on src.{2}={0}.{3}",
                        l, targetDom.Name, curLink.SourceName, curLink.TargetKey);
                }
                l++;
            }

            if (_domainValues.Values.Count(p => p == ValueType.Text) != 0)
            {
                var firstText = _domainValues.First(p => p.Value == ValueType.Text).Key;
                if (l != 'a') selectBuilder.Append(',');
                selectBuilder.AppendFormat("(SELECT fulltext from main_fulltext where docid=src.{0}) [CSRC]",
                                           firstText);
            }

            selectBuilder.Append(' ');
            selectBuilder.Append(fromBuilder);
            selectBuilder.Append(" where src.itemid=@itemid");

            _getText = _parent.BackEnd.CreateCommand();
            _getText.CommandText = selectBuilder.ToString();
            _getText.Parameters.Add("@itemid", System.Data.DbType.Int32);
            _getText.Prepare();
        }

        public void CreateQueries()
        {
            CreateForwardLinkQuery();
            CreateBackwardLinkQuery();
            CreateGetTextQuery();
        }

        public KeyValuePair<int,int>[] GetForwardLinks(int itemId)
        {
            if (_getOutLinks == null) return new KeyValuePair<int, int>[0];
            _getOutLinks.Parameters[0].Value = itemId;
            using(var rdr = _getOutLinks.ExecuteReader())
            {
                if(!rdr.Read()) return new KeyValuePair<int, int>[0];
                var links = new List<KeyValuePair<int, int>>();

                for (var i = 0; i < rdr.FieldCount;i+=2 )
                {
                    if(rdr.IsDBNull(i)) continue;
                    links.Add(new KeyValuePair<int, int>(
                        rdr.GetInt32(i),
                        rdr.GetInt32(i + 1))
                        );   
                }
                return links.ToArray();
            }
        }

        public KeyValuePair<int,int>[] GetIncommingLinks(int itemId)
        {
            if (_getInLinks == null) return new KeyValuePair<int, int>[0];
            _getInLinks.Parameters[0].Value = itemId;
            using (var rdr = _getInLinks.ExecuteReader())
            {
                var links = new List<KeyValuePair<int, int>>();
                while (rdr.Read())
                {
                    links.Add(new KeyValuePair<int, int>(
                        rdr.GetInt32(1),
                        rdr.GetInt32(0))
                        );
                }
                return links.ToArray();
            }
        }

        public string GetTextDescription(int itemId)
        {
            if (_getText == null) return string.Empty;
            _getText.Parameters[0].Value = itemId;
            using(var rdr = _getText.ExecuteReader())
            {
                if (!rdr.Read()) return string.Empty;
                var sb = new StringBuilder();
                for(var i=0;i<rdr.FieldCount;++i)
                {
                    if (rdr.IsDBNull(i)) continue;
                    if (i > 0) sb.Append(" / ");
                    sb.Append(rdr.GetValue(i));
                }
                return sb.ToString();
            }
        }

        public int ID { get { return _id; } }
        public string Name { get { return _name; } }
        public bool IsLeaf { get { return (_flags & 1) == 1; } }

        public DomainLink[] Links { get { return _domainLinks; } }
    }
}
