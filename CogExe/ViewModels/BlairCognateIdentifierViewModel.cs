﻿using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using SIL.Cog.Components;
using SIL.Cog.Services;

namespace SIL.Cog.ViewModels
{
	public class BlairCognateIdentifierViewModel : ComponentSettingsViewModelBase
	{
		private bool _ignoreRegularInsertionDeletion;
		private bool _regularConsEqual;
		private readonly ComponentOptionsViewModel _similarSegments;
		private readonly SegmentMappingsViewModel _ignoredMappings;

		public BlairCognateIdentifierViewModel(IDialogService dialogService, IImportService importService, CogProject project)
			: base("Blair", project)
		{
			_ignoredMappings = new SegmentMappingsViewModel(dialogService, importService, project);
			_ignoredMappings.Mappings.CollectionChanged += MappingsChanged;
			_similarSegments = new ComponentOptionsViewModel("Similar segments", "Type", project, 0,
				new ThresholdSimilarSegmentMappingsViewModel(Project), new ListSimilarSegmentMappingsViewModel(dialogService, importService, Project));
			_similarSegments.PropertyChanged += ChildPropertyChanged;
		}

		public BlairCognateIdentifierViewModel(IDialogService dialogService, IImportService importService, CogProject project, BlairCognateIdentifier cognateIdentifier)
			: base("Blair", project)
		{
			_ignoreRegularInsertionDeletion = cognateIdentifier.IgnoreRegularInsertionDeletion;
			_regularConsEqual = cognateIdentifier.RegularConsonantEqual;
			var ignoredMappings = (ListSegmentMappings) cognateIdentifier.IgnoredMappings;
			_ignoredMappings = new SegmentMappingsViewModel(dialogService, importService, project, ignoredMappings.Mappings);
			_ignoredMappings.Mappings.CollectionChanged += MappingsChanged;

			var similarSegments = (TypeSegmentMappings) cognateIdentifier.SimilarSegments;
			if (similarSegments.VowelMappings is ThresholdSegmentMappings)
			{
				_similarSegments = new ComponentOptionsViewModel("Similar segments", "Type", project, 0,
					new ThresholdSimilarSegmentMappingsViewModel(Project, similarSegments),
					new ListSimilarSegmentMappingsViewModel(dialogService, importService, Project));
			}
			else if (similarSegments.VowelMappings is ListSegmentMappings)
			{
				_similarSegments = new ComponentOptionsViewModel("Similar segments", "Type", project, 1,
					new ThresholdSimilarSegmentMappingsViewModel(Project),
					new ListSimilarSegmentMappingsViewModel(dialogService, importService, Project, similarSegments));
			}
			Debug.Assert(_similarSegments != null);
			_similarSegments.PropertyChanged += ChildPropertyChanged;
		}

		private void MappingsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			IsChanged = true;
		}

		public override void AcceptChanges()
		{
			base.AcceptChanges();
			_similarSegments.AcceptChanges();
		}

		public bool IgnoreRegularInsertionDeletion
		{
			get { return _ignoreRegularInsertionDeletion; }
			set
			{
				Set(() => IgnoreRegularInsertionDeletion, ref _ignoreRegularInsertionDeletion, value);
				IsChanged = true;
			}
		}

		public bool RegularConsonantsEqual
		{
			get { return _regularConsEqual; }
			set
			{
				Set(() => RegularConsonantsEqual, ref _regularConsEqual, value);
				IsChanged = true;
			}
		}

		public SegmentMappingsViewModel IgnoredMappings
		{
			get { return _ignoredMappings; }
		}

		public ComponentOptionsViewModel SimilarSegments
		{
			get { return _similarSegments; }
		}

		public override object UpdateComponent()
		{
			var cognateIdentifier = new BlairCognateIdentifier(Project, _ignoreRegularInsertionDeletion, _regularConsEqual,
				"primary", new ListSegmentMappings(Project, _ignoredMappings.Mappings.Select(m => Tuple.Create(m.Segment1, m.Segment2)), false),
				(ISegmentMappings) _similarSegments.UpdateComponent());
			Project.VarietyPairProcessors["cognateIdentifier"] = cognateIdentifier;
			return cognateIdentifier;
		}
	}
}
