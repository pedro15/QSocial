using System.IO;
using UnityEditor;

namespace QSocialEditor
{
    internal class QSocialContextMenu
    {
        static string GetPackagePath() => Path.GetFullPath("Packages/com.pedro15.qsocial/MainResources");

        [MenuItem("Tools/QSocial/Install Facebook Support")]
        static void InstallFacebookSupport()
        {
            string p = GetPackagePath() + "/FBSupport.unitypackage";
            AssetDatabase.ImportPackage(p, false);
        }

        [MenuItem("Tools/QSocial/Install Essentials")]
        static void InstallEssentials()
        {
            string p = GetPackagePath() + "/Essentials.unitypackage";
            AssetDatabase.ImportPackage(p, false);
        }

    }
}