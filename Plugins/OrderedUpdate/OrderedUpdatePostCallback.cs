using UnityEngine;

public class OrderedUpdatePostCallback : MonoBehaviour 
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