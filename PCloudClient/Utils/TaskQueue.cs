using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TaskRunner = System.Func<System.Threading.Tasks.Task>;

namespace PCloud
{
	/// <summary>Sequential runner of multiple tasks.</summary>
	/// <remarks>Unlike synchronization contexts, this class doesn't interleave multiple tasks on the same thread. It runs them one by one. The use case is reading data from the same socket, with async methods.</remarks>
	class TaskQueue
	{
		const int defaultQueueDepth = 8;

		/// <summary>Max.count of pending tasks. When exceeded, post method will start to block.</summary>
		readonly int maxQueueDepth;

		/// <summary>Semaphore to enforce concurrency limit</summary>
		readonly SemaphoreSlim throttle;

		/// <summary>Lock that guards both queues below, and <see cref="processorRunning"/> field.</summary>
		readonly object syncRoot = new object();

		/// <summary>Queue for the tasks which are currently running. The max.length is enforces by the semaphore.</summary>
		readonly Queue<TaskRunner> queueRunning;

		/// <summary>True while runAsyncProcessor() method is dispatching these tasks.</summary>
		bool processorRunning = false;

		/// <summary>Queue for the tasks which were created but not yet added to the queueRunning due to the concurrency limit. Created on demand because it's not needed in most cases.</summary>
		Queue<TaskRunner> queuePending = null;

		public TaskQueue( int queueDepth = defaultQueueDepth )
		{
			maxQueueDepth = queueDepth;
			throttle = new SemaphoreSlim( queueDepth );
			queueRunning = new Queue<TaskRunner>( queueDepth );
		}

		/// <summary>Post new task to the queue. Can be called from any thread.</summary>
		public Task<Task<TResult>> post<TResult>( Func<Task<TResult>> op, out bool throttled )
		{
			TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
			TaskRunner runner = async () =>
			{
				try
				{
					TResult result = await op().ConfigureAwait( false );
					tcs.SetResult( result );
				}
				catch( Exception ex )
				{
					tcs.SetException( ex );
				}
			};
			return post( tcs.Task, runner, out throttled );
		}

		void enqueueRunner( TaskRunner runner )
		{
			if( 0 == ( queuePending?.Count ?? 0 ) )
				queueRunning.Enqueue( runner );
			else
			{
				queueRunning.Enqueue( queuePending.Dequeue() );
				queuePending.Enqueue( runner );
			}
		}

		/// <summary>Post new task to the queue. Can be called from any thread.</summary>
		Task<Task<TResult>> post<TResult>( Task<TResult> task, TaskRunner runner, out bool throttled )
		{
			Task tSemaphore;
			lock( syncRoot )
			{
				if( !processorRunning )
				{
					// The runAsyncProcessor task ain't running. Start now.
					Debug.Assert( throttle.CurrentCount == maxQueueDepth );
					throttle.Wait();

					enqueueRunner( runner );
					processorRunning = true;
					// Intentionally not using Task.Run to save time on locking & scheduling,
					// the initial synchronous part of the runAsyncProcessor needs the very same lock we're in.
					runAsyncProcessor();
					throttled = false;
					return Task.FromResult( task );
				}

				tSemaphore = throttle.WaitAsync();
				if( tSemaphore.IsCompleted )
				{
					Debug.Assert( queueRunning.Count < maxQueueDepth );
					// The processor is already running but there's enough place for one more task.
					enqueueRunner( runner );
					throttled = false;
					return Task.FromResult( task );
				}

				// No space for the new task. Need to sleep on that semaphore.
				// We need FIFO order, semaphores are not fair, using second FIFO queue to enforce the order.
				if( null == queuePending )
					queuePending = new Queue<TaskRunner>();
				queuePending.Enqueue( runner );
			}

			throttled = true;
			return handlePending( tSemaphore, task );
		}

		async Task<Task<TResult>> handlePending<TResult>( Task tSemaphore, Task<TResult> task )
		{
			await tSemaphore.ConfigureAwait( true );
			lock( syncRoot )
			{
				// This doesn't necessarily move runner of the same task, semaphore.WaitAsync() doesn't guarantee FIFO order.
				queueRunning.Enqueue( queuePending.Dequeue() );
				return task;
			}
		}

		async void runAsyncProcessor()
		{
			while( true )
			{
				Func<Task> op;
				lock( syncRoot )
				{
					if( !queueRunning.TryDequeue( out op ) )
					{
						// No more pending items left
						processorRunning = false;
						return;
					}
				}

				// Run the posted task
				await op().ConfigureAwait( false );

				// Release the semaphore once
				throttle.Release();
			}
		}
	}
}