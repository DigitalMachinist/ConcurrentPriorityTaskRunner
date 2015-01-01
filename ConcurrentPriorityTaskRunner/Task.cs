using System;
using System.Collections.Generic;
using System.Threading;

namespace Axon.Utilities
{
	/// <summary>
	/// A Task represents a single work item to be executed by the ConcurrentPriorityTaskRunner.
	/// </summary>
	/// <typeparam name="T">The data type of the execution context object.</typeparam>
	private class Task
	{
		#region Instance members

		/// <summary>
		/// A function delegate which will be passed the context when called.
		/// </summary>
		public WaitCallback Callback { get; set; }


		/// <summary>
		/// A generically-typed object providing the execution context for the callback.
		/// </summary>
		public Object Context { get; set; }


		/// <summary>
		/// A DoneSignal that can be attached to this Task to allow it to communicate that it
		/// has been completed to the external service that called Run().
		/// </summary>
		public DoneSignal DoneSignal { get; set; }

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
		public Task( Object context, WaitCallback callback )
		{
			Context = context;
			Callback = callback;
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
			ThreadPool.QueueUserWorkItem( 
				( context ) => {
					if ( DoneSignal != null )
					{
						DoneSignal.Set();
					}
					Callback( context );
					if ( DoneSignal != null )
					{
						DoneSignal.Reset();
					}
				}, 
				Context 
			);
		}

		#endregion
	}
}