using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using DataSearchEngine.Utils;

namespace DataSearchEngine.Upload
{
    public class Uploader
    {
        static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        readonly List<DataSource> _sources = new List<DataSource>();
        readonly List<Domain> _domains = new List<Domain>();

        [UseChildren] public ICollection<DataSource>    Sources { get { return _sources; } }
        [UseChildren] public ICollection<Domain>        Domains { get { return _domains; } }

        public string Database { get; set; }


        void Run()
        {
            // Initialize the back-end database
            var ctx = new Context(Database, Sources);

            // Allocate all domain identifiers
            using (new Timer("Allocating domains ID"))
                foreach (var domain in Domains)
                {
                    domain.DomainId = ctx.AllocateDomain(domain.Name,domain.Leaf?1:0);
                }

            // Process main upload process
            foreach (var domain in Domains)
            {
                using (new Timer("Uploading domain [" + domain.Name + "]"))
                {
                    domain.Upload(ctx);
                }
            }
        }

        static public void UploadProcess()
        {
            var instance = new Uploader();
            var appConfig = new XmlDocument();
            appConfig.Load(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);

            var rootConfig = appConfig.SelectSingleNode("/configuration/DataUpload") as XmlElement;
            ObjectLoadHelper.Load(instance, rootConfig);

            using (var t = new Timer("Upload process"))
            {
                instance.Run();
            }
        }
    }
}
