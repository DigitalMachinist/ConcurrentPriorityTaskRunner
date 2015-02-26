using System;
using System.Collections.Generic;
using System.Threading;
using ca.axoninteractive.Collections;

namespace ca.axoninteractive.Utilities
{
	/// <summary>
	/// An enumeration of the possible operating states that the ConcurrentPriorityTaskRunner can 
	/// belong to.
	/// </summary>
	enum 
	TaskRunnerState
	{
		Running, Starting, Stopped, Stopping
	}


	/// <summary>
	/// A thread pooling task runner that executes tasks in parallel with user-specified priority. 
	/// Higher-priority tasks will always begin execution before lower-priority tasks. Tasks with 
	/// the same priority will begin execution in the same order as they were enqueued (FIFO).
	/// </summary>
    public 
	class 
	ConcurrentPriorityTaskRunner
	{
		#region Events

		/// <summary>
		/// An event to signal the completion of all tasks in the queue. Runs after the 
		/// CriticalTasksCompleted event is called for the triggering task completion.
		/// </summary>
		public event Action AllTasksCompleted;


		/// <summary>
		/// An event to signal the completion of all critical tasks in the queue. Runs after the 
		/// TaskCompleted event is called for the triggering task completion.
		/// </summary>
		public event Action CriticalTasksCompleted;
		

		/// <summary>
		/// An event to signal that all tasks in the queue have started execution. Runs after the 
		/// TaskStarted event is called for the triggering task execution.
		/// </summary>
		public event Action QueueEmpty;


		/// <summary>
		/// An event to signal that the task runner started (is now in the Running state).
		/// </summary>
		public event Action Started;


		/// <summary>
		/// An event to signal that the task runner stopped (completed all critical tasks and 
		/// exited the primary ThreadProc() loop).
		/// </summary>
		public event Action Stopped;

		
		/// <summary>
		/// An event to signal that a task has finished executing.
		/// </summary>
		public event Action TaskCompleted;

		
		/// <summary>
		/// An event to signal that a task has been enqueued, but has not yet begun executing.
		/// </summary>
		public event Action TaskEnqueued;

		
		/// <summary>
		/// An event to signal that a task has begun executing.
		/// </summary>
		public event Action TaskStarted;

		#endregion


		#region Instance members

		/// <summary>
		/// A concurrent priority queue used to manage and order all of the tasks to be run.
		/// </summary>
		private ConcurrentPriorityQueue<Task> __queue;


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
		/// The current operating state of the task runner.
		/// </summary>
		private volatile TaskRunnerState __state;


		/// <summary>
		/// The thread the task runner's ThreadProc() method executes under.
		/// </summary>
		private Thread __thread;


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
		/// Indicates whether the queue still contains any non-critical tasks to be run or not.
		/// </summary>
		public bool HasEnqueuedNoncriticalTasks {
			get { return ( __waitingNoncriticalTasks - __runningNoncriticalTasks ) > 0; }
		}


		/// <summary>
		/// Indicates whether there are any critical tasks currently being executed.
		/// </summary>
		public bool HasRunningCriticalTasks {
			get { return __runningCriticalTasks > 0; }
		}


		/// <summary>
		/// Indicates whether there are any non-critical tasks currently being executed.
		/// </summary>
		public bool HasRunningNoncriticalTasks {
			get { return __runningNoncriticalTasks > 0; }
		}


		/// <summary>
		/// Indicates whether there are any critical tasks still waiting to complete.
		/// </summary>
		public bool HasWaitingCriticalTasks {
			get { return __waitingCriticalTasks > 0; }
		}


		/// <summary>
		/// Indicates whether there are any non-critical tasks still waiting to complete.
		/// </summary>
		public bool HasWaitingNoncriticalTasks {
			get { return __waitingNoncriticalTasks > 0; }
		}


		/// <summary>
		/// Indicates whether or not the state of the task runner is currently set to Running.
		/// </summary>
		public bool IsRunning {
			get { return __state == TaskRunnerState.Running; }
		}


		/// <summary>
		/// Indicates whether or not the state of the task runner is currently set to Starting.
		/// </summary>
		public bool IsStarting {
			get { return __state == TaskRunnerState.Starting; }
		}


		/// <summary>
		/// Indicates whether or not the state of the task runner is currently set to Stopped.
		/// </summary>
		public bool IsStopped {
			get { return __state == TaskRunnerState.Stopped; }
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
		public double MinCriticalPriority { get; set; }


		/// <summary>
		/// The total number of critical tasks currently being executed.
		/// </summary>
		public int RunningCriticalTasks {
			get { return __runningCriticalTasks; }
		}


		/// <summary>
		/// The total number of non-critical tasks currently being executed.
		/// </summary>
		public int RunningNoncriticalTasks {
			get { return __runningNoncriticalTasks; }
		}


		/// <summary>
		/// The total number of tasks currently being executed.
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
		/// The total number of critical tasks currently waiting to be completed.
		/// </summary>
		public int WaitingCriticalTasks {
			get { return __waitingCriticalTasks; }
		}


		/// <summary>
		/// The total number of non-critical tasks currently waiting to be completed.
		/// </summary>
		public int WaitingNoncriticalTasks {
			get { return __waitingNoncriticalTasks; }
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
		public
		ConcurrentPriorityTaskRunner()
		{
			__queue = new ConcurrentPriorityQueue<Task>();
			__state = TaskRunnerState.Stopped;
			__thread = null;
			__runningCriticalTasks = 0;
			__runningNoncriticalTasks = 0;
			__waitingCriticalTasks = 0;
			__waitingNoncriticalTasks = 0;
			__runningCriticalTasksLock = new object();
			__runningNoncriticalTasksLock = new object();
			__waitingCriticalTasksLock = new object();
			__waitingNoncriticalTasksLock = new object();
		}

		#endregion


		#region Public methods

		/// <summary>
		/// Clear the task runner of all enqueued tasks.
		/// </summary>
		public
		void
		Clear()
		{
			// Clear the priority queue.
			__queue.Clear();

			// Lock the thread -- CRITICAL SECTION BEGIN
			Monitor.Enter( __runningCriticalTasksLock );
			try
			{
				__runningCriticalTasks = 0;
			}
			finally
			{
				Monitor.Exit( __runningCriticalTasksLock );
				// Unlock the thread -- CRITICAL SECTION END
			}

			// Lock the thread -- CRITICAL SECTION BEGIN
			Monitor.Enter( __runningNoncriticalTasksLock );
			try
			{
				__runningNoncriticalTasks = 0;
			}
			finally
			{
				Monitor.Exit( __runningNoncriticalTasksLock );
				// Unlock the thread -- CRITICAL SECTION END
			}
			
			// Lock the thread -- CRITICAL SECTION BEGIN
			Monitor.Enter( __waitingCriticalTasksLock );
			try
			{
				__waitingCriticalTasks = 0;
			}
			finally
			{
				Monitor.Exit( __waitingCriticalTasksLock );
				// Unlock the thread -- CRITICAL SECTION END
			}

			// Lock the thread -- CRITICAL SECTION BEGIN
			Monitor.Enter( __waitingNoncriticalTasksLock );
			try
			{
				__waitingNoncriticalTasks = 0;
			}
			finally
			{
				Monitor.Exit( __waitingNoncriticalTasksLock );
				// Unlock the thread -- CRITICAL SECTION END
			}
		}

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
		Enqueue( double priority, WaitCallback callback, object context = null )
		{
			if ( IsStopping )
			{
				// We want to block new tasks from being queued while the task runner is trying to
				// shut down, so that it can successfully stop instead of being bogged down by new
				// tasks forever.
				throw new InvalidOperationException( "Cannot enqueue tasks while the task runner is stopping." );
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
			if ( !IsStopped )
			{
				// If the task runner isn't Stopped, we don't want to launch another thread and 
				// maniuplate the state unnecessarily. That would be an error.
				throw new InvalidOperationException( "Cannot start a task runner that is not stopped." );
			}

			// Set the state to Starting.
			__state = TaskRunnerState.Starting;

			// Launch a thread to handle executing tasks on the thread pool.
			__thread = new Thread( ThreadProc );
			__thread.Start();
		}


		/// <summary>
		/// Change the state of the task runner to Stopping so it can safely return to Idle state.
		/// </summary>
		public 
		void 
		Stop()
		{
			if ( !IsRunning )
			{
				// If the task runner isn't Stopped, we don't want to maniuplate  the state. That 
				// would be an error.
				throw new InvalidOperationException( "Cannot stop a task runner that is not running." );
			}

			// Transition to Stopping state to cause threadProc() to wind down.
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
			#region Decrement counters

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
				}
				finally
				{
					Monitor.Exit( __waitingNoncriticalTasksLock );
					// Unlock the thread -- CRITICAL SECTION END
				}
			}

			#endregion
			
			#region Emit events

			// Emit an event to notify listeners that a task has finished executing.
			if ( TaskCompleted != null )
			{
				TaskCompleted();
			}

			// Emit an event to notify listeners that all critical tasks have finished 
			// executing (if so).
			if ( __waitingCriticalTasks <= 0 )
			{
				if ( CriticalTasksCompleted != null )
				{
					CriticalTasksCompleted();
				}
			}

			// Emit an event to notify listeners that all tasks have finished executing 
			// (if so).
			if ( __waitingCriticalTasks <= 0 && __waitingNoncriticalTasks <= 0 )
			{
				if ( AllTasksCompleted != null )
				{
					AllTasksCompleted();
				}
			}

			#endregion
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
			#region Increment counters

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

			#endregion
		
			#region Emit events
			
			// Emit an event to notify listeners that a task has begun executing.
			if ( TaskStarted != null )
			{
				TaskStarted();
			}

			// Emit an event to notify listeners that the queue is empty (if so).
			if ( QueueEmpty != null && __queue.Peek() != null )
			{
				QueueEmpty();
			}

			#endregion
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
			#region Increment counters

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

			#endregion
		
			#region Emit events
			
			// Emit an event to notify listeners that a task has been enqueued.
			if ( TaskEnqueued != null )
			{
				TaskEnqueued();
			}

			#endregion
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

			// Emit an event to notify listeners that the task runner started up.
			if ( Started != null )
			{
				Started();
			}

			// Continue looping until Stop() is called and all critical tasks are finished.
			while ( !IsStopping || IsStopping && HasEnqueuedCriticalTasks ) 
			{
				// Most of this is only important if something is in the queue.
				if ( RunningTasks < ThreadPoolMaxThreads && __queue.Peek() != null )
				{
					// Execute the task on the thread pool.
					PriorityValuePair<Task> elem = __queue.Dequeue();
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
				else
				{
					// Yield the rest of the time slice.
					Thread.Sleep( 0 );
				}
			}

			// Wait for all critical tasks to finish running before stopping.
			while ( HasWaitingCriticalTasks )
			{
				Thread.Sleep( 0 );
			}

			// Flag the task runner as stopped.
			__state = TaskRunnerState.Stopped;
			__thread = null;

			// Emit an event to notify listeners that the task runner stopped.
			if ( Stopped != null )
			{
				Stopped();
			}
		}

		#endregion
	}
}
