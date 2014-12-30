using System;
using System.Collections.Generic;
using System.Threading;
using Axon.Collections;

namespace Axon.Utilities
{

    public class ConcurrentPriorityTaskRunner
	{
		#region Instance members

		/// <summary>
		/// 
		/// </summary>
		private Thread __thread;


		/// <summary>
		/// 
		/// </summary>
		private ConcurrentPriorityQueue< Task<Object> > __queue;


		/// <summary>
		/// 
		/// </summary>
		private bool __isStopping;


		/// <summary>
		/// 
		/// </summary>
		private bool __isStopped;


		/// <summary>
		/// 
		/// </summary>
		public bool IsStopping 
		{
			get { return __isStopping; }
		}


		/// <summary>
		/// 
		/// </summary>
		public bool IsStopped
		{
			get { return __isStopped; }
		}


		/// <summary>
		/// 
		/// </summary>
		public void Stop()
		{
			__isStopping = true;
		}


		/// <summary>
		/// 
		/// </summary>
		public bool IsFinishedCriticalTasks
		{
			get { return ( __queue.PeekPriority() < MinCriticalPriority ); }
		}


		/// <summary>
		/// 
		/// </summary>
		//public bool IgnoreNonCriticalTasks { get; set; }


		/// <summary>
		/// 
		/// </summary>
		public float MinCriticalPriority { get; set; }




		/// <summary>
		/// An object used exclusively for maintaining synchronization with some outside thread or 
		/// process (typically the main Unity thread in my case).
		/// </summary>
		private object __synchronizationMonitor;


		/// <summary>
		/// This function allows another thread to wait for synchronization with the task runner. 
		/// Synchronization entails that ALL critical tasks have been completed, although any 
		/// number of non-critical tasks may be enqueued that have not yet be completed.
		/// </summary>
		/// <param name="clearIncompleteTasks">Specifies whether or not any remaining incomplete 
		/// tasks in the queue should be cleared at the time of synchronization.</param>
		public void WaitForSynchronization( bool clearIncompleteTasks )
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


		public void Start()
		{
			// Launch a thread to handle executing tasks on the thread pool.
			__thread = new Thread( new ThreadStart( 
				() => {
					// Continue looping until Stop() is called and all critical tasks are finished.
					while ( !IsStopping && !IsFinishedCriticalTasks ) 
					{
						// Most of this is only important if something is in the queue.
						if ( __queue.Peek() != null )
						{
							// Get the number of threads currently available to the thread pool.
							// Note: unused is not used for anything, but is necessary as an out 
							// parameter for ThreadPool.GetAvailableThreads() as it requires 2 
							// function arguments.
							int numAvailableThreads, unused;
							ThreadPool.GetAvailableThreads( out numAvailableThreads, out unused );

							// If a thread is available, run the next task.
							if ( numAvailableThreads > 0 )
							{
								Task<Object> task = __queue.DequeueValue();
								if ( task != null )
								{
									// Execute the task on the thread pool (Run() handles that).
									task.Run( true );
								}
							}
						}
						
						// Yield the rest of the time slice.
						Thread.Sleep( 0 );
					}
				} 
			) );

			// Flag the task runner as stopped
			__isStopped = true;
		}

		#endregion


		#region Constructors



		#endregion
	}
}
