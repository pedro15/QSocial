using System.IO;
using UnityEditor;

namespace QSocialEditor
{
    internal class QSocialContextMenu
    {
        static string GetPackagePath()
        {
            string _PACKAGEPATH = Path.GetFullPath("Packages/com.pedro15.qsocial");

            if (Directory.Exists(_PACKAGEPATH))
            {
                return _PACKAGEPATH;
            }

            _PACKAGEPATH = Path.GetFullPath("Packages/QSocial");

            if (Directory.Exists(_PACKAGEPATH))
            {
                return _PACKAGEPATH;
            }

            _PACKAGEPATH = Path.GetFullPath("Assets/..");

            if (Directory.Exists(_PACKAGEPATH))
            {
                if (Directory.Exists(_PACKAGEPATH + "/Assets/QSocial"))
                {
                    _PACKAGEPATH += "Assets/QSocial";
                    return _PACKAGEPATH;
                }
            }

            return null;
        }

        [MenuItem("QSocial/Install Facebook Support")]
        static void InstallFacebookSupport()
        {
            string p = GetPackagePath().Replace(@"/", @"\") + @"\MainResources\FBSupport.unitypackage";
            AssetDatabase.ImportPackage(p, false);
        }

        [MenuItem("QSocial/Install Essentials")]
        static void InstallEssentials()
        {
            string p = GetPackagePath().Replace(@"/", @"\") + @"\MainResources\Essentials.unitypackage";
            AssetDatabase.ImportPackage(p, false);
        }

    }
}