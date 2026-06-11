using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using UnityEngine;

public class LLM:MonoBehaviour
{
    /// <summary>
    /// api��ַ
    /// </summary>
    [SerializeField] protected string url;
    // ���޸�1��ȥ����ק��npcDialog���ĳɡ���ǰ���ڶԻ���NPC��
    protected NPCDialog currentTalkingNPC;
    /// <summary>
    /// ��ʾ�ʣ�����Ϣһ����
    /// </summary>
    [Header("���͵���ʾ���趨")]
    [SerializeField] protected string m_Prompt = string.Empty;
    /// <summary>
    /// ����
    /// </summary
    [Header("���ûظ�������")]
    [SerializeField] protected string lan="����";
    /// <summary>
    /// �����ı�������
    /// </summary>
    [Header("�����ı�������")]
    [SerializeField] protected int m_HistoryKeepCount = 15;
    /// <summary>
    /// ����Ի�
    /// </summary>
    [SerializeField] public List<SendData> m_DataList = new List<SendData>();
    /// <summary>
    /// ���㷽�����õ�ʱ��
    /// </summary>
    [SerializeField] protected Stopwatch stopwatch=new Stopwatch();
    /// <summary>
    /// ������Ϣ
    /// </summary>
     // ���޸�2�������������������ⲿ���á���ǰ���ڶԻ���NPC��
    public void SetCurrentNPC(NPCDialog npc)
    {
        currentTalkingNPC = npc;

        // ����ѡ��ÿ���л�NPCʱ�������һ��NPC�ĶԻ���ʷ
        // m_DataList.Clear();
    }
	// ���޸�3���޸�PostMsg�������õ�ǰNPC����ʾ��
	public virtual void PostMsg(string _msg,Action<string> _callback) {
		//��������������
		CheckHistory();

		// �����߼�������е�ǰ���ڶԻ���NPC����������ʾ�ʣ�������Ĭ�ϵ�
		string finalPrompt = m_Prompt;
		if (currentTalkingNPC != null && !string.IsNullOrEmpty(currentTalkingNPC.npcPrompt))
		{
			finalPrompt = currentTalkingNPC.npcPrompt;
		}

		//��ʾ�ʴ���
		string message = "��ǰΪ��ɫ�������趨��" + finalPrompt +
			" �ش�����ԣ�" + lan +
			" ���������ҵ����ʣ�" + _msg;

		//���淢�͵���Ϣ�б�
		m_DataList.Add(new SendData("user", message));

		// 包装回调：30秒超时自动返回错误提示
		bool hasResponded = false;
		StartCoroutine(Request(message, (_response) => {
			if (!hasResponded) { hasResponded = true; _callback(_response); }
		}));
		StartCoroutine(TimeoutCoroutine(_callback, () => hasResponded = true));
	}

	// 30���ʱ����������δ�յ��ظ����Զ�������ʾ
	private IEnumerator TimeoutCoroutine(Action<string> _callback, System.Action _markDone)
	{
		yield return new WaitForSeconds(30f);
		_markDone();
		_callback("[网络连接超时，请检查网络后重试]");
	}

    public virtual IEnumerator Request(string _postWord, System.Action<string> _callback)
    {
        yield return new WaitForEndOfFrame();
          
    }

    /// <summary>
    /// ���ñ�������������������ֹ̫��
    /// </summary>
    public virtual void CheckHistory()
    {
        if(m_DataList.Count> m_HistoryKeepCount)
        {
            m_DataList.RemoveAt(0);
        }
    }

    [Serializable]
    public class SendData
    {
        [SerializeField] public string role;
        [SerializeField] public string content;
        public SendData() { }
        public SendData(string _role, string _content)
        {
            role = _role;
            content = _content;
        }

    }

}
