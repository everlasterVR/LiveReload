# LiveReload Virt-A-Mate Plugin

Monitors the source files of other plugins for changes and auto-reloads them when changes are detected.

## Usage

Add as scene plugin, session plugin or to any atom which has your plugin(s) loaded.

If added to an atom's plugin tab, LiveReload will only detect other plugins in that tab. If added as a scene or session plugin, it will detect other plugins anywhere in the scene.

Any monitored plugin's dir must be under Custom/Scripts/[CreatorName] where CreatorName is what you have set in User Preferences. Plugins that are under other dirs or in var packages are ignored.

## Notes

Currently, LiveReload is hard coded to skip the following subdirs under the plugin dir:

    .git
    .vscode
    bin
    obj

Similarly, it is hard coded to detect changes only in the following file types:

    *.cs
    *.cslist
    *.json

### Known issues

- The above hard coded lists should be configurable
- Other session plugins are not detected

## Contributing

Useful additions and changes from collaborators are welcome - feel free to fork, modify and come back with a pull request.

## License

[MIT](https://github.com/everlasterVR/LiveReload/blob/master/LICENSE)
