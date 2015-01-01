using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Axon.Collections;

namespace Axon.Utilities
{
	/// <summary>
	/// An enumeration of the possible operating states that the ConcurrentPriorityTaskRunner can 
	/// belong to.
	/// </summary>
	private enum TaskRunnerState
	{
		Idle, Running, Stopping
	}

	/// <summary>
	/// A thread pooling task runner that executes tasks in parallel with user-specified priority. 
	/// Higher-priority tasks will always begin execution before lower-priority tasks.
	/// </summary>
    public class ConcurrentPriorityTaskRunner
	{
		#region Instance members

		/// <summary>
		/// An array of DoneSignals that tracks the completion of tasks being run by the task 
		/// runner on the thread pool.
		/// </summary>
		private DoneSignal[] __doneSignals;


		/// <summary>
		/// Indicates whether the queue still contains any critical tasks to be run or not.
		/// </summary>
		private bool __hasCriticalTasks;


		/// <summary>
		/// A concurrent priority queue used to manage and order all of the tasks to be run.
		/// </summary>
		private ConcurrentPriorityQueue<Task> __queue;


		/// <summary>
		/// The current operating state of the task runner.
		/// </summary>
		private TaskRunnerState __state;


		/// <summary>
		/// An object used exclusively for maintaining synchronization with some outside thread or 
		/// process (typically the main Unity thread in my case).
		/// </summary>
		private object __synchronizationMonitor;


		/// <summary>
		/// Indicates whether the queue still contains any critical tasks to be run or not.
		/// </summary>
		public bool HasCriticalTasks
		{
			get { return __hasCriticalTasks; }
		}


		/// <summary>
		/// Indicates whether or not the state of the task runner is currently set to Idle.
		/// </summary>
		public bool IsIdle
		{
			get { return __state == TaskRunnerState.Idle; }
		}


		/// <summary>
		/// Indicates whether or not the state of the task runner is currently set to Running.
		/// </summary>
		public bool IsRunning
		{
			get { return __state == TaskRunnerState.Running; }
		}


		/// <summary>
		/// Indicates whether or not the state of the task runner is currently set to Stopping.
		/// </summary>
		public bool IsStopping 
		{
			get { return __state == TaskRunnerState.Stopping; }
		}


		/// <summary>
		/// Specifies the minimum priority for a task that is considered to be a critical task.
		/// </summary>
		public float MinCriticalPriority { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Create a new default ConcurrentPriorityTaskRunner.
		/// </summary>
		public ConcurrentPriorityTaskRunner()
		{
			__queue = new ConcurrentPriorityQueue<Task>();
		}


		/// <summary>
		/// Create a new ConcurrentPriorityTaskRunner given the initial capacity of the priority 
		/// queue that implements it internally.
		/// </summary>
		/// <param name="initialCapacity">The initial maximum capacity of the priority queue that 
		/// implements the task runner internally.</param>
		public ConcurrentPriorityTaskRunner( int initialCapacity )
		{
			__queue = new ConcurrentPriorityQueue<Task>( initialCapacity );
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Enqueue a new task to be run.
		/// </summary>
		/// <param name="priority">The priority of this task, where higher-priority tasks will 
		/// always be run before lower-priority tasks.</param>
		/// <param name="context">The execution context object that will be passed to the callback 
		/// when the callback is called by the thread pool.</param>
		/// <param name="callback">A callback function to be executed on a thread pool.</param>
		/// <param name="forceIfStopping">When set, this flag allows the caller to enqueue a task 
		/// even if the task runner is in the process of stopping.</param>
		public 
		void 
		EnqueueTask( float priority, Object context, WaitCallback callback, bool forceIfStopping = false )
		{
			if ( IsStopping && ! forceIfStopping )
			{
				throw new InvalidOperationException( "Cannot enqueue a task while the task runner is stopping (without forceIfStopping = true)." );
			}
			__queue.Enqueue( priority, new Task( context, callback ) );
		}


		/// <summary>
		/// Start the task runner by launching the thread work function.
		/// </summary>
		public 
		void 
		Start()
		{
			if ( IsIdle )
			{
				// Launch a thread to handle executing tasks on the thread pool.
				new Thread( new ThreadStart( ThreadLoop ) );
			}
		}


		/// <summary>
		/// Change the state of the task runner to Stopping so it can safely return to Idle state.
		/// </summary>
		public 
		void 
		Stop()
		{
			__state = TaskRunnerState.Stopping;
		}
		

		/// <summary>
		/// This function allows another thread to wait for synchronization with the task runner. 
		/// Synchronization entails that ALL critical tasks have been completed, although any 
		/// number of non-critical tasks may be enqueued that have not yet be completed.
		/// </summary>
		/// <param name="clearIncompleteTasks">Specifies whether or not any remaining incomplete 
		/// tasks in the queue should be cleared at the time of synchronization.</param>
		public 
		void 
		WaitForSynchronization( bool clearIncompleteTasks )
		{
			try
			{
				Monitor.Enter( __synchronizationMonitor );
			}
			finally
            {
				if ( clearIncompleteTasks )
				{
					__queue.Clear();
				}
                Monitor.Exit( __synchronizationMonitor );
            }
		}

		#endregion

		#region Private methods

		/// <summary>
		/// The function run by the queue management thread initiated by Start(). This sets the 
		/// state to Running and begins executing tasks on a thread pool until the state is set
		/// to Stopping, at which time it will continue to execute critical tasks until they are 
		/// all complete, at which time the state will return to Idle.
		/// </summary>
		#if DEBUG
		public
		#else
		private 
		#endif
		void 
		ThreadLoop()
		{
			// Change state to indicate that the task runner is started.
			__state = TaskRunnerState.Running;

			// Continue looping until Stop() is called and all critical tasks are finished.
			while ( !IsStopping || IsStopping && __hasCriticalTasks ) 
			{
				// Most of this is only important if something is in the queue.
				if ( __queue.Peek() != null )
				{
					// Determine if a thread is available by checking the done signals for 
					// an inactive signal.
					DoneSignal doneSignal = Array.Find( __doneSignals, x => { return x.IsDone; } );

					// If a thread is available, run the next task on the queue.
					if ( doneSignal != null )
					{
						PriorityValuePair<Task> elem = __queue.Dequeue();
						Task task = elem.Value;
						if ( task != null )
						{
							// Check if the current task is a critical task and use that to
							// handle synchronization signaling.
							if ( elem.Priority >= MinCriticalPriority )
							{
								__hasCriticalTasks = true;
								Monitor.Enter( __synchronizationMonitor );
							}
							else
							{
								__hasCriticalTasks = false;
								Monitor.Exit( __synchronizationMonitor );
							}

							// Attach the inactive done signal to the task so we can 
							// monitor when it finishes running.
							task.DoneSignal = doneSignal;

							// Execute the task on the thread pool (Run() handles that).
							task.Run( true );
						}
					}
				}
						
				// Yield the rest of the time slice.
				Thread.Sleep( 0 );
			}

			// Wait for all running tasks to complete.
			ManualResetEvent[] doneEvents = __doneSignals.Select( x => x.DoneEvent ).ToArray();
			WaitHandle.WaitAll( doneEvents );

			// Flag the task runner as stopped.
			__state = TaskRunnerState.Idle;
		} 

		#endregion
	}
}
