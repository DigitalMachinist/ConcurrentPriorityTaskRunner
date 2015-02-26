using System;
using System.Threading;

namespace ca.axoninteractive.Utilities
{
	/// <summary>
	/// A Task represents a single work item to be executed by the ConcurrentPriorityTaskRunner.
	/// </summary>
	public
	class 
	Task
	{
		#region Instance members

		/// <summary>
		/// A function delegate which will be passed the context when called.
		/// </summary>
		public WaitCallback Callback { get; set; }


		/// <summary>
		/// A generically-typed object providing the execution context for the callback.
		/// </summary>
		public object Context { get; set; }


		/// <summary>
		/// An event to signal the completion of this task.
		/// </summary>
		public event Action CallbackReturned;

		#endregion


		#region Constructors

		/// <summary>
		/// Create a new default task.
		/// </summary>
		public Task()
		{
			Context = null;
			Callback = null;
		}

		/// <summary>
		/// Create a new task by supplying the execution context and action.
		/// </summary>
		/// <param name="context">A generically-type context object used by the action.</param>
		/// <param name="action">A function delegate to be called by Run().</param>
		public Task( object context, WaitCallback callback )
		{
			Context = context;
			Callback = callback;
		}

		#endregion


		#region Public methods

		/// <summary>
		/// Executes the Callback in a ThreadPool and passes Context into it as the only parameter.
		/// </summary>
		public void Run()
		{
			if ( Callback == null )
			{
				throw new InvalidOperationException( "Cannot Run() a task with a null Callback." );
			}
			Callback( Context );
			if ( CallbackReturned != null ) 
			{
				CallbackReturned();
			}
		}

		#endregion
	}
}