using System.Collections.Generic;
using MVR.FileManagementSecure;

namespace LiveReload
{
    public class FileSearch
    {
        private readonly List<string> _excludeDirs;
        private readonly List<string> _patterns;
        private readonly string _pluginPath;
        private List<string> _fileNames;

        private Dictionary<string, byte[]> _files;

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

        private void SearchFiles(string path, string pattern)
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
