using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataSearchEngine.Utils;

namespace DataSearchEngine.Upload
{
    abstract public class DomainItem
    {
        [UseAttribute("column")]
        public string Column { get; set; }

        public abstract void Prepare    (Domain.IDomainPrepare context);
        public abstract void ProcessRow (Domain.IDomainContext context);
    }

    /// <summary>
    /// Define a column used for text search
    /// </summary>
    [NamedAs("SearchText")]
    internal class DomainSearchText : DomainItem
    {
        private int _sourceCol, _targetCol;

        public override void Prepare(Domain.IDomainPrepare context)
        {
            _sourceCol = context.GetSourceColumn(Column);
            _targetCol = context.DefineSearchText(Column);
        }

        public override void ProcessRow(Domain.IDomainContext context)
        {
            context.SetFullTextSearch( _targetCol, Convert.ToString(context.GetSourceColVal(_sourceCol)));
        }
    }

    /// <summary>
    /// Define a local identifier. Must be unique.
    /// </summary>
    [NamedAs("Key")]
    internal class DomainKey : DomainItem
    {
        private int _sourceCol, _targetCol;

        [UseAttribute("id")]
        public string Id { get; set; }
        [UseAttribute("useText")]
        public bool UseText { get; set; }

        public override void Prepare(Domain.IDomainPrepare context)
        {
            _sourceCol = context.GetSourceColumn(Column);
            _targetCol = context.CreateKey(Id ?? Column, UseText);
        }

        public override void ProcessRow(Domain.IDomainContext context)
        {
            context.SetLinkKeyValue(_targetCol, context.GetSourceColVal(_sourceCol));
        }
    }

    /// <summary>
    /// Define a link to another domain
    /// </summary>
    [NamedAs("Link")]
    internal class DomainLink : DomainItem
    {
        private int _sourceCol, _targetCol;

        [UseAttribute("id")]
        public string Id { get; set; }
        [UseAttribute("domain")]
        public string Domain    { get; set; }
        [UseAttribute("linkedId")]
        public string LinkedId  { get; set; }
        [UseAttribute("useText")]
        public bool UseText { get; set; }

        public override void Prepare(Domain.IDomainPrepare context)
        {
            _sourceCol = context.GetSourceColumn(Column);
            _targetCol = context.DefineLink(Id ?? Column, Domain, LinkedId, UseText);
        }

        public override void ProcessRow(Domain.IDomainContext context)
        {
            context.SetLinkKeyValue(_targetCol, context.GetSourceColVal(_sourceCol));
        }
    }


}
