using UnityEngine;

public class OrderedUpdatePreCallback : MonoBehaviour 
{
	public System.Action OnUpdate, OnFixedUpdate, OnLateUpdate;

	void Update()
	{
		if( OnUpdate != null )
			OnUpdate();
	}

	void FixedUpdate()
	{
		if( OnFixedUpdate != null )
			OnFixedUpdate();
	}

	void LateUpdate()
	{
		if( OnLateUpdate != null )
			OnLateUpdate();
	}
}