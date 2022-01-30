using System.Collections.Generic;
using MVR.FileManagementSecure;

namespace LiveReload
{
    internal class FileSearch
    {
        private string _pluginPath;
        private List<string> _patterns;
        private List<string> _excludeDirs;

        public FileSearch(string pluginPath, List<string> patterns, List<string> excludeDirs)
        {
            _pluginPath = pluginPath;
            _patterns = patterns;
            _excludeDirs = excludeDirs;
        }

        public Dictionary<string, byte[]> GetFiles()
        {
            var result = new Dictionary<string, byte[]>();

            foreach(var pattern in _patterns)
            {
                foreach(var file in GetFiles(_pluginPath, pattern))
                {
                    result.Add(file.Key, file.Value);
                }
            }

            return result;
        }

        private Dictionary<string, byte[]> GetFiles(string path, string pattern)
        {
            var result = new Dictionary<string, byte[]>();
            List<string> fileNames = FileManagerSecure.GetFiles(path, pattern).ToList();

            foreach(var fileName in fileNames)
            {
                result.Add(fileName, FileManagerSecure.ReadAllBytes(fileName));
            }

            foreach(var dir in FileManagerSecure.GetDirectories(path))
            {
                if(_excludeDirs.Contains(dir))
                {
                    continue;
                }

                foreach(var file in GetFiles(dir, pattern))
                {
                    result.Add(file.Key, file.Value);
                }
            }

            return result;
        }
    }
}
