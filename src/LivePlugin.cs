using System;
using System.Collections.Generic;
using System.Linq;
using MVR.FileManagementSecure;
using UnityEngine;
using UnityEngine.UI;
using static LiveReload.Utils;

namespace LiveReload
{
    public class LivePlugin
    {
        private readonly Atom _atom;
        private readonly MVRPluginManager _manager;

        private readonly FileSearch _fileSearch;
        private readonly string _pluginDir;
        private readonly string _pluginFullPath;
        private readonly string _pluginPath;

        private Dictionary<string, byte[]> _files;
        private Button _reloadButton;
        public JSONStorableBool monitorJsb;

        public bool waitingForUIOpened { get; private set; }
        public string Uid() => $"{_pluginFullPath}:{_atom.uid}";

        public LivePlugin(string pluginFullPath, string atomUid)
        {
            _pluginFullPath = pluginFullPath;
            string[] arr = _pluginFullPath.Split('/');
            _pluginPath = string.Join(@"\", arr).Replace($@"\{arr[arr.Length - 1]}", "");
            _pluginDir = arr[arr.Length - 2];

            _atom = SuperController.singleton.GetAtoms().Find(atom => atom.uid == atomUid);
            if(_atom == null)
            {
                LogError($"atom '{atomUid}' not found in scene!");
                return;
            }

            _manager = _atom.GetComponentInChildren<MVRPluginManager>();
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

            waitingForUIOpened = false;
            CreateMonitorToggle();
        }

        private void CreateMonitorToggle()
        {
            monitorJsb = new JSONStorableBool($"monitor{_atom.uid}{_pluginDir}", true);
            var monitorToggle = Script.script.CreateToggle(monitorJsb);
            monitorToggle.label = _atom.name == "CoreControl"
                ? _pluginDir
                : $"{_atom.uid}: {_pluginDir}";
            Script.script.RegisterBool(monitorJsb);
        }

        public void RemoveFromUI()
        {
            Script.script.RemoveToggle(monitorJsb);
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
                    if(Script.logChangesJsb.val)
                    {
                        SuperController.LogMessage($"{_pluginDir}: {path.Replace(_pluginPath, "").TrimStart('\\')} changed. Reloading.");
                    }

                    reload = true;
                }
            }

            if(reload)
            {
                Reload();
            }
        }

        private void Reload()
        {
            try
            {
                _fileSearch.FindFiles(_files); //ensure any new files added before this reload will be monitored for changes
                _reloadButton.onClick.Invoke();
                _reloadButton = FindReloadButton(); // ensure reload button is correct after reloading plugin
            }
            catch(Exception e)
            {
                LogError($"Error reloading plugin {_pluginFullPath} on atom {_atom.uid}: {e}");
            }
        }

        public void TryFindReloadButton()
        {
            try
            {
                _reloadButton = FindReloadButton();
                if(_reloadButton != null && waitingForUIOpened)
                {
                    LogMessage($"Enabled for {_pluginFullPath}.");
                    waitingForUIOpened = false;
                }
            }
            catch(Exception e)
            {
                LogError($"Unable to find reload button for plugin: {e}");
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

        private Button FindReloadButton()
        {
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
