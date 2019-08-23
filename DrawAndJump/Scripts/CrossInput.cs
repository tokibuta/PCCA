using UnityEngine;

//マルチタッチ対応済み
public class CrossInput
{

	private static Data[] currentData;
	public static int currentDataLength;
	public class Data
	{
		private int fingerId;
		public int GetFingerId(){ return fingerId; }
		public void SetFingerId(int fingerId){ this.fingerId = fingerId; }

		private Vector3 position;
		public Vector3 GetPosition(){ return position; }
		public void SetPosition(Vector3 position){ this.position = position; }

		private Action phase;
		public Action GetPhase(){ return phase; }
		public void SetPhase(Action phase){ this.phase = phase; }
	}

	public enum Action
	{
		None = -1,
		Began = 0,
		Moved = 1,
		Ended = 3
	}

	private enum Platform
	{
		None,
		Android,
		IOS,
		PC
	}

	private static Platform platform = Platform.None;

	private static Platform GetPlatform()
	{

		if(platform == Platform.None)
		{
			if(Application.platform == RuntimePlatform.Android)
			{
				platform = Platform.Android;
			}
			else if(Application.platform == RuntimePlatform.IPhonePlayer)
			{
				platform = Platform.IOS;
			}
			else
			{
				platform = Platform.PC;
			}
		}
		return platform;
	}

	//めっちゃ頭の悪い作り方、時間がなかったので。。。
	//とりあえず、Editorとスマホにあるタッチ操作情報の差を埋めるための処理
	public static Data[] GetData()
	{
		if(GetPlatform() == Platform.PC)
		{
			if(currentData == null)
			{
				currentData = new Data[1];
				currentData[0] = new Data();
			}
			currentDataLength = 1;

			//fingerId
			currentData[0].SetFingerId(0);

			//phase
			if(Input.GetMouseButtonDown(0))
			{
				currentData[0].SetPhase(Action.Began);
			}
			else if(Input.GetMouseButton(0))
			{
				currentData[0].SetPhase(Action.Moved);
			}
			else if(Input.GetMouseButtonUp(0))
			{
				currentData[0].SetPhase(Action.Ended);
			}
			else
			{
				currentData[0].SetPhase(Action.None);
			}

			//position
			currentData[0].SetPosition(Input.mousePosition);
		}
		else
		{
			if(Input.touchCount > 0)
			{
				Touch[] myTouches = Input.touches;
				currentDataLength = Input.touchCount;
				if(currentData == null || currentData.Length < currentDataLength)
				{
					currentData = new Data[currentDataLength];
				}

				for(int i = 0; i < currentDataLength; i++)
				{
					if(currentData[i] == null)
					{
						currentData[i] = new Data();
					}

					currentData[i].SetFingerId(myTouches[i].fingerId);
					currentData[i].SetPosition(myTouches[i].position);
					currentData[i].SetPhase((Action)((int)myTouches[i].phase));
				}
			}
			else
			{
				currentData = null;
			}
		}
		return currentData;
	}

	public static Action GetAction()
	{
		if(GetPlatform() == Platform.PC)
		{
			if(Input.GetMouseButtonDown(0))
			{
				return Action.Began;
			}
			else if(Input.GetMouseButton(0))
			{
				return Action.Moved;
			}
			else if(Input.GetMouseButtonUp(0))
			{
				return Action.Ended;
			}
			return Action.None;
		}
		else
		{
			if(Input.touchCount > 0)
			{
				return (Action)((int)Input.GetTouch(0).phase);
			}
		}
		return Action.None;
	}

	public static Vector3 GetPosition()
	{
		if(GetPlatform() == Platform.PC)
		{
			return Input.mousePosition;
		}
		else
		{
			if(Input.touchCount > 0)
			{
				return Input.GetTouch(0).position;
			}
		}
		return Vector3.zero;
	}
}
