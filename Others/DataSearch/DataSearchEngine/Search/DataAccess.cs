using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Xml;
using DataLink.Core;

namespace DataSearchEngine.Search
{
    namespace Data
    {
        public class DomainInfo
        {
            [ColumnMap("DomainId")]
            public int ID { get; set; }
            [ColumnMap("DomainName")]
            public string Name { get; set; }
            [ColumnMap("Flags")]
            public int Flags { get; set; }
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

        public class MatchData
        {
            [ColumnMap("ItemId")]
            public int Item          { get; set; }
            [ColumnMap("DomainID")]
            public int Domain        { get; set; }
            [ColumnMap("MI")]
            public object MatchInfos { get; set; }
        }


        public interface IBackendProxy
        {
            [SqlStatement("select DomainId,DomainName,Flags from [main_Domain]")]
            IEnumerator<DomainInfo> GetDomains();
            [SqlStatement("select [ValueName],lower(Type) [type],[infos] from [main_DomainMetadata] where DomainId=@domainId")]
            IEnumerator<DomainMetaData> GetDomainMetadata(int domainId);

            [SqlStatement(@"select m.ItemId ItemId,m.DomainId DomainId,matchinfo(main_FullText) MI 
from main_FullText f 
		inner join main_FullTextToMaster fm on f.docid = fm.FullTextId
		inner join main_MASTER m on fm.ItemId = m.ItemId
	where f.FullText MATCH @searchExpression")]
            IEnumerator<MatchData> SearchText(string searchExpression);
        }
    }
}