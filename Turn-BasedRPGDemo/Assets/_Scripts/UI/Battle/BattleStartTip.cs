using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BattleStartTip : MonoBehaviour
{
    public GameObject panel;
    public Text tipText;
    public float showDuration = 3f; // 显示时长

    public void ShowTip()
    {
        tipText.text = "开始战斗！";
        panel.SetActive(true);
        StartCoroutine(HideTipAfterDelay());
    }

    private IEnumerator HideTipAfterDelay()
    {
        yield return new WaitForSeconds(showDuration);
        panel.SetActive(false);
    }
}