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
        public static string KeysPathpub => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "keys", "public");
        public static string SecurePrivateBackupPath => Path.Combine(Directory.GetCurrentDirectory(), "securekeys", "private");
        //generate path for PEM
        public static string GetKeyPathPEMpublic(string fileName)
        {
            return Path.Combine(KeysPathpub, $"{fileName}.json");
        }
        public static string SecurePrivateBackupPathJSON(string fileName)
        {
            return Path.Combine(SecurePrivateBackupPath, $"{fileName}.json");
        }

        //-----------------------------------

        //general
        public static string GetKeyPathGeneral(string fileName)
        {
            //tem que meter o nome da extensao
            return Path.Combine(KeysPath, $"{fileName}");
        }
}

}