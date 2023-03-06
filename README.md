# Bear Data Editor
![alt Bear Data editor splash](https://repository-images.githubusercontent.com/609868413/f422bb44-c553-4368-a2b4-95aa2fa48dca)
Bear Data Editor is a Unity3D editor extension, made to simplify working with data. 
Classes inheriting from ScriptableObject and MonoBehaviour can use the ``[BearDataEditor]``
attribute (available in the ``CollisionBear.BearDataEditor`` namespace)to be exposed in the editor window.
When exposed and chosen, any scriptable object instances of a given type, or prefab with a specific
monobehaviour component will be displayed in the editor sorted by name, regardless of their
location in the project folder.
Several editor instances can be open simultaneously. Searching by name among instances are supported and
next to every asset is a folder icon, allowing you to see its location in your Unity project folder.

## Getting started
First you need a cope of the software. 

### Unity Package
The editor extension can be added Unity's package manager from 'Add package from git URL'
* <https://github.com/CollisionBear/BearDataEditor.git>


### Asset store
* <https://assetstore.unity.com/packages/tools/utilities/bona-data-editor-134191>

### Manual download
You need to put the Bona Data Editor content inside your Unity project's Asset folder.
* <https://github.com/CollisionBear/BearDataEditor.git>

## Example
Decorate a class with the attribute. By default the class's name will be displayed.
```cs
using CollisionBear.BearDataEditor;

[BearDataEditor]
class TestClass: ScriptableObject {}
```
or
```cs
  [CollisionBear.BearDataEditor.BearDataEditor]
  class TestClass: ScriptableObject {}
  ```
If you want to display another name for your class in the editor, enter a `DisplayName` for it in the attribute.
```cs
using CollisionBear.BearDataEditor;

[BearDataEditor(DisplayName = "Some other name")]
class TestClass: ScriptableObject {}
```

## License
This project is released as Open Source under a [MIT license](https://opensource.org/licenses/MIT).
