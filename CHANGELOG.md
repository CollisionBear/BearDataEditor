# Changelog

All notable changes to this project will be documented in this file.

## [1.0.11] - 2026-10-22

### Changed
- Added functionality where the editor serializes the selected type and object, and recreates the list of assets in case of an assembly reload (Unity rebuilds)
- Added a new PreviewIcon Attribute that can be used on Sprites or Texture2D fields on Scriptable objects and attemp to use them as the Preview in the editor.
- Made the name of an asset respect the max size of the list's width. Long names gets cut off and don't push the Show In Project button to the side.

## [1.0.0] - 2022-11-20

### Changed
 - Restructured the entire package to comply with a Unity Package managed through Unity's built in [Package Manager](https://docs.unity3d.com/Manual/upm-ui.html)
