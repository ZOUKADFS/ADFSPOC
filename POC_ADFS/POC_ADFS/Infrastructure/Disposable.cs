namespace POC_ADFS.Core.Infrastructure
{
	using System;

	/// <summary>
	/// </summary>
	public abstract class Disposable : IDisposable
	{
		private bool _disposed;

		/// <summary>
		///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		///     Finalizes an instance of the <see cref="Disposable" /> class.
		/// </summary>
		~Disposable()
		{
			Dispose(false);
		}

		/// <summary>
		///     Releases unmanaged and - optionally - managed resources.
		/// </summary>
		/// <param name="disposing">
		///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		private void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					DisposeManaged();
				}
				ReleaseUnmanaged();
			}

			_disposed = true;
		}

		/// <summary>
		///     Release unmanaged resources
		/// </summary>
		protected virtual void ReleaseUnmanaged()
		{
		}

		/// <summary>
		///     Disposes Managed resources.
		/// </summary>
		protected abstract void DisposeManaged();
	}
}