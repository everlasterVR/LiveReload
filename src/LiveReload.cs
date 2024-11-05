using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace everlaster
{
    public class LiveReload : MVRScript
    {
        public const string VERSION = "v0.0.0";
        bool _initDone;
        bool _isFocused;

        bool _isSessionPlugin;
        List<LivePlugin> _livePlugins;

        string _mainDir;
        FrequencyRunner _runner;

        public JSONStorableBool logChangesJsb { get; private set; }

        void Update()
        {
            if(!_initDone || !_isFocused)
            {
                return;
            }

            try
            {
                _runner.Run(() =>
                {
                    CheckAllPlugins();
                    StartCoroutine(DeferBuildLivePluginsList());
                });
            }
            catch(Exception e)
            {
                Utils.LogError($"Update: {e}");
                enabled = false;
            }
        }

        void OnApplicationFocus(bool isFocused)
        {
            _isFocused = isFocused;
        }

        public override void Init()
        {
            try
            {
                string creatorName = UserPreferences.singleton.creatorName;
                if(creatorName == null || creatorName.Trim().Length == 0)
                {
                    Utils.LogError("Set your creator name in user preferences and reload the plugin.");
                    return;
                }

                _isSessionPlugin = containingAtom.type == "SessionPluginManager";
                _mainDir = $@"Custom\Scripts\{creatorName}";

                var title = TitleTextField("Title", $"\n{nameof(LiveReload)} {VERSION}", 36);
                title.dynamicText.backgroundColor = Color.clear;
                title.dynamicText.textColor = Color.white;

                CreateLogDetectedChangesToggle();
                var spacer = CreateSpacer();
                spacer.height = 10;

                _livePlugins = new List<LivePlugin>();
                _runner = new FrequencyRunner(1);

                StartCoroutine(DeferInit());
            }
            catch(Exception e)
            {
                Utils.LogError($"Init: {e}");
            }
        }

        JSONStorableString TitleTextField(
            string paramName,
            string initialValue,
            int fontSize,
            int height = 120,
            bool rightSide = false
        )
        {
            var storable = new JSONStorableString(paramName, initialValue);
            var textField = CreateTextField(storable, rightSide);
            textField.UItext.fontSize = fontSize;
            textField.height = height;
            return storable;
        }

        void CreateLogDetectedChangesToggle()
        {
            logChangesJsb = new JSONStorableBool("logDetectedChanges", false);
            var logChangesToggle = CreateToggle(logChangesJsb);
            logChangesToggle.label = "Log Detected Changes";
            RegisterBool(logChangesJsb);
        }

        Dictionary<string, List<string>> FindPluginsInSceneJSON()
        {
            try
            {
                var result = new Dictionary<string, List<string>>();
                var atomsJSONArray = SuperController.singleton.GetSaveJSON().AsObject["atoms"].AsArray;
                foreach(JSONClass atomJSON in atomsJSONArray)
                {
                    // skip other atoms if not added as scene/session plugin
                    if(containingAtom.name != "CoreControl" && containingAtom.uid != atomJSON["id"].Value)
                    {
                        continue;
                    }

                    var plugins = FindPluginsInAtomJson(atomJSON);
                    if(plugins != null && plugins.Any())
                    {
                        result[atomJSON["id"]] = plugins;
                    }
                }

                return result;
            }
            catch(Exception e)
            {
                Utils.LogError($"Failed reading scene JSON for plugin paths: {e}");
            }

            return null;
        }

        List<string> FindPluginsInAtomJson(JSONClass atomJSON)
        {
            var managerJSON = FindPluginManagerJSON(atomJSON["storables"].AsArray);
            if(managerJSON != null)
            {
                return FindPluginsInManagerJSON(managerJSON);
            }

            return null;
        }

        JSONClass FindPluginManagerJSON(JSONArray storablesJSONArray)
        {
            foreach(JSONClass storableJSON in storablesJSONArray)
            {
                if(storableJSON["id"].Value == "PluginManager")
                {
                    return storableJSON;
                }
            }

            return null;
        }

        List<string> FindPluginsInManagerJSON(JSONClass managerJson)
        {
            var result = new List<string>();
            var pluginsObj = managerJson["plugins"].AsObject;
            foreach(string key in pluginsObj.Keys)
            {
                string pluginFullPath = pluginsObj[key].Value;
                if(
                    !pluginFullPath.Contains(nameof(LiveReload)) &&
                    pluginFullPath.StartsWith(_mainDir.Replace(@"\", "/"))
                )
                {
                    result.Add(pluginFullPath);
                }
            }

            return result;
        }

        IEnumerator DeferInit()
        {
            yield return new WaitForEndOfFrame();
            while(SuperController.singleton.isLoading)
            {
                yield return null;
            }

            BuildLivePluginsList();
            _initDone = true;
        }

        MVRPluginManager FindManager(string atomUid)
        {
            MVRPluginManager result = null;
            if(_isSessionPlugin && atomUid == "Session")
            {
                result = manager;
            }
            else if(atomUid == "CoreControl")
            {
                var foundAtom = SuperController.singleton.GetAtoms().Find(atom => atom.uid == atomUid);
                var managers = foundAtom.GetComponentsInChildren<MVRPluginManager>();
                foreach(var mgr in managers)
                {
                    if(mgr.name == "ScenePluginManager")
                    {
                        result = mgr;
                    }
                }
            }
            else
            {
                var foundAtom = SuperController.singleton.GetAtoms().Find(atom => atom.uid == atomUid);
                result = foundAtom.GetComponentInChildren<MVRPluginManager>();
            }

            return result;
        }

        void BuildLivePluginsList()
        {
            try
            {
                var pluginsByAtom = FindPluginsInSceneJSON();
                if(_isSessionPlugin)
                {
                    var sessionPlugins = FindPluginsInManagerJSON(manager.GetJSON());
                    if(sessionPlugins.Any())
                    {
                        pluginsByAtom["Session"] = sessionPlugins;
                    }
                }

                foreach(var kvp in pluginsByAtom)
                {
                    string atomUid = kvp.Key;
                    var atomPlugins = kvp.Value;
                    foreach(string pluginFullPath in atomPlugins)
                    {
                        var existingLivePlugin = _livePlugins.Find(existing => existing.Uid() == $"{atomUid}:{pluginFullPath}");
                        if(existingLivePlugin == null)
                        {
                            _livePlugins.Add(new LivePlugin(this, atomUid, pluginFullPath, FindManager(atomUid)));
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Utils.LogError($"BuildLivePluginsList: {e}");
                enabled = false;
            }
        }

        IEnumerator DeferBuildLivePluginsList()
        {
            yield return new WaitForSecondsRealtime(0.67f);
            BuildLivePluginsList();
        }

        void CheckAllPlugins()
        {
            for(int i = _livePlugins.Count - 1; i >= 0; i--)
            {
                var livePlugin = _livePlugins[i];
                if(livePlugin.waitingForUIOpened)
                {
                    livePlugin.TryFindReloadButton();
                    livePlugin.FindFiles();
                }
                else if(!livePlugin.Present())
                {
                    livePlugin.RemoveFromUI();
                    _livePlugins.RemoveAt(i);
                }
                else if(livePlugin.monitorJsb.val)
                {
                    livePlugin.CheckDiff();
                }
            }
        }
    }
}
