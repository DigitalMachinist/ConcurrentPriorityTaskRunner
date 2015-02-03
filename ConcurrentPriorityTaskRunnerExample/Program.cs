using System;
using System.Threading;
using Axon.Utilities;

namespace ConcurrentPriorityTaskRunnerExample
{
	class Program
	{
		static void Main(string[] args)
		{
			ConcurrentPriorityTaskRunner taskRunner = new ConcurrentPriorityTaskRunner();

			// Enqueue a bunch of tasks.
			taskRunner.Enqueue( 1000.0, ( context ) => { Thread.Sleep( 1000 ); Console.WriteLine( "This is a unique phrase." );						} );
			taskRunner.Enqueue( 1000.0, ( context ) => { Thread.Sleep( 1000 ); Console.WriteLine( "So is this." );									} );
			taskRunner.Enqueue( 1000.0, ( context ) => { Thread.Sleep( 1000 ); Console.WriteLine( "What about this one?" );							} );
			taskRunner.Enqueue( 1000.0, ( context ) => { Thread.Sleep( 1000 ); Console.WriteLine( "Holy bujeezus!" );								} );
			taskRunner.Enqueue( 1000.0, ( context ) => { Thread.Sleep( 1000 ); Console.WriteLine( "Could there be more?" );							} );
			taskRunner.Enqueue( 1000.0, ( context ) => { Thread.Sleep( 1000 ); Console.WriteLine( "Whaaaaaaaaaaat!?" );								} );
			taskRunner.Enqueue( 1000.0, ( context ) => { Thread.Sleep( 1000 ); Console.WriteLine( "I think that about covers it." );				} );
			taskRunner.Enqueue( 1000.0, ( context ) => { Thread.Sleep( 1000 ); Console.WriteLine( "More!" );										} );
			taskRunner.Enqueue( 1000.0, ( context ) => { Thread.Sleep( 1000 ); Console.WriteLine( "MOAR!" );										} );
			taskRunner.Enqueue( 1000.0, ( context ) => { Thread.Sleep( 1000 ); Console.WriteLine( "M00004444RRRRRR!" );								} );
			taskRunner.Enqueue( 1000.0, ( context ) => { Thread.Sleep( 1000 ); Console.WriteLine( "w4t!" );											} );
			taskRunner.Enqueue( 1000.0, ( context ) => { Thread.Sleep( 1000 ); Console.WriteLine( "Seriously." );									} );
			taskRunner.Enqueue( 1000.0, ( context ) => { Thread.Sleep( 1000 ); Console.WriteLine( "There's no way this many can run at once." );	} );

			// Set up an handler for the Stopped event
			taskRunner.Stopped += () => {
				Console.WriteLine( "" );
				Console.WriteLine( "Stopped!" );
			};

			// Start the task runner.
			Console.WriteLine( "Starting..." );
			Console.WriteLine( "" );
			taskRunner.Start();

			// Wait for a short time and then try to stop the task runner.
			Thread.Sleep( 5000 );
			Console.WriteLine( "" );
			Console.WriteLine( "Stopping..." );
			taskRunner.Stop();

			Console.ReadLine();
		}
	}
}
