using UnityEngine;
/// <summary>
/// 点击标记UI的面向相机脚本：标记永远正对着主相机，无任何旋转偏差
/// </summary>
public class Billboard : MonoBehaviour
{
    private Camera mainCamera;
    void Awake()
    {
        mainCamera = Camera.main;
    }
    void LateUpdate()
    {
        // 只旋转Y轴，保证标记贴地，不仰头低头
        transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
    }
}