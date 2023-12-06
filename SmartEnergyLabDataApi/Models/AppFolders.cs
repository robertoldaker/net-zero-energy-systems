using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SmartEnergyLabDataApi.Models
{
    public class AppFolders
    {
        private static AppFolders _instance;

        public static void Initialise(string baseFolder, string webFolder)
        {
            _instance = new AppFolders(baseFolder,webFolder);
        }

        public static AppFolders Instance
        {
            get
            {
                if ( _instance == null )
                {
                    throw new Exception("Please call Initialise to create the singleton instance");
                } else
                {
                    return _instance;
                }
            }
        }


        private string _baseFolder;
        private string _webFolder;
        public AppFolders( string baseFolder, string webFolder )
        {
            _baseFolder = baseFolder;
            _webFolder = webFolder;
        }

        public string Uploads
        {
            get
            {
                string folder = Path.Combine(_baseFolder, "App_Data", "uploads");
                Directory.CreateDirectory(folder); 
                return folder;
            }
        }

        public string Temp
        {
            get
            {
                string folder = Path.Combine(_baseFolder, "App_Data", "temp");
                Directory.CreateDirectory(folder); 
                return folder;
            }
        }

        public string Documents
        {
            get
            {
                string folder = Path.Combine(_baseFolder, "App_Data", "Documents");
                Directory.CreateDirectory(folder); 
                return folder;
            }
        }

        public string Logs
        {
            get
            {
                string folder = Path.Combine(_baseFolder, "Logs");
                Directory.CreateDirectory(folder); 
                return folder;
            }
        }

        public string Help
        {
            get
            {
                string folder = Path.Combine(_baseFolder, "Views", "Help");
                Directory.CreateDirectory(folder); 
                return folder;
            }
        }

        public string DbBackups
        {
            get
            {
                string folder = Path.Combine(_baseFolder, "App_Data", "DbBackups");
                Directory.CreateDirectory(folder); 
                return folder;
            }
        }

        public string WebFolder
        {
            get
            {
                return _webFolder;
            }
        }

        public string GetTempFile(string ext) {
            var guid = Guid.NewGuid();
            var folder = Temp;
            Directory.CreateDirectory(folder);            
            var name = Path.Combine(folder,guid.ToString());
            name += ext;
            return name;
        }

    }
}
