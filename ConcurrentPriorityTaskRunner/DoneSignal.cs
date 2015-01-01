using System;
using System.Collections.Generic;
using System.Threading;

namespace Axon.Utilities
{
	private class DoneSignal
	{
		#region Instance members

		/// <summary>
		/// 
		/// </summary>
		private bool __isDone;


		/// <summary>
		/// 
		/// </summary>
		public bool IsDone
		{
			get { return __isDone; }
		}


		/// <summary>
		/// 
		/// </summary>
		private ManualResetEvent __doneEvent;


		/// <summary>
		/// 
		/// </summary>
		public ManualResetEvent DoneEvent
		{
			get { return __doneEvent; }
		}

		#endregion

		#region Constructors

		/// <summary>
		/// 
		/// </summary>
		public DoneSignal()
		{
			__isDone = true;
			__doneEvent = new ManualResetEvent( true );
		}

		#endregion

		#region Public methods

		/// <summary>
		/// 
		/// </summary>
		public void Set()
		{
			__isDone = false;
			if ( __doneEvent != null )
			{
				__doneEvent.Set();
			}
		}


		/// <summary>
		/// 
		/// </summary>
		public void Reset()
		{
			__isDone = true;
			if ( __doneEvent != null )
			{
				__doneEvent.Reset();
			}
		}

		#endregion
	}
}
