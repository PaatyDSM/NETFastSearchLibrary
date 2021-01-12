using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NETFastSearchLibrary
{
    /// <summary>
    /// Represents a class for fast file search in multiple directories.
    /// </summary>
    public class FileSearchMultiple
    {
        private List<FileSearchBase> searchers;

        private CancellationTokenSource tokenSource;

        private bool suppressOperationCanceledException;


        /// <summary>
        /// Event fires when next portion of files is found. Event handlers are not thread safe. 
        /// </summary>
        public event EventHandler<FileEventArgs> FilesFound
        {
            add
            {
                searchers.ForEach((s) => s.FilesFound += value);
            }

            remove
            {
                searchers.ForEach((s) => s.FilesFound -= value);
            }
        }


        /// <summary>
        /// Event fires when search process is completed or stopped.
        /// </summary>
        public event EventHandler<SearchCompletedEventArgs> SearchCompleted;



        /// <summary>
        /// Calls a SearchCompleted event.
        /// </summary>
        /// <param name="isCanceled">Determines whether search process canceled.</param>
        protected virtual void OnSearchCompleted(bool isCanceled)
        {
            EventHandler<SearchCompletedEventArgs> handler = SearchCompleted;

            if (handler != null)
            {
                var arg = new SearchCompletedEventArgs(isCanceled);

                handler(this, arg);
            }
        }


        #region FileCancellationDelegateSearch constructors

        /// <summary>
        /// Initializes a new instance of FileSearchMultiple class.
        /// </summary>
        /// <param name="folders">Start search directories.</param>
        /// <param name="isValid">The delegate that determines algorithm of file selection.</param>
        /// <param name="tokenSource">Instance of CancellationTokenSource for search process cancellation possibility.</param>
        /// <param name="handlerOption">Specifies where FilesFound event handlers are executed.</param>
        /// <param name="suppressOperationCanceledException">Determines whether necessary suppress OperationCanceledException if it possible.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public FileSearchMultiple(List<string> folders, Func<FileInfo, bool> isValid, CancellationTokenSource tokenSource, ExecuteHandlers handlerOption, bool suppressOperationCanceledException)
        {
            CheckFolders(folders);

            CheckDelegate(isValid);

            CheckTokenSource(tokenSource);

            searchers = new List<FileSearchBase>();

            this.suppressOperationCanceledException = suppressOperationCanceledException;

            foreach (var folder in folders)
            {
                searchers.Add(new FileCancellationDelegateSearch(folder, isValid, handlerOption, false, tokenSource.Token));
            }
            
            this.tokenSource = tokenSource;
        }


        /// <summary>
        /// Initializes a new instance of FileSearchMultiple class.
        /// </summary>
        /// <param name="folders">Start search directories.</param>
        /// <param name="isValid">The delegate that determines algorithm of file selection.</param>
        /// <param name="tokenSource">Instance of CancellationTokenSource for search process cancellation possibility.</param>
        /// <param name="handlerOption">Specifies where FilesFound event handlers are executed.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public FileSearchMultiple(List<string> folders, Func<FileInfo, bool> isValid, CancellationTokenSource tokenSource, ExecuteHandlers handlerOption)
            : this(folders, isValid, tokenSource, handlerOption, true)
        {
        }


        /// <summary>
        /// Initializes a new instance of FileSearchMultiple class.
        /// </summary>
        /// <param name="folders">Start search directories.</param>
        /// <param name="isValid">The delegate that determines algorithm of file selection.</param>
        /// <param name="tokenSource">Instance of CancellationTokenSource for search process cancellation possibility.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public FileSearchMultiple(List<string> folders, Func<FileInfo, bool> isValid, CancellationTokenSource tokenSource)
            : this(folders, isValid, tokenSource, ExecuteHandlers.InCurrentTask, true)
        {
        }

        #endregion


        #region FileCancellationPatternSearch constructors

        /// <summary>
        /// Initializes a new instance of FileSearchMultiple class.
        /// </summary>
        /// <param name="folders">Start search directories.</param>
        /// <param name="pattern">The search pattern.</param>
        /// <param name="tokenSource">Instance of CancellationTokenSource for search process cancellation possibility.</param>
        /// <param name="handlerOption">Specifies where FilesFound event handlers are executed.</param>
        /// <param name="suppressOperationCanceledException">Determines whether necessary suppress OperationCanceledException if it possible.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public FileSearchMultiple(List<string> folders, string pattern, CancellationTokenSource tokenSource, ExecuteHandlers handlerOption, bool suppressOperationCanceledException)
        {
            CheckFolders(folders);

            CheckPattern(pattern);

            CheckTokenSource(tokenSource);

            searchers = new List<FileSearchBase>();

            this.suppressOperationCanceledException = suppressOperationCanceledException;

            foreach (var folder in folders)
            {
                searchers.Add(new FileCancellationPatternSearch(folder, pattern, handlerOption, false, tokenSource.Token));
            }

            this.tokenSource = tokenSource;
        }


        /// <summary>
        /// Initializes a new instance of FileSearchMultiple class.
        /// </summary>
        /// <param name="folders">Start search directories.</param>
        /// <param name="pattern">The search pattern.</param>
        /// <param name="tokenSource">Instance of CancellationTokenSource for search process cancellation possibility.</param>
        /// <param name="handlerOption">Specifies where FilesFound event handlers are executed.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public FileSearchMultiple(List<string> folders, string pattern, CancellationTokenSource tokenSource, ExecuteHandlers handlerOption)
            : this(folders, pattern, tokenSource, handlerOption, true)
        {
        }


        /// <summary>
        /// Initializes a new instance of FileSearchMultiple class.
        /// </summary>
        /// <param name="folders">Start search directories.</param>
        /// <param name="pattern">The search pattern.</param>
        /// <param name="tokenSource">Instance of CancellationTokenSource for search process cancellation possibility.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public FileSearchMultiple(List<string> folders, string pattern, CancellationTokenSource tokenSource) 
            : this(folders, pattern, tokenSource, ExecuteHandlers.InCurrentTask, true)
        {
        }


        /// <summary>
        /// Initializes a new instance of FileSearchMultiple class.
        /// </summary>
        /// <param name="folders">Start search directories.</param>
        /// <param name="tokenSource">Instance of CancellationTokenSource for search process cancellation possibility.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public FileSearchMultiple(List<string> folders, CancellationTokenSource tokenSource) 
            : this(folders, "*", tokenSource, ExecuteHandlers.InCurrentTask, true)
        {
        }

        #endregion


        #region Checking methods
        private void CheckFolders(List<string> folders)
        {
            if (folders == null)
                throw new ArgumentNullException(nameof(folders), "Argument is null.");

            if (folders.Count == 0)
                throw new ArgumentException("Argument is an empty list.", nameof(folders));

            foreach (var folder in folders)
                CheckFolder(folder);
        }

        private static void CheckFolder(string folder)
        {
            if (folder == null)
                throw new ArgumentNullException(nameof(folder), "Argument is null.");

            if (string.IsNullOrEmpty(folder))
                throw new ArgumentException("Argument is not valid.", nameof(folder));

            DirectoryInfo dir = new DirectoryInfo(folder);

            if (!dir.Exists)
                throw new ArgumentException("Argument does not represent an existing directory.", nameof(folder));
        }


        private void CheckPattern(string pattern)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern), "Argument is null.");

            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentException("Argument is not valid.", nameof(pattern));
        }


        private void CheckDelegate(Func<FileInfo, bool> isValid)
        {
            if (isValid == null)
                throw new ArgumentNullException(nameof(isValid), "Argument is null.");
        }


        private void CheckTokenSource(CancellationTokenSource tokenSource)
        {
            if (tokenSource == null)
                throw new ArgumentNullException(nameof(tokenSource), "Argument is null.");
        }


        #endregion


        /// <summary>
        /// Starts a file search operation with realtime reporting using several threads in thread pool.
        /// </summary>
        public void StartSearch()
        {
            try
            {
                searchers.ForEach(s =>
                {
                    s.StartSearch();
                });
            }
            catch(OperationCanceledException ex)
            {
                OnSearchCompleted(true);
                if (!suppressOperationCanceledException)
                    throw;
                return;
            }

            OnSearchCompleted(false);
        }


        /// <summary>
        /// Starts a file search operation with realtime reporting using several threads in thread pool as an asynchronous operation.
        /// </summary>
        public Task StartSearchAsync()
        {
             return TaskEx.Run(() =>
             {
                  StartSearch();

             }, tokenSource.Token);      
        }


        /// <summary>
        /// Stops a file search operation.
        /// </summary>
        public void StopSearch()
        {
            tokenSource.Cancel();
        }

    }
}
