using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace everlaster
{
    sealed class LivePlugin
    {
        readonly string _atomUid;

        readonly FileSearch _fileSearch;
        readonly MVRPluginManager _manager;
        readonly string _pluginDir;
        readonly string _pluginFullPath;
        readonly string _pluginPath;
        readonly LiveReload _script;

        Dictionary<string, byte[]> _files;
        Button _reloadButton;
        public JSONStorableBool monitorJsb;

        public LivePlugin(LiveReload script, string atomUid, string pluginFullPath, MVRPluginManager manager)
        {
            _script = script;
            _pluginFullPath = pluginFullPath;
            _atomUid = atomUid;
            _manager = manager;

            string[] arr = _pluginFullPath.Split('/');
            _pluginPath = string.Join(@"\", arr).Replace($@"\{arr[arr.Length - 1]}", "");
            _pluginDir = arr[arr.Length - 2];

            _fileSearch = new FileSearch(
                _pluginPath,
                //todo configurable
                new List<string>
                {
                    "*.cs",
                    "*.cslist",
                    "*.json",
                },
                //todo from gitignore
                new List<string>
                {
                    $@"{_pluginPath}\.git",
                    $@"{_pluginPath}\.vscode",
                    $@"{_pluginPath}\bin",
                    $@"{_pluginPath}\obj",
                }
            );

            CreateMonitorToggle();

            if(monitorJsb.val)
            {
                TryFindReloadButton();
                FindFiles();
            }
        }

        public bool waitingForUIOpened { get; private set; }
        public string Uid() => $"{_atomUid}:{_pluginFullPath}";

        void CreateMonitorToggle()
        {
            monitorJsb = new JSONStorableBool($"monitor{_atomUid}{_pluginDir}", true);
            var monitorToggle = _script.CreateToggle(monitorJsb);
            monitorToggle.label = _atomUid == "CoreControl"
                ? $"Scene: {_pluginDir}"
                : $"{_atomUid}: {_pluginDir}";
            _script.RegisterBool(monitorJsb);
        }

        public void RemoveFromUI()
        {
            _script.DeregisterBool(monitorJsb);
            _script.RemoveToggle(monitorJsb);
        }

        public bool Present()
        {
            var pluginsObj = _manager.GetJSON()["plugins"].AsObject;
            bool present = pluginsObj.Keys
                .Select(key => pluginsObj[key].Value)
                .Any(path => path == _pluginFullPath);

            if(!present)
            {
                // ensure reload button is correct if plugin in error state
                _reloadButton = FindReloadButton();
            }

            return present;
        }

        public void CheckDiff()
        {
            var paths = new List<string>(_files.Keys);
            bool reload = false;
            var reloadedFiles = new List<string>();

            foreach(string path in paths)
            {
                byte[] contents = null;
                try
                {
                    contents = FileManagerSecure.ReadAllBytes(path);
                }
                catch(Exception)
                {
                    // ignored
                }

                if(contents != null && !contents.SequenceEqual(_files[path]))
                {
                    _files[path] = contents;
                    reload = true;
                    if(_script.logChangesJsb.val)
                    {
                        reloadedFiles.Add(path.Replace(_pluginPath, "").TrimStart('\\'));
                    }
                }
            }

            if(reload)
            {
                if(_script.logChangesJsb.val)
                {
                    SuperController.LogMessage($"Reloading {_pluginDir}. Changed: {string.Join(", ", reloadedFiles.ToArray())}");
                }

                Reload();
            }
        }

        void Reload()
        {
            try
            {
                _fileSearch.FindFiles(_files); // ensure any new files added before this reload will be monitored for changes
                _reloadButton.onClick.Invoke();
                _reloadButton = FindReloadButton(); // ensure reload button is correct after reloading plugin
            }
            catch(Exception e)
            {
                _script.logBuilder.Exception(e);
            }
        }

        public void TryFindReloadButton()
        {
            try
            {
                _reloadButton = FindReloadButton();
                if(_reloadButton != null && waitingForUIOpened)
                {
                    _script.logBuilder.Message($"Enabled for {_pluginFullPath}.");
                    waitingForUIOpened = false;
                }
            }
            catch(Exception e)
            {
                _script.logBuilder.Exception(e);
            }
        }

        public void FindFiles()
        {
            if(_files == null)
            {
                _files = new Dictionary<string, byte[]>();
            }

            _fileSearch.FindFiles(_files);
        }

        Button FindReloadButton()
        {
            if(_manager.pluginListPanel == null)
            {
                if(!waitingForUIOpened)
                {
                    _script.logBuilder.Message($"Open the UI of atom '{_atomUid}' once to enable live reloading for {_pluginFullPath}.");
                    waitingForUIOpened = true;
                }

                return null;
            }

            foreach(Transform transform in _manager.pluginListPanel)
            {
                var urlTransform = transform.Find("Panel").Find("URL");
                if(urlTransform.GetComponent<Text>().text == _pluginFullPath)
                {
                    var buttonTransform = transform.Find("ReloadButton");
                    return buttonTransform.GetComponent<Button>();
                }
            }

            return null;
        }
    }
}
