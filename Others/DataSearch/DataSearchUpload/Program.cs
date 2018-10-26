using System;
using DataSearchEngine.Utils;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace DataSearchUpload
{
    class Program
    {
        #region <log related>
        static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static Program()
        {
            try
            {
                Console.BufferHeight = 1024;
                Console.BufferWidth = 1024;
            }
            catch (Exception) { }
        }
        #endregion
        
        static void Main(string[] args)
        {
            var noOptimize = false;
            if(args.Length!=0)
                if (string.Compare(args[0], "-optimize", true) == 0)
                {
                    DataSearchEngine.Optimize.Optimizer.ProcessBackend();
                    return;
                }
                else if (string.Compare(args[0], "-nooptimize", true) == 0)
                {
                    noOptimize = true;
                }


            using (new Timer("Overall Upload process"))
            {
                DataSearchEngine.Upload.Uploader.UploadProcess();
            }

            if (!noOptimize)
            {
                DataSearchEngine.Optimize.Optimizer.ProcessBackend();
            }
        }
    }
}
