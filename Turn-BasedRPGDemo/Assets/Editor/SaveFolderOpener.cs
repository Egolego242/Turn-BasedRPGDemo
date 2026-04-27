using UnityEditor;
using UnityEngine;

public static class SaveFolderOpener
{
    [MenuItem("工具/打开存档文件夹")]
    public static void OpenPersistentDataPath()
    {
        // Unity官方API，直接在系统资源管理器里打开路径
        EditorUtility.RevealInFinder(Application.persistentDataPath);
        Debug.Log($"已打开存档根路径：{Application.persistentDataPath}");
    }
}