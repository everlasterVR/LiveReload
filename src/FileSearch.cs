using MVR.FileManagementSecure;
using System.Collections.Generic;

namespace everlaster
{
    sealed class FileSearch
    {
        readonly List<string> _excludeDirs;
        readonly List<string> _patterns;
        readonly string _pluginPath;
        List<string> _fileNames;

        Dictionary<string, byte[]> _files;

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
            foreach(string pattern in _patterns)
            {
                SearchFiles(_pluginPath, pattern);
            }
        }

        void SearchFiles(string path, string pattern)
        {
            var fileNames = FileManagerSecure.GetFiles(path, pattern).ToList();

            foreach(string fileName in fileNames)
            {
                if(!_fileNames.Contains(fileName))
                {
                    _files.Add(fileName, FileManagerSecure.ReadAllBytes(fileName));
                }
            }

            foreach(string dir in FileManagerSecure.GetDirectories(path))
            {
                if(!_excludeDirs.Contains(dir))
                {
                    SearchFiles(dir, pattern);
                }
            }
        }
    }
}
