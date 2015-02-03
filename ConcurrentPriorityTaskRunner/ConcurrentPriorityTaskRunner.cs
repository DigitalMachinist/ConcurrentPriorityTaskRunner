using System;
using System.Collections.Generic;
using System.Threading;
using Axon.Collections;

namespace Axon.Utilities
{
	/// <summary>
	/// An enumeration of the possible operating states that the ConcurrentPriorityTaskRunner can 
	/// belong to.
	/// </summary>
	enum TaskRunnerState
	{
		Idle, Running, Stopping
	}


	/// <summary>
	/// A thread pooling task runner that executes tasks in parallel with user-specified priority. 
	/// Higher-priority tasks will always begin execution before lower-priority tasks. Tasks with 
	/// the same priority will begin execution in the same order as they were enqueued (FIFO).
	/// </summary>
    class ConcurrentPriorityTaskRunner
	{
		#region Instance members

		/// <summary>
		/// An event to signal the completion of all tasks in the queue.
		/// </summary>
		public event Action AllTasksDone;


		/// <summary>
		/// An event to signal the completion of all critical tasks in the queue.
		/// </summary>
		public event Action CriticalTasksDone;


		/// <summary>
		/// The number of critical tasks currently being executed in the ThreadPool.
		/// </summary>
		private int __runningCriticalTasks;


		/// <summary>
		///	This object exists only as a synchronization lock for __runningCriticalTasks.
		/// </summary>
		private object __runningCriticalTasksLock;


		/// <summary>
		/// The number of non-critical tasks currently being executed in the ThreadPool.
		/// </summary>
		private int __runningNoncriticalTasks;


		/// <summary>
		///	This object exists only as a synchronization lock for __runningNoncriticalTasks.
		/// </summary>
		private object __runningNoncriticalTasksLock;


		/// <summary>
		/// A concurrent priority queue used to manage and order all of the tasks to be run.
		/// </summary>
		private ConcurrentPriorityQueue<Task> __queue;


		/// <summary>
		/// The current operating state of the task runner.
		/// </summary>
		private TaskRunnerState __state;


		/// <summary>
		/// The number of critical tasks currently waiting to be completed.
		/// </summary>
		private int __waitingCriticalTasks;


		/// <summary>
		///	This object exists only as a synchronization lock for __waitingCriticalTasks.
		/// </summary>
		private object __waitingCriticalTasksLock;


		/// <summary>
		/// The number of non-critical currently waiting to be completed.
		/// </summary>
		private int __waitingNoncriticalTasks;


		/// <summary>
		///	This object exists only as a synchronization lock for __waitingNoncriticalTasks.
		/// </summary>
		private object __waitingNoncriticalTasksLock;


		/// <summary>
		/// Indicates whether the queue still contains any critical tasks to be run or not.
		/// </summary>
		public bool HasEnqueuedCriticalTasks {
			get { return ( __waitingCriticalTasks - __runningCriticalTasks ) > 0; }
		}


		/// <summary>
		/// Indicates whether there are any critical tasks still waiting to complete.
		/// </summary>
		public bool HasWaitingCriticalTasks {
			get { return __runningCriticalTasks  > 0; }
		}


		/// <summary>
		/// Indicates whether or not the state of the task runner is currently set to Idle.
		/// </summary>
		public bool IsIdle {
			get { return __state == TaskRunnerState.Idle; }
		}


		/// <summary>
		/// Indicates whether or not the state of the task runner is currently set to Running.
		/// </summary>
		public bool IsRunning {
			get { return __state == TaskRunnerState.Running; }
		}


		/// <summary>
		/// Indicates whether or not the state of the task runner is currently set to Stopping.
		/// </summary>
		public bool IsStopping  {
			get { return __state == TaskRunnerState.Stopping; }
		}


		/// <summary>
		/// Specifies the minimum priority for a task that is considered to be a critical task.
		/// </summary>
		public float MinCriticalPriority { get; set; }


		/// <summary>
		/// The total number of tasks currently waiting to be completed.
		/// </summary>
		public int RunningTasks {
			get { return __runningCriticalTasks + __runningNoncriticalTasks; }
		}


		/// <summary>
		/// Check the murrent maximum number of threads that the ThreadPool can run simultaneously.
		/// </summary>
		private int ThreadPoolMaxThreads {
			get { 
				int workerThreads, unused;
				ThreadPool.GetMaxThreads( out workerThreads, out unused );
				return workerThreads; 
			}
		}


		/// <summary>
		/// The total number of tasks currently waiting to complete.
		/// </summary>
		public int WaitingTasks {
			get { return __waitingCriticalTasks + __waitingNoncriticalTasks; }
		}

		#endregion


		#region Constructors

		/// <summary>
		/// Create a new default ConcurrentPriorityTaskRunner.
		/// </summary>
		public ConcurrentPriorityTaskRunner()
		{
			__queue = new ConcurrentPriorityQueue<Task>();
			__runningCriticalTasks = 0;
			__runningNoncriticalTasks = 0;
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
			__runningCriticalTasks = 0;
			__runningNoncriticalTasks = 0;
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
		public 
		void 
		Enqueue( float priority, Object context, WaitCallback callback )
		{
			if ( IsStopping )
			{
				throw new InvalidOperationException( "Cannot enqueue a task while the task runner is stopping." );
			}

			// Create a new task, wrap it in a PriorityValuePair, and enqueue it.
			Task task = new Task( context, callback );
			PriorityValuePair<Task> elem = new PriorityValuePair<Task>( priority, task );
			__queue.Enqueue( elem );

			// Mark the task as waiting to complete.
			onWaitingTask( elem );

			// Subscribe an event listener to the task's CallbackReturned event that will release 
			// the task resources and allow another to run.
			task.CallbackReturned += () => {
				onCompletedTask( elem );
			};
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
				new Thread( new ThreadStart( ThreadProc ) );
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

		#endregion


		#region Private methods

		/// <summary>
		/// Signal that this task is no longer active nor is it waiting to complete (it's done!).
		/// </summary>
		#if DEBUG
		public
		#else
		private 
		#endif
		void 
		onCompletedTask( PriorityValuePair<Task> elem )
		{
			// Based on its priority, increment the appropriate active counter.
			if ( elem.Priority >= MinCriticalPriority )
			{
				// Lock the thread -- CRITICAL SECTION BEGIN
				Monitor.Enter( __runningCriticalTasksLock );
				try
				{
					__runningCriticalTasks--;
				}
				finally
				{
					Monitor.Exit( __runningCriticalTasksLock );
					// Unlock the thread -- CRITICAL SECTION END
				}
				// Lock the thread -- CRITICAL SECTION BEGIN
				Monitor.Enter( __waitingCriticalTasksLock );
				try
				{
					__waitingCriticalTasks--;
					if ( __waitingCriticalTasks <= 0 )
					{
						__waitingCriticalTasks = 0;
						if ( CriticalTasksDone != null )
						{
							CriticalTasksDone();
						}
					}
				}
				finally
				{
					Monitor.Exit( __waitingCriticalTasksLock );
					// Unlock the thread -- CRITICAL SECTION END
				}
			}
			else
			{
				// Lock the thread -- CRITICAL SECTION BEGIN
				Monitor.Enter( __runningNoncriticalTasksLock );
				try
				{
					__runningNoncriticalTasks--;
				}
				finally
				{
					Monitor.Exit( __runningNoncriticalTasksLock );
					// Unlock the thread -- CRITICAL SECTION END
				}
				// Lock the thread -- CRITICAL SECTION BEGIN
				Monitor.Enter( __waitingNoncriticalTasksLock );
				try
				{
					__waitingNoncriticalTasks--;
					if ( __waitingNoncriticalTasks <= 0 )
					{
						__waitingNoncriticalTasks = 0;
						if ( AllTasksDone != null )
						{
							AllTasksDone();
						}
					}
				}
				finally
				{
					Monitor.Exit( __waitingNoncriticalTasksLock );
					// Unlock the thread -- CRITICAL SECTION END
				}
			}
		}


		/// <summary>
		/// Signal that a task has just begun executing on the ThreadPool.
		/// </summary>
		#if DEBUG
		public
		#else
		private 
		#endif
		void 
		onRunningTask( PriorityValuePair<Task> elem )
		{
			// Based on its priority, increment the appropriate active counter.
			if ( elem.Priority >= MinCriticalPriority )
			{
				// Lock the thread -- CRITICAL SECTION BEGIN
				Monitor.Enter( __runningCriticalTasksLock );
				try
				{
					__runningCriticalTasks++;
				}
				finally
				{
					Monitor.Exit( __runningCriticalTasksLock );
					// Unlock the thread -- CRITICAL SECTION END
				}
			}
			else
			{
				// Lock the thread -- CRITICAL SECTION BEGIN
				Monitor.Enter( __runningNoncriticalTasksLock );
				try
				{
					__runningNoncriticalTasks++;
				}
				finally
				{
					Monitor.Exit( __runningNoncriticalTasksLock );
					// Unlock the thread -- CRITICAL SECTION END
				}
			}
		}


		/// <summary>
		/// Signal that a new task is waiting to be completed.
		/// </summary>
		#if DEBUG
		public
		#else
		private 
		#endif
		void 
		onWaitingTask( PriorityValuePair<Task> elem )
		{
			// Based on its priority, increment the appropriate waiting counter.
			if ( elem.Priority >= MinCriticalPriority )
			{
				// Lock the thread -- CRITICAL SECTION BEGIN
				Monitor.Enter( __waitingCriticalTasksLock );
				try
				{
					__waitingCriticalTasks++;
				}
				finally
				{
					Monitor.Exit( __waitingCriticalTasksLock );
					// Unlock the thread -- CRITICAL SECTION END
				}
			}
			else
			{
				// Lock the thread -- CRITICAL SECTION BEGIN
				Monitor.Enter( __waitingNoncriticalTasksLock );
				try
				{
					__waitingNoncriticalTasks++;
				}
				finally
				{
					Monitor.Exit( __waitingNoncriticalTasksLock );
					// Unlock the thread -- CRITICAL SECTION END
				}
			}
		}


		/// <summary>
		/// Dequeue a task and run it.
		/// </summary>
		#if DEBUG
		public
		#else
		private 
		#endif 
		void 
		RunTask()
		{
			// Lock the thread -- CRITICAL SECTION BEGIN
			Monitor.Enter( __queue );
			try
			{
				PriorityValuePair<Task> elem = __queue.Dequeue();
				if ( elem != null )
				{
					// Execute the task on the thread pool.
					Task task = elem.Value;
					if ( task != null )
					{
						// We're passing a null context here because the task is packaged with its 
						// own execution context that will be inserted during Task.Run().
						ThreadPool.QueueUserWorkItem( ( x ) => { task.Run(); }, null );
					}

					// Mark the task as starting execution.
					// Note: This should run even if the task is null above, otherwise the counts
					// for the number of running/waiting tasks will become skewed.
					onRunningTask( elem );
				}
			}
			finally
			{
				Monitor.Exit( __queue );
				// Unlock the thread -- CRITICAL SECTION END
			}
		}


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
		ThreadProc()
		{
			// Change state to indicate that the task runner is started.
			__state = TaskRunnerState.Running;

			// Continue looping until Stop() is called and all critical tasks are finished.
			while ( !IsStopping || IsStopping && HasEnqueuedCriticalTasks ) 
			{
				// Most of this is only important if something is in the queue.
				if ( RunningTasks < ThreadPoolMaxThreads && __queue.Peek() != null )
				{
					RunTask();
				}
				else
				{
					// Yield the rest of the time slice.
					Thread.Sleep( 0 );
				}
			}

			// Wait for all critical tasks to complete before stopping.
			while ( HasWaitingCriticalTasks )
			{
				Thread.Sleep( 0 );
			}

			// Flag the task runner as stopped.
			__state = TaskRunnerState.Idle;
		}

		#endregion
	}
}
