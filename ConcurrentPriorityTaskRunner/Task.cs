using System;
using System.Collections.Generic;
using System.Threading;

namespace Axon.Utilities
{
	/// <summary>
	/// A Task represents a single work item to be executed by the ConcurrentPriorityTaskRunner.
	/// </summary>
	/// <typeparam name="T">The data type of the execution context object.</typeparam>
	public class Task<T>
	{
		#region Instance members

		/// <summary>
		/// A function delegate which will be passed the context when called.
		/// </summary>
		public WaitCallback Callback { get; set; }


		/// <summary>
		/// A generically-typed object providing the execution context for the callback.
		/// </summary>
		public T Context { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Create a new default task.
		/// </summary>
		public Task()
		{
			Context = default( T );
			Callback = null;
		}

		/// <summary>
		/// Create a new task by supplying the execution context and action.
		/// </summary>
		/// <param name="context">A generically-type context object used by the action.</param>
		/// <param name="action">A function delegate to be called by Run().</param>
		public Task( WaitCallback callback, T context )
		{
			Callback = callback;
			Context = context;
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Executes the Callback in a ThreadPool and passes Context into it as the only parameter.
		/// </summary>
		public void Run( bool failSilently = false )
		{
			if ( Callback == null )
			{
				if ( failSilently )
				{
					return;
				}
				throw new InvalidOperationException( "Cannot Run() a task with a null Callback." );
			}
			ThreadPool.QueueUserWorkItem( Callback, Context );
		}

		#endregion
	}
}