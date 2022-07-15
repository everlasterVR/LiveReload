using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using static LiveReload.Utils;

namespace LiveReload
{
    public class Script : MVRScript
    {
        public static MVRScript script { get; private set; }
        public const string VERSION = "v0.0.0";

        private static string _mainDir;
        private readonly List<LivePlugin> _livePlugins = new List<LivePlugin>();
        private bool _pluginsListBuilt;

        private FrequencyRunner _checkPluginsRunner;

        public static JSONStorableBool logChanges { get; private set; }

        public void Update()
        {
            if(!_pluginsListBuilt)
            {
                return;
            }

            try
            {
                _checkPluginsRunner.Run(CheckAllPlugins);
            }
            catch(Exception e)
            {
                LogError($"Update: {e}. Disabling {nameof(LiveReload)}.");
                enabled = false;
            }
        }

        public override void Init()
        {
            try
            {
                script = this;
                string creatorName = UserPreferences.singleton.creatorName;
                if(creatorName == null || creatorName.Trim().Length == 0)
                {
                    LogError("Set your creator name in user preferences and reload the plugin.");
                    return;
                }

                _mainDir = $@"Custom\Scripts\{creatorName}";

                var title = this.NewTextField("Title", $"\n{nameof(LiveReload)} {VERSION}", 36);
                title.dynamicText.backgroundColor = Color.clear;
                title.dynamicText.textColor = Color.white;

                var spacer = CreateSpacer(true);
                spacer.height = 120;

                _checkPluginsRunner = new FrequencyRunner(1);
                StartCoroutine(DeferBuildLivePluginsList());
                logChanges = script.NewToggle("Log detected changes", false, true);
            }
            catch(Exception e)
            {
                LogError($"Init: {e}");
            }
        }

        private Dictionary<string, List<string>> FindPluginsInSceneJson(JSONArray atomsJSONArray)
        {
            var result = new Dictionary<string, List<string>>();
            try
            {
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
                LogError($"Failed reading scene JSON for plugin paths: {e}");
            }

            return null;
        }

        private static List<string> FindPluginsInAtomJson(JSONClass atomJSON)
        {
            var managerJSON = FindPluginManagerJson(atomJSON["storables"].AsArray);
            if(managerJSON != null)
            {
                return FindPluginsInManagerJSON(managerJSON);
            }

            return null;
        }

        private static JSONClass FindPluginManagerJson(JSONArray storablesJSONArray)
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

        private static List<string> FindPluginsInManagerJSON(JSONClass managerJson)
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

        private IEnumerator DeferBuildLivePluginsList()
        {
            yield return new WaitForEndOfFrame();
            BuildLivePluginsList();
            _pluginsListBuilt = true;
        }

        private void BuildLivePluginsList()
        {
            var sceneJSON = SuperController.singleton.GetSaveJSON().AsObject;
            var pluginsByAtom = FindPluginsInSceneJson(sceneJSON["atoms"].AsArray);

            try
            {
                foreach(var kvp in pluginsByAtom)
                {
                    string atomName = kvp.Key;
                    var atomPlugins = kvp.Value;
                    foreach(string plugin in atomPlugins)
                    {
                        var livePlugin = new LivePlugin(plugin, atomName);
                        livePlugin.CreateMonitorToggle();
                        if(livePlugin.monitorJsb.val)
                        {
                            livePlugin.TryFindReloadButton();
                            livePlugin.FindFiles();
                        }

                        _livePlugins.Add(livePlugin);
                    }
                }
            }
            catch(Exception e)
            {
                LogError($"AddPluginsFromJson: {e}");
                enabled = false;
            }
        }

        private bool CheckAllPlugins()
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

            return true;
        }
    }
}
