using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Dystopian.Rhythm.Editor
{
    [ScriptedImporter(1, "cmchart")]
    public sealed class CmChartImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext context)
        {
            string source = File.ReadAllText(context.assetPath);
            TextAsset chart = new TextAsset(source)
            {
                name = Path.GetFileNameWithoutExtension(context.assetPath)
            };

            context.AddObjectToAsset("chart", chart);
            context.SetMainObject(chart);
        }
    }
}
