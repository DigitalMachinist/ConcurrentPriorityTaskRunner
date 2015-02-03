using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Axon.Utilities;

namespace ConcurrentPriorityTaskRunnerTest
{
	[TestFixture]
	public class ConcurrentPriorityTaskRunnerTest
	{
		#region Instance members
		
		[Test]
		public void PropertyHasEnqueuedCriticalTasks()
		{
			// Create a new task runner.
			ConcurrentPriorityTaskRunner taskRunner = new ConcurrentPriorityTaskRunner();

			// Set the minimum critical priority.
			taskRunner.MinCriticalPriority = 100.0;

			// Ensure that HasEnqueuedCriticalTasks returns false.
			Assert.That( taskRunner.HasEnqueuedCriticalTasks, Is.False );

			// Enqueue a task with less priority than the minimum (a non-critical task).
			taskRunner.Enqueue( 50.0, ( x ) => {} );

			// Ensure that HasEnqueuedCriticalTasks returns false.
			Assert.That( taskRunner.HasEnqueuedCriticalTasks, Is.False );

			// Enqueue a task with more priority than the minimum (a critical task).
			taskRunner.Enqueue( 150.0, ( x ) => {} );

			// Ensure that HasEnqueuedCriticalTasks returns false.
			Assert.That( taskRunner.HasEnqueuedCriticalTasks, Is.True );

			// Clear all of the tasks from the queue.
			taskRunner.Clear();

			// Ensure that HasEnqueuedCriticalTasks returns false.
			Assert.That( taskRunner.HasEnqueuedCriticalTasks, Is.False );
		}


		[Test]
		public void PropertyHasEnqueuedNoncriticalTasks()
		{
			// Create a new task runner.
			ConcurrentPriorityTaskRunner taskRunner = new ConcurrentPriorityTaskRunner();

			// Set the minimum critical priority.
			taskRunner.MinCriticalPriority = 100.0;

			// Ensure that HasEnqueuedNoncriticalTasks returns false.
			Assert.That( taskRunner.HasEnqueuedNoncriticalTasks, Is.False );

			// Enqueue a task with more priority than the minimum (a non-critical task).
			taskRunner.Enqueue( 150.0, ( x ) => {} );

			// Ensure that HasEnqueuedNoncriticalTasks returns false.
			Assert.That( taskRunner.HasEnqueuedNoncriticalTasks, Is.False );

			// Enqueue a task with less priority than the minimum (a critical task).
			taskRunner.Enqueue( 50.0, ( x ) => {} );

			// Ensure that HasEnqueuedNoncriticalTasks returns false.
			Assert.That( taskRunner.HasEnqueuedNoncriticalTasks, Is.True );

			// Clear all of the tasks from the queue.
			taskRunner.Clear();

			// Ensure that HasEnqueuedNoncriticalTasks returns false.
			Assert.That( taskRunner.HasEnqueuedNoncriticalTasks, Is.False );
		}


		[Test]
		public void PropertyHasRunningCriticalTasks()
		{
			// Create a new task runner.
			ConcurrentPriorityTaskRunner taskRunner = new ConcurrentPriorityTaskRunner();

			// Set the minimum critical priority and start the task runner.
			taskRunner.MinCriticalPriority = 100.0;

			// Ensure that HasRunningCriticalTasks returns false.
			Assert.That( taskRunner.HasRunningCriticalTasks, Is.False );

			//// This handler will run when the task runner starts.
			//Action onStarted = null;
			//onStarted = () => {

			//	// Enqueue a task with less priority than the minimum (a non-critical task).
			//	taskRunner.Enqueue( 50.0, ( x ) => { Thread.Sleep( 10 ); } );

			//	// Ensure that HasRunningCriticalTasks still returns false.
			//	Assert.That( taskRunner.HasRunningCriticalTasks, Is.False );

			//};
			//taskRunner.Started += onStarted;
			//taskRunner.Start();
			
			//// This handler will run after the above task completes.
			//Action onFirstComplete = null;
			//onFirstComplete = () => {

			//	// Enqueue a task with more priority than the minimum (a critical task).
			//	taskRunner.Enqueue( 150.0, ( x ) => { Thread.Sleep( 10 ); } );

			//	// Ensure that HasRunningCriticalTasks returns false.
			//	Assert.That( taskRunner.HasRunningCriticalTasks, Is.True );

			//	// Remove this listener.
			//	taskRunner.AllTasksDone -= onFirstComplete;
			//};
			//taskRunner.AllTasksDone += onFirstComplete;

			//// This handler will run after the above task completes.
			//Action onSecondComplete = null;
			//onSecondComplete = () => {

			//	// Enqueue a task with more priority than the minimum (a critical task).
			//	taskRunner.Enqueue( 150.0, ( x ) => { Thread.Sleep( 10 ); } );

			//	// Ensure that HasRunningCriticalTasks returns false.
			//	Assert.That( taskRunner.HasRunningCriticalTasks, Is.True );
				
			//	// Remove this listener.
			//	taskRunner.AllTasksDone -= onSecondComplete;
			//};
			//taskRunner.AllTasksDone += onSecondComplete;

			//// This handler will run after the above task completes.
			//Action onThirdComplete = null;
			//onThirdComplete = () => {

			//	// Ensure that HasRunningCriticalTasks returns false.
			//	Assert.That( taskRunner.HasRunningCriticalTasks, Is.False );

			//	// Stop the task runner.
			//	taskRunner.Stop();

			//};
			//taskRunner.AllTasksDone += onThirdComplete;

			// Wait for the task runner to stop on this thread.
			while ( !taskRunner.IsStopped )
			{
				Console.Write( "|" );
				Thread.Sleep( 0 );
			}

			Console.WriteLine( " DONE!" );
		}


		[Test]
		public void PropertyHasRunningNoncriticalTasks()
		{
			Assert.Fail();
		}


		[Test]
		public void PropertyHasWaitingCriticalTasks()
		{
			Assert.Fail();
		}


		[Test]
		public void PropertyHasWaitingNoncriticalTasks()
		{
			Assert.Fail();
		}


		[Test]
		public void PropertyIsRunning()
		{
			Assert.Fail();
		}


		[Test]
		public void PropertyIsStopped()
		{
			Assert.Fail();
		}


		[Test]
		public void PropertyIsStopping()
		{
			Assert.Fail();
		}


		[Test]
		public void PropertyMinCriticalPriority()
		{
			Assert.Fail();
		}


		[Test]
		public void PropertyRunningCriticalTasks()
		{
			Assert.Fail();
		}


		[Test]
		public void PropertyRunningNoncriticalTasks()
		{
			Assert.Fail();
		}


		[Test]
		public void PropertyRunningTasks()
		{
			Assert.Fail();
		}


		[Test]
		public void PropertyThreadPoolMaxThreads()
		{
			Assert.Fail();
		}


		[Test]
		public void PropertyWaitingCriticalTasks()
		{
			Assert.Fail();
		}


		[Test]
		public void PropertyWaitingNoncriticalTasks()
		{
			Assert.Fail();
		}


		[Test]
		public void PropertyWaitingTasks()
		{
			Assert.Fail();
		}

		#endregion


		#region Constructors

		[Test]
		public void Constructor()
		{
			Assert.Fail();
		}


		[Test]
		public void ConstructorInitialSize()
		{
			Assert.Fail();
		}

		#endregion


		#region Public methods

		[Test]
		public void Clear()
		{
			Assert.Fail();
		}


		[Test]
		public void Enqueue()
		{
			Assert.Fail();
		}


		[Test]
		public void Start()
		{
			Assert.Fail();
		}


		[Test]
		public void Stop()
		{
			Assert.Fail();
		}

		#endregion


		#region Private methods

		[Test]
		public void onCompletedTask()
		{
			Assert.Fail();
		}


		[Test]
		public void onRunningTask()
		{
			Assert.Fail();
		}


		[Test]
		public void onWaitingTask()
		{
			Assert.Fail();
		}


		[Test]
		public void ThreadProc()
		{
			Assert.Fail();
		}

		#endregion
	}
}
