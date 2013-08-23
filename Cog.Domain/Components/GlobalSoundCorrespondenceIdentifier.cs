﻿using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine;

namespace SIL.Cog.Domain.Components
{
	public class GlobalSoundCorrespondenceIdentifier : IProcessor<VarietyPair>
	{
		private readonly SegmentPool _segmentPool;
		private readonly CogProject _project;
		private readonly string _alignerID;

		public GlobalSoundCorrespondenceIdentifier(SegmentPool segmentPool, CogProject project, string alignerID)
		{
			_segmentPool = segmentPool;
			_project = project;
			_alignerID = alignerID;
		}

		private static bool IsOnset(Alignment<Word, ShapeNode> alignment, int column)
		{
			AlignmentCell<ShapeNode> cell1 = alignment[0, column];
			AlignmentCell<ShapeNode> cell2 = alignment[1, column];

			return cell1.First.Type() == CogFeatureSystem.ConsonantType && cell2.First.Type() == CogFeatureSystem.ConsonantType
				&& cell1.First.Annotation.Parent.Children.First == cell1.First.Annotation
				&& cell2.First.Annotation.Parent.Children.First == cell2.First.Annotation;
		}

		private static bool IsNucleus(Alignment<Word, ShapeNode> alignment, int column)
		{
			AlignmentCell<ShapeNode> cell1 = alignment[0, column];
			AlignmentCell<ShapeNode> cell2 = alignment[1, column];

			return cell1.First.Type() == CogFeatureSystem.VowelType && cell2.First.Type() == CogFeatureSystem.VowelType;
		}

		private static bool IsCoda(Alignment<Word, ShapeNode> alignment, int column)
		{
			AlignmentCell<ShapeNode> cell1 = alignment[0, column];
			AlignmentCell<ShapeNode> cell2 = alignment[1, column];

			return cell1.First.Type() == CogFeatureSystem.ConsonantType && cell2.First.Type() == CogFeatureSystem.ConsonantType
				&& cell1.Last.Annotation.Parent.Children.Last == cell1.Last.Annotation
				&& cell2.Last.Annotation.Parent.Children.Last == cell2.Last.Annotation;
		}

		public void Process(VarietyPair data)
		{
			IWordAligner aligner = _project.WordAligners[_alignerID];

			var identifiers = new Dictionary<SyllablePosition, Identifier>
				{
					{SyllablePosition.Onset, new Identifier(IsOnset)},
					{SyllablePosition.Nucleus, new Identifier(IsNucleus)},
					{SyllablePosition.Coda, new Identifier(IsCoda)},
				};

			foreach (WordPair wordPair in data.WordPairs.Where(wp => wp.AreCognatePredicted))
			{
				Alignment<Word, ShapeNode> alignment = aligner.Compute(wordPair).GetAlignments().First();
				for (int i = 0; i < alignment.ColumnCount; i++)
				{
					foreach (Identifier identifier in identifiers.Values)
						identifier.ProcessColumn(_segmentPool, wordPair, alignment, i);
				}
			}

			foreach (KeyValuePair<SyllablePosition, Identifier> kvp in identifiers)
				MergeCorrespondences(kvp.Key, kvp.Value.Correspondences);
		}

		private void MergeCorrespondences(SyllablePosition position, GlobalSoundCorrespondenceCollection source)
		{
			if (source.Count == 0)
				return;

			GlobalSoundCorrespondenceCollection target = _project.GlobalSoundCorrespondenceCollections[position];
			lock (target)
			{
				foreach (GlobalSoundCorrespondence sourceCorr in source)
				{
					GlobalSoundCorrespondence targetCorr;
					if (target.TryGetValue(sourceCorr.Segment1, sourceCorr.Segment2, out targetCorr))
					{
						targetCorr.Frequency += sourceCorr.Frequency;
						targetCorr.WordPairs.AddRange(sourceCorr.WordPairs);
					}
					else
					{
						target.Add(sourceCorr);
					}
				}
			}
		}

		private class Identifier
		{
			private readonly GlobalSoundCorrespondenceCollection _correspondences;
			private readonly Func<Alignment<Word, ShapeNode>, int, bool> _filter;

			public Identifier(Func<Alignment<Word, ShapeNode>, int, bool> filter)
			{
				_correspondences = new GlobalSoundCorrespondenceCollection();
				_filter = filter;
			}

			public GlobalSoundCorrespondenceCollection Correspondences
			{
				get { return _correspondences; }
			}

			public void ProcessColumn(SegmentPool segmentPool, WordPair wp, Alignment<Word, ShapeNode> alignment, int column)
			{
				AlignmentCell<ShapeNode> cell1 = alignment[0, column];
				AlignmentCell<ShapeNode> cell2 = alignment[1, column];
				if (!cell1.IsNull && !cell2.IsNull && _filter(alignment, column))
				{
					Ngram ngram1 = cell1.ToNgram(segmentPool);
					Ngram ngram2 = cell2.ToNgram(segmentPool);
					if (ngram1.Count == 1 && ngram2.Count == 1)
					{
						Segment seg1 = ngram1.First;
						Segment seg2 = ngram2.First;
						if (!seg1.Equals(seg2))
						{
							GlobalSoundCorrespondence corr;
							if (!_correspondences.TryGetValue(seg1, seg2, out corr))
							{
								corr = new GlobalSoundCorrespondence(seg1, seg2);
								_correspondences.Add(corr);
							}
							corr.Frequency++;
							corr.WordPairs.Add(wp);
						}
					}
				}
			}
		}
	}
}