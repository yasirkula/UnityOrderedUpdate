using System.Collections.Generic;
using UnityEngine;

public static class OrderedUpdate
{
	#region Delegates
	public delegate void UpdateCallback();
	#endregion

	#region Inner Classes
	private class UpdateNode
	{
		public int order;
		public List<UpdateCallback> receivers;
		public UpdateNode next;

		public UpdateNode( int order, UpdateCallback receiver, UpdateNode next )
		{
			this.order = order;

			receivers = new List<UpdateCallback>();
			receivers.Add( receiver );

			this.next = next;
		}

		public void Populate( int order, UpdateCallback receiver, UpdateNode next )
		{
			this.order = order;

			receivers = new List<UpdateCallback>();
			receivers.Add( receiver );

			this.next = next;
		}
	}
	#endregion

	#region Constants
	private const int UPDATE_NODE_POOL_MAX_SIZE = 8;
	#endregion

	#region Variables
	private static UpdateNode preUpdateNode = null;
	private static UpdateNode postUpdateNode = null;
	private static UpdateNode preFixedUpdateNode = null;
	private static UpdateNode postFixedUpdateNode = null;
	private static UpdateNode preLateUpdateNode = null;
	private static UpdateNode postLateUpdateNode = null;

	private static UpdateNode[] updateNodesPool;
	private static int updateNodesPoolCount;

	private static OrderedUpdatePreCallback preCallbackBroadcaster;
	private static OrderedUpdatePostCallback postCallbackBroadcaster;
	#endregion

	#region Static Constructor
	static OrderedUpdate()
	{
		GameObject Instance = new GameObject( "OrderedUpdate" );

		preCallbackBroadcaster = Instance.AddComponent<OrderedUpdatePreCallback>();
		postCallbackBroadcaster = Instance.AddComponent<OrderedUpdatePostCallback>();

		Object.DontDestroyOnLoad( Instance );

		updateNodesPool = new UpdateNode[UPDATE_NODE_POOL_MAX_SIZE];
		updateNodesPoolCount = 0;
	}
	#endregion

	#region Shorthand Functions
	/// <summary>Shorthand for [Add/Remove]UpdateReceiver( function, -2000 ). Executed before Update calls.</summary>
	public static event UpdateCallback OnPreUpdate
	{
		add { AddUpdateReceiver( value, -2000 ); }
		remove { RemoveUpdateReceiver( value, -2000 ); }
	}

	/// <summary>Shorthand for [Add/Remove]UpdateReceiver( function ). Executed right after Update calls.</summary>
	public static event UpdateCallback OnUpdate
	{
		add { AddUpdateReceiver( value ); }
		remove { RemoveUpdateReceiver( value ); }
	}

	/// <summary>Shorthand for [Add/Remove]FixedUpdateReceiver( function ). Executed right after FixedUpdate calls.</summary>
	public static event UpdateCallback OnFixedUpdate
	{
		add { AddFixedUpdateReceiver( value ); }
		remove { RemoveFixedUpdateReceiver( value ); }
	}

	/// <summary>Shorthand for [Add/Remove]LateUpdateReceiver( function ). Executed right after LateUpdate calls.</summary>
	public static event UpdateCallback OnLateUpdate
	{
		add { AddLateUpdateReceiver( value ); }
		remove { RemoveLateUpdateReceiver( value ); }
	}
	#endregion

	#region Callback Register Functions
	/// <summary>Calls a function every frame. If <paramref name="order"/> >= 0, function is called after Update; otherwise it is called before Update.</summary>
	/// <param name="receiver">Function to call</param>
	/// <param name="order">Determines the execution order of the function. A function with a smaller order is executed before a function with a larger order.
	/// </param>
	public static void AddUpdateReceiver( UpdateCallback receiver, int order = 0 )
	{
		if( order < 0 )
		{
			if( preUpdateNode == null )
			{
				preUpdateNode = FetchUpdateNode( receiver, order, null );
				preCallbackBroadcaster.OnUpdate = OnPreUpdateCall;
			}
			else
			{
				preUpdateNode = AddReceiverInternal( receiver, order, preUpdateNode );
			}
		}
		else
		{
			if( postUpdateNode == null )
			{
				postUpdateNode = FetchUpdateNode( receiver, order, null );
				postCallbackBroadcaster.OnUpdate = OnPostUpdateCall;
			}
			else
			{
				postUpdateNode = AddReceiverInternal( receiver, order, postUpdateNode );
			}
		}
	}

	/// <summary>Calls a function every fixed framerate frame. If <paramref name="order"/> >= 0, function is called after FixedUpdate; otherwise it is called before FixedUpdate.</summary>
	/// <param name="receiver">Function to call</param>
	/// <param name="order">Determines the execution order of the function. A function with a smaller order is executed before a function with a larger order.
	/// </param>
	public static void AddFixedUpdateReceiver( UpdateCallback receiver, int order = 0 )
	{
		if( order < 0 )
		{
			if( preFixedUpdateNode == null )
			{
				preFixedUpdateNode = FetchUpdateNode( receiver, order, null );
				preCallbackBroadcaster.OnFixedUpdate = OnPreFixedUpdateCall;
			}
			else
			{
				preFixedUpdateNode = AddReceiverInternal( receiver, order, preFixedUpdateNode );
			}
		}
		else
		{
			if( postFixedUpdateNode == null )
			{
				postFixedUpdateNode = FetchUpdateNode( receiver, order, null );
				postCallbackBroadcaster.OnFixedUpdate = OnPostFixedUpdateCall;
			}
			else
			{
				postFixedUpdateNode = AddReceiverInternal( receiver, order, postFixedUpdateNode );
			}
		}
	}

	/// <summary>Calls a function every frame. If <paramref name="order"/> >= 0, function is called after LateUpdate; otherwise it is called before LateUpdate.</summary>
	/// <param name="receiver">Function to call</param>
	/// <param name="order">Determines the execution order of the function. A function with a smaller order is executed before a function with a larger order.
	/// </param>
	public static void AddLateUpdateReceiver( UpdateCallback receiver, int order = 0 )
	{
		if( order < 0 )
		{
			if( preLateUpdateNode == null )
			{
				preLateUpdateNode = FetchUpdateNode( receiver, order, null );
				preCallbackBroadcaster.OnLateUpdate = OnPreLateUpdateCall;
			}
			else
			{
				preLateUpdateNode = AddReceiverInternal( receiver, order, preLateUpdateNode );
			}
		}
		else
		{
			if( postLateUpdateNode == null )
			{
				postLateUpdateNode = FetchUpdateNode( receiver, order, null );
				postCallbackBroadcaster.OnLateUpdate = OnPostLateUpdateCall;
			}
			else
			{
				postLateUpdateNode = AddReceiverInternal( receiver, order, postLateUpdateNode );
			}
		}
	}
	#endregion

	#region Callback Unregister Functions
	/// <summary>Stops calling a function every frame. Parameter '<paramref name="order"/>' must match the value used when calling AddUpdateReceiver.</summary>
	public static void RemoveUpdateReceiver( UpdateCallback receiver, int order = 0 )
	{
		if( order < 0 )
		{
			preUpdateNode = RemoveReceiverInternal( receiver, order, preUpdateNode );
			if( preUpdateNode == null )
				preCallbackBroadcaster.OnUpdate = null;
		}
		else
		{
			postUpdateNode = RemoveReceiverInternal( receiver, order, postUpdateNode );
			if( postUpdateNode == null )
				postCallbackBroadcaster.OnUpdate = null;
		}
	}

	/// <summary>Stops calling a function every fixed framerate frame. Parameter '<paramref name="order"/>' must match the value used when calling AddFixedUpdateReceiver.</summary>
	public static void RemoveFixedUpdateReceiver( UpdateCallback receiver, int order = 0 )
	{
		if( order < 0 )
		{
			preFixedUpdateNode = RemoveReceiverInternal( receiver, order, preFixedUpdateNode );
			if( preFixedUpdateNode == null )
				preCallbackBroadcaster.OnFixedUpdate = null;
		}
		else
		{
			postFixedUpdateNode = RemoveReceiverInternal( receiver, order, postFixedUpdateNode );
			if( postFixedUpdateNode == null )
				postCallbackBroadcaster.OnFixedUpdate = null;
		}
	}

	/// <summary>Stops calling a function every frame. Parameter '<paramref name="order"/>' must match the value used when calling AddLateUpdateReceiver.</summary>
	public static void RemoveLateUpdateReceiver( UpdateCallback receiver, int order = 0 )
	{
		if( order < 0 )
		{
			preLateUpdateNode = RemoveReceiverInternal( receiver, order, preLateUpdateNode );
			if( preLateUpdateNode == null )
				preCallbackBroadcaster.OnLateUpdate = null;
		}
		else
		{
			postLateUpdateNode = RemoveReceiverInternal( receiver, order, postLateUpdateNode );
			if( postLateUpdateNode == null )
				postCallbackBroadcaster.OnLateUpdate = null;
		}
	}
	#endregion

	#region Unity Messages
	private static void OnPreUpdateCall()
	{
		ExecuteNodes( preUpdateNode );
	}

	private static void OnPostUpdateCall()
	{
		ExecuteNodes( postUpdateNode );
	}

	private static void OnPreFixedUpdateCall()
	{
		ExecuteNodes( preFixedUpdateNode );
	}

	private static void OnPostFixedUpdateCall()
	{
		ExecuteNodes( postFixedUpdateNode );
	}

	private static void OnPreLateUpdateCall()
	{
		ExecuteNodes( preLateUpdateNode );
	}

	private static void OnPostLateUpdateCall()
	{
		ExecuteNodes( postLateUpdateNode );
	}
	#endregion

	#region Helper Functions
	private static void ExecuteNodes( UpdateNode head )
	{
		while( head != null )
		{
			List<UpdateCallback> receivers = head.receivers;
			for( int i = receivers.Count - 1; i >= 0; i-- )
			{
				if( receivers[i] != null )
					receivers[i]();
				else
					receivers.RemoveAt( i );
			}

			head = head.next;
		}
	}

	private static UpdateNode AddReceiverInternal( UpdateCallback receiver, int order, UpdateNode head )
	{
		if( head.order < order )
		{
			UpdateNode cachedHead = head;

			UpdateNode next = head.next;
			while( next != null && next.order <= order )
			{
				head = next;
				next = head.next;
			}

			if( head.order < order )
				head.next = FetchUpdateNode( receiver, order, next );
			else
				head.receivers.Add( receiver );

			return cachedHead;
		}

		if( head.order == order )
		{
			head.receivers.Add( receiver );

			return head;
		}

		return FetchUpdateNode( receiver, order, head );
	}

	private static UpdateNode RemoveReceiverInternal( UpdateCallback receiver, int order, UpdateNode head )
	{
		if( head == null || head.order > order )
			return head;

		if( head.order == order )
		{
			if( head.receivers.Remove( receiver ) && head.receivers.Count == 0 )
			{
				UpdateNode result = head.next;
				PoolUpdateNode( head );

				return result;
			}

			return head;
		}

		UpdateNode cachedHead = head;

		UpdateNode next = head.next;
		while( next != null && next.order < order )
		{
			head = next;
			next = head.next;
		}

		if( next != null && next.order == order )
		{
			if( next.receivers.Remove( receiver ) && next.receivers.Count == 0 )
			{
				head.next = next.next;
				PoolUpdateNode( next );
			}
		}

		return cachedHead;
	}
	#endregion

	#region Pool Functions
	private static void PoolUpdateNode( UpdateNode node )
	{
		if( updateNodesPoolCount < UPDATE_NODE_POOL_MAX_SIZE )
		{
			node.next = null;
			updateNodesPool[updateNodesPoolCount++] = node;
		}
	}

	private static UpdateNode FetchUpdateNode( UpdateCallback receiver, int order, UpdateNode next = null )
	{
		if( updateNodesPoolCount > 0 )
		{
			UpdateNode pooledNode = updateNodesPool[--updateNodesPoolCount];
			updateNodesPool[updateNodesPoolCount] = null;

			pooledNode.Populate( order, receiver, next );
			return pooledNode;
		}

		return new UpdateNode( order, receiver, next );
	}
	#endregion
}