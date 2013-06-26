﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using GalaSoft.MvvmLight.Command;
using SIL.Cog.Components;
using SIL.Cog.Services;
using SIL.Collections;
using SIL.Machine;

namespace SIL.Cog.ViewModels
{
	public class WordListsViewModel : WorkspaceViewModelBase
	{
		private readonly SpanFactory<ShapeNode> _spanFactory; 
		private readonly IDialogService _dialogService;
		private readonly IImportService _importService;
		private readonly IExportService _exportService;
		private readonly IProgressService _progressService;
		private CogProject _project;
		private ReadOnlyMirroredCollection<Sense, SenseViewModel> _senses;
 		private ReadOnlyMirroredCollection<Variety, WordListsVarietyViewModel> _varieties;
		private bool _isEmpty;

		public WordListsViewModel(SpanFactory<ShapeNode> spanFactory, IDialogService dialogService, IProgressService progressService, IImportService importService, IExportService exportService)
			: base("Word lists")
		{
			_spanFactory = spanFactory;
			_dialogService = dialogService;
			_importService = importService;
			_progressService = progressService;
			_exportService = exportService;

			TaskAreas.Add(new TaskAreaCommandsViewModel("Common tasks",
					new CommandViewModel("Add a new variety", new RelayCommand(AddNewVariety)),
 					new CommandViewModel("Add a new sense", new RelayCommand(AddNewSense))));

			TaskAreas.Add(new TaskAreaCommandsViewModel("Other tasks",
					new CommandViewModel("Import word lists", new RelayCommand(Import)),
					new CommandViewModel("Export word lists", new RelayCommand(Export)),
					new CommandViewModel("Run stemmer", new RelayCommand(RunStemmer))));
			_isEmpty = true;
		}

		private void AddNewVariety()
		{
			var vm = new EditVarietyViewModel(_project);
			if (_dialogService.ShowDialog(this, vm) == true)
			{
				_project.Varieties.Add(new Variety(vm.Name));
				IsChanged = true;
			}
		}

		private void AddNewSense()
		{
			var vm = new EditSenseViewModel(_project);
			if (_dialogService.ShowDialog(this, vm) == true)
			{
				_project.Senses.Add(new Sense(vm.Gloss, vm.Category));
				IsChanged = true;
			}
		}

		private void Import()
		{
			if (_importService.ImportWordLists(this, _project))
				IsChanged = true;
		}

		private void Export()
		{
			_exportService.ExportWordLists(this, _project);
		}

		private void RunStemmer()
		{
			var vm = new RunStemmerViewModel(true);
			if (_dialogService.ShowDialog(this, vm) == true)
			{
				IEnumerable<IProcessor<Variety>> processors = null;
				switch (vm.Method)
				{
					case StemmingMethod.Automatic:
						foreach (Variety variety in _project.Varieties)
							variety.Affixes.Clear();
						processors = new[] {_project.VarietyProcessors["affixIdentifier"], new Stemmer(_spanFactory, _project)};
						break;
					case StemmingMethod.Hybrid:
						processors = new[] {_project.VarietyProcessors["affixIdentifier"], new Stemmer(_spanFactory, _project)};
						break;
					case StemmingMethod.Manual:
						processors = new[] {new Stemmer(_spanFactory, _project)};
						break;
				}
				Debug.Assert(processors != null);
				var pipeline = new MultiThreadedPipeline<Variety>(processors);

				var progressVM = new ProgressViewModel(pvm =>
					{
						pvm.Text = "Stemming all varieties...";
						pipeline.Process(_project.Varieties);
						while (!pipeline.WaitForComplete(100))
						{
							if (pvm.Canceled)
							{
								pipeline.Cancel();
								pipeline.WaitForComplete();
							}
						}
					});
				pipeline.ProgressUpdated += (sender, e) => progressVM.Value = e.PercentCompleted;

				_progressService.ShowProgress(this, progressVM);
				IsChanged = true;
			}
		}

		public bool IsEmpty
		{
			get { return _isEmpty; }
			set { Set(() => IsEmpty, ref _isEmpty, value); }
		}

		public ReadOnlyObservableCollection<SenseViewModel> Senses
		{
			get { return _senses; }
		}

		public ReadOnlyObservableCollection<WordListsVarietyViewModel> Varieties
		{
			get { return _varieties; }
		}

		public override void Initialize(CogProject project)
		{
			_project = project;
			Set("Senses", ref _senses, new ReadOnlyMirroredCollection<Sense, SenseViewModel>(project.Senses, sense => new SenseViewModel(sense)));
			Set("Varieties", ref _varieties, new ReadOnlyMirroredCollection<Variety, WordListsVarietyViewModel>(project.Varieties, variety =>
				{
					var vm = new WordListsVarietyViewModel(project, variety);
					vm.PropertyChanged += ChildPropertyChanged;
					return vm;
				}));
			SetIsEmpty();
			_project.Varieties.CollectionChanged += VarietiesChanged;
			_project.Senses.CollectionChanged += SensesChanged;
		}

		public override void AcceptChanges()
		{
			base.AcceptChanges();
			ChildrenAcceptChanges(_varieties);
		}

		private void SensesChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			SetIsEmpty();
		}

		private void VarietiesChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			SetIsEmpty();
		}

		private void SetIsEmpty()
		{
			IsEmpty = _varieties.Count == 0 && _senses.Count == 0;
		}
	}
}