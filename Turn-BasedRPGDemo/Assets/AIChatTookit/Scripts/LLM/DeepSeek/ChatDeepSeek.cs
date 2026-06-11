using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ChatDeepSeek : LLM
{

	public ChatDeepSeek()
	{
		url = "https://api.deepseek.com/v1";
	}

	/// <summary>
	/// api key
	/// </summary>
	[SerializeField] private string api_key;
	/// <summary>
	/// AI�趨
	/// </summary>
	public string m_SystemSetting = string.Empty;
	/// <summary>
	/// ģ������
	/// </summary>
	public string m_ModelName = "deepseek-chat";

	private void Start()
	{
		//����ʱ������AI�趨
		m_DataList.Add(new SendData("system", m_SystemSetting));
	}

	/// <summary>
	/// ������Ϣ
	/// </summary>
	/// <returns></returns>
	public override void PostMsg(string _msg, Action<string> _callback)
	{
		base.PostMsg(_msg, _callback);
	}

	/// <summary>
	/// ���ýӿ�
	/// </summary>
	/// <param name="_postWord"></param>
	/// <param name="_callback"></param>
	/// <returns></returns>
	public override IEnumerator Request(string _postWord, System.Action<string> _callback)
	{
		stopwatch.Restart();
		using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
		{
			PostData _postData = new PostData
			{
				model = m_ModelName,
				messages = m_DataList
			};

			string _jsonText = JsonUtility.ToJson(_postData);
			byte[] data = System.Text.Encoding.UTF8.GetBytes(_jsonText);
			request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);
			request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

			request.SetRequestHeader("Content-Type", "application/json");
			request.SetRequestHeader("Authorization", string.Format("Bearer {0}", api_key));

			yield return request.SendWebRequest();

			if (request.responseCode == 200)
			{
				string _msgBack = request.downloadHandler.text;
				MessageBack _textback = JsonUtility.FromJson<MessageBack>(_msgBack);
				if (_textback != null && _textback.choices.Count > 0)
				{

					string _backMsg = _textback.choices[0].message.content;
					//���Ӽ�¼
					m_DataList.Add(new SendData("assistant", _backMsg));
					_callback(_backMsg);
				}
			}
			else
			{
				// 网络失败/API异常时，返回用户友好的提示文本
				string _msgBack = request.downloadHandler.text;
				Debug.LogError($"AI请求失败 (HTTP {request.responseCode}): {_msgBack}");
				_callback("[网络连接失败，请检查网络后重试]");
			}

			stopwatch.Stop();
			Debug.Log("DeepSeek��ʱ��" + stopwatch.Elapsed.TotalSeconds);
		}
	}

	#region ���ݰ�

	[Serializable]
	public class PostData
	{
		public string model;
		public List<SendData> messages;
		public bool stream=false;
	}


	[Serializable]
	public class MessageBack
	{
		public string id;
		public string created;
		public string model;
		public List<MessageBody> choices;
	}
	[Serializable]
	public class MessageBody
	{
		public Message message;
		public string finish_reason;
		public string index;
	}
	[Serializable]
	public class Message
	{
		public string role;
		public string content;
	}

	#endregion


}
