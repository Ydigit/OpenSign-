using System.IO;
using System.Security.Permissions;

namespace OpenSign.Shared
{
    public static class AppPaths
    {
        //base dir for modular paths, belongs to the class,pub acc and 1time only declared/mod
        //Combine with any exec env for dir purposes, with curretnt execution level path and the target dir
        //FOR future dirs, readonly access is important for data integrity and security
        public static string KeysPath => Path.Combine(Directory.GetCurrentDirectory(),"wwwroot","keys");
    
        //generate path for PEM
        public static string GetKeyPathPEM(string fileName)
        {
            return Path.Combine(KeysPath, $"{fileName}.pem");
        }
 
        //-----------------------------------

        //generate path for XML
        public static string GetKeyPathXML(string fileName)
        {
            return Path.Combine(KeysPath, $"{fileName}.xml");
        }
}

}