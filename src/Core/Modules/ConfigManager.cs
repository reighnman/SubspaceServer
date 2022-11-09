﻿using Microsoft.Extensions.ObjectPool;
using SS.Core.ComponentCallbacks;
using SS.Core.ComponentInterfaces;
using SS.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace SS.Core.Modules
{
    /// <summary>
    /// Module that manages configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This differs from ASSS in that changes made via the <see cref="SetStr"/> and <see cref="SetInt"/> methods 
    /// can update or insert into existing conf files, rather than just append to the end of a base conf file.
    /// </para>
    /// <para>
    /// To achieve this, it keeps an in-memory object model representation of each conf file (see <see cref="ConfFile"/>).
    /// Keep in mind that this means, if a setting is changed in a shared <see cref="ConfFile"/> (e.g. settings shared by multiple arenas)
    /// it will affect all 'base' configurations (<see cref="ConfDocument"/> objects) that use the <see cref="ConfFile"/>.
    /// </para>
    /// <para>
    /// Also, ASSS watches only base conf files for changes (not #included files).
    /// Whereas, this watches all used conf files, and reloads dependent base configurations when necessary.
    /// </para>
    /// </remarks>
    [CoreModuleInfo]
    public class ConfigManager : IModule, IModuleLoaderAware, IConfigManager, IConfigLogger
    {
        private ComponentBroker broker;
        private ILogManager logManager;
        private IMainloop mainloop;
        private IServerTimer serverTimer;

        private InterfaceRegistrationToken<IConfigManager> _iConfigManagerToken;

        /// <summary>
        /// Path --> ConfFile
        /// </summary>
        private readonly Dictionary<string, ConfFile> files = new Dictionary<string, ConfFile>();

        /// <summary>
        /// Path --> ConfDocument
        /// </summary>
        private readonly Dictionary<string, DocumentInfo> documents = new Dictionary<string, DocumentInfo>();

        // Lock that synchronizes access.  Many can read at the same time.  Only one can write or modify the collections and objects within them at a given time.
        private readonly ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();

        private readonly DefaultObjectPool<List<DocumentInfo>> documentInfoListPool = new(new DocumentInfoListPooledObjectPolicy());

        public bool Load(
            ComponentBroker broker, 
            IMainloop mainloop,
            IServerTimer serverTimer)
        {
            this.broker = broker ?? throw new ArgumentNullException(nameof(broker));
            this.mainloop = mainloop ?? throw new ArgumentNullException(nameof(mainloop));
            this.serverTimer = serverTimer ?? throw new ArgumentNullException(nameof(serverTimer));

            Global = OpenConfigFile(null, null, GlobalChanged);
            if (Global == null)
            {
                Log(LogLevel.Error, "Failed to open global.conf.");
                return false;
            }

            SetTimers();

            _iConfigManagerToken = broker.RegisterInterface<IConfigManager>(this);

            return true;
        }

        public bool Unload(ComponentBroker broker)
        {
            if (broker.UnregisterInterface(ref _iConfigManagerToken) != 0)
                return false;

            return true;
        }

        #region IModuleLoaderAware Members

        bool IModuleLoaderAware.PostLoad(ComponentBroker broker)
        {
            logManager = broker.GetInterface<ILogManager>();
            return true;
        }

        bool IModuleLoaderAware.PreUnload(ComponentBroker broker)
        {
            if (logManager != null)
                broker.ReleaseInterface(ref logManager);

            return true;
        }

        #endregion

        [ConfigHelp("Config", "FlushDirtyValuesInterval", ConfigScope.Global,  typeof(int), DefaultValue = "500", 
            Description = "How often to write modified config settings back to disk (in ticks).")]
        [ConfigHelp("Config", "CheckModifiedFilesInterval", ConfigScope.Global, typeof(int), DefaultValue = "1500",
            Description = "How often to check for modified config files on disk (in ticks).")]
        private void SetTimers()
        {
            int dirty = GetInt(Global, "Config", "FlushDirtyValuesInterval", 500);
            serverTimer.ClearTimer(ServerTimer_SaveChanges, null);
            serverTimer.SetTimer(ServerTimer_SaveChanges, 700, dirty * 10, null);

            int files = GetInt(Global, "Config", "CheckModifiedFilesInterval", 1500);
            serverTimer.ClearTimer(ServerTimer_ReloadModified, null);
            serverTimer.SetTimer(ServerTimer_ReloadModified, 1500, files * 10, null);
        }

        private bool ServerTimer_ReloadModified()
        {
            bool IsReloadNeeded()
            {
                foreach(DocumentInfo documentInfo in documents.Values)
                    if (documentInfo.Document.IsReloadNeeded)
                        return true;

                foreach(ConfFile file in files.Values)
                    if (file.IsReloadNeeded)
                        return true;

                return false;
            }

            List<DocumentInfo> notifyList = null;

            rwLock.EnterUpgradeableReadLock();

            try
            {
                // check if any document needs to be reloaded
                // or any file has been modified on disk and needs to be reloaded
                // note: checking files second since it requires I/O
                if (IsReloadNeeded())
                {
                    rwLock.EnterWriteLock();

                    try
                    {
                        // reload files that have been modified on disk
                        // note: this is done first, because it affects documents
                        // (a document that consists of a file that was reloaded will need to be reloaded afterwards)
                        foreach (ConfFile file in files.Values)
                        {
                            if (file.IsReloadNeeded)
                            {
                                Log(LogLevel.Info, $"Reloading conf file '{file.Path}' from disk.");

                                try
                                {
                                    file.Load();
                                }
                                catch (Exception ex)
                                {
                                    Log(LogLevel.Warn, $"Failed to reload conf file '{file.Path}'. {ex.Message}");
                                }
                            }
                        }

                        // reload each document that needs to be reloaded
                        // note: a document may need to be reloaded if any of the files it consists of was reloaded
                        // or a file shared by multiple documents was updated by 1 of the documents (the other documents will need reloading)
                        foreach (var docInfo in documents.Values)
                        {
                            if (docInfo.Document.IsReloadNeeded)
                            {
                                Log(LogLevel.Info, $"Reloading settings for base conf '{docInfo.Path}'.");
                                docInfo.Document.Load();
                                docInfo.IsChangeNotificationPending = true;
                            }

                            if (docInfo.IsChangeNotificationPending)
                            {
                                if (notifyList == null)
                                    notifyList = documentInfoListPool.Get();

                                notifyList.Add(docInfo);
                            }
                        }

                        // TODO: remove documents that are no longer referenced by a handle

                        // TODO: remove files that are no longer referenced by a document
                    }
                    finally
                    {
                        rwLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                rwLock.ExitUpgradeableReadLock();
            }

            // notify of changes (outside of reader/writer lock)
            if (notifyList != null)
            {
                mainloop.QueueMainWorkItem(MainloopWork_NotifyChanged, notifyList);
            }

            return true;
        }

        private void MainloopWork_NotifyChanged(List<DocumentInfo> notifyList)
        {
            try
            {
                foreach (var docInfo in notifyList)
                {
                    docInfo.NotifyChanged();
                }
            }
            finally
            {
                documentInfoListPool.Return(notifyList);
            }
        }

        private bool ServerTimer_SaveChanges()
        {
            bool IsAnyFileDirty()
            {
                foreach (ConfFile file in files.Values)
                    if (file.IsDirty)
                        return true;

                return false;
            }

            rwLock.EnterUpgradeableReadLock();

            try
            {
                if (IsAnyFileDirty())
                {
                    rwLock.EnterWriteLock();

                    try
                    {
                        foreach (ConfFile file in files.Values)
                        {
                            if (file.IsDirty)
                            {
                                Log(LogLevel.Info, $"Saving changes to conf file '{file.Path}'.");

                                try
                                {
                                    // save the file
                                    // Note: Also updates file.LastModified so that it doesn't appear as being modified to us and get reloaded
                                    file.Save();
                                }
                                catch (Exception ex)
                                {
                                    Log(LogLevel.Warn, $"Failed to save changes to conf file '{file.Path}'. {ex.Message}");
                                }
                            }
                        }
                    }
                    finally
                    {
                        rwLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                rwLock.ExitUpgradeableReadLock();
            }

            return true;
        }

        public ConfigHandle Global { get; private set; }

        private void GlobalChanged()
        {
            // fire the callback on the mainloop thread
            mainloop.QueueMainWorkItem<object>(
                _ => { GlobalConfigChangedCallback.Fire(broker); }, 
                null);            
        }

        public ConfigHandle OpenConfigFile(string arena, string name)
        {
            return OpenConfigFile(
                arena, 
                name, 
                (documentInfo) => documentInfo.CreateHandle(
                    arena != null ? ConfigScope.Arena : ConfigScope.Global, 
                    name));
        }

        public ConfigHandle OpenConfigFile(string arena, string name, ConfigChangedDelegate changedCallback)
        {
            return OpenConfigFile(
                arena,
                name,
                (documentInfo) => documentInfo.CreateHandle(
                    arena != null ? ConfigScope.Arena : ConfigScope.Global,
                    name,
                    changedCallback));
        }

        public ConfigHandle OpenConfigFile<TState>(string arena, string name, ConfigChangedDelegate<TState> changedCallback, TState state)
        {
            return OpenConfigFile(
                arena, 
                name, 
                (documentInfo) => documentInfo.CreateHandle(
                    arena != null ? ConfigScope.Arena : ConfigScope.Global,
                    name,
                    changedCallback,
                    state));
        }

        private ConfigHandle OpenConfigFile(string arena, string name, Func<DocumentInfo, ConfigHandle> createHandle)
        {
            if (createHandle == null)
                throw new ArgumentNullException(nameof(createHandle));

            string path = LocateConfigFile(arena, name);
            if (string.IsNullOrWhiteSpace(path))
            {
                Log(LogLevel.Warn, $"File not found in search paths (arena='{arena}', name='{name}').");
                return null;
            }

            rwLock.EnterWriteLock();

            try
            {
                if (!documents.TryGetValue(path, out DocumentInfo documentInfo))
                {
                    ConfFileProvider fileProvider = new ConfFileProvider(this, arena);
                    ConfDocument document = new ConfDocument(name, fileProvider, this);
                    document.Load();

                    documentInfo = new DocumentInfo(path, document);
                    documents.Add(path, documentInfo);
                }

                return createHandle(documentInfo);
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        public void CloseConfigFile(ConfigHandle handle)
        {
            if (handle == null)
                throw new ArgumentNullException(nameof(handle));

            if (handle is not DocumentHandle documentHandle)
                throw new ArgumentException("Only handles created by this module are valid.", nameof(handle));

            rwLock.EnterWriteLock();

            try
            {
                documentHandle.DocumentInfo?.CloseHandle(documentHandle);
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        public int GetInt(ConfigHandle handle, ReadOnlySpan<char> section, ReadOnlySpan<char> key, int defaultValue)
        {
            if (handle is not DocumentHandle documentHandle)
                throw new ArgumentException("Only handles created by this module are valid.", nameof(handle));

            string value = GetStr(handle, section, key);

            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            if (int.TryParse(value, out int result))
                return result;

            // Check if there's a ConfigHelp attribute for it being an enum type.
            IConfigHelp configHelp = broker.GetInterface<IConfigHelp>();
            if (configHelp != null)
            {
                try
                {
                    // TODO: remove string allocations when the IConfigHelp is spannified
                    string sectionStr = section.ToString();
                    string keyStr = key.ToString();

                    if (configHelp.Sections.Contains(sectionStr))
                    {
                        var helpAttributes =
                            from helpTuple in configHelp.Sections[sectionStr]
                            where string.Equals(helpTuple.Attr.Key, keyStr, StringComparison.OrdinalIgnoreCase)
                                && helpTuple.Attr.Type.IsEnum
                                && helpTuple.Attr.Scope == documentHandle.Scope
                                && string.Equals(helpTuple.Attr.FileName, documentHandle.FileName, StringComparison.OrdinalIgnoreCase)
                            select helpTuple.Attr;

                        foreach (var attribute in helpAttributes)
                        {
                            if (Enum.TryParse(attribute.Type, value, out object enumResult))
                            {
                                return (int)enumResult;
                            }
                        }
                    }
                }
                finally
                {
                    broker.ReleaseInterface(ref configHelp);
                }
            }

            ReadOnlySpan<char> valueSpan = value.AsSpan().Trim();
            if (valueSpan.Equals("Y", StringComparison.OrdinalIgnoreCase)
                || valueSpan.Equals("Yes", StringComparison.OrdinalIgnoreCase)
                || valueSpan.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            return defaultValue; // Note: This differs from ASSS which returns 0.
        }

        public T GetEnum<T>(ConfigHandle handle, ReadOnlySpan<char> section, ReadOnlySpan<char> key, T defaultValue) where T : struct, Enum
        {
            string value = GetStr(handle, section, key);

            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            return Enum.TryParse(value, true, out T enumValue)
                ? enumValue 
                : defaultValue;
        }

        public string GetStr(ConfigHandle handle, ReadOnlySpan<char> section, ReadOnlySpan<char> key)
        {
            if (handle == null)
                throw new ArgumentNullException(nameof(handle));

            if (handle is not DocumentHandle documentHandle)
                throw new ArgumentException("Only handles created by this module are valid.", nameof(handle));

            if (documentHandle.DocumentInfo != null)
            {
                rwLock.EnterReadLock();

                try
                {
                    if (documentHandle.DocumentInfo != null)
                    {
                        if (documentHandle.DocumentInfo.Document.TryGetValue(section, key, out string value))
                        {
                            return value;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                finally
                {
                    rwLock.ExitReadLock();
                }
            }

            throw new InvalidOperationException("Handle is closed.");
        }

        public void SetInt(ConfigHandle handle, string section, string key, int value, string comment, bool permanent)
        {
            SetStr(handle, section, key, value.ToString("D", CultureInfo.InvariantCulture), comment, permanent);
        }

        public void SetEnum<T>(ConfigHandle handle, string section, string key, T value, string comment, bool permanent) where T : struct, Enum
        {
            SetStr(handle, section, key, value.ToString("G"), comment, permanent);
        }

        public void SetStr(ConfigHandle handle, string section, string key, string value, string comment, bool permanent)
        {
            if (handle == null)
                throw new ArgumentNullException(nameof(handle));

            if (handle is not DocumentHandle documentHandle)
                throw new ArgumentException("Only handles created by this module are valid.", nameof(handle));    

            if (documentHandle.DocumentInfo != null)
            {
                rwLock.EnterWriteLock();

                try
                {
                    if (documentHandle.DocumentInfo != null)
                    {
                        documentHandle.DocumentInfo.Document.UpdateOrAddProperty(section, key, value, permanent, comment);

                        // set a flag so that we remember to fire change notifications (done in a timer)
                        documentHandle.DocumentInfo.IsChangeNotificationPending = true;

                        return;
                    }
                }
                finally
                {
                    rwLock.ExitWriteLock();
                }
            }

            throw new InvalidOperationException("Handle is closed.");
        }

        void IConfigLogger.Log(LogLevel level, string message)
        {
            Log(level, message);
        }

        private void Log(LogLevel level, string message)
        {
            if (logManager != null)
            {
                logManager.LogM(level, nameof(ConfigManager), message);
            }
            else
            {
                Console.Error.WriteLine($"{(LogCode)level} <{nameof(ConfigManager)}> {message}");
            }
        }

        // This is called by the ConfigFileProvider which will only be used when a write lock is already held.
        private ConfFile GetConfFile(string arena, string name)
        {
            if (!rwLock.IsWriteLockHeld)
                return null;

            // determine the path of the file
            string path = LocateConfigFile(arena, name);
            if (path == null)
            {
                Log(LogLevel.Warn, $"File not found for arena '{arena}', name '{name}'.");
                return null;
            }

            // check if we already have it loaded
            if (files.TryGetValue(path, out ConfFile file))
            {
                return file;
            }

            // not loaded yet, let's do it
            try
            {
                file = new ConfFile(path, this);
                file.Load();
            }
            catch (Exception ex)
            {
                Log(LogLevel.Warn, $"Failed to load '{path}' for arena '{arena}', name '{name}'. {ex.Message}");
                return null;
            }

            files.Add(path, file);
            return file;
        }

        private static string LocateConfigFile(string arena, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = string.IsNullOrWhiteSpace(arena) ? "global.conf" : "arena.conf";
            }

            return PathUtil.FindFileOnPath(Constants.ConfigSearchPaths, name, arena);
        }

        /// <summary>
        /// Helper class that also keeps track of additional context, an optional arena,
        /// for which files are to be retrieved.
        /// </summary>
        private class ConfFileProvider : IConfFileProvider
        {
            private readonly ConfigManager manager;
            private readonly string arena;

            public ConfFileProvider(ConfigManager manager, string arena)
            {
                this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
                this.arena = arena;
            }

            public ConfFile GetFile(string name) => manager.GetConfFile(arena, name);
        }

        private interface IConfigChangedInvoker
        {
            void Invoke();
        }

        private class ConfigChangedInvoker : IConfigChangedInvoker
        {
            private readonly ConfigChangedDelegate callback;

            public ConfigChangedInvoker(ConfigChangedDelegate callback)
            {
                this.callback = callback ?? throw new ArgumentNullException(nameof(callback));
            }

            public void Invoke()
            {
                callback();
            }
        }

        private class ConfigChangedInvoker<T> : IConfigChangedInvoker
        {
            private readonly ConfigChangedDelegate<T> callback;
            private readonly T state;

            public ConfigChangedInvoker(ConfigChangedDelegate<T> callback, T state)
            {
                this.callback = callback ?? throw new ArgumentNullException(nameof(callback));
                this.state = state;
            }

            public void Invoke()
            {
                callback?.Invoke(state);
            }
        }

        private class DocumentHandle : ConfigHandle
        {
            private readonly IConfigChangedInvoker invoker;

            public DocumentHandle(
                DocumentInfo documentInfo,
                ConfigScope scope,
                string filename,
                IConfigChangedInvoker invoker)
            {
                DocumentInfo = documentInfo ?? throw new ArgumentNullException(nameof(documentInfo));
                Scope = scope;
                FileName = filename;
                this.invoker = invoker; // can be null
            }

            public DocumentInfo DocumentInfo { get; internal set; }

            public ConfigScope Scope { get; private set; }

            public string FileName { get; private set; }

            public void NotifyConfigChanged()
            {
                invoker?.Invoke();
            }
        }

        private class DocumentInfo
        {
            private readonly LinkedList<DocumentHandle> handles = new LinkedList<DocumentHandle>();
            private readonly object lockObj = new object();

            public DocumentInfo(string path, ConfDocument document)
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("A path is required.", nameof(path));

                Path = path;
                Document = document ?? throw new ArgumentNullException(nameof(document));
            }

            public string Path { get; }
            public ConfDocument Document { get; }
            private bool isChangeNotificationPending = false;
            public bool IsChangeNotificationPending
            {
                get
                {
                    lock (lockObj)
                    {
                        return isChangeNotificationPending;
                    }
                }

                set
                {
                    lock (lockObj)
                    {
                        isChangeNotificationPending = value;
                    }
                }
            }

            public DocumentHandle CreateHandle(ConfigScope scope, string fileName)
            {
                DocumentHandle handle = new DocumentHandle(this, scope, fileName,  null);

                lock (lockObj)
                {
                    handles.AddLast(handle);
                }

                return handle;
            }

            public DocumentHandle CreateHandle(ConfigScope scope, string fileName, ConfigChangedDelegate callback)
            {
                DocumentHandle handle = new DocumentHandle(
                    this,
                    scope,
                    fileName,
                    callback != null ? new ConfigChangedInvoker(callback) : null);

                lock (lockObj)
                {
                    handles.AddLast(handle);
                }

                return handle;
            }

            public DocumentHandle CreateHandle<TState>(ConfigScope scope, string fileName, ConfigChangedDelegate<TState> callback, TState state)
            {
                DocumentHandle handle = new DocumentHandle(
                    this,
                    scope,
                    fileName,
                    callback != null ? new ConfigChangedInvoker<TState>(callback, state) : null);

                lock (lockObj)
                {
                    handles.AddLast(handle);
                }

                return handle;
            }

            public bool CloseHandle(DocumentHandle handle)
            {
                if (handle == null)
                    throw new ArgumentNullException(nameof(handle));

                if (handle.DocumentInfo != this)
                    return false;

                handle.DocumentInfo = null;

                lock (lockObj)
                {
                    return handles.Remove(handle);
                }
            }

            public void NotifyChanged()
            {
                lock (lockObj)
                {
                    foreach (DocumentHandle handle in handles)
                    {
                        handle.NotifyConfigChanged();
                    }

                    IsChangeNotificationPending = false;
                }
            }
        }

        private class DocumentInfoListPooledObjectPolicy : PooledObjectPolicy<List<DocumentInfo>>
        {
            public override List<DocumentInfo> Create()
            {
                return new List<DocumentInfo>();
            }

            public override bool Return(List<DocumentInfo> obj)
            {
                if (obj == null)
                    return false;

                obj.Clear();
                return true;
            }
        }
    }
}
