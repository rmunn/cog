using System.Collections.Generic;
using System.Collections.Specialized;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.FeatureModel;

namespace SIL.Cog
{
	public class CogProject : NotifyPropertyChangedBase
	{
		private FeatureSystem _featSys;
		private readonly Segmenter _segmenter;

		private readonly BulkObservableCollection<Variety> _varieties;
		private readonly BulkObservableCollection<Sense> _senses;
		private readonly VarietyPairCollection _varietyPairs;

		private readonly ObservableDictionary<string, IAligner> _aligners; 

		private readonly ObservableDictionary<string, IProcessor<CogProject>> _projectProcessors; 
		private readonly ObservableDictionary<string, IProcessor<Variety>> _varietyProcessors;
		private readonly ObservableDictionary<string, IProcessor<VarietyPair>> _varietyPairProcessors;

		public CogProject(SpanFactory<ShapeNode> spanFactory)
		{
			_segmenter = new Segmenter(spanFactory);
			_senses = new BulkObservableCollection<Sense>();
			_senses.CollectionChanged += SensesChanged;
			_varieties = new BulkObservableCollection<Variety>();
			_varieties.CollectionChanged += VarietiesChanged;
			_varietyPairs = new VarietyPairCollection();

			_aligners = new ObservableDictionary<string, IAligner>();

			_projectProcessors = new ObservableDictionary<string, IProcessor<CogProject>>();
			_varietyProcessors = new ObservableDictionary<string, IProcessor<Variety>>();
			_varietyPairProcessors = new ObservableDictionary<string, IProcessor<VarietyPair>>();
		}

		private void SensesChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Remove:
				case NotifyCollectionChangedAction.Replace:
				case NotifyCollectionChangedAction.Reset:
					if (_senses.Count > 0)
					{
						var senses = new HashSet<Sense>(_senses);
						foreach (Variety variety in _varieties)
							variety.Words.RemoveAll(w => !senses.Contains(w.Sense));
					}
					else
					{
						foreach (Variety variety in _varieties)
							variety.Words.Clear();
					}
					break;
			}
		}

		private void VarietiesChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Remove:
				case NotifyCollectionChangedAction.Replace:
				case NotifyCollectionChangedAction.Reset:
					if (_varieties.Count > 0)
					{
						var varieties = new HashSet<Variety>(_varieties);
						_varietyPairs.RemoveAll(vp => !varieties.Contains(vp.Variety1) || !varieties.Contains(vp.Variety2));
					}
					else
					{
						_varietyPairs.Clear();
					}
					break;
			}
		}

		public FeatureSystem FeatureSystem
		{
			get { return _featSys; }
			set
			{
				_featSys = value;
				OnPropertyChanged("FeatureSystem");
			}
		}

		public Segmenter Segmenter
		{
			get { return _segmenter; }
		}

		public BulkObservableCollection<Sense> Senses
		{
			get { return _senses; }
		}

		public BulkObservableCollection<Variety> Varieties
		{
			get { return _varieties; }
		}

		public BulkObservableCollection<VarietyPair> VarietyPairs
		{
			get { return _varietyPairs; }
		}

		public ObservableDictionary<string, IAligner> Aligners
		{
			get { return _aligners; }
		}

		public ObservableDictionary<string, IProcessor<CogProject>> ProjectProcessors
		{
			get { return _projectProcessors; }
		}

		public ObservableDictionary<string, IProcessor<Variety>> VarietyProcessors
		{
			get { return _varietyProcessors; }
		}

		public ObservableDictionary<string, IProcessor<VarietyPair>> VarietyPairProcessors
		{
			get { return _varietyPairProcessors; }
		}
	}
}