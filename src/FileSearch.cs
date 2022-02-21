using System.Collections.Generic;
using MVR.FileManagementSecure;

namespace LiveReload
{
    internal class FileSearch
    {
        private string _pluginPath;
        private List<string> _patterns;
        private List<string> _excludeDirs;

        private Dictionary<string, byte[]> _files;
        private List<string> _fileNames;

        public FileSearch(string pluginPath, List<string> patterns, List<string> excludeDirs)
        {
            _pluginPath = pluginPath;
            _patterns = patterns;
            _excludeDirs = excludeDirs;
        }

        public void FindFiles(Dictionary<string, byte[]> files)
        {
            _files = files;
            _fileNames = _files.Keys.ToList();
            foreach(var pattern in _patterns)
            {
                SearchFiles(_pluginPath, pattern);
            }
        }

        private void SearchFiles(string path, string pattern)
        {
            var result = new Dictionary<string, byte[]>();
            List<string> fileNames = FileManagerSecure.GetFiles(path, pattern).ToList();

            foreach(var fileName in fileNames)
            {
                if(!_fileNames.Contains(fileName))
                {
                    _files.Add(fileName, FileManagerSecure.ReadAllBytes(fileName));
                }
            }

            foreach(var dir in FileManagerSecure.GetDirectories(path))
            {
                if(!_excludeDirs.Contains(dir))
                {
                    SearchFiles(dir, pattern);
                }
            }
        }
    }
}
