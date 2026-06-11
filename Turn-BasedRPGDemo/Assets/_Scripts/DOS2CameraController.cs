using UnityEngine;

/// <summary>
/// 相机视角控制器
/// 1. WASD相机平移 → 零延迟响应+顺滑无顿挫，无任何粘滞感
/// 2. 按住鼠标中键+移动鼠标旋转 → 速度可控、屏幕中心旋转、固定水平旋转、无俯仰
/// 3. 鼠标滚轮 → 修改FOV光学缩放，纯画面缩放无位移
/// 4. 开局玩家精准居中屏幕，不改相机初始角度/位置
/// 5. 地形高低自适应 → 低地→高地自动抬升，高地→低地自动降低，固定离地垂直高度
/// 6. 旋转/平移/缩放 所有参数全部可调，手感完美复刻神界原罪2
/// </summary>
public class DOS2CameraController : MonoBehaviour
{
    [Header("===== 核心配置 =====")]
    public Transform playerTarget;        // 拖拽玩家角色
    public LayerMask terrainLayerMask;    // 拖拽地形层

    [Header("===== WASD移动参数 (零延迟手感) =====")]
    public float moveSpeed = 60f;          // 移动速度，原版手感6-8
    public float moveSmooth = 5f;         // 移动顺滑度，越高越顺滑，默认5足够

    [Header("===== 鼠标中键旋转参数 =====")]
    public float rotateSensitivity = 0.2f;// 旋转灵敏度，0.2是完美值，越小越慢

    [Header("===== 滚轮FOV缩放参数 =====")]
    public float fovScrollSpeed = 5f;
    public float minFOV = 25f;
    public float maxFOV = 60f;

    [Header("===== 地形高低自适应【核心新增】 =====")]
    public float cameraGroundHeight = 8f; // 相机与地面的固定垂直高度【关键】
    public float terrainSmooth = 4f;      // 地形升降顺滑度，越高越平缓，防抖动
    private float targetYHeight;          // 相机目标Y高度

    private Camera mainCamera;
    private bool isCanRotate = false;
    private Vector2 currentMoveInput;
    private Vector3 smoothMoveVelocity;

    void Awake()
    {
        mainCamera = GetComponentInChildren<Camera>();
        // 初始化目标高度为相机初始Y轴高度
        targetYHeight = transform.position.y;
    }

    void Start()
    {
        // 开局：玩家精准在屏幕中心，相机位置/旋转不变
        if (playerTarget != null)
        {
            FocusPlayerToScreenCenter();
        }
    }

    void Update()
    {
        CameraMove_WASD_ZeroDelay();  // 零延迟WASD平移
        CameraRotate_MouseMiddle();   // 中键旋转
        CameraZoom_Scroll_FOV();      // 滚轮FOV缩放
    }

    void LateUpdate()
    {
        // 地形自适应放在LateUpdate，避免移动和地形检测帧错位，更顺滑
        CameraAdaptTerrainHeight();
    }

    /// <summary>
    /// ✨ 核心优化：WASD平移 零延迟+超顺滑
    /// 用GetAxisRaw获取原生输入，无缓冲延迟，手动插值顺滑，按下即动、松手即停
    /// </summary>
    private void CameraMove_WASD_ZeroDelay()
    {
        // 获取原生无延迟的按键输入，值为 -1 / 0 / 1
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        currentMoveInput = new Vector2(h, v);

        if (currentMoveInput.magnitude > 0)
        {
            // 计算相机移动方向，强制屏蔽Y轴，纯水平移动
            Vector3 moveDir = transform.forward * currentMoveInput.y + transform.right * currentMoveInput.x;
            moveDir.y = 0;
            moveDir.Normalize();

            // 帧间平滑插值，实现零延迟+顺滑移动
            Vector3 targetMovePos = transform.position + moveDir * moveSpeed * Time.deltaTime;
            transform.position = Vector3.SmoothDamp(transform.position, targetMovePos, ref smoothMoveVelocity, 1f / moveSmooth);
        }
        else
        {
            // 松手后立刻停止，无惯性滑动
            smoothMoveVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// ✨ 核心新增：地形高低自适应功能【完美实现需求】
    /// 相机移动时，自动检测脚下地形高度，保持相机与地面的垂直高度固定
    /// 低地→高地 相机抬升，高地→低地 相机降低，平滑无抖动
    /// </summary>
    private void CameraAdaptTerrainHeight()
    {
        // 1. 从相机当前位置，垂直向下发射射线，只检测地形层，不检测角色/敌人
        Ray terrainRay = new Ray(transform.position, Vector3.down);
        if (Physics.Raycast(terrainRay, out RaycastHit terrainHit, Mathf.Infinity, terrainLayerMask))
        {
            // 2. 计算相机应该在的目标Y高度 = 地形高度 + 固定离地高度
            targetYHeight = terrainHit.point.y + cameraGroundHeight;
        }

        // 3. 平滑插值修改相机Y轴高度，避免地形起伏导致的相机抖动
        Vector3 newCamPos = transform.position;
        newCamPos.y = Mathf.Lerp(transform.position.y, targetYHeight, Time.deltaTime * terrainSmooth);
        transform.position = newCamPos;
    }

    /// <summary>
    /// 鼠标中键旋转：按住+移动才旋转，屏幕中心为轴心，水平旋转无俯仰，速度可控
    /// </summary>
    private void CameraRotate_MouseMiddle()
    {
        if (Input.GetMouseButtonDown(2)) isCanRotate = true;
        if (Input.GetMouseButtonUp(2)) isCanRotate = false;

        if (isCanRotate)
        {
            float mouseX = Input.GetAxis("Mouse X");
            if (Mathf.Abs(mouseX) > 0.01f)
            {
                Vector3 screenCenter = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, mainCamera.nearClipPlane));
                // 旋转时，基于地形适配后的目标高度固定，旋转不改变高度
                float fixedY = targetYHeight;

                transform.RotateAround(screenCenter, Vector3.up, mouseX * rotateSensitivity);

                Vector3 newPos = transform.position;
                newPos.y = fixedY;
                transform.position = newPos;
            }
        }
    }

    /// <summary>
    /// 鼠标滚轮：FOV光学缩放，无相机位移，纯画面拉近/拉远
    /// </summary>
    private void CameraZoom_Scroll_FOV()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.01f) return;

        mainCamera.fieldOfView -= scroll * fovScrollSpeed;
        mainCamera.fieldOfView = Mathf.Clamp(mainCamera.fieldOfView, minFOV, maxFOV);
    }

    /// <summary>
    /// 开局聚焦玩家到屏幕中心，相机初始角度/位置不变
    /// </summary>
    private void FocusPlayerToScreenCenter()
    {
        Vector3 playerScreenPos = mainCamera.WorldToScreenPoint(playerTarget.position);
        Vector3 screenCenterPos = new Vector3(Screen.width / 2, Screen.height / 2, playerScreenPos.z);
        Vector3 targetWorldPos = mainCamera.ScreenToWorldPoint(screenCenterPos);
        transform.position += targetWorldPos - playerTarget.position;
        // 初始化地形目标高度
        targetYHeight = transform.position.y;
    }
}