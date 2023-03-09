using System.Collections;
using UnityEditor;
using UnityEngine;
 
/// <summary>
/// Inspector for .SVG assets
/// </summary>
[CustomEditor(typeof(DefaultAsset))]
public class GeoJsonFileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // .svg files are imported as a DefaultAsset.
        // Need to determine that this default asset is an .svg file
        var path = AssetDatabase.GetAssetPath(target);
 
        if (path.EndsWith(".geojson"))
        {
            GeoJsonInspectorGUI();
        }
        else
        {
            base.OnInspectorGUI();
        }
    }
 
    private void GeoJsonInspectorGUI()
    {  
        // TODO: Add inspector code here
        
    }
}