using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 单个回合头像类
/// 挂载在回合头像预制体上
/// </summary>
public class TurnItem : MonoBehaviour
{
    [Header("UI引用")]
    [Tooltip("角色头像Image")]
    public Image headIcon;
    //[Tooltip("高亮边框Image（默认隐藏）")]
    //public Image highlightFrame;
    [Tooltip("死亡灰化遮罩Image（默认隐藏）")]
    public Image deadMask;

    [HideInInspector] public BaseCharacterAttr owner;

    /// <summary>
    /// 初始化回合头像
    /// </summary>
    public void Init(BaseCharacterAttr character)
    {
        owner = character;

        // 初始化状态
        //if (highlightFrame != null)
        //{
        //    highlightFrame.gameObject.SetActive(false);
        //}
        if (deadMask != null)
        {
            deadMask.gameObject.SetActive(character.isDead);
        }

        // 新增：设置角色对应的头像
        if (headIcon != null && character.headIconSprite != null)
        {
            headIcon.sprite = character.headIconSprite;
            headIcon.color = Color.white;
            headIcon.gameObject.SetActive(true); // 确保头像显示
        }
        else
        {
            headIcon.gameObject.SetActive(false); // 无头像则隐藏
            Debug.LogWarning($"{character.name} 未设置头像Sprite！");
        }
        // （可选）如果有角色名称，添加一个Text并在这里赋值
        // if (nameText != null) nameText.text = character.gameObject.name;
    }

    /// <summary>
    /// 设置高亮状态
    /// </summary>
    public void SetHighlight(bool isHighlight)
    {
        //if (highlightFrame != null)
        //{
        //    highlightFrame.gameObject.SetActive(isHighlight);
        //}

        // 实时更新死亡状态
        if (deadMask != null && owner != null)
        {
            deadMask.gameObject.SetActive(owner.isDead);
        }
    }
}