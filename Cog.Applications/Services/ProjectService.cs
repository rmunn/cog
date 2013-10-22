﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using GalaSoft.MvvmLight.Messaging;
using ProtoBuf;
using SIL.Cog.Applications.ViewModels;
using SIL.Cog.Domain;
using SIL.Cog.Domain.Config;
using SIL.Machine;

namespace SIL.Cog.Applications.Services
{
	public class ProjectService : IProjectService
	{
		private const int CacheVersion = 1;

		private static readonly FileType CogProjectFileType = new FileType("Cog Project", ".cogx");

		public event EventHandler<EventArgs> ProjectOpened;

		private readonly SpanFactory<ShapeNode> _spanFactory;
		private readonly SegmentPool _segmentPool;
		private readonly IDialogService _dialogService;
		private readonly IBusyService _busyService;
		private readonly ISettingsService _settingsService;
		private readonly Lazy<IAnalysisService> _analysisService;

		private CogProject _project;
		private string _projectName;
		private bool _isChanged;
		private bool _isNew;
		private FileStream _projectFileStream;

		public ProjectService(SpanFactory<ShapeNode> spanFactory, SegmentPool segmentPool, IDialogService dialogService, IBusyService busyService,
			ISettingsService settingsService, Lazy<IAnalysisService> analysisService)
		{
			_spanFactory = spanFactory;
			_segmentPool = segmentPool;
			_dialogService = dialogService;
			_busyService = busyService;
			_settingsService = settingsService;
			_analysisService = analysisService;

			Messenger.Default.Register<DomainModelChangedMessage>(this, HandleDomainModelChanged);
			Messenger.Default.Register<ComparisonPerformedMessage>(this, HandleComparisonPerformed);
		}

		private void HandleDomainModelChanged(DomainModelChangedMessage msg)
		{
			_isChanged = true;
			if (msg.AffectsComparison)
				_project.VarietyPairs.Clear();
		}

		private void HandleComparisonPerformed(ComparisonPerformedMessage msg)
		{
			if (_projectFileStream != null && !_isChanged)
				SaveComparisonCache();
		}

		public bool Init()
		{
			string projectPath = _settingsService.LastProject;
			bool usingCmdLineArg = false;
			string[] args = Environment.GetCommandLineArgs();
			if (args.Length > 1)
			{
				projectPath = args[1];
				usingCmdLineArg = true;
			}

			if (string.IsNullOrEmpty(projectPath) || projectPath == "<new>" || !File.Exists(projectPath))
			{
				NewProject();
			}
			else
			{
				try
				{
					OpenProject(projectPath);
				}
				catch (ConfigException)
				{
					if (usingCmdLineArg)
					{
						_dialogService.ShowError(null, "The specified file is not a valid Cog configuration file.", "Cog");
						CloseProject();
						return false;
					}
					NewProject();
				}
				catch (IOException ioe)
				{
					if (usingCmdLineArg)
					{
						_dialogService.ShowError(null, string.Format("Error opening project file:\n{0}", ioe.Message), "Cog");
						CloseProject();
						return false;
					}
					NewProject();
				}
			}

			return true;
		}

		public bool New(object ownerViewModel)
		{
			if (_isNew && !_isChanged)
			{
				NewProject();
				return true;
			}

			StartNewProcess("<new>", 5000);
			return false;
		}

		private void StartNewProcess(string projectPath, int timeout)
		{
			_busyService.ShowBusyIndicator(() =>
				{
					Process process = Process.Start(Assembly.GetEntryAssembly().Location, string.Format("\"{0}\"", projectPath));
					Debug.Assert(process != null);
					var stopwatch = new Stopwatch();
					stopwatch.Start();
					while (process.MainWindowHandle == IntPtr.Zero && stopwatch.ElapsedMilliseconds < timeout)
					{
						Thread.Sleep(100);
						process.Refresh();
					}
					stopwatch.Stop();
				});
		}

		private void NewProject()
		{
			_busyService.ShowBusyIndicatorUntilUpdated();
			Stream stream = Assembly.GetAssembly(GetType()).GetManifestResourceStream("SIL.Cog.Applications.NewProject.cogx");
			CogProject project = ConfigManager.Load(_spanFactory, _segmentPool, stream);
			SetupProject(null, "New Project", project);
			_isNew = true;
		}

		public bool Open(object ownerViewModel)
		{
			if (_isNew && !_isChanged)
			{
				FileDialogResult result = _dialogService.ShowOpenFileDialog(ownerViewModel, CogProjectFileType);
				if (result.IsValid && result.FileName != _settingsService.LastProject)
				{
					try
					{
						OpenProject(result.FileName);
						return true;
					}
					catch (ConfigException)
					{
						_dialogService.ShowError(ownerViewModel, "The specified file is not a valid Cog configuration file.", "Cog");
						CloseProject();
					}
					catch (IOException ioe)
					{
						_dialogService.ShowError(ownerViewModel, string.Format("Error opening project file:\n{0}", ioe.Message), "Cog");
						CloseProject();
					}
				}
			}
			else
			{
				FileDialogResult result = _dialogService.ShowOpenFileDialog(ownerViewModel, CogProjectFileType);
				if (result.IsValid && result.FileName != _settingsService.LastProject)
					StartNewProcess(result.FileName, 5000);
			}

			return false;
		}

		public bool Close(object ownerViewModel)
		{
			if (IsChanged)
			{
				bool? res = _dialogService.ShowQuestion(ownerViewModel, "Do you want to save the changes to this project?", "Cog");
				if (res == true)
					Save(ownerViewModel);
				else if (res == null)
					return false;
			}
			CloseProject();
			return true;
		}

		private void CloseProject()
		{
			if (_projectFileStream != null)
			{
				_projectFileStream.Close();
				_projectFileStream = null;
			}
			_project = null;
			_projectName = null;
			_isChanged = false;
		}

		private void OpenProject(string path)
		{
			_busyService.ShowBusyIndicatorUntilUpdated();
			_projectFileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			CogProject project = ConfigManager.Load(_spanFactory, _segmentPool, _projectFileStream);
			SetupProject(path, Path.GetFileNameWithoutExtension(path), project);
			_isNew = false;
		}

		public bool Save(object ownerViewModel)
		{
			if (_projectFileStream == null)
			{
				FileDialogResult result = _dialogService.ShowSaveFileDialog(ownerViewModel, CogProjectFileType);
				if (result.IsValid)
				{
					SaveAsProject(result.FileName);
					return true;
				}

				return false;
			}

			SaveProject();
			return true;
		}

		public bool SaveAs(object ownerViewModel)
		{
			FileDialogResult result = _dialogService.ShowSaveFileDialog(ownerViewModel, CogProjectFileType);
			if (result.IsValid)
			{
				SaveAsProject(result.FileName);
				return true;
			}

			return false;
		}

		private void SaveAsProject(string path)
		{
			_projectFileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
			SaveProject();
			_settingsService.LastProject = path;
			_projectName = Path.GetFileNameWithoutExtension(path);
		}

		private void SaveProject()
		{
			_projectFileStream.Seek(0, SeekOrigin.Begin);
			ConfigManager.Save(_project, _projectFileStream);
			_projectFileStream.Flush();
			_projectFileStream.SetLength(_projectFileStream.Position);
			SaveComparisonCache();
			_isChanged = false;
			_isNew = false;
		}

		private void SetupProject(string path, string name, CogProject project)
		{
			_settingsService.LastProject = path;
			_isChanged = false;
			_project = project;
			_projectName = name;

			_analysisService.Value.SegmentAll();

			if (path != null)
			{
				string cogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SIL", "Cog");
				string cacheFileName = Path.Combine(cogPath, name + ".cache");
				if (File.Exists(cacheFileName))
				{
					bool delete = true;
					try
					{
						using (FileStream fs = File.OpenRead(cacheFileName))
						{
							var version = Serializer.DeserializeWithLengthPrefix<int>(fs, PrefixStyle.Base128, 1);
							if (version == CacheVersion)
							{
								var hash = Serializer.DeserializeWithLengthPrefix<string>(fs, PrefixStyle.Base128, 1);
								if (hash == CalcProjectHash())
								{
									_project.VarietyPairs.AddRange(Serializer.DeserializeItems<VarietyPairSurrogate>(fs, PrefixStyle.Base128, 1)
										.Select(surrogate => surrogate.ToVarietyPair(_segmentPool, _project)));
									delete = false;
								}
							}
						}
					}
					catch (Exception)
					{
						_dialogService.ShowError(this, "An error ocurred while loading the cached comparison results. You must re-run the comparison.", "Cog");
					}
					if (delete)
						File.Delete(cacheFileName);
				}
			}

			OnProjectOpened(new EventArgs());
		}

		private void SaveComparisonCache()
		{
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SIL", "Cog");
			string name = Path.GetFileNameWithoutExtension(_settingsService.LastProject);
			string cacheFileName = Path.Combine(path, name + ".cache");
			if (_project.VarietyPairs.Count == 0)
			{
				if (File.Exists(cacheFileName))
					File.Delete(cacheFileName);
			}
			else
			{
				Directory.CreateDirectory(path);
				using (FileStream fs = File.Create(cacheFileName))
				{
					Serializer.SerializeWithLengthPrefix(fs, CacheVersion, PrefixStyle.Base128, 1);
					Serializer.SerializeWithLengthPrefix(fs, CalcProjectHash(), PrefixStyle.Base128, 1);
					foreach (VarietyPair vp in _project.VarietyPairs)
					{
						var surrogate = new VarietyPairSurrogate(vp);
						Serializer.SerializeWithLengthPrefix(fs, surrogate, PrefixStyle.Base128, 1);
					}
				}
			}
		}

		private string CalcProjectHash()
		{
			using (var md5 = MD5.Create())
			{
				_projectFileStream.Seek(0, SeekOrigin.Begin);
				return BitConverter.ToString(md5.ComputeHash(_projectFileStream)).Replace("-","").ToLower();
			}
		}

		private void OnProjectOpened(EventArgs e)
		{
			if (ProjectOpened != null)
				ProjectOpened(this, e);
		}

		public bool IsChanged
		{
			get { return _isChanged; }
		}

		public CogProject Project
		{
			get { return _project; }
		}

		public string ProjectName
		{
			get { return _projectName; }
		}
	}
}
